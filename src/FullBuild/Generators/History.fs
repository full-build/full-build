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

module History

let textHeader (version : string) =
    ()

let textFooter () =
    ()

let htmlHeader (version : string) =
    printfn "<html>"
    printfn "<body>"
    printfn "<h2>version %s</h2>" version

let htmlFooter () =
    printfn "</body>"

let textBody (repo : string) (content : string) =
    IoHelpers.DisplayHighlight repo
    printfn "%s" content

let htmlBody (repo : string) (content : string) =
    printfn "<b>%s</b><br>" repo
    let htmlContent = content.Replace(System.Environment.NewLine, "<br>")
    printfn "%s<br><br>" htmlContent


