// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r "nuget: FAKE.Core.Target"
#r "nuget: FAKE.Core.ReleaseNotes"
#r "nuget: FAKE.DotNet.Cli"
#r "nuget: FAKE.DotNet.Fsi"
#r "nuget: FAKE.DotNet.AssemblyInfoFile"
#r "nuget: FAKE.Tools.Git"
#r "nuget: FAKE.DotNet.Testing.XUnit2"
#r "nuget: System.Reactive"
#r "nuget: MSBuild.StructuredLogger, 2.3.71"

let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []
Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet

// --------------------------------------------------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let projectName = "FsDiff"
let projectSummary = "F# automatic differentation library for scientific computing"

let projectDescription =
    """FsDiff is an automatic differentiation (AD) library.

AD allows exact and efficient calculation of derivatives, by systematically invoking the chain rule of calculus at the elementary operator level during program execution.

FsDiff supports nested forward and reverse AD up to any level. Derivative calculations (gradients, Hessians, Jacobians, directional derivatives, and matrix-free Hessian- and Jacobian-vector products) can be incorporated with minimal change into existing algorithms."""

let authors = "Atılım Güneş Baydin,Barak A. Pearlmutter, Andrew C. Martin"
let companyName = ""
let tags = "automatic-differentiation F# fsharp FsSci scientific-computing"
let license = "BSD-2-Clause"
let iconUrl = ""

let copyright =
    """Copyright (c) 2026-     Scott Polar Research Institute, University of Cambridge (Andrew Martin <am2288@cam.ac.uk>)

This project incorporates and modifies code originally distributed as DiffSharp.

Copyright (c) 2016-     University of Oxford (Atilim Gunes Baydin)
Copyright (c) 2017-     Microsoft Research, Cambridge, UK (Don Syme)
Copyright (c) 2014-     National University of Ireland Maynooth (Barak A. Pearlmutter)
Copyright (c) 2014-2016 National University of Ireland Maynooth (Atilim Gunes Baydin)"""

let packageProjectUrl = "https://github.com/ecolambda/FsDiff"
let repositoryType = "git"
let repositoryUrl = "https://github.com/ecolambda/FsDiff"
let repositoryContentUrl = "https://raw.githubusercontent.com/ecolambda/FsDiff"

// --------------------------------------------------------------------------------------
// The rest of the code is standard F# build script
// --------------------------------------------------------------------------------------

// Read release notes & version info from RELEASE_NOTES.md
System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let release =
    System.IO.File.ReadLines "RELEASE_NOTES.MD" |> Fake.Core.ReleaseNotes.parse

// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" (fun _ ->
    let fileName = "src/FsDiff/AssemblyInfo.fs"

    AssemblyInfoFile.createFSharpWithConfig
        fileName
        [
            Fake.DotNet.AssemblyInfo.Title projectName
            Fake.DotNet.AssemblyInfo.Company companyName
            Fake.DotNet.AssemblyInfo.Product projectName
            Fake.DotNet.AssemblyInfo.Description projectSummary
            Fake.DotNet.AssemblyInfo.Version release.AssemblyVersion
            Fake.DotNet.AssemblyInfo.FileVersion release.AssemblyVersion
        ]
        (AssemblyInfoFileConfig(false)))

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target.create "Clean" (fun _ ->
    Fake.IO.Shell.cleanDirs [ "bin"; "output" ]
    Fake.IO.Shell.cleanDirs [ "tests/FsDiff.Tests/bin"; "tests/FsDiff.Tests/obj" ])

// --------------------------------------------------------------------------------------
// Check formatting with Fantomas

Target.create "CheckFormat" (fun _ ->
    let result = DotNet.exec id "fantomas" "./src --check"

    if result.ExitCode = 0 then
        Trace.log "No files need formatting"
    elif result.ExitCode = 99 then
        failwith "Some files need formatting, run 'dotnet run fantomas' to resolve this."
    else
        Trace.logf "Errors while formatting: %A" result.Errors)

// --------------------------------------------------------------------------------------
// Build library & test project

Target.create "Build" (fun _ ->
    Trace.log " --- Building the library --- "
    Fake.DotNet.DotNet.build id "src/FsDiff/FsDiff.fsproj")

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete

Target.create "RunTests" (fun _ ->
    let result =
        Fake.DotNet.DotNet.exec
            (fun args ->
                { args with
                    Verbosity = Some Fake.DotNet.DotNet.Verbosity.Normal
                })
            "run"
            ("--project tests/FsDiff.Tests/FsDiff.Tests.fsproj")

    if result.ExitCode <> 0 then
        failwith "Tests failed")

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "NuGet" (fun _ ->
    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let projectDescription =
        projectDescription.Replace("\r", "").Replace("\n", "").Replace("  ", " ")

    // Format the release notes
    let releaseNotes = release.Notes |> String.concat "\n"

    let properties =
        [
            ("Version", release.NugetVersion)
            ("Authors", authors)
            ("PackageProjectUrl", packageProjectUrl)
            ("PackageTags", tags)
            ("RepositoryType", repositoryType)
            ("RepositoryUrl", repositoryUrl)
            ("PackageLicenseExpression", license)
            ("PackageRequireLicenseAcceptance", "false")
            ("PackageReleaseNotes", releaseNotes)
            ("Summary", projectSummary)
            ("PackageDescription", projectDescription)
            ("EnableSourceLink", "true")
            ("PublishRepositoryUrl", "true")
            ("EmbedUntrackedSources", "true")
            ("IncludeSymbols", "true")
            ("IncludeSymbols", "false")
            ("SymbolPackageFormat", "snupkg")
            ("Copyright", copyright)
        ]

    DotNet.pack
        (fun p ->
            { p with
                Configuration = DotNet.BuildConfiguration.Release
                OutputPath = Some "bin"
                MSBuildParams =
                    { p.MSBuildParams with
                        Properties = properties
                    }
            })
        "src/FsDiff/FsDiff.fsproj")

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target.create "All" ignore

"Clean" ==> "CheckFormat" ==> "AssemblyInfo" ==> "Build"
"Build" ==> "NuGet" ==> "All"
"Build" ==> "RunTests" ==> "All"

Target.runOrDefault "All"
