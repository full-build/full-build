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
module FileExtensions

open System.IO

let GetSubDirectory (dir : DirectoryInfo) (subDir : string) : DirectoryInfo = 
    let newPath = Path.Combine(dir.FullName, subDir)
    new DirectoryInfo(newPath)

let GetFile (dir : DirectoryInfo) (fileName : string) : FileInfo = 
    let fullFileName = Path.Combine(dir.FullName, fileName)
    new FileInfo(fullFileName)

let rec private ComputeRelativePath2 (topDir : DirectoryInfo) (childDir : DirectoryInfo) (path : string) = 
    if topDir.FullName = childDir.FullName then path
    else 
        let newPath = Path.Combine(childDir.Name, path)
        ComputeRelativePath2 topDir childDir.Parent newPath

let ComputeRelativePath (dir : DirectoryInfo) (file : FileInfo) : string = 
    let path = file.Name
    ComputeRelativePath2 dir file.Directory path

