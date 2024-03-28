using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Internationalization.Handlers
{
    public class Translations : RequestHandler
    {
        public override HttpStatusCode Get(Request r) {
            if (r.path.Length==2) {
                var dict = TranslationRegistry.GetAll(r.path[0], r.path[1]);
                var data = JsonSerializer.Serialize(dict);
                r.content_json();
                r.write_text(data);
                return HttpStatusCode.OK;
            } else if (r.path.Length==2) {
                var data = TranslationRegistry.Get(r.path[0], r.path[1], r.path[2]);
                r.content_text();
                r.write_text(data);
                return HttpStatusCode.OK;
            } else {
                return HttpStatusCode.BadRequest;
            }
        }

        public override HttpStatusCode Put(Request r) {
            if (r.path.Length != 3) return HttpStatusCode.BadRequest;

            var data = new StreamReader(r.req.InputStream, r.req.ContentEncoding).ReadToEnd();

            if (TranslationRegistry.Set(r.path[0], r.path[1], r.path[2], data))
                return HttpStatusCode.NoContent;
            else 
                return HttpStatusCode.BadRequest;
        }
    }
}
