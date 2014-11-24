namespace Persimmon.VisualStudio.TestExplorer

open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
open Persimmon
open Persimmon.Runner
open Persimmon.Output
open System.Reflection
open System.IO

[<FileExtension(".dll")>]
[<ExtensionUri(Constant.extensionUri)>]
[<DefaultExecutorUri(Constant.extensionUri)>]
type VSTestRunner() =
  let persimmonFamilies = [
    "Persimmon.dll"
    "Persimmon.Runner.dll"
  ]
  let collectTestObject sourceAssembly =
    let asm = Assembly.LoadFile(sourceAssembly)
    (sourceAssembly, TestCollector.collectRootTestObjects [asm])
  let collectTestCase source =
      List.ofSeq source
      |> List.filter (fun (x: string) -> persimmonFamilies |> List.exists (fun y -> x.EndsWith(y)) |> not)
      |> List.map collectTestObject
      |> List.collect (fun (s, l) -> List.collect (VSTestCase.collectWithPersimmonTestObject s) l)
  interface ITestDiscoverer with
    member this.DiscoverTests(sources: string seq, context: IDiscoveryContext,logger: IMessageLogger, sink: ITestCaseDiscoverySink) =
      logger.SendMessage(TestMessageLevel.Informational, sprintf "%A" sources)
      sources
      |> collectTestCase
      |> List.map snd
      |> List.iter (fun c -> sink.SendTestCase(c))
  interface ITestExecutor with
    member this.RunTests(tests: TestCase seq, runContext: IRunContext, handle: IFrameworkHandle) =
      (this :> ITestExecutor).RunTests(tests |> Seq.map (fun c -> c.Source), runContext, handle)
    member this.RunTests(tests: string seq, runContext: IRunContext, handle: IFrameworkHandle) =
      // fake reporter
      use reporter =
        new Reporter(
          new Printer<_>(new StringWriter(), Formatter.ProgressFormatter.dot),
          new Printer<_>(new StringWriter(), Formatter.SummaryFormatter.normal),
          new Printer<_>(new StringWriter(), Formatter.ErrorFormatter.normal))
      tests
      |> collectTestCase
      |> fun l ->
        l
        |> List.map (fst >> (fun x -> x :> TestObject))
        |> TestRunner.runAllTests reporter
        |> fun r -> r.ExecutedRootTestResults
        |> Seq.zip (List.map snd l)
        |> Seq.map (fun (c, r) -> VSTestResult.ofPersimmonTestResult c r)
        |> Seq.iter handle.RecordResult
    member this.Cancel() =
      ()
