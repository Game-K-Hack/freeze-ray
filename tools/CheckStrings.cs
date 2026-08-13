// Contrôle des tables de traduction, destiné à l'intégration continue.
//
// Une clé absente d'une langue ne provoque aucune erreur à l'exécution : le
// texte retombe silencieusement sur l'anglais, et le défaut passe inaperçu.
// Un emplacement « {0} » perdu à la traduction est pire encore : le nombre ou
// le titre de fenêtre disparaît de la phrase.
//
// Compilation :
//   csc.exe /nologo /codepage:65001 /out:CheckStrings.exe tools\CheckStrings.cs Strings.cs
using System;
using System.Collections.Generic;
using FreezeRay;

internal static class CheckStrings
{
    private static int Main()
    {
        Strings.Entry[] all = Strings.All;
        Strings.Entry reference = all[0]; // l'anglais fait foi

        Console.WriteLine("langues : " + all.Length);
        Console.WriteLine("clés de référence (" + reference.Code + ") : " + reference.Table.Count);

        int problems = 0;

        foreach (Strings.Entry entry in all)
        {
            List<string> missing = new List<string>();
            foreach (string key in reference.Table.Keys)
                if (!entry.Table.ContainsKey(key)) missing.Add(key);

            List<string> extra = new List<string>();
            foreach (string key in entry.Table.Keys)
                if (!reference.Table.ContainsKey(key)) extra.Add(key);

            List<string> placeholders = new List<string>();
            foreach (KeyValuePair<string, string> pair in reference.Table)
            {
                if (pair.Value.IndexOf("{0}", StringComparison.Ordinal) < 0) continue;
                string translated;
                if (entry.Table.TryGetValue(pair.Key, out translated)
                    && translated.IndexOf("{0}", StringComparison.Ordinal) < 0)
                {
                    placeholders.Add(pair.Key);
                }
            }

            int count = missing.Count + extra.Count + placeholders.Count;
            problems += count;

            Console.WriteLine("  " + entry.Code + "  " + entry.Table.Count + " clés  "
                              + (count == 0 ? "OK" : "PROBLEME"));
            foreach (string key in missing) Console.WriteLine("      manquante : " + key);
            foreach (string key in extra) Console.WriteLine("      en trop   : " + key);
            foreach (string key in placeholders) Console.WriteLine("      {0} perdu : " + key);
        }

        // Les codes doivent faire l'aller-retour, sinon un réglage enregistré se
        // relirait dans une autre langue que celle choisie.
        foreach (Strings.Entry entry in all)
        {
            if (Strings.FromCode(entry.Code) != entry.Id)
            {
                Console.WriteLine("  aller-retour impossible : " + entry.Code);
                problems++;
            }
        }

        Console.WriteLine(problems == 0
            ? "=> tables cohérentes"
            : "=> " + problems + " écart(s) détecté(s)");
        return problems == 0 ? 0 : 1;
    }
}
