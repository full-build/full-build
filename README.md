# full-build: smart build system for .net

full-build is a smart build system allowing to either compile all your projects in one shot or to define on the fly small solution files to build parts of your system individually without building everything else.

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

Check out [full-build.io](http://full-build.io)

# build status

Platform|Status
--------|------
.net 4.5|[![build status](https://ci.appveyor.com/api/projects/status/github/full-build/full-build?branch=master)](https://ci.appveyor.com/project/pchalamet/full-build/branch/master)

# how to build
* On windows
    * Install .net 4.5
    * Install F# 4
    * Install Nunit > 3.0 (http://www.nunit.org/index.php?p=download)
    * Add Nunit nunit3-console.exe to your environment variables 
       * C:\Program Files (x86)\NUnit.org\nunit-console
    * Add MSBuild to your environment variables 
        * .net v4.0 x86 : C:\Windows\Microsoft.NET\Framework\v4.0.30319
        * .net v4.0 x64 : C:\Windows\Microsoft.NET\Framework64\v4.0.30319
	* Add Nuget to your environment variables
		* nuget.exe cli : https://dist.nuget.org/index.html
    * Run build-all.cmd

* On Linux/OSX
    * Install .net 4.5
    * Install F# 4
    * Run build-all.sh

Once build is done, binaries are in refbin folder.

Note that before using Visual Studio or Xamarin Studio, you have to compile first as this setup development environment. Solution fullbuild.sln can then be used.

# contribution
Contributions are welcomed. Ensure you have read CONTRIBUTING.md and LICENSE.txt before sending PR.


# licence
   Copyright 2014-2016 Pierre Chalamet

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
