# RAPPORT SPRINT 2 — Color Flow

## Profondeur stratégique, score et méta-jeu

| | |
|---|---|
| **Sprint** | 2 — « La boucle devient stratégique » |
| **Version livrée** | v0.2.0 (APK debug) |
| **Plateforme** | Android, portrait, 1080×1920 |
| **Moteur** | Unity 6000.0.23f1, Built-in 2D, IL2CPP ARMv7 + ARM64 |
| **Contrainte respectée** | zéro asset externe : 100 % des visuels et sons sont générés par le code |
| **Tests** | 86 tests Core/EditMode + 3 tests PlayMode, **tous verts** |

---

## 1. Objectif et périmètre

Le lead a validé le Sprint 1 (UI, blobs, juice) mais a pointé le vrai problème :
**« taper pour remplir » ne demande aucune décision**. Le Sprint 2 devait ajouter de la
profondeur sans casser le socle `Core/` ni la règle d'équité du Sprint 0.

Quatre livrables ont été réalisés :

| Livrable | Contenu | Statut |
|---|---|---|
| **A** | 3 blobs spéciaux (bombe 3×3, arc-en-ciel, ligne), tirés via l'équité | ✅ |
| **B** | Score, combos ×2/×3/×4 sur 1,5 s, popup flottant animé | ✅ |
| **C** | `GoalType` FillAll / ScoreTarget (60 s) / TimeAttack (30 s) + 3 niveaux | ✅ |
| **D** | Pièces d'or, boutique dans le menu, 3 skins achetables, PlayerPrefs | ✅ |

Le jeu se termine désormais par un écran qui dit **« Objectif atteint »** ou
**« Objectif manqué »**, avec le score et les pièces gagnées.

---

## 2. Livrable A — les trois blobs spéciaux

### 2.1 Comportement

| Blob | Couleur affichée | Effet au tap | Symbole dessiné |
|---|---|---|---|
| **Bombe** | Gris ardoise `#4A5163` | Peint le carré **3×3** autour du prochain pixel de sa couleur | Bombe noire à mèche dorée |
| **Arc-en-ciel** | Blob multicolore (dégradé HSV généré) | Peint **le prochain pixel cible, quelle que soit sa couleur** | Étoile jaune |
| **Ligne** | Bleu profond `#1F4E9C` | Peint **toute la rangée ou colonne** la plus rentable | Trois barres blanches |

Les symboles sont posés sur une pastille blanche en bas à droite du blob : ils restent
lisibles sur toutes les couleurs et sur le blob arc-en-ciel.

### 2.2 Tirage et équité

`CreatureSpawner.SpecialChancePercent = 10` : à chaque naissance, un tirage à
**1 chance sur 10** décide du type, puis un second tirage répartit bombe / arc-en-ciel /
ligne. La **couleur métier** est toujours tirée par la formule d'équité historique :

```
weight(c) = max(0, remainingPixels(c) − creaturesOnBelt(c))
```

Une créature spéciale ne peut donc **jamais apparaître morte** : sa couleur a encore
des pixels à peindre. Pour l'arc-en-ciel, cette couleur métier sert de garantie
d'utilité ; l'effet ignore la couleur réelle de la cible.

### 2.3 Le garde-fou d'équité des effets de zone (PaintAllowance)

Une bombe ou une ligne peint plusieurs pixels, parfois d'autres couleurs. Sans
garde-fou, elle pouvait épuiser une couleur alors qu'un blob de cette couleur attendait
sur le tapis : ce blob devenait inutilisable, ce que la règle d'équité interdit.

Nouveau budget, calculé avant chaque action :

```
allowed(c) = pixelsRestants(c) − créaturesDeCouleurCSurLeTapis(hors blob tapé)
```

Chaque case peinte consomme 1 point de `allowed(c)`. Une bombe peut donc peindre
**au maximum** ce qui laisse au moins un pixel à chaque blob déjà posé. La créature
tapée est exclue du calcul : son propre pixel est toujours autorisé.

**Conséquence de game design** : les pouvoirs ne « pourrissent » jamais la file. Ils
accélèrent, mais ne créent jamais d'impasse.

---

## 3. Livrable B — score et combos

### 3.1 Barème

- **10 points par pixel peint** (`ScoreRules.PointsPerPixel`).
- Une bombe qui peint 4 pixels rapporte donc 40 points **avant** multiplicateur ;
  une ligne complète peut rapporter plus de 100 points.
- Le score est affiché en haut à droite, **à côté de l'étoile** (bulle dédiée).

### 3.2 Combo

- Deux taps sur des blobs de **même couleur affichée** à moins de **1,5 s**
  d'intervalle enchaînent le combo.
- Multiplicateur = longueur de la chaîne, plafonné à **×4** :
  ×1 → ×2 → ×3 → ×4, puis reste à ×4 tant que la chaîne continue.
- Une couleur différente, un tap refusé ou 1,5 s sans tap remettent la chaîne à ×1.
- Les créatures spéciales combotent entre elles (deux bombes = même gris), ce qui
  récompense une lecture rapide du tapis.
- Popup **« COMBO ×2 ! »** au centre du plateau : apparition rebond (`OutBack`),
  montée douce puis fondu. Couleur : jaune ×2, rose ×3, violet ×4.

### 3.3 Intérêt stratégique

Le joueur arbitre en permanence :

1. **Garder** une bombe pour un carré dense plutôt que la dépenser sur un coin ;
2. **Enchaîner** deux blobs de la même couleur avant 1,5 s pour doubler les points ;
3. **Choisir l'ordre** des taps pour ne pas casser une chaîne à ×4 ;
4. Sur un objectif de score, privilégier les pouvoirs qui peignent beaucoup de pixels.

---

## 4. Livrable C — objectifs de niveau variables

`GoalType` est stocké dans `LevelDefinition` et dans `LevelData` (ScriptableObject).

| Objectif | Condition de victoire | Condition de défaite |
|---|---|---|
| `FillAll` | motif entièrement peint | tapis plein (6ᵉ créature) |
| `ScoreTarget` | score cible atteint avant la limite | temps écoulé, tapis plein, ou motif terminé avant le score |
| `TimeAttack` | motif terminé avant la limite | temps écoulé ou tapis plein |

Valeurs par défaut du cahier des charges : **60 s** pour `ScoreTarget`, **30 s** pour
`TimeAttack`. Le HUD affiche l'objectif et un chrono qui devient rose et pulse sous
5 secondes. Cas limite traité : en `ScoreTarget`, terminer le motif sans atteindre le
score est une **défaite** (plus rien ne peut rapporter de points).

### 4.1 Trois nouveaux niveaux

| Asset | Nom | Objectif | Particularité |
|---|---|---|---|
| `Level_GoalFill.asset` | Le Grand Cœur | FillAll | motif 6×6, 2 couleurs, rythme 1,3 s |
| `Level_GoalScore.asset` | Ruée au Score | ScoreTarget **400 pts / 60 s** | damier 6×6 à 3 couleurs, rythme 0,95 s |
| `Level_GoalTime.asset` | Sprint 30 Secondes | TimeAttack / **30 s** | motif 5×5 de 17 pixels, rythme 0,8 s |

Le menu passe de 3 à **6 niveaux**, affichés en grille de 3 colonnes sur 2 rangées.
Le libellé d'objectif du niveau sélectionné s'affiche sous les cartes.

---

## 5. Livrable D — méta-jeu, économie et skins

### 5.1 Économie

À la fin de chaque partie, le joueur gagne des pièces :

```
pièces = score / 40 + (victoire ? 20 : 3)
```

Le barème est volontairement généreux : même une défaite rapporte 3 pièces, ce qui
entretient l'envie de rejouer. Les pièces sont affichées dans le menu (bourse en haut
à droite) et dans la boutique.

### 5.2 Boutique

Bouton **BOUTIQUE** généré par le code en bas à droite du menu. Il ouvre un panneau
en surimpression (pas de nouvelle scène, donc pas de chargement) :

- bourse du joueur ;
- 4 lignes : Classique (gratuit), **Lunettes 120**, **Chapeau 200**, **Couronne 320** ;
- un skin non possédé affiche son prix et un bouton **ACHETER** (désactivé si le
  solde est insuffisant) ;
- un skin possédé propose **ÉQUIPER** ; le skin porté affiche **ÉQUIPÉ** ;
- achat réussi = achat + équipement immédiat, avec son de validation.

### 5.3 Rendu des skins

Les accessoires sont des sprites générés par `ArtFactory` (lunettes à monture sombre,
chapeau à bandeau rose, couronne dorée). Ils sont superposés au blob dans
`ConveyorView`, sur les **créatures normales uniquement** : les spéciales gardent leur
symbole de pouvoir, qui doit rester lisible.

### 5.4 Sauvegarde

`MetaProgress` (PlayerPrefs, `Assets/Game/Data/MetaProgress.cs`) persiste :

| Clé | Contenu |
|---|---|
| `colorflow.coins` | solde de pièces |
| `colorflow.skin.selected` | skin équipé |
| `colorflow.skin.owned.<id>` | déblocage de chaque skin |

`PlayerPrefs.Save()` est appelé à chaque achat / équipement. Aucun autre état n'est
sauvegardé (pas de progression de niveaux au Sprint 2).

---

## 6. Architecture, qualité et tests

### 6.1 Séparation maintenue

- `Assets/Core/` reste **100 % C# pur** (`noEngineReferences: true`) : nouvelles règles
  `CreatureKind`, `GoalType`, `ScoreRules`, `EconomyRules`, `GoalRules`,
  `PaintAllowance`, extensions de `GridModel`, `PaintResolver`, `CreatureSpawner`,
  `GameController`.
- `Assets/Game/` ne contient que l'Unity : `MetaProgress`, `SkinCatalog`, `ShopPanel`,
  `ArtFactory`, `HudView`, `ConveyorView`, `MainMenuController`, `GameSceneController`.
- Aucun asset externe ajouté : sprites (symboles, accessoires, pièce, blob arc-en-ciel)
  et animations sont calculés à l'exécution.

### 6.2 Nouveaux fichiers notables

| Fichier | Rôle |
|---|---|
| `Core/Model/CreatureKind.cs` | Normal / Bomb / Rainbow / Line |
| `Core/Model/GoalType.cs` | FillAll / ScoreTarget / TimeAttack |
| `Core/Rules/ScoreRules.cs` | 10 pts/pixel, fenêtre 1,5 s, plafond ×4 |
| `Core/Rules/EconomyRules.cs` | conversion score → pièces |
| `Core/Rules/GoalRules.cs` | évaluation et libellés d'objectif |
| `Core/Rules/PaintAllowance.cs` | budget d'équité des effets de zone |
| `Game/Data/MetaProgress.cs` | PlayerPrefs : pièces et skins |
| `Game/Data/SkinCatalog.cs` | catalogue et prix |
| `Game/Presentation/ShopPanel.cs` | boutique en surimpression |
| `Assets/Tests/EditMode/SprintTwoTests.cs` | 20 tests dédiés au Sprint 2 |

### 6.3 Résultats des tests

| Suite | Résultat |
|---|---|
| `dotnet test Tools/CoreTests/CoreTests.csproj` | **86 / 86** |
| Unity Test Runner **EditMode** | **86 / 86** |
| Unity Test Runner **PlayMode** | **3 / 3** |

Couverture spécifique Sprint 2 :

- couleurs affichées des 3 spéciales ;
- tirage à 10 % désactivable, et à 100 % produisant les 3 types sans couleur morte ;
- bombe 3×3 (coins tronqués et centre), respect du budget d'équité ;
- arc-en-ciel peignant une couleur différente de sa couleur métier ;
- ligne peignant une rangée entière ;
- score ×1/×2/×3, plafond ×4, reset sur couleur différente et après 1,5 s ;
- `ScoreTarget` gagné au score cible, perdu au chrono ;
- `TimeAttack` gagné à la complétion, perdu au chrono ;
- valeurs par défaut 60 s / 30 s et libellés d'objectif ;
- conversion économique et reset complet au restart.

Les tests PlayMode vérifient le chargement de la scène Game, le tap (désormais
compatible avec une peinture multiple) et le restart.

---

## 7. Build APK et vérifications

| Élément | Valeur |
|---|---|
| Commande | `powershell -ExecutionPolicy Bypass -File .\build-android.ps1` |
| Sortie | `Builds/Android/colorflow-debug.apk` |
| Taille | **39 427 851 octets (37,60 Mo)** |
| Durée du build | **137 s** (build incrémental ; 1 044 s pour le premier build complet) |
| SHA-256 | `C684FAA63D7C8E8508636DE61EFA0EE3BF5F9F1FABABF42765AFB6C13876854D` |
| Package | `com.digitalholding.colorflow` |
| Version | `versionName=0.2.0`, `versionCode=3` |
| minSdk / targetSdk | 23 / 36 |
| Architecture | ARMv7 + ARM64 (IL2CPP) |
| Signature | keystore debug Unity |
| Log | `Builds/build.log` (marqueur `[ColorFlow][Build] SUCCES`, 0 erreur / 0 avertissement) |

Vérification `aapt dump badging` :

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

L'APK est joint à la release GitHub **« latest »** (`v0.2.0`).

---

## 8. Décisions de game design et équilibrage

1. **La couleur métier des spéciales reste cachée.** Afficher la couleur d'équité
   aurait donné deux informations contradictoires (« bombe rouge » qui peint du bleu).
   Le joueur lit le pouvoir, l'équité garantit l'utilité.
2. **Les spéciales comptent dans le combo via leur couleur affichée.** Deux bombes
   s'enchaînent, une bombe et un blob rouge non : la règle reste « même couleur »,
   mais lisible à l'écran.
3. **Budget d'équité plutôt que suppression d'effet.** Une bombe qui ne peut pas
   peindre 9 cases en peint moins : le pouvoir reste utile, la fairness reste intacte.
4. **Objectif de score perdu si le motif se termine avant le score.** Sans cette règle,
   la grille pleine bloquait la partie en `Playing` pour toujours.
5. **Boutique en surimpression, pas en scène.** Moins de chargements, moins de risques,
   et le portefeuille se met à jour immédiatement.
6. **Prix progressifs (120 / 200 / 320).** Il faut environ 5 à 10 parties pour le
   premier skin, ce qui donne une cible court terme sans rendre la couronne triviale.
7. **Consolation de 3 pièces en défaite.** Évite la sensation de partie « perdue pour
   rien », surtout sur les objectifs chronométrés.
8. **Les accessoires sont masqués sur les spéciales.** Le symbole de pouvoir est
   l'information prioritaire ; superposer une couronne nuirait à la lisibilité.

---

## 9. Limites assumées et suite

### Limites

- **Pas de test sur appareil physique** : l'APK est vérifié statiquement (`aapt`,
  SHA-256) et par les tests Unity, mais le ressenti tactile reste à valider par le lead
  (`TEST_PROTOCOL.md`).
- **Pas de nouvelle musique ni de voix** : seuls les sons procéduraux existants sont
  réutilisés (splat, pop, erreur, victoire).
- **Progression de niveaux non sauvegardée** : seules les pièces et les skins le sont.
- **Pas d'analytics** : l'équilibrage (400 pts en 60 s, prix des skins) est une
  hypothèse de design, à confirmer par des parties réelles.
- **Boutique non localisée** : interface en français uniquement.

### Prochaines étapes proposées (Sprint 3)

1. Tutoriel contextuel du premier pouvoir (bulle explicative au premier blob spécial).
2. Objectifs secondaires (3 étoiles par niveau : score, temps, sans erreur).
3. Skins animés (particules, traînée) et boutique avec aperçu en mouvement.
4. Événements quotidiens et série de combos récompensée.
5. Test device + réglage du rythme de spawn sur les niveaux chronométrés.

---

## Annexes

- Procédure de build : `BUILD.md`
- Protocole de test device : `TEST_PROTOCOL.md`
- Code : `Assets/Core/` (règles pures) et `Assets/Game/` (Unity)
