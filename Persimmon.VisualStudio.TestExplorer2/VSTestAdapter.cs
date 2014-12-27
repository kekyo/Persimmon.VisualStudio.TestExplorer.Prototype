using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Persimmon.VisualStudio.TestExplorer2
{

    [FileExtension(".dll")]
    [ExtensionUri("executor://persimmon.visualstudio.testexplorer")]
    [DefaultExecutorUri("executor://persimmon.visualstudio.testexplorer")]
    public class VSTestAdapter : ITestDiscoverer, ITestExecutor
    {
        private VSTestRunner runner = new VSTestRunner();

        public void Cancel()
        {
        }

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            try
            {
                logger.SendMessage(TestMessageLevel.Informational, sources.ToString());
                runner.DiscoverTests(sources.ToList(), discoverySink);
            }
            catch (Exception e)
            {
                logger.SendMessage(TestMessageLevel.Error, e.ToString());
            }
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            runner.RunTests(sources.Distinct().ToList(), frameworkHandle);
        }

        public void RunTests(IEnumerable<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            this.RunTests(tests.Select(c => c.Source), runContext, frameworkHandle);
        }
    }
}
