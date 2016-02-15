using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using Persimmon.VisualStudio.TestRunner;

namespace Persimmon.VisualStudio.TestExplorer
{
    internal sealed class DiscoverySink : ITestExecutorSink
    {
        private readonly IDiscoveryContext discoveryContext_;
        private readonly IMessageLogger logger_;
        private readonly ITestCaseDiscoverySink discoverySink_;

        public DiscoverySink(
            IDiscoveryContext discoveryContext,
            IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            discoveryContext_ = discoveryContext;
            logger_ = logger;
            discoverySink_ = discoverySink;
        }

        public void Begin(string message)
        {
            logger_.SendMessage(TestMessageLevel.Informational, message);
        }

        public void Ident(ExecutorTestCase testCase)
        {
            discoverySink_.SendTestCase(new TestCase(
                testCase.FullyQualifiedName,
                Constant.ExtensionUri,
                testCase.Source));
        }

        public void Finished(string message)
        {
            logger_.SendMessage(TestMessageLevel.Informational, message);
        }
    }
}
