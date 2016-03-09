﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Persimmon.VisualStudio.TestRunner.Internals
{
    /// <summary>
    /// Test assembly load/execute via remote AppDomain implementation.
    /// </summary>
    public sealed class RemotableTestExecutor : MarshalByRefObject
    {
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
        /// <param name="persimmonPartialAssemblyName">Target type name</param>
        /// <param name="persimmonTypeName">Target type name</param>
        /// <param name="sinkTrampoline">Execution logger interface</param>
        /// <param name="rawAction">Action delegate(TestCollector, TestAssembly)</param>
        private void Execute(
            string targetAssemblyPath,
            string persimmonPartialAssemblyName,
            string persimmonTypeName,
            ISinkTrampoline sinkTrampoline,
            Action<dynamic, Assembly> rawAction)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(!string.IsNullOrWhiteSpace(persimmonPartialAssemblyName));
            Debug.Assert(!string.IsNullOrWhiteSpace(persimmonTypeName));
            Debug.Assert(sinkTrampoline != null);
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

            sinkTrampoline.Begin(targetAssemblyPath);

            // 3. extract Persimmon assembly name via test assembly,
            var persimmonFullAssemblyName = testAssembly.GetReferencedAssemblies().
                FirstOrDefault(assembly => assembly.Name == persimmonPartialAssemblyName);
            if (persimmonFullAssemblyName != null)
            {
                //   and load persimmon assembly.
                var persimmonAssembly = Assembly.Load(persimmonFullAssemblyName);

                // 4. Instantiate TestCollector/TestRunner class (by dynamic), and do action.
                //   --> Because TestCollector/TestRunner class containing assembly version is unknown,
                //       so this TestRunner assembly can't statically refering The Persimmon assembly...
                var persimmonType = persimmonAssembly.GetType(persimmonTypeName);
                dynamic persimmonInstance = Activator.CreateInstance(persimmonType);

                rawAction(persimmonInstance, testAssembly);
            }

            sinkTrampoline.Finished(targetAssemblyPath);
        }

        /// <summary>
        /// Discover tests target assembly.
        /// </summary>
        /// <param name="targetAssemblyPath">Target assembly path</param>
        /// <param name="sinkTrampoline">Execution logger interface</param>
        public void Discover(
            string targetAssemblyPath,
            ISinkTrampoline sinkTrampoline)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(sinkTrampoline != null);

            Debug.WriteLine(string.Format(
                "{0}: Discover: TargetAssembly={1}",
                this.GetType().FullName,
                targetAssemblyPath));

            DiaSession diaSession = null;
            try
            {
                diaSession = new DiaSession(targetAssemblyPath);
            }
            catch (Exception ex)
            {
                // Ignore cannot load.
                Debug.WriteLine(ex.ToString());
            }

            try
            {
                // Callback delegate: testCase is ITestCase.
                var callback = new Action<dynamic>(testCase =>
                {
                    MemberInfo member = testCase.DeclaredMember.Value;
                    var method = member as MethodInfo;
                    var type = (method != null) ? method.DeclaringType : null;

                    // If enable DiaSession, lookup debug information.
                    DiaNavigationData navigationData = null;
                    if (diaSession != null)
                    {
                        if ((method != null) && (type != null))
                        {
                            navigationData = diaSession.GetNavigationData(
                                type.FullName,
                                method.Name);
                        }
                    }

                    // Re-construct results by safe serializable type. (object array)
                    sinkTrampoline.Progress(new[]
                    {
                        testCase.FullName,
                        testCase.FullName,  // TODO: Context-structual path
                        (type != null) ? type.FullName : member.Name,
                        (method != null) ? method.Name : member.Name,
                        (navigationData != null) ? navigationData.FileName : null,
                        (navigationData != null) ? navigationData.MinLineNumber : -1
                    });
                });

                this.Execute(
                    targetAssemblyPath,
                    "Persimmon",
                    "Persimmon.Internals.TestCollector",
                    sinkTrampoline,
                    (testCollector, testAssembly) => testCollector.CollectAndCallback(
                        testAssembly,
                        callback));
            }
            finally
            {
                if (diaSession != null) diaSession.Dispose();
            }
        }

        /// <summary>
        /// Run tests target assembly.
        /// </summary>
        /// <param name="targetAssemblyPath">Target assembly path</param>
        /// <param name="fullyQualifiedTestNames">Target test names. Run all tests if empty.</param>
        /// <param name="sinkTrampoline">Execution logger interface</param>
        public void Run(
            string targetAssemblyPath,
            string[] fullyQualifiedTestNames,
            ISinkTrampoline sinkTrampoline)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(fullyQualifiedTestNames != null);
            Debug.Assert(sinkTrampoline != null);

            Debug.WriteLine(string.Format(
                "{0}: Run: TargetAssembly={1}",
                this.GetType().FullName,
                targetAssemblyPath));

            // Callback delegate: testResult is ITestResult.
            var callback = new Action<dynamic>(testResult =>
            {
                // Re-construct results by safe serializable type. (object array)
                sinkTrampoline.Progress(new[]
                {
                    testResult.FullName,
                    testResult.DeclaredType.FullName,
                    testResult.Outcome
                });
            });

            this.Execute(
                targetAssemblyPath,
                "Persimmon",
                "Persimmon.Internals.TestRunner",
                sinkTrampoline,
                (TestRunner, testAssembly) => TestRunner.RunTestsAndCallback(
                    testAssembly,
                    fullyQualifiedTestNames,
                    callback));
        }
    }
}
