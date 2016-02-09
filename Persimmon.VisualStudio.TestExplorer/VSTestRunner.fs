namespace Persimmon.VisualStudio.TestExplorer

open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
open System
open System.Reflection
open Persimmon.Runner.Wrapper
open System.Security.Policy

type VSTestRunner() =
  inherit MarshalByRefObject()

  let persimmonFamilies = [
    "Persimmon.dll"
    "Persimmon.Runner.dll"
  ]

  let assemblyName source =
    AssemblyName.GetAssemblyName(source)

  let collectTestCase (collector: Persimmon.Runner.Wrapper.IExecutor<Persimmon.Runner.Wrapper.TestCase>) sources =
      sources
      |> Seq.filter (fun (x: string) -> persimmonFamilies |> List.exists (fun y -> x.EndsWith(y)) |> not)
      |> Seq.map assemblyName
      |> Seq.toArray
      |> collector.Execute
      |> Seq.map VSTestCase.ofWrapperTestCase

  let runAllTestCase (runner: Persimmon.Runner.Wrapper.IExecutor<Persimmon.Runner.Wrapper.TestCase * Persimmon.Runner.Wrapper.TestResult>) sources =
      sources
      |> Seq.filter (fun (x: string) -> persimmonFamilies |> List.exists (fun y -> x.EndsWith(y)) |> not)
      |> Seq.map assemblyName
      |> Seq.toArray
      |> runner.Execute
      |> Seq.map (fun (c, r) -> VSTestResult.ofWrapperTestResult (VSTestCase.ofWrapperTestCase c) r)

  member this.Run<'T>(t: Type, f: Persimmon.Runner.Wrapper.IExecutor<'T> -> unit) =
#if DEBUG
    let result = NativeMethods.ShowWaitingMessageBox("Wait on VSTestRunner.Run()...")
#endif
    let currentAppDomain = AppDomain.CurrentDomain
    let assembly = Assembly.GetExecutingAssembly()
    let fullPath = assembly.Location
    let directory = IO.Path.GetDirectoryName(fullPath)
    let setup = AppDomainSetup(LoaderOptimization = System.LoaderOptimization.MultiDomain, PrivateBinPath = directory, ApplicationBase = directory, DisallowBindingRedirects = false)
    let evidence = AppDomain.CurrentDomain.Evidence
    //let setup = currentAppDomain.SetupInformation
    //let evidence = new Evidence(currentAppDomain.Evidence)
    let appDomain = AppDomain.CreateDomain("persimmon visual studio test explorer domain", evidence, setup)
    try
      let executor = appDomain.CreateInstanceAndUnwrap(t.Assembly.FullName, t.FullName) :?> Persimmon.Runner.Wrapper.IExecutor<'T>
      f executor
    finally
      AppDomain.Unload(appDomain)

  member this.DiscoverTests(sources: string seq, sink: ITestCaseDiscoverySink) =
#if DEBUG
    let currentAppDomain = AppDomain.CurrentDomain
    let assembly = Assembly.GetExecutingAssembly()
#endif
    this.Run(typeof<TestCollector>, fun collector ->
      sources
      |> collectTestCase collector
      |> Seq.iter (fun c -> sink.SendTestCase(c))
    )
  member this.RunTests(sources: string seq, handle: IFrameworkHandle) =
    this.Run(typeof<TestRunner>, fun runner ->
      sources
      |> Seq.distinct
      |> runAllTestCase runner
      |> Seq.iter handle.RecordResult
    )
