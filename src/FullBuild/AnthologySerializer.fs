module AnthologySerializer

open Anthology
open System.IO
open System
open Collections
open System.Text

//
// file format:
//
// # version 1
// # repository <id>
//   - type <type>
//   - url url
// ...
// # project <guid>
//   - repo <id>
//   - type dll/exe
//   - out <assemblyName>
//   - fx <id>
//   - file <file>
//   # assembly <id>
//   ...
//   # package <id1>
//   ...
//   # project <id1>
//   ...
// ...
//
//
//
//let SerializeAnthologyV1 (antho : Anthology) =
//    seq {
//        yield "version 1"
//
//        for repo in antho.Repositories do
//            yield sprintf "repository %s %A %s" repo.Name.Value repo.Vcs repo.Url.Value
//        
//        for project in antho.Projects do
//            yield sprintf "project %s %s %A %s %A %s" project.Repository.Value (project.ProjectGuid.Value.ToString("D")) project.OutputType project.Output.Value project.FxTarget project.RelativeProjectFile.Value
//            for ass in project.AssemblyReferences do
//                yield sprintf "  assembly %s" ass.Value
//            for pkg in project.PackageReferences do
//                yield sprintf "  package %s" pkg.Value
//            for prj in project.ProjectReferences do
//                yield sprintf "  project %s" (prj.Value.ToString("D"))
//    }
//
//
//
//let DeserializeAnthology (lines : string list) =
//
//
//    
//let Save (filename : FileInfo) (antho : Anthology) =
//    let content = SerializeAnthology antho
//    File.WriteAllLines(filename.FullName, content)
//
//let Load (filename : FileInfo) : Anthology
//    let content = File.ReadAllLines (filename.FullName) |> Seq.toList
//    DeserializeAnthology content
//
//
