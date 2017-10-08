//   Copyright 2014-2017 Pierre Chalamet
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

module Core.TestRunners
open Env
open Collections
open Graph
open Exec
open FsHelpers


let TestSolution (view : Views.View) =
    // http://codito.in/how-dotnet-test-works/
    let wsDir = GetFolder Env.Folder.Workspace
    let viewFile = wsDir |> GetFile (view.Name |> AddExt Extension.Solution)
    let args = sprintf "test %A --configuration %s --no-build --no-restore /p:SolutionDir=%A /p:SolutionName=%A" 
                       viewFile.FullName view.Configuration wsDir.FullName view.Name
    Exec "dotnet" args wsDir Map.empty |> IO.CheckResponseCode
