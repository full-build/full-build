module Exec

open System.Diagnostics

let Exec (command : string) (args : string) (dir : string) =
    let psi = new ProcessStartInfo(FileName = command, Arguments = args, UseShellExecute = false, WorkingDirectory = dir)
    use proc = Process.Start(psi)
    if proc = null then failwith "Failed to start process"
    proc.WaitForExit()
    if proc.ExitCode <> 0 then failwith (sprintf "Process failed with error %d" proc.ExitCode)
