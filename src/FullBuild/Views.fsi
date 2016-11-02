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

module Views

open Graph
open Collections

type [<Sealed>] View = interface System.IComparable
with
    member Name: string
    member Filters: string set
    member UpReferences: bool
    member DownReferences: bool
    member Modified : bool
    member AppFilter : string option
    member Builder: BuilderType
    member Projects: Project set
    member Save: isDefault : bool option
                          -> unit
    member SaveStatic: unit
                    -> unit
    member Delete: unit
                -> unit

and [<Sealed>] Factory =
    member DefaultView : View option
    member Views : View set
    member CreateView: name : string
                    -> filters : string set
                    -> downReferences : bool
                    -> upReferences : bool
                    -> modified : bool
                    -> app : string option
                    -> builder : BuilderType
                    -> View

val from: graph : Graph
       -> Factory
