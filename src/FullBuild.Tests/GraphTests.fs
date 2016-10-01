module GraphTests

open System
open System.IO
open NUnit.Framework
open FsUnit
open Anthology
open Graph
open TestHelpers


[<Test>]
let ConvertToGraph () =
    let fileSimplified = FileInfo(testFile "anthology-graph.yaml")
    let anthology = AnthologySerializer.Load fileSimplified

    let graph = Graph.from anthology

    let apps = graph.Applications |> Seq.map (fun x -> x.Name) |> set
    let expectedApps = [ "cassandrasharp-log4net"
                         "cqlplus" ] |> set
    apps |> should equal expectedApps

    let repos = graph.Repositories |> Seq.map (fun x -> x.Name, x.Projects() |> Seq.map (fun x -> x.ProjectId) |> set) |> set
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
    let cassandrasharpReferencies = cassandrasharpProject.References() |> Seq.map (fun x -> x.ProjectId) |> set
    let expectedDependencies = [ "cassandrasharp.interfaces"] |> set
    cassandrasharpReferencies |> should equal expectedDependencies

    let cassandrasharpReferencedBy = cassandrasharpProject.ReferencedBy() |> Seq.map (fun x -> x.ProjectId) |> set
    let expectedReferencedBy = [ "cqlplus"
                                 "cassandrasharpunittests" 
                                 "samples"
                                 "cassandrasharp.contrib.log4netunittests" ] |> set
    cassandrasharpReferencedBy |> should equal expectedReferencedBy

