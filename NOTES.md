# Some words before
full-build just replaces project reference (ProjectReference) and NuGet assembly references (Reference) with an indirection (an MSBuild target).
This target allows a lot of possibilities:
* stop referencing NuGet directly. Not a big deal bu valuable when you are updating all your NuGet. There is no reason for project to be changed that much. Also it's clearer to import a package - you do not need all it's references, only root NuGet.
* small solution file. The problem with project references are that you need to compile and have them in your projet to build your project. As projects are being bigger, you care less and less of all dependencies. People then switch to internal NuGet, and then start the NuGet hell. No way to build all your sources from scratch without dealing with the dependency graph. Replacing project references with target files then allow to either use a full fledge ProjectReference if you want sources or just a Reference if you just need it for compiling. Faster, clearer and task focused.

# View
A view (which is just a name for a project subset) is just a collection of project you want to have in your solution.
This is basically 2 things:
* an sln file to work with (Visual|Xamarin) Studio
* a target file which defines which project should be seen as source or binary


# ProjectReference
A project reference lies inside *proj file. This is replaced by a target file too which in turn use the target of the view to decide how to import a project.



Once converted, a project reference is only a project Import:




# Reference
full-build detect references to NuGet and is able to replace a whole chunk of Refences to a single Import to a target.




# NuGet
full-build used to deal with NuGet directly. With so many API quirks, this has been removed. As of now, full-build use Paket in a very controlled manner.
Paket screwed all projects - it puts insane references everywhere. it is only used to get NuGet from a paket.references stored in .full-build folder.
Packages are then post-processed to generate a target file to be used by projects.

Do not even try to use Paket with full-build, it will be a disaster.
Paket is only supported when converting projects.

# baseline
When dealing with several repositories progressing at different speed, we sometimes need to view the past with a specific version.
.full-build acts as the main repository. A version from this repo is a global revision number. The anthology file (in .full-build\baseline) stores the hash of all repositories.
To go back in past, just checkout .full-build repository. Read the baseline file and then again, checkout with specified version specified repository.
(This is a kind of pointer indirection).



