# KeepScreen

Petit utilitaire de zone de notification, dans l'esprit de **DeskPin**, mais dont
le but principal est de **garder une fenêtre visible et à la même place quand on
change de bureau virtuel** (`Ctrl + Win + ←/→`).

Il s'appuie sur le même mécanisme que le clic droit *« Afficher cette fenêtre sur
tous les bureaux »* de la vue des tâches, exposé par les interfaces COM du shell
Windows (`IApplicationViewCollection` / `IVirtualDesktopPinnedApps`).

## Raccourcis

| Raccourci | Effet |
|---|---|
| `Ctrl + Alt + K` | Épingle / désépingle la fenêtre active sur **tous les bureaux** |
| `Ctrl + Alt + T` | Bascule « toujours au premier plan » (comportement DeskPin classique) |
| `Ctrl + Alt + U` | Désépingle toutes les fenêtres |
| Clic gauche sur l'icône | Équivaut à `Ctrl + Alt + K` |
| Clic droit sur l'icône | Menu : liste des fenêtres épinglées, démarrage automatique, quitter |

L'icône de la zone de notification passe du gris à l'orange dès qu'au moins une
fenêtre est épinglée.

## Compilation

Aucun SDK à installer : le compilateur C# du .NET Framework 4 déjà présent dans
Windows suffit.

```bat
build.bat
```

Produit `KeepScreen.exe` à côté des sources.

## Utilisation

1. Lancer `KeepScreen.exe` (il n'affiche pas de fenêtre, seulement l'icône).
2. Cliquer sur la fenêtre à conserver, puis `Ctrl + Alt + K`.
3. Changer de bureau avec `Ctrl + Win + ←/→` : la fenêtre reste affichée, au même
   endroit.

Menu de l'icône : *Démarrer avec Windows* ajoute/retire une entrée dans
`HKCU\...\CurrentVersion\Run`. *Tout désépingler en quittant* (activé par défaut)
évite de laisser des fenêtres épinglées après la fermeture de l'application.

## Limites connues

- Une fenêtre appartenant à un processus **élevé** (lancé en administrateur) ne
  peut être épinglée que si KeepScreen est lui aussi lancé en administrateur.
- Les interfaces COM utilisées ne sont pas documentées par Microsoft et leurs
  identifiants changent selon les versions de Windows. Les GUID retenus ici sont
  ceux de **Windows 10 1803 → 22H2** ; vérifié sur la build **19045**. Sur
  Windows 11, `IVirtualDesktopPinnedApps` a un autre IID et
  [VirtualDesktop.cs](VirtualDesktop.cs) devra être ajusté.
- L'épinglage porte sur la fenêtre, pas sur l'application : rouvrir la fenêtre
  après l'avoir fermée demande de la ré-épingler.
