using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Internationalization.Handlers
{
    public class TranslationFile : RequestHandler
    {
        public TranslationFile() {}

        string TranslationPath(string uniqueId, string locale) {
            var dir = TranslationRegistry.TranslationPath(uniqueId);
            if (dir == null) return null;
            return Path.Combine(dir, locale + ".json");
        }

        // Requesting translation files.
        public override HttpStatusCode Get(Request r) {
            if (r.path.Length != 2) return HttpStatusCode.Forbidden;
            var file = TranslationPath(r.path[0], r.path[1]);
            if (file==null) return HttpStatusCode.NotFound;
            try {
                var data = File.ReadAllBytes(file);
                r.content_json();
                r.write_buffer(data);
                return HttpStatusCode.OK;
            } catch {
                return HttpStatusCode.NotFound;
            }
        }

        // Saving translation files.
        public override HttpStatusCode Put(Request r) {
            if (r.path.Length != 2) return HttpStatusCode.Forbidden;
            if (!r.req.HasEntityBody) return HttpStatusCode.BadRequest;
            var file = TranslationPath(r.path[0], r.path[1]);
            if (file==null) return HttpStatusCode.NotFound;
            try {
                var stream = r.req.InputStream;
                var output = File.OpenWrite(file);
                stream.CopyTo(output);
                output.Close();
                return HttpStatusCode.NoContent;
            } catch {
                return HttpStatusCode.NotFound;
            }
        }
    }
}
