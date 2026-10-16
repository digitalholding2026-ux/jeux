# Règles de travail — Fruits Crush (obligatoires)

## 1. Fin de tâche : commit + push + build APK (systématique)
À la fin de CHAQUE tâche (feature, fix, équilibrage, visuels, perf), exécuter
dans l'ordre, sans exception :

1. **Vérifier** : extraire le JS de `index.html` et `node --check` → 0 erreur.
   Vérifier aussi : SVG équilibrés, aucune référence orpheline.
2. **Synchroniser** : copier `index.html` (et tout asset modifié : fonts, sons…)
   vers `android/app/src/main/assets/www/` (hashes identiques exigés).
3. **Builder l'APK** : `:app:assembleDebug` via Gradle (8.7, JDK 17,
   SDK 34 via `android/local.properties`), build `BUILD SUCCESSFUL` exigé.
4. **Publier l'APK** : copier `app-debug.apk` vers `FruitsCrush.apk` à la racine.
5. **Commit + push sur `main`** : message FR, clair, préfixé par le sujet
   (ex. `Effets speciaux combos (…) + rebuild APK`).
6. **Résumer** : changements, vérifications, n° de commit.

Exception : tâche 100 % documentaire sans impact sur le jeu (ex. ce fichier)
→ rebuild APK inutile (binaire identique), commit + push restants dus.

## 2. Hygiène Git
- Ne JAMAIS committer : images de référence (`*.jfif`, `*.webp` racine),
  fichiers temporaires (`TMPCHECK.js`, `swap_*.js`, `*.out`), `.gradle/`,
  `android/app/build/`, `android/.gradle/`.
- `git add` explicite des fichiers concernés uniquement, jamais `git add -A`.

## 3. Qualité mobile d'abord
- Toute animation/effet nouveau : version réduite en mode `fast`/`perf`
  (timings via `wt()`, particules plafonnées via `lowFX()`).
- Pas d'attente ajoutée qui ralentit le jeu sans raison (hitstop ≤ 100 ms,
  uniquement paliers x5+).
- Pas de `backdrop-filter` ni de `filter` lourd sur des couches plein écran.

## 4. Style de code
- Fichier unique `index.html` (jeu vanilla, pas de bundler).
- FR : commentaires et textes utilisateurs en français.
