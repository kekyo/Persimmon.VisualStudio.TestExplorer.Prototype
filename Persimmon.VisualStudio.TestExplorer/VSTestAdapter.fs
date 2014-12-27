﻿namespace Persimmon.VisualStudio.TestExplorer

open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
open System
open System.Linq

[<FileExtension(".dll")>]
[<ExtensionUri(Constant.extensionUri)>]
[<DefaultExecutorUri(Constant.extensionUri)>]
type VSTestAdapter() =

  let runner = VSTestRunner()

  interface ITestDiscoverer with
    member this.DiscoverTests(sources: string seq, context: IDiscoveryContext,logger: IMessageLogger, sink: ITestCaseDiscoverySink) =
      try
        logger.SendMessage(TestMessageLevel.Informational, sprintf "%A" sources)
        runner.DiscoverTests(sources.ToList(), sink)
      with e ->
        logger.SendMessage(TestMessageLevel.Error, e.ToString())
  interface ITestExecutor with
    member this.RunTests(tests: TestCase seq, runContext: IRunContext, handle: IFrameworkHandle) =
      (this :> ITestExecutor).RunTests(tests |> Seq.map (fun c -> c.Source), runContext, handle)
    member this.RunTests(sources: string seq, _: IRunContext, handle: IFrameworkHandle) =
      runner.RunTests(sources.Distinct().ToList(), handle)
    member __.Cancel() =
      ()
