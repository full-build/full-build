module Conversion
open Anthology
open System.IO
open System.Xml.Linq


let XDocumentLoader (fileName : FileInfo) =
    XDocument.Load fileName.FullName

let XDocumentSaver (fileName : FileInfo) (xdoc : XDocument) =
    xdoc.Save (fileName.FullName)


let convertMsBuild repos =
    let antho = Configuration.LoadAnthology ()
    let projects = antho.Projects |> Set.filter (fun x -> repos |> Set.contains x.Repository)

    MsBuildConversion.GenerateProjects projects XDocumentSaver
    MsBuildConversion.ConvertProjects projects XDocumentLoader XDocumentSaver
    MsBuildConversion.RemoveUselessStuff projects



let Convert builder repos =
    match builder with
    | BuilderType.MSBuild -> convertMsBuild repos
