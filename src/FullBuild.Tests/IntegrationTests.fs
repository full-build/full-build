////   Copyright 2014-2017 Pierre Chalamet
////
////   Licensed under the Apache License, Version 2.0 (the "License");
////   you may not use this file except in compliance with the License.
////   You may obtain a copy of the License at
////
////       http://www.apache.org/licenses/LICENSE-2.0
////
////   Unless required by applicable law or agreed to in writing, software
////   distributed under the License is distributed on an "AS IS" BASIS,
////   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
////   See the License for the specific language governing permissions and
////   limitations under the License.

module Integration

//open FsUnit
//open NUnit.Framework
//open StringHelpers
//open System.IO
//open System.Diagnostics
//open Collections
//open FsHelpers


//let getBinFile viewName projectName platform =
//    let wsDir = Env.GetFolder Env.Folder.Workspace
//    let graph = Graph.load()
//    let views = graph |> Views.from
//    let view = views.Views |> Seq.find (fun x -> x.Name = viewName)
//    let project = graph.Projects |> Seq.find (fun x -> x.ProjectId = projectName)
//    let prjFile =  wsDir |> GetSubDirectory project.Repository.Name
//                         |> GetFile project.ProjectFile
//    let prjDir = prjFile.Directory |> GetSubDirectory "bin" 
//                                   |> GetSubDirectory view.Configuration 
//                                   |> GetSubDirectory platform
//    let exe = prjDir |> GetFile (project.Output.Name |> AddExt Extension.Exe)
//    exe

//let runFB (args : string) =
//    printfn "==== running fullbuild %s" args
//    let exe = getBinFile "fullbuild" "fullbuild" "net452"
//    let wsDir = Env.GetFolder Env.Folder.Workspace
//    Exec.Exec exe.FullName args wsDir Map.empty |> IO.CheckResponseCode

//[<Test>]
//let CheckSourceBuildIsSameAsBinaryBuild () = 
//    System.Environment.CurrentDirectory <- NUnit.Framework.TestContext.CurrentContext.TestDirectory
    
//    let expectedFilesMono = [ "libproject.dll"
//                              "libproject.dll.mdb"
//                              "mainproject.exe"
//                              "mainproject.exe.config"
//                              "mainproject.exe.mdb"
//                              "Zlib.Portable.dll" ] |> Seq.map (fun x -> x.ToLower()) |> Set

//    let expectedFilesWindows = [ "libproject.dll"
//                                 "libproject.pdb"
//                                 "mainproject.exe"
//                                 "mainproject.exe.config"
//                                 "mainproject.pdb"
//                                 "Zlib.Portable.dll" ] |> Seq.map (fun x -> x.ToLower()) |> Set

//    let expectedFiles = Env.IsMono() ? (expectedFilesMono, expectedFilesWindows)
                         
//    printfn "******************* BUILDING SOURCES ***********************"
//    runFB "--verbose view testsrc tests/*"
//    runFB "--verbose rebuild testsrc"    
//    printfn "******************* DONE BUILDING SOURCES ***********************"

//    let outputBin = getBinFile "testsrc" "mainproject" "net452"
//    let outputFileSrc = outputBin.Directory.EnumerateFiles () |> Seq.map (fun x -> x.Name.ToLower()) |> set
//    outputFileSrc |> should equal expectedFiles

//    printfn "******************* BUILDING BINARIES ***********************"
//    runFB "--verbose view testbin tests/mainproject"
//    runFB "--verbose rebuild testbin"
//    printfn "******************* DONE BUILDING BINARIES ***********************"
//    let outputFileBin = outputBin.Directory.EnumerateFiles () |> Seq.map (fun x -> x.Name.ToLower()) |> set
//    outputFileBin |> should equal expectedFiles
