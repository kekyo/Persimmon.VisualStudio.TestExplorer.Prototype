﻿namespace global

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: AssemblyTitle("Persimmon.Runner.Wrapper")>]
[<assembly: AssemblyProduct("Persimmon.Runner.Wrapper")>]
[<assembly: Guid("152bb9b3-89a3-4edc-b9d1-17bf3a03ce8e")>]

[<assembly: AssemblyVersion("1.0.0")>]
[<assembly: AssemblyFileVersion("1.0.0")>]
[<assembly: AssemblyInformationalVersion("1.0.0-alpha1")>]

#if DEBUG
[<assembly: InternalsVisibleTo("Persimmon.VisualStudio.TestExplorer, publickey=0024000004800000940000000602000000240000525341310004000001000100d9bea8339db64ef351726a408b9974b51a37808ed8937bccf2f695a8f6eb0845142c69d3629f5c1aa003816bc67e755d57819f957d5c569dc514509d9faced2771dbfa535df84219d10fb3e5ccad2f876b529c742f03521852a386bb8d21c72ad87e323aaa6403d4e038f883ebca3ebbc6a62c9e5038cef493827bb0160744a6")>]
#endif

do ()
