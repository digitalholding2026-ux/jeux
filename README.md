# 🍓 Fruits Crush

Jeu mobile style Candy Crush avec des fruits — 50 niveaux, design pastel soft.

- **`index.html`** : le jeu complet (jouable dans un navigateur, PC + mobile)
- **`android/`** : projet Android natif (WebView) — compile `FruitsCrush.apk`
- **`FruitsCrush.apk`** : APK debug prête à installer (Android 7.0+)

## Compiler l'APK

```bat
cd android
gradle assembleDebug
:: APK -> app\build\outputs\apk\debug\app-debug.apk
```

Prérequis : JDK 17 + Android SDK (platform 34, build-tools 34.0.0).
