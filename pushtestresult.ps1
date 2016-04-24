# upload results to AppVeyor
$wc = New-Object 'System.Net.WebClient'
$wc.UploadFile("https://ci.appveyor.com/api/testresults/nunit2/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\TestResults.xml))

