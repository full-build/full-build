module Commands.NuGet


let Add (url : string) =
    let graph = Graph.load()
    let newGraph = graph.CreateNuGet url
    newGraph.Save()

let List () =
    let graph = Graph.load()
    for nuget in graph.NuGets do
        printfn "%s" nuget
