module Commands.Package

let Update () =
    Core.Package.UpdatePackages()

let Outdated () =
    Tools.Paket.PaketOutdated ()

let List () =
    Tools.Paket.PaketInstalled ()
