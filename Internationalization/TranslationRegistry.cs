using Microsoft.CodeAnalysis;
using StardewModdingAPI;
using StardewValley.Network.NetEvents;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Internationalization
{
    static class TranslationRegistry {
        internal struct Entry { 
            readonly public IModInfo Mod;
            readonly public ITranslationHelper Translations;
            readonly public string I18nPath;

            public Entry(IModInfo mod) {
                Mod = mod;
                Translations = ReflectionHelper.Property<ITranslationHelper>(mod, "Translations");
                I18nPath     = Path.Combine(ReflectionHelper.Property<string>(mod, "DirectoryPath"), "i18n");
            }
        }

        static Dictionary<string,Entry> table;

        static public void Init(IModRegistry registry) {
            // Sanity check to prevent issues later.
            ReflectionHelper.Constructor<Translation>("", "", ""); 

            // Create the internal table.
            table = new Dictionary<string, Entry>();
            foreach (var i in registry.GetAll()) {
                table[i.Manifest.UniqueID] = new Entry(i);
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
            if (locale == "current")
                locale = e.Translations.Locale;
            if (locale == "")
                locale = "default";
            return Path.Combine(e.I18nPath, locale + ".json");
        }

        internal static string Get(string uniqueId, string key) {
            if (!table.TryGetValue(uniqueId, out var e)) return null;
            var ForLocale = ReflectionHelper.Property<IDictionary<string, Translation>>(e, "ForLocale");
            if (ForLocale == null) return null;
            if (!ForLocale.TryGetValue(key, out var res)) return null;
            return res; // Automatically converts to text.
        }

        internal static bool Set(string uniqueId, string key, string value) {
            if (!table.TryGetValue(uniqueId, out var e)) return false;
            var ForLocale = ReflectionHelper.Property<IDictionary<string, Translation>>(e, "ForLocale");
            if (ForLocale == null) return false;
            if (!ForLocale.ContainsKey(key)) return false;
            ForLocale[key] = ReflectionHelper.Constructor<Translation>(e.Translations.Locale, key, value);
            return true;
        }
    }
}
