using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Internationalization
{
    static class ReflectionHelper {
        private static Dictionary<(System.Type, string), PropertyInfo> cache_property = new Dictionary<(System.Type, string), PropertyInfo>();
        private static Dictionary<(System.Type, ImmutableArray<System.Type>), ConstructorInfo> cache_constructor = new Dictionary<(System.Type, ImmutableArray<System.Type>), ConstructorInfo>();
        public static T Property<T>(object o, string name) {
            var main_type = o.GetType();
            var key = (main_type,name);
            if (!cache_property.TryGetValue(key, out var property)) {
                for (var type = main_type; type != null; type = type.BaseType) {
                    property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property != null) {
                        cache_property[key] = property;
                        goto found;
                    }
                }
                throw new System.Exception($"Could not find property {name} in {main_type.FullName}");
            }
            found:
            return (T)property.GetValue(o);
        }

        public static T Constructor<T>(params object[] args) {
            var types = args.Select(x=>x.GetType()).ToArray();
            var key = (typeof(T), types.ToImmutableArray());
            if (!cache_constructor.TryGetValue(key, out var method)) {
                method = typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, types);
                if (method != null) {
                    cache_constructor[key] = method;
                    goto found;
                }
                throw new System.Exception($"Could not find constructor for {typeof(T)}");
            }
            found:
            return (T)method.Invoke(args);
        }
    }
}
