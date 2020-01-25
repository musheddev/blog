module Build

open System
open System.IO
open Fake
open Fake.Tools.Git
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.Core
open Fake.Tools
open BlackFox.Fake


[<EntryPoint>]
let main argv =
    printfn "%A" argv
    BuildTask.setupContextFromArgv argv

    Target.initEnvironment ()

    let release = Fake.Core.ReleaseNotes.load "RELEASE_NOTES.md"

    let platformTool tool winTool =
        let tool = if Environment.isUnix then tool else winTool
        match ProcessUtils.tryFindFileOnPath tool with
        | Some t -> t
        | _ -> failwith (tool + " was not found in path. ")

    let nodeTool = platformTool "node" "node.exe"
    let yarnTool = platformTool "yarn" "yarn.cmd"

    let runTool cmd args workingDir =
        let arguments = args |> String.split ' ' |> Arguments.OfArgs
        Command.RawCommand (cmd, arguments)
        |> CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory workingDir
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    let runDotNet cmd workingDir =
        let result =
            DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
        if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

    let openBrowser url =
        //https://github.com/dotnet/corefx/issues/10361
        Command.ShellCommand url
        |> CreateProcess.fromCommand
        |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
        |> Proc.run
        |> ignore

    let installClient = BuildTask.createFn "InstallClient" [] (fun _ ->
        printfn "Node version:"
        runTool nodeTool "--version" __SOURCE_DIRECTORY__
        printfn "Yarn version:"
        runTool yarnTool "--version" __SOURCE_DIRECTORY__
        runTool yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
    )

    let build = BuildTask.createFn "Build" [installClient] (fun _ ->
        runTool yarnTool "webpack-cli -p" __SOURCE_DIRECTORY__
    )

    let run = BuildTask.createFn "Run" [installClient] (fun _ ->
        let client = async {
            runTool yarnTool "webpack-dev-server" __SOURCE_DIRECTORY__
        }
        let browser = async {
            do! Async.Sleep 5000
            openBrowser "http://localhost:8080"
        }

        let vsCodeSession = Environment.hasEnvironVar "vsCodeSession"

        let tasks =
            [ yield client
              if not vsCodeSession then yield browser ]

        tasks
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore
    )

    let releaseSite = BuildTask.createFn "Deploy" [build] (fun _ ->
        Shell.copyFile "Site/activeparsers.html" "Site/index.html"
        Shell.copyFile "Site/theming.html" "Site/index.html" 
        async {
            runTool yarnTool "gh-pages -d Site" __SOURCE_DIRECTORY__
        } |> Async.RunSynchronously
    )

    BuildTask.runOrDefault build
    0 // return an integer exit code
