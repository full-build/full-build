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

module DgmlTests

open System.IO
open NUnit.Framework
open FsUnit
open StringHelpers
open Generators.Solution
open TestHelpers
open Collections
open System.Xml.Linq


[<Test>]
let CheckGenerateDgmlNoDependency () =
    let currFolder = TestContext.CurrentContext.TestDirectory
    try
        System.Environment.CurrentDirectory <- TestContext.CurrentContext.TestDirectory
        let expectedDgml = XDocument.Load(testFile "single-node.dgml")
        let file = FileInfo(testFile "anthology-view.yaml")
        let graph = AnthologySerializer.Load file |> Graph.from
        let viewRepository = Views.from graph
        let projects = graph.Projects
        let goal = projects |> selectProjects ["g"]

        let view = viewRepository.CreateView "test" (set ["*/g"]) false false false None false Graph.BuilderType.MSBuild
        let res = Generators.Dgml.GraphContent view.Projects true

        res.ToString() |> should equal (expectedDgml.ToString())
    finally
        System.Environment.CurrentDirectory <- currFolder

[<Test>]
let CheckGenerateDgmlWithDependencies () =
    let currFolder = TestContext.CurrentContext.TestDirectory
    try
        System.Environment.CurrentDirectory <- TestContext.CurrentContext.TestDirectory
        let expectedDgml = XDocument.Load(testFile "single-node-dependencies.dgml")
        let file = FileInfo(testFile "anthology-view.yaml")
        let graph = AnthologySerializer.Load file |> Graph.from
        let viewRepository = Views.from graph
        let projects = graph.Projects
        let goal = projects |> selectProjects ["g"]

        let view = viewRepository.CreateView "test" (set ["*/g"]) true false false None false Graph.BuilderType.MSBuild
        let res = Generators.Dgml.GraphContent view.Projects true

        res.ToString() |> should equal (expectedDgml.ToString())
    finally
        System.Environment.CurrentDirectory <- currFolder
