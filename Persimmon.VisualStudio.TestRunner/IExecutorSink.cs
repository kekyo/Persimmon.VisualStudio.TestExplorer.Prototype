namespace Persimmon.VisualStudio.TestRunner
{
    public interface IExecutorSink
    {
        void Begin(string message);
        void Finished(string message);
    }
}
