﻿// Copyright (c) 2014-2015, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
module Main

open CommandLineParsing

[<EntryPoint>]
let main argv = 
    let cmd = ParseCommandLine (argv |> Seq.toList)
    match cmd with
    // workspace
    | CreateWorkspace wsInfo -> Workspace.Create wsInfo.Path
    | InitWorkspace wsInfo -> Workspace.Init wsInfo.Path
    | IndexWorkspace -> Workspace.Index ()
    | ConvertWorkspace -> Workspace.Convert ()

    // repository
    | AddRepository repo -> Repo.Add repo
    | CloneRepositories repoInfo -> Repo.Clone repoInfo.Filters
    | ListRepositories -> Repo.List ()

    // view
    | CreateView viewInfo -> View.Create viewInfo.Name viewInfo.Filters
    | DropView viewInfo -> View.Drop viewInfo.Name
    | ListViews -> View.List ()
    | DescribeView viewInfo -> View.Describe viewInfo.Name
    | GenerateView viewInfo -> View.Generate viewInfo.Name
    | GraphView viewInfo -> View.Graph viewInfo.Name

    // package
    | InstallPackages -> Package.Install ()
    | SimplifyPackages -> Package.Simplify ()
    | UpdatePackages -> Package.Update ()
    | ListPackages -> Package.List ()

    // misc
    | Usage -> DisplayUsage ()
    | Error -> DisplayUsage ()
//    | GraphView {Name=vwName} -> FullBuild.Commands.Views.Views.Graph (vwName)
//    | RefreshWorkspace -> FullBuild.Commands.Workspace.Workspace.RefreshWorkspace ()
//    | OptimizeWorkspace -> FullBuild.Commands.Workspace.Workspace.Optimize ()
//    | BookmarkWorkspace -> FullBuild.Commands.Workspace.Workspace.Bookmark ()
//    | CheckoutWorkspace {Version=wsVersion} -> FullBuild.Commands.Workspace.Workspace.CheckoutBookmark (wsVersion)
//    | AddNuGet {Url=url} -> FullBuild.Commands.Packages.Packages.AddNuGet(url)
//    | ListNuGets -> FullBuild.Commands.Packages.Packages.ListNuGets ()
//    | ListPackages -> FullBuild.Commands.Binaries.Binaries.List ()
//    | InstallPackages -> FullBuild.Commands.Packages.Packages.InstallAll ()
//    | UpgradePackages -> FullBuild.Commands.Packages.Packages.UpgradePackages ()
//    | UsePackage {Id=pkgName; Version=pkgVersion} -> FullBuild.Commands.Packages.Packages.UsePackage (pkgName, pkgVersion)
//    | CheckPackages -> FullBuild.Commands.Packages.Packages.CheckPackages ()
//    | InitView {Name=vwName; Filters=vwFilter} -> FullBuild.Commands.Views.Views.Init (vwName, vwFilter |> Seq.toArray)
//    | DropView {Name=vwName} -> FullBuild.Commands.Views.Views.Delete (vwName)
//    | ListViews -> FullBuild.Commands.Views.Views.List ()
//    | DescribeView {Name=vwName} -> FullBuild.Commands.Views.Views.Describe (vwName)
//    | GraphView {Name=vwName} -> FullBuild.Commands.Views.Views.Graph (vwName)
//    | GenerateView {Name=vwName} -> FullBuild.Commands.Views.Views.Generate (vwName)
//    | BuildView {Name=vwName} -> FullBuild.Commands.Views.Views.BuildView (vwName)
//    | RefreshSources -> FullBuild.Commands.Workspace.Workspace.RefreshSources ()
//    | ListBinaries -> FullBuild.Commands.Binaries.Binaries.List ()

    let retCode = if cmd = Error then 5
                  else 0
    retCode
