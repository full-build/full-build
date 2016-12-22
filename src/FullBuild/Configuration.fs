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

module Configuration

open Anthology
open Env

type WorkspaceConfiguration = 
    { Repositories : Repository list }

let LoadAnthology() : Anthology = 
    let anthoFn = GetAnthologyFile ()
    AnthologySerializer.Load anthoFn

let SaveAnthology  = 
    let anthoFn = GetAnthologyFile ()
    AnthologySerializer.Save anthoFn

let LoadView (viewId :ViewId) : View =
    let viewFile = GetViewFile viewId.toString 
    if not viewFile.Exists then failwithf "View %A does not exist" viewId.toString
    ViewSerializer.Load viewFile

let DefaultView () : ViewId option =
    let vwFolder = Env.GetFolder Env.Folder.View
    let defaultFile = vwFolder |> IoHelpers.GetFile "default"
    if defaultFile.Exists then
        let viewName = System.IO.File.ReadAllText(defaultFile.FullName)
        Some (Anthology.ViewId viewName)
    else    
        None

let DeleteDefaultView() =
    let vwFolder = Env.GetFolder Env.Folder.View
    let defaultFile = vwFolder |> IoHelpers.GetFile "default"
    if defaultFile.Exists then
        defaultFile.Delete()

let DeleteView (viewId : ViewId) =
    let vwDir = Env.GetFolder Env.Folder.View
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let viewFile = vwDir |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.View viewId.toString)
    let targetFile = vwDir |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.Targets viewId.toString)
    let slnFile =  wsDir |> IoHelpers.GetFile (IoHelpers.AddExt IoHelpers.Extension.Solution viewId.toString)
    let defaultFile = vwDir |> IoHelpers.GetFile "default"
    if viewFile.Exists then viewFile.Delete()
    if targetFile.Exists then targetFile.Delete()
    if slnFile.Exists then slnFile.Delete()
    if DefaultView() = Some viewId && defaultFile.Exists then
        defaultFile.Delete()

let private setDefaultView (viewId : ViewId) =
    let vwFolder = Env.GetFolder Env.Folder.View
    let defaultFile = vwFolder |> IoHelpers.GetFile "default"
    System.IO.File.WriteAllText (defaultFile.FullName, viewId.toString)

let ViewExistsAndNotCorrupted viewName =
    [|viewName |> Env.GetSolutionFile; 
      viewName |> Env.GetSolutionDefinesFile; 
      viewName |> Env.GetViewFile|]
        |> Array.forall(fun x->x.Exists)
let SaveView (viewId : ViewId) view (isDefault : bool option) =
    let viewFile = GetViewFile viewId.toString
    ViewSerializer.Save viewFile view
    match isDefault with
    | None -> ()
    | Some false -> if DefaultView () = Some viewId then DeleteDefaultView()
    | Some true -> setDefaultView viewId

let ViewExists viewName =
    let viewFile = GetViewFile viewName 
    viewFile.Exists

let LoadBranch () : string =
    let file = Env.GetBranchFile ()
    let branch = System.IO.File.ReadAllText(file.FullName)
    branch

let SaveBranch (branch : string) : unit =
    let file = Env.GetBranchFile ()
    System.IO.File.WriteAllText(file.FullName, branch)
