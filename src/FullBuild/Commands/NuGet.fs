module Commands.NuGet


let Add (url : string) =
    let antho = Configuration.LoadAnthology ()
    let nugets = antho.NuGets @ [Anthology.RepositoryUrl.from url] |> List.distinct
    let newAntho = { antho
                     with
                        NuGets = nugets }
    Configuration.SaveAnthology newAntho

let List () =
    let antho = Configuration.LoadAnthology()
    for nuget in antho.NuGets do
        printfn "%s" nuget.toLocalOrUrl
