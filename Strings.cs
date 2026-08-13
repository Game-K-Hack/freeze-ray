using System.Collections.Generic;
using System.Globalization;

namespace FreezeRay
{
    internal enum Language
    {
        English,
        French,
        German,
        Spanish,
        Italian,
        Japanese,
        Korean,
        Russian,
        Chinese
    }

    /// <summary>
    /// Textes de l'interface. Une table par langue plutôt que des fichiers de
    /// ressources : l'application se compile avec le seul csc fourni par Windows,
    /// sans outil de génération d'assemblys satellites.
    ///
    /// Ajouter une langue : écrire sa table, puis l'inscrire dans <see cref="Entries"/>.
    /// La liste déroulante des paramètres et le fichier de réglages s'y adaptent
    /// d'eux-mêmes.
    /// </summary>
    internal static class Strings
    {
        /// <summary>Nom du produit : jamais traduit.</summary>
        public const string AppName = "Freeze Ray";

        /// <summary>Une langue proposée : code ISO, nom dans sa propre langue, table.</summary>
        internal sealed class Entry
        {
            public readonly Language Id;
            public readonly string Code;
            public readonly string NativeName;
            public readonly Dictionary<string, string> Table;

            public Entry(Language id, string code, string nativeName,
                         Dictionary<string, string> table)
            {
                Id = id;
                Code = code;
                NativeName = nativeName;
                Table = table;
            }
        }

        // Volontairement pas initialisé avec Detect() : les initialiseurs statiques
        // s'exécutent dans l'ordre du fichier, et Entries n'existe pas encore ici.
        // La langue réelle est posée au démarrage, depuis les réglages.
        private static Language _current = Language.English;

        public static Language Current
        {
            get { return _current; }
            set { _current = value; }
        }

        /// <summary>Langues proposées, dans l'ordre d'affichage.</summary>
        public static Entry[] All
        {
            get { return Entries; }
        }

        public static Entry Get(Language language)
        {
            foreach (Entry entry in Entries)
                if (entry.Id == language) return entry;
            return Entries[0];
        }

        public static int IndexOf(Language language)
        {
            for (int i = 0; i < Entries.Length; i++)
                if (Entries[i].Id == language) return i;
            return 0;
        }

        public static string CodeOf(Language language)
        {
            return Get(language).Code;
        }

        public static Language FromCode(string code)
        {
            if (!string.IsNullOrEmpty(code))
            {
                foreach (Entry entry in Entries)
                {
                    if (string.Equals(entry.Code, code,
                            System.StringComparison.OrdinalIgnoreCase))
                        return entry.Id;
                }
            }
            return Language.English;
        }

        /// <summary>Langue du système au premier lancement, anglais si non traduite.</summary>
        public static Language Detect()
        {
            return FromCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        }

        public static string T(string key)
        {
            string value;
            if (Get(_current).Table.TryGetValue(key, out value)) return value;
            // Repli sur l'anglais : une traduction incomplète ne doit jamais
            // produire de libellé vide.
            if (En.TryGetValue(key, out value)) return value;
            return key;
        }

        public static string T(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "app.alreadyRunning", "Freeze Ray is already running (icon in the notification area)." },

            { "menu.allDesktops", "Keep on screen (all desktops)…" },
            { "menu.allDesktops.tip", "Click here, then pick the window to keep when switching desktops." },
            { "menu.topMost", "Always on top…" },
            { "menu.topMost.tip", "Click here, then pick the window to keep above the others." },
            { "menu.noLocked", "No locked window" },
            { "menu.locked", "Locked windows ({0})" },
            { "menu.releaseTip", "Click to release this window." },
            { "menu.releaseAll", "Release all" },
            { "menu.settings", "Settings…" },
            { "menu.quit", "Quit" },

            { "state.both", "all desktops + always on top" },
            { "state.desktops", "all desktops" },
            { "state.topMost", "always on top" },

            { "tray.picking", "Freeze Ray — pick a window (Esc to cancel)" },
            { "tray.locked", "Freeze Ray — {0} locked window(s)" },

            { "notif.vd.title", "Virtual desktops unavailable" },
            { "notif.vd.text", "Could not reach the Windows shell: {0}" },
            { "notif.vd.unknown", "unknown reason" },
            { "notif.vd.noAnswer", "The shell did not answer." },
            { "notif.unusable.title", "Unusable window" },
            { "notif.unusable.text", "Freeze Ray could not identify a window at this spot." },
            { "notif.failed.title", "Failed" },
            { "notif.failed.pin", "Could not change “{0}”.\nWindows belonging to elevated applications require Freeze Ray to be elevated too." },
            { "notif.failed.topMost", "Could not change “{0}”.\nAn elevated application requires Freeze Ray to be elevated too." },
            { "notif.desktops.on", "Kept on all desktops" },
            { "notif.desktops.off", "No longer follows desktops" },
            { "notif.topMost.on", "Always on top" },
            { "notif.topMost.off", "Always on top disabled" },
            { "notif.released.title", "Window released" },
            { "notif.released.count", "{0} window(s) released." },
            { "notif.autostart.error", "Could not change the startup entry: {0}" },

            { "settings.title", "Settings" },
            { "settings.general", "General" },
            { "settings.startWithWindows", "Start with Windows" },
            { "settings.releaseOnExit", "Release everything on exit" },
            { "settings.notifications", "Show notifications" },
            { "settings.notificationsHint", "Errors are always reported." },
            { "settings.language", "Language" },
            { "settings.updates", "Updates" },
            { "settings.check", "Check for updates" },
            { "settings.checkAtStartup", "Check for updates at startup" },
            { "settings.close", "Close" },
            { "settings.version", "Version {0}" },

            { "update.checking", "Checking…" },
            { "update.upToDate", "You have the latest version ({0})." },
            { "update.available", "Version {0} is available. Open the download page?" },
            { "update.availableTitle", "Update available" },
            { "update.availableBalloon", "Version {0} is available. Click to open the download page." },
            { "update.noRelease", "No release has been published on this repository yet." },
            { "update.error", "Check failed: {0}" },
            { "update.badResponse", "unexpected response" },
        };

        private static readonly Dictionary<string, string> Fr = new Dictionary<string, string>
        {
            { "app.alreadyRunning", "Freeze Ray est déjà lancé (icône dans la zone de notification)." },

            { "menu.allDesktops", "Maintenir à l'écran (tous les bureaux)…" },
            { "menu.allDesktops.tip", "Cliquez ici, puis désignez la fenêtre à conserver lors des changements de bureau." },
            { "menu.topMost", "Premier plan (toujours visible)…" },
            { "menu.topMost.tip", "Cliquez ici, puis désignez la fenêtre à garder au-dessus des autres." },
            { "menu.noLocked", "Aucune fenêtre verrouillée" },
            { "menu.locked", "Fenêtres verrouillées ({0})" },
            { "menu.releaseTip", "Cliquer pour libérer cette fenêtre." },
            { "menu.releaseAll", "Tout libérer" },
            { "menu.settings", "Paramètres…" },
            { "menu.quit", "Quitter" },

            { "state.both", "tous les bureaux + premier plan" },
            { "state.desktops", "tous les bureaux" },
            { "state.topMost", "premier plan" },

            { "tray.picking", "Freeze Ray — désignez une fenêtre (Échap pour annuler)" },
            { "tray.locked", "Freeze Ray — {0} fenêtre(s) verrouillée(s)" },

            { "notif.vd.title", "Bureaux virtuels indisponibles" },
            { "notif.vd.text", "Impossible de joindre le shell Windows : {0}" },
            { "notif.vd.unknown", "raison inconnue" },
            { "notif.vd.noAnswer", "Le shell n'a pas répondu." },
            { "notif.unusable.title", "Fenêtre inutilisable" },
            { "notif.unusable.text", "Freeze Ray n'a pas pu identifier de fenêtre à cet endroit." },
            { "notif.failed.title", "Échec" },
            { "notif.failed.pin", "Impossible de modifier « {0} ».\nLes fenêtres d'applications lancées en administrateur exigent que Freeze Ray le soit aussi." },
            { "notif.failed.topMost", "Impossible de modifier « {0} ».\nUne application lancée en administrateur exige que Freeze Ray le soit aussi." },
            { "notif.desktops.on", "Maintenue sur tous les bureaux" },
            { "notif.desktops.off", "Ne suit plus les bureaux" },
            { "notif.topMost.on", "Toujours au premier plan" },
            { "notif.topMost.off", "Premier plan désactivé" },
            { "notif.released.title", "Fenêtre libérée" },
            { "notif.released.count", "{0} fenêtre(s) libérée(s)." },
            { "notif.autostart.error", "Impossible de modifier le démarrage automatique : {0}" },

            { "settings.title", "Paramètres" },
            { "settings.general", "Général" },
            { "settings.startWithWindows", "Démarrer avec Windows" },
            { "settings.releaseOnExit", "Tout libérer en quittant" },
            { "settings.notifications", "Afficher les notifications" },
            { "settings.notificationsHint", "Les erreurs restent toujours signalées." },
            { "settings.language", "Langue" },
            { "settings.updates", "Mises à jour" },
            { "settings.check", "Rechercher les mises à jour" },
            { "settings.checkAtStartup", "Rechercher les mises à jour au démarrage" },
            { "settings.close", "Fermer" },
            { "settings.version", "Version {0}" },

            { "update.checking", "Recherche en cours…" },
            { "update.upToDate", "Vous avez la dernière version ({0})." },
            { "update.available", "Version {0} disponible. Ouvrir la page de téléchargement ?" },
            { "update.availableTitle", "Mise à jour disponible" },
            { "update.availableBalloon", "Version {0} disponible. Cliquez pour ouvrir la page de téléchargement." },
            { "update.noRelease", "Aucune version n'est encore publiée sur ce dépôt." },
            { "update.error", "Recherche impossible : {0}" },
            { "update.badResponse", "réponse inattendue" },
        };

        private static readonly Dictionary<string, string> De = new Dictionary<string, string>
        {
            { "app.alreadyRunning", "Freeze Ray läuft bereits (Symbol im Infobereich)." },

            { "menu.allDesktops", "Auf dem Bildschirm halten (alle Desktops)…" },
            { "menu.allDesktops.tip", "Hier klicken, dann das Fenster wählen, das beim Desktopwechsel bleiben soll." },
            { "menu.topMost", "Immer im Vordergrund…" },
            { "menu.topMost.tip", "Hier klicken, dann das Fenster wählen, das über den anderen bleiben soll." },
            { "menu.noLocked", "Kein fixiertes Fenster" },
            { "menu.locked", "Fixierte Fenster ({0})" },
            { "menu.releaseTip", "Klicken, um dieses Fenster freizugeben." },
            { "menu.releaseAll", "Alle freigeben" },
            { "menu.settings", "Einstellungen…" },
            { "menu.quit", "Beenden" },

            { "state.both", "alle Desktops + Vordergrund" },
            { "state.desktops", "alle Desktops" },
            { "state.topMost", "Vordergrund" },

            { "tray.picking", "Freeze Ray — Fenster wählen (Esc bricht ab)" },
            { "tray.locked", "Freeze Ray — {0} fixierte(s) Fenster" },

            { "notif.vd.title", "Virtuelle Desktops nicht verfügbar" },
            { "notif.vd.text", "Windows-Shell nicht erreichbar: {0}" },
            { "notif.vd.unknown", "unbekannter Grund" },
            { "notif.vd.noAnswer", "Die Shell hat nicht geantwortet." },
            { "notif.unusable.title", "Unbrauchbares Fenster" },
            { "notif.unusable.text", "Freeze Ray konnte an dieser Stelle kein Fenster erkennen." },
            { "notif.failed.title", "Fehlgeschlagen" },
            { "notif.failed.pin", "„{0}“ konnte nicht geändert werden.\nFenster erhöht laufender Anwendungen verlangen, dass auch Freeze Ray erhöht läuft." },
            { "notif.failed.topMost", "„{0}“ konnte nicht geändert werden.\nEine erhöht laufende Anwendung verlangt, dass auch Freeze Ray erhöht läuft." },
            { "notif.desktops.on", "Auf allen Desktops gehalten" },
            { "notif.desktops.off", "Folgt den Desktops nicht mehr" },
            { "notif.topMost.on", "Immer im Vordergrund" },
            { "notif.topMost.off", "Vordergrund deaktiviert" },
            { "notif.released.title", "Fenster freigegeben" },
            { "notif.released.count", "{0} Fenster freigegeben." },
            { "notif.autostart.error", "Der Autostart-Eintrag konnte nicht geändert werden: {0}" },

            { "settings.title", "Einstellungen" },
            { "settings.general", "Allgemein" },
            { "settings.startWithWindows", "Mit Windows starten" },
            { "settings.releaseOnExit", "Beim Beenden alles freigeben" },
            { "settings.notifications", "Benachrichtigungen anzeigen" },
            { "settings.notificationsHint", "Fehler werden immer gemeldet." },
            { "settings.language", "Sprache" },
            { "settings.updates", "Updates" },
            { "settings.check", "Nach Updates suchen" },
            { "settings.checkAtStartup", "Beim Start nach Updates suchen" },
            { "settings.close", "Schließen" },
            { "settings.version", "Version {0}" },

            { "update.checking", "Wird gesucht…" },
            { "update.upToDate", "Sie haben die neueste Version ({0})." },
            { "update.available", "Version {0} ist verfügbar. Download-Seite öffnen?" },
            { "update.availableTitle", "Update verfügbar" },
            { "update.availableBalloon", "Version {0} ist verfügbar. Zum Öffnen der Download-Seite klicken." },
            { "update.noRelease", "In diesem Repository wurde noch keine Version veröffentlicht." },
            { "update.error", "Suche fehlgeschlagen: {0}" },
            { "update.badResponse", "unerwartete Antwort" },
        };

        private static readonly Dictionary<string, string> Es = new Dictionary<string, string>
        {
            { "app.alreadyRunning", "Freeze Ray ya se está ejecutando (icono en el área de notificación)." },

            { "menu.allDesktops", "Mantener en pantalla (todos los escritorios)…" },
            { "menu.allDesktops.tip", "Haz clic aquí y luego elige la ventana que debe conservarse al cambiar de escritorio." },
            { "menu.topMost", "Siempre visible…" },
            { "menu.topMost.tip", "Haz clic aquí y luego elige la ventana que debe quedar por encima de las demás." },
            { "menu.noLocked", "Ninguna ventana bloqueada" },
            { "menu.locked", "Ventanas bloqueadas ({0})" },
            { "menu.releaseTip", "Haz clic para liberar esta ventana." },
            { "menu.releaseAll", "Liberar todo" },
            { "menu.settings", "Configuración…" },
            { "menu.quit", "Salir" },

            { "state.both", "todos los escritorios + siempre visible" },
            { "state.desktops", "todos los escritorios" },
            { "state.topMost", "siempre visible" },

            { "tray.picking", "Freeze Ray — elige una ventana (Esc para cancelar)" },
            { "tray.locked", "Freeze Ray — {0} ventana(s) bloqueada(s)" },

            { "notif.vd.title", "Escritorios virtuales no disponibles" },
            { "notif.vd.text", "No se pudo contactar con el shell de Windows: {0}" },
            { "notif.vd.unknown", "motivo desconocido" },
            { "notif.vd.noAnswer", "El shell no respondió." },
            { "notif.unusable.title", "Ventana inutilizable" },
            { "notif.unusable.text", "Freeze Ray no pudo identificar ninguna ventana en este punto." },
            { "notif.failed.title", "Error" },
            { "notif.failed.pin", "No se pudo modificar «{0}».\nLas ventanas de aplicaciones ejecutadas como administrador exigen que Freeze Ray también lo esté." },
            { "notif.failed.topMost", "No se pudo modificar «{0}».\nUna aplicación ejecutada como administrador exige que Freeze Ray también lo esté." },
            { "notif.desktops.on", "Mantenida en todos los escritorios" },
            { "notif.desktops.off", "Ya no sigue los escritorios" },
            { "notif.topMost.on", "Siempre visible" },
            { "notif.topMost.off", "Siempre visible desactivado" },
            { "notif.released.title", "Ventana liberada" },
            { "notif.released.count", "{0} ventana(s) liberada(s)." },
            { "notif.autostart.error", "No se pudo modificar el inicio automático: {0}" },

            { "settings.title", "Configuración" },
            { "settings.general", "General" },
            { "settings.startWithWindows", "Iniciar con Windows" },
            { "settings.releaseOnExit", "Liberar todo al salir" },
            { "settings.notifications", "Mostrar notificaciones" },
            { "settings.notificationsHint", "Los errores siempre se avisan." },
            { "settings.language", "Idioma" },
            { "settings.updates", "Actualizaciones" },
            { "settings.check", "Buscar actualizaciones" },
            { "settings.checkAtStartup", "Buscar actualizaciones al iniciar" },
            { "settings.close", "Cerrar" },
            { "settings.version", "Versión {0}" },

            { "update.checking", "Buscando…" },
            { "update.upToDate", "Tienes la última versión ({0})." },
            { "update.available", "La versión {0} está disponible. ¿Abrir la página de descarga?" },
            { "update.availableTitle", "Actualización disponible" },
            { "update.availableBalloon", "La versión {0} está disponible. Haz clic para abrir la página de descarga." },
            { "update.noRelease", "Todavía no se ha publicado ninguna versión en este repositorio." },
            { "update.error", "No se pudo comprobar: {0}" },
            { "update.badResponse", "respuesta inesperada" },
        };

        private static readonly Dictionary<string, string> It = new Dictionary<string, string>
        {
            { "app.alreadyRunning", "Freeze Ray è già in esecuzione (icona nell'area di notifica)." },

            { "menu.allDesktops", "Mantieni a schermo (tutti i desktop)…" },
            { "menu.allDesktops.tip", "Fai clic qui, poi scegli la finestra da conservare quando cambi desktop." },
            { "menu.topMost", "Sempre in primo piano…" },
            { "menu.topMost.tip", "Fai clic qui, poi scegli la finestra da tenere sopra le altre." },
            { "menu.noLocked", "Nessuna finestra bloccata" },
            { "menu.locked", "Finestre bloccate ({0})" },
            { "menu.releaseTip", "Fai clic per liberare questa finestra." },
            { "menu.releaseAll", "Libera tutto" },
            { "menu.settings", "Impostazioni…" },
            { "menu.quit", "Esci" },

            { "state.both", "tutti i desktop + primo piano" },
            { "state.desktops", "tutti i desktop" },
            { "state.topMost", "primo piano" },

            { "tray.picking", "Freeze Ray — scegli una finestra (Esc per annullare)" },
            { "tray.locked", "Freeze Ray — {0} finestra/e bloccata/e" },

            { "notif.vd.title", "Desktop virtuali non disponibili" },
            { "notif.vd.text", "Impossibile raggiungere la shell di Windows: {0}" },
            { "notif.vd.unknown", "motivo sconosciuto" },
            { "notif.vd.noAnswer", "La shell non ha risposto." },
            { "notif.unusable.title", "Finestra inutilizzabile" },
            { "notif.unusable.text", "Freeze Ray non ha individuato alcuna finestra in questo punto." },
            { "notif.failed.title", "Non riuscito" },
            { "notif.failed.pin", "Impossibile modificare «{0}».\nLe finestre di applicazioni avviate come amministratore richiedono che anche Freeze Ray lo sia." },
            { "notif.failed.topMost", "Impossibile modificare «{0}».\nUn'applicazione avviata come amministratore richiede che anche Freeze Ray lo sia." },
            { "notif.desktops.on", "Mantenuta su tutti i desktop" },
            { "notif.desktops.off", "Non segue più i desktop" },
            { "notif.topMost.on", "Sempre in primo piano" },
            { "notif.topMost.off", "Primo piano disattivato" },
            { "notif.released.title", "Finestra liberata" },
            { "notif.released.count", "{0} finestra/e liberata/e." },
            { "notif.autostart.error", "Impossibile modificare l'avvio automatico: {0}" },

            { "settings.title", "Impostazioni" },
            { "settings.general", "Generale" },
            { "settings.startWithWindows", "Avvia con Windows" },
            { "settings.releaseOnExit", "Libera tutto all'uscita" },
            { "settings.notifications", "Mostra le notifiche" },
            { "settings.notificationsHint", "Gli errori vengono sempre segnalati." },
            { "settings.language", "Lingua" },
            { "settings.updates", "Aggiornamenti" },
            { "settings.check", "Cerca aggiornamenti" },
            { "settings.checkAtStartup", "Cerca aggiornamenti all'avvio" },
            { "settings.close", "Chiudi" },
            { "settings.version", "Versione {0}" },

            { "update.checking", "Ricerca in corso…" },
            { "update.upToDate", "Hai l'ultima versione ({0})." },
            { "update.available", "È disponibile la versione {0}. Aprire la pagina di download?" },
            { "update.availableTitle", "Aggiornamento disponibile" },
            { "update.availableBalloon", "È disponibile la versione {0}. Fai clic per aprire la pagina di download." },
            { "update.noRelease", "Su questo repository non è ancora stata pubblicata alcuna versione." },
            { "update.error", "Controllo non riuscito: {0}" },
            { "update.badResponse", "risposta inattesa" },
        };

        private static readonly Dictionary<string, string> Ja = new Dictionary<string, string>
        {
            { "app.alreadyRunning", "Freeze Ray はすでに実行中です（通知領域のアイコン）。" },

            { "menu.allDesktops", "画面に保持（すべてのデスクトップ）…" },
            { "menu.allDesktops.tip", "ここをクリックしてから、デスクトップを切り替えても残したいウィンドウを指定します。" },
            { "menu.topMost", "最前面に固定…" },
            { "menu.topMost.tip", "ここをクリックしてから、他より前面に保ちたいウィンドウを指定します。" },
            { "menu.noLocked", "固定中のウィンドウはありません" },
            { "menu.locked", "固定中のウィンドウ ({0})" },
            { "menu.releaseTip", "クリックするとこのウィンドウを解除します。" },
            { "menu.releaseAll", "すべて解除" },
            { "menu.settings", "設定…" },
            { "menu.quit", "終了" },

            { "state.both", "すべてのデスクトップ＋最前面" },
            { "state.desktops", "すべてのデスクトップ" },
            { "state.topMost", "最前面" },

            { "tray.picking", "Freeze Ray — ウィンドウを指定（Esc で中止）" },
            { "tray.locked", "Freeze Ray — 固定中 {0} 個" },

            { "notif.vd.title", "仮想デスクトップを利用できません" },
            { "notif.vd.text", "Windows シェルに接続できません: {0}" },
            { "notif.vd.unknown", "原因不明" },
            { "notif.vd.noAnswer", "シェルが応答しませんでした。" },
            { "notif.unusable.title", "使用できないウィンドウ" },
            { "notif.unusable.text", "この位置でウィンドウを特定できませんでした。" },
            { "notif.failed.title", "失敗" },
            { "notif.failed.pin", "「{0}」を変更できません。\n管理者として実行中のアプリのウィンドウを扱うには、Freeze Ray も管理者として実行する必要があります。" },
            { "notif.failed.topMost", "「{0}」を変更できません。\n管理者として実行中のアプリを扱うには、Freeze Ray も管理者として実行する必要があります。" },
            { "notif.desktops.on", "すべてのデスクトップに保持しました" },
            { "notif.desktops.off", "デスクトップに追随しなくなりました" },
            { "notif.topMost.on", "最前面に固定しました" },
            { "notif.topMost.off", "最前面固定を解除しました" },
            { "notif.released.title", "ウィンドウを解除しました" },
            { "notif.released.count", "{0} 個のウィンドウを解除しました。" },
            { "notif.autostart.error", "スタートアップ登録を変更できません: {0}" },

            { "settings.title", "設定" },
            { "settings.general", "全般" },
            { "settings.startWithWindows", "Windows と一緒に起動" },
            { "settings.releaseOnExit", "終了時にすべて解除" },
            { "settings.notifications", "通知を表示" },
            { "settings.notificationsHint", "エラーは常に通知されます。" },
            { "settings.language", "言語" },
            { "settings.updates", "更新" },
            { "settings.check", "更新を確認" },
            { "settings.checkAtStartup", "起動時に更新を確認" },
            { "settings.close", "閉じる" },
            { "settings.version", "バージョン {0}" },

            { "update.checking", "確認中…" },
            { "update.upToDate", "最新版です（{0}）。" },
            { "update.available", "バージョン {0} が利用できます。ダウンロードページを開きますか？" },
            { "update.availableTitle", "更新があります" },
            { "update.availableBalloon", "バージョン {0} が利用できます。クリックするとダウンロードページを開きます。" },
            { "update.noRelease", "このリポジトリにはまだリリースが公開されていません。" },
            { "update.error", "確認できませんでした: {0}" },
            { "update.badResponse", "予期しない応答" },
        };

        private static readonly Dictionary<string, string> Ko = new Dictionary<string, string>
        {
            { "app.alreadyRunning", "Freeze Ray가 이미 실행 중입니다(알림 영역 아이콘)." },

            { "menu.allDesktops", "화면에 유지(모든 데스크톱)…" },
            { "menu.allDesktops.tip", "여기를 클릭한 뒤, 데스크톱을 바꿔도 남겨 둘 창을 지정하세요." },
            { "menu.topMost", "항상 위에…" },
            { "menu.topMost.tip", "여기를 클릭한 뒤, 다른 창보다 앞에 둘 창을 지정하세요." },
            { "menu.noLocked", "고정된 창 없음" },
            { "menu.locked", "고정된 창 ({0})" },
            { "menu.releaseTip", "클릭하면 이 창을 해제합니다." },
            { "menu.releaseAll", "모두 해제" },
            { "menu.settings", "설정…" },
            { "menu.quit", "종료" },

            { "state.both", "모든 데스크톱 + 항상 위에" },
            { "state.desktops", "모든 데스크톱" },
            { "state.topMost", "항상 위에" },

            { "tray.picking", "Freeze Ray — 창을 지정하세요(Esc로 취소)" },
            { "tray.locked", "Freeze Ray — 고정된 창 {0}개" },

            { "notif.vd.title", "가상 데스크톱을 사용할 수 없음" },
            { "notif.vd.text", "Windows 셸에 연결할 수 없습니다: {0}" },
            { "notif.vd.unknown", "알 수 없는 이유" },
            { "notif.vd.noAnswer", "셸이 응답하지 않았습니다." },
            { "notif.unusable.title", "사용할 수 없는 창" },
            { "notif.unusable.text", "이 위치에서 창을 찾지 못했습니다." },
            { "notif.failed.title", "실패" },
            { "notif.failed.pin", "'{0}'을(를) 변경할 수 없습니다.\n관리자로 실행된 앱의 창을 다루려면 Freeze Ray도 관리자로 실행해야 합니다." },
            { "notif.failed.topMost", "'{0}'을(를) 변경할 수 없습니다.\n관리자로 실행된 앱을 다루려면 Freeze Ray도 관리자로 실행해야 합니다." },
            { "notif.desktops.on", "모든 데스크톱에 유지함" },
            { "notif.desktops.off", "더 이상 데스크톱을 따라가지 않음" },
            { "notif.topMost.on", "항상 위에 고정함" },
            { "notif.topMost.off", "항상 위에 고정 해제함" },
            { "notif.released.title", "창을 해제함" },
            { "notif.released.count", "창 {0}개를 해제했습니다." },
            { "notif.autostart.error", "시작 프로그램 등록을 변경할 수 없습니다: {0}" },

            { "settings.title", "설정" },
            { "settings.general", "일반" },
            { "settings.startWithWindows", "Windows 시작 시 실행" },
            { "settings.releaseOnExit", "종료할 때 모두 해제" },
            { "settings.notifications", "알림 표시" },
            { "settings.notificationsHint", "오류는 항상 알립니다." },
            { "settings.language", "언어" },
            { "settings.updates", "업데이트" },
            { "settings.check", "업데이트 확인" },
            { "settings.checkAtStartup", "시작할 때 업데이트 확인" },
            { "settings.close", "닫기" },
            { "settings.version", "버전 {0}" },

            { "update.checking", "확인 중…" },
            { "update.upToDate", "최신 버전입니다({0})." },
            { "update.available", "버전 {0}을(를) 사용할 수 있습니다. 다운로드 페이지를 열까요?" },
            { "update.availableTitle", "업데이트 있음" },
            { "update.availableBalloon", "버전 {0}을(를) 사용할 수 있습니다. 클릭하면 다운로드 페이지가 열립니다." },
            { "update.noRelease", "이 저장소에는 아직 릴리스가 없습니다." },
            { "update.error", "확인하지 못했습니다: {0}" },
            { "update.badResponse", "예상치 못한 응답" },
        };

        private static readonly Dictionary<string, string> Ru = new Dictionary<string, string>
        {
            { "app.alreadyRunning", "Freeze Ray уже запущен (значок в области уведомлений)." },

            { "menu.allDesktops", "Удерживать на экране (все рабочие столы)…" },
            { "menu.allDesktops.tip", "Щёлкните здесь, затем укажите окно, которое должно оставаться при смене рабочего стола." },
            { "menu.topMost", "Поверх остальных…" },
            { "menu.topMost.tip", "Щёлкните здесь, затем укажите окно, которое должно оставаться выше других." },
            { "menu.noLocked", "Нет закреплённых окон" },
            { "menu.locked", "Закреплённые окна ({0})" },
            { "menu.releaseTip", "Щёлкните, чтобы освободить это окно." },
            { "menu.releaseAll", "Освободить всё" },
            { "menu.settings", "Параметры…" },
            { "menu.quit", "Выход" },

            { "state.both", "все рабочие столы + поверх остальных" },
            { "state.desktops", "все рабочие столы" },
            { "state.topMost", "поверх остальных" },

            { "tray.picking", "Freeze Ray — укажите окно (Esc — отмена)" },
            { "tray.locked", "Freeze Ray — закреплено окон: {0}" },

            { "notif.vd.title", "Виртуальные рабочие столы недоступны" },
            { "notif.vd.text", "Не удалось обратиться к оболочке Windows: {0}" },
            { "notif.vd.unknown", "причина неизвестна" },
            { "notif.vd.noAnswer", "Оболочка не ответила." },
            { "notif.unusable.title", "Непригодное окно" },
            { "notif.unusable.text", "Freeze Ray не смог определить окно в этом месте." },
            { "notif.failed.title", "Не удалось" },
            { "notif.failed.pin", "Не удалось изменить «{0}».\nОкна программ, запущенных от имени администратора, требуют, чтобы и Freeze Ray был запущен так же." },
            { "notif.failed.topMost", "Не удалось изменить «{0}».\nПрограмма, запущенная от имени администратора, требует, чтобы и Freeze Ray был запущен так же." },
            { "notif.desktops.on", "Удерживается на всех рабочих столах" },
            { "notif.desktops.off", "Больше не следует за рабочими столами" },
            { "notif.topMost.on", "Поверх остальных окон" },
            { "notif.topMost.off", "Режим «поверх остальных» отключён" },
            { "notif.released.title", "Окно освобождено" },
            { "notif.released.count", "Освобождено окон: {0}." },
            { "notif.autostart.error", "Не удалось изменить автозапуск: {0}" },

            { "settings.title", "Параметры" },
            { "settings.general", "Общие" },
            { "settings.startWithWindows", "Запускать вместе с Windows" },
            { "settings.releaseOnExit", "Освобождать всё при выходе" },
            { "settings.notifications", "Показывать уведомления" },
            { "settings.notificationsHint", "Об ошибках сообщается всегда." },
            { "settings.language", "Язык" },
            { "settings.updates", "Обновления" },
            { "settings.check", "Проверить обновления" },
            { "settings.checkAtStartup", "Проверять обновления при запуске" },
            { "settings.close", "Закрыть" },
            { "settings.version", "Версия {0}" },

            { "update.checking", "Проверка…" },
            { "update.upToDate", "У вас последняя версия ({0})." },
            { "update.available", "Доступна версия {0}. Открыть страницу загрузки?" },
            { "update.availableTitle", "Доступно обновление" },
            { "update.availableBalloon", "Доступна версия {0}. Щёлкните, чтобы открыть страницу загрузки." },
            { "update.noRelease", "В этом репозитории пока нет опубликованных выпусков." },
            { "update.error", "Не удалось проверить: {0}" },
            { "update.badResponse", "неожиданный ответ" },
        };

        private static readonly Dictionary<string, string> Zh = new Dictionary<string, string>
        {
            { "app.alreadyRunning", "Freeze Ray 已在运行（通知区域中的图标）。" },

            { "menu.allDesktops", "保持在屏幕上（所有桌面）…" },
            { "menu.allDesktops.tip", "点击此项，然后指定切换桌面时要保留的窗口。" },
            { "menu.topMost", "始终置顶…" },
            { "menu.topMost.tip", "点击此项，然后指定要保持在其他窗口之上的窗口。" },
            { "menu.noLocked", "没有锁定的窗口" },
            { "menu.locked", "已锁定的窗口 ({0})" },
            { "menu.releaseTip", "点击以解除此窗口。" },
            { "menu.releaseAll", "全部解除" },
            { "menu.settings", "设置…" },
            { "menu.quit", "退出" },

            { "state.both", "所有桌面 + 置顶" },
            { "state.desktops", "所有桌面" },
            { "state.topMost", "置顶" },

            { "tray.picking", "Freeze Ray — 请指定窗口（Esc 取消）" },
            { "tray.locked", "Freeze Ray — 已锁定 {0} 个窗口" },

            { "notif.vd.title", "虚拟桌面不可用" },
            { "notif.vd.text", "无法连接 Windows 外壳：{0}" },
            { "notif.vd.unknown", "原因不明" },
            { "notif.vd.noAnswer", "外壳没有响应。" },
            { "notif.unusable.title", "无法使用的窗口" },
            { "notif.unusable.text", "Freeze Ray 未能在此处识别出窗口。" },
            { "notif.failed.title", "失败" },
            { "notif.failed.pin", "无法修改“{0}”。\n以管理员身份运行的程序窗口，要求 Freeze Ray 也以管理员身份运行。" },
            { "notif.failed.topMost", "无法修改“{0}”。\n以管理员身份运行的程序，要求 Freeze Ray 也以管理员身份运行。" },
            { "notif.desktops.on", "已保持在所有桌面" },
            { "notif.desktops.off", "不再跟随桌面" },
            { "notif.topMost.on", "已设为始终置顶" },
            { "notif.topMost.off", "已取消置顶" },
            { "notif.released.title", "窗口已解除" },
            { "notif.released.count", "已解除 {0} 个窗口。" },
            { "notif.autostart.error", "无法修改开机启动项：{0}" },

            { "settings.title", "设置" },
            { "settings.general", "常规" },
            { "settings.startWithWindows", "开机启动" },
            { "settings.releaseOnExit", "退出时全部解除" },
            { "settings.notifications", "显示通知" },
            { "settings.notificationsHint", "错误始终会提示。" },
            { "settings.language", "语言" },
            { "settings.updates", "更新" },
            { "settings.check", "检查更新" },
            { "settings.checkAtStartup", "启动时检查更新" },
            { "settings.close", "关闭" },
            { "settings.version", "版本 {0}" },

            { "update.checking", "正在检查…" },
            { "update.upToDate", "已是最新版本（{0}）。" },
            { "update.available", "版本 {0} 可用。是否打开下载页面？" },
            { "update.availableTitle", "有可用更新" },
            { "update.availableBalloon", "版本 {0} 可用。点击可打开下载页面。" },
            { "update.noRelease", "该仓库尚未发布任何版本。" },
            { "update.error", "检查失败：{0}" },
            { "update.badResponse", "意外的响应" },
        };

        /// <summary>Ordre d'affichage dans la liste déroulante des paramètres.</summary>
        private static readonly Entry[] Entries =
        {
            new Entry(Language.English, "en", "English", En),
            new Entry(Language.French, "fr", "Français", Fr),
            new Entry(Language.German, "de", "Deutsch", De),
            new Entry(Language.Spanish, "es", "Español", Es),
            new Entry(Language.Italian, "it", "Italiano", It),
            new Entry(Language.Japanese, "ja", "日本語", Ja),
            new Entry(Language.Korean, "ko", "한국어", Ko),
            new Entry(Language.Russian, "ru", "Русский", Ru),
            new Entry(Language.Chinese, "zh", "中文", Zh),
        };
    }
}
