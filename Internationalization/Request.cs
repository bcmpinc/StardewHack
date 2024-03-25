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
        public abstract void handle(Request req);
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
