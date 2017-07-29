module Algorithm
open Collections

let private computeClosure<'T when 'T : comparison> (checkCycle : bool) (seeds : 'T set) (getName : 'T -> string) (iterDown : 'T -> 'T set) (iterUp : 'T -> 'T set) : 'T set =
    let rec exploreNext (node : 'T) (next : 'T -> 'T set) (path : 'T list) (boundaries : 'T set) =
        let nextNodes = next node
        Set.fold (fun s n -> s + (explore n next path s)) boundaries nextNodes

    and explore (node : 'T) (next : 'T -> 'T set) (path : 'T list) (boundaries : 'T set) =
        let currPath = node :: path

        // detect cycle first - eventually break there
        let hasCycle = path |> List.contains node
        if hasCycle then 
            if checkCycle then currPath |> Seq.rev
                                        |> Seq.map getName
                                        |> String.concat " -> "
                                        |> failwith
            else path |> set
        else
            if boundaries |> Set.contains node then currPath |> set
            else exploreNext node next currPath boundaries

    let refBoundaries = Set.fold (fun s t -> exploreNext t iterDown [t] s) seeds seeds
    let refByBoundaries = Set.fold (fun s t -> exploreNext t iterUp [t] s) refBoundaries seeds
    refByBoundaries


let Closure<'T when 'T : comparison> = computeClosure<'T> false

let FindCycle<'T when 'T : comparison> (seeds : 'T set) (getName : 'T -> string) (iterDown : 'T -> 'T set) (iterUp : 'T -> 'T set) : string option =
    try
        computeClosure<'T> true seeds getName iterDown iterUp |> ignore
        None
    with
        exn -> Some exn.Message
 