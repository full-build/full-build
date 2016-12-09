//   Copyright 2014-2016 Pierre Chalamet
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

type [<Sealed>] Bookmark = interface System.IComparable
with
    member Repository : Repository
    member Version : string

and [<Sealed>] Baseline = interface System.IComparable
with
    member IsIncremental: bool
    member BuildNumber: string
    member Bookmarks: Bookmark set

    static member (-): Baseline*Baseline
                    -> Bookmark set
    member Save: unit
              -> unit


and [<Sealed>] Factory =
    member Baseline : Baseline
    member CreateBaseline: incremental : bool
                        -> buildNumber : string
                        -> Baseline

val from: graph : Graph
       -> Factory
