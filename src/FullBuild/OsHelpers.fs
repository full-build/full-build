module OsHelpers

open Microsoft.Win32

let RegisterSystemExtension (installFolder : System.IO.DirectoryInfo) =
    if Env.IsMono() |> not then
        let extension = IoHelpers.Extension.View |> IoHelpers.GetExtensionString |> sprintf ".%s"
        let keyName = "fullbuild"
        let description = "Fullbuild view file"
        let openWith = System.IO.Path.Combine(installFolder.FullName, "fullbuild.exe")
        let icon = System.IO.Path.Combine(installFolder.FullName, "favicon.ico")
    
        let baseKey = Registry.ClassesRoot.CreateSubKey(extension)
        baseKey.SetValue("", keyName)

        let openMethod = Registry.ClassesRoot.CreateSubKey(keyName)
        openMethod.SetValue("", description)
        openMethod.CreateSubKey("DefaultIcon").SetValue("",sprintf "\"%s\"" icon)
    
        let shell = openMethod.CreateSubKey("Shell")
        shell.CreateSubKey("edit").CreateSubKey("command").SetValue("", sprintf "\"%s\" \"%%1\"" openWith);
        shell.CreateSubKey("open").CreateSubKey("command").SetValue("", sprintf "\"%s\" \"%%1\"" openWith);
        baseKey.Close()
        openMethod.Close()
        shell.Close()
