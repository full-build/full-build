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

module AnthologySerializer

open Anthology
open ArtifactsSerializer
open ProjectsSerializer

type private AnthologyConfig = FSharp.Configuration.YamlConfig<"Examples/anthology.yaml">


let Serialize (antho : Anthology) =
    let artifacts = { MinVersion = antho.MinVersion
                      Binaries = antho.Binaries
                      NuGets = antho.NuGets
                      Vcs = antho.Vcs
                      MasterRepository = antho.MasterRepository
                      Repositories = antho.Repositories
                      Applications = antho.Applications
                      Tester = antho.Tester }
    let projects = { Projects = antho.Projects }
    (artifacts, projects)    

let Deserialize (artifacts : ArtifactsSerializer.Artifacts) (projects : ProjectsSerializer.Projects) =
    { MinVersion = artifacts.MinVersion
      Binaries = artifacts.Binaries
      NuGets = artifacts.NuGets
      Vcs = artifacts.Vcs
      MasterRepository = artifacts.MasterRepository
      Repositories = artifacts.Repositories
      Applications = artifacts.Applications
      Projects = projects.Projects
      Tester = artifacts.Tester }
