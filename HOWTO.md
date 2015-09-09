# Setup
* configure .full-build in user home
* setup a git or mercurial master repository
* setup a binary share using appropriate security : 
  * CI must be able to write to this share and is the only trusted client
  * Developers should have only readonly access to binary share                                                   

# How to configure Continuous Integration for full build
CI role is to build all sources.

Following steps must be orchestrated on CI:
* clone master repository (fullbuild init <folder>)
* cd <folder>
* clone all respositories using full-build (fullbuild clone *)
* generate a view with all sources (fullbuild add view all *)
* build all sources (fullbuild build all)
* generate a new baseline (fullbuild baseline)

# Partial build (developer environment or CI partial build):
* clone master repository (fullbuild init <folder>)
* cd <folder>
* clone required repositories (fullbuild clone <repoName>)
* build a view with all sources (fullbuild add view partial *)
* build all sources (fullbuild build partial)

# Optimization
Eventually, consider cleaning the work folder using full-build (fullbuild rebase)
