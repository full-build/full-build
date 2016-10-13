module Commands.Package

let Update () =
    Plumbing.Package.UpdatePackages()

let Outdated () =
    PaketInterface.PaketOutdated ()

let List () =
    PaketInterface.PaketInstalled ()
