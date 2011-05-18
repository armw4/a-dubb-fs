// Learn more about F# at http://fsharp.net
open System.Xml.Linq
open System.Reflection
open System.IO
open System.Linq
open Microsoft.FSharp.Control
open System.Threading
open System
 
let pathAttributeName = "path"
let xs n = XName.Get(n)
 
let workflow = async {
    printfn "Started listening at %A..." DateTime.Now

    while Console.ReadLine() <> "q"
        do
            let doc = (Assembly.GetExecutingAssembly().Location |> Path.GetDirectoryName) + "\Synch.config" |> XDocument.Load
            let config = doc.Root
            let root = config.Element("root" |> xs)
            let rootDir = root.Attribute(pathAttributeName |> xs).Value
 
            let mapdirs (e : XElement) =
                e.Elements("add" |> xs)
                |> Seq.map((fun (e : XElement) ->
                                let dir = [|rootDir; e.Attribute(pathAttributeName |> xs).Value;|] |> String.Concat
                                dir))
 
            let directories = config.Element("directories" |> xs)
            let masters = directories.Element("masters" |> xs) |> mapdirs
            let slaves = directories.Element("slaves" |> xs) |> mapdirs
 
            let directoryWatchers = masters
                                    |> Seq.map((fun d -> 
                                                    new FileSystemWatcher(d, EnableRaisingEvents = true, IncludeSubdirectories = true)))
 
            directoryWatchers 
            |> Seq.iter((fun w -> 
                            w.Changed.Add((fun e -> 
                                            let merge fp fn =
                                                let targetDir = Path.GetDirectoryName fp
                                                 
                                                try
                                                    use fs = File.Open(fp, FileMode.Open, FileAccess.Read, FileShare.Read)
                                                     
                                                    let size = fs.Length |> int
                                                    let buffer = Array.zeroCreate<byte> size
                                                     
                                                    fs.Read(buffer, 0, size) |> ignore
                                                     
                                                    slaves
                                                    |> Seq.iter ((fun d -> 
                                                                    let fileName = [|d; "\\"; fn;|] |> String.Concat
 
                                                                    if File.Exists fileName then
                                                                        File.WriteAllBytes(fileName, buffer)
 
                                                                        printfn "Merged %s to %s at %A %s" fp fileName DateTime.Now Environment.NewLine
                                                                    else
                                                                        printfn "File %s did not exist in directory %s. No merge required. Aborting...%s" fileName d Environment.NewLine
                                                                    ))
                                                with
                                                  | :? IOException as ioe ->
                                                        printfn "exception occured %s %s" ioe.Message Environment.NewLine
                                                
                                            e.Name |> merge e.FullPath))))
 
    printfn "Stopped listening at %A..." DateTime.Now
}
 
let start() = 
    workflow |> Async.RunSynchronously
 
do start()