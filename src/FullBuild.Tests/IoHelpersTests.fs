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

module FileExtensionsTests

open System
open System.IO
open IoHelpers
open NUnit.Framework
open FsUnit

[<Test>]
let CheckGetSubDirectory () =
    let currDir = IoHelpers.CurrentFolder ()
    let subdir = currDir |> GetSubDirectory "toto"
    subdir.FullName.Contains("toto") |> should equal true

[<Test>]
let CheckCreateSubDirectory () =
    let tmpDir = DirectoryInfo (Path.GetTempPath())
    let dirName = Guid.NewGuid().ToString("D")
    let dir = tmpDir |> CreateSubDirectory dirName
    dir.Exists |> should equal true

[<Test>]
let CheckGetFile () =
    let currDir = IoHelpers.CurrentFolder ()
    let file = currDir |> GetFile "toto" 
    file.FullName.Contains("toto") |> should equal true

[<Test>]
let CheckAddExt () =
    let file = AddExt Targets "toto"
    file |> should equal "toto.targets"

[<Test>]
let CheckRelativeHops () =
    let res = IoHelpers.ComputeHops "toto/tutu/pouet.csproj"
    res |> should equal "../../"
