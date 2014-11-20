taskkill /im tgitcache.exe
robocopy titi toto /MIR

try
{
    push-location toto

    FullBuild index workspace
    copy ..\Template.csproj .full-build
    FullBuild convert projects
    FullBuild init view cs with cassandra-sharp cassandra-sharp-contrib
    FullBuild generate view cs
    FullBuild build view cs
    FullBuild exec "echo %FULLBUILD_REPO% & git log -n 1 && echo."
    FullBuild exec "git status"

    write-host *********************************************
    write-host SUCCESS
    write-host *********************************************
}
catch
{

    write-host *********************************************
    write-host FAILURE
    write-host *********************************************
}
finally
{
    Pop-Location
}
