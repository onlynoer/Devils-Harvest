the current mcp package has an error for our version (6000.5...) to fix this open
library > packCache > com.kitwright.unity.mcp@85ba3bf664a6 > editor > tools > helpers > ObjectIdCodec.cs
and change I think line 68 to UNITY_6000_3_OR_NEWER

when making a new branch or cloning the repo, if none of the assets are loading one must download them with
```bash
git lfs pull origin <branch name>
```