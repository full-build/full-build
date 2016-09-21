
module Graph

open Collections

type [<RequireQualifiedAccess>] PackageVersion =
    | PackageVersion of string
    | Unspecified

[<RequireQualifiedAccess>]
type OutputType =
    | Exe
    | Dll

[<RequireQualifiedAccess>]
type PublisherType =
    | Copy
    | Zip
    | Docker

[<RequireQualifiedAccess>]
type BuilderType =
    | MSBuild
    | Skip

type [<Sealed>] Package = 
    member Name : string

type [<Sealed>] Assembly = 
    member Name : string

type [<Sealed>] Application =
    member Name : string
    member Publisher : PublisherType
    member Project : Project

and [<Sealed>] Repository =
    member Name : string
    member Builder : BuilderType
    member Projects : Project seq

and [<Sealed>] Project =
    member Repository : Repository
    member Application : Application option
    member ReferencedBy : Project seq
    member ProjectReferences : Project seq
    member RelativeProjectFile : string
    member UniqueProjectId : string
    member Output : Assembly
    member ProjectId : string
    member OutputType : OutputType
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
