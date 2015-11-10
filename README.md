full-build: smart build system for .net
=======================================
master branch [![build status](https://ci.appveyor.com/api/projects/status/github/pchalamet/full-build?branch=master)](https://ci.appveyor.com/project/pchalamet/full-build/branch/master)

Check out [full-build.io](http://full-build.io)

full-build is a smart build system allowing to either compile all your projects in one shot or to define on the fly small solution files to build parts of your system individually without building everything else.


The full build is usually running on your CI server. This build produces all the artifacts required for partial builds.
Partial builds are running on development computers - based on artifacts produced by the full build.

To achieve this, full-build lets you define the universe of all projects (it's called an anthology) - it can be splitted among different repositiories (if you have different teams working on different products).
Note that projects are converted to work with full-build. They are still regular *proj using msbuild but are adapted in a way to allow partial builds (you can still use MsBuild or VisualStudio).

Once the anthology is built, developers can work using partial builds and still resynchronize with master build when then are required to.

full-build paradigms are based on the fact that:
- nuget must not be used to store teams artifacts - only external dependencies
- solution files are not good to manage enterprise point of view (global consistency) and developer point of view (local development)
- global consistency is required to ensure the whole system can be rebuilt from sources only


full-build provides following benefits:
* Manage teams repositories (git or mercurial) as single workspace
* Handle NuGet packages consistency at workspace level
* Allow a full consistent build (all sources + external NuGet) for CI
* Focus developer on selected repositories (local builds based on full build outputs)
* Promote code review and low-coupling between developers
