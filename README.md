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
curseur prend la forme du logo** de l'application, signalant qu'une fenêtre est
attendue ; le clic suivant la choisit. Ce clic est consommé par KeepScreen, il
n'actionne donc pas ce qui se trouve sous le pointeur.

- **Échap** ou un **clic droit** annulent la désignation ; cliquer le bureau ou la
  barre des tâches y renonce également, sans message.
- Désigner une fenêtre déjà verrouillée la libère (l'action est une bascule).
- L'infobulle de l'icône indique en permanence l'état en cours.

La désignation repose sur un **calque transparent couvrant tous les moniteurs**,
et non sur `SetCapture`. La capture souris ne redirige en effet les messages que
si un bouton est maintenu enfoncé ou si le pointeur survole la fenêtre capturante
— c'est ce qui fait fonctionner l'outil de recherche de Spy++, qu'on utilise en
*glissant*. Sans bouton pressé, chaque fenêtre survolée continuait d'imposer son
propre curseur et le logo n'apparaissait jamais. Avec le calque, le pointeur est
en permanence au-dessus de notre fenêtre : elle impose son curseur et reçoit le
clic.

### La marque sur la barre de titre

Une fenêtre verrouillée reçoit **le logo sur sa barre de titre**, à gauche des
boutons système. **Cliquer dessus libère la fenêtre** et retire la marque.

La marque suit sa fenêtre : déplacement, redimensionnement, réduction (elle
s'efface), et elle se place juste devant elle dans l'ordre de profondeur — une
autre fenêtre qui recouvre la cible recouvre donc aussi la marque. Une fenêtre
maintenue sur tous les bureaux emmène sa marque avec elle d'un bureau à l'autre.

Techniquement c'est une fenêtre à transparence par pixel (`WS_EX_LAYERED` +
`UpdateLayeredWindow`), ce qui préserve l'anticrénelage du logo sur n'importe quel
fond ; elle n'accepte jamais le focus, cliquer dessus ne désactive donc pas la
fenêtre visée, et les zones transparentes laissent passer le clic vers la barre de
titre en dessous.

Les fenêtres à cadre personnalisé (navigateurs, applications UWP…) ne publient pas
toujours la géométrie de leur barre de titre ; la marque se place alors dans le
coin supérieur droit du cadre visible.

**Pour déplacer la marque**, un seul réglage dans [WindowMarker.cs](WindowMarker.cs) :
`BUTTON_GAP` (l'écart avec le premier bouton système, 4 px). Plus il est petit,
plus la marque va à droite ; en dessous de zéro elle empiète sur le bouton
Réduire.

La largeur du bloc des boutons ne peut pas être lue directement : la métrique
système `SM_CXSIZE` vaut 36 px là où Windows 10 dessine des boutons de 46 px
(mesuré au pixel : glyphes centrés tous les 46 px). Elle suit en revanche
correctement la mise à l'échelle de l'affichage, d'où le rapport 46/36 appliqué
dans le code.

## Compilation

Aucun SDK à installer : le compilateur C# du .NET Framework 4 déjà présent dans
Windows suffit.

```bat
build.bat
```

Produit `KeepScreen.exe` à côté des sources. Le logo est **embarqué dans
l'exécutable** : `KeepScreen.exe` fonctionne seul, sans le dossier `assets`.

## Le logo

| Fichier | Rôle |
|---|---|
| `assets/icon.svg`, `assets/icon.png` | Sources fournies ; le PNG (512×512, transparent) sert de logo à l'exécution |
| `assets/app.ico` | **Généré** par `tools/MakeIcon.cs` — icône du fichier et de la fenêtre |

`icon.ico` ne contenait qu'une seule image 256×256 : Windows aurait dû la réduire
lui-même pour la zone de notification (16×16) et la barre de titre, avec un rendu
flou. `tools/MakeIcon.cs` pré-calcule donc les neuf tailles utiles (16 → 256) à
partir du PNG, avec un rééchantillonnage de qualité.

**Si vous changez de logo**, remplacez `assets/icon.png` puis régénérez :

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:MakeIcon.exe /r:System.Drawing.dll tools\MakeIcon.cs
MakeIcon.exe
build.bat
```

## Marche à suivre

1. Lancer `KeepScreen.exe` (il n'affiche pas de fenêtre, seulement l'icône).
2. Cliquer sur l'icône → *Maintenir à l'écran (tous les bureaux)…*
3. Le curseur devient le logo : cliquer sur la fenêtre à conserver. Elle reçoit le
   logo sur sa barre de titre.
4. Changer de bureau avec `Ctrl + Win + ←/→` : la fenêtre reste affichée, au même
   endroit.
5. Pour la libérer : cliquer le logo sur sa barre de titre.

## Le veto des applications sur le premier plan

Certaines applications **refusent** qu'on modifie leur ordre de profondeur : elles
interceptent `WM_WINDOWPOSCHANGING` et neutralisent le changement au passage.
`SetWindowPos` renvoie alors un **succès sans avoir rien fait** — c'est le cas de
VLC pendant la lecture d'une vidéo (constaté : drapeau toujours absent, y compris
une seconde après l'appel).

D'où deux précautions dans le code :

- l'indicateur `SWP_NOSENDCHANGING` supprime cette notification, ce qui prive
  l'application de son droit de veto ;
- l'état est **relu après coup** au lieu de faire confiance au code de retour, de
  sorte qu'un échec réel soit signalé plutôt que passé sous silence.

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
