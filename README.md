# About
A simple C# command-line GitHub's pull-request manipulator

# Configuration

1. Compile
2. Add the directory of **pr.exe** to **PATH**
3. Edit **pr.exe.config** and enter the following values:

* **GitHubRepo** - The repo to work with e.g: **angular/angularjs-batarang**
* **ApiRoot**
  * For github: **https://api.github.com/repos/** 
  * For corporate GitHub: **https://HOST/api/v3/repos/**
* **Token** - The token can be generated, in case of Github at: https://github.com/settings/tokens/new
* **MasterBranch** - The master development branch, e.g: **master**

#Usage

```
Command line pull requests 0.1
Copyright (C) 2014 Vitaly Belman

API root:       https://pdihub.hi.inet/api/v3/repos/
GitHub repo:    tugo/tugo-web-client

List PRs: pr -l
Create PR: pr -c -b feature/my-new-feature

  -l, --list        List open PRs

  -c, --create      Create a new PR

  -o, --open-web    Open the PR page

  --sha             Get the HEAD SHA of the PR

  --pr-id           ID of the PR to manipulate

  -b, --branch      The name of the branch

  --help            Display this help screen.
  ```
