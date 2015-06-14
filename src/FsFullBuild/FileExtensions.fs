module FileExtensions

open System.IO


let GetSubDirectory (subDir : string) (dir : DirectoryInfo) : DirectoryInfo =
    let newPath = Path.Combine(dir.FullName,subDir)
    new DirectoryInfo(newPath)

let GetFile (fileName : string) (dir : DirectoryInfo) : FileInfo =
    let fullFileName = Path.Combine(dir.FullName, fileName)
    new FileInfo(fullFileName)
