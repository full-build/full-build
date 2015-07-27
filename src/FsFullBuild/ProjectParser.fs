module ProjectParser

open System
open System.IO
open System.Linq
open System.Xml.Linq
open Anthology

type ProjectDescriptor = 
    { Binaries : Binary list
      Packages : Package list
      Project : Project }

let NsMsBuild = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003")
let inline (!<) (x : ^a) : ^b = (((^a or ^b) : (static member op_Implicit : ^a -> ^b) x))
let inline (!>) (x : ^a) : ^b = (((^a or ^b) : (static member op_Explicit : ^a -> ^b) x))

let ParseGuid(s : string) = 
    match Guid.TryParseExact(s, "B") with
    | true, value -> value
    | _ ->  match Guid.TryParseExact(s, "D") with // F# guid are badly formatted
            | true, value -> value
            | _ -> failwith (sprintf "string %A is not a Guid" s)

let ExtractGuid(xdoc : XDocument) = 
    let xguid = xdoc.Descendants(NsMsBuild + "ProjectGuid").Single()
    let sguid = !> xguid : string
    ParseGuid sguid

let GetProjectGuid (dir : DirectoryInfo) (relFile : string) : Guid = 
    let file = relFile |> FileExtensions.GetFile dir
    let xdoc = XDocument.Load(file.FullName)
    ExtractGuid xdoc

let GetProjectReferences (prjDir : DirectoryInfo) (xdoc : XDocument) = 
    let prjRefs = xdoc.Descendants(NsMsBuild + "ProjectReference")
                  |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Include") : string)
                  |> Seq.map (GetProjectGuid prjDir)
    
    let fbRefs = xdoc.Descendants(NsMsBuild + "Import")
                 |> Seq.map (fun x -> !> x.Attribute(XNamespace.None + "Project") : string)
                 |> Seq.filter (fun x -> x.StartsWith("$(SolutionDir)/.full-build/"))
                 |> Seq.map (Path.GetFileNameWithoutExtension)
                 |> Seq.map ParseGuid
    
    prjRefs |> Seq.append fbRefs
            |> Seq.distinct
            |> Seq.toList

let ParseBinary(binRef : XElement) : Binary = 
    let inc = !> binRef.Attribute(XNamespace.None + "Include") : string
    let incFileName = inc.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries).[0]
    let hintPath = !> binRef.Descendants(NsMsBuild + "HintPath").SingleOrDefault() : string 
    
    let hintPath2 = if String.IsNullOrWhiteSpace(hintPath) then Some hintPath
                    else None
    { AssemblyName = incFileName
      HintPath = hintPath2 }

let GetBinaries(xdoc : XDocument) = 
    xdoc.Descendants(NsMsBuild + "Reference") |> Seq.map ParseBinary
                                              |> Seq.toList

let ParsePackage (pkgRef : XElement) : Package =
    let pkgId : string = !> pkgRef.Attribute(XNamespace.None + "id")
    let pkgVer = !> pkgRef.Attribute(XNamespace.None + "version") : string
    let pkgFx = !> pkgRef.Attribute(XNamespace.None + "targetFramework") : string
    { Id = pkgId
      Version = pkgVer
      TargetFramework = pkgFx }

let GetPackages (xdocLoader : FileInfo -> XDocument) (prjDir : DirectoryInfo) =
    let pkgFile = "packages.config" |> FileExtensions.GetFile prjDir
    let xdoc = xdocLoader pkgFile

    xdoc.Descendants(XNamespace.None + "package") |> Seq.map ParsePackage 
                                                  |> Seq.toList

let ParseProjectContent (xdocLoader : FileInfo -> XDocument) (repoDir : DirectoryInfo) (file : FileInfo) =
    let repoName = repoDir.Name
    let relativeProjectFile = FileExtensions.ComputeRelativePath repoDir file
    let xdoc = xdocLoader file
    let xguid = !> xdoc.Descendants(NsMsBuild + "ProjectGuid").Single() : string
    let guid = ParseGuid xguid
    let assemblyName = !> xdoc.Descendants(NsMsBuild + "AssemblyName").Single() : string
    
    let extension =  match !> xdoc.Descendants(NsMsBuild + "OutputType").Single() : string with
                     | "Library" -> OutputType.Dll
                     | _ -> OutputType.Exe
    
    let fxTarget = "v4.5"
    let prjRefs = GetProjectReferences file.Directory xdoc
    
    let binaries = GetBinaries xdoc
    let binRefs = binaries |> List.map (fun x -> x.AssemblyName)

    let packages = GetPackages xdocLoader file.Directory
    let pkgRefs = packages |> List.map (fun x -> x.Id)

    { Binaries = binaries
      Packages = packages
      Project = { Repository = repoName
                  RelativeProjectFile = relativeProjectFile
                  ProjectGuid = guid
                  AssemblyName = assemblyName
                  OutputType = extension
                  FxTarget = fxTarget
                  BinaryReferences = binRefs
                  PackageReferences = pkgRefs
                  ProjectReferences = prjRefs } }


let ParseProject (repoDir : DirectoryInfo) (file : FileInfo) : ProjectDescriptor = 
    ParseProjectContent (fun x -> XDocument.Load (x.FullName)) repoDir file
