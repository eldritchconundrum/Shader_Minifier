module Main

open System
open System.IO
open Options.Globals

let mutable identTable: string[] = [||] // carried on from one file to the next? is this on purpose?
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
     
    Printer.printMode <- Printer.SingleChar
    let codes = Renamer.renameTopLevel codes Renamer.Unambiguous identTable
    identTable <- computeFrequencyIdentTable codes
    Renamer.computeContextTable codes

    Printer.printMode <- Printer.FromTable
    let codes = Renamer.renameTopLevel codes Renamer.Context identTable
    codes

let minifyFile filename =
    let (streamName, content) =
        if filename = ""
        then ("stdin", (new StreamReader(Console.OpenStandardInput())).ReadToEnd())
        else (filename, (new StreamReader(filename)).ReadToEnd())
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

let () = // parse argv, then process filenames
    try
        if System.Diagnostics.Debugger.IsAttached then
            options.outputName <- ""
            options.preserveExternals <- true
            let filenames = [|
                "tests/real/chocolux.frag"
                "tests/real/clod.frag"
                "tests/real/deform.frag"
                "tests/real/disco.frag"
                "tests/real/extatique/blit2.frag"
                "tests/real/extatique/blitcorner.frag"
                "tests/real/extatique/blit.frag"
                "tests/real/extatique/blitgirl.frag"
                "tests/real/extatique/blitsquare.frag"
                "tests/real/extatique/distort.frag"
                "tests/real/extatique/final.frag"
                "tests/real/extatique/font.frag"
                "tests/real/extatique/glow.frag"
                "tests/real/extatique/graindo12.frag"
                "tests/real/extatique/lambert2.frag"
                "tests/real/extatique/lambert.frag"
                "tests/real/extatique/loading.frag"
                "tests/real/extatique/log.frag"
                "tests/real/extatique/particle.frag"
                "tests/real/extatique/progress.frag"
                "tests/real/extatique/put.frag"
                "tests/real/extatique/raymarch.frag"
                "tests/real/extatique/scene30.frag"
                "tests/real/extatique/scene40.frag"
                "tests/real/extatique/scene45.frag"
                "tests/real/extatique/skybox2.frag"
                "tests/real/extatique/skybox.frag"
                "tests/real/extatique/snake.frag"
                "tests/real/extatique/water.frag"
                "tests/real/fly.frag"
                "tests/real/gfx monitor/distancefieldRaytrace.frag"
                "tests/real/gfx monitor/gradation.frag"
                "tests/real/heart.frag"
                "tests/real/julia.frag"
                "tests/real/kaleidoscope.frag"
                "tests/real/kinder_painter.frag"
                "tests/real/leizex.frag"
                "tests/real/lunaquatic.frag"
                "tests/real/mandelbulb.frag"
                "tests/real/mandel.frag"
                "tests/real/metatunnel.frag"
                "tests/real/monjori.frag"
                "tests/real/motion_blur.frag"
                "tests/real/multitexture.frag"
                "tests/real/postprocessing.frag"
                "tests/real/quaternion.frag"
                "tests/real/radial_blur.frag"
                "tests/real/relief_tunnel.frag"
                "tests/real/slisesix.frag"
                "tests/real/square_tunnel.frag"
                "tests/real/star.frag"
                "tests/real/sult.frag"
                "tests/real/to_the_road_of_ribbon.frag"
                "tests/real/tunnel.frag"
                "tests/real/twist.frag"
                "tests/real/z_invert.frag"
                "tests/unit/1.simple.frag"
                "tests/unit/2.simple.frag"
                "tests/unit/2.simple.opt.frag"
                "tests/unit/array.frag"
                "tests/unit/blocks.frag"
                "tests/unit/commas.frag"
                "tests/unit/empty_block.frag"
                "tests/unit/float.frag"
                "tests/unit/hexa.frag"
                "tests/unit/inline.frag"
                "tests/unit/keyword_prefix.frag"
                "tests/unit/numbers.frag"
                "tests/unit/overload.frag"
                "tests/unit/precedence.frag"
                "tests/unit/rename_conflict.frag"
                "tests/unit/simple.frag"
                "tests/unit/smoothstep.frag"
                "tests/unit/suffix.frag"
                "tests/real/the orange guy/ball_pixel.glsl"
                "tests/real/the orange guy/ball_vertex.glsl"
                "tests/real/the orange guy/bloom2_pixel.glsl"
                "tests/real/the orange guy/bloom2_vertex.glsl"
                "tests/real/the orange guy/bloom_pixel.glsl"
                "tests/real/the orange guy/bloom_vertex.glsl"
                "tests/real/the orange guy/dfc_pixel.glsl"
                "tests/real/the orange guy/dfc_vertex.glsl"
                "tests/real/the orange guy/final_pixel.glsl"
                "tests/real/the orange guy/final_vertex.glsl"
                "tests/real/the orange guy/font_pixel.glsl"
                "tests/real/the orange guy/font_vertex.glsl"
                "tests/real/the orange guy/logofinal_pixel.glsl"
                "tests/real/the orange guy/logofinal_vertex.glsl"
                "tests/real/the orange guy/orange_pixel.glsl"
                "tests/real/the orange guy/orange_vertex.glsl"
                "tests/real/the orange guy/particle_pixel.glsl"
                "tests/real/the orange guy/particle_vertex.glsl"
                "tests/real/the orange guy/put_pixel.glsl"
                "tests/real/the orange guy/puttexture_pixel.glsl"
                "tests/real/the orange guy/puttexture_vertex.glsl"
                "tests/real/the orange guy/put_vertex.glsl"
                "tests/real/the orange guy/scene10_vertex.glsl"
                "tests/real/the orange guy/tonemapping_pixel.glsl"
                "tests/real/the orange guy/tonemapping_vertex.glsl"
                "tests/real/the orange guy/tunnel_pixel.glsl"
                "tests/real/the orange guy/tunnel_vertex.glsl"
            |]
            options.filenames.AddRange(Array.map (fun s -> "../../../" + s) filenames)
        else
            Options.parse ()
        // first minify every file, then print minified files
        let codes = Array.map minifyFile (options.filenames.ToArray())
        CGen.print (Array.zip (options.filenames.ToArray()) codes) options.targetOutput
        myExit 0
    with e -> printfn "%s" (e.ToString())
              myExit 1
