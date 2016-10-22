$branch = $env:APPVEYOR_REPO_BRANCH
If ($branch -eq "master" -And $env:APPVEYOR_REPO_TAG_NAME) { $version = $env:APPVEYOR_REPO_TAG_NAME }
If ($branch -eq "develop") { $version = "v999.0" }
If ($branch -like "release/*") { $version = $branch }
If ($branch -like "feature/*") { $version = "v0.0" }
$posAfterVchar = $branch.LastIndexOf("v") + 1
$versionLength = $branch.Length - $posAfterVchar
$version = $version.substring($posAfterVchar, $versionLength)
$newVersion = "$version.$env:APPVEYOR_BUILD_NUMBER"
Write-Host "Building version $newVersion"
