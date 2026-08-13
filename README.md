# KeepScreen

Petit utilitaire de zone de notification, dans l'esprit de **DeskPin**, mais dont
le but principal est de **garder une fenêtre visible et à la même place quand on
change de bureau virtuel** (`Ctrl + Win + ←/→`).

Il s'appuie sur le même mécanisme que le clic droit *« Afficher cette fenêtre sur
tous les bureaux »* de la vue des tâches, exposé par les interfaces COM du shell
Windows (`IApplicationViewCollection` / `IVirtualDesktopPinnedApps`).

## Utilisation : tout passe par l'icône

Aucun raccourci clavier global n'est enregistré, donc aucun risque de conflit
avec une autre application. **Un clic (gauche ou droit) sur l'icône ouvre le
menu** :

| Entrée | Effet |
|---|---|
| *Fenêtre : …* | Rappelle la fenêtre visée (dernière fenêtre active) |
| **Garder sur tous les bureaux** | Épingle / désépingle cette fenêtre — cochée quand elle l'est |
| **Toujours au premier plan** | Bascule le `TOPMOST` (comportement DeskPin classique) |
| **Fenêtres épinglées (n)** | Liste ; cliquer sur une entrée la désépingle |
| **Tout désépingler** | Remet tout à plat |
| **Démarrer avec Windows** | Entrée dans `HKCU\...\CurrentVersion\Run` |
| **Tout désépingler en quittant** | Activé par défaut, évite de laisser des fenêtres collées |
| **Quitter** | |

Comme cliquer sur l'icône donne le focus à la barre des tâches, KeepScreen
mémorise en permanence la dernière fenêtre réellement active (barre des tâches,
bureau, menu Démarrer et vue des tâches sont ignorés). C'est cette fenêtre-là,
dont le titre est affiché en tête du menu, qui est visée.

L'icône passe du gris à l'orange dès qu'au moins une fenêtre est épinglée.

## Compilation

Aucun SDK à installer : le compilateur C# du .NET Framework 4 déjà présent dans
Windows suffit.

```bat
build.bat
```

Produit `KeepScreen.exe` à côté des sources.

## Marche à suivre

1. Lancer `KeepScreen.exe` (il n'affiche pas de fenêtre, seulement l'icône).
2. Cliquer sur la fenêtre à conserver pour l'activer.
3. Cliquer sur l'icône → *Garder sur tous les bureaux*.
4. Changer de bureau avec `Ctrl + Win + ←/→` : la fenêtre reste affichée, au même
   endroit.

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
