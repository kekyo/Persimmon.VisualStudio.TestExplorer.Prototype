using System;
using System.Diagnostics;

namespace Persimmon.VisualStudio.TestRunner.Internals
{
    internal sealed class SinkTrampoline : MarshalByRefObject, ITestExecutorSink
    {
        private readonly ITestExecutorSink parentSink_;

        public SinkTrampoline(ITestExecutorSink parentSink)
        {
            Debug.Assert(parentSink != null);
            parentSink_ = parentSink;
        }

        public void Begin(string message)
        {
            parentSink_.Begin(message);
        }

        public void Ident(ExecutorTestCase testCase)
        {
            parentSink_.Ident(testCase);
        }

        public void Finished(string message)
        {
            parentSink_.Finished(message);
        }
    }
}
