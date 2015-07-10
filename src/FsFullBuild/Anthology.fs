module Anthology

open System


type OutputType =
    | Exe
    | Dll

type Application =
    {
        Name : string
        Projects : Guid list
    }

type Binary = 
    {
        AssemblyName : string
        HintPath : string
    }

type Bookmark =
    {
        Name : string
        Version : string
    }

type Package =
    {
        Name : string
        Version : string
    }

type Project =
    {
        ProjectGuid : Guid
        AssemblyName : string
        OutputType : OutputType
        RelativeProjectFile : string
        FxTarget : string
        ProjectReferences : Guid list
        BinaryReferences : string list
        PackageReferences : string list
    }

type Anthology =
    {
        Applications : Application list
        Binaries : Binary list
        Bookmarks : Bookmark list
        Packages : Package list
        Projects : Project list
    }


let LoadAnthology : Anthology =
    failwith "not implemented"

let SaveAnthology (anthology : Anthology) =
    failwith "not implemented"

