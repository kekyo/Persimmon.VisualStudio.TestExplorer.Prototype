using System;
using System.Diagnostics;
using System.IO;
using System.Security.Policy;

namespace Persimmon.VisualStudio.TestRunner
{
    /// <summary>
    /// Test assembly load/execute facade.
    /// </summary>
    public sealed class Executor
    {
        private static readonly Type remotableExecutorType_ = typeof(RemotableExecutor);
        private static readonly string testRunnerAssemblyPath_ = remotableExecutorType_.Assembly.Location;

        /// <summary>
        /// Test execute target assembly.
        /// </summary>
        /// <param name="targetAssemblyPath">Target assembly path</param>
        /// <param name="sink">Execution logger interface</param>
        public void Execute(
            string targetAssemblyPath,
            IExecutorSink sink)
        {
            // Strategy: Shadow copy information: https://msdn.microsoft.com/en-us/library/ms404279%28v=vs.110%29.aspx

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
                separatedAppDomainSetup.ConfigurationFile = configurationFilePath;
            }

            // Derived current evidence.
            var separatedAppDomainEvidence = new Evidence(AppDomain.CurrentDomain.Evidence);

            // Create AppDomain.
            var separatedAppDomain = AppDomain.CreateDomain(
                separatedAppDomainName,
                separatedAppDomainEvidence,
                separatedAppDomainSetup);

            try
            {
                // Create RemotableExecutor instance into new AppDomain.
                var remoteExecutor = (RemotableExecutor)separatedAppDomain.CreateInstanceFromAndUnwrap(
                    testRunnerAssemblyPath_,
                    remotableExecutorType_.FullName);

                // Execute.
                var sinkTrampoline = new SinkTrampoline(sink);
                remoteExecutor.Execute(
                    targetAssemblyPath,
                    sinkTrampoline);
            }
            finally
            {
                // Discard AppDomain.
                AppDomain.Unload(separatedAppDomain);
            }
        }

        private sealed class SinkTrampoline : MarshalByRefObject, IExecutorSink
        {
            private readonly IExecutorSink parentSink_;

            public SinkTrampoline(IExecutorSink parentSink)
            {
                Debug.Assert(parentSink != null);
                parentSink_ = parentSink;
            }

            public void Begin(string message)
            {
                parentSink_.Begin(message);
            }

            public void Finished(string message)
            {
                parentSink_.Finished(message);
            }
        }
    }
}
