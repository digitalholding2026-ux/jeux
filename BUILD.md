# BUILD.md — APK Android debug de Color Flow

Ce document décrit comment produire **`Builds/Android/colorflow-debug.apk`** à partir du
projet Unity, en GUI ou en ligne de commande (batchmode).

> **Statut de vérification (Sprint 2)** : le pipeline ci-dessous a été **exécuté
> avec succès** sur la machine de l'agent pour l'APK **v0.2.0**. APK produit, mesuré
> et vérifié (`aapt dump badging`). Voir §9 « Résultat du build » pour les valeurs
> exactes (version d'Unity, taille, durée, SHA-256). La validation **device** reste
> à faire par le lead via `TEST_PROTOCOL.md`.

---

## 1. Sortie attendue

| Élément | Valeur |
|---|---|
| Fichier | `Builds/Android/colorflow-debug.apk` |
| Type | APK (pas AAB), build de développement |
| Architecture | ARMv7 + ARM64 (fat APK) |
| Backend script | IL2CPP |
| minSdk | 23 (Android 6.0) |
| Package | `com.digitalholding.colorflow` |
| Nom affiché | Color Flow |
| Signature | Keystore debug Unity (aucun keystore à fournir) |
| Taille indicative | ~30–80 Mo (varie selon version Unity/modules) |

Le build est un **build de développement** (`BuildOptions.Development`) : il est plus
volumineux et plus lent qu'un build release, mais c'est celui qui permet `adb logcat`
et le profilage. Il n'est pas destiné au store.

---

## 2. Prérequis exacts

1. **Unity Hub** installé.
2. **Unity Editor `6000.0.23f1`** (version exacte du projet — `ProjectSettings/ProjectVersion.txt`).
3. Module **Android Build Support** installé pour cette version, avec :
   - **Android SDK & NDK Tools**,
   - **OpenJDK**.
4. **Licence Unity activée** (Personal, Plus ou Pro) — voir §3.
5. ~10 Go d'espace disque libre (éditeur + modules + build).
6. Pour l'installation sur device : `adb` (platform-tools) **ou** un câble USB /
   transfert de fichier.

### Installation via Unity Hub (GUI)

1. Ouvrir Unity Hub → **Installs** → **Install Editor**.
2. Choisir **6000.0.23f1** (onglet « Archive » si absente de la liste).
3. Cocher **Android Build Support**, puis ses sous-modules
   **Android SDK & NDK Tools** et **OpenJDK**.
4. Installer, puis ouvrir le projet une première fois (import des assets).

### Installation via Unity Hub (ligne de commande, optionnel)

```bash
# Exemple Windows (adapter le chemin de Unity Hub)
"C:\Program Files\Unity Hub\Unity Hub.exe" -- \
  --headless install \
  --version 6000.0.23f1 \
  --changeset <changeset officiel de la version> \
  --module android android-sdk-ndk-tools android-open-jdk
```

Le *changeset* exact est indiqué sur la page de téléchargement Unity de la version.
Sans changeset correct, Hub refuse l'installation.

---

## 3. Licence Unity — trois méthodes

### Méthode A — GUI (recommandée pour Personal)

1. Lancer Unity Hub, se connecter avec un compte Unity.
2. **Preferences → Licenses → Add → Get a free personal license** (ou saisir une
   licence Plus/Pro).
3. Vérifier que la licence apparaît « Activated ».
4. Relancer le build.

### Méthode B — identifiants / numéro de série en ligne de commande

Pour les licences **Plus/Pro** disposant d'un numéro de série :

```bash
"$UNITY" -batchmode -nographics -quit \
  -username "$UNITY_EMAIL" \
  -password "$UNITY_PASSWORD" \
  -serial "$UNITY_SERIAL" \
  -logFile Builds/activation.log
```

- `UNITY_SERIAL` est obligatoire pour ce mode.
- Pour **Personal**, l'activation par `-username/-password` seul n'est plus
  officiellement supportée : utiliser la méthode A ou C.
- Ne jamais committer les identifiants : les passer par variables d'environnement.

### Méthode C — fichier `.ulf` pré-activé (machine sans GUI / CI)

1. Générer le fichier de requête :

   ```bash
   "$UNITY" -batchmode -nographics -quit \
     -createManualActivationFile \
     -logFile Builds/activation.log
   ```

   Cela produit un fichier `Unity_v6000.0.23f1.alf` (nom variable).

2. Se connecter sur <https://license.unity3d.com/manual>, téléverser le `.alf`,
   choisir le type de licence, télécharger le `.ulf`.
3. Activer :

   ```bash
   "$UNITY" -batchmode -nographics -quit \
     -manualLicenseFile /chemin/vers/Unity_v6000.x.ulf \
     -logFile Builds/activation.log
   ```

4. Vérifier `Builds/activation.log` : la licence doit être acceptée.

---

## 4. Commande de build

### Option 1 — script Bash (Linux / macOS / Git Bash Windows)

```bash
./build-android.sh
```

Avec un Unity hors chemin standard :

```bash
UNITY_PATH="/chemin/vers/Unity" ./build-android.sh
```

### Option 2 — script PowerShell (Windows)

```powershell
powershell -ExecutionPolicy Bypass -File .\build-android.ps1
```

ou, si `UNITY_PATH` est défini :

```powershell
$env:UNITY_PATH = "C:\Program Files\Unity\Hub\Editor\6000.0.23f1\Editor\Unity.exe"
.\build-android.ps1
```

### Option 3 — commande Unity brute

```bash
"$UNITY" \
  -batchmode -nographics -quit \
  -projectPath "$(pwd)" \
  -buildTarget Android \
  -executeMethod ColorFlow.Editor.BuildScript.BuildAndroidDebug \
  -logFile Builds/build.log
```

Les deux scripts :

- détectent Unity (`UNITY_PATH`, puis chemins Unity Hub standards) ;
- créent `Builds/` et suppriment l'ancien APK ;
- lancent la méthode `ColorFlow.Editor.BuildScript.BuildAndroidDebug` ;
- retournent un **code de sortie non nul** en cas d'échec Unity ;
- vérifient la **présence** de l'APK et affichent sa taille et la durée.

### Ce que fait `BuildScript.BuildAndroidDebug()`

- Applique : package `com.digitalholding.colorflow`, minSdk 23, IL2CPP,
  ARMv7 + ARM64, orientation portrait, nom « Color Flow ».
- Construit la liste des scènes **activées** dans `ProjectSettings/EditorBuildSettings.asset`.
- Lance `BuildPipeline.BuildPlayer` en `BuildOptions.Development`.
- Écrit un log de fin préfixé `[ColorFlow][Build]` :
  `SUCCÈS` / `ÉCHEC`, chemin, taille, durée.
- En batchmode, termine l'éditeur avec le code 0 (succès) ou 1 (échec).

En GUI : menu **Color Flow → Build Android Debug**.

---

## 5. Vérifier l'APK produit

```bash
ls -lh Builds/Android/colorflow-debug.apk
```

Contrôle du package et du minSdk (SDK build-tools requis) :

```bash
aapt dump badging Builds/Android/colorflow-debug.apk | head -5
# attendu : package: name='com.digitalholding.colorflow' ... sdkVersion:'23'
```

Installation :

```bash
adb install -r Builds/Android/colorflow-debug.apk
adb shell pm list packages | grep com.digitalholding.colorflow
```

La suite est décrite dans `TEST_PROTOCOL.md`.

---

## 6. Lire `Builds/build.log` en cas d'échec

```bash
# Lignes du build lui-même
grep -n "\[ColorFlow\]\[Build\]" Builds/build.log

# Erreurs / exceptions
grep -n -i -E "error|exception|failed|fatal" Builds/build.log | tail -60

# Contexte autour de la première erreur
grep -n -i -m 1 "error" Builds/build.log
```

Le log complet d'un build Unity fait plusieurs milliers de lignes : chercher d'abord
`[ColorFlow][Build]`, puis `Build Finished`, puis la première `error`.

---

## 7. Erreurs connues et correctifs

| Erreur (extrait du log) | Cause | Correctif |
|---|---|---|
| `Android build support is not installed` / `Unable to find a valid NDK` | Module Android absent | Installer **Android Build Support + SDK/NDK + OpenJDK** via Hub |
| `No valid Unity Editor license found` / `License is not active` | Licence non activée | §3 (GUI, `-serial`, ou `.ulf`) |
| `Failed to find JDK` / `JAVA_HOME` | OpenJDK manquant | Cocher **OpenJDK** dans le module Android, ou définir `JAVA_HOME` |
| `SDK location not found` | SDK Android non configuré | Unity → Preferences → External Tools → SDK/NDK/JDK (chemins du module) |
| `Another Unity instance is running with this project` | Projet ouvert ailleurs / lock | Fermer l'autre Unity, supprimer `Temp/UnityLockfile` si le process est mort |
| `executeMethod class ... not found` | Erreur de compilation des scripts Editor | Ouvrir le projet, corriger les erreurs Console, relancer |
| `IL2CPP` / `il2cpp.exe not found` | Backend IL2CPP indisponible | Installer le module Android complet (IL2CPP inclus) |
| `Gradle build failed` + `INSTALL_FAILED_...` | Conflit de signature/version à l'installation | `adb uninstall com.digitalholding.colorflow` puis réinstaller |
| `ARMv7 is not supported` (selon version) | Architecture retirée de l'éditeur | Passer `BuildScript.cs` en ARM64 seul et documenter l'écart |
| Build très long (> 15 min) | Premier build IL2CPP + import | Normal ; surveiller `Builds/build.log`, ne pas interrompre |
| `error CS0104: 'Debug' is an ambiguous reference` | Un script Editor importe `System.Diagnostics` **et** `UnityEngine` : `Debug` devient ambigu | Retirer `using System.Diagnostics;` et qualifier `System.Diagnostics.Stopwatch` (corrigé dans `Assets/Editor/BuildScript.cs`) |
| `UnityException: JDK not found` | Aucun JDK livré avec le module Android, et le chemin personnalisé n'est pas pris en compte si « JDK Installed with Unity » est décoché | Installer OpenJDK 17 dans `<Editor>/Data/PlaybackEngines/AndroidPlayer/OpenJDK` (détection automatique), ou recocher l'option dans External Tools |
| `UnityException: JDK not found` alors que `JAVA_HOME` est défini | Unity 6000.0.23f1 n'accepte pas un **JDK 21** (ou plus récent) comme JDK Android | Pointer `JAVA_HOME` sur un **JDK 17** ; `build-android.ps1` détecte la version et bascule automatiquement sur `%LOCALAPPDATA%\Portable\jdk17` si besoin |
| `UnityException: Android NDK not found` | **Unity 6000.0.23f1 exige le NDK r23b (23.1.7779620)** : un NDK r26b/r28 est rejeté par la validation. Le chemin NDK n'est pas résolu par les seules préférences | Installer r23b puis exporter `ANDROID_NDK_HOME` (et `ANDROID_NDK_ROOT`) vers `<sdk>\ndk\23.1.7779620` avant de lancer Unity |
| `build-android.ps1` affiche `ECHEC : Unity a retourne le code` (vide) en ~0 s | `Unity.exe` est une application GUI : le call operator PowerShell ne l'attend pas, `$LASTEXITCODE` est vide | `Start-Process -Wait -PassThru` puis `$process.ExitCode` (corrigé dans `build-android.ps1`) |

> En cas de doute, la cause réelle est presque toujours la **première** erreur du
> log : les suivantes sont des conséquences.

---

## 8. Limites / non vérifié

- Le build a été exécuté plusieurs fois sur la machine Windows de l'agent (Windows
  10 64 bits, ~4 Go de RAM, sans GPU) : premier build complet puis builds
  incrémentaux pour le Sprint 2. La reproductibilité sur une autre machine n'est
  pas prouvée.
- `build-android.sh` (bash) n'a **pas** été exécuté : aucune machine Linux/WSL
  disponible. Seul `build-android.ps1` a été exécuté de bout en bout.
- La **configuration de l'outillage Android** (JDK, SDK, NDK) n'est pas entièrement
  automatisée : `build-android.ps1` résout le JDK 17 et le NDK r23b s'ils sont aux
  emplacements portables connus, mais une installation standard doit être configurée
  une fois (§9.3).
- Le premier build complet a duré **1 044 s** (~17 min) ; les builds incrémentaux
  du Sprint 2 ont pris **137 s**. Ces valeurs dépendent de la machine et du cache
  `Library/`.
- L'APK n'a été ni installé ni lancé sur un appareil : voir `TEST_PROTOCOL.md`,
  à exécuter par le lead.

---

## 9. Résultat du build

### 9.1 Verdict

**Succès.** Le pipeline décrit dans ce document produit un APK debug installable,
sans aucune modification manuelle du projet entre la commande et l'APK.

| Élément | Valeur mesurée |
|---|---|
| Commande | `powershell -NoProfile -ExecutionPolicy Bypass -File build-android.ps1` |
| Unity | 6000.0.23f1 (1c4764c07fb4), revision 1853284 |
| Sortie | `Builds/Android/colorflow-debug.apk` |
| Version | `versionName=0.2.0`, `versionCode=3` |
| Taille | 39 427 851 octets (37,60 Mo) |
| Durée totale | 137 s (build incrémental) ; 1 044 s pour le premier build complet |
| Code de sortie Unity | 0 |
| Erreurs / avertissements | 0 / 0 |
| SHA-256 | `C684FAA63D7C8E8508636DE61EFA0EE3BF5F9F1FABABF42765AFB6C13876854D` |
| Log complet | `Builds/build.log` (marqueur final `[ColorFlow][Build] SUCCES`) |

### 9.2 Vérification du contenu de l'APK

`aapt dump badging Builds/Android/colorflow-debug.apk` :

```
package: name='com.digitalholding.colorflow' versionCode='3' versionName='0.2.0'
         platformBuildVersionName='16' platformBuildVersionCode='36'
         compileSdkVersion='36' compileSdkVersionCodename='16'
sdkVersion:'23'
targetSdkVersion:'36'
application-label:'Color Flow'
launchable-activity: name='com.unity3d.player.UnityPlayerActivity'
native-code: 'arm64-v8a' 'armeabi-v7a'
```

Conforme au cahier des charges : package `com.digitalholding.colorflow`,
`minSdkVersion` 23, label « Color Flow », build de développement (debug,
ARMv7 + ARM64, IL2CPP).

### 9.3 Environnement exact qui a produit cet APK

Ces points sont **obligatoires** pour reproduire le build ; ils ne sont pas
devinables depuis le projet :

| Brique | Version / chemin | Pourquoi |
|---|---|---|
| Unity Editor | 6000.0.23f1 + module **Android Build Support** (SDK/NDK tools + IL2CPP) | Version du projet (`ProjectSettings/ProjectVersion.txt`) |
| Licence | Unity Personal activée (Hub, compte `digitalholding2026`) | Sans licence, `-batchmode` refuse de builder |
| JDK | **OpenJDK 17** (`C:\Users\Docker\AppData\Local\Portable\jdk17`, Temurin 17.0.20.1) | Unity 6000.0.23f1 refuse le JDK 21 présent dans `JAVA_HOME` ; le JDK 17 est obligatoire. Le module Android de cette machine ne fournit pas d'OpenJDK embarqué |
| SDK Android | `C:\Users\Docker\AppData\Local\Portable\android-sdk` (build-tools 36.0.0, platform 36) | Détecté via `ANDROID_HOME` / `ANDROID_SDK_ROOT` |
| NDK | **r23b = 23.1.7779620** dans `<sdk>\ndk\23.1.7779620` | **Unity 6000.0.23f1 rejette r26b et r28** ; r23b est la seule version validée par cette révision |
| Variables d'environnement | `ANDROID_HOME`, `ANDROID_SDK_ROOT`, **`ANDROID_NDK_HOME`**, `ANDROID_NDK_ROOT` | Sans `ANDROID_NDK_HOME`, Unity échoue sur `Android NDK not found` même avec le NDK installé et le chemin saisi dans External Tools |
| `JAVA_HOME` | chemin du **JDK 17** | Utilisé par Gradle. `build-android.ps1` lit `JAVA_VERSION` et bascule automatiquement sur le JDK 17 portable si `JAVA_HOME` pointe sur une autre version |

> **Piège vérifié** : les préférences « External Tools » d'Unity sont stockées en
> **valeurs binaires avec des noms hachés** dans le registre
> (`HKCU\Software\Unity Technologies\Unity Editor 5.x`, clés `AndroidNDKRoot_h…`).
> Écrire `REG_SZ` à la main **ne fonctionne pas** : il faut passer par l'UI d'Unity
> ou par `EditorPrefs` dans un script Editor. C'est pour cette raison que le NDK a
> dû être exposé via `ANDROID_NDK_HOME`.

### 9.4 Temps de mise en place (information lead)

La mise en place de l'outillage a été la partie la plus longue du sprint :
installation d'Unity + module Android, licence, puis trois itérations sur le JDK et
le NDK (r28 → r26b → r23b). Sur une machine déjà équipée, le livrable B se limite à
`build-android.ps1` + `BUILD.md`.
