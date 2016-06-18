﻿//   Copyright 2014-2016 Pierre Chalamet
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


let private generateItemGroupContent (pkgDir : DirectoryInfo) (files : FileInfo seq) =
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



let private generateItemGroupCopyContent (pkgDir : DirectoryInfo) (fxLibs : DirectoryInfo) =
    let relativePath = ComputeRelativeDirPath pkgDir fxLibs
    let files = sprintf "$(FBWorkspaceDir)/.full-build/packages/%s/**/*.*" relativePath
    let copyFiles = XElement(NsMsBuild + "FBCopyFiles",
                        XAttribute(NsNone + "Include", files))
    copyFiles        



let private generateItemGroup (fxLibs : DirectoryInfo) (condition : string) =
    let pkgDir = Env.GetFolder Env.Package
    let dlls = fxLibs.EnumerateFiles("*.dll")
    let exes = fxLibs.EnumerateFiles("*.exes")
    let files = Seq.append dlls exes
    let itemGroup = generateItemGroupContent pkgDir files
    XElement(NsMsBuild + "When",
        XAttribute(NsNone + "Condition", condition),
            XElement(NsMsBuild + "ItemGroup", 
                itemGroup))


let private generateItemGroupCopy (fxLibs : DirectoryInfo) (condition : string) =
    let pkgDir = Env.GetFolder Env.Package
    let itemGroup = generateItemGroupCopyContent pkgDir fxLibs
    XElement(NsMsBuild + "When",
        XAttribute(NsNone + "Condition", condition),
            XElement(NsMsBuild + "ItemGroup", 
                itemGroup))


let private generateChooseRefContent (libDir : DirectoryInfo) (package : PackageId) =
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
                yield generateItemGroup pathLib whenCondition
    }

    seq {
        if whens.Any() then
            yield XElement (NsMsBuild + "Choose", whens)
    }



let private generateChooseCopyContent (libDir : DirectoryInfo) (package : PackageId) =
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
                yield generateItemGroupCopy pathLib whenCondition
    }

    seq {
        if whens.Any() then
            yield XElement (NsMsBuild + "Choose", whens)
    }





    
let private generateDependenciesRefContent (dependencies : PackageId seq) =
    seq {
        for dependency in dependencies do
            let defineName = PackagePropertyName dependency
            let condition = sprintf "'$(%s)' == ''" defineName

            let depId = dependency.toString
            let dependencyTargets = sprintf "%s%s/package.targets" MSBUILD_PACKAGE_FOLDER depId
            yield XElement(NsMsBuild + "Import",
                      XAttribute(NsNone + "Project", dependencyTargets),
                      XAttribute(NsNone + "Condition", condition))
    }



let private generateDependenciesCopyContent (dependencies : PackageId seq) =
    seq {
        for dependency in dependencies do
            let defineName = PackagePropertyName dependency
            let condition = sprintf "'$(%sCopy)' == ''" defineName

            let depId = dependency.toString
            let dependencyTargets = sprintf "%s%s/package-copy.targets" MSBUILD_PACKAGE_FOLDER depId
    
            yield XElement(NsMsBuild + "Import",
                      XAttribute(NsNone + "Project", dependencyTargets),
                      XAttribute(NsNone + "Condition", condition))
    }




let private generateProjectRefContent (package : PackageId) (imports : XElement seq) (choose : XElement seq) =
    let defineName = PackagePropertyName package
    let propCondition = sprintf "'$(%s)' == ''" defineName
    let project = XElement (NsMsBuild + "Project",
                    XAttribute (NsNone + "Condition", propCondition),
                    XElement (NsMsBuild + "PropertyGroup",
                        XElement (NsMsBuild + defineName, "Y")),
                    imports,
                    choose)
    project


let private generateProjectCopyContent (package : PackageId) (imports : XElement seq) (choose : XElement seq) =
    let defineName = sprintf "%sCopy" (PackagePropertyName package)
    let propCondition = sprintf "'$(%s)' == ''" defineName
    let project = XElement (NsMsBuild + "Project",
                    XAttribute (NsNone + "Condition", propCondition),
                    XElement (NsMsBuild + "PropertyGroup",
                        XElement (NsMsBuild + defineName, "Y")),
                    imports,
                    choose)
    project



let private generateTargetForPackageRef (package : PackageId) =
    let pkgsDir = Env.GetFolder Env.Package
    let pkgDir = pkgsDir |> GetSubDirectory (package.toString)
    let libDir = pkgDir |> GetSubDirectory "lib" 
    
    let nuspecFile = pkgDir |> GetFile (IoHelpers.AddExt NuSpec (package.toString))
    let xnuspec = XDocument.Load (nuspecFile.FullName)
    let dependencies = NuGets.GetPackageDependencies xnuspec

    let imports = generateDependenciesRefContent dependencies
    let choose = generateChooseRefContent libDir package
    let project = generateProjectRefContent package imports choose

    let targetFile = pkgDir |> GetFile "package.targets" 
    project.Save (targetFile.FullName)

let private generateTargetForPackageCopy (package : PackageId) =
    let pkgsDir = Env.GetFolder Env.Package
    let pkgDir = pkgsDir |> GetSubDirectory (package.toString)
    let libDir = pkgDir |> GetSubDirectory "lib" 
    
    let nuspecFile = pkgDir |> GetFile (IoHelpers.AddExt NuSpec (package.toString))
    let xnuspec = XDocument.Load (nuspecFile.FullName)
    let dependencies = NuGets.GetPackageDependencies xnuspec

    let imports = generateDependenciesCopyContent dependencies
    let choose = generateChooseCopyContent libDir package
    let project = generateProjectCopyContent package imports choose

    let targetFile = pkgDir |> GetFile "package-copy.targets" 
    project.Save (targetFile.FullName)


let private generateTargetsForPackage (package : PackageId) =
    generateTargetForPackageRef package
    generateTargetForPackageCopy package



let private gatherAllAssemblies (package : PackageId) : AssemblyId set =
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


let private generatePackageImports () =
    PaketInterface.ParsePaketDependencies ()
        |> NuGets.BuildPackageDependencies
        |> Map.toList
        |> Seq.map fst
        |> Seq.iter generateTargetsForPackage

let private installPackages (nugets : RepositoryUrl list) =
    PaketInterface.UpdateSources nugets
    PaketInterface.PaketInstall ()
    generatePackageImports()

let RestorePackages () =
    PaketInterface.PaketRestore ()
    generatePackageImports()

let Update () =
    PaketInterface.PaketUpdate ()
    
    let allPackages = NuGets.BuildPackageDependencies (PaketInterface.ParsePaketDependencies ())
                      |> Map.toList
                      |> Seq.map fst
    allPackages |> Seq.iter generateTargetForPackageRef

let Outdated () =
    PaketInterface.PaketOutdated ()

let List () =
    PaketInterface.PaketInstalled ()

let private removeUnusedPackages (antho : Anthology) =
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
    let package2files = allPackages |> Seq.map (fun x -> (x, gatherAllAssemblies x)) 
                                    |> Map
    let newAntho = SimplifyAnthologyWithPackages antho package2files package2packages
    removeUnusedPackages newAntho
    newAntho

let Simplify (antho : Anthology) =
    installPackages antho.NuGets
    
    let newAntho = simplifyAnthologyWithPackages antho
    newAntho
