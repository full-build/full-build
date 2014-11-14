full-build is a workspace management for .net.

With full-build, you can:
- link a workspace to several git or mercurial repositories
- build from sources all your projects in one shot
- build a single repository (or more) against all other projects but as binary imports - focus on specific projects
- switch from binary to source and vis-versa at will
- generate views for repositories of your interest (artifact is a solution file you can use with Visual Studio) while maintaining whole workspace consistency
- promote automatically binary or NuGet references to real project sources (if sources or available in on of your repositories)
- manage NuGet packages for the entire workspace - not at project level
- get rid of NuGet hell, slowness and risky packages upgrade
- get rid of Paket mess (projects file won't be modified if a package is upgraded) - source references are clean
- still use packages from NuGet or private NuGet
- migration from NuGet and Paket supported
- checkout sources/binaries corresponding to a specific build version across all your repositories
