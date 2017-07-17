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

module Baselines

open Collections
open Graph

[<RequireQualifiedAccess; Sealed>]
type BuildInfo =
    member Branch: string

    member Version: string

    member Format: unit
                -> string

    static member Parse: string
                      -> BuildInfo

type [<Sealed>] Bookmark = interface System.IComparable
with
    member Repository : Repository
    member Version : string

and [<Sealed>] Baseline = interface System.IComparable
with
    member Info: BuildInfo

    member Bookmarks: Bookmark set

    static member (-): Baseline*Baseline
                    -> Bookmark set


and [<Sealed>] Factory =
    //The baseline of the current content of "bin" folder (pulled baseline += built)
    member GetBaseline: unit 
                      -> Baseline option
    
    //The baseline of the sources (pulled baseline += cloned repos)
    member GetSourcesBaseline: unit 
                      -> Baseline

    //Create temp baseline (pulled baseline += cloned), should take the repos list as param(or view)
    member UpdateBaseline: buildNumber: string
                        -> unit
    
    member FindMatchingBuildInfo: unit
                            -> BuildInfo option

    member TagMasterRepository: string
                            -> unit
val from: graph : Graph
       -> Factory
