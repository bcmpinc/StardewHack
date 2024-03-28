using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Internationalization.Handlers
{
    public class Info : RequestHandler
    {
        private readonly ITranslationHelper translation;

        struct InfoEntry {
            [JsonInclude] public Dictionary<string, ModEntry> mods;
            [JsonInclude] public Dictionary<string, LocaleEntry> locales;
            [JsonInclude] public string current_locale;
            public InfoEntry(string current_locale) {
                mods = new Dictionary<string, ModEntry>();
                locales = new Dictionary<string, LocaleEntry>();
                this.current_locale = current_locale;
            }
        }

        struct ModEntry {
            [JsonInclude] public string name;
            [JsonInclude] public Dictionary<string,TranslationStatus> locales;
            [JsonInclude] public int lines_total;
            public ModEntry(string name) { 
                this.name = name;
                locales = new Dictionary<string, TranslationStatus>();
                lines_total = 0;
            }
        }

        struct LocaleEntry {
            [JsonInclude] public string language_id;
        }

        public Info(ITranslationHelper translation) {
            this.translation = translation;
        }

        public override bool Get(Request r) {
            if (r.path.Length == 0) {
                string current_locale = translation.Locale.Length > 0 ? translation.Locale.Split("-")[0] : "default";
                InfoEntry info = new InfoEntry(current_locale);
                foreach (var m in TranslationRegistry.AllMods()) {
                    var id = m.Manifest.UniqueID;
                    var mod = new ModEntry(m.Manifest.Name);
                    foreach (var locale in TranslationRegistry.Locales(id)) {
                        mod.locales[locale] = TranslationRegistry.Status(id,locale);
                    }
                    mod.lines_total = mod.locales["default"].lines_translated;
                    info.mods[m.Manifest.UniqueID] = mod;
                }
                DataLoader.AdditionalLanguages(Game1.content);
                foreach (var m in System.Enum.GetNames(typeof(LocalizedContentManager.LanguageCode))) {
                    info.locales[m] = new LocaleEntry();
                }
                foreach (var m in DataLoader.AdditionalLanguages(Game1.content)) {
                    info.locales[m.LanguageCode] = new LocaleEntry() {language_id = m.Id};
                }
                var data = JsonSerializer.Serialize(info);
                r.content_json();
                return r.write_text(HttpStatusCode.OK, data);
            } else {
                return r.status(HttpStatusCode.BadRequest);
            }
        }
    }
}
