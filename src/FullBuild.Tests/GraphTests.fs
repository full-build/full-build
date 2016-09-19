module GraphTests

open System
open System.IO
open NUnit.Framework
open FsUnit
open Anthology
open Graph


[<Test>]
let ConvertToGraph () =
    let fileSimplified = FileInfo("anthology-graph.yaml")
    let anthology = AnthologySerializer.Load fileSimplified

    let graph = Graph.from anthology

    let apps = graph.Applications() |> Seq.map (fun x -> x.UnderlyingApplication().Name) |> set
    let expectedApps = [ ApplicationId.from "cassandrasharp-log4net"
                         ApplicationId.from "cqlplus" ] |> set
    apps |> should equal expectedApps

    let repos = graph.Repositories() |> Seq.map (fun x -> x.UnderlyingRepository() .Repository.Name, x.Projects() |> Seq.map (fun x -> x.UnderlyingProject().ProjectId) |> set) |> set
    let expectedRepos = [ RepositoryId.from "cassandra-sharp", [ ProjectId.from "apache.cassandra"
                                                                 ProjectId.from "cassandrasharp.interfaces"
                                                                 ProjectId.from "cassandrasharp"
                                                                 ProjectId.from "cassandrasharpunittests"
                                                                 ProjectId.from "samples"
                                                                 ProjectId.from "thrift"
                                                                 ProjectId.from "cqlplus" ] |> set
                          RepositoryId.from "cassandra-sharp-contrib", [ ProjectId.from "cassandrasharp.contrib.log4net"
                                                                         ProjectId.from "cassandrasharp.contrib.log4netunittests" ] |> set ] |> set
    repos |> should equal expectedRepos


    let projects = graph.Projects()
    let cassandrasharpProject = projects |> Seq.find (fun x -> x.UnderlyingProject().ProjectId = ProjectId.from "cassandrasharp")
    let cassandrasharpReferencies = cassandrasharpProject.References() |> Seq.map (fun x -> x.UnderlyingProject().ProjectId) |> set
    let expectedDependencies = [ ProjectId.from "cassandrasharp.interfaces"] |> set
    cassandrasharpReferencies |> should equal expectedDependencies

    let cassandrasharpReferencedBy = cassandrasharpProject.ReferencedBy() |> Seq.map (fun x -> x.UnderlyingProject().ProjectId) |> set
    let expectedReferencedBy = [ ProjectId.from "cqlplus"
                                 ProjectId.from "cassandrasharpunittests" 
                                 ProjectId.from "samples"
                                 ProjectId.from "cassandrasharp.contrib.log4netunittests" ] |> set
    cassandrasharpReferencedBy |> should equal expectedReferencedBy

