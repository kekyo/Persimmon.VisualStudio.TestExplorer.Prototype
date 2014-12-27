using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Persimmon.VisualStudio.TestExplorer2
{
    public class VSTestResult : MarshalByRefObject
    {

        public Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult Convert(
            Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase testcase,
            Runner.Wrapper.TestResult result)
        {
            return new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(testcase)
            {
                DisplayName = result.DisplayName,
                Outcome = (TestOutcome)result.Outcome,
                Duration = result.Duration,
                ErrorMessage = result.ErrorMessage,
                ErrorStackTrace = result.ErrorStackTrace
            };
        }
    }
}
