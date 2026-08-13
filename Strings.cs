using System.Collections.Generic;
using System.Globalization;

namespace FreezeRay
{
    internal enum Language
    {
        French,
        English
    }

    /// <summary>
    /// Textes de l'interface. Une table par langue plutôt que des fichiers de
    /// ressources : l'application se compile avec le seul csc fourni par Windows,
    /// sans outil de génération de satellites.
    /// </summary>
    internal static class Strings
    {
        /// <summary>Nom du produit : jamais traduit.</summary>
        public const string AppName = "Freeze Ray";

        private static Language _current = Detect();

        public static Language Current
        {
            get { return _current; }
            set { _current = value; }
        }

        /// <summary>Langue du système au premier lancement, anglais par défaut.</summary>
        public static Language Detect()
        {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr"
                ? Language.French
                : Language.English;
        }

        public static string T(string key)
        {
            Dictionary<string, string> table = _current == Language.French ? Fr : En;
            string value;
            if (table.TryGetValue(key, out value)) return value;
            if (Fr.TryGetValue(key, out value)) return value; // repli : jamais de texte vide
            return key;
        }

        public static string T(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

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
            { "settings.updateSource", "Dépôt GitHub :" },
            { "settings.updateSourceHint", "Au format proprietaire/depot. Laissez vide si vous n'en utilisez pas." },
            { "settings.check", "Rechercher les mises à jour" },
            { "settings.close", "Fermer" },
            { "settings.version", "Version {0}" },
            { "settings.languageNote", "La langue est appliquée immédiatement." },

            { "update.checking", "Recherche en cours…" },
            { "update.upToDate", "Vous avez la dernière version ({0})." },
            { "update.available", "Version {0} disponible. Ouvrir la page de téléchargement ?" },
            { "update.availableTitle", "Mise à jour disponible" },
            { "update.noSource", "Aucun dépôt renseigné : indiquez-en un pour rechercher les mises à jour." },
            { "update.error", "Recherche impossible : {0}" },
        };

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
            { "settings.updateSource", "GitHub repository:" },
            { "settings.updateSourceHint", "In owner/repository form. Leave empty if you do not use one." },
            { "settings.check", "Check for updates" },
            { "settings.close", "Close" },
            { "settings.version", "Version {0}" },
            { "settings.languageNote", "The language is applied immediately." },

            { "update.checking", "Checking…" },
            { "update.upToDate", "You have the latest version ({0})." },
            { "update.available", "Version {0} is available. Open the download page?" },
            { "update.availableTitle", "Update available" },
            { "update.noSource", "No repository set: provide one to check for updates." },
            { "update.error", "Check failed: {0}" },
        };
    }
}
