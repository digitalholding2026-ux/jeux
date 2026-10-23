# Color Flow — Hotfix 2.5 + Sprint 4

Puzzle hybride mobile (portrait, public 8–14 ans). Le dépôt contient le jeu
complet **Sprint 4** : glisser-déposer des blobs, résolution match-3, blobs
spéciaux, score et combos, objectifs variables, chrono permanent, économie de
pièces, boutique de skins et sauvegarde locale. Le hotfix **2.5** ajoute l'UI
adaptée aux encoches, le feedback de boutique et la persistance du skin.

Aucun asset externe : tous les visuels (blobs, symboles, pièces, accessoires,
icône de boutique), les animations, l'UI et les sons sont **générés par le code**
à l'exécution (SFX, combo qui monte en pitch, musique de fond en boucle).

📄 Rapport détaillé : [RAPPORT_SPRINT4.md](RAPPORT_SPRINT4.md)
📌 Règle de publication : [AGENTS.md](AGENTS.md) — chaque travail se termine par
tests, build APK, commit, push et release `vX.Y.Z`.

## Contenu

- Grille N×M décrite par des données, remplie par des **alignements de 3 blobs**.
- Tapis roulant FIFO de 5 créatures, spawn équitable (aucune couleur morte).
- **Glisser-déposer** : le blob suit le doigt, la case cible est surlignée en vert
  ou en rose, un drop invalide rebondit sur le tapis.
- **Match-3** : 3 blobs alignés peignent le motif, 4 créent un **Blob Ligne**,
  5 créent un **Blob Bombe**.
- **3 blobs spéciaux** : bombe 3×3, ligne, arc-en-ciel (forme en +).
- **Score** et **combos ×2 / ×3 / ×4** sur 1,5 s.
- **3 objectifs** : remplir le motif, atteindre un score en 60 s, remplir en 30 s.
- **Chrono toujours visible** : compte à rebours ou chrono ascendant.
- **6 niveaux** visibles + **1 niveau secret** (dinosaure) débloqué après
  3 victoires uniques.
- **Pièces d'or** gagnées selon le score, **boutique** et **4 skins**.
- **Aire sûre** respectée sur tous les écrans (encoche, barre système, 20:9).
- Sauvegarde `PlayerPrefs` (pièces, skins possédés, skin équipé, victoires).
- Écran de fin avec révélation du motif case par case, score et pièces gagnées.

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

- Le menu propose une grille de niveaux, une **bourse de pièces** et un bouton
  **BOUTIQUE** ancré dans l'aire sûre.
- Une créature apparaît régulièrement sur le tapis (5 emplacements).
- **Glisser un blob du tapis vers une case libre du motif.** La case cible est
  surlignée en vert si le drop est valide, en rose sinon ; un drop refusé renvoie
  le blob sur le tapis avec un rebond.
- **3 blobs de même couleur alignés** (ligne ou colonne) explosent, peignent
  leurs cases et disparaissent.
- **4 alignés** créent un Blob Ligne ; **5 alignés** créent un Blob Bombe. Les
  spéciaux retournent sur le tapis et se lancent sur n'importe quelle case du
  motif non peinte.
- **Bombe** : peint le carré 3×3 autour de la case visée.
- **Arc-en-ciel** : peint la case visée et ses quatre voisines.
- **Ligne** : peint la rangée ou la colonne la plus rentable.
- **Combo** : enchaîner deux actions qui peignent en moins de 1,5 s multiplie
  les points (×2, ×3, ×4 maximum).
- Tapis plein à la 6ᵉ créature → **Perdu**. Grille pleine sans spécial et sans
  alignement possible → **Perdu** (ou score non atteint / temps écoulé).
- Motif entièrement peint → **Gagné** : révélation case par case, puis pièces
  créditées automatiquement.

### Objectifs

| Objectif | Victoire | Défaite |
|---|---|---|
| `FillAll` | motif entièrement peint | tapis plein, grille bloquée |
| `ScoreTarget` | score cible avant la limite (60 s par défaut) | chrono, tapis plein, motif fini avant le score |
| `TimeAttack` | motif fini avant la limite (30 s par défaut) | chrono, tapis plein |

### Niveau secret

« Le Dinosaure » (motif 11×11) n'apparaît dans le menu qu'après **3 victoires sur
des niveaux différents**. Les replays ne comptent pas : le déblocage est une vraie
récompense de progression.

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

- Onglet **EditMode** → *Run All* (109 tests)
- Onglet **PlayMode** → *Run All* (9 tests)

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
                             # GameState, GoalType, DropResult
    Rules/                   # PaintResolver, PaintResult, PaintAllowance,
                             # Match3Resolver, ScoreRules, EconomyRules, GoalRules
    Flow/                    # GameController, CreatureSpawner, IRandomSource
  Game/                      # Couche Unity
    ColorFlow.Game.asmdef    # référence ColorFlow.Core
    Data/                    # LevelData, LevelCatalog, MetaProgress, SkinCatalog
    Presentation/            # GameSceneController, GridView, ConveyorView,
                             # ConveyorDragRelay, SafeArea, HudView, ShopPanel,
                             # UiFactory, ArtFactory, MainMenuController,
                             # BootController
  Resources/Levels/          # 7 niveaux (.asset, dont le niveau secret)
  Scenes/                    # Boot, MainMenu, Game
  Tests/
    EditMode/                # 109 tests NUnit (domaine + auto-play match-3)
    PlayMode/                # 9 tests scènes / drag / boutique / secret
Tools/CoreTests/             # compilation/tests hors Unity (CI)
```

### Séparation modèle / vue / contrôleur

- **Modèle** : `GridModel`, `ConveyorQueue`, `LevelDefinition` — état et règles,
  zéro Unity.
- **Règles** : `PaintResolver` décide quelle peinture appliquer (simple ou
  spéciale) et pourquoi c'est invalide ; `PaintAllowance` garantit qu'un effet
  de zone ne rend jamais morte une créature déjà posée ; `ScoreRules`,
  `EconomyRules` et `GoalRules` isolent score, monnaie et objectifs.
- **Contrôleur** : `GameController` orchestre spawn, drop match-3, score, combo,
  chrono et transitions `Playing / Won / Lost`. Il est **pur C#** : le MonoBehaviour
  lui transmet seulement `Time.deltaTime` et les positions de drop.
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

## Limites assumées de la livraison

- Pas de test sur appareil physique (vérification statique + tests Unity).
- APK debug en version 0.3.0 / code 5, non signé pour la release.
- Pas de gravité ni de cascades : le plateau est statique après chaque coup.
- Progression des 6 niveaux standard non verrouillée ; seules les victoires
  (compteur de déblocage et œuf de Pâques) sont sauvegardées.
- Pas d'analytics, pas d'IAP, pas de publicité, pas de réglages audio/haptique.
- Interface en français uniquement, sans mode daltonien dédié.
- Équilibrage (score/temps, prix des skins) à confirmer par des parties réelles.
