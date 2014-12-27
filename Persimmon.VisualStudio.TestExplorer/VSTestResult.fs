namespace Persimmon.VisualStudio.TestExplorer

open System
open Microsoft.VisualStudio.TestPlatform.ObjectModel

module VSTestResult =

  let private convertTestOutcome (outcome: Persimmon.Runner.Wrapper.TestOutcome) =
    outcome
    |> int
    |> enum<TestOutcome>

  let ofWrapperTestResult case (result: Persimmon.Runner.Wrapper.TestResult) =
    TestResult(
      case,
      DisplayName = result.DisplayName,
      Outcome = convertTestOutcome result.Outcome,
      Duration = result.Duration,
      ErrorMessage = result.ErrorMessage,
      ErrorStackTrace = result.ErrorStackTrace
    )
