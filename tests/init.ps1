taskkill /im tgitcache.exe
remove-item -Recurse -Force titi
New-Item -itemtype Directory -path titi

fullbuild set config PackageGlobalCache c:\PackageGlobalCache
fullbuild set config RepoType Git
fullbuild set config RepoUrl https://github.com/pchalamet/cassandra-sharp-full-build

fullbuild init workspace titi

push-location titi

fullbuild add git repo cassandra-sharp from https://github.com/pchalamet/cassandra-sharp
fullbuild add git repo cassandra-sharp-contrib from https://github.com/pchalamet/cassandra-sharp-contrib
fullbuild clone repo * 

pop-location
