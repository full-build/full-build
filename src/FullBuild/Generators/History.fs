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

module Generators.History


type HistoryType =
    | Html
    | Text


let writeText (revisions : (Graph.Repository * string list) seq) =
    seq {
        for (repo, rev) in revisions do
            yield sprintf "==> %s" repo.Name
            yield! rev |> List.map (sprintf "%s")
    }


let writeHtml (revisions : (Graph.Repository * string list) seq) =
    seq {
        yield "<html>"
        yield "<body>"
        for (repo, rev) in revisions do
            yield sprintf "<b>%s</b><br>" repo.Name
            yield! rev |> List.map (sprintf "%s<br>")
            yield "<br>"
        yield "</body>"
    }


let Save (histType : HistoryType) (revisions : (Graph.Repository*string list) seq) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let lines, ext = match histType with
                     | HistoryType.Text -> writeText revisions, FsHelpers.Extension.Text
                     | HistoryType.Html -> writeHtml revisions, FsHelpers.Extension.Html
    let historyFile = wsDir |> FsHelpers.GetFile ("history" |> FsHelpers.AddExt ext)
    System.IO.File.WriteAllLines(historyFile.FullName, lines)

    // print out changes
    for (repo, rev) in revisions do
        ConHelpers.DisplayInfo repo.Name
        rev |> List.iter (printfn "%s")
