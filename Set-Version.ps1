$branch = $env:APPVEYOR_REPO_BRANCH
if($branch -eq 'master') {
    $version = $env:APPVEYOR_REPO_TAG_NAME
} 
# develop (alpha)
elseif ($branch -eq 'develop') {
    $version = '999.999.999'
}
# release 
else {
    $posAfterVchar = $branch.LastIndexOf("v") + 1
    $versionLength = $branch.Length - $posAfterVchar
    $version = $branch.substring($posAfterVchar, $versionLength)
}

$newVersion = "$version.$env:APPVEYOR_BUILD_NUMBER"
Write-Host "Update appveyor build version to: $newVersion"

$env:APPVEYOR_BUILD_VERSION = "$newVersion"
Update-AppveyorBuild -Version "$newVersion"
