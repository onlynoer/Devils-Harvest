the current mcp package has an error for our version (6000.5...) to fix this open
library > packCache > com.kitwright.unity.mcp@85ba3bf664a6 > editor > tools > helpers > ObjectIdCodec.cs
and change I think line 68 to UNITY_6000_3_OR_NEWER

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