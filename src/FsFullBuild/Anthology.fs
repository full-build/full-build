module Anthology

open System
open System.IO
open WellknownFolders
open FileExtensions
open Newtonsoft.Json



let ANTHOLOGY_FILENAME = "anthology.json"

[<JsonConverter(typeof<Newtonsoft.Json.Converters.StringEnumConverter>)>] 
type OutputType =
    | Exe = 0
    | Dll = 1

type Application =
    {
        Name : string
        Projects : Guid list
    }

type Binary = 
    {
        AssemblyName : string
        HintPath : string option
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
        AssemblyName : string
        OutputType : OutputType
        ProjectGuid : Guid
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

let private GetAnthologyFileName () =
    let wsDir = WorkspaceFolder ()
    let fbDir = wsDir |> GetSubDirectory WORKSPACE_CONFIG_FOLDER
    let anthoFn = fbDir |> GetFile ANTHOLOGY_FILENAME
    anthoFn

let LoadAnthologyFromFile (anthoFn : FileInfo) : Anthology =
    let json = File.ReadAllText anthoFn.FullName
    let antho = JsonConvert.DeserializeObject<Anthology> (json)
    antho

let SaveAnthologyToFile (anthoFn : FileInfo) (anthology : Anthology) =
    let json = JsonConvert.SerializeObject(anthology, Formatting.Indented);
    File.WriteAllText (anthoFn.FullName, json)

let LoadAnthology : Anthology =
    let anthoFn = GetAnthologyFileName ()
    LoadAnthologyFromFile anthoFn
    
let SaveAnthology (anthology : Anthology) =
    let anthoFn = GetAnthologyFileName ()
    SaveAnthologyToFile anthoFn anthology
