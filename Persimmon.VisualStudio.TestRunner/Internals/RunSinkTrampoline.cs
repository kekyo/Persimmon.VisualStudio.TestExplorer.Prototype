using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Persimmon.VisualStudio.TestRunner.Internals
{
    public sealed class RunSinkTrampoline : MarshalByRefObject, ISinkTrampoline
    {
        private readonly string targetAssemblyPath_;
        private readonly ITestRunSink parentSink_;
        private readonly Dictionary<string, TestCase> testCases_;

        internal RunSinkTrampoline(
            string targetAssemblyPath,
            ITestRunSink parentSink,
            Dictionary<string, TestCase> testCases)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(parentSink != null);
            Debug.Assert(testCases != null);

            targetAssemblyPath_ = targetAssemblyPath;
            parentSink_ = parentSink;
            testCases_ = testCases;
        }

        public void Begin(string message)
        {
            parentSink_.Begin(message);
        }

        public void Progress(dynamic[] args)
        {
            TestCase testCase;
            if (testCases_.TryGetValue(args[0], out testCase) == false)
            {
                testCase = new TestCase(
                    args[0],
                    parentSink_.ExtensionUri,
                    targetAssemblyPath_);
            }

            var testResult = new TestResult(testCase);
            var exceptions = args[2] as Exception[];

            // TODO: Other outcome require handled.
            //   Strategy: testCases_ included target test cases,
            //     so match and filter into Finished(), filtered test cases marking TestOutcome.Notfound.
            testResult.Outcome = (exceptions.Length >= 1) ? TestOutcome.Failed : TestOutcome.Passed;
            testResult.Duration = args[3];

            parentSink_.Progress(testResult);
        }

        public void Finished(string message)
        {
            parentSink_.Finished(message);
        }
    }
}
