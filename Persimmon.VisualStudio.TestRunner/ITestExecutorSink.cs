using System;

namespace Persimmon.VisualStudio.TestRunner
{
    public interface ITestExecutorSink
    {
        void Begin(string message);

        void Ident(ExecutorTestCase testCase);

        void Finished(string message);
    }
}
