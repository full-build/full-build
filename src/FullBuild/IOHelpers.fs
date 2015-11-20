// Copyright (c) 2014-2015, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
module IoHelpers

open System.IO


type Extension =
    | View
    | Solution
    | Targets
    | CsProj
    | FsProj
    | VbProj
    | NuSpec
    | Dgml
    | App
    | Exe
    | Dll

let AddExt (ext : Extension) (fileName : string) : string =
    let sext = match ext with 
               | View -> "view"
               | Solution -> "sln"
               | Targets -> "targets"
               | CsProj -> "csproj"
               | FsProj -> "fsproj"
               | VbProj -> "vbproj"
               | NuSpec -> "nuspec"
               | Dgml -> "dgml"
               | App -> "app"
               | Exe -> "exe"
               | Dll -> "dll"
    sprintf "%s.%s" fileName sext

let ToUnix (f : string) : string =
    if f = null then f
    else f.Replace(@"\", "/")

let GetSubDirectory (subDir : string) (dir : DirectoryInfo) : DirectoryInfo = 
    let newPath = Path.Combine(dir.FullName, subDir)
    DirectoryInfo (newPath)

let CreateSubDirectory (dirName : string) (parentDir : DirectoryInfo) : DirectoryInfo =
    let dir = parentDir |> GetSubDirectory dirName
    dir.Create ()
    dir

let GetFile (fileName : string) (dir : DirectoryInfo) : FileInfo = 
    let fullFileName = Path.Combine(dir.FullName, fileName)
    FileInfo (fullFileName)

let rec private ComputeRelativePathInc (topDir : DirectoryInfo) (childDir : DirectoryInfo) (path : string) = 
    if topDir.FullName = childDir.FullName then path |> ToUnix
    else 
        let newPath = Path.Combine(childDir.Name, path)
        ComputeRelativePathInc topDir childDir.Parent newPath

let ComputeRelativePath (dir : DirectoryInfo) (file : FileInfo) : string = 
    let path = file.Name
    ComputeRelativePathInc dir file.Directory path


let CurrentFolder() : DirectoryInfo = 
    DirectoryInfo(System.Environment.CurrentDirectory)


let CopyFolder (source : DirectoryInfo) (target : DirectoryInfo) (readOnly : bool) =
    let currDir = CurrentFolder()
    let setRead = if readOnly then "/A+:R"
                  else "/A-:R"

    let args = sprintf "%s /MIR /MT /NP /NFL /NDL /NJH %A %A" setRead source.FullName target.FullName
    Exec.Exec "robocopy.exe" args currDir

let GetExtension (file : FileInfo) =
    file.Extension.Replace(".", "")

let GetRootDirectory (file : string) =
    let idx = file.IndexOf('/')
    file.Substring(0, idx)

let GetFilewithoutRootDirectory (file : string) =
    let idx = file.IndexOf('/')
    file.Substring(idx+1)
   