[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$version = ((Get-Content (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -TotalCount 1) -split ': ')[1]
$editor = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"
if (!(Test-Path -LiteralPath $editor)) { throw "Unity $version is required at $editor" }
$builds = Join-Path $project 'Builds'
[void][IO.Directory]::CreateDirectory($builds)
$log = Join-Path $builds 'WindowsBuild.log'

function Build-Player {
    $arguments = "-batchmode -quit -projectPath ""$project"" -executeMethod ReviewBuild.Windows -logFile ""$log"""
    (Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru).ExitCode
}

$result = Build-Player
if ($result -ne 0 -and $version -eq '6000.5.5f1' -and (Select-String -LiteralPath $log -SimpleMatch "error CS0619: 'SerializedProperty.objectReferenceInstanceIDValue'" -Quiet)) {
    # The Beta lockfile's editor-only MCP package selects a removed API on Unity 6000.5.
    $codec = Join-Path $project 'Library/PackageCache/com.kitwright.unity.mcp@e67728296742/Editor/Tools/Helpers/ObjectIdCodec.cs'
    $source = [IO.File]::ReadAllText($codec)
    $guard = '(?m)^#if UNITY_6000_6_OR_NEWER(?=\r?\n +return property\.objectReferenceEntityIdValue\.IsValid\(\);)'
    if ([regex]::Matches($source, $guard).Count -ne 1) { throw "Unexpected KitWright source. See $log" }
    [IO.File]::WriteAllText($codec, [regex]::Replace($source, $guard, '#if UNITY_6000_5_OR_NEWER'))
    $result = Build-Player
}
if ($result -ne 0) { throw "Build failed ($result). See $log" }
if (!(Select-String -LiteralPath $log -SimpleMatch 'WINDOWS BUILD Succeeded errors=0' -Quiet)) {
    throw "Unity exited without confirming a successful player build. See $log"
}
Write-Output (Join-Path $builds 'Windows/DevilsHarvest.exe')
