using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Internationalization.Handlers
{
    public class StaticHandler : RequestHandler
    {
        static public Dictionary<string,string> Mime = new Dictionary<string, string> {
            { ".css",  "text/css" },
            { ".html", "text/html" },
            { ".js",   "text/javascript" },
            { ".json", "application/json" },
            { ".png",  "application/png" },
        };

        readonly string root;
        public StaticHandler(string root) { 
            this.root = root;
        }

        public override bool Get(Request r) {
            if (r.path.Length != 1) return r.status(HttpStatusCode.Forbidden);

            var file = Path.Combine(root, r.path[0]);
            try {
                var ext = Path.GetExtension(r.path[0]);
                if (Mime.TryGetValue(ext, out var mime)) r.content(mime);
                var data = File.ReadAllBytes(file);
                if (data.Length > 0) {
                    return r.write_buffer(HttpStatusCode.OK, data);
                }
            } catch {
            }
            return r.status(HttpStatusCode.NotFound);
        }

    }
}
