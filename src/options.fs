module Options

open System.Collections.Generic
open System.IO
open Microsoft.FSharp.Text

let version = "1.1.6" // Shader Minifier version
let debugMode = false

type TargetOutput = Text | CHeader | CList | JS | Nasm

type Options() =
    let mutable _canonicalFieldNames = "xyzw"
    member val outputName = "shader_code.h" with get, set
    member val targetOutput = CHeader with get, set
    member val verbose = false with get, set
    member val smoothstepTrick = false with get, set
    member this.canonicalFieldNames with get () = _canonicalFieldNames
    member this.trySetCanonicalFieldNames(s: string) =
        if not (s = "rgba" || s = "xyzw" || s = "stpq" || s = "") then
            false
        else
            _canonicalFieldNames <- s
            true
    member val preserveExternals = false with get, set
    member val preserveAllGlobals = false with get, set
    member val reorderDeclarations = false with get, set
    member val reorderFunctions = false with get, set
    member val hlsl = false with get, set
    member val noSequence = false with get, set
    member val noRenaming = false with get, set
    member val noRenamingList = [ "main" ] with get, set
    member val filenames = List<string>() with get, set

module Globals =
    let options = Options()

    // enhance debugging on windows
    let myExit exitCode =
        if debugMode then System.Console.ReadLine() |> ignore
        exit exitCode

    // like printfn when verbose option is set
    let vprintfn fmt = fprintfn (if options.verbose then stdout else TextWriter.Null) fmt
open Globals

let printHeader () =
    printfn "Shader Minifier %s - https://github.com/laurentlb/Shader_Minifier" version
    printfn ""

let parse () =
    let specs =
        let setFieldNames s =
            if not (options.trySetCanonicalFieldNames s) then
                printfn "'%s' is not a valid value for field-names" s
                printfn "You must use 'rgba', 'xyzw', or 'stpq'."
                //myExit 1
        let noRenamingFct (s:string) = options.noRenamingList <- [for i in s.Split([|','|]) -> i.Trim()]
        let setFormat = fun s ->
            options.targetOutput <-
                match s with
                | "c-variables" -> CHeader
                | "js" -> JS
                | "c-array" -> CList
                | "none" -> Text
                | "nasm" -> Nasm
                | s -> printfn "'%s' is not a valid format" s; options.targetOutput
        ["-o", ArgType.String (fun s -> options.outputName <- s), "Set the output filename (default is shader_code.h)"
         "-v", ArgType.Unit (fun() -> options.verbose<-true), "Verbose, display additional information"
         "--hlsl", ArgType.Unit (fun() -> options.hlsl<-true), "Use HLSL (default is GLSL)"
         "--format", ArgType.String setFormat, "Can be: c-variables (default), c-array, js, nasm, or none"
         "--field-names", ArgType.String setFieldNames, "Choose the field names for vectors: 'rgba', 'xyzw', or 'stpq'"
         "--preserve-externals", ArgType.Unit (fun() -> options.preserveExternals<-true), "Do not rename external values (e.g. uniform)"
         "--preserve-all-globals", ArgType.Unit (fun() -> options.preserveAllGlobals<-true; options.preserveExternals<-true), "Do not rename functions and global variables"
         "--no-renaming", ArgType.Unit (fun() -> options.noRenaming<-true), "Do not rename anything"
         "--no-renaming-list", ArgType.String noRenamingFct, "Comma-separated list of functions to preserve"
         "--no-sequence", ArgType.Unit (fun() -> options.noSequence<-true), "Do not use the comma operator trick"
         "--smoothstep", ArgType.Unit (fun() -> options.smoothstepTrick<-true), "Use IQ's smoothstep trick"
         "--", ArgType.Rest options.filenames.Add, "Stop parsing command line"
        ] |> List.map ArgInfo

    ArgParser.Parse(specs, options.filenames.Add)

    if options.filenames.Count = 0 then
        printHeader()
        ArgParser.Usage(specs, usage = "Please give the shader files to compress on the command line.")
        myExit 1
    if options.filenames.Count > 1 && not options.preserveExternals then
        printfn "When compressing multiple files, you must use the --preserve-externals option."
        myExit 1
    if options.verbose then printHeader()
