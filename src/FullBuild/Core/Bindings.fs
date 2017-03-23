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

module Core.Bindings
open IoHelpers
open Env
open System.IO
open System.Linq
open System.Xml.Linq
open XmlHelpers
open Collections


let private generateBindingUnsafe (allAssemblies : string set) (file : FileInfo) =
    // HACK
    let assId = Path.GetFileNameWithoutExtension(file.FullName).ToLowerInvariant()
    if not (allAssemblies |> Set.contains assId) then
        let assName = System.Reflection.AssemblyName.GetAssemblyName(file.FullName)
        let publicKey = assName.GetPublicKeyToken()
        if  publicKey <> null && publicKey.Length <> 0 then
            // <dependentAssembly>
            //   <assemblyIdentity name="protobuf-net" publicKeyToken="257b51d87d2e4d67"/>
            //   <bindingRedirect oldVersion="2.0.0.280" newVersion="2.0.0.481"/>
            // </dependentAssembly>
            let publicKeyToken = publicKey |> Seq.map (fun x -> x.ToString("x2")) |> System.String.Concat
            let depAss = XElement(NsRuntime + "dependentAssembly",
                                    XElement(NsRuntime + "assemblyIdentity",
                                        XAttribute(NsNone + "name", assName.Name),
                                        XAttribute(NsNone + "publicKeyToken", publicKeyToken)),
                                    XElement(NsRuntime + "bindingRedirect",
                                        XAttribute(NsNone + "oldVersion", "0.0.0.0-65535.65535.65535.65535"),
                                        XAttribute(NsNone + "newVersion", assName.Version.ToString())))
            depAss
        else null
    else null


let private generateBinding (allAssemblies : string set) (file : FileInfo) =
    try
        generateBindingUnsafe allAssemblies file
    with
        :? System.BadImageFormatException -> null

let private getAssemblyConfig (file : FileInfo) =
    file.FullName |> IoHelpers.AddExt IoHelpers.Extension.Config |> FileInfo


let private forceBindings (bindings : XElement) (appConfig : FileInfo) =
    let config = if appConfig.Exists then XDocument.Load(appConfig.FullName)
                 else XDocument(XElement(NsNone + "configuration"))

    let mutable runtime = config.Descendants(NsNone + "runtime").SingleOrDefault()
    if null = runtime then
        runtime <- XElement(NsNone + "runtime")
        config.Root.Add(runtime)

    config.Descendants(NsRuntime + "assemblyBinding").Remove()
    runtime.Add (bindings)

    // <assemblyBinding>
    //   <assemblyIdentity name="protobuf-net" publicKeyToken="257b51d87d2e4d67"/>
    //   <bindingRedirect oldVersion="2.0.0.280" newVersion="2.0.0.481"/>
    // </assemblyBinding>
    config.Save(appConfig.FullName)

let private anthologyAssemblies () =
    let graph = Graph.load()
    let assemblies = graph.Projects |> Seq.map (fun x -> x.Output.Name)
                                    |> Set
    assemblies

let private generateBindings (allAssemblies : string set) (artifactDir : DirectoryInfo) =
    let dependentAssemblies = artifactDir.GetFiles ("*.dll")
                              |> Seq.map (generateBinding allAssemblies)
                              |> Seq.filter (fun x -> x <> null)
    let bindings = XElement(NsRuntime + "assemblyBinding", dependentAssemblies)
    bindings


let UpdateArtifactBindingRedirects (artifactDir : DirectoryInfo) =
    let assemblies = anthologyAssemblies()
    let bindings = generateBindings assemblies artifactDir

    let dllConfigs = artifactDir.GetFiles ("*.dll") |> Seq.map getAssemblyConfig
    let exeConfigs = artifactDir.GetFiles ("*.exe") |> Seq.map getAssemblyConfig
    let templateConfig = artifactDir |> GetFile "app.template.config" |> List.singleton

    exeConfigs
        |> Seq.append dllConfigs
        |> Seq.append templateConfig
        |> Seq.filter (fun x -> x.Exists)
        |> Seq.iter (forceBindings bindings)

let UpdateProjectBindingRedirects (project : Graph.Project) =
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let repoDir = wsDir |> IoHelpers.GetSubDirectory project.Repository.Name
    let prjFile = repoDir |> GetFile project.ProjectFile
    let projectDir = prjFile.Directory

    let assemblies = anthologyAssemblies()
    let artifactDir = projectDir |> GetSubDirectory "bin"
    let bindings = generateBindings assemblies artifactDir
    let appConfig = projectDir |> GetFile "app.config"
    forceBindings bindings appConfig
