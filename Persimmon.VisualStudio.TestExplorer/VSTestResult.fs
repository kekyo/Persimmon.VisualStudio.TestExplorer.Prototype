namespace Persimmon.VisualStudio.TestExplorer

open System
open Microsoft.VisualStudio.TestPlatform.ObjectModel
//open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
//open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
open Persimmon
open Persimmon.ActivePatterns

// copy from https://github.com/persimmon-projects/Persimmon/blob/086828c9faacb49e5282fb8042b86ba2231ed14a/Persimmon.Runner/Formatter.fs#L29\
module private Formatter =

  let private tryGetCause = function
  | Passed _ -> None
  | NotPassed cause -> Some cause

  let indentStr indent = String.replicate indent " "

  let private bar width (barChar: char) (center: string) =
    let barLen = width - center.Length - 2
    let left = barLen / 2
    let res = (String.replicate left (string barChar)) + " " + center + " "
    let right = width - res.Length
    res + (String.replicate right (string barChar))

  let private causesToStrs indent (causes: NotPassedCause list) =
    causes
    |> Seq.mapi (fun i (Skipped c | Violated c) -> (i + 1, c))
    |> Seq.collect (fun (i, c) ->
        seq {
          match c.Split([|"\r\n";"\r";"\n"|], StringSplitOptions.None) |> Array.toList with
          | [] -> yield ""
          | x::xs ->
              let no = (string i) + ". "
              yield indent + no + x
              yield! xs |> Seq.map (fun x -> indent + (String.replicate no.Length " ") + x)
        }
       )

  let exnsToStrs indent (exns: exn list) =
    exns
    |> Seq.mapi (fun i exn -> (i + 1, exn))
    |> Seq.collect (fun (i, exn) ->
        seq {
          match exn.ToString().Split([|"\r\n";"\r";"\n"|], StringSplitOptions.None) |> Array.toList with
          | [] -> yield ""
          | x::xs ->
              let no = (string i) + ". "
              yield indent + no + x
              yield! xs |> Seq.map (fun x -> indent + (String.replicate no.Length " ") + x)
        }
       )

  let rec toStrs indent = function
  | EndMarker -> Seq.empty
  | ContextResult ctx ->
      seq {
        yield (indentStr indent) + "begin " + ctx.Name
        yield! ctx.Children |> Seq.collect (toStrs (indent + 1))
        yield (indentStr indent) + "end " + ctx.Name
      }
  | TestResult tr ->
      match tr with
      | Error (meta, es, res, _) ->
          seq {
            let indent = indentStr indent
            yield indent + "FATAL ERROR: " + meta.FullName
            if not (res.IsEmpty) then
              yield (bar (70 - indent.Length) '-' "finished assertions")
              yield! res |> causesToStrs indent
            yield indent + (bar (70 - indent.Length) '-' "exceptions")
            yield! es |> List.rev |> exnsToStrs indent
          }
      | Done (meta, res, _) ->
          seq {
            let indent = indentStr indent
            match res |> AssertionResult.NonEmptyList.typicalResult with
            | Passed _ -> ()
            | NotPassed (Skipped _) ->
                yield indent + "Assertion skipped: " + meta.FullName
            | NotPassed (Violated _) ->
                yield indent + "Assertion Violated: " + meta.FullName
            let causes = res |> NonEmptyList.toList |> List.choose tryGetCause
            yield! causes |> causesToStrs indent
          }

module VSTestResult =

  let private assertionToTestOutcome = function
  | Passed _ -> TestOutcome.Passed
  | NotPassed (Skipped _) -> TestOutcome.Skipped
  | NotPassed (Violated _) -> TestOutcome.Failed

  let private assertionsToTestOutcome results =
    (match results |> List.tryFind (function | NotPassed (Violated _) -> true | _ -> false) with
    | Some v -> v |> assertionToTestOutcome
    | None ->
      match results |> List.tryFind (function | Passed _ -> true | _ -> false) with
      | Some p -> p |> assertionToTestOutcome
      | None ->
        if List.isEmpty results then TestOutcome.None
        else List.head results |> assertionToTestOutcome)

  let rec private resultToTestOutcome = function
  | EndMarker -> TestOutcome.None
  | ContextResult ctx ->
    ctx.Children
    |> List.map resultToTestOutcome
    |> List.fold (fun res o ->
      if res = TestOutcome.Failed then res
      else res) TestOutcome.None
  | TestResult tr ->
    match tr with
    | Error(_, _, a, _) -> a |> List.map NotPassed |> assertionsToTestOutcome
    | Done(_, a, _) -> a |> NonEmptyList.toList  |> assertionsToTestOutcome

  let rec private collectException = function
  | EndMarker -> []
  | ContextResult ctx -> ctx.Children |> List.collect collectException
  | TestResult tr ->
    match tr with
    | Error(_, el, _, _) -> el
    | _ -> []

  let private appendStrs xs = Seq.fold (fun acc s -> sprintf "%s%s%s" acc s Environment.NewLine) "" xs

  let rec private collectDuration = function
  | EndMarker -> TimeSpan.Zero
  | ContextResult ctx -> ctx.Children |> List.map collectDuration |> List.fold (+) TimeSpan.Zero
  | TestResult tr ->
    match tr with
    | Error(_, _, _, d)
    | Done(_, _, d) -> d

  let ofPersimmonTestResult case result =
    let msg = result |> Formatter.toStrs 0 |> appendStrs
    let trace = result |> collectException |> Formatter.exnsToStrs (Formatter.indentStr 0) |> appendStrs
    let d = collectDuration result
    let outcome = resultToTestOutcome result
    let result =
      TestResult(
        case,
        DisplayName = case.DisplayName,
        Outcome = outcome,
        Duration = d)
    if String.IsNullOrEmpty(msg) then result.ErrorMessage <- msg
    if String.IsNullOrEmpty(trace) then result.ErrorStackTrace <- msg
    result
