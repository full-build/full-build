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

module Bindings
open Anthology
open IoHelpers
open Env
open System.IO
open System.Linq
open System.Xml.Linq
open MsBuildHelpers
open Collections


let generateBinding (allAssemblies : AssemblyId set) (file : FileInfo) =
    let assId = AssemblyId.from file
    if not (allAssemblies |> Set.contains assId) then
        let ass = Mono.Cecil.AssemblyDefinition.ReadAssembly(file.FullName)
        if null <> ass then
            let name = ass.Name
            if ass.Name.HasPublicKey then
                // <dependentAssembly>
                //   <assemblyIdentity name="protobuf-net" publicKeyToken="257b51d87d2e4d67"/>
                //   <bindingRedirect oldVersion="2.0.0.280" newVersion="2.0.0.481"/>
                // </dependentAssembly>
                let publicKey = name.PublicKeyToken
                let publicKeyToken = publicKey |> Seq.map (fun x -> x.ToString("x2")) |> System.String.Concat
                let depAss = XElement(NsRuntime + "dependentAssembly",
                                        XElement(NsRuntime + "assemblyIdentity",
                                            XAttribute(NsNone + "name", name.Name), 
                                            XAttribute(NsNone + "publicKeyToken", publicKeyToken)),
                                        XElement(NsRuntime + "bindingRedirect", 
                                            XAttribute(NsNone + "oldVersion", "0.0.0.0-65535.65535.65535.65535"), 
                                            XAttribute(NsNone + "newVersion", name.Version.ToString())))
                depAss 
            else null
        else null
    else null


let getAssemblyConfig (file : FileInfo) =
    file.FullName |> IoHelpers.AddExt IoHelpers.Extension.Config |> FileInfo


let forceBindings (bindings : XElement) (appConfig : FileInfo) =
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

let anthologyAssemblies () =
    let antho = Configuration.LoadAnthology()
    let assemblies = antho.Projects |> Seq.map (fun x -> x.Output)
                                    |> Set
    assemblies

let generateBindings (allAssemblies : AssemblyId set) (artifactDir : DirectoryInfo) =
    let dependentAssemblies = artifactDir.GetFiles ("*.dll")  
                              |> Seq.map (generateBinding allAssemblies)
                              |> Seq.filter (fun x -> x <> null)
    let bindings = XElement(NsRuntime + "assemblyBinding", dependentAssemblies)
    bindings

let UpdateArtifactBindingRedirects (artifactDir : DirectoryInfo) =
    let assemblies = anthologyAssemblies()
    let bindings = generateBindings assemblies artifactDir 

    let dllConfigs = artifactDir.GetFiles ("*.dll") |> Seq.map getAssemblyConfig |> Seq.where(fun x -> x.Exists)
    let exeConfigs = artifactDir.GetFiles ("*.exe") |> Seq.map getAssemblyConfig 
    let templateConfig = GetFile "app.template.config" artifactDir |> Seq.singleton

    exeConfigs|> Seq.append dllConfigs |> Seq.append templateConfig |> Seq.iter (forceBindings bindings)

let UpdateProjectBindingRedirects (projectDir : DirectoryInfo) =
    let assemblies = anthologyAssemblies()
    let artifactDir = projectDir |> GetSubDirectory "bin"
    let bindings = generateBindings assemblies artifactDir 
    let appConfig = projectDir |> GetFile "app.config"
    forceBindings bindings appConfig
