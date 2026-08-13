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
| **Maintenir à l'écran (tous les bureaux)…** | Passe en désignation, puis la fenêtre cliquée suit tous les bureaux |
| **Premier plan (toujours visible)…** | Passe en désignation, puis la fenêtre cliquée passe en `TOPMOST` |
| **Fenêtres verrouillées (n)** | Liste avec l'état de chacune ; cliquer sur une entrée la libère |
| **Tout libérer** | Remet toutes les fenêtres à leur état normal |
| **Démarrer avec Windows** | Entrée dans `HKCU\...\CurrentVersion\Run` |
| **Tout libérer en quittant** | Activé par défaut, évite de laisser des fenêtres bloquées |
| **Quitter** | |

### Le mode désignation

Les deux premières entrées fonctionnent comme DeskPin : après le clic, **le
curseur devient une épingle** et le clic suivant choisit la fenêtre à verrouiller.
Ce clic est consommé par KeepScreen, il n'actionne donc pas ce qui se trouve sous
le pointeur.

- **Échap**, un **clic droit**, ou un clic sur l'icône : annulent la désignation.
- Désigner une fenêtre déjà verrouillée la libère (l'action est une bascule).
- L'infobulle de l'icône indique en permanence l'état en cours.

L'icône passe du gris à l'orange pendant la désignation et tant qu'au moins une
fenêtre est verrouillée.

## Compilation

Aucun SDK à installer : le compilateur C# du .NET Framework 4 déjà présent dans
Windows suffit.

```bat
build.bat
```

Produit `KeepScreen.exe` à côté des sources.

## Marche à suivre

1. Lancer `KeepScreen.exe` (il n'affiche pas de fenêtre, seulement l'icône).
2. Cliquer sur l'icône → *Maintenir à l'écran (tous les bureaux)…*
3. Le curseur devient une épingle : cliquer sur la fenêtre à conserver.
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
