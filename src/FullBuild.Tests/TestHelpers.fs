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

module TestHelpers
open Collections
open Graph

let testFile file = NUnit.Framework.TestContext.CurrentContext.TestDirectory + "/" + file

let selectProjects (selection : string list) (projects : Project set) =
    let mapProjects = projects |> Seq.map (fun x -> x.Output.Name, x) |> Map
    mapProjects |> Seq.filter (fun kvp -> selection |> List.contains kvp.Key)
                |> Seq.map (fun kvp -> kvp.Value)
                |> set
