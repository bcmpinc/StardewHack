using StardewModdingAPI;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Internationalization
{
    static class ReflectionHelper {
        private static Dictionary<(System.Type, string), PropertyInfo> cache = new Dictionary<(System.Type, string), PropertyInfo>();
        public static T get_property<T>(object o, string name) {
            var main_type = o.GetType();
            var key = (main_type,name);
            if (!cache.TryGetValue(key, out var property)) {
                for (var type = main_type; type != null; type = type.BaseType) {
                    property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property != null) {
                        cache[key] = property;
                        goto found;
                    }
                }
                throw new System.Exception($"Could not find property {name} in {main_type.FullName}");
            }
            found:
            return (T)property.GetValue(o);
        }
    }

    static class TranslationRegistry {
        private struct Entry { 
            readonly public IModInfo Mod;
            readonly public ITranslationHelper Translations;

            public Entry(IModInfo mod) {
                Mod = mod;
                Translations = ReflectionHelper.get_property<ITranslationHelper>(mod, "Translations");
            }
        }

        static Dictionary<string,Entry> table;

        static public void Init(IModRegistry registry) {
            table = new Dictionary<string, Entry>();
            foreach (var i in registry.GetAll()) {
                table[i.Manifest.UniqueID] = new Entry(i);
            }
        }

        internal static IEnumerable<IModInfo> AllMods() {
            return table.Values.Select(e => e.Mod);
        }
    }
}
