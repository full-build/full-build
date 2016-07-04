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

module CommandLineToken

type TokenOption =
    | Debug
    | All
    | Bin
    | Src
    | Exclude
    | Multithread
    | Shallow
    | Default
    | Branch
    | Version
    | Rebase
    | Reset
    | View
    | Modified
    | Html

let (|TokenOption|_|) (token : string) =
    match token with
    | "--debug" -> Some TokenOption.Debug
    | "--all" -> Some TokenOption.All
    | "--bin" -> Some TokenOption.Bin
    | "--src" -> Some TokenOption.Src
    | "--exclude" -> Some TokenOption.Exclude
    | "--mt" -> Some TokenOption.Multithread
    | "--shallow" -> Some TokenOption.Shallow
    | "--default" -> Some TokenOption.Default
    | "--branch" -> Some TokenOption.Branch
    | "--version" -> Some TokenOption.Version
    | "--rebase" -> Some TokenOption.Rebase
    | "--reset" -> Some TokenOption.Reset
    | "--view" -> Some TokenOption.View
    | "--modified" -> Some TokenOption.Modified
    | "--html" -> Some TokenOption.Html
    | _ -> None


type Token = 
    | Version
    | Workspace
    | Help
    | Upgrade
    | Setup
    | Init
    | Clone
    | Update
    | Build
    | Rebuild
    | Index
    | Convert
    | Push
    | Graph
    | Install
    | Simplify
    | Outdated
    | Publish
    | Pull
    | Checkout
    | Branch
    | Exec
    | Test
    | Alter
    | Open
    | Bind
    | History

    | Add
    | Drop
    | List
    | Describe

    | View
    | Repo
    | Package
    | NuGet
    | App

    | Clean
    | UpdateGuids
    | Migrate


let (|Token|_|) (token : string) = 
    match token with
    | "version" -> Some Version
    | "workspace" -> Some Workspace

    | "help" -> Some Help
    | "upgrade" -> Some Upgrade
    | "setup" -> Some Setup
    | "init" -> Some Init
    | "clone" -> Some Clone
    | "update" -> Some Update
    | "build" -> Some Build
    | "rebuild" -> Some Rebuild
    | "index" -> Some Index
    | "convert" -> Some Convert
    | "push" -> Some Push
    | "graph" -> Some Graph
    | "install" -> Some Install
    | "outdated" -> Some Outdated
    | "publish" -> Some Publish
    | "pull" -> Some Pull
    | "checkout" -> Some Checkout
    | "branch" -> Some Branch
    | "exec" -> Some Exec
    | "clean" -> Some Clean
    | "test" -> Some Test
    | "alter" -> Some Alter
    | "open" -> Some Open
    | "bind" -> Some Bind
    | "history" -> Some History

    | "add" -> Some Add
    | "drop" -> Some Drop
    | "list" -> Some List
    | "describe" -> Some Describe

    | "view" -> Some View
    | "repo" -> Some Repo
    | "package" -> Some Package
    | "nuget" -> Some NuGet
    | "app" -> Some App

    | "update-guids" -> Some UpdateGuids
    | "migrate" -> Some Migrate
    | _ -> None
