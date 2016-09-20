﻿
module Graph

open Collections


[<RequireQualifiedAccess>]
type PackageVersion =
    | PackageVersion of string
    | Unspecified

and [<Sealed>] Package = 
    member Name : string
    member Version : PackageVersion

and [<Sealed>] Assembly = 
    member Name : string

and [<Sealed>] Application =
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
    member FxVersion : Anthology.FxInfo
    member FxProfile : Anthology.FxInfo
    member FxIdentifier : Anthology.FxInfo
    member HasTests : bool
    member AssemblyReferences : Assembly seq
    member PackageReferences : Package seq
and[<Sealed>] Graph =
    static member from : Anthology.Anthology -> Graph 
    member Projects : Project seq
    member Applications : Application seq    
    member Repositories : Repository seq   
