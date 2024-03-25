using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Internationalization.Handlers
{
    public class ModList : RequestHandler
    {

        public override HttpStatusCode Get(Request r) {
            if (r.path.Length != 0) return HttpStatusCode.NotFound;

            var entries = TranslationRegistry.AllMods().Select(m => $"\"{m.Manifest.UniqueID}\":\"{m.Manifest.Name}\"");
            var data = "{"+string.Join(",",entries)+"}";
            r.content_json();
            r.write_text(data);
            return HttpStatusCode.OK;
        }
    }
}
