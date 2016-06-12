//   Copyright 2014-2016 Pierre Chalamet
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

module Package 
open Anthology
open IoHelpers
open System.IO
open System.Xml.Linq
open System.Linq
open MsBuildHelpers
open Env
open Collections
open Simplify


let GenerateItemGroupContent (pkgDir : DirectoryInfo) (files : FileInfo seq) =
    seq {
        for file in files do
            let assemblyName = Path.GetFileNameWithoutExtension (file.FullName)
            let relativePath = ComputeRelativeFilePath pkgDir file
            let hintPath = sprintf "%s%s" MSBUILD_PACKAGE_FOLDER relativePath |> ToUnix
            yield XElement(NsMsBuild + "Reference",
                    XAttribute(NsNone + "Include", assemblyName),
                    XElement(NsMsBuild + "HintPath", hintPath),
                    XElement(NsMsBuild + "Private", "true"))
    }



let GenerateItemGroupCopyContent (pkgDir : DirectoryInfo) (fxLibs : DirectoryInfo) =
    let relativePath = ComputeRelativeDirPath pkgDir fxLibs
    let files = sprintf "$(FBWorkspaceDir)/.full-build/packages/%s/**/*.*" relativePath
    let copyFiles = XElement(NsMsBuild + "FBCopyFiles",
                        XAttribute(NsNone + "Include", files))
    copyFiles        



let GenerateItemGroup (fxLibs : DirectoryInfo) (condition : string) =
    let pkgDir = Env.GetFolder Env.Package
    let dlls = fxLibs.EnumerateFiles("*.dll")
    let exes = fxLibs.EnumerateFiles("*.exes")
    let files = Seq.append dlls exes
    let itemGroup = GenerateItemGroupContent pkgDir files
    XElement(NsMsBuild + "When",
        XAttribute(NsNone + "Condition", condition),
            XElement(NsMsBuild + "ItemGroup", 
                itemGroup))


let GenerateItemGroupCopy (fxLibs : DirectoryInfo) (condition : string) =
    let pkgDir = Env.GetFolder Env.Package
    let itemGroup = GenerateItemGroupCopyContent pkgDir fxLibs
    XElement(NsMsBuild + "When",
        XAttribute(NsNone + "Condition", condition),
            XElement(NsMsBuild + "ItemGroup", 
                itemGroup))


let GenerateChooseRefContent (libDir : DirectoryInfo) (package : PackageId) =
    let pkgProp = PackagePropertyName package

    let whens = seq {    
        if libDir.Exists then
            let foundDirs = libDir.EnumerateDirectories() |> Seq.map (fun x -> x.Name) |> List.ofSeq
            // for very old nugets we do not have folder per platform
            let dirs = if foundDirs.Length = 0 then [""]
                       else foundDirs
            let path2platforms = Paket.PlatformMatching.getSupportedTargetProfiles dirs

            for path2pf in path2platforms do
                let pathLib = libDir |> IoHelpers.GetSubDirectory path2pf.Key
                let condition = Paket.PlatformMatching.getCondition None (List.ofSeq path2pf.Value)
                let whenCondition = if condition = "$(TargetFrameworkIdentifier) == 'true'" then "True"
                                    else condition
                yield GenerateItemGroup pathLib whenCondition
    }

    seq {
        if whens.Any() then
            yield XElement (NsMsBuild + "Choose", whens)
    }



let GenerateChooseCopyContent (libDir : DirectoryInfo) (package : PackageId) =
    let pkgProp = PackagePropertyName package

    let whens = seq {    
        if libDir.Exists then
            let foundDirs = libDir.EnumerateDirectories() |> Seq.map (fun x -> x.Name) |> List.ofSeq
            // for very old nugets we do not have folder per platform
            let dirs = if foundDirs.Length = 0 then [""]
                       else foundDirs
            let path2platforms = Paket.PlatformMatching.getSupportedTargetProfiles dirs

            for path2pf in path2platforms do
                let pathLib = libDir |> IoHelpers.GetSubDirectory path2pf.Key
                let condition = Paket.PlatformMatching.getCondition None (List.ofSeq path2pf.Value)
                let whenCondition = if condition = "$(TargetFrameworkIdentifier) == 'true'" then "True"
                                    else condition
                yield GenerateItemGroupCopy pathLib whenCondition
    }

    seq {
        if whens.Any() then
            yield XElement (NsMsBuild + "Choose", whens)
    }





    
let GenerateDependenciesRefContent (dependencies : PackageId seq) =
    seq {
        for dependency in dependencies do
            let depId = dependency.toString
            let dependencyTargets = sprintf "%s%s/package.targets" MSBUILD_PACKAGE_FOLDER depId
            let pkgProperty = PackagePropertyName dependency
            let condition = sprintf "'$(%s)' == ''" pkgProperty
    
            yield XElement(NsMsBuild + "Import",
                      XAttribute(NsNone + "Project", dependencyTargets),
                      XAttribute(NsNone + "Condition", condition))
    }



let GenerateDependenciesCopyContent (dependencies : PackageId seq) =
    seq {
        for dependency in dependencies do
            let depId = dependency.toString
            let dependencyTargets = sprintf "%s%s/packagecopy.targets" MSBUILD_PACKAGE_FOLDER depId
            let pkgProperty = PackagePropertyName dependency
            let condition = sprintf "'$(%sCopy)' == ''" pkgProperty
    
            yield XElement(NsMsBuild + "Import",
                      XAttribute(NsNone + "Project", dependencyTargets),
                      XAttribute(NsNone + "Condition", condition))
    }




let GenerateProjectRefContent (package : PackageId) (imports : XElement seq) (choose : XElement seq) =
    let defineName = PackagePropertyName package
    let propCondition = sprintf "'$(%s)' == ''" defineName
    let project = XElement (NsMsBuild + "Project",
                    XAttribute (NsNone + "Condition", propCondition),
                    XElement (NsMsBuild + "PropertyGroup",
                        XElement (NsMsBuild + defineName, "Y")),
                    imports,
                    choose)
    project


let GenerateProjectCopyContent (package : PackageId) (imports : XElement seq) (choose : XElement seq) =
    let defineName = sprintf "%sCopy" (PackagePropertyName package)
    let propCondition = sprintf "'$(%s)' == ''" defineName
    let project = XElement (NsMsBuild + "Project",
                    XAttribute (NsNone + "Condition", propCondition),
                    XElement (NsMsBuild + "PropertyGroup",
                        XElement (NsMsBuild + defineName, "Y")),
                    imports,
                    choose)
    project


let GenerateProjectContent (package : PackageId) =
    let defineName = PackagePropertyName package
    let propConditionRef = sprintf "'$(%s)' == ''" defineName
    let propConditionCopy = sprintf "'$(%sCopy)' == ''" defineName
                    
    let refFile = sprintf "%s%s/packageref.targets" MSBUILD_PACKAGE_FOLDER (package.toString)
    let copyFile = sprintf "%s%s/packagecopy.targets" MSBUILD_PACKAGE_FOLDER (package.toString)

    let project = XElement (NsMsBuild + "Project",
                    XElement(NsMsBuild + "Import", new XAttribute(NsNone + "Project", refFile), new XAttribute(NsNone + "Condition", propConditionRef)),
                    XElement(NsMsBuild + "Import", new XAttribute(NsNone + "Project", copyFile), new XAttribute(NsNone + "Condition", propConditionCopy)))
    project


let GenerateTargetForPackageRef (package : PackageId) =
    let pkgsDir = Env.GetFolder Env.Package
    let pkgDir = pkgsDir |> GetSubDirectory (package.toString)
    let libDir = pkgDir |> GetSubDirectory "lib" 
    
    let nuspecFile = pkgDir |> GetFile (IoHelpers.AddExt NuSpec (package.toString))
    let xnuspec = XDocument.Load (nuspecFile.FullName)
    let dependencies = NuGets.GetPackageDependencies xnuspec

    let imports = GenerateDependenciesRefContent dependencies
    let choose = GenerateChooseRefContent libDir package
    let project = GenerateProjectRefContent package imports choose

    let targetFile = pkgDir |> GetFile "package.targets" 
    project.Save (targetFile.FullName)

let GenerateTargetForPackageCopy (package : PackageId) =
    let pkgsDir = Env.GetFolder Env.Package
    let pkgDir = pkgsDir |> GetSubDirectory (package.toString)
    let libDir = pkgDir |> GetSubDirectory "lib" 
    
    let nuspecFile = pkgDir |> GetFile (IoHelpers.AddExt NuSpec (package.toString))
    let xnuspec = XDocument.Load (nuspecFile.FullName)
    let dependencies = NuGets.GetPackageDependencies xnuspec

    let imports = GenerateDependenciesCopyContent dependencies
    let choose = GenerateChooseCopyContent libDir package
    let project = GenerateProjectCopyContent package imports choose

    let targetFile = pkgDir |> GetFile "packagecopy.targets" 
    project.Save (targetFile.FullName)


let GenerateTargetsForPackage (package : PackageId) =
    GenerateTargetForPackageRef package
    GenerateTargetForPackageCopy package



let GatherAllAssemblies (package : PackageId) : AssemblyId set =
    let pkgsDir = Env.GetFolder Env.Package
    let pkgDir = pkgsDir |> GetSubDirectory (package.toString)

    let nuspecFile = pkgDir |> GetFile (IoHelpers.AddExt NuSpec (package.toString))
    let xnuspec = XDocument.Load (nuspecFile.FullName)
    let fxDependencies = NuGets.GetFrameworkDependencies xnuspec

    let dlls = pkgDir.EnumerateFiles("*.dll", SearchOption.AllDirectories)
    let exes = pkgDir.EnumerateFiles("*.exes", SearchOption.AllDirectories)
    let files = Seq.append dlls exes |> Seq.map AssemblyId.from 
                                     |> Set
    Set.difference files fxDependencies


let GeneratePackageImports () =
    PaketInterface.ParsePaketDependencies ()
        |> NuGets.BuildPackageDependencies
        |> Map.toList
        |> Seq.map fst
        |> Seq.iter GenerateTargetsForPackage

let InstallPackages (nugets : RepositoryUrl list) =
    PaketInterface.UpdateSources nugets
    PaketInterface.PaketInstall ()
    GeneratePackageImports()

let RestorePackages () =
    PaketInterface.PaketRestore ()
    GeneratePackageImports()

let Update () =
    PaketInterface.PaketUpdate ()
    
    let allPackages = NuGets.BuildPackageDependencies (PaketInterface.ParsePaketDependencies ())
                      |> Map.toList
                      |> Seq.map fst
    allPackages |> Seq.iter GenerateTargetForPackageRef

let Outdated () =
    PaketInterface.PaketOutdated ()

let List () =
    PaketInterface.PaketInstalled ()

let RemoveUnusedPackages (antho : Anthology) =
    let packages = PaketInterface.ParsePaketDependencies ()
    let usedPackages = antho.Projects |> Set.map (fun x -> x.PackageReferences)
                                      |> Set.unionMany
    let packagesToRemove = packages |> Set.filter (fun x -> (not << Set.contains x) usedPackages)
    PaketInterface.RemoveDependencies packagesToRemove

let simplifyAnthologyWithPackages (antho) =
    let promotedPackageAntho = SimplifyAnthologyWithoutPackage antho

    let packages = promotedPackageAntho.Projects |> Set.map (fun x -> x.PackageReferences)
                                                 |> Set.unionMany
    let package2packages = NuGets.BuildPackageDependencies packages
    let allPackages = package2packages |> Seq.map (fun x -> x.Key) 
    let package2files = allPackages |> Seq.map (fun x -> (x, GatherAllAssemblies x)) 
                                    |> Map
    let newAntho = SimplifyAnthologyWithPackages antho package2files package2packages
    RemoveUnusedPackages newAntho
    newAntho

let Simplify (antho : Anthology) =
    InstallPackages antho.NuGets
    
    let newAntho = simplifyAnthologyWithPackages antho
    newAntho
