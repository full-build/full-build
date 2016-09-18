module GraphTests

open System
open System.IO
open NUnit.Framework
open FsUnit
open Anthology


[<Test>]
let ConvertToGraph () =
    let fileSimplified = FileInfo("anthology-graph.yaml")
    let anthology = AnthologySerializer.Load fileSimplified

    let graph = Graph.toGraph anthology

    let apps = graph.Applications |> Set.map (fun x -> x.UnderlyingApplication.Name)
    let expectedApps = [ ApplicationId.from "cassandrasharp-log4net"
                         ApplicationId.from "cqlplus" ] |> set
    apps |> should equal expectedApps

    let repos = graph.Repositories |> Set.map (fun x -> x.UnderlyingRepository.Repository.Name, x.Projects |> Set.map (fun x -> x.UnderlyingProject.ProjectId))
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

    let cassandrasharpunittestsProject = graph.Projects |> Seq.find (fun x -> x.UnderlyingProject.ProjectId = ProjectId.from "cassandrasharpunittests")
    let apachecassandraProject = graph.Projects |> Seq.find (fun x -> x.UnderlyingProject.ProjectId = ProjectId.from "apache.cassandra")
    let cassandrasharpProject = graph.Projects |> Seq.find (fun x -> x.UnderlyingProject.ProjectId = ProjectId.from "cassandrasharp")
    let cassandrasharpinterfacesProject = graph.Projects |> Seq.find (fun x -> x.UnderlyingProject.ProjectId = ProjectId.from "cassandrasharp.interfaces")
    let thriftProject = graph.Projects |> Seq.find (fun x -> x.UnderlyingProject.ProjectId = ProjectId.from "thrift")
    let cassandrasharpcontriblog4netProject = graph.Projects |> Seq.find (fun x -> x.UnderlyingProject.ProjectId = ProjectId.from "cassandrasharp.contrib.log4net")
    let cassandrasharpcontriblog4netunittestsProject = graph.Projects |> Seq.find (fun x -> x.UnderlyingProject.ProjectId = ProjectId.from "cassandrasharp.contrib.log4netunittests")

    cassandrasharpunittestsProject.ProjectReferences |> should equal ([ apachecassandraProject
                                                                        cassandrasharpProject
                                                                        cassandrasharpinterfacesProject
                                                                        thriftProject ] |> set)
    apachecassandraProject.ProjectReferences |> should equal ([ thriftProject ] |> set)
    thriftProject.ProjectReferences |> should equal Set.empty
    cassandrasharpProject.ProjectReferences |> should equal ([ cassandrasharpinterfacesProject ] |> set)
    cassandrasharpcontriblog4netProject.ProjectReferences |> should equal ([ cassandrasharpinterfacesProject ] |> set)
    cassandrasharpcontriblog4netunittestsProject.ProjectReferences |> should equal ([ cassandrasharpProject 
                                                                                      cassandrasharpinterfacesProject ] |> set)

