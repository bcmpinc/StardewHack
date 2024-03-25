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

        public override HttpStatusCode Get(Request r) {
            if (r.path.Length != 1) return HttpStatusCode.Forbidden;

            var file = Path.Combine(root, r.path[0]);
            try { 
                var data = File.ReadAllBytes(file);
                r.write_buffer(data);
                return HttpStatusCode.OK;
            } catch {
                return HttpStatusCode.NotFound;
            }
        }
    }
}
