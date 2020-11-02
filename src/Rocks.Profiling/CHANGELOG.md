## 4.16.0 - 2020-10-27
### Changed
- Background profiling queue processing implementation changed from `BlockingCollection` to `ConcurrentQueue`

## 4.15.0 - 2020-08-20
### Changed
- Packages update (SimpleInjector 5)

## 4.14.0 - 2018-24-06
### Added
- Added IProfilerEventsHandler.OnSessionStarted and IProfilerEventsHandler.OnOperationStarted methods

## 4.13.0 - 2018-02-07
### Fixed
- Possible fix for IndexOutOfRangeException for ProfileSession.StartMeasure
### Added
- Added ProfileSession.HasOperations property

## 4.12.2 - 2018-12-06
### Fixed
- Fixed issue with duplicate library setup overwriting previous registrations.

## 4.12.1 - 2018-12-06
### Fixed
- Fixed issue with multiple async operations awaited later could get wrong parent

## 4.12.0 - 2018-12-05
### Fixed
- Fixed issue with multiple async operations awaited in wrong order generated OperationsOutOfOrderProfillingException. 
### Changed
- Packages update

## 4.11.0 - 2018-09-20
### Fixed
- Fixed issue when DB providers not always been intercepted and wrapped into profiled instance
### Changed
- Packages update

## 4.10.2 - 2018-09-18
### Fixed
- Changed IProfiler to be always singleton

## 4.10.1 - 2018-07-16
### Fixed
- Implemented SetupLock to fix issue in integration tests parallel initialization

## 4.10.0 - 2018-06-01
### Changed
- Packages update

## 4.9.0 - 2018-05-31
### Changed
- Packages update

## 4.8.0 - 2018-05-10
### Removed
- Removed .NET 4.6.1

## 4.7.0 - 2018-04-27
### Changed
- Update packages