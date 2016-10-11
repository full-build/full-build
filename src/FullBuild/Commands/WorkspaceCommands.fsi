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

module WorkspaceCommands


val Create: Commands.SetupWorkspace
         -> unit

val Push: Commands.PushWorkspace
       -> unit

val Checkout: Commands.CheckoutVersion
           -> unit

val Branch: Commands.BranchWorkspace
         -> unit

val Install: unit
          -> unit

val Pull: Commands.PullWorkspace
       -> unit

val Init: Commands.InitWorkspace
       -> unit

val Exec: Commands.Exec
       -> unit

val Clean: unit
        -> unit

val UpdateGuid: repositoryId : string
             -> unit

val History: Commands.History
          -> unit

val Index: Commands.IndexRepositories
        -> unit

val Convert: Commands.ConvertRepositories
          -> unit
