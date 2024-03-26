using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley.Network.NetEvents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Internationalization
{
    static class TranslationRegistry {
        internal struct Entry { 
            readonly public IModInfo Mod;
            readonly public ITranslationHelper Translations;
            readonly public string I18nPath;
            readonly private object Translator;
            readonly public IDictionary<string, IDictionary<string, string>> All;

            internal Entry(IModInfo mod) {
                Mod = mod;
                Translations = ReflectionHelper.Property<ITranslationHelper>(mod, "Translations");
                I18nPath     = Path.Combine(ReflectionHelper.Property<string>(mod, "DirectoryPath"), "i18n");
                Translator   = ReflectionHelper.Field<object>(Translations, "Translator");
                All          = ReflectionHelper.Field<IDictionary<string, IDictionary<string, string>>>(Translator, "All");
            }

            private readonly IDictionary<string, Translation> ForLocale {get => ReflectionHelper.Field<IDictionary<string, Translation>>(Translator , "ForLocale"); }
            internal bool HasKey(string key) {
                return ForLocale.ContainsKey(key);
            }

            internal void UpdateKey(string key) {
                string text = ReflectionHelper.Method<string>(Translator, "GetRaw", key, Translations.Locale, true);
                if (text != null)
                    ForLocale[key] = ReflectionHelper.Constructor<Translation>(Translations.Locale, key, text);
            }

            internal string Current() {
                var res = Translations.Locale;
                if (res == "") return "default";
                return res;
            }
        }

        static Translation NewTranslation(string locale, string key, string text) {
            return ReflectionHelper.Constructor<Translation>(locale, key, text); 
        }

        static Dictionary<string,Entry> table;

        static public void Init(IModRegistry registry) {
            // Make sure we can make new translations to prevent issues later.
            NewTranslation("", "", ""); 

            // Create the internal table.
            table = new Dictionary<string, Entry>();
            foreach (var i in registry.GetAll()) {
                var ent = new Entry(i);
                if (ent.All.Count > 0) {
                    table[i.Manifest.UniqueID] = ent;
                }
            }
        }

        internal static IEnumerable<IModInfo> AllMods() {
            return table.Values.Select(e => e.Mod);
        }

        internal static IModInfo Mod(string uniqueId) {
            if (!table.TryGetValue(uniqueId, out var e)) return null;
            return e.Mod;
        }

        internal static string TranslationPath(string uniqueId, string locale) {
            if (!table.TryGetValue(uniqueId, out var e)) return null;
            return Path.Combine(e.I18nPath, locale + ".json");
        }
        
        internal static object GetAll(string uniqueId, string locale) {
            if (!table.TryGetValue(uniqueId, out var e)) return null;
            if (!e.All.TryGetValue(locale, out var dict)) return null;
            return dict;
        }


        internal static string Get(string uniqueId, string locale, string key) {
            if (!table.TryGetValue(uniqueId, out var e)) return null;
            if (!e.All.TryGetValue(locale, out var dict)) return null;
            if (!dict .TryGetValue(key, out var res)) return null;
            return res;
        }

        internal static bool Set(string uniqueId, string locale, string key, string value) {
            if (!table.TryGetValue(uniqueId, out var e)) return false;
            
            // Make sure this key exists 
            if (!e.HasKey(key)) return false;

            // Create locale if it does not yet exist.
            if (!e.All.TryGetValue(locale, out var dict)) {
                dict = new Dictionary<string, string>();
                e.All[locale] = dict;
            }
            
            // Set new value & update current localization
            dict[key] = value;
            e.UpdateKey(key);
            return true;
        }

        internal static string Current(string uniqueId) {
            if (!table.TryGetValue(uniqueId, out var e)) return null;
            return e.Current();
        }

        internal static string[] Locales(string uniqueId) {
            if (!table.TryGetValue(uniqueId, out var e)) return null;
            return e.All.Keys.ToArray();                        
        }
    }
}
