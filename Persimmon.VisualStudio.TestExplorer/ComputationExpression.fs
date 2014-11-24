namespace Persimmon.VisualStudio.TestExplorer

open System

type OptionBuilder() =
  member __.Bind(x, f) = Option.bind f x
  member __.Return(x) = Some x
  member __.ReturnFrom(x) = x
  member this.Using(x: #IDisposable, f: #IDisposable -> _ option) =
    try f x
    finally match box x with null -> () | _ -> x.Dispose()

[<AutoOpen>]
module ComputationExpression =

  let option = OptionBuilder()
