module FileExtensions

open System.IO


let GetSubDirectory (subDir : string) (dir : DirectoryInfo) : DirectoryInfo =
    let newPath = Path.Combine(dir.FullName,subDir)
    new DirectoryInfo(newPath)
