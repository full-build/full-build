module Algorithm
open Collections

let Closure<'T when 'T : comparison> (seeds : 'T set) (getName : 'T -> string) (iterDown : 'T -> 'T set) (iterUp : 'T -> 'T set) : 'T set =
    let rec exploreNext (node : 'T) (next : 'T -> 'T set) (path : 'T list) (boundaries : 'T set) =
        let nextNodes = next node
        Set.fold (fun s n -> s + (explore n next path s)) boundaries nextNodes

    and explore (node : 'T) (next : 'T -> 'T set) (path : 'T list) (boundaries : 'T set) =
        let currPath = node :: path
        if boundaries |> Set.contains node then
            currPath |> set
        else
            let hasCycle = path |> List.contains node

            if hasCycle then 
                path |> set
            else
                exploreNext node next currPath boundaries

    let refBoundaries = Set.fold (fun s t -> exploreNext t iterDown [t] s) seeds seeds
    let refByBoundaries = Set.fold (fun s t -> exploreNext t iterUp [t] s) refBoundaries seeds
    refByBoundaries
