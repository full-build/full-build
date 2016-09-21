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

module Graph
open Collections


[<RequireQualifiedAccess>] 
type PackageVersion =
    | PackageVersion of string
    | Unspecified

[<RequireQualifiedAccess>]
type OutputType =
    | Exe
    | Dll

[<RequireQualifiedAccess>]
type PublisherType =
    | Copy
    | Zip
    | Docker

[<RequireQualifiedAccess>]
type BuilderType =
    | MSBuild
    | Skip

type Package =
    { Anthology : Anthology.Anthology
      Package : Anthology.PackageId }
    with
        member this.Name = this.Package.toString

type Assembly = 
    { Anthology : Anthology.Anthology
      Assembly : Anthology.AssemblyId }
    with
        member this.Name = this.Assembly.toString

type Application =
    { Anthology : Anthology.Anthology
      Application : Anthology.Application } 
    with
        member this.Name = this.Application.Name.toString

        member this.Publisher = match this.Application.Publisher with
                                | Anthology.PublisherType.Copy -> PublisherType.Copy
                                | Anthology.PublisherType.Zip -> PublisherType.Zip
                                | Anthology.PublisherType.Docker -> PublisherType.Docker

        member this.Project : Project =
            { Anthology = this.Anthology
              Project = this.Anthology.Projects |> Seq.find (fun x -> x.ProjectId = this.Application.Project) }

and Repository =
    { Anthology : Anthology.Anthology
      Repository : Anthology.BuildableRepository }
    with
        member this.Name = this.Repository.Repository.Name.toString

        member this.Builder = match this.Repository.Builder with
                              | Anthology.BuilderType.MSBuild -> BuilderType.MSBuild
                              | Anthology.BuilderType.Skip -> BuilderType.Skip

        member this.Projects =
            this.Anthology.Projects |> Set.filter (fun x -> x.Repository = this.Repository.Repository.Name)
                                    |> Set.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

and Project =
    { Anthology : Anthology.Anthology
      Project : Anthology.Project }
    with
        member this.Repository =
            { Anthology = this.Anthology
              Repository = this.Anthology.Repositories |> Seq.find (fun x -> x.Repository.Name = this.Project.Repository) }

        member this.Application =
            let app = this.Anthology.Applications |> Seq.tryFind (fun x -> x.Project = this.Project.ProjectId)
            match app with
            | Some x -> Some { Anthology = this.Anthology
                               Application = x }
            | _ -> None

        member this.References =
            this.Anthology.Projects |> Set.filter (fun x -> this.Project.ProjectReferences |> Set.contains x.ProjectId) 
                                    |> Set.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

        member this.ReferencedBy =
            this.Anthology.Projects |> Set.filter (fun x -> x.ProjectReferences |> Set.contains this.Project.ProjectId) 
                                    |> Set.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

        member this.RelativeProjectFile = this.Project.RelativeProjectFile.toString
        member this.UniqueProjectId = this.Project.UniqueProjectId.toString
        member this.Output = { Anthology = this.Anthology
                               Assembly = this.Project.Output }
        member this.ProjectId = this.Project.ProjectId.toString
        member this.OutputType = match this.Project.OutputType with
                                 | Anthology.OutputType.Dll -> OutputType.Dll
                                 | Anthology.OutputType.Exe -> OutputType.Exe
        member this.FxVersion = this.Project.FxVersion.toString
        member this.FxProfile = this.Project.FxProfile.toString
        member this.FxIdentifier = this.Project.FxIdentifier.toString
        member this.HasTests = this.Project.HasTests
        member this.AssemblyReferences = this.Project.AssemblyReferences |> Set.map (fun x -> { Anthology = this.Anthology
                                                                                                Assembly = x })
        member this.PackageReferences = this.Project.PackageReferences |> Set.map (fun x -> { Anthology = this.Anthology
                                                                                              Package = x })


and Graph =
    { Anthology : Anthology.Anthology }
    with
        static member from (antho : Anthology.Anthology) : Graph =
            { Anthology = antho }

        member this.Projects =
            this.Anthology.Projects |> Set.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

        member this.Applications =
            this.Anthology.Applications |> Set.map (fun x -> { Anthology = this.Anthology
                                                               Application = x })

        member this.Repositories =
            this.Anthology.Repositories |> Set.map (fun x -> { Anthology = this.Anthology
                                                               Repository = x })

