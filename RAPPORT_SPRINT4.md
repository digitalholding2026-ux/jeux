# RAPPORT FINAL — Color Flow

## Hotfix UI 2.5 + Sprint 4 : glisser-déposer, match-3 et œuf de Pâques

| | |
|---|---|
| **Livraison** | Hotfix Sprint 2.5 + Sprint 4 |
| **Version APK** | 0.2.0 (versionCode 3), build debug |
| **Plateforme** | Android, portrait, layout adaptatif 16:9 → 20:9 |
| **Moteur** | Unity 6000.0.23f1, Built-in 2D, IL2CPP ARMv7 + ARM64 |
| **Contrainte respectée** | zéro asset externe : visuels, icônes et sons générés par le code |
| **Tests** | 109 EditMode/Core + 9 PlayMode, **tous verts** |
| **APK** | `Builds/Android/colorflow-debug.apk` — 45,73 Mo |

---

## 1. Objectif et périmètre

Deux commandes ont été traitées dans cette livraison.

**Hotfix 2.5** — le lead a relevé trois défauts sur écran 20:9 : le bouton
« BOUTIQUE » mal ancré, les éléments de menu/HUD rognés par les encoches, et une
boutique muette en cas de fonds insuffisants. Le skin acheté devait aussi survivre
au redémarrage de l'application.

**Sprint 4** — remplacer le tap par un vrai **glisser-déposer** depuis le tapis,
introduire une résolution **match-3** (alignements, blobs Ligne/Bombe), remplir les
motifs par les alignements, garder un **chrono visible**, révéler la victoire case
par case et cacher un **niveau œuf de Pâques** (dinosaure) jusqu'à déblocage.

| Livrable | Contenu | Statut |
|---|---|---|
| **2.5-A** | `SafeArea` + menu ancré + bouton BOUTIQUE avec icône procédurale | ✅ |
| **2.5-B** | Feedback boutique (refus rouge + secousse / succès animé) | ✅ |
| **2.5-C** | Persistance du skin équipé via PlayerPrefs, visible au relaunch | ✅ |
| **4-A** | Drag & drop `IBeginDragHandler / IDragHandler / IEndDragHandler` | ✅ |
| **4-B** | `Match3Resolver` : alignements 3/4/5, Ligne, Bombe, Arc-en-ciel | ✅ |
| **4-C** | Objectifs par motif, chrono permanent, révélation de victoire | ✅ |
| **4-D** | Niveau secret « Le Dinosaure » débloqué après 3 victoires | ✅ |
| **4-E** | Tests Core/EditMode/PlayMode + APK + rapport | ✅ |

Contraintes tenues : couche `Core/` en C# pur sans référence Unity, aucun asset
importé, UI 100 % construite à l'exécution, tests déterministes.

---

## 2. Hotfix 2.5 — encoches, ancrage et bouton boutique

### 2.1 Une seule source de vérité pour l'aire sûre

Nouveau composant `SafeArea` (`Assets/Game/Presentation/SafeArea.cs`), posé sur un
`RectTransform` plein écran :

- lit `Screen.safeArea` et convertit ses bornes en **ancres normalisées** ;
- se recalcule si la résolution ou l'aire sûre change (rotation, barre système,
  fenêtre redimensionnée) ;
- retombe sur un plein écran si la plateforme renvoie une aire dégénérée (éditeur).

`UiFactory.CreateSafeArea(...)` crée ce conteneur. **Toute l'UI interactive** du menu
et de la scène de jeu vit dedans ; seuls le fond dégradé, le soleil, les nuages et
les particules restent en plein écran.

### 2.2 Menu reconstruit en ancrages

L'ancien menu plaçait chaque bloc à des coordonnées absolues autour du centre
(`y = 610`, `y = 470`, `y = -205`…), ce qui débordait sur un écran 20:9. Le nouveau
layout est ancré :

| Élément | Ancrage | Position |
|---|---|---|
| Titre « COLOR FLOW » | haut-centre | −16 px sous le bord sûr |
| Sous-titre | haut-centre | −186 px |
| Portefeuille | haut-droite | marge 24 / 20 px |
| Cartes de niveaux | haut-centre | grille 3 colonnes, à partir de −286 px |
| JOUER | barre basse, à gauche | 580×176, marge 24 px via la barre |
| BOUTIQUE | barre basse, à droite | 300×124, marge 24 px via la barre |
| Aide | bas-centre | au-dessus de la barre de boutons |

La barre basse est **étirée entre les deux bords sûrs** (largeur = aire sûre − 48)
et haute de 200 px : JOUER s'ancre à gauche, BOUTIQUE à droite. Sur un canvas 20:9
de 966 px de large, la barre mesure 918 px, JOUER occupe 24→604 et BOUTIQUE
642→942, soit 38 px d'écart et **aucun chevauchement**. Les cartes font 296×132
avec 24×22 px d'espacement ; le nombre de lignes s'adapte au nombre de niveaux
visibles (2 ou 3). Les libellés longs passent en `resizeTextForBestFit` (18–32 px)
pour ne jamais être tronqués. Aucun élément ne dépend plus du centre absolu de
l'écran.

### 2.3 Bouton BOUTIQUE et icône dessinée

Le bouton est désormais dans la **barre du bas**, aligné avec JOUER, à 24 px du bord
sûr droit. Il reste un bouton 3D (`UiFactory.CreateButton3D`) avec son ombre, son
enfoncement et son rebond.

L'icône est un **sac de boutique** dessiné par `ArtFactory.ShopIcon()` :

- corps arrondi, rabat supérieur légèrement grisé ;
- anse en anneau au-dessus du sac ;
- deux points de couture.

L'icône blanche est teintée par l'`Image` du bouton (violet), mesure 64×64 px et le
libellé est décalé à droite. Aucun sprite importé.

### 2.4 Scène de jeu

- `SafeArea` → `ShakeRoot` (secousses) → grille centrée 980×980, tapis ancré en bas
  (40 px, hauteur 430), HUD dans l'aire sûre **hors** du nœud secoué.
- La bulle d'aide du HUD a été déplacée dans l'interstice de 70 px entre la grille
  et le tapis : elle ne recouvre plus les cases du bas.
- Le chrono et les bulles de score/objectif restent sous le bord sûr supérieur.

### 2.5 Vérification automatisée

`Menu_Buttons_AreInsideTheSafeArea` récupère les coins monde du titre, de JOUER, de
BOUTIQUE, de l'aide et du portefeuille, les convertit dans le repère de l'aire sûre
et vérifie qu'ils sont **tous** contenus dans son `rect`. Le test échoue si un
élément dépasse, quelle que soit la résolution de l'éditeur.

---

## 3. Hotfix 2.5 — feedback boutique et persistance des skins

### 3.1 Deux issues, deux feedbacks

`ShopPanel` dispose maintenant d'un texte de feedback animé (fondu + pop) et d'un
accès direct à la ligne concernée.

**Fonds insuffisants** :

- le bouton d'achat reste **cliquable** (il n'est plus grisé-interdit) pour que le
  joueur comprenne le refus ;
- secousse horizontale amortie de 18 px du bouton ;
- texte rouge `Pas assez de pièces !` affiché 1,25 s puis fondu ;
- son d'erreur + vibration légère ;
- aucune pièce débitée, aucun skin débloqué.

**Achat réussi** :

- `MetaProgress.TryBuy` débite le prix (Lunettes 120, Chapeau 200, Couronne 320) ;
- le skin est immédiatement équipé ;
- message vert `Skin débloqué et équipé !` ;
- pop du blob, pop du portefeuille et gerbe d'étincelles ;
- son de validation + vibration ;
- la ligne passe à « ÉQUIPÉ » et le solde se met à jour.

### 3.2 Carte adaptée à l'aire sûre

La carte de boutique (960×1340) est enfant d'un `SafeArea` et se met à l'échelle au
moment de l'ouverture : `min(1, (largeur−24)/960, (hauteur−24)/1340)`, bornée à 0,55.
Sur un écran court ou avec une barre de navigation épaisse, la carte rétrécit au lieu
de déborder.

### 3.3 Persistance du skin

`MetaProgress` sauvegarde dans `PlayerPrefs` : pièces, skins possédés, skin équipé.
`ConveyorView` lit `MetaProgress.SelectedSkin` et affiche l'accessoire (lunettes,
chapeau ou couronne) sur **chaque blob** du tapis, y compris après relance.

Le test PlayMode `EquippedSkin_IsRestoredOnTheNextGameLaunch` achète la Couronne,
écrit le skin équipé, charge la scène Game, fait apparaître cinq blobs et vérifie
qu'au moins un accessoire est actif. C'est le scénario exact « je ferme, je relance ».

---

## 4. Sprint 4 — glisser-déposer depuis le tapis

### 4.1 Le tapis ne « tape » plus

`ConveyorDragRelay` est posé sur chaque emplacement et implémente les trois
interfaces Unity :

```csharp
IBeginDragHandler.OnBeginDrag  → ConveyorView.BeginDrag(index, eventData)
IDragHandler.OnDrag            → ConveyorView.DragTo(eventData)
IEndDragHandler.OnEndDrag      → ConveyorView.EndDrag(eventData)
```

Un simple clic ne peint plus : il affiche l'aide « Glisse le blob du tapis vers une
case du motif ! » et fait rebondir le blob. L'ancien `GameController.Tap()` reste
présent uniquement pour ne pas casser les tests des sprints 0 à 2 ; **l'UI ne
l'appelle jamais**.

### 4.2 Ghost, surbrillance et retour

- Au début du glisser, un **ghost** de 190 px (même sprite/couleur que le blob) est
  créé dans le canvas racine et suit le doigt ; l'emplacement d'origine est atténué
  à 25 % d'opacité.
- Pendant le glisser, `GridView.TryScreenPointToCell` convertit le point écran en
  case (gestion des espaces entre cases, du scale canvas et de l'aire sûre).
- La case survolée est surlignée en **vert** si `GameController.IsValidDropTarget`
  l'accepte, en **rose** sinon. Aucune règle de validité n'est dupliquée dans l'UI.
- Au relâchement :
  - case vide valide → `Drop()` ;
  - case peinte / occupée / hors motif → `PlayDropReturn` : remontée de 42 px et
    rebond `OutBounce`, son d'erreur, vibration, message explicite ;
  - lâcher hors grille → même retour + « Lâche le blob sur une case du motif ! ».

### 4.3 Capacité et défaite

Le tapis garde **5 emplacements**. La création d'un blob spécial passe par le même
`TryEnqueue` : si un 6ᵉ blob doit entrer, l'ajout échoue et la partie est perdue.
Le joueur doit donc vider le tapis, pas seulement poser.

### 4.4 Choix assumé : pas de gravité

La grille est **statique** : pas de chute de blobs ni de cascades après un
alignement. C'est un choix de lisibilité pour ce sprint ; les cases peintes
libèrent l'emplacement et le motif reste lisible.

---

## 5. Sprint 4 — résolution match-3 et blobs spéciaux

`Match3Resolver` (C# pur, `Assets/Core/Rules/`) concentre toutes les règles.

### 5.1 Placement

- Un blob **normal** se pose sur n'importe quelle case du motif **vide et non
  peinte**, quelle que soit sa couleur. La couleur du blob ne contraint pas la case.
- La peinture n'a lieu **que** lors d'un alignement, et chaque case peinte prend la
  **couleur cible du motif** (et non celle du blob). Le motif garde donc ses couleurs.
- Un blob **spécial** peut être lâché sur n'importe quelle case du motif non peinte,
  même occupée : il déclenche son effet au lieu de se poser.

### 5.2 Alignements et création de spéciaux

| Alignement | Effet |
|---|---|
| **3** | Les 3 blobs explosent (particules + son + vibration), les cases sont peintes et les blobs disparaissent |
| **4** | Même explosion, puis création d'un **Blob Ligne** renvoyé sur le tapis |
| **5 ou +** | Même explosion, puis création d'un **Blob Bombe** renvoyé sur le tapis |

Un alignement en croix (3 horizontal + 3 vertical) peint **l'union** des deux runs
(5 cases) sans créer de spécial. La longueur retenue est la plus longue des deux
directions.

La création passe par `CreatureSpawner.CreateSpecial`, qui émet `Spawned` : le
spécial est enfilé sur le tapis comme un blob normal et peut donc provoquer un
débordement (6ᵉ) et une défaite. C'est une tension volontaire.

### 5.3 Effets des spéciaux

| Blob | Effet au lâcher |
|---|---|
| **Ligne** | Peint la ligne **ou** la colonne traversant la case ciblée, en choisissant celle qui a le plus de cases peignables |
| **Bombe** | Peint le carré **3×3** autour de la case, tronqué aux bords et au motif |
| **Arc-en-ciel** | Peint la case ciblée et ses 4 voisines (forme **+**, 5 cases) |

Toutes les peintures sont filtrées : seules les cases du motif non peintes comptent.
Si le spécial ne peint rien, le drop est refusé (`NoEffect`) et le blob revient.

Le barème de score et les combos du Sprint 2 s'appliquent au nombre de cases peintes
(`ScoreRules.PointsFor(paintedCount, multiplier)`).

### 5.4 Détection d'impasse

`Match3Resolver.IsBlocked(grid, specialOnBelt)` renvoie vrai si :

- le motif n'est pas complet ;
- aucune case libre et non peinte ne reste ;
- **aucun** blob spécial n'est sur le tapis ;
- aucun alignement de 3 ne subsiste sur la grille.

`GameController.EvaluateDeadlock()` est appelée après chaque drop et après chaque
action ; dans ce cas la partie passe à `Lost` et le spawner s'arrête. Une grille
pleine sans spécial est donc immédiatement une défaite.

### 5.5 Couverture de tests

`Match3Tests` vérifie : refus hors motif / case peinte / case occupée, alignement
horizontal et vertical, union en croix, création Ligne et Bombe, effets des trois
spéciaux, placeabilité, `WouldMatch`, `HasMatch`, `IsBlocked`, scoring et combos.

`AutoPlayMatch3Tests` simule un joueur **intelligent** qui, à chaque tour, choisit
un blob et une case produisant un alignement (placement préparatoire sinon), et
gagne deux niveaux complets (damier et croix) sans intervention. Ce test prouve que
la boucle est réellement jouable et menable à la victoire, pas seulement que les
règles unitaires passent.

**Total : 109 tests EditMode**, exécutés à la fois par `dotnet test
Tools/CoreTests/CoreTests.csproj` et par le Test Runner Unity — mêmes sources,
mêmes 109 résultats verts.

---

## 6. Sprint 4 — objectifs, chrono, victoire et œuf de Pâques

### 6.1 Objectifs

- **FillAll** : le motif se remplit désormais uniquement par les alignements ;
  la barre de progression compte les cases peintes sur les cases cibles.
- **ScoreTarget** (60 s) et **TimeAttack** (30 s) sont conservés : le score vient
  des alignements et des spéciaux.
- La victoire est évaluée après chaque drop (`EvaluateGoalAfterAction`) ; la
  défaite par temps ou score est conservée.

### 6.2 Chrono toujours visible

`HudView.SetGoal(label, hasTimeLimit)` ne cache plus la pastille :

- niveau limité → compte à rebours `N s`, pastille jaune, puis orange sous 30 s et
  rose sous 10 s ;
- niveau sans limite → chrono **ascendant** `0:00`, pastille bleue, mis à jour
  chaque frame avec `GameController.ElapsedTime`.

Le joueur voit donc toujours le temps écoulé ou restant, conformément à la demande.

### 6.3 Révélation de victoire

À l'état `Won` :

1. enregistrement de la victoire (`MetaProgress.RecordWin`, une seule fois par
   niveau) et son de victoire ;
2. la coroutine `RevealThenResult` parcourt le motif ligne par ligne ;
3. chaque case peinte reçoit un `PulseCell` (pop élastique) et une gerbe
   d'étincelles de sa couleur cible, toutes les 22 ms ;
4. après un court temps de respiration, l'écran de fin s'affiche (objectif, score,
   pièces créditées **une seule fois** grâce à `_coinsAwarded`).

Un redémarrage pendant la révélation l'interrompt proprement (`StopReveal`).

### 6.4 Niveau secret « Le Dinosaure »

Nouvel asset `Assets/Resources/Levels/Level_Dino.asset` :

- motif 11×11 en silhouette de dinosaure (58 cases cibles, vert `#5AD26F`) ;
- `requiredWins: 3`, `timeLimitSeconds: 0` (chrono ascendant) ;
- `LevelData.RequiredWins` / `IsHidden` exposent la règle.

`MetaProgress.TotalWins` compte les **premières victoires uniques** : `RecordWin`
ignore un niveau déjà gagné, ce qui empêche de farmer le déblocage en rejouant le
même niveau. `MetaProgress.IsUnlocked(level)` compare `TotalWins` au seuil.

`MainMenuController` filtre entièrement les niveaux verrouillés : ils ne sont ni
affichés, ni sélectionnables, et aucune carte « coming soon » ne trahit leur
existence. Avec 3 victoires, une 7ᵉ carte « Le Dinosaure » apparaît.

Le test `EasterEgg_StaysHidden_UntilThreeWins` vérifie 6 cartes au départ, puis
7 cartes après trois victoires enregistrées.

---

## 7. Architecture et qualité

### 7.1 Séparation maintenue

| Couche | Rôle | Dépendances |
|---|---|---|
| `Assets/Core/` | grille, spawner équitable, score, objectifs, résolution match-3, contrôleur de partie | C# pur, **aucune** référence Unity |
| `Assets/Game/Data/` | ScriptableObjects de niveaux, PlayerPrefs, catalogue de skins | Unity minimal |
| `Assets/Game/Presentation/` | UI procédurale, drag & drop, particules, HUD, menu | Unity |
| `Assets/Editor/` | build Android | UnityEditor |
| `Assets/Tests/` | EditMode + PlayMode | NUnit / UTF |

Le `GameController` reste la seule autorité de jeu ; l'UI observe ses événements
(`DropResolved`, `DropRejected`, `PixelPainted`, `StateChanged`, `ScoreChanged`,
`ComboChanged`…). La validité d'un drop est calculée par `IsValidDropTarget`, pas
réimplémentée dans la vue.

### 7.2 Nouveaux fichiers notables

| Fichier | Rôle |
|---|---|
| `Assets/Core/Rules/Match3Resolver.cs` | alignements, spéciaux, impasse |
| `Assets/Core/Model/DropResult.cs` | résultat de drop, raison d'échec, case peinte |
| `Assets/Game/Presentation/SafeArea.cs` | ancrage sur `Screen.safeArea` |
| `Assets/Game/Presentation/ConveyorDragRelay.cs` | relais des interfaces de drag |
| `Assets/Game/Presentation/ConveyorView.cs` | ghost, retour rebond, accessoires |
| `Assets/Game/Presentation/GridView.cs` | blobs posés, surbrillance, screen→case, pulse |
| `Assets/Game/Presentation/AudioManager.cs` | SFX + combo + musique, tout synthétisé |
| `Assets/Resources/Levels/Level_Dino.asset` | œuf de Pâques |
| `Assets/Tests/EditMode/Match3Tests.cs` | 20+ tests de règles match-3 |
| `Assets/Tests/EditMode/AutoPlayMatch3Tests.cs` | auto-jeu intelligent |
| `Assets/Tests/PlayMode/SprintTwoPointFiveTests.cs` | 5 tests UI/persistance/secret |
| `Assets/Tests/PlayMode/SprintThreeAudioTests.cs` | 3 tests audio (musique, combo, stop/restart) |

### 7.3 Game feel et audio (Sprint 3 finalisé)

| Effet | Implémentation |
|---|---|
| Blobs qui « respirent » | `ConveyorView.Update` : scale `1 ± 4,5 %` + flottement vertical, ombre synchrone |
| Apparition rebond | `Tween.Scale` 0,25 → 1 en `Ease.OutElastic` (0,55 s) |
| Case remplie | `Tween.PopCell` : 0,5 → 1,2 → 1,0 en 0,12 s |
| Combo | texte « COMBO ×N ! » qui monte et s'estompe + `Tween.Shake` du plateau |
| Victoire | confettis (110 particules) + « GAGNÉ ! » en `OutBack` |
| Défaite | « PERDU » qui tombe du haut en `OutBounce` + son descendant |
| Haptique | `Handheld.Vibrate` : tap court, impact sur combo/spécial, game over |
| Sons | 100 % `AudioClip.Create` : pop, splotch (bruit filtré), spawn, clic, erreur, arpège de victoire, descente de défaite |
| Son de combo | carillon 880 Hz + harmoniques, **+2 demi-tons par palier** (x2 → x7), source dédiée |
| Musique | boucle de 12,8 s à 22 050 Hz (nappe Do/La m/Fa/Sol + arpège), fondu aux deux bords, `DontDestroyOnLoad` |

Aucun fichier `.wav`/`.mp3` : tout est calculé au démarrage (~1,1 Mo de PCM pour
la boucle). `AudioManager.Init()` lance la musique une seule fois ; elle survit aux
changements de scène et n'est jamais redémarrée.

### 7.4 Résultats des tests

| Suite | Nombre | Résultat |
|---|---|---|
| Core via `dotnet test Tools/CoreTests/CoreTests.csproj` | 109 | ✅ 109/109 |
| Unity EditMode (mêmes sources) | 109 | ✅ 109/109 |
| Unity PlayMode | 12 | ✅ 12/12 |

PlayMode :

| Test | Ce qu'il prouve |
|---|---|
| `MainMenu_Loads_AndStartsGame` | menu construit, JOUER lance la scène Game |
| `GameScene_Loads_AndDragFillsACell` | un vrai drag pose un blob et le retire du tapis |
| `GameScene_ThreeAlignedBlobs_PaintTheMotif` | 3 blobs alignés peignent 3 cases |
| `GameScene_Restart_ResetsTheGrid` | REJOUER vide grille, peinture et tapis |
| `Menu_Buttons_AreInsideTheSafeArea` | aucun élément rogné (coins dans l'aire sûre) |
| `Shop_WithoutEnoughCoins_ShowsRedFeedbackAndShakes` | feedback rouge, aucune pièce débitée |
| `Shop_Purchase_DeductsCoinsAndEquipsTheSkin` | achat, débit, équipement, message vert |
| `EquippedSkin_IsRestoredOnTheNextGameLaunch` | skin rechargé depuis PlayerPrefs au relaunch |
| `EasterEgg_StaysHidden_UntilThreeWins` | 6 cartes puis 7 après 3 victoires |
| `Audio_StartsCodeGeneratedMusicLoop` | la boucle de fond démarre via `Init()` |
| `Audio_ComboRaisesPitchWithoutStoppingMusic` | x2/x3/x6 ne coupent pas la musique |
| `Audio_StopAndRestartMusic` | `StopMusic`/`PlayMusic` fonctionnent |

Les tests PlayMode passent en batchmode `-nographics` ; ils exercent la vraie
hiérarchie UI, les vrais `Button`, le vrai pipeline de drag et les vraies scènes.

---

## 8. Build APK et vérifications

### 8.1 Commande

```powershell
$env:JAVA_HOME='C:\Program Files\Microsoft\jdk-17.0.20.101-hotspot'
$env:ANDROID_HOME='C:\Program Files\Unity\Hub\Editor\6000.0.23f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK'
$env:ANDROID_SDK_ROOT=$env:ANDROID_HOME
$env:ANDROID_NDK_HOME='C:\Program Files\Unity\Hub\Editor\6000.0.23f1\Editor\Data\PlaybackEngines\AndroidPlayer\NDK'
$env:ANDROID_NDK_ROOT=$env:ANDROID_NDK_HOME
powershell -ExecutionPolicy Bypass -File .\build-android.ps1
```

Le script vérifie le JDK 17, le NDK r23b, lance Unity en batchmode
(`BuildScript.BuildAndroidDebug`), contrôle avec `aapt` qu'une **activité lançable**
existe, puis calcule le SHA-256. Le premier build complet de cette livraison a pris
**1246 s** (20 min 46 s) ; le rebuild final après les ajustements de la barre de
boutons a pris **205 s** (relink incrémental).

### 8.2 Artefact

| | |
|---|---|
| **Chemin** | `Builds/Android/colorflow-debug.apk` |
| **Taille** | 47 948 058 octets (45,73 Mo) |
| **SHA-256** | `0394C6807EA0D26785CFCEF957FA695F188D50EC962049209D74EC08733C16FD` |
| **Package** | `com.digitalholding.colorflow` |
| **Version** | 0.2.0 — versionCode 3 |
| **SDK** | minSdk 23, targetSdk 35, compileSdk 35 |
| **ABI** | `arm64-v8a` + `armeabi-v7a` (IL2CPP) |
| **Libellé** | Color Flow |
| **Activité** | `com.unity3d.player.UnityPlayerActivity` (lanceable) |

### 8.3 Contenu vérifié

- **3 scènes** : `assets/bin/Data/level0`, `level1`, `level2` (Boot, MainMenu,
  Game) ;
- **7 assets de niveaux** dans le build, dont `Level_Dino` (fichier
  `assets/bin/Data/7c1e5b9a4d2f43a1b6c8d0e2f4a6b8c0`, 504 octets) : l'œuf de Pâques
  est bien embarqué ;
- `global-metadata.dat`, `ScriptingAssemblies.json` et les deux ABIs présents ;
- icône adaptative `res/mipmap-anydpi-v26/app_icon.xml` déclarée ;
- `aapt dump badging` confirme `launchable-activity`, donc l'APK s'installe avec
  icône et bouton « Ouvrir ».

### 8.4 Limite d'environnement

Aucun appareil ni émulateur Android n'était connecté (`adb devices` vide, aucun AVD
installé). L'APK n'a donc **pas** été installé sur un terminal physique dans cet
environnement. La validation fonctionnelle repose sur les 9 tests PlayMode exécutés
dans l'éditeur, sur la compilation IL2CPP réussie et sur l'inspection `aapt`/ZIP.
Un `adb install -r Builds/Android/colorflow-debug.apk` reste la première étape de la
recette device.

---

## 9. Décisions de design, limites et suite

### 9.1 Décisions structurantes

1. **Placement indépendant de la couleur** : le joueur choisit la case, l'alignement
   décide de la peinture. Cela évite les blocages « mauvaise couleur, mauvaise
   case » et rend le match-3 lisible.
2. **Peinture à la couleur du motif** : les cases peintes gardent les couleurs
   voulues par le niveau, même si le blob posé était d'une autre couleur.
3. **Spéciaux posables sur case occupée** : un spécial n'est pas un blob à poser,
   c'est un effet à déclencher ; l'autoriser sur une case occupée évite les
   frustrations de ciblage.
4. **Débordement = défaite** : les spéciaux créés retournent sur le tapis, ce qui
   peut le remplir. La gestion du tapis reste une décision, pas un automatisme.
5. **Impasse explicite** : grille pleine sans spécial = perte immédiate ; c'est la
   traduction directe de la règle demandée.
6. **Déblocage par victoires uniques** : `TotalWins` ne compte pas les replays,
   pour que l'œuf de Pâques reste une vraie récompense de progression.
7. **Aire sûre au niveau du canvas** : une seule transformation, tous les écrans en
   héritent ; le décor reste plein écran pour ne pas laisser de bandes vides.

### 9.2 Limites assumées

- **Pas de test sur device** dans cet environnement (voir 8.4) ; les perfs IL2CPP
  réelles et le ressenti tactile restent à valider.
- **Version 0.2.0 / code 3 inchangés** : le build est un debug de livraison, pas un
  build de release signé. Un passage en 0.3.0/0.4.0 avec keystore de release reste à
  faire.
- **Pas de gravité ni de cascades** : choix de lisibilité, mais le plateau peut
  paraître statique après quelques coups.
- **Difficulté non réglée par playtest humain** : les niveaux Score 300 pts / 30 s
  peuvent être tendus ; l'équilibrage devra être ajusté sur device.
- **Aide statique** : pas de solveur de coup, le hint se contente d'expliquer le
  geste.
- **4 skins, français uniquement**, pas d'écran de réglages audio/haptique.
- **Accessibilité** : les spéciaux ont un symbole (bombe, barres, étoile) mais pas
  de mode daltonien dédié.

### 9.3 Prochaines étapes proposées

1. Recette device (20:9 et 16:9) : installation ADB, vérification des encoches, du
   drag, du feedback boutique et du relaunch.
2. Playtest et équilibrage des niveaux Score/Temps, courbe de spawn et fréquence des
   spéciaux.
3. Tutoriel court au premier lancement (glisser, aligner, utiliser un spécial).
4. Réglages (musique, SFX, vibration) et sauvegarde de la progression des niveaux.
5. Build de release signé (AAB + APK), versionnement 0.4.0, et éventuellement
   cascades/gravité en option.
6. Localisation EN/FR et mode daltonien.

**Conclusion** — Le hotfix 2.5 et le Sprint 4 sont livrés : UI ancrée dans l'aire
sûre, boutique réactive et persistante, glisser-déposer complet, match-3 avec
spéciaux, objectifs, chrono permanent, révélation de victoire et niveau secret. Les
109 tests EditMode/Core et les 9 tests PlayMode passent, et l'APK debug de 45,73 Mo
est généré et vérifié.
