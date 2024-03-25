using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Internationalization
{
    public abstract class RequestHandler
    {
        public HttpStatusCode Handle(Request r) {
            try {
                switch (r.req.HttpMethod) {
                    case "GET": return Get(r);
                    case "PUT": return Put(r);
                    default: return HttpStatusCode.MethodNotAllowed;
                }
            } catch (System.Exception ex) {
                r.write_text(ex.ToString());
                return HttpStatusCode.InternalServerError;
            }
        }

        public virtual HttpStatusCode Get(Request r) => HttpStatusCode.MethodNotAllowed;
        public virtual HttpStatusCode Put(Request r) => HttpStatusCode.MethodNotAllowed;
    }

    public class Request
    {
        public string[] path;
        public readonly HttpListenerRequest req;
        public readonly HttpListenerResponse res;

        internal Request(HttpListenerContext ctx)
        {
            path = ctx.Request.Url.LocalPath.Split("/", StringSplitOptions.RemoveEmptyEntries); ;
            req = ctx.Request;
            res = ctx.Response;
            ModEntry.Log($"Received {req.HttpMethod} {req.Url}");
        }

        public void status(HttpStatusCode code) {
            res.StatusCode = (int)code;
            res.StatusDescription = code.ToString();
        }

        public void write_buffer(byte[] data)
        {
            res.ContentLength64 = data.Length;
            res.OutputStream.Write(data, 0, data.Length);
        }
        public void write_text(string data) => write_buffer(Encoding.UTF8.GetBytes(data));

        public void content(string type) => res.Headers.Set("Content-Type", type);
        public void content_text() => content("text/plain");
        public void content_json() => content("text/json");
    }
}
