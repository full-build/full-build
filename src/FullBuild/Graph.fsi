
module Graph

open Collections

[<Sealed>]
type Application =
    member UnderlyingApplication : unit -> Anthology.Application
    member Project : unit -> Project
and [<Sealed>] Repository =
    member UnderlyingRepository : unit -> Anthology.BuildableRepository    
    member Projects : unit -> Project seq
and [<Sealed>] Project =
    member UnderlyingProject : unit -> Anthology.Project
    member Repository : unit -> Repository
    member Application : unit -> Application option
    member ReferencedBy : unit -> Project seq
    member References : unit -> Project seq
and[<Sealed>] Graph =
    static member from : Anthology.Anthology -> Graph 
    member Projects : unit -> Project seq
    member Applications : unit -> Application seq    
    member Repositories : unit -> Repository seq   
