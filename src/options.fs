module Options

open System.Collections.Generic
open System.IO
open Argu

let version = "1.1.6" // Shader Minifier version
let debugMode = false

type TargetOutput =
    | [<CustomCommandLine("text")>] Text
    | [<CustomCommandLine("c-variables")>] CHeader
    | [<CustomCommandLine("c-array")>] CList
    | [<CustomCommandLine("js")>] JS
    | [<CustomCommandLine("nasm")>] Nasm

let text() = Text

type FieldSet =
    | RGBA
    | XYZW
    | STPQ
    
type CliArguments =
    | [<CustomCommandLine("-o")>] OutputName of string
    | [<CustomCommandLine("-v")>] Verbose
    | [<CustomCommandLine("--hlsl")>] Hlsl
    | [<CustomCommandLine("--format")>] FormatArg of TargetOutput
    | [<CustomCommandLine("--field-names")>] FieldNames of FieldSet
    | [<CustomCommandLine("--preserve-externals")>] PreserveExternals
    | [<CustomCommandLine("--preserve-all-globals")>] PreserveAllGlobals
    | [<CustomCommandLine("--no-renaming")>] NoRenaming
    | [<CustomCommandLine("--no-renaming-list")>] NoRenamingList of string
    | [<CustomCommandLine("--no-sequence")>] NoSequence
    | [<CustomCommandLine("--smoothstep")>] Smoothstep
    | [<MainCommand>] Filenames of filename:string list
 
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | OutputName _ -> "Set the output filename (default is shader_code.h)"
            | Verbose -> "Verbose, display additional information"
            | Hlsl -> "Use HLSL (default is GLSL)"
            | FormatArg _ -> "Choose to format the output (use none if you want just the shader)"
            | FieldNames _ -> "Choose the field names for vectors: 'rgba', 'xyzw', or 'stpq'"
            | PreserveExternals _ -> "Do not rename external values (e.g. uniform)"
            | PreserveAllGlobals _ -> "Do not rename functions and global variables"
            | NoRenaming -> "Do not rename anything"
            | NoRenamingList _ -> "Comma-separated list of functions to preserve"
            | NoSequence -> "Do not use the comma operator trick"
            | Smoothstep -> "Use IQ's smoothstep trick"
            | Filenames _ -> "List of files to minify" 


let argParser = ArgumentParser.Create<CliArguments>(helpTextMessage =
    sprintf "Shader Minifier %s - https://github.com/laurentlb/Shader_Minifier" version)

type Options() =
    let mutable args: ParseResults<CliArguments> =
        Unchecked.defaultof<ParseResults<CliArguments>>

    member this.outputName =
        args.GetResult(OutputName, defaultValue = "shader_code.h")

    member this.targetOutput =
        args.GetResult(FormatArg, defaultValue = CHeader)

    member this.verbose =
        args.Contains(Verbose)

    member this.smoothstepTrick =
        args.Contains(Smoothstep)

    member this.canonicalFieldNames =
        args.GetResult(FieldNames, defaultValue = XYZW).ToString().ToLower()

    member this.preserveExternals =
        args.Contains(PreserveExternals)
    
    member this.preserveAllGlobals =
        args.Contains(PreserveAllGlobals)
    
    member val reorderDeclarations = false with get, set
    member val reorderFunctions = false with get, set

    member this.hlsl =
        args.Contains(Hlsl)

    member this.noSequence =
        args.Contains(NoSequence)

    member this.noRenaming =
        args.Contains(NoRenaming)

    member this.noRenamingList =
        let opt = args.GetResult(NoRenamingList, defaultValue = "main")
        [for i in opt.Split([|','|]) -> i.Trim()]

    member this.filenames =
        args.GetResult(Filenames, defaultValue=[]) |> List.toArray

    member this.init(argv) =
        args <- argParser.Parse(argv)
        if this.filenames.Length = 0 then
            printfn "%s" (argParser.PrintUsage(message = "Missing parameter: the list of shaders to minify"))
            false
        elif this.filenames.Length > 1 && not this.preserveExternals then
            printfn "When compressing multiple files, you must use the --preserve-externals option."
            false
        else
            true

module Globals =
    let options = Options()

    ////< HEAD
    
    // like printf when verbose option is set
    let vprintf fmt = fprintf (if options.verbose then stdout else TextWriter.Null) fmt
    
    ////=
    
    // like printfn when verbose option is set
    let vprintfn fmt = fprintfn (if options.verbose then stdout else TextWriter.Null) fmt
open Globals

//let parse () =
//    let printHeader () =
//        printfn "Shader Minifier %s - https://github.com/laurentlb/Shader_Minifier" version
//        printfn ""
//    let specs =
//        let setFieldNames s =
//            if not (options.trySetCanonicalFieldNames s) then
//                printfn "'%s' is not a valid value for field-names" s
//                printfn "You must use 'rgba', 'xyzw', or 'stpq'."
//                //myExit 1
//        let noRenamingFct (s:string) = options.noRenamingList <- [for i in s.Split([|','|]) -> i.Trim()]
//        let setFormat = fun s ->
//            options.targetOutput <-
//                match s with
//                | "c-variables" -> CHeader
//                | "js" -> JS
//                | "c-array" -> CList
//                | "none" -> Text
//                | "nasm" -> Nasm
//                | s -> printfn "'%s' is not a valid format" s; options.targetOutput
//        ["-o", ArgType.String (fun s -> options.outputName <- s), "Set the output filename (default is shader_code.h)"
//         "-v", ArgType.Unit (fun() -> options.verbose<-true), "Verbose, display additional information"
//         "--hlsl", ArgType.Unit (fun() -> options.hlsl<-true), "Use HLSL (default is GLSL)"
//         "--format", ArgType.String setFormat, "Can be: c-variables (default), c-array, js, nasm, or none"
//         "--field-names", ArgType.String setFieldNames, "Choose the field names for vectors: 'rgba', 'xyzw', or 'stpq'"
//         "--preserve-externals", ArgType.Unit (fun() -> options.preserveExternals<-true), "Do not rename external values (e.g. uniform)"
//         "--preserve-all-globals", ArgType.Unit (fun() -> options.preserveAllGlobals<-true; options.preserveExternals<-true), "Do not rename functions and global variables"
//         "--no-renaming", ArgType.Unit (fun() -> options.noRenaming<-true), "Do not rename anything"
//         "--no-renaming-list", ArgType.String noRenamingFct, "Comma-separated list of functions to preserve"
//         "--no-sequence", ArgType.Unit (fun() -> options.noSequence<-true), "Do not use the comma operator trick"
//         "--smoothstep", ArgType.Unit (fun() -> options.smoothstepTrick<-true), "Use IQ's smoothstep trick"
//         "--", ArgType.Rest options.filenames.Add, "Stop parsing command line"
//        ] |> List.map ArgInfo
//
//    ArgParser.Parse(specs, options.filenames.Add)
//
//    if options.filenames.Count = 0 then
//        printHeader()
//        ArgParser.Usage(specs, usage = "Please give the shader files to compress on the command line.")
//        myExit 1
//    if options.filenames.Count > 1 && not options.preserveExternals then
//        printfn "When compressing multiple files, you must use the --preserve-externals option."
//        myExit 1
//    if options.verbose then printHeader()

    ////> wip
