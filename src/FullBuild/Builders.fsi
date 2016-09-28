
module Builders

open Graph
open System.IO

val BuildWithBuilder: builder : BuilderType
                   -> viewFile : FileInfo
                   -> config : string
                   -> clean : bool
                   -> multithread : bool
                   -> version : string option
                   -> unit
