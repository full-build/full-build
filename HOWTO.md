# Setup
* configure .full-build in user home
* setup a git or mercurial master repository
* setup a binary share using appropriate security : 
  * CI must be able to write to this share and is the only trusted client
  * Developers should have only readonly access to binary share                                                   

# Before going on
It is useful to recall full-build does use a central binary repository to allow partial builds.
This puts some contraints on the way you build your projects. There are two point of attention as everything ends up in the same folder (more precisely workspace/bin) you have to ensure copy'ed files do not get overriden accross project.

# Configure from scratch
## master repository
First of all, create an empty repository (either Git or Mercurial). This will be the master repository.
Ensure developpers have read access. Allow write access to people in charge of configuration.

# ssh keys
TO DO

## master artifacts
Then create a public share. Full build artifacts will be pushed there and should be available to anyone.
Ensure developpers have read access. Allow write access to people in charge of configuration (including CI).

## anthology configuration
An anthology is the term used in full-build to describe the universe of projects, nugets and assemblies. full-build tracks everything is order to allow you to create smaller build based on the full build.

Create a new workspace using command:
  fullbuild setup <local folder> <master repository> <master artifacts>

Under <local folder>, full-build has initialized everything to start a new anthology configuration from scratch.
Now, you can start adding external repositories. They have to be converted later to be compatible with full-build.

Add a new repository using command:
  fullbuild add repo <name> <url>

<name> is the nickname you want to set for your repository.
<url> is where your repository can be found

Now clone your repository before converting:
  fullbuild clone <name>

<name> is a wildcard (* supported)

A moment later, all sources are cloned. It is time to convert to full-build using command:
  fullbuild convert

If everything is ok, you have now successfuly added projects to the anthology.
You can now start building and check if everything is ok:
  fullbuild add view add *
  fullbuild build all

Eventually commit & push everything:
  fullbuild push

# How to configure Continuous Integration for full build
CI role is to build all sources.

Following steps must be orchestrated on CI:
* clone master repository (fullbuild init <folder> <master repository>)
* cd <folder>
* clone all respositories using full-build (fullbuild clone *)
* generate a view with all sources (fullbuild add view all *)
* build all sources (fullbuild build all)
* generate a new version (fullbuild fullbuild push)

# Partial build
Developer environment or CI partial build

* clone master repository (fullbuild init <folder> <master repository>)
* cd <folder>
* clone required repositories (fullbuild clone <repoName>)
* build a view with all sources (fullbuild add view mypartialview *)
* build all sources (fullbuild build mypartialview)

