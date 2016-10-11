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

module Commands.Workspace


val Create: CLI.Commands.SetupWorkspace
         -> unit

val Push: CLI.Commands.PushWorkspace
       -> unit

val Checkout: CLI.Commands.CheckoutVersion
           -> unit

val Branch: CLI.Commands.BranchWorkspace
         -> unit

val Install: unit
          -> unit

val Pull: CLI.Commands.PullWorkspace
       -> unit

val Init: CLI.Commands.InitWorkspace
       -> unit

val Exec: CLI.Commands.Exec
       -> unit

val Clean: unit
        -> unit

val UpdateGuid: updInfo : CLI.Commands.UpdateGuids
             -> unit

val History: CLI.Commands.History
          -> unit

val Index: CLI.Commands.IndexRepositories
        -> unit

val Convert: CLI.Commands.ConvertRepositories
          -> unit
