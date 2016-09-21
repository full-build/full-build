
module Graph

open Collections

[<RequireQualifiedAccess>] 
type PackageVersion =
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

[<Sealed>]
type Package  = interface System.IComparable
with
    member Name : string

[<Sealed>] 
type Assembly = interface System.IComparable
with
    member Name : string

[<Sealed>]
type Application = interface System.IComparable
with
    member Name : string
    member Publisher : PublisherType
    member Project : Project

and [<Sealed>] Repository = interface System.IComparable
with
    member Name : string
    member Builder : BuilderType
    member Projects : Project set

and [<Sealed>] Project = interface System.IComparable
with
    member Repository : Repository
    member Application : Application option
    member ReferencedBy : Project set
    member ProjectReferences : Project set
    member RelativeProjectFile : string
    member UniqueProjectId : string
    member Output : Assembly
    member ProjectId : string
    member OutputType : OutputType
    member FxVersion : string
    member FxProfile : string
    member FxIdentifier : string
    member HasTests : bool
    member AssemblyReferences : Assembly set
    member PackageReferences : Package set

type [<Sealed>] Graph =
    static member from : Anthology.Anthology -> Graph 
    member Projects : Project set
    member Applications : Application set    
    member Repositories : Repository set   
