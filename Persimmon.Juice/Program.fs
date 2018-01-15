open System.IO
open System
open System.Reactive.Linq
open System.Diagnostics
open FSharp.Control.Reactive

let runTests (argv: string[]) =
  use proc = new Process()
  let startInfo = proc.StartInfo
  startInfo.FileName <- @".\Persimmon.Console\Persimmon.Console.exe"
  startInfo.Arguments <- argv |> Seq.map (fun x -> if x.StartsWith("-") then x else "\"" + x + "\"") |> String.concat " "
  startInfo.RedirectStandardOutput <- true
  startInfo.RedirectStandardError <- true
  startInfo.UseShellExecute <- false
  startInfo.CreateNoWindow <- true

  Console.Clear()
  printfn "run tests at %s." (DateTime.Now.ToString())
  printfn "%s %s" startInfo.FileName startInfo.Arguments
  proc.Start() |> ignore
  Console.WriteLine(proc.StandardOutput.ReadToEnd())
  Console.WriteLine(proc.StandardError.ReadToEnd())
  proc.WaitForExit()

  if proc.ExitCode = 0 then
    Console.ForegroundColor <- ConsoleColor.Green
    Console.WriteLine("success")
  else
    Console.ForegroundColor <- ConsoleColor.Red
    Console.WriteLine("failure")
  Console.WriteLine()
  Console.ResetColor()

  printfn "press 'q' to quit or Enter to run tests."

[<EntryPoint>]
let main argv =
  runTests(argv)
  
  let args = Args.parse Args.empty (List.ofArray argv)
  let watchFile = args.Inputs.Head

  use watcher = new FileSystemWatcher(watchFile.DirectoryName, watchFile.Name)

  Observable.merge watcher.Created watcher.Changed
  |> Observable.throttle (TimeSpan.FromSeconds(2.0))
  |> Observable.add (fun _ -> runTests argv)

  watcher.EnableRaisingEvents <- true
  
  let rec loop() =
    match Console.ReadKey().Key with
    | ConsoleKey.Q -> 0
    | ConsoleKey.Enter -> runTests argv; loop()
    | _ -> loop()
  loop()