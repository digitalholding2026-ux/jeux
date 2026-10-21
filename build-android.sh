#!/usr/bin/env bash
# Build Android debug de Color Flow → Builds/Android/colorflow-debug.apk
# Voir BUILD.md pour les prérequis (Unity 6000.0.23f1 + module Android + licence).
set -u -o pipefail

UNITY_VERSION="6000.0.23f1"
PROJECT_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_DIR="$PROJECT_PATH/Builds"
LOG_FILE="$LOG_DIR/build.log"
APK_PATH="$PROJECT_PATH/Builds/Android/colorflow-debug.apk"
EXECUTE_METHOD="ColorFlow.Editor.BuildScript.BuildAndroidDebug"

fail() {
  echo "ERREUR : $*" >&2
  exit 1
}

echo "== Color Flow — build Android debug =="
echo "Version Unity attendue : $UNITY_VERSION"

UNITY_BIN=""

if [[ -n "${UNITY_PATH:-}" ]]; then
  [[ -f "$UNITY_PATH" ]] || fail "UNITY_PATH ne pointe pas vers un fichier exécutable : $UNITY_PATH"
  UNITY_BIN="$UNITY_PATH"
else
  candidates=()
  case "$(uname -s)" in
    Darwin)
      candidates+=("/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity")
      candidates+=("$HOME/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity")
      ;;
    Linux)
      candidates+=("$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity")
      candidates+=("/opt/Unity/Editor/Unity")
      candidates+=("/usr/local/bin/unity-editor")
      ;;
    MINGW*|MSYS*|CYGWIN*)
      candidates+=("/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe")
      candidates+=("/c/Program Files (x86)/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe")
      candidates+=("/c/Program Files/Unity $UNITY_VERSION/Editor/Unity.exe")
      ;;
    *)
      echo "Système non reconnu : $(uname -s)" >&2
      ;;
  esac

  for candidate in ${candidates[@]+"${candidates[@]}"}; do
    if [[ -f "$candidate" ]]; then
      UNITY_BIN="$candidate"
      break
    fi
  done
fi

if [[ -z "$UNITY_BIN" ]]; then
  cat >&2 <<EOF
ERREUR : binaire Unity introuvable (version $UNITY_VERSION).

Chemins recherchés :
  - \$UNITY_PATH (variable d'environnement)
  - Unity Hub : <Hub>/Editor/$UNITY_VERSION/...
  - macOS  : /Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity
  - Linux  : \$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity, /opt/Unity/Editor/Unity
  - Windows: C:\\Program Files\\Unity\\Hub\\Editor\\$UNITY_VERSION\\Editor\\Unity.exe

Installation :
  1. Installer Unity Hub.
  2. Installer Unity $UNITY_VERSION avec le module « Android Build Support »
     (cocher « Android SDK & NDK Tools » et « OpenJDK »).
  3. Activer une licence Unity (Personal, Plus ou Pro) — voir BUILD.md §Licence.
  4. Relancer : UNITY_PATH="/chemin/vers/Unity" ./build-android.sh
EOF
  exit 1
fi

mkdir -p "$LOG_DIR" "$(dirname "$APK_PATH")"
rm -f "$APK_PATH"

echo "Unity    : $UNITY_BIN"
echo "Projet   : $PROJECT_PATH"
echo "APK      : $APK_PATH"
echo "Log      : $LOG_FILE"
echo

START_TS=$(date +%s)

"$UNITY_BIN" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -buildTarget Android \
  -executeMethod "$EXECUTE_METHOD" \
  -logFile "$LOG_FILE"
STATUS=$?

END_TS=$(date +%s)
DURATION=$((END_TS - START_TS))

echo
if [[ $STATUS -ne 0 ]]; then
  echo "ÉCHEC : Unity a retourné le code $STATUS (durée ${DURATION}s)." >&2
  echo "--- 40 dernières lignes de $LOG_FILE ---" >&2
  tail -n 40 "$LOG_FILE" 2>/dev/null || echo "(log absent)" >&2
  exit "$STATUS"
fi

if [[ ! -f "$APK_PATH" ]]; then
  echo "ÉCHEC : Unity a retourné 0 mais l'APK est absent : $APK_PATH" >&2
  echo "--- 40 dernières lignes de $LOG_FILE ---" >&2
  tail -n 40 "$LOG_FILE" 2>/dev/null || echo "(log absent)" >&2
  exit 1
fi

BYTES=$(wc -c < "$APK_PATH" | tr -d '[:space:]')
echo "SUCCÈS : $APK_PATH"
echo "Taille  : $BYTES octets ($((BYTES / 1024 / 1024)) Mo)"
echo "Durée   : ${DURATION}s"
exit 0
