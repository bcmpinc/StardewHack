using System.IO;
using System.Net;
using System.Text.Json;

namespace Internationalization.Handlers
{
    public class Translations : RequestHandler
    {
        public override bool Get(Request r) {
            if (r.path.Length==2) {
                var dict = TranslationRegistry.GetAll(r.path[0], r.path[1]);
                if (dict == null) return r.status(HttpStatusCode.NotFound);
                var data = JsonSerializer.Serialize(dict);
                r.content_json();
                return r.write_text(HttpStatusCode.OK, data);
            } else if (r.path.Length==2) {
                var data = TranslationRegistry.Get(r.path[0], r.path[1], r.path[2]);
                if (data == null) return r.status(HttpStatusCode.NotFound);
                r.content_text();
                return r.write_text(HttpStatusCode.OK, data);
            } else {
                return r.status(HttpStatusCode.BadRequest);
            }
        }

        public override bool Put(Request r) {
            if (r.path.Length != 3) return r.status(HttpStatusCode.BadRequest);

            var data = new StreamReader(r.req.InputStream, r.req.ContentEncoding).ReadToEnd();

            if (TranslationRegistry.Set(r.path[0], r.path[1], r.path[2], data))
                return r.status(HttpStatusCode.NoContent);
            else 
                return r.status(HttpStatusCode.BadRequest);
        }
    }
}
