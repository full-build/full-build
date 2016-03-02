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

module Publishers
open Anthology
open IoHelpers
open Env
open System.IO
open System.Linq
open System.Xml.Linq
open MsBuildHelpers


let private checkErrorCode err =
    if err <> 0 then failwithf "Process failed with error %d" err

let private checkedExec = 
    Exec.Exec checkErrorCode


let generateBinding (file : FileInfo) =
    try
        let bytes = File.ReadAllBytes(file.FullName)
        let ass = System.Reflection.Assembly.ReflectionOnlyLoad(bytes)
        if null = ass then null
        else
            let name = ass.GetName()
            let flags = name.Flags
            if System.Reflection.AssemblyNameFlags.None = (flags &&& System.Reflection.AssemblyNameFlags.PublicKey) then null
            else
                // <dependentAssembly>
                //   <assemblyIdentity name="protobuf-net" publicKeyToken="257b51d87d2e4d67"/>
                //   <bindingRedirect oldVersion="2.0.0.280" newVersion="2.0.0.481"/>
                // </dependentAssembly>
                let publicKey = name.GetPublicKeyToken()
                let publicKeyToken = publicKey |> Seq.map (fun x -> x.ToString("x2")) |> System.String.Concat
                let depAss = XElement(NsRuntime + "dependentAssembly",
                                        XElement(NsRuntime + "assemblyIdentity",
                                            XAttribute(NsNone + "name", name.Name), 
                                            XAttribute(NsNone + "publicKeyToken", publicKeyToken)),
                                        XElement(NsRuntime + "bindingRedirect", 
                                            XAttribute(NsNone + "oldVersion", "0.0.0.0-65535.65535.65535.65535"), 
                                            XAttribute(NsNone + "newVersion", name.Version.ToString())))
                depAss                                      
    with
        exn -> null


let forceBindings (bindings : XElement) (exeFile : FileInfo) =
    let appConfig = exeFile.FullName |> IoHelpers.AddExt IoHelpers.Extension.Config |> FileInfo
    let config = if appConfig.Exists then XDocument.Load(appConfig.FullName)
                 else XDocument(XElement(NsNone + "configuration"))

    let mutable runtime = config.Descendants(NsNone + "runtime").SingleOrDefault()
    if null = runtime then
        runtime <- XElement(NsNone + "runtime")
        config.Root.Add(runtime)

    let mutable assBinding = config.Descendants(NsRuntime + "assemblyBinding").SingleOrDefault()
    if null <> assBinding then
        assBinding.Remove()
        runtime.Add (bindings)      

    File.WriteAllText(appConfig.FullName, config.ToString())


let updateBindingRedirects (artifactDir : DirectoryInfo) =
    let dependentAssemblies = artifactDir.GetFiles ("*.dll")  
                              |> Seq.map generateBinding
                              |> Seq.filter (fun x -> x <> null)
    let bindings = XElement(NsRuntime + "assemblyBinding", dependentAssemblies)
    
    artifactDir.GetFiles ("*.exe") |> Seq.iter (forceBindings bindings)

let publishCopy (app : Anthology.Application) =
    let wsDir = GetFolder Env.Workspace
    let antho = Configuration.LoadAnthology ()
    let project = antho.Projects |> Seq.find (fun x -> x.ProjectId = app.Project)
    let repoDir = wsDir |> GetSubDirectory (project.Repository.toString)
    if repoDir.Exists then
        let projFile = repoDir |> GetFile project.RelativeProjectFile.toString 
        let args = sprintf "/nologo /t:FBPublish /p:SolutionDir=%A /p:FBApp=%A %A" wsDir.FullName app.Name.toString projFile.FullName

        if Env.IsMono () then checkedExec "xbuild" args wsDir
        else checkedExec "msbuild" args wsDir

        let appDir = GetFolder Env.AppOutput
        let artifactDir = appDir |> GetSubDirectory app.Name.toString
        updateBindingRedirects artifactDir
    else
        printfn "[WARNING] Can't publish application %A without repository" app.Name.toString

let publishZip (app : Anthology.Application) =
    let tmpApp = { app
                   with Name = ApplicationId.from "tmpzip"
                        Publisher = PublisherType.Copy }

    publishCopy tmpApp

    let appDir = GetFolder Env.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.Name.toString)
    let targetFile = appDir |> GetFile app.Name.toString
    if targetFile.Exists then targetFile.Delete()

    System.IO.Compression.ZipFile.CreateFromDirectory(sourceFolder.FullName, targetFile.FullName, Compression.CompressionLevel.Optimal, false)
    sourceFolder.Delete(true)
    

let publishFake (app : Anthology.Application) =
    let args = sprintf @".full-build\build.fsx target=Publish app=%A" app.Name
    let wsDir = Env.GetFolder Env.Workspace
    checkedExec "fake" args wsDir



let choosePublisher (pubType : PublisherType) appCopy appZip appFake =
    let publish = match pubType with
                  | PublisherType.Copy -> appCopy
                  | PublisherType.Zip -> appZip
                  | PublisherType.Fake -> appFake
    publish


let PublishWithPublisher (pubType : PublisherType) =
    choosePublisher pubType publishCopy publishZip publishFake

