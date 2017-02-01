module Commands.Migrate

let Migrate () =
    let confDir = Env.GetFolder Env.Folder.Config
    let anthoFile = confDir |> IoHelpers.GetFile "anthology"
    if anthoFile.Exists then
        Configuration.SaveBranch "master"

        let antho = Migration.AnthologyLoader2.Load anthoFile
        Configuration.SaveAnthology antho
        anthoFile.Delete()
