module Commands.Package

let Update () =
    Core.Package.UpdatePackages()

let Outdated () =
    Tools.PaketInterface.PaketOutdated ()

let List () =
    Tools.PaketInterface.PaketInstalled ()
