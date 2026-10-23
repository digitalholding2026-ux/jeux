# Règles de travail — Color Flow

> Ces règles s'appliquent à **toute** intervention sur ce dépôt, humaine ou automatisée
> (agent IA inclus). Elles priment sur toute habitude ou préférence locale.

---

## RÈGLE ABSOLUE — Publication obligatoire en fin de travail

**Aucun travail n'est terminé tant qu'il n'a pas été commité, poussé et publié en release.**

À la fin de **chaque** session de travail — sans exception et sans attendre qu'on le demande —
exécuter la séquence suivante, dans cet ordre :

1. **Tests** — `dotnet test Tools/CoreTests/CoreTests.csproj` doit être vert.
   Si Unity est disponible : tests EditMode/PlayMode également.
2. **Build** — `powershell -ExecutionPolicy Bypass -File .\build-android.ps1`
   doit produire `Builds/Android/colorflow-debug.apk`.
3. **Commit** — message explicite en français, format `type(scope): résumé`
   (`feat`, `fix`, `refactor`, `test`, `docs`, `build`, `chore`).
4. **Push** — `git push origin main` (jamais de force push sur `main`).
5. **Release** — tag `vX.Y.Z` + release GitHub **latest** avec l'APK en asset.

### Commande officielle

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\publish.ps1 -Message "feat(game): description courte" -Version 0.3.0
```

`Tools/publish.ps1` applique la séquence complète : tests → bump de version
(`bundleVersion` + `AndroidBundleVersionCode`) → build APK → commit → push →
tag → release GitHub avec l'APK joint. Il vérifie l'authentification GitHub
**avant** de lancer le build et échoue proprement si un prérequis manque.

Options : `-SkipTests`, `-SkipBuild` (déconseillé), `-DryRun` (affiche les actions
sans rien modifier), `-Message` (obligatoire s'il y a des modifications),
`-Version` (sinon la version courante de `ProjectSettings.asset` est réutilisée).

### Interdits

- Terminer un travail avec un `git status` non vide (fichiers modifiés, ajoutés ou non suivis).
- Terminer sans que `origin/main` contienne le commit local.
- Terminer sans release publiée pour un lot de modifications livrable.
- `git push --force`, `git reset --hard` ou toute réécriture de l'historique déjà publié sur `main`.
- Committer `Builds/`, `Library/`, `Temp/`, `bin/`, `obj/`, `*.apk`, `*.aab`, un keystore ou un secret.
- Annoncer « terminé » si le push ou la release a échoué : l'échec doit être signalé
  explicitement, avec la commande exacte à relancer.

### Seules exceptions admises

1. **Aucune modification** : arbre de travail propre et rien de nouveau à livrer →
   le dire explicitement, ne rien créer artificiellement.
2. **Blocage externe réel** (authentification GitHub absente, réseau coupé, licence Unity
   invalide) → faire au minimum le **commit local**, indiquer clairement que le push/release
   n'a pas eu lieu, et fournir la commande de reprise.

Ces exceptions ne dispensent jamais de prévenir : le travail est alors **« commité mais non
publié »**, jamais « terminé ».

### Versionnement

- Semver `MAJEUR.MINEUR.CORRECTIF`, tags `vX.Y.Z` (le dépôt utilise aussi
  `vX.Y.Z-sprintN` pour les étapes intermédiaires).
- Source de vérité : `bundleVersion` et `AndroidBundleVersionCode` dans
  `ProjectSettings/ProjectSettings.asset`.
- Chaque release publie l'APK debug **et** les notes générées depuis les commits.
- La release la plus récente porte le badge **latest**.

### Checklist de fin de travail

```
[ ] tests verts
[ ] APK produit (build-android.ps1 -> SUCCES)
[ ] commit pousse (git status propre, HEAD == origin/main)
[ ] tag vX.Y.Z pousse
[ ] release GitHub latest publiee avec l'APK
[ ] documentation mise a jour si le comportement change
```

---

## Environnement de référence

| Brique | Chemin / version |
|---|---|
| Unity Editor | `6000.0.23f1` + module Android Build Support (SDK/NDK, OpenJDK) |
| JDK 17 | `%LOCALAPPDATA%\Portable\jdk17` (Temurin 17) — Unity 6000.0.23f1 refuse JDK 21+ |
| Android SDK | `%LOCALAPPDATA%\Portable\android-sdk` (platform 36, build-tools 36.0.0) |
| NDK | `r23b` = `23.1.7779620` dans `<sdk>\ndk\23.1.7779620` |
| Git / gh / dotnet | `%LOCALAPPDATA%\Portable\{git,gh,dotnet}` (dans le `PATH` utilisateur) |

Voir `BUILD.md` (§9.3) pour le détail des pièges JDK/NDK.
