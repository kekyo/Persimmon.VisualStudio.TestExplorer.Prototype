using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Persimmon.VisualStudio.TestExplorer2
{
    public class VSTestRunner : MarshalByRefObject
    {

        private static List<string> persimmonFamilies = new List<string>
        {
            "Persimmon.dll",
            "Persimmon.Runner.dll"
        };

        private const string runnerName = "Persimmon.Runner.Wrapper";

        private VSTestCase testCase = new VSTestCase();
        private VSTestResult testResult = new VSTestResult();

        public void DiscoverTests(List<string> sources, ITestCaseDiscoverySink sink)
        {
            this.Run<Runner.Wrapper.TestCase>("Persimmon.Runner.Wrapper.TestCollector", collector =>
            {
                foreach(var testcase in this.CollectTestCase(collector, sources))
                {
                    sink.SendTestCase(testcase);
                }
            });
        }

        public void RunTests(List<string> sources, IFrameworkHandle handle)
        {
            this.Run<Tuple<Runner.Wrapper.TestCase, Runner.Wrapper.TestResult>>("Persimmon.Runner.Wrapper.TestRunner", runner =>
            {
                foreach(var result in this.RunAllTestCase(runner, sources))
                {
                    handle.RecordResult(result);
                }
            });
        }

        private void Run<T>(string typeName, Action<Runner.Wrapper.IExecutor<T>> f)
        {
            var fullPath = Assembly.GetExecutingAssembly().Location;
            var directory = System.IO.Path.GetDirectoryName(fullPath);
            var setup = new AppDomainSetup
            {
                LoaderOptimization = LoaderOptimization.MultiDomain,
                PrivateBinPath = directory,
                ApplicationBase = directory,
                DisallowBindingRedirects = true
            };
            var evidence = AppDomain.CurrentDomain.Evidence;
            var appDomain = AppDomain.CreateDomain("persimmon visual studio test explorer domain", null, setup);
            try
            {
                f(this.CreateExecutor(appDomain, typeName));
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        private dynamic CreateExecutor(AppDomain appDomain, string typeName)
        {
            appDomain.Load(runnerName);
            return appDomain.CreateInstanceAndUnwrap(runnerName, typeName);
        }

        private IEnumerable<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase> CollectTestCase
            (Runner.Wrapper.IExecutor<Runner.Wrapper.TestCase> collector, List<string> sources)
        {
            var cs = sources
                .Where(x => !persimmonFamilies.Any(y => x.EndsWith(y)))
                .ToList();
            return collector.Execute(cs)
                .Select(testCase.Convert);
        }

        private IEnumerable<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult> RunAllTestCase
            (Runner.Wrapper.IExecutor<Tuple<Runner.Wrapper.TestCase, Runner.Wrapper.TestResult>> runner, List<string> sources)
        {
            var cs = sources
                .Where(x => !persimmonFamilies.Any(y => x.EndsWith(y)))
                .ToList();
            return runner.Execute(cs)
                .Select(tpl => testResult.Convert(testCase.Convert(tpl.Item1), tpl.Item2));
        }
    }
}
