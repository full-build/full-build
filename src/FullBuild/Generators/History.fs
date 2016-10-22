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

module Generators.History

let private textHeader (version : string) =
    ()

let private textFooter () =
    ()

let private htmlHeader (version : string) =
    printfn "<html>"
    printfn "<body>"
    printfn "<h2>version %s</h2>" version

let private htmlFooter () =
    printfn "</body>"

let private textBody (repo : string) (content : string list) =
    IoHelpers.DisplayHighlight repo
    content |> Seq.iter (printfn "%s")

let private htmlBody (repo : string) (content : string list) =
    printfn "<b>%s</b><br>" repo
    content |> Seq.iter (printfn "%s<br>")
    printfn "<br>"

type HistoryType =
    | Html
    | Text


let Save (histType : HistoryType) (version : string) (revisions : (Graph.Repository*string list) seq) =
    let header, body, footer = match histType with
                               | HistoryType.Html -> htmlHeader, htmlBody, htmlFooter
                               | HistoryType.Text -> textHeader, textBody, textFooter

    header version
    revisions |> Seq.iter (fun x -> let repo = x |> fst
                                    let revision = x |> snd
                                    body repo.Name revision)
    footer ()
