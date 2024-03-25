using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Internationalization.Handlers
{
    public class StaticHandler : RequestHandler
    {
        readonly string root;
        public StaticHandler(string root) { 
            this.root = root;
        }

        public override void handle(Request r) {
            if (r.path.Length != 1) {
                r.status(HttpStatusCode.Forbidden);
                return;
            }

            var file = Path.Combine(root, r.path[0]);
            try { 
                var data = File.ReadAllBytes(file);
                r.status(HttpStatusCode.OK);
                r.write_buffer(data);
            } catch {
                r.status(HttpStatusCode.NotFound);
            }
            return;
        }
    }
}
