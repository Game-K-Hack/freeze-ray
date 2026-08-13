using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace FreezeRay
{
    internal enum UpdateStatus
    {
        UpToDate,
        Available,

        /// <summary>Dépôt joignable mais sans version publiée, ou dépôt inexistant.</summary>
        NoRelease,

        Error
    }

    internal sealed class UpdateResult
    {
        public UpdateStatus Status { get; set; }
        public string LatestVersion { get; set; }
        public string PageUrl { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Recherche de mise à jour via l'API publique des versions de GitHub.
    ///
    /// L'application ne se met pas à jour toute seule : elle compare les numéros
    /// et propose d'ouvrir la page de téléchargement. Remplacer un exécutable en
    /// cours d'exécution demande un programme relais, et le faire sans
    /// signature ni vérification serait un vecteur d'attaque.
    /// </summary>
    internal static class Updater
    {
        /// <summary>
        /// Dépôt officiel du projet. Volontairement figé ici plutôt que réglable :
        /// une source modifiable serait un moyen commode de faire télécharger
        /// n'importe quoi à l'utilisateur sous le nom de l'application.
        /// </summary>
        public const string Repository = "Game-K-Hack/freeze-ray";

        public const string ReleasesUrl = "https://github.com/" + Repository + "/releases/latest";

        private const int TIMEOUT_MS = 10000;

        public static Version CurrentVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public static string CurrentVersionText
        {
            get
            {
                Version v = CurrentVersion;
                return v.Major + "." + v.Minor + "." + v.Build;
            }
        }

        /// <summary>
        /// Interroge GitHub en arrière-plan ; <paramref name="callback"/> est
        /// appelé depuis un thread de travail, à l'appelant de revenir sur
        /// l'interface.
        /// </summary>
        public static void CheckAsync(Action<UpdateResult> callback)
        {
            ThreadPool.QueueUserWorkItem(delegate { callback(Check()); });
        }

        private static UpdateResult Check()
        {
            UpdateResult result = new UpdateResult();
            result.PageUrl = ReleasesUrl;

            try
            {
                // .NET 4 négocie SSL 3 / TLS 1.0 par défaut, que GitHub refuse.
                // La valeur numérique évite de dépendre d'une énumération absente
                // des versions anciennes du framework.
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                    "https://api.github.com/repos/" + Repository + "/releases/latest");
                request.UserAgent = Strings.AppName;      // exigé par l'API GitHub
                request.Accept = "application/vnd.github+json";
                request.Timeout = TIMEOUT_MS;
                request.ReadWriteTimeout = TIMEOUT_MS;

                string json;
                using (WebResponse response = request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    json = reader.ReadToEnd();
                }

                Match match = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                if (!match.Success)
                {
                    result.Status = UpdateStatus.Error;
                    result.Error = Strings.T("update.badResponse");
                    return result;
                }

                string tag = match.Groups[1].Value;
                result.LatestVersion = tag;

                Version latest = ParseVersion(tag);
                if (latest == null)
                {
                    result.Status = UpdateStatus.Error;
                    result.Error = tag;
                    return result;
                }

                Version current = CurrentVersion;
                result.Status = latest > new Version(current.Major, current.Minor, current.Build)
                    ? UpdateStatus.Available
                    : UpdateStatus.UpToDate;
                return result;
            }
            catch (WebException ex)
            {
                // GitHub répond 404 aussi bien pour un dépôt inconnu que pour un
                // dépôt sans version publiée : les deux se disent de la même façon.
                HttpWebResponse response = ex.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    result.Status = UpdateStatus.NoRelease;
                    return result;
                }

                result.Status = UpdateStatus.Error;
                result.Error = ex.Message;
                return result;
            }
            catch (Exception ex)
            {
                result.Status = UpdateStatus.Error;
                result.Error = ex.Message;
                return result;
            }
        }

        /// <summary>Accepte « v1.2.3 », « 1.2 », « release-1.2.3 »…</summary>
        private static Version ParseVersion(string tag)
        {
            Match digits = Regex.Match(tag, @"(\d+)(?:\.(\d+))?(?:\.(\d+))?");
            if (!digits.Success) return null;

            int major = int.Parse(digits.Groups[1].Value);
            int minor = digits.Groups[2].Success ? int.Parse(digits.Groups[2].Value) : 0;
            int build = digits.Groups[3].Success ? int.Parse(digits.Groups[3].Value) : 0;
            return new Version(major, minor, build);
        }
    }
}
