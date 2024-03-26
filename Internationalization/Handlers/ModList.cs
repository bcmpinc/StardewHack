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
            [JsonInclude] public string[] locales;
        }

        public override HttpStatusCode Get(Request r) {
            if (r.path.Length != 0) return HttpStatusCode.BadRequest;
            var entries = new Dictionary<string, Entry>();
            foreach (var m in TranslationRegistry.AllMods()) {
                var id = m.Manifest.UniqueID;
                entries[id] = new Entry() {
                    name = m.Manifest.Name,
                    locales = TranslationRegistry.Locales(id),
                };
            };
            var data = JsonSerializer.Serialize(entries);
            r.content_json();
            r.write_text(data);
            return HttpStatusCode.OK;
        }
    }
}
