open System.IO
open System
open System.Reactive.Linq
open System.Diagnostics

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
  proc.Start() |> ignore
  Console.WriteLine(proc.StandardOutput.ReadToEnd())
  Console.WriteLine(proc.StandardError.ReadToEnd())
  proc.WaitForExit()

  printfn "press 'q' to quit or Enter to run tests."

module Observable =
  let throttle (dueTime: TimeSpan) (source: IObservable<_>) = source.Throttle(dueTime)

[<EntryPoint>]
let main argv =
  runTests(argv)
  
  let args = Args.parse Args.empty (List.ofArray argv)
  let watchFile = args.Inputs.Head

  use watcher = new FileSystemWatcher(watchFile.DirectoryName, watchFile.Name)

  Observable.merge watcher.Created watcher.Changed
  |> Observable.throttle (TimeSpan.FromSeconds(1.0))
  |> Observable.add (fun _ -> runTests argv)

  watcher.EnableRaisingEvents <- true
  
  let rec loop() =
    match Console.ReadKey().Key with
    | ConsoleKey.Q -> 0
    | ConsoleKey.Enter -> runTests argv; loop()
    | _ -> loop()
  loop()