module BuildArtifacts
open System.IO
open IoHelpers
open Anthology



let Publish buildnum hash =
    let antho = Configuration.LoadAnthology ()
    let mainRepo = antho.MasterRepository
    let wsDir = Env.GetFolder Env.Workspace
    let versionDir = DirectoryInfo(antho.Artifacts) |> GetSubDirectory hash
    let tmpVersionDir = DirectoryInfo(versionDir.FullName + ".tmp")
    if tmpVersionDir.Exists then
        tmpVersionDir.Delete(true)
    if versionDir.Exists then
        printfn "[WARNING] Build output already exists - skipping"
    else
        try
            let sourceBinDir = Env.GetFolder Env.Bin
            let targetBinDir = tmpVersionDir |> GetSubDirectory Env.PUBLISH_BIN_FOLDER
            IoHelpers.CopyFolder sourceBinDir targetBinDir true

            let appTargetDir = tmpVersionDir |> GetSubDirectory Env.PUBLISH_APPS_FOLDER
            let appDir = Env.GetFolder Env.AppOutput
            IoHelpers.CopyFolder appDir appTargetDir true

            // publish
            Try (fun () -> Vcs.VcsPush wsDir mainRepo)

            tmpVersionDir.MoveTo(versionDir.FullName)

            let latestVersionFile = DirectoryInfo(antho.Artifacts) |> GetFile "versions"
            let version = sprintf "%s:%s" buildnum hash
            File.AppendAllLines(latestVersionFile.FullName, [version])
        with
            _ -> versionDir.Refresh ()
                 if versionDir.Exists then versionDir.MoveTo(versionDir.FullName + ".failed")

                 tmpVersionDir.Refresh()
                 if tmpVersionDir.Exists then tmpVersionDir.Delete(true)

                 reraise ()

let PullReferenceBinaries version =
    let antho = Configuration.LoadAnthology ()
    let artifactDir = antho.Artifacts |> DirectoryInfo

    let versionDir = artifactDir |> GetSubDirectory version
    if versionDir.Exists then
        DisplayHighlight (sprintf "Getting binaries %s" version)
        let sourceBinDir = versionDir |> GetSubDirectory Env.PUBLISH_BIN_FOLDER
        let targetBinDir = Env.GetFolder Env.Bin
        IoHelpers.CopyFolder sourceBinDir targetBinDir false
    else
        DisplayHighlight "[WARNING] No reference binaries found"

let PullLatestReferenceBinaries () =
    let antho = Configuration.LoadAnthology ()
    let versionsFile = DirectoryInfo(antho.Artifacts) |> GetFile "versions"
    let version = File.ReadAllLines(versionsFile.FullName) |> Seq.last
    let hash = version.Split(':') |> Seq.last
    PullReferenceBinaries hash
