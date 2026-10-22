# Color Flow — Sprint 2

Puzzle hybride mobile (portrait, public 8–14 ans). Le dépôt contient le jeu
complet **Sprint 2** : boucle de jeu, blobs spéciaux, score et combos, objectifs
variables, économie de pièces, boutique de skins et sauvegarde locale.

Aucun asset externe : tous les visuels (blobs, symboles, pièces, accessoires),
les animations, l'UI et les sons sont **générés par le code** à l'exécution.

## Contenu

- Grille N×M décrite par des données, peinte par des créatures colorées.
- Tapis roulant FIFO de 5 créatures, spawn équitable (aucune couleur morte).
- **3 blobs spéciaux** tirés à 10 % : bombe 3×3, arc-en-ciel, ligne.
- **Score** (10 pts/pixel) et **combos ×2 / ×3 / ×4** sur 1,5 s.
- **3 objectifs** : remplir le motif, atteindre un score en 60 s, remplir en 30 s.
- **6 niveaux** de test, dont 3 dédiés aux nouveaux objectifs.
- **Pièces d'or** gagnées selon le score, **boutique** et **3 skins** achetables.
- Sauvegarde `PlayerPrefs` (pièces, skins possédés, skin équipé).
- Écran de fin indiquant objectif atteint ou manqué, score et pièces gagnées.

## Prérequis

| Outil | Version |
|---|---|
| Unity | **Unity 6 LTS** (`6000.0.23f1` ou plus récent) |
| Modules Unity | Android Build Support (+ SDK, NDK r23b, JDK 17) pour l'APK |
| Test Framework | inclus via `Packages/manifest.json` (1.4.5) |
| .NET SDK | 8.0 (uniquement pour les tests Core hors Unity) |

Le projet utilise le **render pipeline Built-in en 2D** : aucune dépendance URP,
aucun package externe.

## Ouvrir le projet

1. Unity Hub → **Add** → sélectionner le dossier du dépôt.
2. Ouvrir avec Unity 6 LTS. La première ouverture importe les assets et
   compile les assemblies (`ColorFlow.Core`, `ColorFlow.Game`, tests).
3. Ouvrir `Assets/Scenes/Boot.unity` puis **Play**.

Les trois scènes sont déjà dans **File > Build Profiles** : `Boot`, `MainMenu`,
`Game`.

## Jouer

- Le menu propose **6 niveaux** en grille, une **bourse de pièces** et un bouton
  **BOUTIQUE**.
- Une créature apparaît régulièrement sur le tapis (5 emplacements).
- **Taper une créature** peint le prochain pixel de sa couleur
  (haut-gauche → bas-droite).
- Une créature dont la couleur n'est plus nécessaire est refusée : elle reste
  sur le tapis et un message l'indique.
- **Bombe** : peint le carré 3×3 autour du prochain pixel de sa couleur.
- **Arc-en-ciel** : peint le prochain pixel cible, quelle que soit sa couleur.
- **Ligne** : peint toute la rangée ou colonne la plus rentable.
- **Combo** : enchaîner deux blobs de même couleur affichée en moins de 1,5 s
  multiplie les points (×2, ×3, ×4 maximum).
- Tapis plein à la 6ᵉ créature → **Perdu** (ou score non atteint / temps écoulé).
- Objectif rempli → **Gagné**. Les pièces sont créditées automatiquement.

### Objectifs

| Objectif | Victoire | Défaite |
|---|---|---|
| `FillAll` | motif entièrement peint | tapis plein |
| `ScoreTarget` | score cible avant la limite (60 s par défaut) | chrono, tapis plein, motif fini avant le score |
| `TimeAttack` | motif fini avant la limite (30 s par défaut) | chrono, tapis plein |

### Boutique et skins

| Skin | Prix | Accessoire dessiné |
|---|---|---|
| Classique | 0 | aucun |
| Lunettes | 120 | lunettes rondes |
| Chapeau | 200 | chapeau à bandeau rose |
| Couronne | 320 | couronne dorée |

Les pièces gagnées valent `score / 40 + (20 si victoire, sinon 3)`.
Les skins achetés et le skin équipé sont sauvegardés dans `PlayerPrefs` et
restaurés au lancement. Les accessoires sont masqués sur les blobs spéciaux pour
laisser le symbole de pouvoir lisible.

## Tests

### Dans l'éditeur

**Window > General > Test Runner**

- Onglet **EditMode** → *Run All* (86 tests)
- Onglet **PlayMode** → *Run All* (3 smoke tests)

### En ligne de commande

```bash
Unity -batchmode -projectPath . -runTests -testPlatform EditMode \
      -testResults Logs/editmode.xml -quit
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode \
      -testResults Logs/playmode.xml -quit
```

### Sans Unity (vérification du Core)

Le dossier `Tools/CoreTests` compile `Assets/Core` et les tests EditMode avec le
SDK .NET, **sans aucune référence UnityEngine** :

```bash
dotnet test Tools/CoreTests/CoreTests.csproj
```

C'est la preuve automatique que `Core/` reste du C# pur, et cela permet de
lancer les tests sur une CI sans licence Unity.

## Build Android

```bash
powershell -ExecutionPolicy Bypass -File .\build-android.ps1
```

Le script détecte Unity, bascule sur le JDK 17 si `JAVA_HOME` pointe sur une
version plus récente, résout le NDK r23b, construit l'APK, vérifie l'activité
lançable avec `aapt` et affiche taille + SHA-256. Sortie :
`Builds/Android/colorflow-debug.apk`. Détails et dépannage : `BUILD.md`.

Configuration : portrait, package `com.digitalholding.colorflow`, IL2CPP,
ARMv7 + ARM64, minSdk 23. Le Canvas utilise `CanvasScaler` en **Scale With
Screen Size**, référence **1080×1920**, match `0.5`.

## Architecture

```
Assets/
  Core/                      # Domaine pur C# — AUCUNE référence Unity
    ColorFlow.Core.asmdef    # noEngineReferences: true
    Model/                   # RgbaColor, Creature, CreatureKind, GridModel,
                             # ConveyorQueue, LevelDefinition, LevelBuilder,
                             # GameState, GoalType
    Rules/                   # PaintResolver, PaintResult, PaintAllowance,
                             # ScoreRules, EconomyRules, GoalRules
    Flow/                    # GameController, CreatureSpawner, IRandomSource
  Game/                      # Couche Unity
    ColorFlow.Game.asmdef    # référence ColorFlow.Core
    Data/                    # LevelData, LevelCatalog, MetaProgress, SkinCatalog
    Presentation/            # GameSceneController, GridView, ConveyorView,
                             # HudView, ShopPanel, UiFactory, ArtFactory,
                             # MainMenuController, BootController
  Resources/Levels/          # 6 niveaux (.asset)
  Scenes/                    # Boot, MainMenu, Game
  Tests/
    EditMode/                # 86 tests NUnit (domaine uniquement)
    PlayMode/                # scènes Game / MainMenu / restart
Tools/CoreTests/             # compilation/tests hors Unity (CI)
```

### Séparation modèle / vue / contrôleur

- **Modèle** : `GridModel`, `ConveyorQueue`, `LevelDefinition` — état et règles,
  zéro Unity.
- **Règles** : `PaintResolver` décide quelle peinture appliquer (simple ou
  spéciale) et pourquoi c'est invalide ; `PaintAllowance` garantit qu'un effet
  de zone ne rend jamais morte une créature déjà posée ; `ScoreRules`,
  `EconomyRules` et `GoalRules` isolent score, monnaie et objectifs.
- **Contrôleur** : `GameController` orchestre spawn, tap, score, combo, chrono
  et transitions `Playing / Won / Lost`. Il est **pur C#** : le MonoBehaviour
  lui transmet seulement `Time.deltaTime` et les taps.
- **Vue** : `GameSceneController` + `GridView` / `ConveyorView` / `HudView` /
  `ShopPanel` construisent l'UI et écoutent les événements du contrôleur.

`Core/` ne contient que `using System;`, `System.Collections.Generic` et
`System.Globalization`. L'asmdef `noEngineReferences: true` fait échouer la
compilation Unity si une référence Unity y est ajoutée.

### Pourquoi Built-in 2D plutôt qu'URP 2D

- Uniquement des `Image` UI à plat → aucun besoin de post-processing, lumières
  2D ni shaders.
- URP ajoute un package + des assets de pipeline à configurer : contraire à la
  contrainte « pas de dépendance externe ».
- Built-in démarre plus vite et produit un APK plus léger ; le passage à URP
  reste possible plus tard sans toucher à `Core/`.

## Données de niveau

`LevelData` (ScriptableObject) expose :

| Champ | Rôle |
|---|---|
| `levelId` | identifiant technique |
| `displayName` | nom affiché |
| `rows` | une ligne de texte par rangée, `.` = vide, `1`..`9` = index palette |
| `palette` | couleurs hexadécimales (`#RRGGBB`) |
| `spawnInterval` | secondes entre deux créatures |
| `goalType` | `FillAll` (0), `ScoreTarget` (1), `TimeAttack` (2) |
| `targetScore` | score à atteindre (`ScoreTarget`) |
| `timeLimitSeconds` | durée max ; 0 = valeur par défaut de l'objectif |

Exemple (croix 7×7) :

```
rows:
- ..111..
- ..111..
- 1111111
- 1111111
- 1111111
- ..111..
- ..111..
palette:
- '#FF7A59'
spawnInterval: 1.4
goalType: 0
targetScore: 300
timeLimitSeconds: 0
```

Ajouter un niveau = créer un `LevelData` dans `Assets/Resources/Levels/`
(clic droit → **Create > Color Flow > Niveau**). Il apparaît automatiquement
dans le menu (grille de 3 colonnes), sans modification de code.

## Limites assumées du Sprint 2

- Pas de test sur appareil physique (vérification statique + tests Unity).
- Pas de nouvelle musique ni de voix (sons procéduraux réutilisés).
- Progression de niveaux non sauvegardée (seules pièces et skins le sont).
- Pas d'analytics, pas d'IAP, pas de publicité.
- Interface en français uniquement.
- Équilibrage (400 pts/60 s, prix des skins) à confirmer par des parties réelles.
