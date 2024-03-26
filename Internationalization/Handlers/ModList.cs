using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Internationalization.Handlers
{
    public class ModList : RequestHandler
    {

        struct Entry {
            [JsonInclude] public string name;
            [JsonInclude] public string current_locale;
            [JsonInclude] public string[] locales;
        }

        public override HttpStatusCode Get(Request r) {
            switch (r.path.Length) {
                case 0: {
                    var entries = new Dictionary<string, string>();
                    foreach (var m in TranslationRegistry.AllMods()) {
                        var id = m.Manifest.UniqueID;
                        entries[id] = m.Manifest.Name;
                    }
                    var data = JsonSerializer.Serialize(entries);
                    r.content_json();
                    r.write_text(data);
                    return HttpStatusCode.OK;
                }
                case 1: {
                    var id = r.path[0];
                    var m = TranslationRegistry.Mod(id);
                    var entry = new Entry() {
                        name = m.Manifest.Name,
                        current_locale = TranslationRegistry.Current(id),
                        locales = TranslationRegistry.Locales(id),
                    };
                    var data = JsonSerializer.Serialize(entry);
                    r.content_json();
                    r.write_text(data);
                    return HttpStatusCode.OK;
                }
                default:
                    return HttpStatusCode.BadRequest;
            }
        }
    }
}
