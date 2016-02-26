namespace Persimmon.VisualStudio.TestRunner.Internals
{
    public interface ISinkTrampoline
    {
        void Begin(string message);

        void Progress(object[] args);

        void Finished(string message);
    }
}
