// Copyright (c) 2014-2015, Pierre Chalamet
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
module CommandLineToken

type Token = 
    | Workspace
    | View
    | Help
    | Create
    | Init
    | Clone
    | Repo
    | Package
    | Update
    | Index
    | Drop
    | Build
    | Convert
    | Add
    | List
    | Bookmark
    | Checkout
    | NuGet
    | Graph
    | Check
    | Describe
    | Generate
    | Install
    | Simplify
    | Outdated
    | Debug
    | Application
    | Deploy
    | Rebase
    | Unknown

let (|Token|) (token : string) = 
    match token with
    | "workspace" -> Workspace
    | "view" -> View
    | "help" -> Help
    | "create" -> Create
    | "init" -> Init
    | "clone" -> Clone
    | "repo" -> Repo
    | "package" -> Package
    | "update" -> Update
    | "index" -> Index
    | "drop" -> Drop
    | "build" -> Build
    | "convert" -> Convert
    | "add" -> Add
    | "list" -> List
    | "bookmark" -> Bookmark
    | "checkout" -> Checkout
    | "nuget" -> NuGet
    | "graph" -> Graph
    | "check" -> Check
    | "describe" -> Describe
    | "generate" -> Generate
    | "install" -> Install
    | "simplify" -> Simplify
    | "outdated" -> Outdated
    | "debug" -> Debug
    | "app" -> Application
    | "deploy" -> Deploy
    | "rebase" -> Rebase
    | _ -> Unknown

