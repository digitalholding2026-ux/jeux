# TEST_PROTOCOL.md — Validation device de Color Flow (Sprint 2)

Protocole à exécuter par le lead sur un **appareil Android réel**. Ce document est
la seule source de vérité pour clôturer le Sprint 2 : le sprint n'est **pas clos**
tant que les scénarios 1 à 5 et les scénarios S2-1 à S2-5 ne sont pas validés.

- **APK testé** : `Builds/Android/colorflow-debug.apk` (build de développement)
- **Package** : `com.digitalholding.colorflow`
- **Nom affiché** : Color Flow
- **Matériel conseillé** : écran 20:9 (ex. Redmi 10, Galaxy A14), Android 8+
  (minSdk 23 = Android 6.0)
- **Durée estimée** : 25–35 minutes

---

## 0. Préparation

1. Sauvegarder la version exacte de l'APK testé (date + taille + empreinte) :

   ```bash
   sha256sum Builds/Android/colorflow-debug.apk
   ```

2. Sur le téléphone : **Paramètres → À propos → Numéro de build** (appuyer 7 fois)
   pour activer les **Options développeur**.
3. Activer **Débogage USB** (nécessaire pour `adb` et `logcat`).
4. Relever la version Android : **Paramètres → À propos → Version d'Android**.
5. Noter la mémoire de l'app au repos (voir scénario 4).

---

## 1. Installation

### Méthode A — ADB (recommandée)

```bash
adb devices
# doit afficher l'appareil en "device" (pas "unauthorized")

adb install -r Builds/Android/colorflow-debug.apk
```

- `-r` réinstalle en conservant les données. En cas de conflit :
  `adb uninstall com.digitalholding.colorflow` puis réinstaller.
- Si `adb` n'est pas installé : `sudo apt install android-tools-adb` (Linux) ou
  télécharger *platform-tools* (Windows/macOS).

### Méthode B — transfert manuel

1. Copier `colorflow-debug.apk` sur le téléphone (USB, cloud, e-mail).
2. Ouvrir le fichier depuis **Fichiers**.
3. Autoriser **Installer des applications inconnues** pour l'application source
   (Fichiers/Chrome), puis **Installer**.
4. Si Play Protect bloque : **Installer quand même**.

### Vérification obligatoire après installation

```bash
adb shell pm list packages | grep com.digitalholding.colorflow
adb shell dumpsys package com.digitalholding.colorflow | grep -E "versionName|versionCode"
```

- [ ] Le package `com.digitalholding.colorflow` est présent.
- [ ] Le nom affiché dans le lanceur est **Color Flow**.
- [ ] L'icône se lance sans crash immédiat.

---

## 2. Scénario 1 — Victoire (niveau 1, croix)

**But** : vérifier la boucle complète spawn → tap → peinture → victoire.

1. Lancer **Color Flow** → **Jouer** (niveau 1 « La Croix »).
2. Taper les créatures pour peindre toute la croix (33 pixels).
3. Chronométrer du lancement à l'écran de victoire.

**Attendu**

- Chaque créature tapée peint un pixel de sa couleur.
- Aucune créature ne reste bloquée sur le tapis sans pixel disponible.
- Écran de **victoire** clair à la fin.
- Bouton **Rejouer** fonctionnel (relance une grille vide).
- **Zéro crash**, aucune saccade visible.

| Mesure | Valeur observée |
|---|---|
| Durée de la partie | |
| Débordement survenu ? | oui / non |
| Bouton Rejouer OK ? | oui / non |
| Statut | OK / KO |

---

## 3. Scénario 2 — Défaite (niveau 2, cœur)

**But** : vérifier que le débordement du tapis (6ᵉ créature) est compréhensible.

1. Retour au menu → niveau 2 « Le Cœur ».
2. **Ne rien taper.** Laisser le tapis se remplir (capacité 5).
3. Attendre la 6ᵉ créature (≈ 6 × 1,4 s ≈ 9 s).

**Attendu**

- Le tapis se remplit visiblement (5 créatures), puis la 6ᵉ déclenche la **défaite**.
- Message/écran de défaite **explicite** (on comprend que le tapis a débordé).
- Bouton **Rejouer** fonctionnel.
- Aucun crash.

| Mesure | Valeur observée |
|---|---|
| Temps avant défaite | |
| Débordement visuellement compréhensible ? | oui / non |
| Rejouer OK ? | oui / non |
| Statut | OK / KO |

---

## 4. Scénario 3 — Équité (niveau 3, damier)

**But** : valider la règle `weight(c) = max(0, remaining(c) − onBelt(c))` en jeu réel.

1. Niveau 3 « Le Damier » (2 couleurs, 32 pixels).
2. Jouer **normalement** (taper ce qui est disponible, sans chercher à optimiser).
3. Observer attentivement le tapis pendant toute la partie.

**Attendu**

- **Aucune créature morte** : jamais une créature dont la couleur n'a plus de pixel
  à peindre.
- Aucun **blocage** : si le tapis semble bloqué, taper une créature débloque le spawn.
- Aucun **tap refusé** (pas de feedback « impossible »).
- Partie menée jusqu'à la victoire.

> Rappel : quand la somme des poids est nulle, le spawn s'arrête volontairement.
> C'est normal : le joueur doit taper pour libérer du poids. Ce n'est **pas** un bug.

| Mesure | Valeur observée |
|---|---|
| Créature morte observée ? | oui / non |
| Blocage observé ? | oui / non |
| Tap refusé observé ? | oui / non |
| Victoire atteinte ? | oui / non |
| Statut | OK / KO |

---

## 5. Scénario 4 — Robustesse (10 parties enchaînées)

**But** : détecter crash, fuite mémoire visible, ralentissement.

1. Avant la première partie : **Paramètres → Applications → Color Flow → Mémoire**
   (ou `adb shell dumpsys meminfo com.digitalholding.colorflow`), noter la valeur.
2. Enchaîner **10 parties** en mélangeant victoires (niveaux 1 et 3) et défaites
   (niveau 2, sans taper).
3. Après la 10ᵉ partie : relever à nouveau la mémoire.
4. Vérifier : pas de crash, pas de ralentissement progressif, pas d'écran noir,
   bouton Rejouer toujours réactif.

```bash
adb shell dumpsys meminfo com.digitalholding.colorflow | grep -E "TOTAL|Java Heap|Native Heap"
```

**Attendu**

- 10 parties sans crash ni ANR.
- Mémoire stable (variation raisonnable ; noter les valeurs, pas de seuil inventé).
- Aucune dégradation visible des animations/du tap.

| Mesure | Début | Fin |
|---|---|---|
| Mémoire totale (Mo) | | |
| Java Heap (Mo) | | |
| Native Heap (Mo) | | |

| Constat | Valeur |
|---|---|
| Crash / ANR | oui / non |
| Ralentissement visible | oui / non |
| Statut | OK / KO |

---

## 6. Scénario 5 — Ratios d'écran (si appareil disponible)

**But** : vérifier l'adaptation UI hors 20:9.

1. Tester sur un 16:9 ou une tablette (ou changer la résolution via les options
   développeur si disponible).
2. Vérifier : grille centrée, tapis entièrement visible, HUD non rogné, boutons
   atteignables, pas de débordement.

| Constat | Valeur |
|---|---|
| Ratio testé | |
| Grille centrée | oui / non |
| Tapis visible en entier | oui / non |
| HUD rogné | oui / non |
| Statut | OK / KO / non testé |

---

## 7. Tableau récapitulatif des résultats

| Scénario | Attendu | Observé | Statut | Capture / log |
|---|---|---|---|---|
| 1 — Victoire croix | Victoire + Rejouer + 0 crash | | OK / KO | |
| 2 — Défaite cœur | Défaite claire à la 6ᵉ créature | | OK / KO | |
| 3 — Équité damier | 0 créature morte, 0 blocage | | OK / KO | |
| 4 — 10 parties | 0 crash, mémoire stable | | OK / KO | |
| 5 — Ratios | UI non rognée | | OK / KO / N/T | |

---

## 8. Template de rapport de bug (à remplir par le lead)

```markdown
### BUG-<numéro> — <titre court>

- **Scénario** : 1 / 2 / 3 / 4 / 5
- **Appareil** : <marque + modèle>, <résolution>
- **Version Android** : <ex. 13>
- **Version APK** : colorflow-debug.apk, <taille>, <sha256 court>
- **Fréquence** : systématique / 1 fois sur N / une seule fois

**Étapes**
1. ...
2. ...
3. ...

**Attendu**
...

**Observé**
...

**Preuves**
- Capture d'écran : <fichier>
- Logcat : `adb logcat -d > logcat.txt` (joindre le fichier)
```

### Récupérer les logs

```bash
adb logcat -c
# reproduire le bug
adb logcat -d > logcat.txt
```

Chercher en priorité : `FATAL EXCEPTION`, `ANR in com.digitalholding.colorflow`,
`Unity`, `Exception`.

---

## 9. Critères de clôture Sprint 2

Tous obligatoires :

- [ ] `dotnet test Tools/CoreTests/CoreTests.csproj` → **100 % de réussite** (86/86).
- [ ] APK produit (`Builds/Android/colorflow-debug.apk`) + taille + hash notés.
- [ ] Scénarios **1, 2, 3** et **S2-1, S2-2, S2-3, S2-4** : statut **OK**.
- [ ] Scénario **4** et **S2-5** : aucun crash, aucune fuite mémoire visible.
- [ ] Scénario **5** : exécuté ou explicitement marqué « non testé » avec raison.
- [ ] Aucun bug bloquant ouvert ; sinon hotfix borné avant clôture.

**Procédure en cas de KO** : joindre le rapport de bug ci-dessus. L'agent exécute
un hotfix limité aux bugs constatés, reconstruit l'APK, met à jour ce protocole si
nécessaire, puis le lead re-teste.

---

## 10. Scénarios Sprint 2 (à valider en plus des précédents)

> Ces scénarios couvrent les ajouts du Sprint 2 : blobs spéciaux, score/combos,
> objectifs chronométrés et méta-jeu. Ils ne remplacent pas les scénarios 1 à 5.

### S2-1 — Blobs spéciaux et équité

1. Lancer une partie sur `Level_GoalFill` (« Le Grand Cœur ») et jouer ~2 minutes.
2. Observer l'apparition des trois symboles : bombe (pastille grise), étoile
   (arc-en-ciel), barres (ligne bleue).
3. Taper chaque type de spéciale au moins une fois, en notant le nombre de pixels
   peints.

**Attendu**

- [ ] La bombe peint jusqu'à 9 pixels autour du prochain pixel de sa couleur.
- [ ] L'arc-en-ciel peint un pixel, même d'une autre couleur que le blob.
- [ ] La ligne peint une rangée ou une colonne entière.
- [ ] Aucune créature déjà posée ne devient inutilisable après un pouvoir.
- [ ] Aucun crash ni pic de saccade sur les effets.

### S2-2 — Score et combos

1. Sur `Level_GoalScore` (« Ruée au Score »), enchaîner deux blobs de même couleur
   en moins de 1,5 s.
2. Recommencer pour atteindre ×3 puis ×4, puis laisser passer 2 s.
3. Vérifier le score affiché à côté de l'étoile.

**Attendu**

- [ ] Le popup « COMBO ×2 ! » (puis ×3, ×4) apparaît avec un rebond.
- [ ] Le multiplicateur ne dépasse jamais ×4.
- [ ] Après 1,5 s sans tap ou un changement de couleur, le combo repart à ×1.
- [ ] Le score augmente de 10 points par pixel × multiplicateur.

### S2-3 — Objectifs et écran de fin

1. `Level_GoalScore` : atteindre 400 points avant 60 s.
2. Relancer et laisser le chrono expirer.
3. `Level_GoalTime` : remplir les 17 pixels avant 30 s, puis relancer sans jouer.

**Attendu**

- [ ] Chrono visible et lisible ; il devient rose et pulse sous 5 s.
- [ ] Victoire affiche « Objectif atteint : 400 pts » (ou le temps réalisé).
- [ ] Défaite affiche la raison exacte (« temps écoulé » ou « x / 400 pts »).
- [ ] Les boutons REJOUER et MENU fonctionnent.

### S2-4 — Boutique, pièces et sauvegarde

1. Relever la bourse dans le menu, jouer une partie, revenir au menu.
2. Ouvrir **BOUTIQUE**, acheter un skin abordable, l'équiper.
3. Tuer l'application (paramètres Android → Forcer l'arrêt), la relancer.

**Attendu**

- [ ] La bourse augmente après la partie (score / 40 + prime).
- [ ] Le bouton ACHETER est désactivé si le solde est insuffisant.
- [ ] L'achat débite le solde et équipe immédiatement le skin.
- [ ] L'accessoire est visible sur les blobs normaux, pas sur les spéciales.
- [ ] Après redémarrage : pièces, skins possédés et skin équipé conservés.

### S2-5 — Robustesse Sprint 2

1. Enchaîner 10 parties mélangeant les 6 niveaux et les 3 objectifs.
2. Acheter/équiper au moins deux skins entre les parties.
3. Relever la mémoire avant/après (même méthode que le scénario 4).

**Attendu**

- [ ] 0 crash, 0 ANR, aucune fuite mémoire visible.
- [ ] Aucun niveau ne reste bloqué en `Playing`.
- [ ] Les objectifs chronométrés se terminent toujours (victoire ou défaite).

| Scénario | Attendu | Observé | Statut |
|---|---|---|---|
| S2-1 Spéciales | 3 pouvoirs, équité intacte | | OK / KO |
| S2-2 Combos | ×2→×4, reset après 1,5 s | | OK / KO |
| S2-3 Objectifs | victoire/défaite explicites | | OK / KO |
| S2-4 Boutique | achat + persistance | | OK / KO |
| S2-5 Robustesse | 0 crash, mémoire stable | | OK / KO |
