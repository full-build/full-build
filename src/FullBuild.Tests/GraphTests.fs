module GraphTests

open System
open System.IO
open System.Linq
open System.Xml.Linq
open ProjectParsing
open NUnit.Framework
open FsUnit
open Anthology
open StringHelpers
open MsBuildHelpers


[<Test>]
let ConvertToGraph () =
    let fileSimplified = FileInfo("anthology-simplified.yaml")
    let anthology = AnthologySerializer.Load fileSimplified

    let graph = Graph.toGraph anthology
    ()
