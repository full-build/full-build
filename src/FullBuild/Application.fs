module Application
open Anthology
open Collections


let Deploy (names : ApplicationId set) =
    ()


let ListApplicationsContent (apps : Application seq) =
    seq {
        for app in apps do
            yield sprintf "%s" app.Name.Value
    }

let List () =
    let antho = Configuration.LoadAnthology ()

    let content = ListApplicationsContent antho.Applications 
    content |> Seq.iter (fun x -> printfn "%s" x)
