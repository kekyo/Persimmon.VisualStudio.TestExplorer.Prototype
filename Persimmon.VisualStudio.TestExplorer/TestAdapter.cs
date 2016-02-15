using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using Persimmon.VisualStudio.TestRunner;

namespace Persimmon.VisualStudio.TestExplorer
{
    [FileExtension(".dll")]
    [ExtensionUri(Constant.ExtensionUriString)]
    [DefaultExecutorUri(Constant.ExtensionUriString)]
    public sealed class TestAdapter : ITestDiscoverer, ITestExecutor
    {
        private readonly Version version_ = typeof(TestAdapter).Assembly.GetName().Version;

        public void DiscoverTests(
            IEnumerable<string> sources,
            IDiscoveryContext discoveryContext,
            IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            logger.SendMessage(
                TestMessageLevel.Informational,
                string.Format("Persimmon Test Explorer {0} discovering tests is started", version_));
            try
            {
                var testExecutor = new TestExecutor();

                testExecutor.Execute(
                    sources,
                    new DiscoverySink(discoveryContext, logger, discoverySink),
                    ExecutionModes.Discover);
            }
            catch (Exception ex)
            {
                logger.SendMessage(
                    TestMessageLevel.Error,
                    ex.ToString());
            }
            finally
            {
                logger.SendMessage(
                    TestMessageLevel.Informational,
                    string.Format("Persimmon Test Explorer {0} discovering tests is finished", version_));
            }
        }

        public void RunTests(
            IEnumerable<string> sources,
            IRunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
            frameworkHandle.SendMessage(
                TestMessageLevel.Informational,
                string.Format("Persimmon Test Explorer {0} run tests is started", version_));
            try
            {
                var testExecutor = new TestExecutor();

                testExecutor.Execute(
                    sources,
                    new RunSink(runContext, frameworkHandle),
                    ExecutionModes.Run);
            }
            catch (Exception ex)
            {
                frameworkHandle.SendMessage(
                    TestMessageLevel.Error,
                    ex.ToString());
            }
            finally
            {
                frameworkHandle.SendMessage(
                    TestMessageLevel.Informational,
                    string.Format("Persimmon Test Explorer {0} run tests is finished", version_));
            }
        }

        public void RunTests(
            IEnumerable<TestCase> tests,
            IRunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
            // TODO: enable per-testcase execution
            this.RunTests(tests.Select(test => test.CodeFilePath), runContext, frameworkHandle);
        }

        public void Cancel()
        {
            // TODO: enable cancellation
        }

#if false
        interface ITestDiscoverer with
        member this.DiscoverTests(sources: string seq, context: IDiscoveryContext,logger: IMessageLogger, sink: ITestCaseDiscoverySink) =
            try
            logger.SendMessage(TestMessageLevel.Informational, sprintf "Persimmon Test Explorer %s discovering tests is started" version)
            try
                runner.DiscoverTests(sources, sink)
            with e ->
                logger.SendMessage(TestMessageLevel.Error, e.ToString())
            finally
            logger.SendMessage(TestMessageLevel.Informational, sprintf "Persimmon Test Explorer %s discovering tests is finished" version)
        interface ITestExecutor with
        member this.RunTests(tests: TestCase seq, runContext: IRunContext, handle: IFrameworkHandle) =
            (this :> ITestExecutor).RunTests(tests |> Seq.map (fun c -> c.Source), runContext, handle)
        member this.RunTests(sources: string seq, _: IRunContext, handle: IFrameworkHandle) =
            runner.RunTests(sources, handle)
        member __.Cancel() =
            ()
#endif
    }
}
