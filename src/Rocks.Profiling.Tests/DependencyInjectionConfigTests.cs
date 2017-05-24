using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Rocks.SimpleInjector.NotThreadSafeCheck;
using Rocks.SimpleInjector.NotThreadSafeCheck.Models;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using SimpleInjector.Lifestyles;
using Xunit;

namespace Rocks.Profiling.Tests
{
    public class DependencyInjectionConfigTests
    {
        [Fact]
        public void Setup_Always_Verifiable_AndContainsNoDiagnosticWarnings()
        {
            // arrange
            var container = new Container { Options = { AllowOverridingRegistrations = true } };
            ProfilingLibrary.Setup(() => null, container);


            // act, assert
            container.Verify();

            var result = Analyzer.Analyze(container);

            result = result.Where(x => x.DiagnosticType != DiagnosticType.SingleResponsibilityViolation).ToArray();
            result.Should().BeEmpty(because: Environment.NewLine + string.Join(Environment.NewLine, result.Select(x => x.Description)));
        }


        [Fact]
        public void Setup_Always_DoesNotHaveNonThreadSafeSingletons()
        {
            // arrange
            var container = new Container { Options = { AllowOverridingRegistrations = true } };
            ProfilingLibrary.Setup(() => null, container);

            var assembly = typeof(ProfilingLibrary).Assembly;


            // act
            var result = container
                .GetRegistrationsInfo(x => x.Lifestyle == Lifestyle.Singleton && x.ServiceType.Assembly == assembly)
                .Where(x => !x.HasSingletonAttribute &&
                            x.HasNotThreadSafeMembers &&
                            !x.ImplementationType.Name.EndsWith("Configuration"))
                .Select(x => $"Potential non thread safe singleton {x.ImplementationType}:\n" +
                             $"{string.Join(Environment.NewLine, x.NotThreadSafeMembers)}\n\n")
                .ToList();


            // assert
            result.Should()
                  .BeEmpty(because: Environment.NewLine +
                                    "there are potential non thread safe singleton registrations " +
                                    "which are not marked with [Singleton] attribute." +
                                    Environment.NewLine + Environment.NewLine +
                                    string.Join(Environment.NewLine, result));
        }


        [Fact]
        public void Setup_Always_DoesNotHaveThreadSafeNonSingletons()
        {
            // arrange
            var container = new Container { Options = { AllowOverridingRegistrations = true } };
            ProfilingLibrary.Setup(() => null, container);

            var assembly = typeof(ProfilingLibrary).Assembly;


            // act
            IList<SimpleInjectorRegistrationInfo> registration_infos;
            List<string> result;

            using (AsyncScopedLifestyle.BeginScope(container))
            {
                registration_infos = container.GetRegistrationsInfo(x => x.Lifestyle != Lifestyle.Singleton &&
                                                                         x.ServiceType.Assembly == assembly,
                                                                    x => x.KnownNotMutableTypes.Add(typeof(Container)));

                result = registration_infos
                    .Where(x => !x.HasNotThreadSafeMembers && !x.HasNotSingletonAttribute)
                    .Select(x => $"Potential thread safe non singleton: {x.ImplementationType}.")
                    .OrderBy(x => x)
                    .ToList();
            }


            // assert
            result.Should()
                  .BeEmpty(because: Environment.NewLine +
                                    "there are potential thread safe non singleton registrations " +
                                    "which are not marked with [NotSingleton] attribute." +
                                    Environment.NewLine + Environment.NewLine +
                                    string.Join(Environment.NewLine, result) +
                                    Environment.NewLine + Environment.NewLine);

            foreach (var message in registration_infos.Where(x => !x.HasNotSingletonAttribute)
                                                      .Select(x => $"Not singleton: {x.ImplementationType}")
                                                      .OrderBy(x => x))
                Console.WriteLine(message);
        }
    }
}