// include Fake lib
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake

RestorePackages()

// Properties
let buildDir = "./build/"
let deployDir = "./deploy/"
let projName = "MyTy.Blog.Web"

// Targets
Target "Clean" (fun _ ->
    CleanDir buildDir
)

Target "BuildApp" (fun _ ->
    !! "src/**/*.csproj"
      |> MSBuildRelease buildDir "Build"
      |> Log "AppBuild-Output: "
)

Target "Zip" (fun _ ->
    !! (buildDir + "_PublishedWebsites/" + projName + "/**/*.*")
        -- "*.zip"
        |> Zip (buildDir + "_PublishedWebsites/" + projName + "/") (projName + ".zip")
)

// Dependencies
"Clean"
  ==> "BuildApp"
  ==> "Zip"

// start build
RunTargetOrDefault "Zip"
