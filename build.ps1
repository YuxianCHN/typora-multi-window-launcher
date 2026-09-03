param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$compilerCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) {
    throw "The .NET Framework C# compiler was not found."
}

$buildDirectory = Join-Path $repoRoot "build"
$packageDirectory = Join-Path $buildDirectory "package"
$distDirectory = Join-Path $repoRoot "dist"
New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $distDirectory -Force | Out-Null

$commonSource = Join-Path $repoRoot "src\Common\TyporaLocator.cs"
$launcherSource = Join-Path $repoRoot "src\Launcher\Program.cs"
$installerSource = Join-Path $repoRoot "src\Installer\Program.cs"
$uninstallerSource = Join-Path $repoRoot "src\Uninstaller\Program.cs"

$launcher = Join-Path $packageDirectory "TyporaMultiWindowLauncher.exe"
$installer = Join-Path $packageDirectory "Install.exe"
$uninstaller = Join-Path $packageDirectory "Uninstall.exe"

& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /reference:System.Windows.Forms.dll /out:$launcher $commonSource $launcherSource
if ($LASTEXITCODE -ne 0) { throw "Launcher compilation failed." }

& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /reference:System.Windows.Forms.dll /out:$installer $commonSource $installerSource
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed." }

& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /reference:System.Windows.Forms.dll /out:$uninstaller $commonSource $uninstallerSource
if ($LASTEXITCODE -ne 0) { throw "Uninstaller compilation failed." }

Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination $packageDirectory -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "LICENSE") -Destination $packageDirectory -Force

$hashFiles = @($launcher, $installer, $uninstaller)
$hashLines = $hashFiles | ForEach-Object {
    $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $_
    $hash.Hash + "  " + (Split-Path -Leaf $_)
}
$hashPath = Join-Path $packageDirectory "SHA256SUMS.txt"
$hashLines | Set-Content -LiteralPath $hashPath -Encoding Ascii

$archive = Join-Path $distDirectory ("TyporaMultiWindowLauncher-v" + $Version + ".zip")
$packageFiles = @($launcher, $installer, $uninstaller, (Join-Path $packageDirectory "README.md"), (Join-Path $packageDirectory "LICENSE"), $hashPath)
Compress-Archive -LiteralPath $packageFiles -DestinationPath $archive -Force

Write-Host "Built: $archive"
