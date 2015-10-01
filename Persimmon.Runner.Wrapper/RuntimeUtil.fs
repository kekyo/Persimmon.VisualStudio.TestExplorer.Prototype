﻿// copy from Persimmon.Runner
module Persimmon.Runner.Wrapper.RuntimeUtil

open Persimmon

open System
open System.Reflection
open Microsoft.FSharp.Reflection

module internal Runtime =
  let getModule<'TSameAssemblyType>(name: string) = (typeof<'TSameAssemblyType>).Assembly.GetType(name)

  let invoke (m: MethodInfo) typeArgs args =
    let f =
      match typeArgs with
      | [||] -> m
      | _ -> m.MakeGenericMethod(typeArgs)
    f.Invoke(null, args)

  let lambda (srcType, dstType) body =
    let typ = FSharpType.MakeFunctionType(srcType, dstType)
    FSharpValue.MakeFunction(typ, body)

module internal RuntimeSeq =
  let private typ = Runtime.getModule<_ list>("Microsoft.FSharp.Collections.SeqModule")

  let private mapMethod = typ.GetMethod("Map")
  let map<'TDest> (f: obj -> obj (* 'a -> 'TDest *)) (xs: obj (* 'a seq *), elemType: Type (* typeof<'a> *)) =
    let f = Runtime.lambda (elemType, typeof<'TDest>) f
    let result = Runtime.invoke mapMethod [| elemType; typeof<'TDest> |] [| f; xs |]
    result :?> 'TDest seq

module internal RuntimeList =
  let private typ = Runtime.getModule<_ list>("Microsoft.FSharp.Collections.ListModule")

  let private mapMethod = typ.GetMethod("Map")
  let map<'TDest> (f: obj -> obj (* 'a -> 'TDest *)) (xs: obj (* 'a list *), elemType: Type (* typeof<'a>*)) =
    let f = Runtime.lambda (elemType, typeof<'TDest>) f
    let result = Runtime.invoke mapMethod [| elemType; typeof<'TDest> |] [| f; xs |]
    result :?> 'TDest list

module internal RuntimeArray =
  let private typ = Runtime.getModule<_ list>("Microsoft.FSharp.Collections.ArrayModule")

  let private mapMethod = typ.GetMethod("Map")
  let map<'TDest> (f: obj -> obj (* 'a -> 'TDest *)) (xs: obj (* 'a[] *), elemType: Type (* typeof<'a>*)) =
    let f = Runtime.lambda (elemType, typeof<'TDest>) f
    let result = Runtime.invoke mapMethod [| elemType; typeof<'TDest> |] [| f; xs |]
    result :?> 'TDest[]
