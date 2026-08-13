<div align="center">

![Bannière Freeze Ray](./banner.png)

**Figez une fenêtre : gardez-la visible au même endroit sur tous les bureaux virtuels, et au-dessus de tout le reste.**

[English](../README.md) · **Français** · [Deutsch](./README.de.md) · [Español](./README.es.md) · [Italiano](./README.it.md) · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [Русский](./README.ru.md) · [中文](./README.zh.md)

<p align="center">
  <a href="https://github.com/Game-K-Hack/freeze-ray/releases/latest"><img src="https://img.shields.io/github/v/release/Game-K-Hack/freeze-ray?label=T%C3%A9l%C3%A9charger&style=for-the-badge&logo=windows" alt="Télécharger"></a>
</p>

</div>

## À quoi ça sert

Windows permet de changer de bureau virtuel avec `Ctrl + Win + ←/→`, mais tout ce
que vous regardiez disparaît avec le bureau que vous quittez. Freeze Ray fixe une
fenêtre pour qu'elle **reste visible, exactement au même endroit, sur tous les
bureaux**.

C'est un utilitaire de zone de notification dans l'esprit de **DeskPin**, avec
deux actions indépendantes :

- **Maintenir à l'écran** — la fenêtre vous suit sur tous les bureaux virtuels.
- **Premier plan** — la fenêtre reste au-dessus des autres, le comportement
  classique de DeskPin.

Les deux peuvent s'appliquer à la même fenêtre.

## Prérequis

- **Windows 10** (compilé et vérifié sur la build 19045, 22H2).
- **.NET Framework 4** — déjà fourni avec Windows, rien à installer.

> Sous Windows 11, les interfaces non documentées du shell utilisées pour les
> bureaux virtuels ont d'autres identifiants. Voir
> [Limites connues](#limites-connues).

## Pour démarrer

1. Téléchargez `Freeze Ray.exe` depuis la
   [dernière version](https://github.com/Game-K-Hack/freeze-ray/releases/latest),
   ou [compilez-le vous-même](#compiler-depuis-les-sources).
2. Lancez-le. Aucune fenêtre ne s'ouvre : seule une icône apparaît dans la zone
   de notification.
3. Cliquez sur l'icône → **Maintenir à l'écran (tous les bureaux)…**
4. Le curseur prend la forme du logo : cliquez la fenêtre à conserver. Elle reçoit
   un petit logo sur sa barre de titre.
5. Changez de bureau avec `Ctrl + Win + ←/→` : la fenêtre est toujours là.
6. Pour la libérer, cliquez le logo sur sa barre de titre.

L'exécutable est autonome : ni installateur, ni dossier `assets` nécessaires.

## Utilisation

**Aucun raccourci clavier global n'est enregistré**, donc aucun risque de conflit
avec une autre application. Tout passe par l'icône, et **un clic — gauche ou
droit — ouvre le menu** :

| Entrée | Effet |
|---|---|
| **Maintenir à l'écran (tous les bureaux)…** | Passe en désignation ; la fenêtre cliquée suit ensuite tous les bureaux |
| **Premier plan (toujours visible)…** | Passe en désignation ; la fenêtre cliquée passe en `TOPMOST` |
| **Fenêtres verrouillées (n)** | Les liste avec leur état ; cliquer sur l'une d'elles la libère |
| **Tout libérer** | Remet toutes les fenêtres à leur état normal |
| **Paramètres…** | Ouvre la fenêtre de réglages |
| **Quitter** | |

### Le mode désignation

Après un clic sur l'une des deux premières entrées, **le curseur devient le logo**
de l'application, signalant qu'une fenêtre est attendue ; le clic suivant la
choisit. Ce clic est consommé par Freeze Ray, il n'actionne donc pas ce qui se
trouve sous le pointeur.

- **Échap** ou un **clic droit** annulent. Cliquer le bureau ou la barre des
  tâches y renonce également, sans message.
- Désigner une fenêtre déjà verrouillée la libère : l'action est une bascule.
- L'infobulle de l'icône indique en permanence l'état en cours.

### La marque sur la barre de titre

Une fenêtre verrouillée reçoit **le logo sur sa barre de titre**, juste à gauche
des boutons système. **Cliquer dessus libère la fenêtre** et retire la marque.

La marque suit sa fenêtre lors d'un déplacement ou d'un redimensionnement,
s'efface quand la fenêtre est réduite, et se place juste devant elle dans l'ordre
de profondeur — une autre fenêtre qui recouvre la cible recouvre donc aussi la
marque. Une fenêtre maintenue sur tous les bureaux emmène sa marque d'un bureau à
l'autre.

Les fenêtres à cadre personnalisé (navigateurs, applications UWP…) ne publient pas
toujours la géométrie de leur barre de titre ; la marque se place alors dans le
coin supérieur droit du cadre visible.

## Paramètres

Accessibles par **Paramètres…** dans le menu. La fenêtre affiche le logo, le nom
et le **numéro de version**, puis :

| Réglage | Détail |
|---|---|
| **Démarrer avec Windows** | Écrit dans `HKCU\...\CurrentVersion\Run`. Le registre reste la seule source de vérité : la case relit l'état réel et se réaligne si l'écriture échoue |
| **Tout libérer en quittant** | Évite de laisser des fenêtres bloquées |
| **Afficher les notifications** | Ne masque que les bulles d'information — **les erreurs restent toujours signalées**, car les taire ferait passer une action en échec pour une action sans effet |
| **Langue** | Appliquée immédiatement, menu, infobulle et notifications compris |
| **Dépôt GitHub** | Source utilisée pour la recherche de mise à jour, au format `proprietaire/depot` |

Les réglages vivent dans `%APPDATA%\Freeze Ray\settings.ini`, un simple fichier
`clé=valeur` qui se lit et se corrige à la main. Au premier lancement, la langue
suit celle de Windows (anglais si le système n'est pas en français).

Les textes vivent dans [Strings.cs](../Strings.cs), une table par langue plutôt
que des fichiers de ressources : le projet reste compilable avec le compilateur
fourni par Windows. Ajouter une langue revient à ajouter une table et une entrée
dans la liste déroulante.

### Mises à jour

**Rechercher les mises à jour** interroge l'API publique des versions de GitHub
pour le dépôt configuré, compare les numéros et propose d'ouvrir la page de
téléchargement.

**L'application ne se met volontairement pas à jour toute seule.** Remplacer un
exécutable en cours d'exécution demande un programme relais, et le faire sans
signature ni vérification d'intégrité serait un vecteur d'attaque — le gain ne
vaut pas ce risque pour un utilitaire de cette taille.

## Compiler depuis les sources

Aucun SDK à installer : le compilateur C# du .NET Framework 4, déjà présent dans
Windows, suffit.

```bat
build.bat
```

Cela produit `Freeze Ray.exe` à côté des sources. Le logo est **embarqué dans
l'exécutable**, qui fonctionne donc seul.

## Remplacer le logo

| Fichier | Rôle |
|---|---|
| `assets/icon.png` | Logo source (512×512, transparent) — icône de la zone de notification, curseur de désignation et marque |
| `assets/app.ico` | **Généré** par `tools/MakeIcon.cs` — icône du fichier et de la fenêtre |
| `assets/Freeze Ray.png` | Illustration réservée à l'en-tête des paramètres |

`icon.ico` ne contenait à l'origine qu'une seule image 256×256, que Windows aurait
dû réduire lui-même pour la zone de notification (16×16) et la barre de titre, avec
un rendu flou. `tools/MakeIcon.cs` pré-calcule donc les neuf tailles utiles
(16 → 256) à partir du PNG, avec un rééchantillonnage de qualité.

Pour changer de logo, remplacez `assets/icon.png` puis régénérez :

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:MakeIcon.exe /r:System.Drawing.dll tools\MakeIcon.cs
MakeIcon.exe
build.bat
```

## Comment ça marche

### Les bureaux virtuels

Maintenir une fenêtre sur tous les bureaux emprunte le mécanisme même du menu
contextuel *« Afficher cette fenêtre sur tous les bureaux »* de la vue des tâches,
exposé par les interfaces COM non documentées du shell
`IApplicationViewCollection` et `IVirtualDesktopPinnedApps` — voir
[VirtualDesktop.cs](../VirtualDesktop.cs).

### Désigner par calque, pas par capture souris

La désignation repose sur un **calque transparent couvrant tous les moniteurs**,
et non sur `SetCapture`. La capture souris ne redirige les messages que si un
bouton est maintenu enfoncé ou si le pointeur survole la fenêtre capturante —
c'est ce qui fait qu'on utilise l'outil de recherche de Spy++ en *glissant*. Sans
bouton pressé, chaque fenêtre survolée continuait d'imposer son propre curseur et
le logo n'apparaissait jamais. Avec le calque, le pointeur est en permanence
au-dessus de notre fenêtre : elle impose son curseur et reçoit le clic. Voir
[WindowPicker.cs](../WindowPicker.cs).

### La marque

La marque est une fenêtre à transparence par pixel (`WS_EX_LAYERED` +
`UpdateLayeredWindow`), ce qui préserve l'anticrénelage du logo sur n'importe quel
fond. Elle n'accepte jamais le focus, cliquer dessus ne désactive donc pas la
fenêtre visée, et ses zones transparentes laissent passer le clic vers la barre de
titre en dessous.

**Pour déplacer la marque**, un seul réglage dans
[WindowMarker.cs](../WindowMarker.cs) : `BUTTON_GAP`, l'écart avec le premier
bouton système (4 px). Plus il est petit, plus la marque va à droite ; en dessous
de zéro, elle empiète sur le bouton Réduire.

La largeur du bloc des boutons ne peut pas être lue directement : la métrique
système `SM_CXSIZE` annonce 36 px là où Windows 10 dessine des boutons de 46 px
(mesuré au pixel : glyphes centrés tous les 46 px). Elle suit en revanche
correctement la mise à l'échelle de l'affichage, d'où le rapport 46/36 appliqué
dans le code.

### Le veto des applications sur le premier plan

Certaines applications **refusent** qu'on modifie leur ordre de profondeur : elles
interceptent `WM_WINDOWPOSCHANGING` et neutralisent le changement au passage.
`SetWindowPos` renvoie alors un **succès sans avoir rien fait** — VLC se comporte
ainsi pendant la lecture d'une vidéo (mesuré : le drapeau était toujours absent
une seconde entière après l'appel).

D'où deux précautions dans le code :

- l'indicateur `SWP_NOSENDCHANGING` supprime cette notification et prive
  l'application de son droit de veto ;
- l'état est **relu après coup** au lieu de faire confiance au code de retour, de
  sorte qu'un échec réel soit signalé plutôt que passé sous silence.

### Les notifications

Les bulles d'information affichent **le logo de l'application** au lieu du « i »
bleu du système. WinForms ne sait pas le faire : `NotifyIcon.ShowBalloonTip`
n'accepte que les icônes système et rejette toute valeur hors de son énumération.
On s'adresse donc directement au shell (`Shell_NotifyIcon` avec `NIIF_USER`), en
réutilisant l'identification interne de l'entrée créée par WinForms — voir
[Notifications.cs](../Notifications.cs). Si ce détail interne venait à changer, le
code retombe sur la bulle standard.

L'en-tête de la notification affiche `Freeze Ray.exe` : Windows y met le nom du
fichier exécutable. Déclarer un `AppUserModelID` n'y change rien (vérifié) ; seule
l'installation d'un raccourci dans le menu Démarrer permettrait un nom sans
extension.

## Limites connues

- Une fenêtre appartenant à un processus **élevé** ne peut être modifiée que si
  Freeze Ray est lui aussi lancé en administrateur.
- Les interfaces COM utilisées pour les bureaux virtuels ne sont pas documentées
  et leurs identifiants changent selon les versions de Windows. Les GUID retenus
  ici sont ceux de **Windows 10 1803 → 22H2**, vérifiés sur la build **19045**.
  Sous Windows 11, `IVirtualDesktopPinnedApps` a un autre IID et
  [VirtualDesktop.cs](../VirtualDesktop.cs) devra être ajusté.
- L'épinglage porte sur la fenêtre, pas sur l'application : rouvrir une fenêtre
  après l'avoir fermée demande de la ré-épingler.
