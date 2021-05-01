module tests

open System
open System.IO
open System.Diagnostics

let binaryFilename = "src/bin/Debug/net5.0/ShaderMinifier-rider.dll"

let testFiles = Directory.GetFiles("tests-output/", "*.*", SearchOption.AllDirectories)
                |> Array.filter (fun f -> not(f.EndsWith(".expected")))
                |> Array.filter (fun f -> not(f.EndsWith(".actual")))
                |> Array.filter (fun f -> not(f.EndsWith(".unsupported")))
let quoteArgs = fun s -> "\"" + s + "\""

let runTest (filename: string) =
    let args = binaryFilename + " " + //"--no-sequence" + " " +
                (if filename.EndsWith(".hlsl") then "--hlsl" + " " else "") +
                (String.concat " " (List.map quoteArgs [ filename; "-o"; filename + ".actual" ]))
    let p = Process.Start("dotnet", args)
    p.WaitForExit()
    if p.ExitCode <> 0 then
        printfn "*** error while testing %s: program returned %i" filename p.ExitCode
        printfn "\n"
    p.ExitCode <> 0

let () =
    if not(File.Exists(binaryFilename)) then
        printfn "You need to compile %s first!" binaryFilename
        exit 1
    let stopwatch = Stopwatch.StartNew()
    printfn "Running %i tests..." (testFiles.Length)
    let mutable failedTestCount = 0
    for filename in testFiles do
        failedTestCount <- failedTestCount + if runTest filename then 1 else 0
    printfn "%i tests run in %i seconds, %i failed." testFiles.Length (int(stopwatch.Elapsed.TotalSeconds)) failedTestCount
    ()

// rm -rf tests-output/ && cp -r tests tests-output
// time dotnet fsi --quiet --exec src/tests.fs && zsh -c 'for i in **/*.expected; do echo \# diff "$i" "${i/.expected/.actual/}"; diff "$i" <(sed s/_ACTUAL_/_EXPECTED_/ "${i/.expected/.actual}") || echo "FAILED: ${i/.expected/}"; done'
// zsh -c 'echo rm **/*.actual'
// attention, ne gère pas "--hlsl" : zsh -c 'rm **/*.expected; for i in tests-output/**/*.*; do echo "$i"; dotnet bin/Debug/net5.0/ConsoleApp1.dll "$i" -o "$i.expected"; done'

// list of tests using --smoothstep
//      unit/smoothstep.frag real/slisesix.frag real/relief_tunnel.frag real/elevated.hlsl
//      real/radial_blur.frag real/postprocessing.frag real/lunaquatic.frag real/extatique/progress.frag real/extatique/blitsquare.frag real/extatique/blitgirl.frag

// zsh -c './minifier.sh --preserve-externals -o "" tests-output/**/*.{frag,glsl}'
