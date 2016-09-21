
module Graph

open Collections

type [<RequireQualifiedAccess>] PackageVersion =
    | PackageVersion of string
    | Unspecified

type [<Sealed>] Package = 
    member Name : string

type [<Sealed>] Assembly = 
    member Name : string

type [<Sealed>] Application =
    member Name : string
    member Publisher : Anthology.PublisherType
    member Project : Project

and [<Sealed>] Repository =
    member UnderlyingRepository : Anthology.BuildableRepository    
    member Projects : Project seq

and [<Sealed>] Project =
    member UnderlyingProject : Anthology.Project
    member Repository : Repository
    member Application : Application option
    member ReferencedBy : Project seq
    member ProjectReferences : Project seq

    member RelativeProjectFile : Anthology.ProjectRelativeFile
    member UniqueProjectId : Anthology.ProjectUniqueId
    member Output : Anthology.AssemblyId
    member ProjectId : Anthology.ProjectId
    member OutputType : Anthology.OutputType
    member FxVersion : string
    member FxProfile : string
    member FxIdentifier : string
    member HasTests : bool
    member AssemblyReferences : Assembly seq
    member PackageReferences : Package seq

type [<Sealed>] Graph =
    static member from : Anthology.Anthology -> Graph 
    member Projects : Project seq
    member Applications : Application seq    
    member Repositories : Repository seq   
