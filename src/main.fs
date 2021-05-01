module Main

open System
open System.IO
open Options.Globals

let rename codes =
    // Compute the list of possible 1-letter and 2-letter variables names, ordered by descending letter frequency
    let computeFrequencyIdentTable codes =
        let str = Printer.quickPrint codes
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
    let shaderSize () = (Printer.quickPrint codes).Length
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

let () = // parse argv, then process filenames
    try
        if System.Diagnostics.Debugger.IsAttached then
            options.outputName <- ""
            options.preserveExternals <- true
            options.filenames.AddRange(Directory.GetFiles("tests/", "*.frag", SearchOption.AllDirectories))
            options.filenames.AddRange(Directory.GetFiles("tests/", "*.glsl", SearchOption.AllDirectories))
        else
            Options.parse ()
        // first minify every file, then print minified files
        let codes = Array.map minifyFile (options.filenames.ToArray())
        CGen.print (Array.zip (options.filenames.ToArray()) codes) options.targetOutput
        myExit 0
    with e -> printfn "%s" (e.ToString())
              myExit 1
