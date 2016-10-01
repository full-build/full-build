module GraphHelpers
open Graph
open Collections

let ComputeTransitiveReferences (seeds : Project set) : Project set =
    let rec collectChildren (project : Project) =
        let allChildren = project.References |> set
        allChildren |> Set.fold (fun s t -> collectChildren t |> Set.union s) allChildren
    seeds |> Set.fold (fun s t -> s |> Set.union (collectChildren t)) seeds            

let ComputeTransitiveReferencedBy (seeds : Project set) : Project set =
    let rec collectParents (project : Project) =
        let allParents = project.ReferencedBy |> set
        allParents |> Set.fold (fun s t -> collectParents t |> Set.union s) allParents
    seeds |> Set.fold (fun s t -> s |> Set.union (collectParents t)) seeds            

let ComputeClosure (seeds : Project set) : Project set =

    // input: 
    //   - project : this is our current project in exploration path
    //   - explorationProjects : list of project that lead to this project
    //   - failedExploredProjects : set of explored projects that failed
    //   - boundaries : set of boundaries
    // output:
    //   - explorationProjects : set of project leading to successful search
    //   - exploredProjects : failed exploration path
    // if /project/ is in /failedExploredProjects/ then fail exploration
    // if /project/ is in /boundaries/ then validate exploration path
    // else explore /project/ dependencies and referencedBy:
    //          - project : explored reference
    //          - explorationPath : project @ explorationPath
    //          - exploredProject : exploredProject
    //          - boundaries : boundaries
    //      exploredProjects is enhanced with exploredProjects
    //      boundaries is enhanced with explorationPath

    let rec exploreProjects (projects : Project set) (exploration : Project set) (redProjects : Project set) (greenProjects : Project set) : Project set*Project set =

        let rec explore (project : Project) (exploration : Project set) (redProjects : Project set) (greenProjects : Project set) : Project set*Project set =
            if redProjects |> Set.contains project then
                (redProjects, Set.empty)
            else if greenProjects |> Set.contains project then
                (Set.empty, exploration)
            else
                let linkProjects = project.References |> set |> Set.union (project.ReferencedBy |> set)
                exploreProjects linkProjects linkProjects redProjects greenProjects

        let mutable allRedProjects = redProjects
        let mutable allGreenProjects = greenProjects
        for linkProject in projects do
            let res = explore linkProject (exploration |> Set.add linkProject) allRedProjects allGreenProjects
            allRedProjects <- (res |> fst) |> Set.union allRedProjects
            allGreenProjects <- (res |> snd) |> Set.union allGreenProjects
        (allGreenProjects |> Set.difference allRedProjects, allGreenProjects)

    let res = exploreProjects seeds Set.empty Set.empty Set.empty
    res |> snd

