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

    internal class TranslationRegistry
    {
        struct Entry { 
            readonly public IModInfo Mod;
            public ITranslationHelper Translations;

            public Entry(IModInfo mod) {
                Mod = mod;
                Translations = ReflectionHelper.get_property<ITranslationHelper>(mod, "Translations");
            }
        }

        Dictionary<string,Entry> table = new Dictionary<string, Entry>();

        public TranslationRegistry(IModRegistry registry) {
            var all_mods = registry.GetAll();
            foreach (var i in all_mods) {
                table[i.Manifest.UniqueID] = new Entry(i);
            }
        }
    }
}
