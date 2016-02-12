using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Persimmon.VisualStudio.TestRunner;

namespace ConsoleApplication1
{
    public sealed class Sink : IExecutorSink
    {

        public void Begin(string message)
        {
            Console.WriteLine(message);
        }

        public void Finished(string message)
        {
            Console.WriteLine(message);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var sink = new Sink();

            var executor = new Executor();
            executor.Execute(@"D:\PROJECT\Persimmon.VisualStudio.TestExplorer.Prototype\Persimmon.Runner.Wrapper\bin\Debug\Persimmon.Runner.Wrapper.dll", sink);
        }
    }
}
