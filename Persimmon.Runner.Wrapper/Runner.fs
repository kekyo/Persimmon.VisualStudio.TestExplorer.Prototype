namespace Persimmon.Runner.Wrapper

open System
open System.Reflection
open System.Collections.Generic
open Persimmon.Runner
open Persimmon.ActivePatterns

module private TestRunnerImpl =

  open Persimmon

  let runTests (test: TestObject) =
    match test with
    | Context ctx -> ctx.Run(ignore) :> ITestResult
    | TestCase tc ->
      let result = tc.Run()
      result :> ITestResult

type TestRunner() =
  inherit MarshalByRefObject()
  member __.RunAllTests(asms: IEnumerable<Assembly>) =
    asms
    |> Seq.collect (fun s ->
      s
      |> TestCollectorImpl.publicTypes
      |> Seq.collect TestCollectorImpl.testObjects
      |> Seq.map (fun x -> (TestCase.ofTestObject s.FullName x, snd x))
    )
    |> Seq.map (fun (c, o) -> (c, o |> TestRunnerImpl.runTests |> TestResult.ofITestResult c))
  interface IExecutor<TestCase * TestResult> with
    member this.Execute(asms) = this.RunAllTests(asms)
