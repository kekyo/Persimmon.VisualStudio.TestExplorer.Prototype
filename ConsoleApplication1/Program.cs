using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Persimmon.VisualStudio.TestRunner;

namespace ConsoleApplication1
{
    public sealed class Sink : ITestExecutorSink
    {

        public void Begin(string message)
        {
            Console.WriteLine(message);
        }

        public void Finished(string message)
        {
            Console.WriteLine(message);
        }


        public void Ident(ExecutorTestCase testCase)
        {
            Console.WriteLine(testCase);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var sink = new Sink();

            var executor = new TestExecutor();
            executor.Execute(
                @"D:\PROJECT\Persimmon\examples\Persimmon.Sample\bin\Debug\Persimmon.Sample.dll",
                sink,
                ExecutionModes.Discover);
        }
    }
}
