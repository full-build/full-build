# Before you file a bug...
  * Did you [read the documentation](https://github.com/pchalamet/full-build/wiki)?
  * Did you search the issues list to see if someone already reported it?
  * Did you create a simple repro for the problem?
  * If you check all previous steps then go forward and engage with us !

# Development workflow
  * Master is protected (no direct push)
  * Work and PR must be based from 'develop' branch (full-build use git flow)
  * Commit references issue number (eg: 'blah blah #42')
  * Patch is developed in branch or cloned repository and requires appveyor + ff merge
  * Merge will be squashed
  * Official release come only from master branch
  * Manual step to disable prerelease flag (ie: publish latest version).

# Before you submit a PR...
  * Did you ensure this is an [accepted up-for-grabs issue]? (If not, open one to start the discussion)
  * Does the code follow existing coding styles? (spaces, comments, no regions, etc.)?
  * Did you write unit tests?
  * Did you pass integration tests?
