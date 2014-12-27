// include Fake lib
#r @"../packages/FAKE/tools/FakeLib.dll"
open Fake

RestorePackages()

// Properties
let artifactDir = "./artifacts/"
let projName = "MyTy.Blog.Web"

// Targets
Target "Clean" (fun _ ->
    CleanDir artifactDir
)

Target "BuildApp" (fun _ ->
    !! "src/MyTy.Blog.Web/MyTy.Blog.Web.csproj"
      |> MSBuildRelease artifactDir "Build"
      |> Log "AppBuild-Output: "
)

Target "Zip" (fun _ ->
    !! (artifactDir + "_PublishedWebsites/" + projName + "/**/*.*")
        -- "*.zip"
        |> Zip (artifactDir + "_PublishedWebsites/" + projName + "/") (artifactDir + projName + ".zip")
)

// Dependencies
"Clean"
  ==> "BuildApp"
  ==> "Zip"

// start build
RunTargetOrDefault "Zip"
