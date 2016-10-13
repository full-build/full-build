module Commands.Package

let Update () =
    Core.Package.UpdatePackages()

let Outdated () =
    PaketInterface.PaketOutdated ()

let List () =
    PaketInterface.PaketInstalled ()
