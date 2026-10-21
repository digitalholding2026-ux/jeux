# BUILD.md — APK Android debug de Color Flow

Ce document décrit comment produire **`Builds/Android/colorflow-debug.apk`** à partir du
projet Unity, en GUI ou en ligne de commande (batchmode).

> **Statut de vérification (Sprint 0.5)** : le pipeline ci-dessous a été écrit et
> **exécuté jusqu'à la détection de l'outillage**, qui a échoué sur la machine de
> l'agent (Unity Editor absent, aucune licence). **Aucun APK n'a été produit par
> l'agent.** Voir le rapport Sprint 0.5, section « tenté et échoué », et
> `TEST_PROTOCOL.md` pour la validation device.

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

> En cas de doute, la cause réelle est presque toujours la **première** erreur du
> log : les suivantes sont des conséquences.

---

## 8. Limites / non vérifié

- Aucun build n'a été exécuté avec succès dans l'environnement de l'agent :
  pas d'Unity Editor, pas de module Android, pas de licence.
- Les scripts `build-android.sh` / `build-android.ps1` ont été exécutés et
  s'arrêtent proprement sur « Unity introuvable » (code de sortie non nul) —
  c'est le comportement attendu sur une machine sans outillage.
- Les valeurs de durée et de taille d'APK ne peuvent donc pas être confirmées
  avant le premier build réel par le lead.
