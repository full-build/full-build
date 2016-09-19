
module Graph

open Collections

[<Sealed>]
type Application =
    member UnderlyingApplication : Anthology.Application
    member Project : Project
and [<Sealed>] Repository =
    member UnderlyingRepository : Anthology.BuildableRepository    
    member Projects : Project seq
and [<Sealed>] Project =
    member UnderlyingProject : Anthology.Project
    member Repository : Repository
    member Application : Application option
    member ReferencedBy : Project seq
    member References : Project seq
and[<Sealed>] Graph =
    static member from : Anthology.Anthology -> Graph 
    member Projects : Project seq
    member Applications : Application seq    
    member Repositories : Repository seq   
