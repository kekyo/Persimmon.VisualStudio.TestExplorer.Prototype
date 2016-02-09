namespace Persimmon.Runner.Wrapper

open System.Collections.Generic
open System.Reflection

type IExecutor<'T> =
  abstract Execute: AssemblyName[] -> 'T[]
