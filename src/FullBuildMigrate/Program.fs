
open System.IO
open FsHelpers


[<EntryPoint>]
let main argv = 
    printfn "Migrating from v4.0 to v4.1"
    let wsDir = System.Environment.CurrentDirectory |> DirectoryInfo
    
    // load artifacts
    let artifactsFile = wsDir |> GetSubDirectory ".full-build" |> GetFile "artifacts"
    let artifacts = ArtifactsSerializer.Load artifactsFile

    // load all projects
    let mutable projects = Set.empty
    for repo in artifacts.Repositories do
        let localProjectsFile = wsDir |> GetSubDirectory repo.Repository.Name.toString |> GetFile ".fbprojects"
        if localProjectsFile.Exists then
            let localProjects = ProjectsSerializer.Load localProjectsFile
            projects <- projects |> Set.union localProjects        

    // save globals
    let globalsFile = wsDir |> GetSubDirectory ".full-build" |> GetFile "globals"
    let globals = { Anthology.MinVersion = artifacts.MinVersion
                    Anthology.Binaries = artifacts.Binaries
                    Anthology.NuGets = artifacts.NuGets
                    Anthology.Vcs = artifacts.Vcs
                    Anthology.MasterRepository = artifacts.MasterRepository
                    Anthology.Repositories = artifacts.Repositories
                    Anthology.Tester = artifacts.Tester }
    GlobalsSerializer.Save globalsFile globals

    // save anthology
    let anthology = { Anthology.Applications = artifacts.Applications
                      Anthology.Projects = projects }
    Configuration.SaveAnthology anthology

    // cleanup
    artifactsFile.Delete()
    for repo in artifacts.Repositories do
        let localProjectsFile = wsDir |> GetSubDirectory repo.Repository.Name.toString |> GetFile ".fbprojects"
        if localProjectsFile.Exists then localProjectsFile.Delete()

    0 // return an integer exit code
