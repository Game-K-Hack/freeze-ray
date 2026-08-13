<div align="center">

![Freeze Ray banner](documentation/banner.png)

**Freeze a window in place: keep it visible at the same spot across every virtual desktop, and on top of everything else.**

**English** · [Français](./documentation/README.fr.md) · [Deutsch](./documentation/README.de.md) · [Español](./documentation/README.es.md) · [Italiano](./documentation/README.it.md) · [日本語](./documentation/README.ja.md) · [한국어](./documentation/README.ko.md) · [Русский](./documentation/README.ru.md) · [中文](./documentation/README.zh.md)

<p align="center">
  <a href="https://github.com/Game-K-Hack/freeze-ray/releases/latest"><img src="https://img.shields.io/github/v/release/Game-K-Hack/freeze-ray?label=Download&style=for-the-badge&logo=windows" alt="Download"></a>
</p>

</div>

## What it does

Windows lets you switch virtual desktops with `Ctrl + Win + ←/→`, but everything
you were looking at disappears with the desktop you left. Freeze Ray pins a window
so it **stays visible, at the exact same spot, on every desktop**.

It is a tray utility in the spirit of **DeskPin**, with two independent actions:

- **Keep on screen** — the window follows you across all virtual desktops.
- **Always on top** — the window stays above the others, the classic DeskPin behaviour.

Both can be applied to the same window.

## Requirements

- **Windows 10** (built and verified on build 19045, 22H2).
- **.NET Framework 4** — already part of Windows, nothing to install.

> On Windows 11 the undocumented shell interfaces used for virtual desktops have
> different identifiers. See [Known limitations](#known-limitations).

## Getting started

1. Download `Freeze Ray.exe` from the
   [latest release](https://github.com/Game-K-Hack/freeze-ray/releases/latest),
   or [build it yourself](#building-from-source).
2. Run it. No window opens — only an icon appears in the notification area.
3. Click the icon → **Keep on screen (all desktops)…**
4. The cursor turns into the app logo: click the window you want to keep. It gets
   a small logo on its title bar.
5. Switch desktops with `Ctrl + Win + ←/→`: the window is still there.
6. To release it, click the logo on its title bar.

Two ways to install it: run `Freeze Ray Setup.exe`, which shows the licence, lets
you pick the folder and offers desktop and Start-menu shortcuts; or simply drop
`Freeze Ray.exe` wherever you like — it is self-contained and needs no `assets`
folder.

## Using it

**No global keyboard shortcut is registered**, so nothing can clash with another
application. Everything goes through the icon, and **a click — left or right —
opens the menu**:

| Entry | Effect |
|---|---|
| **Keep on screen (all desktops)…** | Enters picking mode; the window you click then follows every desktop |
| **Always on top…** | Enters picking mode; the window you click becomes `TOPMOST` |
| **Locked windows (n)** | Lists them with their state; clicking one releases it |
| **Release all** | Puts every window back to normal |
| **Settings…** | Opens the settings window |
| **Quit** | |

### Picking mode

After clicking one of the first two entries, **the cursor becomes the app logo**,
showing that a window is expected; the next click chooses it. That click is
consumed by Freeze Ray, so it does not press whatever is under the pointer.

- **Esc** or a **right click** cancel. Clicking the desktop or the taskbar also
  gives up, silently.
- Picking an already locked window releases it — the action is a toggle.
- The icon tooltip always shows the current state.

### The title-bar marker

A locked window gets **the logo on its title bar**, just left of the system
buttons. **Clicking it releases the window** and removes the marker.

The marker follows its window when moved or resized, disappears when the window
is minimised, and sits immediately in front of it in the z-order — so another
window covering the target covers the marker too. A window kept on all desktops
takes its marker along from one desktop to the next.

Windows with a custom frame (browsers, UWP apps…) do not always publish their
title-bar geometry; the marker then goes to the top-right corner of the visible
frame.

## Settings

Reachable through **Settings…** in the menu. The window shows the logo, the name
and the **version number**, then:

| Setting | Detail |
|---|---|
| **Start with Windows** | Writes to `HKCU\...\CurrentVersion\Run`. The registry stays the single source of truth: the checkbox reads the real state back and realigns if the write fails |
| **Release everything on exit** | Avoids leaving windows stuck |
| **Show notifications** | Hides informational balloons only — **errors are always reported**, because silencing them would make a broken action look like nothing happened |
| **Language** | Applied immediately, including the menu, the tooltip and the notifications |
| **Check for updates at startup** | Queries GitHub when the application starts; silent unless a newer version exists |

Settings live in `settings.ini`, a plain `key=value` file you
can read and fix by hand. On first run the language follows Windows, falling back to English. Nine languages are available: English, French, German, Spanish, Italian, Japanese, Korean, Russian and Chinese.

The file sits **next to the application**, so the whole installation is one
folder you can copy onto a USB stick or delete in one go. If that folder is not
writable — a copy dropped into `Program Files`, say — the settings fall back to
`%APPDATA%\Freeze Ray` instead of being silently lost.

The texts live in [Strings.cs](Strings.cs) as one table per language rather than
resource files, so the project stays buildable with the compiler shipped with
Windows. Adding a language means adding a table and one entry in the drop-down.

### Updates

**Check for updates** queries the public GitHub releases API, compares version
numbers and offers to open the download page. The same check can run at startup,
where it stays **silent unless a newer version exists** — it then shows a
notification you can click to open the download page. Nothing is reported when
you are up to date or when GitHub cannot be reached.

The repository is **fixed in the code** ([Updater.cs](Updater.cs)) rather than
being configurable: a user-editable update source would be a convenient way to
have someone download anything under the application's name.

**The application deliberately does not update itself.** Replacing a running
executable requires a helper process, and doing it without a signature or an
integrity check would be an attack vector — not a worthwhile trade for a utility
this size.

## Building from source

No SDK to install: the C# compiler shipped with .NET Framework 4, already present
in Windows, is enough.

```bat
build.bat
```

This produces `Freeze Ray.exe` next to the sources. The logo is **embedded in the
executable**, so the binary works on its own.

## How it works

### Virtual desktops

Keeping a window on every desktop uses the very mechanism behind the *“Show this
window on all desktops”* right-click entry of Task View, exposed by the
undocumented shell COM interfaces `IApplicationViewCollection` and
`IVirtualDesktopPinnedApps` — see [VirtualDesktop.cs](VirtualDesktop.cs).

### Picking with an overlay, not mouse capture

Picking relies on a **transparent layer covering every monitor**, not on
`SetCapture`. Mouse capture only redirects messages while a button is held down
or while the pointer is over the capturing window — which is why the Spy++ finder
tool is used by *dragging*. With no button pressed, every hovered window kept
imposing its own cursor and the logo never appeared. With the overlay the pointer
is permanently over our own window, so it imposes its cursor and receives the
click. See [WindowPicker.cs](WindowPicker.cs).

### The marker

The marker is a per-pixel transparent window (`WS_EX_LAYERED` +
`UpdateLayeredWindow`), which preserves the antialiasing of the logo over any
background. It never takes focus, so clicking it does not deactivate the target
window, and its transparent areas let clicks through to the title bar underneath.

**To move the marker**, one setting in [WindowMarker.cs](WindowMarker.cs):
`BUTTON_GAP`, the gap to the first system button (4 px). The smaller it is, the
further right the marker sits; below zero it overlaps the Minimise button.

The width of the button block cannot be read directly: the system metric
`SM_CXSIZE` reports 36 px where Windows 10 draws 46 px buttons (measured to the
pixel: glyphs centred every 46 px). It does follow display scaling correctly,
hence the 46/36 ratio used in the code.

### Applications vetoing “always on top”

Some applications **refuse** to have their z-order changed: they intercept
`WM_WINDOWPOSCHANGING` and neutralise the change on the way through.
`SetWindowPos` then returns **success without doing anything** — VLC behaves this
way while playing a video (measured: the flag was still absent a full second
after the call).

Hence two precautions in the code:

- the `SWP_NOSENDCHANGING` flag suppresses that notification, denying the
  application its veto;
- the state is **read back afterwards** instead of trusting the return value, so a
  real failure is reported rather than silently swallowed.

### Notifications

Informational balloons show **the app logo** instead of the blue system “i”.
WinForms cannot do this: `NotifyIcon.ShowBalloonTip` only accepts system icons and
rejects any value outside its enumeration. The shell is therefore addressed
directly (`Shell_NotifyIcon` with `NIIF_USER`), reusing the internal identity of
the entry WinForms created — see [Notifications.cs](Notifications.cs). Should that
internal detail ever change, the code falls back to the standard balloon.

The notification header shows `Freeze Ray.exe`: Windows puts the executable file
name there. Declaring an `AppUserModelID` changes nothing (verified); only
installing a Start-menu shortcut would allow a name without the extension.

## Known limitations

- A window owned by an **elevated** process can only be changed if Freeze Ray is
  elevated too.
- The COM interfaces used for virtual desktops are undocumented and their
  identifiers change between Windows versions. The GUIDs used here are those of
  **Windows 10 1803 → 22H2**, verified on build **19045**. On Windows 11,
  `IVirtualDesktopPinnedApps` has a different IID and
  [VirtualDesktop.cs](VirtualDesktop.cs) must be adjusted.
- Pinning applies to the window, not to the application: reopening a window after
  closing it requires pinning it again.

## Licence

Free and unlimited use, at home and at work. You may copy it, study it, modify it
and pass it on, provided you **do not sell it**, **credit the author** in any
public modified version, and keep the licence with every copy.

Full text: [LICENSE.md](LICENSE.md).
