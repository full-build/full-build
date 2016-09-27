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

[<RequireQualifiedAccess>]
type VcsType =
    | Gerrit
    | Git
    | Hg

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

        member this.Projects =
            this.Anthology.Projects |> Seq.filter (fun x -> this.Application.Projects |> Set.contains x.ProjectId)
                                    |> Seq.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

and Repository =
    { Anthology : Anthology.Anthology
      Repository : Anthology.BuildableRepository }
    with
        member this.Name = this.Repository.Repository.Name.toString

        member this.Builder = match this.Repository.Builder with
                              | Anthology.BuilderType.MSBuild -> BuilderType.MSBuild
                              | Anthology.BuilderType.Skip -> BuilderType.Skip

        member this.Vcs = match this.Anthology.Vcs with
                          | Anthology.VcsType.Gerrit -> VcsType.Gerrit
                          | Anthology.VcsType.Git -> VcsType.Git
                          | Anthology.VcsType.Hg -> VcsType.Hg

        member this.Branch = match this.Repository.Repository.Branch with
                             | Some x -> x.toString
                             | None -> match this.Vcs with
                                       | VcsType.Gerrit | VcsType.Git -> "master"
                                       | VcsType.Hg -> "default"

        member this.Uri = this.Repository.Repository.Url.toString

        member this.Projects =
            this.Anthology.Projects |> Seq.filter (fun x -> x.Repository = this.Repository.Repository.Name)
                                    |> Seq.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

and Project =
    { Anthology : Anthology.Anthology
      Project : Anthology.Project }
    with
        member this.Repository =
            { Anthology = this.Anthology
              Repository = this.Anthology.Repositories |> Seq.find (fun x -> x.Repository.Name = this.Project.Repository) }

        member this.Applications =
            this.Anthology.Applications |> Seq.filter (fun x -> x.Projects |> Set.contains this.Project.ProjectId)
                                        |> Seq.map (fun x -> { Anthology = this.Anthology
                                                               Application = x })

        member this.References =
            this.Anthology.Projects |> Seq.filter (fun x -> this.Project.ProjectReferences |> Set.contains x.ProjectId) 
                                    |> Seq.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

        member this.ReferencedBy =
            this.Anthology.Projects |> Seq.filter (fun x -> x.ProjectReferences |> Set.contains this.Project.ProjectId) 
                                    |> Seq.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

        member this.RelativeProjectFile = this.Project.RelativeProjectFile.toString

        member this.UniqueProjectId = this.Project.UniqueProjectId.toString

        member this.Output = { Anthology = this.Anthology
                               Assembly = this.Project.Output }

        member this.ProjectId = this.Project.ProjectId.toString

        member this.OutputType = match this.Project.OutputType with
                                 | Anthology.OutputType.Dll -> OutputType.Dll
                                 | Anthology.OutputType.Exe -> OutputType.Exe

        member this.FxVersion = match this.Project.FxVersion.toString with
                                | null -> None
                                | x -> Some x

        member this.FxProfile = match this.Project.FxProfile.toString with
                                | null -> None
                                | x -> Some x

        member this.FxIdentifier = match this.Project.FxIdentifier.toString with
                                   | null -> None
                                   | x -> Some x

        member this.HasTests = this.Project.HasTests

        member this.AssemblyReferences = 
            this.Project.AssemblyReferences |> Seq.map (fun x -> { Anthology = this.Anthology
                                                                   Assembly = x })
        member this.PackageReferences = 
            this.Project.PackageReferences |> Seq.map (fun x -> { Anthology = this.Anthology
                                                                  Package = x })


and Graph =
    { Anthology : Anthology.Anthology }
    with
        member this.Projects =
            this.Anthology.Projects |> Seq.map (fun x -> { Anthology = this.Anthology
                                                           Project = x })

        member this.Applications =
            this.Anthology.Applications |> Seq.map (fun x -> { Anthology = this.Anthology
                                                               Application = x })

        member this.Repositories =
            this.Anthology.Repositories |> Seq.map (fun x -> { Anthology = this.Anthology
                                                               Repository = x })

let from (antho : Anthology.Anthology) : Graph =
    { Anthology = antho }
