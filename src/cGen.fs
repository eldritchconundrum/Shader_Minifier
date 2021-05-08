module Formatter

open System
open System.IO
open Options.Globals

// Values to export in the C code (uniform and attribute values)
let mutable private exportedValues = ([] : (string * string * string) list)

// 'ty' is a prefix for the type of shader param. Nothing (VAR) for vars, "F" for hlsl functions
let export ty name (newName:string) =
    if newName.[0] <> '0' then
        exportedValues <- exportedValues |> List.map (fun (ty2, name2, newName2 as arg) ->
            if ty = ty2 && name = newName2
            then ty, name2, newName
            else arg
        )
    else
        exportedValues <- (ty, name, newName) :: exportedValues

let private header = sprintf "File generated with Shader Minifier %s" Options.version
let private url = "http://www.ctrl-alt-test.fr"

let private formatC out data asAList =
    let fileName =
        if options.outputName = "" || options.outputName = "-" then "shader_code.h"
        else Path.GetFileName options.outputName
    let macroName = fileName.Replace(".", "_").ToUpper() + "_"

    fprintfn out "/* %s" header
    fprintfn out " * %s" url
    fprintfn out " */"

    if not asAList then
        fprintfn out "#ifndef %s" macroName
        fprintfn out "# define %s" macroName

    for ty, name, newName in List.sort exportedValues do
        // let newName = Printer.identTable.[int newName]
        if ty = "" then
            fprintfn out "# define VAR_%s \"%s\"" (name.ToUpper()) newName
        else
            fprintfn out "# define %c_%s \"%s\"" (System.Char.ToUpper ty.[0]) (name.ToUpper()) newName

    fprintfn out ""
    for file : string, code in data do
        let name = (Path.GetFileName file).Replace(".", "_")
        if asAList then
            fprintfn out "// %s" file
            fprintfn out "\"%s\"," code
        else
            fprintfn out "const char *%s =%s \"%s\";" name Environment.NewLine code
        fprintfn out ""

    if not asAList then fprintfn out "#endif // %s" macroName

let private formatRaw out data =
    let str = [for _, code in data -> code] |> String.concat "\n"
    fprintf out "%s" str

let private formatJS out data =
    fprintfn out "/* %s" header
    fprintfn out " * %s" url
    fprintfn out " */"

    for ty, name, newName in List.sort exportedValues do
        if ty = "" then
            fprintfn out "var var_%s = \"%s\"" (name.ToUpper()) newName
        else
            fprintfn out "var %c_%s = \"%s\"" (Char.ToUpper ty.[0]) (name.ToUpper()) newName

    fprintfn out ""
    for file : string, code in data do
        let name = (Path.GetFileName file).Replace(".", "_")
        fprintfn out "var %s =%s \"%s\"" name Environment.NewLine code
        fprintfn out ""

let private formatNasm out data =
    fprintfn out "; %s" header
    fprintfn out "; %s" url

    for ty, name, newName in List.sort exportedValues do
        if ty = "" then
            fprintfn out "_var_%s: db '%s', 0" (name.ToUpper()) newName
        else
            fprintfn out "_%c_%s: db '%s', 0" (Char.ToUpper ty.[0]) (name.ToUpper()) newName

    fprintfn out ""
    for file : string, code in data do
        let name = (Path.GetFileName file).Replace(".", "_")
        fprintfn out "_%s:%s\tdb '%s', 0" name Environment.NewLine code
        fprintfn out ""

let format out filenamesAndMinifiedPairs = function
    | Options.Text -> formatRaw out filenamesAndMinifiedPairs
    | Options.CHeader -> formatC out filenamesAndMinifiedPairs false
    | Options.CList -> formatC out filenamesAndMinifiedPairs true
    | Options.JS -> formatJS out filenamesAndMinifiedPairs
    | Options.Nasm -> formatNasm out filenamesAndMinifiedPairs
