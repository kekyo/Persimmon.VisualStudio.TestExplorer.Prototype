﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Persimmon.VisualStudio.TestRunner.Internals
{
    /// <summary>
    /// Test assembly load/execute via remote AppDomain implementation.
    /// </summary>
    public sealed class RemotableTestExecutor : MarshalByRefObject
    {
        static RemotableTestExecutor()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs e)
        {
            Debug.WriteLine(string.Format(
                "AssemblyResolve: Name={0}, Requesting={1}, Current={2}",
                e.Name,
                e.RequestingAssembly,
                AppDomain.CurrentDomain));

            return null;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public RemotableTestExecutor()
        {
            Debug.Assert(this.GetType().Assembly.GlobalAssemblyCache);

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
        /// Load target assembly and do action.
        /// </summary>
        /// <param name="targetAssemblyPath">Target assembly path</param>
        /// <param name="executorSink">Execution logger interface</param>
        /// <param name="rawAction">Action delegate(TestCollector, TestAssembly)</param>
        private void Execute(
            string targetAssemblyPath,
            ITestExecutorSink executorSink,
            Action<dynamic, Assembly> rawAction)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(executorSink != null);
            Debug.Assert(rawAction != null);

            Debug.Assert(
                Path.GetDirectoryName(targetAssemblyPath) == AppDomain.CurrentDomain.BaseDirectory);

            // 1. pre-load target assembly and analyze fully-qualified assembly name.
            //   --> Assebly.ReflectionOnlyLoadFrom() is load assembly into reflection-only context.
            var preLoadAssembly = Assembly.ReflectionOnlyLoadFrom(targetAssemblyPath);
            var assemblyFullName = preLoadAssembly.FullName;

            // 2. load assembly by fully-qualified assembly name.
            //   --> Assembly.Load() is load assembly into "default context."
            //   --> Failed if current AppDomain.ApplicationBase folder is not target assembly path.
            var testAssembly = Assembly.Load(assemblyFullName);

            executorSink.Begin(targetAssemblyPath);

            // 3. extract Persimmon assembly name via test assembly,
            var persimmonAssemblyName = testAssembly.GetReferencedAssemblies().
                FirstOrDefault(assembly => assembly.Name == "Persimmon");
            if (persimmonAssemblyName != null)
            {
                //   and load persimmon assembly.
                var persimmonAssembly = Assembly.Load(persimmonAssemblyName);

                // 4. Instantiate TestCollector (by dynamic), and do action.
                //   --> Because TestCollector containing assembly version is unknown,
                //       so this TestRunner assembly can't statically refering The Persimmon assembly...
                var testCollectorType = persimmonAssembly.GetType(
                    "Persimmon.Internals.TestCollector");
                dynamic testCollector = Activator.CreateInstance(testCollectorType);

                rawAction(testCollector, testAssembly);
            }

            executorSink.Finished(targetAssemblyPath);
        }

        /// <summary>
        /// Convert Action&lt;ExecutorTestCase&gt; to Action&lt;dynamic&gt;
        /// </summary>
        /// <param name="action">Target delegate</param>
        /// <returns>Converted delegate</returns>
        private static Action<dynamic> ToDynamicAction(
            Action<ExecutorTestCase> action)
        {
            Debug.Assert(action != null);

            return new Action<dynamic>(testCase =>
            {
                action(new ExecutorTestCase(
                    testCase.FullyQualifiedName,
                    testCase.ClassName,
                    testCase.Source,
                    testCase.DisplayName));
            });
        }

        /// <summary>
        /// Discover tests target assembly.
        /// </summary>
        /// <param name="targetAssemblyPath">Target assembly path</param>
        /// <param name="executorSink">Execution logger interface</param>
        public void Discover(
            string targetAssemblyPath,
            ITestExecutorSink executorSink)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(executorSink is MarshalByRefObject);

            Debug.WriteLine(string.Format(
                "{0}: Discover: TargetAssembly={1}",
                this.GetType().FullName,
                targetAssemblyPath));

            this.Execute(
                targetAssemblyPath,
                executorSink,
                (testCollector, testAssembly) => testCollector.RunAndMarshal(
                    testAssembly,
                    ToDynamicAction(executorSink.Ident)));
        }

        /// <summary>
        /// Run tests target assembly.
        /// </summary>
        /// <param name="targetAssemblyPath">Target assembly path</param>
        /// <param name="executorSink">Execution logger interface</param>
        public void Run(
            string targetAssemblyPath,
            ITestExecutorSink executorSink)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(executorSink is MarshalByRefObject);

            Debug.WriteLine(string.Format(
                "{0}: Run: TargetAssembly={1}",
                this.GetType().FullName,
                targetAssemblyPath));

            this.Execute(
                targetAssemblyPath,
                executorSink,
                (testCollector, testAssembly) => testCollector.RunAndMarshal(
                    testAssembly,
                    ToDynamicAction(executorSink.Ident)));
        }
    }
}