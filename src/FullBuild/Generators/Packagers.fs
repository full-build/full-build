module Generators.Packagers

open System.IO

type Nuget = FSharp.Data.XmlProvider< "Examples/Library.nuspec", Global=true, SampleIsList=true >

let private getDependencyVersion package =
    let packageNuspec = package 
                        |> Parsers.PackageRelationship.GetDependencyNuspec 
                        |> Nuget.Load
    match packageNuspec.Package, packageNuspec.Package2 with
    | Some pkg, _ -> pkg.Metadata.Version
    | _, Some pkg -> pkg.Metadata.Version
    | _ -> failwith "Unable to parse nuspec"

let UpdateDependencies (nuspecFile : FileInfo) = 
    let updateVersion (xElement:System.Xml.Linq.XElement) version =
        xElement.SetAttributeValue(System.Xml.Linq.XName.Get "version", version)
    let nuspec = Nuget.Load(nuspecFile.FullName)
    match nuspec.Package, nuspec.Package2 with
    | Some pkg, _ -> pkg.Metadata.Dependencies |> Seq.iter (fun dep -> dep.Id |> getDependencyVersion |> updateVersion dep.XElement)
    | _, Some pkg -> pkg.Metadata.Dependencies |> Seq.iter (fun dep -> dep.Id |> getDependencyVersion |> updateVersion dep.XElement)
    | _ -> ()
    nuspec.XElement.Save(nuspecFile.FullName)