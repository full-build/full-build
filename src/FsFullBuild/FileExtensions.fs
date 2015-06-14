module FileExtensions

open System.IO


let GetSubDirectory (subDir : string) (dir : DirectoryInfo) : DirectoryInfo =
    let newPath = Path.Combine(dir.FullName,subDir)
    new DirectoryInfo(newPath)

let GetFile (dir : DirectoryInfo) (fileName : string) : FileInfo =
    let fullFileName = Path.Combine(dir.FullName, fileName)
    new FileInfo(fullFileName)
