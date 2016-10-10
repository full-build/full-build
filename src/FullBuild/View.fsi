//module View
//
//open Graph
//open System.IO
//
////type View
//type View = class end
//with
//    member Name: string
//    member Filters: string seq
//    member Parameters: string seq
//    member Dependencies: bool
//    member ReferencedBy: bool
//    member Modified : bool
//    member Builder: BuilderType
//    member Projects: graph : Graph
//                  -> Project seq
//    member Solution: FileInfo
//    member Save: unit
//              -> unit
//
//val Load: name : string
//              -> View
//
//val Create: name : string
//         -> filters : string seq
//         -> parameters: string seq
//         -> dependencies : bool
//         -> referencedBy : bool
//         -> modified : bool
//         -> builder : BuilderType
//         -> View
//
//val Delete: name : string
//         -> unit
//
//val Views: unit
//        -> string seq
//
//val Default: unit
//          -> string option
