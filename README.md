full-build: workspace management for .NET enterprise projects
=============================================================
[![build status](https://ci.appveyor.com/api/projects/status/github/pchalamet/full-build?branch=master)](https://ci.appveyor.com/project/pchalamet/full-build)

Check out [full-build.io](http://full-build.io)

full-build is a enterprise grade workspace management for .net developers (pfeew !).

It provides following benefits:
* Manage teams repositories (git or mercurial) as single workspace
* Handle NuGet packages consistency at workspace level
* Allow a full consistent build (all sources + NuGet) for CI
* Focus developer on selected repositories (local builds based on full build outputs)
* Promote code review and low-coupling between developers