namespace Persimmon.VisualStudio.TestExplorer

open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
open System
open System.Linq
open System.Reflection

type VSTestRunner() =
  inherit MarshalByRefObject()

  let persimmonFamilies = [
    "Persimmon.dll"
    "Persimmon.Runner.dll"
  ]

  let collectTestCase (collector: Persimmon.Runner.Wrapper.IExecutor<Persimmon.Runner.Wrapper.TestCase>)
    (sources: ResizeArray<string>) =
      sources.Where(fun (x: string) -> persimmonFamilies |> List.exists (fun y -> x.EndsWith(y)) |> not)
        .ToList()
      |> collector.Execute
      |> Seq.map VSTestCase.ofWrapperTestCase

  let runAllTestCase (runner: Persimmon.Runner.Wrapper.IExecutor<Persimmon.Runner.Wrapper.TestCase * Persimmon.Runner.Wrapper.TestResult>)
    (sources: ResizeArray<string>) =
      sources.Where(fun (x: string) -> persimmonFamilies |> List.exists (fun y -> x.EndsWith(y)) |> not)
        .ToList()
      |> runner.Execute
      |> Seq.map (fun (c, r) -> VSTestResult.ofWrapperTestResult (VSTestCase.ofWrapperTestCase c) r)

  member __.Run<'T>(typeName: string, f: Persimmon.Runner.Wrapper.IExecutor<'T> -> unit) =
    let fullPath = Assembly.GetExecutingAssembly().Location
    let directory = IO.Path.GetDirectoryName(fullPath)
    let setup = AppDomainSetup(LoaderOptimization = System.LoaderOptimization.MultiDomain, PrivateBinPath = directory, ApplicationBase = directory, DisallowBindingRedirects = true)
    let evidence = AppDomain.CurrentDomain.Evidence
    let appDomain = AppDomain.CreateDomain("persimmon visual studio test explorer domain", evidence, setup)
    try
      let t = appDomain.CreateInstanceAndUnwrap("Persimmon.Runner.Wrapper", typeName) :?> Persimmon.Runner.Wrapper.IExecutor<'T>
      f t
    finally
      AppDomain.Unload(appDomain)

  member this.DiscoverTests(sources: ResizeArray<string>, sink: ITestCaseDiscoverySink) =
    this.Run("Persimmon.Runner.Wrapper.TestCollector", fun collector ->
      sources
      |> collectTestCase collector
      |> Seq.iter (fun c -> sink.SendTestCase(c))
    )
  member this.RunTests(sources: ResizeArray<string>, handle: IFrameworkHandle) =
    this.Run("Persimmon.Runner.Wrapper.TestRunner", fun runner ->
      sources
      |> runAllTestCase runner
      |> Seq.iter handle.RecordResult
    )
