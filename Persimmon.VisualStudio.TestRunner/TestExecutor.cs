using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        /// <param name="action">Action</param>
        private void Execute(
            string targetAssemblyPath,
            Action<RemotableTestExecutor> action)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(action != null);

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
                Debug.WriteLine(string.Format(
                    "Persimmon test runner: Try to set configuration file: Path={0}", configurationFilePath));

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

                action(remoteExecutor);

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
        public void Discover(
            string targetAssemblyPath,
            ITestDiscoverSink sink)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(sink != null);

            this.Execute(
                targetAssemblyPath,
                executor => executor.Discover(
                    targetAssemblyPath,
                    new DiscoverSinkTrampoline(targetAssemblyPath, sink)));
        }

        /// <summary>
        /// Test execute target assembly.
        /// </summary>
        /// <param name="targetAssemblyPath">Target assembly path</param>
        /// <param name="fullyQualifiedTestNames">Target test names. Run all tests if empty.</param>
        /// <param name="sink">Execution logger interface</param>
        public void Run(
            string targetAssemblyPath,
            IEnumerable<string> fullyQualifiedTestNames,
            ITestRunSink sink)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(fullyQualifiedTestNames != null);
            Debug.Assert(sink != null);

            this.Execute(
                targetAssemblyPath,
                executor => executor.Run(
                    targetAssemblyPath,
                    fullyQualifiedTestNames.ToArray(),
                    new RunSinkTrampoline(targetAssemblyPath, sink)));
        }
    }
}
