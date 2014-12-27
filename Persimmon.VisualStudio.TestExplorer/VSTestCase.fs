module Persimmon.VisualStudio.TestExplorer.VSTestCase

open System
open Microsoft.VisualStudio.TestPlatform.ObjectModel

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

let ofWrapperTestCase (case: Persimmon.Runner.Wrapper.TestCase) =
  let c = TestCase(case.FullyQualifiedName, Uri(Constant.extensionUri), case.Source, DisplayName = case.DisplayName)
  tryGetNavigationData case.ClassName c.FullyQualifiedName c.Source
  |> Option.iter (fun d ->
    c.CodeFilePath <- d.FileName
    c.LineNumber <- d.MinLineNumber)
  c
