module FrameworkDetection

open NUnit.Framework
open FsUnit
open Collections
open Paket

[<Test>]
let Parse () =
    FrameworkDetection.Extract(".NETFramework2.0") |> should equal (Some (DotNetFramework FrameworkVersion.V2))
    FrameworkDetection.Extract(".NETFramework3.5") |> should equal (Some (DotNetFramework FrameworkVersion.V3_5))
    FrameworkDetection.Extract(".NETFramework4.0") |> should equal (Some (DotNetFramework FrameworkVersion.V4_Client))
    FrameworkDetection.Extract(".NETFramework4.5") |> should equal (Some (DotNetFramework FrameworkVersion.V4_5))
    FrameworkDetection.Extract(".NETStandard1.3") |> should equal (Some (DotNetStandard DotNetStandardVersion.V1_3))

