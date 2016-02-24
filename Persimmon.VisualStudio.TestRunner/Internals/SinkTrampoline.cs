using System;
using System.Diagnostics;

namespace Persimmon.VisualStudio.TestRunner.Internals
{
    public sealed class SinkTrampoline : MarshalByRefObject
    {
        private readonly ITestExecutorSink parentSink_;

        internal SinkTrampoline(ITestExecutorSink parentSink)
        {
            Debug.Assert(parentSink != null);
            parentSink_ = parentSink;
        }

        public void Begin(string message)
        {
            parentSink_.Begin(message);
        }

        public void Ident(string targetAssemblyPath, object[] args)
        {
            parentSink_.Ident(new ExecutorTestCase(
                args[0].ToString(),
                args[1].ToString(),
                targetAssemblyPath));
        }

        public void Finished(string message)
        {
            parentSink_.Finished(message);
        }
    }
}
