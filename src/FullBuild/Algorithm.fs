module Algorithm
open Collections

let private computeClosure<'T when 'T : comparison> onCycle (seeds : 'T set) (iterDown : 'T -> 'T set) (iterUp : 'T -> 'T set) : 'T set =
    let rec exploreNext (node : 'T) (next : 'T -> 'T set) (path : 'T list) (boundaries : 'T set) =
        let nextNodes = next node
        Set.fold (fun s n -> s + (explore n next path s)) boundaries nextNodes

    and explore (node : 'T) (next : 'T -> 'T set) (path : 'T list) (boundaries : 'T set) =
        let currPath = node :: path
        let hasCycle = path |> List.contains node
        match onCycle with
        | _ when hasCycle |> not -> if boundaries |> Set.contains node then currPath |> set
                                    else exploreNext node next currPath boundaries
        | Some f -> currPath |> f |> failwith
        | _ -> path |> set
 
    let refBoundaries = Set.fold (fun s t -> exploreNext t iterDown [t] s) seeds seeds
    let refByBoundaries = Set.fold (fun s t -> exploreNext t iterUp [t] s) refBoundaries seeds
    refByBoundaries


let Closure<'T when 'T : comparison> = computeClosure<'T> None

let private onCycle<'T when 'T : comparison> (getName : 'T -> string) currPath =
    currPath |> Seq.rev
             |> Seq.map getName
             |> String.concat " -> "

let FindCycle<'T when 'T : comparison> (seeds : 'T set) (getName : 'T -> string) (iterDown : 'T -> 'T set) (iterUp : 'T -> 'T set) : string option =
    try
        computeClosure<'T> (onCycle getName |> Some) seeds iterDown iterUp |> ignore
        None
    with
        exn -> Some exn.Message
 