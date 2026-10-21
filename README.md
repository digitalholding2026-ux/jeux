# Color Flow — Prototype Sprint 0

Puzzle hybride mobile (portrait, public 8–14 ans). Ce dépôt contient **uniquement
la boucle de jeu core** du Sprint 0 : aucun art final, aucun son, aucune
monétisation, aucune sauvegarde.

## Contenu

- Grille N×M décrite par des données, peinte par des créatures colorées.
- Tapis roulant FIFO de 5 créatures.
- Spawn automatique de créatures issues des couleurs encore utiles.
- Victoire quand le motif est complet, défaite quand le tapis déborde.
- 3 niveaux placeholder : croix, cœur, damier.
- Écran de fin avec bouton « Rejouer ».

## Prérequis

| Outil | Version |
|---|---|
| Unity | **Unity 6 LTS** (`6000.0.23f1` ou plus récent) |
| Modules Unity | Android Build Support (+ SDK, NDK, JDK) pour l'APK |
| Test Framework | inclus via `Packages/manifest.json` (1.4.5) |

Le projet utilise le **render pipeline Built-in en 2D** (voir la justification
plus bas) : aucune dépendance URP, aucun package externe.

## Ouvrir le projet

1. Unity Hub → **Add** → sélectionner le dossier du dépôt.
2. Ouvrir avec Unity 6 LTS. La première ouverture importe les assets et
   compile les assemblies (`ColorFlow.Core`, `ColorFlow.Game`, tests).
3. Ouvrir `Assets/Scenes/Boot.unity` puis **Play**.

Les trois scènes sont déjà dans **File > Build Settings** : `Boot`, `MainMenu`,
`Game`.

## Jouer

- Le jeu démarre sur le menu, 3 boutons (un par niveau).
- Une créature apparaît toutes les 1,2 à 1,6 s sur le tapis (5 emplacements).
- **Taper une créature** peint le prochain pixel de sa couleur (parcours
  haut-gauche → bas-droite).
- Une créature dont la couleur n'est plus nécessaire est refusée : elle reste
  sur le tapis et un message l'indique.
- Tapis plein à la 6ᵉ créature → **Perdu**. Motif complet → **Gagné**.

## Tests

### Dans l'éditeur

**Window > General > Test Runner**

- Onglet **EditMode** → *Run All*
- Onglet **PlayMode** → *Run All* (smoke test de la scène `Game`)

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

1. **File > Build Profiles** (ou Build Settings) → **Android** → *Switch Platform*.
2. Vérifier dans **Player Settings** :
   - Orientation : **Portrait** ;
   - Package name : `com.digitalholding.colorflow` ;
   - Scripting backend : **IL2CPP**, architecture **ARM64** ;
   - Minimum API level : 23.
3. **Build** → APK, ou **Build And Run** avec un appareil branché en USB
   (débogage USB activé).

Le Canvas utilise `CanvasScaler` en **Scale With Screen Size**, référence
**1080×1920**, match `0.5` : l'UI s'adapte aux ratios 18:9 à 21:9.

## Architecture

```
Assets/
  Core/                      # Domaine pur C# — AUCUNE référence Unity
    ColorFlow.Core.asmdef    # noEngineReferences: true
    Model/                   # RgbaColor, Creature, GridModel, ConveyorQueue,
                             # LevelDefinition, LevelBuilder, GameState
    Rules/                   # PaintResolver, PaintResult, PaintFailureReason
    Flow/                    # GameController, CreatureSpawner, IRandomSource
  Game/                      # Couche Unity
    ColorFlow.Game.asmdef    # référence ColorFlow.Core
    Data/LevelData.cs        # ScriptableObject -> LevelDefinition
    Presentation/            # GameSceneController, GridView, ConveyorView,
                             # HudView, UiFactory, MainMenuController,
                             # BootController, LevelCatalog
  Resources/Levels/          # 3 niveaux placeholder (.asset)
  Scenes/                    # Boot, MainMenu, Game
  Tests/
    EditMode/                # 50 tests NUnit (domaine uniquement)
    PlayMode/                # smoke test scène Game + restart
Tools/CoreTests/             # compilation/tests hors Unity (CI)
```

### Séparation modèle / vue / contrôleur

- **Modèle** : `GridModel`, `ConveyorQueue`, `LevelDefinition` — état et règles,
  zéro Unity.
- **Règles** : `PaintResolver` décide quelle peinture appliquer ou pourquoi
  c'est invalide.
- **Contrôleur** : `GameController` orchestre spawn, tap, transitions
  `Playing / Won / Lost`. Il est **pur C#** : le MonoBehaviour lui transmet
  seulement `Time.deltaTime` et les taps.
- **Vue** : `GameSceneController` + `GridView` / `ConveyorView` / `HudView`
  construisent l'UI et écoutent les événements du contrôleur.

`Core/` ne contient que `using System;`, `System.Collections.Generic` et
`System.Globalization`. L'asmdef `noEngineReferences: true` fait échouer la
compilation Unity si une référence Unity y est ajoutée.

### Pourquoi Built-in 2D plutôt qu'URP 2D

- Sprint 0 : uniquement des `Image` UI à plat → aucun besoin de post-processing,
  lumières 2D ni shaders.
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
```

Ajouter un niveau = créer un `LevelData` dans `Assets/Resources/Levels/`
(clic droit → **Create > Color Flow > Niveau**). Il apparaît automatiquement
dans le menu, sans modification de code.

## Limites assumées du Sprint 0

- Aucun art final, son, haptique, tutoriel, analytics, IAP ou pub.
- UI construite à l'exécution (pas de prefabs) pour rester sans art.
- Aucune sauvegarde de progression.
- 3 niveaux uniquement.
