# Build Android debug de Color Flow -> Builds\Android\colorflow-debug.apk
# Voir BUILD.md pour les prerequis (Unity 6000.0.23f1 + module Android + licence).
$ErrorActionPreference = 'Stop'

$UnityVersion = '6000.0.23f1'
$ProjectPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$LogDir = Join-Path $ProjectPath 'Builds'
$LogFile = Join-Path $LogDir 'build.log'
$ApkPath = Join-Path $ProjectPath 'Builds\Android\colorflow-debug.apk'
$ExecuteMethod = 'ColorFlow.Editor.BuildScript.BuildAndroidDebug'

function Fail([string]$Message) {
    Write-Host "ERREUR : $Message" -ForegroundColor Red
    exit 1
}

Write-Host "== Color Flow - build Android debug =="
Write-Host "Version Unity attendue : $UnityVersion"

$UnityBin = $null

if ($env:UNITY_PATH) {
    if (-not (Test-Path -LiteralPath $env:UNITY_PATH -PathType Leaf)) {
        Fail "UNITY_PATH ne pointe pas vers un fichier : $env:UNITY_PATH"
    }
    $UnityBin = $env:UNITY_PATH
} else {
    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe",
        "C:\Program Files (x86)\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe",
        "$env:LOCALAPPDATA\Programs\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
    )

    $UnityBin = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}

if (-not $UnityBin) {
    Write-Host "ERREUR : binaire Unity introuvable (version $UnityVersion)." -ForegroundColor Red
    Write-Host ""
    Write-Host "Chemins recherches :"
    Write-Host "  - `$env:UNITY_PATH"
    Write-Host "  - C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
    Write-Host "  - C:\Program Files (x86)\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
    Write-Host "  - $env:LOCALAPPDATA\Programs\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
    Write-Host ""
    Write-Host "Installation :"
    Write-Host "  1. Installer Unity Hub."
    Write-Host "  2. Installer Unity $UnityVersion avec le module 'Android Build Support'"
    Write-Host "     (cocher 'Android SDK & NDK Tools' et 'OpenJDK')."
    Write-Host "  3. Activer une licence Unity (Personal, Plus ou Pro) - voir BUILD.md."
    Write-Host "  4. Relancer : .\build-android.ps1  (ou definir `$env:UNITY_PATH)."
    exit 1
}

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ApkPath) | Out-Null
if (Test-Path -LiteralPath $ApkPath) {
    Remove-Item -LiteralPath $ApkPath -Force
}

Write-Host "Unity    : $UnityBin"
Write-Host "Projet   : $ProjectPath"
Write-Host "APK      : $ApkPath"
Write-Host "Log      : $LogFile"
Write-Host ""

# --- Outils Android : Unity 6000.0.23f1 exige un JDK 17 et le NDK r23b.
# JAVA_HOME global peut pointer sur un JDK plus recent (refuse par Unity) :
# on retombe alors sur le JDK 17 portable si present.
function Get-JavaMajor([string]$JavaHome) {
    if (-not $JavaHome) { return 0 }
    $release = Join-Path $JavaHome 'release'
    if (-not (Test-Path -LiteralPath $release)) { return 0 }
    $match = Select-String -LiteralPath $release -Pattern '^JAVA_VERSION="(\d+)' | Select-Object -First 1
    if ($match) { return [int]$match.Matches[0].Groups[1].Value }
    return 0
}

$javaMajor = Get-JavaMajor $env:JAVA_HOME
if ($javaMajor -ne 17) {
    $jdk17 = Join-Path $env:LOCALAPPDATA 'Portable\jdk17'
    if (Test-Path -LiteralPath (Join-Path $jdk17 'bin\java.exe')) {
        Write-Host "JAVA_HOME : JDK $javaMajor detecte, bascule sur JDK 17 -> $jdk17"
        $env:JAVA_HOME = $jdk17
    } else {
        Write-Host "AVERTISSEMENT : JDK 17 introuvable. Unity risque de refuser le build." -ForegroundColor Yellow
    }
}
Write-Host "JAVA_HOME : $env:JAVA_HOME"

if (-not $env:ANDROID_NDK_HOME -and $env:ANDROID_HOME) {
    $ndk23 = Join-Path $env:ANDROID_HOME 'ndk\23.1.7779620'
    if (Test-Path -LiteralPath $ndk23) {
        $env:ANDROID_NDK_HOME = $ndk23
        $env:ANDROID_NDK_ROOT = $ndk23
    }
}

if (-not $env:ANDROID_NDK_HOME) {
    Write-Host "AVERTISSEMENT : ANDROID_NDK_HOME n'est pas defini." -ForegroundColor Yellow
    Write-Host "  Unity 6000.0.23f1 exige le NDK r23b (23.1.7779620) et ne le resout pas" -ForegroundColor Yellow
    Write-Host "  toujours via External Tools. Voir BUILD.md section 9.3." -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "NDK      : $env:ANDROID_NDK_HOME"
}

Write-Host ""

$start = Get-Date

# Unity.exe est une application GUI : le call operator PowerShell ne l'attend pas.
# Start-Process -Wait est obligatoire pour recuperer le vrai code de sortie.
$unityArgs = @(
    '-batchmode',
    '-nographics',
    '-quit',
    '-projectPath', "`"$ProjectPath`"",
    '-buildTarget', 'Android',
    '-executeMethod', $ExecuteMethod,
    '-logFile', "`"$LogFile`""
)

$process = Start-Process -FilePath $UnityBin -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow
$status = $process.ExitCode
$duration = [int](((Get-Date) - $start).TotalSeconds)

Write-Host ""
if ($status -ne 0) {
    Write-Host "ECHEC : Unity a retourne le code $status (duree ${duration}s)." -ForegroundColor Red
    Write-Host "--- 40 dernieres lignes de $LogFile ---" -ForegroundColor Red
    if (Test-Path -LiteralPath $LogFile) {
        Get-Content -LiteralPath $LogFile -Tail 40
    } else {
        Write-Host "(log absent)"
    }
    exit $status
}

if (-not (Test-Path -LiteralPath $ApkPath -PathType Leaf)) {
    Write-Host "ECHEC : Unity a retourne 0 mais l'APK est absent : $ApkPath" -ForegroundColor Red
    Write-Host "--- 40 dernieres lignes de $LogFile ---" -ForegroundColor Red
    if (Test-Path -LiteralPath $LogFile) {
        Get-Content -LiteralPath $LogFile -Tail 40
    } else {
        Write-Host "(log absent)"
    }
    exit 1
}

$bytes = (Get-Item -LiteralPath $ApkPath).Length
$sizeMb = [math]::Round($bytes / 1MB, 2)

# --- Garde-fou : sans activite lançable, l'APK s'installe mais n'a ni icone ni
# bouton "Ouvrir". On refuse de livrer un APK dans cet etat.
$aapt = $null
if ($env:ANDROID_HOME) {
    $buildTools = Join-Path $env:ANDROID_HOME 'build-tools'
    if (Test-Path -LiteralPath $buildTools) {
        $aapt = Get-ChildItem -LiteralPath $buildTools -Directory |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName 'aapt.exe' } |
            Where-Object { Test-Path -LiteralPath $_ } |
            Select-Object -First 1
    }
}

if ($aapt) {
    $badging = & $aapt dump badging $ApkPath 2>&1
    if (-not ($badging | Select-String -SimpleMatch 'launchable-activity:')) {
        Write-Host "ECHEC : l'APK ne declare aucune activite lançable (pas d'icone ni de bouton Ouvrir)." -ForegroundColor Red
        Write-Host "Verifier qu'aucun Assets/Plugins/Android/AndroidManifest.xml ne remplace le manifeste Unity." -ForegroundColor Red
        exit 1
    }

    Write-Host "Verification : activite lançable presente."
} else {
    Write-Host "AVERTISSEMENT : aapt introuvable, verification du manifeste ignoree." -ForegroundColor Yellow
}

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $ApkPath).Hash

Write-Host "SUCCES : $ApkPath"
Write-Host "Taille  : $bytes octets ($sizeMb Mo)"
Write-Host "SHA-256 : $hash"
Write-Host "Duree   : ${duration}s"
exit 0
