module Package 
open Anthology
open IoHelpers
open System.IO
open System.Xml.Linq
open System.Linq
open MsBuildHelpers
open Env

let FxVersion2Folder =
    [ ("v4.6", ["net46"]) 
      ("v4.5.4", ["net454"])
      ("v4.5.3", ["net453"])
      ("v4.5.2", ["net452"])
      ("v4.5.1", ["net451"])
      ("v4.5", ["45"; "net45"; "net45-full"])
      ("v4.0", ["40"; "net4"; "net40"; "net40-full"; "net40-client"])
      ("v3.5", ["35"; "net35"; "net35-full"])
      ("v2.0", ["20"; "net20"; "net20-full"; "net"])
      ("v1.1", ["11"; "net11"])    
      ("v1.0", ["10"]) ] 

let GenerateItemGroupContent (pkgDir : DirectoryInfo) (files : FileInfo seq) =
    seq {
        for file in files do
            let assemblyName = Path.GetFileNameWithoutExtension (file.FullName)
            let relativePath = ComputeRelativePath pkgDir file
            let hintPath = sprintf "%s%s" MSBUILD_PACKAGE_FOLDER relativePath |> ToUnix
            yield XElement(NsMsBuild + "Reference",
                    XAttribute(NsNone + "Include", assemblyName),
                    XElement(NsMsBuild + "HintPath", hintPath),
                    XElement(NsMsBuild + "Private", "true"))
    }

let GenerateItemGroup (fxLibs : DirectoryInfo) =
    let pkgDir = WorkspacePackageFolder ()
    let dlls = fxLibs.EnumerateFiles("*.dll")
    let exes = fxLibs.EnumerateFiles("*.exes")
    let files = Seq.append dlls exes
    GenerateItemGroupContent pkgDir files

let rec GenerateWhenContent (fxFolders : DirectoryInfo seq) (fxVersion : string) (nugetFolderAliases : string list) =
    let matchLibfolder (fx : string) (dir : DirectoryInfo) =
        if fx = "" then true
        else
            let dirNames = dir.Name.Replace("portable-", "").Split('+')
            dirNames |> Seq.contains fx

    match nugetFolderAliases with
    | [] -> null
    | fxFolder::tail -> let libDir = fxFolders |> Seq.tryFind (matchLibfolder fxFolder)
                        match libDir with
                        | None -> GenerateWhenContent fxFolders fxVersion tail
                        | Some libFolder -> let itemGroup = GenerateItemGroup libFolder
                                            if itemGroup.Any() then
                                                let condition = sprintf "'$(TargetFrameworkVersion)' == '%s'" fxVersion
                                                XElement(NsMsBuild + "When",
                                                    XAttribute(NsNone + "Condition", condition),
                                                    XElement(NsMsBuild + "ItemGroup", 
                                                        itemGroup))
                                            else
                                                null

let GenerateChooseContent (libDir : DirectoryInfo) =
    let whens = seq {
            if libDir.Exists then
                let fxFolders = libDir.EnumerateDirectories()
                for (fxName, _) in FxVersion2Folder do
                    let fxWhens = FxVersion2Folder |> Seq.skipWhile (fun (fx, _) -> fx <> fxName)
                                                   |> Seq.map (fun (_, folders) -> GenerateWhenContent fxFolders fxName folders)
                    let fxDefaultWhen = [ [""] ] |> Seq.map (fun folders -> GenerateWhenContent [libDir] fxName folders)
                    let fxWhen = Seq.append fxWhens fxDefaultWhen |> Seq.tryFind (fun x -> x <> null)

                    match fxWhen with
                    | Some x -> yield x
                    | None -> ()
        }
    if whens.Any() then XElement (NsMsBuild + "Choose", whens)
    else null
    
let GenerateDependenciesContent (dependencies : string seq) =
    seq {
        for dependency in dependencies do
            let dependencyTargets = sprintf "%s%s/package.targets" MSBUILD_PACKAGE_FOLDER dependency
            let pkgProperty = PackagePropertyName dependency
            let condition = sprintf "'$(%s)' == ''" pkgProperty
    
            yield XElement(NsMsBuild + "Import",
                      XAttribute(NsNone + "Project", dependencyTargets),
                      XAttribute(NsNone + "Condition", condition))
    }

let GenerateProjectContent (package : Package) (imports : XElement seq) (choose : XElement) =
    let defineName = PackagePropertyName package.Id
    let propCondition = sprintf "'$(%s)' == ''" defineName
    let project = XElement (NsMsBuild + "Project",
                    XAttribute (NsNone + "Condition", propCondition),
                    XElement (NsMsBuild + "PropertyGroup",
                        XElement (NsMsBuild + defineName, "Y")),
                    imports,
                    choose)
    project

let GetPackageDependencies (xnuspec : XDocument) =
    xnuspec.Descendants().Where(fun x -> x.Name.LocalName = "dependency") 
        |> Seq.map (fun x -> !> x.Attribute(NsNone + "id") : string)

let GenerateTargetForPackage (package : Package) =
    let pkgsDir = Env.WorkspacePackageFolder ()
    let pkgDir = pkgsDir |> GetSubDirectory package.Id
    let libDir = pkgDir |> GetSubDirectory "lib" 
    
    let nuspecFile = pkgDir |> GetFile (IoHelpers.AddExt package.Id NuSpec)
    let xnuspec = XDocument.Load (nuspecFile.FullName)
    let dependencies = GetPackageDependencies xnuspec

    let imports = GenerateDependenciesContent dependencies
    let choose = GenerateChooseContent libDir
    let project = GenerateProjectContent package imports choose

    let targetFile = pkgDir |> GetFile "package.targets" 
    project.Save (targetFile.FullName)

let GatherAllAssemblies (package : Package) =
    let pkgsDir = Env.WorkspacePackageFolder ()
    let pkgDir = pkgsDir |> GetSubDirectory package.Id 
    let dlls = pkgDir.EnumerateFiles("*.dll", SearchOption.AllDirectories)
    let exes = pkgDir.EnumerateFiles("*.exes", SearchOption.AllDirectories)
    let files = Seq.append dlls exes
    let distinctFiles = files |> Seq.map (fun x -> x.Name) |> Seq.distinct
    distinctFiles

let Install () =
    let confDir = Env.WorkspaceConfigFolder ()
    Exec.Exec "paket.exe" "install" confDir.FullName
    
    let antho = Configuration.LoadAnthology ()
    antho.Packages |> Seq.iter GenerateTargetForPackage
