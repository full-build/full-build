module Publishers
open Anthology
open IoHelpers
open Env
open System.IO


let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode



let publishCopy (app : Anthology.Application) =
    let wsDir = GetFolder Env.Workspace
    let antho = Configuration.LoadAnthology ()
    let project = antho.Projects |> Seq.find (fun x -> x.ProjectId = app.Project)
    let repoDir = wsDir |> GetSubDirectory (project.Repository.toString)
    if repoDir.Exists then
        let projFile = repoDir |> GetFile project.RelativeProjectFile.toString 
        let args = sprintf "/nologo /t:FBPublish /p:SolutionDir=%A /p:FBApp=%A %A" wsDir.FullName app.Name.toString projFile.FullName

        if Env.IsMono () then checkedExec "xbuild" args wsDir
        else checkedExec "msbuild" args wsDir
    else
        printfn "[WARNING] Can't publish application %A without repository" app.Name.toString

let publishZip (app : Anthology.Application) =
    let tmpApp = { app
                   with Name = ApplicationId.from "tmpzip"
                        Publisher = PublisherType.Copy }

    publishCopy tmpApp

    let appDir = GetFolder Env.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.Name.toString)
    let targetFile = appDir |> GetFile (AddExt Extension.Zip app.Name.toString)
    if targetFile.Exists then targetFile.Delete()

    System.IO.Compression.ZipFile.CreateFromDirectory(sourceFolder.FullName, targetFile.FullName, Compression.CompressionLevel.Optimal, false)
    sourceFolder.Delete(true)
    

let publishFake (app : Anthology.Application) =
    ()





let choosePublisher (pubType : PublisherType) appCopy appZip appFake =
    let publish = match pubType with
                  | PublisherType.Copy -> appCopy
                  | PublisherType.Zip -> appZip
                  | PublisherType.Fake -> appFake
    publish


let PublishWithPublisher (pubType : PublisherType) =
    choosePublisher pubType publishCopy publishZip publishFake

