# CI — configuration et limites

> **Statut : non exécuté.** Les workflows de ce dossier ont été écrits après un
> build local réussi (voir `BUILD.md` §9), mais ils n'ont **jamais été lancés** :
> aucun secret n'est configuré sur le dépôt et l'agent n'a pas le droit de pousser
> des modifications de secrets. Tant que §2 et §3 ne sont pas faits, les workflows
> échoueront à l'étape Unity (licence manquante).

Livrable **D** du Sprint 0.5 (optionnel, conditionné au succès du build local).

---

## 1. Ce que font les workflows

| Fichier | Déclencheur | Rôle |
|---|---|---|
| `.github/workflows/tests.yml` | push sur `main`, PR, manuel | EditMode **et** PlayMode en parallèle, résultats NUnit en artefacts |
| `.github/workflows/build-android.yml` | push sur `main` (chemins projet), manuel | APK debug Android (IL2CPP, ARMv7 + ARM64, minSdk 23) en artefact |

Versions figées :

- Unity **6000.0.23f1** (`ProjectSettings/ProjectVersion.txt`)
- `game-ci/unity-test-runner@v4`
- `game-ci/unity-builder@v4`
- `actions/checkout@v4`, `actions/cache@v4`, `actions/upload-artifact@v4`

Le cache `Library/` est indexé sur le hash de `Assets/`, `Packages/` et
`ProjectSettings/` : un changement de code invalide le cache, un changement de
workflow seul ne l'invalide pas.

---

## 2. Secrets à créer (Settings → Secrets and variables → Actions)

| Secret | Obligatoire | Contenu |
|---|---|---|
| `UNITY_LICENSE` | oui | **Contenu XML complet** du fichier `.ulf` (licence Unity) |
| `UNITY_EMAIL` | oui (Personal) | Email du compte Unity |
| `UNITY_PASSWORD` | oui (Personal) | Mot de passe du compte Unity |
| `UNITY_SERIAL` | Pro uniquement | Numéro de série (remplace `UNITY_LICENSE`) |

`GITHUB_TOKEN` est fourni automatiquement par GitHub, ne pas le créer.

### 2.1 Obtenir `UNITY_LICENSE` (licence Personal)

Deux méthodes, au choix.

**A. Depuis la machine qui a déjà activé Unity (la plus simple ici)**

1. Localiser le fichier de licence :
   - Windows : `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS : `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux : `~/.local/share/unity3d/Unity/Unity_lic.ulf`
2. Ouvrir le fichier, copier **tout** le XML (`<Root>…</Root>`).
3. Le coller dans le secret `UNITY_LICENSE`.

**B. Activation manuelle (si le fichier est absent)**

1. `Unity.exe -batchmode -nographics -createManualActivationFile -logFile -`
   (ou `unity-editor` sous Linux/macOS) → produit `Unity_v6000.x.alf`.
2. Déposer le `.alf` sur <https://license.unity3d.com/manual>, choisir
   *Unity Personal*, télécharger le `.ulf`.
3. Mettre le contenu du `.ulf` dans `UNITY_LICENSE`.

> La licence Personal est liée à un compte et **peut expirer / nécessiter une
> réactivation**. Si la CI échoue avec `No valid Unity Editor license found`,
> régénérer le `.ulf` et mettre à jour le secret.

---

## 3. Réglages du dépôt à vérifier une fois

- **Actions activées** : Settings → Actions → General → *Allow all actions*.
- **Permissions du workflow** : *Read and write permissions* si vous voulez que
  `unity-test-runner` publie les résultats via `GITHUB_TOKEN` (facultatif ; les
  artefacts fonctionnent avec *Read* seul).
- **Dépôt privé** : les minutes Actions sont décomptées du quota. Un build Android
  IL2CPP consomme beaucoup (~15–25 min de runner), contrairement aux tests.
- **Artifacts** : conservation 14 jours (paramétrable dans les workflows).

---

## 4. Différences entre la CI et le build local (important)

| Point | Local (`build-android.ps1`) | CI (`game-ci/unity-builder`) |
|---|---|---|
| Entrée de build | `ColorFlow.Editor.BuildScript.BuildAndroidDebug` | builder interne de game-ci |
| Sortie | `Builds/Android/colorflow-debug.apk` | `build/Android/*.apk` (uploadé en artefact) |
| NDK | r23b installé manuellement + `ANDROID_NDK_HOME` | fourni par l'image Docker game-ci, version choisie par game-ci |
| Licence | locale (Unity Hub) | secrets GitHub |

Conséquence : **la CI ne teste pas `BuildScript.cs`**. Elle prouve que le projet
compile et produit un APK, pas que le script livré au lead fonctionne. Le build
local (§9 de `BUILD.md`) reste la référence pour le livrable B.

Si vous voulez que la CI exerce réellement `BuildScript.cs`, il faut passer
`buildMethod: ColorFlow.Editor.BuildScript.BuildAndroidDebug` à `unity-builder`
et adapter l'upload vers `Builds/Android` — **non fait ici**, car non testé.

---

## 5. Lire les résultats

- **Tests** : onglet Actions → run → artefacts `test-results-editmode` et
  `test-results-playmode` (`results.xml`, logs). Le commentaire de check
  `Unity editmode` / `Unity playmode` apparaît sur la PR.
- **APK** : artefact `colorflow-debug-apk` → télécharger, puis suivre
  `TEST_PROTOCOL.md` pour l'installation.

---

## 6. Ce qui n'est pas couvert

- Aucun test sur appareil réel (la CI n'a pas d'Android) : scénarios 1 à 5 de
  `TEST_PROTOCOL.md` restent manuels.
- Aucune signature release / keystore : uniquement du debug.
- Aucun déploiement (pas de Play Store, pas de Firebase App Distribution).
- Aucune exécution : voir l'avertissement en tête de fichier.
