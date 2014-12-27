.\.nuget\NuGet.exe Install Persimmon.Console -Pre -OutputDirectory packages -ExcludeVersion

.\packages\Persimmon.Console\tools\\Persimmon.Console.exe --parallel .\Persimmon.VisualStudio.TestExplorer.Tests\bin\Debug\Persimmon.VisualStudio.TestExplorer.Tests.dll
