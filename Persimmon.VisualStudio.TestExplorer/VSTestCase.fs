namespace Persimmon.VisualStudio.TestExplorer

open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
open Persimmon
open Persimmon.ActivePatterns
open System

module VSTestCase =

  let private tryCreateDiaSession sourceAssembly =
    try
      Some (new DiaSession(sourceAssembly))
    with _ ->
      None

  let private tryGetNavigationData className methodName sourceAssembly =
    option {
      use! diaSession = tryCreateDiaSession sourceAssembly
      let navigationData = diaSession.GetNavigationData(className, methodName);
      return!
        if navigationData <> null && navigationData.FileName <> null then Some navigationData
        else
          None
    }

  let private ofPersimmonTestCase sourceAssembly (case: TestCase<_>) =
    TestCase(case.FullName, Uri(Constant.extensionUri),
      sourceAssembly, DisplayName = case.FullName, CodeFilePath = null, LineNumber = 0)

  let rec collectWithPersimmonTestObject sourceAssembly = function
  | Context ctx ->
    ctx.Children
    |> List.collect (collectWithPersimmonTestObject sourceAssembly)
    |> List.map (fun (pc, c: TestCase) ->
      c.FullyQualifiedName <- sprintf "%s.%s" ctx.Name c.FullyQualifiedName
      if c.CodeFilePath = null then
        tryGetNavigationData ctx.Name c.FullyQualifiedName sourceAssembly
        |> Option.iter (fun d ->
          c.CodeFilePath <- d.FileName
          c.LineNumber <- d.MinLineNumber)
      (pc, c))
  | TestCase c -> [ (c, ofPersimmonTestCase sourceAssembly c) ]
