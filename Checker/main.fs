open System.Runtime.InteropServices
open OpenTK.Graphics.OpenGL
open System
open System.Diagnostics
open System.IO
open System.Linq

let initOpenTK () =
    // OpenTK requires a GameWindow
    let _ = new OpenTK.GameWindow()
    ()

// Return true if the file can be compiled as a GLSL shader.
let canBeCompiled content =
    let fragmentShader = GL.CreateShader(ShaderType.FragmentShader)
    GL.ShaderSource(fragmentShader, content)
    GL.CompileShader(fragmentShader)
    let info = GL.GetShaderInfoLog(fragmentShader)
    GL.DeleteShader(fragmentShader)
    if info = "" then
        true
    else
        printfn "compilation failed: %s" info
        false

let doMinify content =
    Printer.printText(Main.minify("input", content))

let testMinifyAndCompile (file: string) =
    try
        let content = System.IO.File.ReadAllText file
        if not (canBeCompiled content) then
            printfn "Invalid input file '%s'" file
            false
        else
            let minified = doMinify content + "\n"
            if not (canBeCompiled minified) then
                printfn "Minification broke the file '%s'" file
                printfn "%s" minified
                false
            else
                printfn "Success: %s" file
                true
    with :? IO.FileNotFoundException as e ->
        printfn "%A" e
        false

let testPerformance files =
    printfn "Running performance tests..."
    let contents = files |> Array.map System.IO.File.ReadAllText
    let stopwatch = Stopwatch.StartNew()
    for str in contents do
        doMinify str |> ignore
    let time = stopwatch.Elapsed
    printfn "%i files minified in %f seconds." files.Length time.TotalSeconds

[<EntryPoint>]
let main argv =
    initOpenTK()
    assert Options.Globals.options.init([|"--format"; "text"; "fake.frag"|])
    let mutable failures = 0
    let unitTests = Directory.GetFiles("tests/unit", "*.frag", SearchOption.AllDirectories)
    let realTests = Directory.GetFiles("tests/real", "*.frag", SearchOption.AllDirectories)
    let allTests = Seq.concat [realTests; unitTests] |> Seq.toArray
    for f in allTests do
        if not (testMinifyAndCompile f) then
            failures <- failures + 1
    testPerformance allTests
    if failures = 0 then
        printfn "All good."
    else
        printfn "%d failures." failures

    if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
        System.Console.ReadLine() |> ignore
    if failures = 0 then 0 else 1
