using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Policy;

using Persimmon.VisualStudio.TestRunner.Internals;

namespace Persimmon.VisualStudio.TestRunner
{
    /// <summary>
    /// Test assembly load/execute facade.
    /// </summary>
    public sealed class TestExecutor
    {
        private static readonly Type remotableExecutorType_ = typeof(RemotableTestExecutor);
        private static readonly string testRunnerAssemblyPath_ = remotableExecutorType_.Assembly.Location;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TestExecutor()
        {
            Debug.Assert(this.GetType().Assembly.GlobalAssemblyCache);
        }

        /// <summary>
        /// Test execute target assembly.
        /// </summary>
        /// <param name="targetAssemblyPath">Target assembly path</param>
        /// <param name="sink">SinkTrampoline</param>
        /// <param name="mode">Execution mode</param>
        private void InternalExecute(
            string targetAssemblyPath,
            SinkTrampoline sink,
            ExecutionModes mode)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(sink != null);

            // Strategy: Shadow copy information:
            //   https://msdn.microsoft.com/en-us/library/ms404279%28v=vs.110%29.aspx

            // Execution context id (for diagnose).
            var contextId = Guid.NewGuid();

            // ApplicationBase path.
            // Important: Change from current AppDomain.ApplicationBase,
            //   may be stable execution test assemblies.
            var applicationBasePath = Path.GetDirectoryName(targetAssemblyPath);

            // Shadow copy target paths.
            var shadowCopyTargets = string.Join(
                ";",
                new []
                {
                    applicationBasePath,
                    Path.GetDirectoryName(testRunnerAssemblyPath_)
                });

            // AppDomain name.
            var separatedAppDomainName = string.Format(
                "{0}-{1}",
                this.GetType().FullName,
                contextId);

            // AppDomainSetup informations.
            var separatedAppDomainSetup = new AppDomainSetup
            {
                ApplicationName = separatedAppDomainName,
                ApplicationBase = applicationBasePath,
                ShadowCopyFiles = "true",
                ShadowCopyDirectories = shadowCopyTargets
            };

            // If test assembly has configuration file, try to set.
            var configurationFilePath = targetAssemblyPath + ".config";
            if (File.Exists(configurationFilePath))
            {
                Debug.WriteLine(string.Format("Persimmon test runner: Try to set configuration file: Path={0}", configurationFilePath));

                separatedAppDomainSetup.ConfigurationFile = configurationFilePath;
            }

            // Derived current evidence.
            // (vstest agent may be full trust...)
            var separatedAppDomainEvidence = new Evidence(AppDomain.CurrentDomain.Evidence);

            // Create AppDomain.
            var separatedAppDomain = AppDomain.CreateDomain(
                separatedAppDomainName,
                separatedAppDomainEvidence,
                separatedAppDomainSetup);

            try
            {
                // Create RemotableTestExecutor instance into new AppDomain,
                //   and get remote reference.
                var remoteExecutor = (RemotableTestExecutor)separatedAppDomain.CreateInstanceFromAndUnwrap(
                    testRunnerAssemblyPath_,
                    remotableExecutorType_.FullName);
                
                ///////////////////////////////////////////////////////////////////////////////////////////
                // Execute via remote AppDomain
                var sinkTrampoline = new SinkTrampoline(sink);

                if (mode == ExecutionModes.Discover)
                {
                    remoteExecutor.Discover(targetAssemblyPath, sinkTrampoline);
                }
                else
                {
                    remoteExecutor.Run(targetAssemblyPath, sinkTrampoline);
                }
                ///////////////////////////////////////////////////////////////////////////////////////////
            }
            finally
            {
                // Discard AppDomain.
                AppDomain.Unload(separatedAppDomain);
            }
        }

        /// <summary>
        /// Test execute target assembly.
        /// </summary>
        /// <param name="targetAssemblyPath">Target assembly path</param>
        /// <param name="sink">Execution logger interface</param>
        /// <param name="mode">Execution mode</param>
        public void Execute(
            string targetAssemblyPath,
            ITestExecutorSink sink,
            ExecutionModes mode)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(sink != null);

            this.InternalExecute(targetAssemblyPath, new SinkTrampoline(sink), mode);
        }

        /// <summary>
        /// Test execute target assemblies.
        /// </summary>
        /// <param name="targetAssemblyPaths">Target assembly paths</param>
        /// <param name="sink">Execution logger interface</param>
        /// <param name="mode">Execution mode</param>
        public void Execute(
            IEnumerable<string> targetAssemblyPaths,
            ITestExecutorSink sink,
            ExecutionModes mode)
        {
            Debug.Assert(targetAssemblyPaths != null);
            Debug.Assert(sink != null);

            var sinkTrampoline = new SinkTrampoline(sink);
            foreach (var path in targetAssemblyPaths)
            {
                Debug.Assert(!string.IsNullOrWhiteSpace(path));

                this.InternalExecute(path, sinkTrampoline, mode);
            }
        }
    }
}
