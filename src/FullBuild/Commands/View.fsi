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

module Commands.View

val Add: cmd : CLI.Commands.AddView
      -> unit

val Drop: name : string
       -> unit

val List: unit
       -> unit

val Describe: name : string
           -> unit

val Graph: cmd : CLI.Commands.GraphView
        -> unit

val Build: cmd : CLI.Commands.BuildView
        -> unit

val Alter: cmd : CLI.Commands.AlterView
        -> unit

val Open: cmd : CLI.Commands.OpenView
      -> unit
