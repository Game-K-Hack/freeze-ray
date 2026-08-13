<div align="center">

![Freeze-Ray-Banner](./banner.png)

**Frieren Sie ein Fenster ein: Es bleibt auf jedem virtuellen Desktop an derselben Stelle sichtbar – und über allem anderen.**

[English](../README.md) · [Français](./README.fr.md) · **Deutsch** · [Español](./README.es.md) · [Italiano](./README.it.md) · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [Русский](./README.ru.md) · [中文](./README.zh.md)

<p align="center">
  <a href="https://github.com/Game-K-Hack/freeze-ray/releases/latest"><img src="https://img.shields.io/github/v/release/Game-K-Hack/freeze-ray?label=Download&style=for-the-badge&logo=windows" alt="Download"></a>
</p>

</div>

## Wozu es dient

Mit `Strg + Win + ←/→` wechselt man unter Windows den virtuellen Desktop – doch
alles, was man gerade betrachtet hat, verschwindet mit dem verlassenen Desktop.
Freeze Ray heftet ein Fenster fest, sodass es **auf jedem Desktop an genau
derselben Stelle sichtbar bleibt**.

Es ist ein Infobereich-Werkzeug im Geiste von **DeskPin**, mit zwei unabhängigen
Aktionen:

- **Auf dem Bildschirm halten** – das Fenster folgt Ihnen über alle virtuellen
  Desktops.
- **Immer im Vordergrund** – das Fenster bleibt über den anderen, das klassische
  DeskPin-Verhalten.

Beides lässt sich auf dasselbe Fenster anwenden.

## Voraussetzungen

- **Windows 10** (gebaut und geprüft auf Build 19045, 22H2).
- **.NET Framework 4** – bereits Teil von Windows, nichts zu installieren.

> Unter Windows 11 tragen die undokumentierten Shell-Schnittstellen für virtuelle
> Desktops andere Bezeichner. Siehe [Bekannte Grenzen](#bekannte-grenzen).

## Erste Schritte

1. Laden Sie `Freeze Ray.exe` aus der
   [neuesten Version](https://github.com/Game-K-Hack/freeze-ray/releases/latest)
   herunter oder [bauen Sie es selbst](#aus-den-quellen-bauen).
2. Starten Sie es. Es öffnet sich kein Fenster – nur ein Symbol im Infobereich.
3. Klicken Sie auf das Symbol → **Auf dem Bildschirm halten (alle Desktops)…**
4. Der Mauszeiger wird zum Logo der Anwendung: Klicken Sie das gewünschte Fenster
   an. Es erhält ein kleines Logo in seiner Titelleiste.
5. Wechseln Sie den Desktop mit `Strg + Win + ←/→`: Das Fenster ist noch da.
6. Zum Freigeben klicken Sie das Logo in der Titelleiste an.

Die ausführbare Datei ist eigenständig: kein Installationsprogramm, kein
`assets`-Ordner nötig.

## Bedienung

**Es wird kein globales Tastenkürzel registriert**, ein Konflikt mit anderen
Anwendungen ist also ausgeschlossen. Alles läuft über das Symbol, und **ein Klick
– links oder rechts – öffnet das Menü**:

| Eintrag | Wirkung |
|---|---|
| **Auf dem Bildschirm halten (alle Desktops)…** | Startet die Auswahl; das angeklickte Fenster folgt danach allen Desktops |
| **Immer im Vordergrund…** | Startet die Auswahl; das angeklickte Fenster wird `TOPMOST` |
| **Fixierte Fenster (n)** | Listet sie mit ihrem Zustand auf; ein Klick gibt eines frei |
| **Alle freigeben** | Setzt alle Fenster zurück |
| **Einstellungen…** | Öffnet das Einstellungsfenster |
| **Beenden** | |

### Der Auswahlmodus

Nach dem Klick auf einen der ersten beiden Einträge **wird der Mauszeiger zum
Logo** und zeigt damit an, dass ein Fenster erwartet wird; der nächste Klick wählt
es aus. Diesen Klick verbraucht Freeze Ray, er betätigt also nicht, was sich unter
dem Zeiger befindet.

- **Esc** oder ein **Rechtsklick** brechen ab. Ein Klick auf den Desktop oder die
  Taskleiste bricht ebenfalls ab, ohne Meldung.
- Ein bereits fixiertes Fenster auszuwählen gibt es frei – die Aktion schaltet um.
- Die Quickinfo des Symbols zeigt jederzeit den aktuellen Zustand.

### Die Marke in der Titelleiste

Ein fixiertes Fenster erhält **das Logo in seiner Titelleiste**, direkt links der
Systemschaltflächen. **Ein Klick darauf gibt das Fenster frei** und entfernt die
Marke.

Die Marke folgt ihrem Fenster beim Verschieben und beim Ändern der Größe,
verschwindet beim Minimieren und liegt in der Z-Reihenfolge unmittelbar davor –
ein anderes Fenster, das das Ziel verdeckt, verdeckt daher auch die Marke. Ein
Fenster, das auf allen Desktops gehalten wird, nimmt seine Marke mit.

Fenster mit eigenem Rahmen (Browser, UWP-Apps …) veröffentlichen die Geometrie
ihrer Titelleiste nicht immer; die Marke sitzt dann in der oberen rechten Ecke des
sichtbaren Rahmens.

## Einstellungen

Erreichbar über **Einstellungen…** im Menü. Das Fenster zeigt Logo, Name und die
**Versionsnummer**, danach:

| Einstellung | Erläuterung |
|---|---|
| **Mit Windows starten** | Schreibt nach `HKCU\...\CurrentVersion\Run`. Die Registrierung bleibt die einzige Wahrheitsquelle: Das Kästchen liest den tatsächlichen Zustand zurück und richtet sich neu aus, falls das Schreiben fehlschlägt |
| **Beim Beenden alles freigeben** | Verhindert hängengebliebene Fenster |
| **Benachrichtigungen anzeigen** | Blendet nur informative Sprechblasen aus – **Fehler werden immer gemeldet**, denn sie zu verschweigen ließe eine fehlgeschlagene Aktion wie eine wirkungslose aussehen |
| **Sprache** | Sofort wirksam, einschließlich Menü, Quickinfo und Benachrichtigungen |
| **GitHub-Repository** | Quelle für die Update-Prüfung, in der Form `besitzer/repository` |

Die Einstellungen liegen in `%APPDATA%\Freeze Ray\settings.ini`, einer schlichten
`Schlüssel=Wert`-Datei, die sich von Hand lesen und korrigieren lässt. Beim ersten Start folgt die Sprache jener von Windows, ersatzweise Englisch. Neun Sprachen stehen zur Wahl: Englisch, Französisch, Deutsch, Spanisch, Italienisch, Japanisch, Koreanisch, Russisch und Chinesisch.

Die Texte stehen in [Strings.cs](../Strings.cs) als eine Tabelle je Sprache statt
in Ressourcendateien, damit sich das Projekt weiterhin mit dem von Windows
mitgelieferten Compiler bauen lässt. Eine Sprache hinzuzufügen heißt: eine Tabelle
und einen Eintrag im Auswahlfeld ergänzen.

### Updates

**Nach Updates suchen** fragt die öffentliche GitHub-Releases-API für das
konfigurierte Repository ab, vergleicht die Versionsnummern und bietet an, die
Download-Seite zu öffnen.

**Die Anwendung aktualisiert sich bewusst nicht selbst.** Eine laufende
ausführbare Datei zu ersetzen erfordert einen Hilfsprozess, und ohne Signatur oder
Integritätsprüfung wäre das ein Angriffsvektor – für ein Werkzeug dieser Größe
kein lohnender Handel.

## Aus den Quellen bauen

Kein SDK nötig: Der mit .NET Framework 4 ausgelieferte C#-Compiler, in Windows
bereits vorhanden, genügt.

```bat
build.bat
```

Das erzeugt `Freeze Ray.exe` neben den Quellen. Das Logo ist **in die ausführbare
Datei eingebettet**, das Binary funktioniert also allein.

## Das Logo ersetzen

| Datei | Rolle |
|---|---|
| `assets/icon.png` | Quell-Logo (512×512, transparent) – Infobereichssymbol, Auswahlzeiger und Titelleistenmarke |
| `assets/app.ico` | **Erzeugt** von `tools/MakeIcon.cs` – Datei- und Fenstersymbol |
| `assets/Freeze Ray.png` | Nur für den Kopfbereich der Einstellungen verwendete Illustration |

`icon.ico` enthielt ursprünglich ein einziges 256×256-Bild, das Windows für den
Infobereich (16×16) und die Titelleiste selbst hätte verkleinern müssen – mit
unscharfem Ergebnis. `tools/MakeIcon.cs` berechnet deshalb die neun nützlichen
Größen (16 → 256) aus dem PNG mit hochwertiger Neuabtastung vor.

Zum Wechseln des Logos ersetzen Sie `assets/icon.png` und erzeugen neu:

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:MakeIcon.exe /r:System.Drawing.dll tools\MakeIcon.cs
MakeIcon.exe
build.bat
```

## Wie es funktioniert

### Virtuelle Desktops

Ein Fenster auf allen Desktops zu halten nutzt genau den Mechanismus hinter dem
Kontextmenü-Eintrag *„Dieses Fenster auf allen Desktops anzeigen“* der
Taskansicht, bereitgestellt von den undokumentierten Shell-COM-Schnittstellen
`IApplicationViewCollection` und `IVirtualDesktopPinnedApps` – siehe
[VirtualDesktop.cs](../VirtualDesktop.cs).

### Auswahl per Overlay statt per Mausaufzeichnung

Die Auswahl beruht auf einer **transparenten Schicht über allen Monitoren**, nicht
auf `SetCapture`. Die Mausaufzeichnung leitet Nachrichten nur um, solange eine
Taste gedrückt ist oder der Zeiger über dem aufzeichnenden Fenster steht – darum
bedient man das Suchwerkzeug von Spy++ *ziehend*. Ohne gedrückte Taste setzte
jedes überfahrene Fenster weiterhin seinen eigenen Zeiger, und das Logo erschien
nie. Mit dem Overlay steht der Zeiger dauerhaft über unserem eigenen Fenster: Es
setzt seinen Zeiger durch und empfängt den Klick. Siehe
[WindowPicker.cs](../WindowPicker.cs).

### Die Marke

Die Marke ist ein Fenster mit Transparenz je Pixel (`WS_EX_LAYERED` +
`UpdateLayeredWindow`), was die Kantenglättung des Logos auf jedem Untergrund
erhält. Sie nimmt nie den Fokus, ein Klick darauf deaktiviert das Zielfenster also
nicht, und ihre transparenten Bereiche lassen den Klick zur darunterliegenden
Titelleiste durch.

**Zum Verschieben der Marke** genügt eine Einstellung in
[WindowMarker.cs](../WindowMarker.cs): `BUTTON_GAP`, der Abstand zur ersten
Systemschaltfläche (4 px). Je kleiner er ist, desto weiter rechts sitzt die Marke;
unter null überlappt sie die Schaltfläche „Minimieren“.

Die Breite des Schaltflächenblocks lässt sich nicht direkt auslesen: Die
Systemmetrik `SM_CXSIZE` meldet 36 px, während Windows 10 Schaltflächen von 46 px
zeichnet (auf den Pixel gemessen: Glyphen alle 46 px zentriert). Sie folgt aber
korrekt der Anzeigeskalierung, daher das im Code verwendete Verhältnis 46/36.

### Anwendungen mit Veto gegen „Immer im Vordergrund“

Manche Anwendungen **verweigern** eine Änderung ihrer Z-Reihenfolge: Sie fangen
`WM_WINDOWPOSCHANGING` ab und entschärfen die Änderung im Vorbeigehen.
`SetWindowPos` meldet dann **Erfolg, ohne etwas getan zu haben** – VLC verhält sich
während der Videowiedergabe so (gemessen: Das Flag fehlte noch eine volle Sekunde
nach dem Aufruf).

Daher zwei Vorkehrungen im Code:

- Das Flag `SWP_NOSENDCHANGING` unterdrückt diese Benachrichtigung und nimmt der
  Anwendung ihr Vetorecht;
- der Zustand wird **hinterher zurückgelesen**, statt dem Rückgabewert zu trauen,
  damit ein echter Fehlschlag gemeldet und nicht verschluckt wird.

### Benachrichtigungen

Informative Sprechblasen zeigen **das Logo der Anwendung** statt des blauen
System-„i“. WinForms kann das nicht: `NotifyIcon.ShowBalloonTip` akzeptiert nur
Systemsymbole und weist jeden Wert außerhalb seiner Aufzählung zurück. Daher wird
die Shell direkt angesprochen (`Shell_NotifyIcon` mit `NIIF_USER`), unter
Wiederverwendung der internen Kennung des von WinForms angelegten Eintrags – siehe
[Notifications.cs](../Notifications.cs). Sollte sich dieses interne Detail je
ändern, fällt der Code auf die Standardsprechblase zurück.

Die Kopfzeile der Benachrichtigung zeigt `Freeze Ray.exe`: Windows setzt dort den
Dateinamen ein. Eine `AppUserModelID` zu deklarieren ändert daran nichts
(überprüft); nur eine Verknüpfung im Startmenü erlaubte einen Namen ohne Endung.

## Bekannte Grenzen

- Ein Fenster eines **erhöht** laufenden Prozesses lässt sich nur ändern, wenn
  Freeze Ray ebenfalls als Administrator läuft.
- Die für virtuelle Desktops genutzten COM-Schnittstellen sind undokumentiert und
  ihre Bezeichner ändern sich zwischen Windows-Versionen. Die hier verwendeten
  GUIDs sind jene von **Windows 10 1803 → 22H2**, geprüft auf Build **19045**.
  Unter Windows 11 hat `IVirtualDesktopPinnedApps` eine andere IID und
  [VirtualDesktop.cs](../VirtualDesktop.cs) muss angepasst werden.
- Das Fixieren gilt dem Fenster, nicht der Anwendung: Ein wieder geöffnetes
  Fenster muss erneut fixiert werden.
