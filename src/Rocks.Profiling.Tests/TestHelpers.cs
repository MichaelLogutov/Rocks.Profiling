using System;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Moq;
using Ploeh.AutoFixture;

namespace Rocks.Profiling.Tests
{
    internal static class TestHelpers
    {
        /// <summary>
        ///     Sets the property value for the object.
        /// </summary>
        public static void SetPropertyValue<TModel, TProperty>([NotNull] this TModel obj,
                                                               [NotNull] Expression<Func<TModel, TProperty>> property,
                                                               [CanBeNull] TProperty value)
        {
            var expression = (MemberExpression) property.Body;
            var property_name = expression.Member.Name;

            obj.SetPropertyValue(property_name, value);
        }


        /// <summary>
        ///     Sets the property value for the object.
        /// </summary>
        public static void SetPropertyValue([NotNull] this object obj, [NotNull] string propertyName, [CanBeNull] object value)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Argument is null or whitespace", nameof(propertyName));

            var type = obj.GetType();

            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
                throw new InvalidOperationException($"Property \"{propertyName}\" was not found on \"{type}\".");

            if (property.CanWrite)
                property.SetValue(obj, value);
            else
            {
                var property_backing_field_name = $"<{propertyName}>k__BackingField";

                var backing_field = type.GetField(property_backing_field_name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (backing_field == null)
                    throw new InvalidOperationException($"Unable to find backing field for property \"{propertyName}\" on \"{type}\".");

                backing_field.SetValue(obj, value);
            }
        }


        public static Mock<T> FreezeMock<T>(this IFixture fixture) where T : class
        {
            var mock = fixture.Freeze<T>();

            return Mock.Get(mock);
        }
    }
}