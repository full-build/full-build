module FrameworkDetection

open Paket

let FromFx profile version identifier =
    //currently only .netframework
    match version with
    | Some "v1.0" -> DotNetFramework FrameworkVersion.V1 |> Some
    | Some "v1.1" -> DotNetFramework FrameworkVersion.V1_1 |> Some
    | Some "v2.0" -> DotNetFramework FrameworkVersion.V2 |> Some
    | Some "v3.0" -> DotNetFramework FrameworkVersion.V3 |> Some
    | Some "v3.5" -> DotNetFramework FrameworkVersion.V3_5 |> Some
    | Some "v4.0" -> DotNetFramework FrameworkVersion.V4 |> Some
    | Some "v4.5" -> DotNetFramework FrameworkVersion.V4_5 |> Some
    | Some "v4.5.1" -> DotNetFramework FrameworkVersion.V4_5_1 |> Some
    | Some "v4.5.2" -> DotNetFramework FrameworkVersion.V4_5_2 |> Some
    | Some "v4.5.3" -> DotNetFramework FrameworkVersion.V4_5_3 |> Some
    | Some "v4.6" -> DotNetFramework FrameworkVersion.V4_6 |> Some
    | Some "v4.6.1" -> DotNetFramework FrameworkVersion.V4_6_1 |> Some
    | Some "v4.6.2" -> DotNetFramework FrameworkVersion.V4_6_2 |> Some
    | Some "v4.6.3" -> DotNetFramework FrameworkVersion.V4_6_3 |> Some
    | Some "v5.0" -> DotNetFramework FrameworkVersion.V5_0 |> Some
    | _ -> None