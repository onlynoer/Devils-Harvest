Unity 6000.5.5f1 and the locked KitWright package require a local compatibility fix after a fresh import. Close Unity. In Library/PackageCache/com.kitwright.unity.mcp@e67728296742/Editor/Tools/Helpers/ObjectIdCodec.cs, find HasSerializedObjectReferenceId and change the guard immediately before property.objectReferenceEntityIdValue.IsValid() from UNITY_6000_6_OR_NEWER to UNITY_6000_3_OR_NEWER. Reopen the project. A package cache rebuild can require this step again.

switching from main to a branch
first run
```bash
git branch
```
this should show you are on main and list branches available
if its not listed then run:
```bash
git branch -a
```
this will show all the branches that exists
make sure you see the branch you want to switch to
then run
```bash
git checkout --track origin/branchnamehere
```
then run
```bash
git fetch
git pull
```
and then run the lfs cmd below

when making a new branch or cloning the repo, if none of the assets are loading one must download them with
```bash
git lfs pull origin <branch name>
```