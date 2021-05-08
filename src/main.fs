module Main

open System
open System.IO
open System.Runtime.InteropServices
open Options.Globals

let rename codes =
    // Compute the list of possible 1-letter and 2-letter variables names, ordered by descending letter frequency
    let computeFrequencyIdentTable codes =
        let str = Printer.printText codes
        let charCounts = Seq.countBy id str |> dict
        let count c = let ok, res = charCounts.TryGetValue(c) in if ok then res else 0
        let letters = ['a'..'z']@['A'..'Z']
        let oneLetterIdentifiers = letters |> List.sortByDescending count |> List.map string
        let twoLettersIdentifiers =
            [for c1 in letters do
             for c2 in letters do
             yield c1.ToString() + c2.ToString()]
            |> List.sortByDescending (fun s -> count s.[0] + count s.[1])
        Array.ofList (oneLetterIdentifiers @ twoLettersIdentifiers)

    let codes = Renamer.renameTopLevel codes Renamer.Unambiguous [||]
    let identTable = computeFrequencyIdentTable codes
    Renamer.computeContextTable codes

    let codes = Renamer.renameTopLevel codes Renamer.Context identTable
    codes

let minify(streamName, (content : String)) =
    vprintfn "Minifying '%s': Input file size is: %d" streamName (content.Length)
    let mutable codes = Parse.runParser streamName content
    let shaderSize () = (Printer.printText codes).Length
    vprintfn "File parsed. Shader size is: %d" (shaderSize ())
    codes <- Rewriter.reorder codes
    codes <- Rewriter.apply codes
    vprintfn "Rewrite tricks applied. Shader size is: %d" (shaderSize ())
    if not options.noRenaming then
        codes <- rename codes
        vprintfn "%d identifiers renamed. Shader size is: %d" Renamer.numberOfUsedIdents (shaderSize ())
    vprintfn "Minification of '%s' finished." streamName
    codes

let minifyFile filename =
    let (streamName, content) =
        if filename = ""
        then ("stdin", (new StreamReader(Console.OpenStandardInput())).ReadToEnd())
        else (filename, (new StreamReader(filename)).ReadToEnd())
    minify(streamName, content)

[<EntryPoint>]
let main argv =
    let err =
        try
            if options.init argv then 
                if options.verbose then
                    printfn "Shader Minifier %s - https://github.com/laurentlb/Shader_Minifier" Options.version
                use out =
                    if Options.debugMode || options.outputName = "" || options.outputName = "-" then stdout
                    else new StreamWriter(options.outputName) :> TextWriter
                // first minify every file, then print minified files
                let filenamesAndMinifiedCodes = Array.map (fun f -> (f, Printer.print (minifyFile f))) options.filenames
                Formatter.format out filenamesAndMinifiedCodes options.outputFormat
                0
            else 1
        with
        | :? Argu.ArguParseException as ex ->
            printfn "%s" ex.Message
            1
    if Options.debugMode && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then System.Console.ReadLine() |> ignore
    exit err
