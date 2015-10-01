namespace Persimmon.Runner.Wrapper

open System.Collections.Generic
open System.Reflection

type IExecutor<'T> =
  abstract Execute: IEnumerable<Assembly> -> IEnumerable<'T>
