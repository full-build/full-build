﻿//   Copyright 2014-2016 Pierre Chalamet
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

module Configuration

open Anthology
open Env

type WorkspaceConfiguration = 
    { Repositories : Repository list }

let LoadAnthology() : Anthology = 
    let anthoFn = GetAnthologyFileName ()
    AnthologySerializer.Load anthoFn

let SaveAnthology  = 
    let anthoFn = GetAnthologyFileName ()
    AnthologySerializer.Save anthoFn

let LoadBaseline() : Baseline =
    let baselineFile = GetBaselineFileName ()
    BaselineSerializer.Load baselineFile

let SaveBaseline =
    let baselineFile = GetBaselineFileName ()
    BaselineSerializer.Save baselineFile

let LoadView (viewId :ViewId) : View =
    let viewFile = GetViewFileName viewId.toString 
    if not viewFile.Exists then failwithf "View %A does not exist" viewId.toString
    ViewSerializer.Load viewFile

let SaveView (viewId : ViewId) =
    let viewFile = GetViewFileName viewId.toString 
    ViewSerializer.Save viewFile

let ViewExists (viewId : ViewId) =
    let viewFile = GetViewFileName viewId.toString 
    viewFile.Exists
    


let Migrate () =
//    let antho = LoadAnthology ()
//    
//    let toProjectRef (id : ProjectId) =
//        let guid = id.toString |> ParseGuid |> ProjectUniqueId.from
//        let project = antho.Projects |> Seq.find (fun x -> x.UniqueProjectId = guid)
//        project.Output.toString |> ProjectId.from
//
//    let newAntho = { antho
//                     with Applications = antho.Applications |> Set.map (fun x -> { x with Projects = x.Projects |> Set.map toProjectRef })
//                          Projects = antho.Projects |> Set.map (fun x -> { x with ProjectReferences = x.ProjectReferences |> Set.map toProjectRef })
//                    }
//
//    SaveAnthology newAntho
    ()