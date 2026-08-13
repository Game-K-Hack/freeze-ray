<div align="center">

![Banner di Freeze Ray](./banner.png)

**Congela una finestra: resta visibile nello stesso punto su tutti i desktop virtuali, e sopra ogni altra cosa.**

[English](../README.md) · [Français](./README.fr.md) · [Deutsch](./README.de.md) · [Español](./README.es.md) · **Italiano** · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [Русский](./README.ru.md) · [中文](./README.zh.md)

<p align="center">
  <a href="https://github.com/Game-K-Hack/freeze-ray/releases/latest"><img src="https://img.shields.io/github/v/release/Game-K-Hack/freeze-ray?label=Scarica&style=for-the-badge&logo=windows" alt="Scarica"></a>
</p>

</div>

## A che cosa serve

Windows permette di cambiare desktop virtuale con `Ctrl + Win + ←/→`, ma tutto ciò
che si stava guardando sparisce insieme al desktop che si lascia. Freeze Ray fissa
una finestra perché **resti visibile, esattamente nello stesso punto, su tutti i
desktop**.

È un'utilità dell'area di notifica nello spirito di **DeskPin**, con due azioni
indipendenti:

- **Mantieni a schermo**: la finestra ti segue su tutti i desktop virtuali.
- **Sempre in primo piano**: la finestra resta sopra le altre, il comportamento
  classico di DeskPin.

Entrambe possono essere applicate alla stessa finestra.

## Requisiti

- **Windows 10** (compilato e verificato sulla build 19045, 22H2).
- **.NET Framework 4**: già incluso in Windows, nulla da installare.

> Su Windows 11 le interfacce non documentate della shell usate per i desktop
> virtuali hanno identificatori diversi. Vedi
> [Limiti noti](#limiti-noti).

## Per iniziare

1. Scarica `Freeze Ray.exe` dall'
   [ultima versione](https://github.com/Game-K-Hack/freeze-ray/releases/latest),
   oppure [compilalo tu stesso](#compilare-dai-sorgenti).
2. Avvialo. Non si apre alcuna finestra: compare solo un'icona nell'area di
   notifica.
3. Fai clic sull'icona → **Mantieni a schermo (tutti i desktop)…**
4. Il cursore diventa il logo dell'applicazione: fai clic sulla finestra da
   conservare. Riceverà un piccolo logo sulla barra del titolo.
5. Cambia desktop con `Ctrl + Win + ←/→`: la finestra è ancora lì.
6. Per liberarla, fai clic sul logo nella sua barra del titolo.

L'eseguibile è autonomo: non servono né installer né la cartella `assets`.

## Utilizzo

**Non viene registrata alcuna scorciatoia da tastiera globale**, quindi nulla può
entrare in conflitto con un'altra applicazione. Tutto passa dall'icona, e **un
clic — sinistro o destro — apre il menu**:

| Voce | Effetto |
|---|---|
| **Mantieni a schermo (tutti i desktop)…** | Avvia la selezione; la finestra su cui fai clic seguirà tutti i desktop |
| **Sempre in primo piano…** | Avvia la selezione; la finestra su cui fai clic diventa `TOPMOST` |
| **Finestre bloccate (n)** | Le elenca con il loro stato; facendo clic su una la si libera |
| **Libera tutto** | Riporta tutte le finestre allo stato normale |
| **Impostazioni…** | Apre la finestra delle impostazioni |
| **Esci** | |

### La modalità selezione

Dopo il clic su una delle prime due voci, **il cursore diventa il logo**,
segnalando che si attende una finestra; il clic successivo la sceglie. Quel clic
viene consumato da Freeze Ray, quindi non aziona ciò che si trova sotto il
puntatore.

- **Esc** o un **clic destro** annullano. Anche un clic sul desktop o sulla barra
  delle applicazioni rinuncia, senza messaggi.
- Selezionare una finestra già bloccata la libera: l'azione è un interruttore.
- La descrizione dell'icona mostra sempre lo stato corrente.

### Il contrassegno sulla barra del titolo

Una finestra bloccata riceve **il logo sulla barra del titolo**, subito a sinistra
dei pulsanti di sistema. **Facendo clic sul logo la finestra viene liberata** e il
contrassegno sparisce.

Il contrassegno segue la sua finestra quando viene spostata o ridimensionata,
scompare quando viene ridotta a icona e si colloca immediatamente davanti a essa
nell'ordine di profondità: un'altra finestra che copre l'obiettivo copre anche il
contrassegno. Una finestra mantenuta su tutti i desktop porta con sé il proprio
contrassegno da un desktop all'altro.

Le finestre con cornice personalizzata (browser, app UWP…) non pubblicano sempre
la geometria della barra del titolo; il contrassegno si posiziona allora
nell'angolo in alto a destra della cornice visibile.

## Impostazioni

Raggiungibili con **Impostazioni…** dal menu. La finestra mostra il logo, il nome
e il **numero di versione**, poi:

| Impostazione | Dettaglio |
|---|---|
| **Avvia con Windows** | Scrive in `HKCU\...\CurrentVersion\Run`. Il registro resta l'unica fonte di verità: la casella rilegge lo stato reale e si riallinea se la scrittura fallisce |
| **Libera tutto all'uscita** | Evita di lasciare finestre bloccate |
| **Mostra le notifiche** | Nasconde solo i fumetti informativi: **gli errori vengono sempre segnalati**, perché tacerli farebbe sembrare un'azione fallita un'azione senza effetto |
| **Lingua** | Applicata subito, menu, descrizione comando e notifiche compresi |
| **Cerca aggiornamenti all'avvio** | Interroga GitHub all'avvio; silenzioso a meno che non esista una versione più recente |

Le impostazioni vivono in `%APPDATA%\Freeze Ray\settings.ini`, un semplice file
`chiave=valore` leggibile e correggibile a mano. Al primo avvio la lingua segue quella di Windows, con ripiego sull'inglese. Sono disponibili nove lingue: inglese, francese, tedesco, spagnolo, italiano, giapponese, coreano, russo e cinese.

I testi stanno in [Strings.cs](../Strings.cs), una tabella per lingua invece di
file di risorse, così il progetto resta compilabile con il compilatore fornito da
Windows. Aggiungere una lingua significa aggiungere una tabella e una voce
nell'elenco a discesa.

### Aggiornamenti

**Cerca aggiornamenti** interroga l'API pubblica delle release di GitHub per il
repository configurato, confronta i numeri di versione e propone di aprire la
pagina di download.

**L'applicazione non si aggiorna da sola, deliberatamente.** Sostituire un
eseguibile in esecuzione richiede un processo di appoggio, e farlo senza firma né
verifica di integrità sarebbe un vettore d'attacco: per un'utilità di queste
dimensioni non vale lo scambio.

## Compilare dai sorgenti

Nessun SDK da installare: basta il compilatore C# fornito con .NET Framework 4,
già presente in Windows.

```bat
build.bat
```

Produce `Freeze Ray.exe` accanto ai sorgenti. Il logo è **incorporato
nell'eseguibile**, quindi il binario funziona da solo.

## Sostituire il logo

| File | Ruolo |
|---|---|
| `assets/icon.png` | Logo sorgente (512×512, trasparente): icona dell'area di notifica, cursore di selezione e contrassegno sulla barra del titolo |
| `assets/app.ico` | Icona multi-risoluzione (16 → 256): icona del file e della finestra |
| `assets/Freeze Ray.png` | Illustrazione usata solo nell'intestazione delle impostazioni |

Per cambiare logo, sostituisci `assets/icon.png`, genera un `assets/app.ico`
con le nove dimensioni consuete (16, 20, 24, 32, 40, 48, 64, 128, 256) usando un
qualsiasi editor di icone, poi esegui `build.bat`.

Una sola immagine da 256×256 non basta: Windows dovrebbe rimpicciolirla da sé per
l'area di notifica (16×16) e la barra del titolo, con un risultato sfocato.

## Come funziona

### I desktop virtuali

Mantenere una finestra su tutti i desktop sfrutta lo stesso meccanismo della voce
*«Mostra questa finestra su tutti i desktop»* del menu contestuale di
Visualizzazione attività, esposto dalle interfacce COM non documentate della shell
`IApplicationViewCollection` e `IVirtualDesktopPinnedApps` — vedi
[VirtualDesktop.cs](../VirtualDesktop.cs).

### Selezione con un livello, non con la cattura del mouse

La selezione si basa su un **livello trasparente che copre tutti i monitor**, non
su `SetCapture`. La cattura del mouse reindirizza i messaggi solo mentre un
pulsante è premuto o mentre il puntatore si trova sopra la finestra che cattura:
per questo lo strumento di ricerca di Spy++ si usa *trascinando*. Senza pulsante
premuto, ogni finestra sorvolata continuava a imporre il proprio cursore e il logo
non compariva mai. Con il livello il puntatore è sempre sopra la nostra finestra:
è lei a imporre il cursore e a ricevere il clic. Vedi
[WindowPicker.cs](../WindowPicker.cs).

### Il contrassegno

Il contrassegno è una finestra a trasparenza per pixel (`WS_EX_LAYERED` +
`UpdateLayeredWindow`), che preserva l'antialiasing del logo su qualsiasi sfondo.
Non prende mai il fuoco, quindi farci clic non disattiva la finestra bersaglio, e
le sue zone trasparenti lasciano passare il clic verso la barra del titolo
sottostante.

**Per spostare il contrassegno** basta un'impostazione in
[WindowMarker.cs](../WindowMarker.cs): `BUTTON_GAP`, la distanza dal primo
pulsante di sistema (4 px). Più è piccola, più il contrassegno va a destra; sotto
zero si sovrappone al pulsante Riduci a icona.

La larghezza del blocco dei pulsanti non è leggibile direttamente: la metrica di
sistema `SM_CXSIZE` dichiara 36 px là dove Windows 10 disegna pulsanti da 46 px
(misurato al pixel: glifi centrati ogni 46 px). Segue però correttamente il
ridimensionamento dello schermo, da cui il rapporto 46/36 usato nel codice.

### Applicazioni che pongono il veto al «sempre in primo piano»

Alcune applicazioni **rifiutano** che se ne modifichi l'ordine di profondità:
intercettano `WM_WINDOWPOSCHANGING` e neutralizzano la modifica al passaggio.
`SetWindowPos` restituisce allora **successo senza aver fatto nulla**: VLC si
comporta così durante la riproduzione di un video (misurato: il flag era ancora
assente un secondo intero dopo la chiamata).

Da qui due precauzioni nel codice:

- il flag `SWP_NOSENDCHANGING` sopprime quella notifica e priva l'applicazione del
  suo diritto di veto;
- lo stato viene **riletto dopo** invece di fidarsi del valore restituito, così un
  fallimento reale viene segnalato anziché passare sotto silenzio.

### Le notifiche

I fumetti informativi mostrano **il logo dell'applicazione** al posto della «i»
azzurra di sistema. WinForms non sa farlo: `NotifyIcon.ShowBalloonTip` accetta
solo icone di sistema e rifiuta qualsiasi valore fuori dalla sua enumerazione. Ci
si rivolge quindi direttamente alla shell (`Shell_NotifyIcon` con `NIIF_USER`),
riutilizzando l'identificazione interna della voce creata da WinForms — vedi
[Notifications.cs](../Notifications.cs). Se quel dettaglio interno dovesse
cambiare, il codice ripiega sul fumetto standard.

L'intestazione della notifica mostra `Freeze Ray.exe`: Windows vi inserisce il
nome del file eseguibile. Dichiarare un `AppUserModelID` non cambia nulla
(verificato); solo l'installazione di un collegamento nel menu Start consentirebbe
un nome senza estensione.

## Limiti noti

- Una finestra appartenente a un processo **con privilegi elevati** può essere
  modificata solo se anche Freeze Ray è avviato come amministratore.
- Le interfacce COM usate per i desktop virtuali non sono documentate e i loro
  identificatori cambiano tra le versioni di Windows. I GUID adottati qui sono
  quelli di **Windows 10 1803 → 22H2**, verificati sulla build **19045**. Su
  Windows 11 `IVirtualDesktopPinnedApps` ha un IID diverso e
  [VirtualDesktop.cs](../VirtualDesktop.cs) va adattato.
- Il fissaggio riguarda la finestra, non l'applicazione: riaprire una finestra
  dopo averla chiusa richiede di fissarla di nuovo.
