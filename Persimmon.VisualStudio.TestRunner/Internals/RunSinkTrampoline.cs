using System;
using System.Diagnostics;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Persimmon.VisualStudio.TestRunner.Internals
{
    public sealed class RunSinkTrampoline : MarshalByRefObject, ISinkTrampoline
    {
        private readonly string targetAssemblyPath_;
        private readonly ITestRunSink parentSink_;

        internal RunSinkTrampoline(string targetAssemblyPath, ITestRunSink parentSink)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(parentSink != null);

            targetAssemblyPath_ = targetAssemblyPath;
            parentSink_ = parentSink;
        }

        public void Begin(string message)
        {
            parentSink_.Begin(message);
        }

        public void Progress(dynamic[] args)
        {
            // TODO: Require matching original TestCase instance.
            var testCase = new TestCase(
                args[0],
                parentSink_.ExtensionUri,
                targetAssemblyPath_);

            var testResult = new TestResult(testCase);
            testResult.Outcome = (TestOutcome) Enum.Parse(typeof (TestOutcome), args[2]);

            parentSink_.Progress(testResult);
        }

        public void Finished(string message)
        {
            parentSink_.Finished(message);
        }
    }
}
