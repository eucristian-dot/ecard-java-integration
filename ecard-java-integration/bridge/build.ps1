param(
    [string]$SdkDir = (Join-Path $PSScriptRoot '..\..\eCard.SDK.1.3.0.4'),
    [string]$OutputDir = (Join-Path $PSScriptRoot 'bin')
)

$ErrorActionPreference = 'Stop'

$resolvedSdkDir = (Resolve-Path -LiteralPath $SdkDir).Path
$resolvedOutputDir = New-Item -ItemType Directory -Force -Path $OutputDir

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}

if (-not (Test-Path -LiteralPath $csc)) {
    throw 'Cannot find .NET Framework csc.exe. Install/enable .NET Framework 4.x developer tools.'
}

$refs = @(
    (Join-Path $resolvedSdkDir 'Ceas.eCard.SDK.dll'),
    (Join-Path $resolvedSdkDir 'Newtonsoft.Json.dll'),
    (Join-Path $resolvedSdkDir 'BouncyCastle.Crypto.dll'),
    'System.dll',
    'System.Core.dll'
)

foreach ($ref in $refs) {
    if ($ref.EndsWith('.dll') -and $ref.Contains('\') -and -not (Test-Path -LiteralPath $ref)) {
        throw "Missing reference: $ref"
    }
}

$outExe = Join-Path $resolvedOutputDir.FullName 'ECardBridge.exe'

& $csc `
    /nologo `
    /target:exe `
    /platform:anycpu `
    /optimize+ `
    "/out:$outExe" `
    "/reference:$($refs[0])" `
    "/reference:$($refs[1])" `
    "/reference:$($refs[2])" `
    /reference:System.dll `
    /reference:System.Core.dll `
    (Join-Path $PSScriptRoot 'ECardBridge.cs')

if ($LASTEXITCODE -ne 0) {
    throw "csc.exe failed with exit code $LASTEXITCODE"
}

Copy-Item -Force -LiteralPath (Join-Path $resolvedSdkDir 'Ceas.eCard.SDK.dll') -Destination $resolvedOutputDir.FullName
Copy-Item -Force -LiteralPath (Join-Path $resolvedSdkDir 'Newtonsoft.Json.dll') -Destination $resolvedOutputDir.FullName
Copy-Item -Force -LiteralPath (Join-Path $resolvedSdkDir 'BouncyCastle.Crypto.dll') -Destination $resolvedOutputDir.FullName

Write-Host "Built $outExe"
