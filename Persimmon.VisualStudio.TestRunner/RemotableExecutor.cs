using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Persimmon.VisualStudio.TestRunner
{
    /// <summary>
    /// Test assembly load/execute implementation.
    /// </summary>
    public sealed class RemotableExecutor : MarshalByRefObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public RemotableExecutor()
        {
            Debug.WriteLine(string.Format(
                "{0}: constructed: Process={1}, Thread=[{2},{3}], AppDomain=[{4},{5},{6}]",
                this.GetType().FullName,
                Process.GetCurrentProcess().Id,
                Thread.CurrentThread.ManagedThreadId,
                Thread.CurrentThread.Name,
                AppDomain.CurrentDomain.Id,
                AppDomain.CurrentDomain.FriendlyName,
                AppDomain.CurrentDomain.BaseDirectory));
        }

        /// <summary>
        /// Test execute target assembly.
        /// </summary>
        /// <param name="targetAssemblyPath">Target assembly path</param>
        /// <param name="executorSink">Execution logger interface</param>
        public void Execute(
            string targetAssemblyPath,
            IExecutorSink executorSink)
        {
            Debug.WriteLine(string.Format(
                "{0}: execute: TargetAssembly={1}",
                this.GetType().FullName,
                targetAssemblyPath));

            // TODO: Shadowed copy is equal?
            Debug.Assert(
                Path.GetDirectoryName(targetAssemblyPath) == AppDomain.CurrentDomain.BaseDirectory);

            // First, pre-load target assembly and analyze fully-qualified assembly name.
            var preLoadAssembly = Assembly.ReflectionOnlyLoadFrom(targetAssemblyPath);
            var assemblyFullName = preLoadAssembly.FullName;

            // Second, load assembly by fully-qualified assembly name.
            //   --> Assembly.Load() is load assembly into default context.
            //   --> Failed if current AppDomain.ApplicationBase folder is not target assembly path.
            var assembly = Assembly.Load(assemblyFullName);

            executorSink.Begin(targetAssemblyPath);

            // TODO: execute tests.

            executorSink.Finished(targetAssemblyPath);
        }
    }
}
