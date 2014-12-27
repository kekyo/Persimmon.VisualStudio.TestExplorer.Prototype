using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Persimmon.VisualStudio.TestExplorer2
{
    public class VSTestCase : MarshalByRefObject
    {
        public const string extensionUri = "executor://persimmon.visualstudio.testexplorer";

        public Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase Convert(Runner.Wrapper.TestCase testcase)
        {
            var c = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase(
                testcase.FullyQualifiedName,
                new Uri(extensionUri),
                testcase.Source
            )
            {
                DisplayName = testcase.DisplayName
            };
            var navigationData = this.GetNavigationData(testcase.ClassName, c.FullyQualifiedName, c.Source);
            if(navigationData != null)
            {
                c.CodeFilePath = navigationData.FileName;
                c.LineNumber = navigationData.MinLineNumber;
            }
            return c;
        }

        private DiaNavigationData GetNavigationData(string className, string methodName, string source)
        {
            var session = this.CreateDiaSession(source);
            if(session != null)
            {
                using(session)
                {
                    return session.GetNavigationData(className, methodName);
                }
            }
            return null;
        }

        private DiaSession CreateDiaSession(string souce)
        {
            try
            {
                return new DiaSession(souce);
            } catch
            {
                return null;
            }
        }
    }
}