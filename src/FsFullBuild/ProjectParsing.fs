module ProjectParsing

open System
open System.IO
open FSharp.Data
open System.Linq
open System.Xml.Linq
open Anthology


let NsMsBuild = XNamespace.Get ("http://schemas.microsoft.com/developer/msbuild/2003")

let inline (!<) (x : ^a) : ^b = ((^a or ^b) : (static member op_Implicit : ^a -> ^b) x)
let inline (!>) (x : ^a) : ^b = ((^a or ^b) : (static member op_Explicit : ^a -> ^b) x)

let ParseGuid (s : string) =
    match Guid.TryParseExact (s, "B") with
    | true, value -> value
    | _ -> match Guid.TryParseExact (s, "D") with // F# guid are badly formatted
           | true, value -> value
           | _ -> failwith (sprintf "string %A is not a Guid" s)

let ExtractGuid (xdoc : XDocument) =
    let xguid = xdoc.Descendants(NsMsBuild + "ProjectGuid").Single()
    let sguid = !> xguid : string
    ParseGuid sguid

let GetProjectGuid (dir : DirectoryInfo) (relFile : string) : Guid =
    let file = relFile |> FileExtensions.GetFile dir
    let xdoc = XDocument.Load (file.FullName)
    ExtractGuid xdoc

let GetProjectReferences (prjDir : DirectoryInfo) (xdoc : XDocument) =
    let prjRefs = xdoc.Descendants(NsMsBuild + "ProjectReference")
                  |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Include") : string)
                  |> Seq.map (GetProjectGuid prjDir)
    let fbRefs = xdoc.Descendants(NsMsBuild + "Import")
                 |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Project") : string)
                 |> Seq.filter (fun x -> x.StartsWith ("$(SolutionDir)/.full-build/"))
                 |> Seq.map (Path.GetFileNameWithoutExtension)
                 |> Seq.map ParseGuid
    prjRefs |> Seq.append fbRefs |> Seq.distinct |> Seq.toList

let ParseProject (repoDir : DirectoryInfo) (file : FileInfo) =
    let repoName = repoDir.Name
    let relativeProjectFile = FileExtensions.ComputeRelativePath repoDir file

    let xdoc = XDocument.Load file.FullName
    let xguid = !> xdoc.Descendants(NsMsBuild + "ProjectGuid").Single() : string
    let guid = ParseGuid xguid    
    let assemblyName = !> xdoc.Descendants(NsMsBuild + "AssemblyName").Single() : string

    let extension = match !> xdoc.Descendants(NsMsBuild + "OutputType").Single() : string with
                    | "Library" -> OutputType.Dll
                    | _ -> OutputType.Exe

    let fxTarget = "v4.5"

    let prjRefs = GetProjectReferences file.Directory xdoc

    { Repository = repoName; RelativeProjectFile = relativeProjectFile;
      ProjectGuid = guid; AssemblyName = assemblyName; OutputType = extension; FxTarget = fxTarget; 
      BinaryReferences = []; PackageReferences = []; ProjectReferences = prjRefs }
