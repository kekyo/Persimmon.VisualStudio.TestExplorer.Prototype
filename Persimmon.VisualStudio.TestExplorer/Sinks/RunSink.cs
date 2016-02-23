using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using Persimmon.VisualStudio.TestRunner;

namespace Persimmon.VisualStudio.TestExplorer.Sinks
{
    internal sealed class RunSink : ITestExecutorSink
    {
        private readonly IRunContext runContext_;
        private readonly IFrameworkHandle frameworkHandle_;

        public RunSink(
            IRunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
            runContext_ = runContext;
            frameworkHandle_ = frameworkHandle;
        }

        public void Begin(string message)
        {
            frameworkHandle_.SendMessage(
                TestMessageLevel.Informational,
                string.Format("Begin tests: Path={0}", message));
        }

        public void Ident(ExecutorTestCase testCase)
        {
            // TODO: enable DiaSession

            frameworkHandle_.RecordResult(new TestResult(new TestCase(
                testCase.FullyQualifiedTestName,
                Constant.ExtensionUri,
                testCase.AssemblyPath)));
        }

        public void Finished(string message)
        {
            frameworkHandle_.SendMessage(
                TestMessageLevel.Informational,
                string.Format("Finished tests: Path={0}", message));
        }
    }
#if false
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

let ofWrapperTestCase (case: TestResult) =
  let c = TestResult(case.FullyQualifiedName, Uri(Constant.extensionUri), case.Source, DisplayName = case.DisplayName)
  tryGetNavigationData case.ClassName c.FullyQualifiedName c.Source
  |> Option.iter (fun d ->
    c.CodeFilePath <- d.FileName
    c.LineNumber <- d.MinLineNumber)
  c
#endif
}
