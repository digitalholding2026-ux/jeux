<#
  Tools/publish.ps1 - publication obligatoire de fin de travail (voir AGENTS.md).

  Sequence : tests -> bump de version -> build APK -> commit -> push -> tag -> release GitHub.
  Echec propre (code de sortie 1) si un prerequis manque, avant de lancer le build autant que possible.

  Exemples :
    powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\publish.ps1 `
        -Message "feat(game): nouveau mode" -Version 0.3.0
    .\Tools\publish.ps1 -Message "docs: mise a jour BUILD.md" -SkipBuild
    .\Tools\publish.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [string]$Message,
    [string]$Version,
    [switch]$SkipTests,
    [switch]$SkipBuild,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $RepoRoot

function Fail([string]$m) {
    Write-Host ""
    Write-Host "ECHEC : $m" -ForegroundColor Red
    exit 1
}
function Step([string]$m) {
    Write-Host ""
    Write-Host "== $m ==" -ForegroundColor Cyan
}
function Info([string]$m) { Write-Host "   $m" }
function Run([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { Fail "commande en echec : $exe $($arguments -join ' ')" }
}

function Resolve-Tool([string]$Name, [string]$Fallback) {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    if ($Fallback -and (Test-Path -LiteralPath $Fallback)) { return $Fallback }
    return $null
}

Write-Host "== Color Flow - publication de fin de travail ==" -ForegroundColor Cyan
Info "Depot : $RepoRoot"

# --- Outils -----------------------------------------------------------------
$git    = Resolve-Tool 'git'    (Join-Path $env:LOCALAPPDATA 'Portable\git\cmd\git.exe')
$gh     = Resolve-Tool 'gh'     (Join-Path $env:LOCALAPPDATA 'Portable\gh\gh.exe')
$dotnet = Resolve-Tool 'dotnet' (Join-Path $env:LOCALAPPDATA 'Portable\dotnet\dotnet.exe')

if (-not $git) { Fail "git introuvable. Ajouter %LOCALAPPDATA%\Portable\git\cmd au PATH." }
if (-not $gh)  { Fail "gh (GitHub CLI) introuvable. Ajouter %LOCALAPPDATA%\Portable\gh au PATH." }
if (-not $dotnet) { Fail "dotnet introuvable. Ajouter %LOCALAPPDATA%\Portable\dotnet au PATH." }

# --- Environnement Android (session courante) --------------------------------
$sdk = Join-Path $env:LOCALAPPDATA 'Portable\android-sdk'
$jdk = Join-Path $env:LOCALAPPDATA 'Portable\jdk17'
if (-not $env:ANDROID_HOME -and (Test-Path -LiteralPath $sdk)) {
    $env:ANDROID_HOME = $sdk
    $env:ANDROID_SDK_ROOT = $sdk
}
if (-not $env:JAVA_HOME -and (Test-Path -LiteralPath $jdk)) { $env:JAVA_HOME = $jdk }
if (-not $env:ANDROID_NDK_HOME -and $env:ANDROID_HOME) {
    $ndk = Join-Path $env:ANDROID_HOME 'ndk\23.1.7779620'
    if (Test-Path -LiteralPath $ndk) {
        $env:ANDROID_NDK_HOME = $ndk
        $env:ANDROID_NDK_ROOT = $ndk
    }
}

# --- Authentification GitHub (avant toute chose couteuse) --------------------
if (-not $DryRun) {
    & $gh auth status 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Fail @"
GitHub CLI n'est pas authentifie.
Lancer une fois :  & "$gh" auth login --hostname github.com --git-protocol https --web
Puis relancer ce script.
"@
    }
    # git doit utiliser le jeton de gh pour pousser
    & $git config --local credential.https://github.com.helper "!`"$gh`" auth git-credential"
}

# --- Version ----------------------------------------------------------------
$settingsPath = Join-Path $RepoRoot 'ProjectSettings\ProjectSettings.asset'
if (-not (Test-Path -LiteralPath $settingsPath)) { Fail "ProjectSettings.asset introuvable." }
$settings = Get-Content -Raw -LiteralPath $settingsPath
$mVersion = [regex]::Match($settings, '(?m)^\s*bundleVersion:\s*(\S+)\s*$')
if (-not $mVersion.Success) { Fail "bundleVersion introuvable dans ProjectSettings.asset." }
$currentVersion = $mVersion.Groups[1].Value
$targetVersion = if ($Version) { $Version.TrimStart('v') } else { $currentVersion }
if ($targetVersion -notmatch '^\d+\.\d+\.\d+$') { Fail "version invalide '$targetVersion' (attendu X.Y.Z)." }
$tag = "v$targetVersion"

Info "Version courante : $currentVersion"
Info "Version cible    : $targetVersion (tag $tag)"

# --- Etat du depot ----------------------------------------------------------
$dirty = (& $git status --porcelain) -join "`n"
if ($dirty -and -not $Message -and -not $DryRun) {
    Write-Host $dirty
    Fail "Des modifications existent : -Message est obligatoire (format 'type(scope): resume')."
}

if ($DryRun) {
    Step "Mode -DryRun : aucune modification"
    Info "1. tests   : dotnet test Tools/CoreTests/CoreTests.csproj"
    Info "2. version : $currentVersion -> $targetVersion"
    Info "3. build   : build-android.ps1"
    Info "4. commit  : $Message"
    Info "5. push    : origin main"
    Info "6. release : $tag + APK"
    exit 0
}

# --- 1. Tests ---------------------------------------------------------------
if (-not $SkipTests) {
    Step "1/6 Tests (CoreTests)"
    Run $dotnet @('test', (Join-Path $RepoRoot 'Tools\CoreTests\CoreTests.csproj'), '--nologo')
} else {
    Step "1/6 Tests ignores (-SkipTests)"
}

# --- 2. Bump de version -----------------------------------------------------
Step "2/6 Version"
if ($targetVersion -ne $currentVersion) {
    $settings = $settings -replace "(?m)^(\s*bundleVersion:\s*)$([regex]::Escape($currentVersion))\s*$", "`${1}$targetVersion"
    $mCode = [regex]::Match($settings, '(?m)^\s*AndroidBundleVersionCode:\s*(\d+)\s*$')
    if ($mCode.Success) {
        $newCode = [int]$mCode.Groups[1].Value + 1
        $settings = $settings -replace "(?m)^(\s*AndroidBundleVersionCode:\s*)$($mCode.Groups[1].Value)\s*$", "`${1}$newCode"
        Info "AndroidBundleVersionCode : $($mCode.Groups[1].Value) -> $newCode"
    }
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($settingsPath, $settings, $utf8)
    Info "bundleVersion : $currentVersion -> $targetVersion"
} else {
    Info "Version inchangee ($currentVersion)"
}

# --- 3. Build APK -----------------------------------------------------------
$apk = Join-Path $RepoRoot 'Builds\Android\colorflow-debug.apk'
if (-not $SkipBuild) {
    Step "3/6 Build APK"
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot 'build-android.ps1')
    if ($LASTEXITCODE -ne 0) { Fail "build APK en echec. Voir Builds\build.log (marqueur [ColorFlow][Build])." }
} else {
    Step "3/6 Build ignore (-SkipBuild)"
    if (-not (Test-Path -LiteralPath $apk)) { Fail "APK absent : $apk (impossible de publier la release)." }
}
if (-not (Test-Path -LiteralPath $apk)) { Fail "APK absent apres build : $apk" }

# --- 4. Commit --------------------------------------------------------------
Step "4/6 Commit"
& $git add -A
$staged = (& $git status --porcelain) -join "`n"
if ($staged) {
    Run $git @('commit', '-m', $Message)
    Info "commit cree"
} else {
    Info "aucune modification a commiter"
}

# --- 5. Push ----------------------------------------------------------------
Step "5/6 Push origin main"
if ($DryRun) { Info "simule" } else { Run $git @('push', 'origin', 'main') }

# --- 6. Tag + release -------------------------------------------------------
Step "6/6 Tag et release GitHub"
$existingTag = (& $git tag --list $tag) -join ''
if (-not $existingTag) {
    Run $git @('tag', '-a', $tag, '-m', "Color Flow $tag")
    Info "tag $tag cree"
} else {
    Info "tag $tag deja present"
}
Run $git @('push', 'origin', $tag)

$previousTag = (& $git describe --tags --abbrev=0 "$tag^" 2>$null) -join ''
$range = if ($previousTag) { "$previousTag..$tag" } else { $tag }
$notes = (& $git log $range --pretty=format:"- %s") -join "`n"
if (-not $notes) { $notes = "- Release $tag" }
$title = "Color Flow $tag"
$apkName = Split-Path -Leaf $apk

& $gh release view $tag --json tagName 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Info "release existante : mise a jour"
    Run $gh @('release', 'upload', $tag, $apk, '--clobber')
    Run $gh @('release', 'edit', $tag, '--title', $title, '--notes', $notes, '--latest')
} else {
    Run $gh @('release', 'create', $tag, $apk, '--title', $title, '--notes', $notes, '--latest')
}
Info "release $tag publiee avec $apkName"

# --- Verification -----------------------------------------------------------
Step "Verification finale"
$localHead = ((& $git rev-parse HEAD) -join '').Trim()
Run $git @('fetch', 'origin', 'main')
$remoteHead = ((& $git rev-parse origin/main) -join '').Trim()
if ($localHead -ne $remoteHead) { Fail "HEAD local ($localHead) different de origin/main ($remoteHead)." }
if ((& $git status --porcelain)) { Fail "arbre de travail non propre apres publication." }

Write-Host ""
Write-Host "SUCCES : travail publie" -ForegroundColor Green
Info "commit  : $localHead"
Info "tag     : $tag"
Info "release : $title (asset $apkName)"
exit 0
