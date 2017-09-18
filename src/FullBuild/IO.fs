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

module IO

open System

type Result =
    { Code: int
      Info : string
      Out: string list
      Error: string list }

let private resultToError execResult =
    if execResult.Code <> 0 then 
        sprintf "Operation '%s' failed with error %d" execResult.Info execResult.Code 
            :: execResult.Out 
            @ execResult.Error
            |> String.concat Environment.NewLine
            |> Some
    else None

let GetOutput execResult =
    match execResult |> resultToError with
    | Some error -> failwith error
    | None -> execResult.Out

let CheckResponseCode execResult =
    match execResult |> resultToError with
    | Some error -> failwith error
    | None -> ()

let AndThen f execResult =
    match execResult |> resultToError with
    | Some _ -> execResult
    | None -> f()

let CheckMultipleResponseCode execResults =
    let errors = execResults |> Seq.choose (fun execResult -> execResult |> resultToError)
    if errors |> Seq.isEmpty |> not then
        errors |> String.concat System.Environment.NewLine |> failwith
  