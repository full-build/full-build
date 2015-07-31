module MsBuildHelpers
open Anthology
open System.Xml.Linq


let NsMsBuild = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003")

let NsNone = XNamespace.None

let inline (!<) (x : ^a) : ^b = (((^a or ^b) : (static member op_Implicit : ^a -> ^b) x))

let inline (!>) (x : ^a) : ^b = (((^a or ^b) : (static member op_Explicit : ^a -> ^b) x))


let ProjectPropertyName (project : Project) =
    sprintf "FullBuild_%s_%s_Prj" project.AssemblyName project.FxTarget
