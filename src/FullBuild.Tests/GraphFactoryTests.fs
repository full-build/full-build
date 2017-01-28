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

module GraphFactoryTests

open System
open System.IO
open NUnit.Framework
open FsUnit
open Graph
open Collections
open TestHelpers




[<Test>]
let ConvertToGraph () =
    let artifactsFile = FileInfo(testFile "graph-artifacts.yaml")
    let projectsFile = FileInfo(testFile "graph-projects.yaml")
    let artifacts = ArtifactsSerializer.Load artifactsFile
    let projects = ProjectsSerializer.Load projectsFile
    let anthology = AnthologySerializer.Deserialize artifacts projects
    let graph = Graph.from anthology

    let apps = graph.Applications |> Seq.map (fun x -> x.Name) 
                                  |> Set.ofSeq
    let expectedApps = [ "cassandrasharp-log4net"
                         "cqlplus" ] |> set
    apps |> should equal expectedApps

    let repos = graph.Repositories |> Seq.map (fun x -> x.Name, x.Projects |> Seq.map (fun x -> x.ProjectId) |> set) 
                                   |> Set.ofSeq
    let expectedRepos = [ "cassandra-sharp", [ "apache.cassandra"
                                               "cassandrasharp.interfaces"
                                               "cassandrasharp"
                                               "cassandrasharpunittests"
                                               "samples"
                                               "thrift"
                                               "cqlplus" ] |> set
                          "cassandra-sharp-contrib", [ "cassandrasharp.contrib.log4net"
                                                       "cassandrasharp.contrib.log4netunittests" ] |> set ] |> set
    repos |> should equal expectedRepos


    let projects = graph.Projects
    let cassandrasharpProject = projects |> Seq.find (fun x -> x.ProjectId = "cassandrasharp")
    let cassandrasharpReferencies = cassandrasharpProject.References |> Seq.map (fun x -> x.ProjectId) |> set
    let expectedDependencies = [ "cassandrasharp.interfaces"] |> set
    cassandrasharpReferencies |> should equal expectedDependencies

    let cassandrasharpReferencedBy = cassandrasharpProject.ReferencedBy |> Seq.map (fun x -> x.ProjectId) |> set
    let expectedReferencedBy = [ "cqlplus"
                                 "cassandrasharpunittests" 
                                 "samples"
                                 "cassandrasharp.contrib.log4netunittests" ] |> set
    cassandrasharpReferencedBy |> should equal expectedReferencedBy
