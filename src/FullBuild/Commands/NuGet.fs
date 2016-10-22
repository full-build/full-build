module Commands.NuGet


let Add (url : string) =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let newGraph = graph.CreateNuGet url
    newGraph.Save()

let List () =
    let graph = Configuration.LoadAnthology() |> Graph.from
    for nuget in graph.NuGets do
        printfn "%s" nuget
