[CmdletBinding()]
param(
    [string]$ProjectPath = "src/TokenIDP.Server/TokenIDP.Server.csproj",
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts/nuget",
    [string]$BaseVersion = "0.1.0",
    [string]$Version = "",
    [bool]$PackDependencies = $false,
    [switch]$Publish,
    [string]$Source = "https://api.nuget.org/v3/index.json",
    [string]$ApiKey = $env:NUGET_API_KEY,
    [switch]$SkipDuplicate = $true
)

$ErrorActionPreference = "Stop"
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -Scope Global -ErrorAction SilentlyContinue) {
    $Global:PSNativeCommandUseErrorActionPreference = $false
}

function Invoke-Git {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & git @Arguments 2>$null
        if ($LASTEXITCODE -ne 0) {
            return ""
        }

        return ($output | Select-Object -First 1)
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Get-PackageVersion {
    param(
        [string]$RequestedVersion,
        [string]$RequestedBaseVersion
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedVersion)) {
        return $RequestedVersion.Trim()
    }

    if (-not [string]::IsNullOrWhiteSpace($env:PACKAGE_VERSION)) {
        return $env:PACKAGE_VERSION.Trim()
    }

    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_REF_NAME) -and
        $env:GITHUB_REF -like "refs/tags/*" -and
        $env:GITHUB_REF_NAME -match "^v?(\d+\.\d+\.\d+(?:\.\d+)?(?:-[0-9A-Za-z][0-9A-Za-z.-]*)?)$") {
        return $Matches[1]
    }

    $exactTag = Invoke-Git -Arguments @("describe", "--tags", "--exact-match", "HEAD")
    if ($exactTag -match "^v?(\d+\.\d+\.\d+(?:\.\d+)?(?:-[0-9A-Za-z][0-9A-Za-z.-]*)?)$") {
        return $Matches[1]
    }

    $base = if (-not [string]::IsNullOrWhiteSpace($env:PACKAGE_BASE_VERSION)) {
        $env:PACKAGE_BASE_VERSION.Trim()
    }
    else {
        $RequestedBaseVersion.Trim()
    }

    if ($base -notmatch "^\d+\.\d+\.\d+(?:\.\d+)?$") {
        throw "BaseVersion must be numeric SemVer, for example 1.2.3. Actual: '$base'."
    }

    $stamp = [DateTime]::UtcNow.ToString("yyyyMMddHHmmss")
    $sha = Invoke-Git -Arguments @("rev-parse", "--short=8", "HEAD")
    if ([string]::IsNullOrWhiteSpace($sha)) {
        $sha = "local"
    }

    return "$base-ci.$stamp.$sha"
}

$repoRoot = Invoke-Git -Arguments @("rev-parse", "--show-toplevel")
if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    $repoRoot = (Resolve-Path ".").Path
}

$resolvedProject = Resolve-Path (Join-Path $repoRoot $ProjectPath)
$resolvedOutput = Join-Path $repoRoot $OutputDirectory
$packageVersion = Get-PackageVersion -RequestedVersion $Version -RequestedBaseVersion $BaseVersion
$repositoryCommit = Invoke-Git -Arguments @("rev-parse", "HEAD")
$dependencyProjects = @(
    "src/TokenIDP.Domain/TokenIDP.Domain.csproj",
    "src/TokenIDP.Core/TokenIDP.Core.csproj",
    "src/TokenIDP.Infrastructure/TokenIDP.Infrastructure.csproj",
    "src/TokenIDP.Workers/TokenIDP.Workers.csproj"
)

New-Item -ItemType Directory -Path $resolvedOutput -Force | Out-Null

Write-Host "Package version: $packageVersion"
Write-Host "Output: $resolvedOutput"

dotnet restore $resolvedProject
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed."
}

function Invoke-PackProject {
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolvedPath = Resolve-Path (Join-Path $repoRoot $Path)

    Write-Host "Packing $resolvedPath"

    $packArgs = @(
        "pack",
        $resolvedPath,
        "--configuration", $Configuration,
        "--no-restore",
        "--output", $resolvedOutput,
        "-p:PackageVersion=$packageVersion",
        "-p:Version=$packageVersion",
        "-p:ContinuousIntegrationBuild=true"
    )

    if (-not [string]::IsNullOrWhiteSpace($repositoryCommit)) {
        $packArgs += "-p:RepositoryCommit=$repositoryCommit"
    }

    dotnet @packArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed for $resolvedPath."
    }
}

if ($PackDependencies) {
    foreach ($dependencyProject in $dependencyProjects) {
        Invoke-PackProject -Path $dependencyProject
    }
}

Invoke-PackProject -Path $ProjectPath

$packages = Get-ChildItem -Path $resolvedOutput -Filter "*.$packageVersion.nupkg" |
    Sort-Object Name

if (@($packages).Count -eq 0) {
    throw "No packages were created for version $packageVersion."
}

Write-Host "Created packages:"
$packages | ForEach-Object { Write-Host "  $($_.FullName)" }

if ($Publish) {
    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        throw "Publishing requires an API key. Set NUGET_API_KEY or pass -ApiKey."
    }

    foreach ($package in $packages) {
        $pushArgs = @(
            "nuget", "push",
            $package.FullName,
            "--source", $Source,
            "--api-key", $ApiKey
        )

        if ($SkipDuplicate) {
            $pushArgs += "--skip-duplicate"
        }

        dotnet @pushArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet nuget push failed for $($package.FullName)."
        }
    }
    Write-Host "Published package to $Source"
}

Write-Host "PACKAGE_VERSION=$packageVersion"
Write-Host "PACKAGE_PATHS=$($packages.FullName -join ';')"

if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
    "package_version=$packageVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "package_paths=$($packages.FullName -join ';')" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
}
