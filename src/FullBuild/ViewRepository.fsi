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

module ViewRepository

open Graph
open Collections

type [<Sealed>] View = interface System.IComparable
with
    member Name: string
    member Filters: string set
    member Parameters: string set
    member References: bool
    member ReferencedBy: bool
    member Modified : bool
    member Builder: BuilderType
    member Projects: Project set
    member Save: isDefault : bool option
              -> unit
    member Delete: unit
                -> unit

and [<Sealed>] ViewRepository =
    member DefaultView : View option
    member Views : View set
    member CreateView: name : string
                    -> filters : string set
                    -> parameters: string set
                    -> dependencies : bool
                    -> referencedBy : bool
                    -> modified : bool
                    -> builder : BuilderType
                    -> View

val from: graph : Graph
       -> ViewRepository
