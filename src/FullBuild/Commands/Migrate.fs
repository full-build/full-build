module Commands.Migrate

let Migrate () =
    let confDir = Env.GetFolder Env.Folder.Config
    let anthoFile = confDir |> IoHelpers.GetFile "anthology"
    if anthoFile.Exists then
        let antho = Migration.AnthologyLoader2.Load anthoFile
        Configuration.SaveAnthology antho
        anthoFile.Delete()

        let projects = antho.Projects |> Seq.groupBy (fun x -> x.Repository)
                                      |> Seq.map (fun (r, p) -> r, p |> Set.ofSeq)
                                      |> dict

        for kvp in projects do
            let repo = kvp.Key
            let projects = { ProjectsSerializer.Projects = kvp.Value }
            Configuration.SaveProjectsRepository repo projects
