using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Internationalization.Handlers
{
    public class Images : RequestHandler
    {
        readonly IGameContentHelper content;
        public Images(IGameContentHelper content) { 
            this.content = content;
        }

        public override HttpStatusCode Get(Request r) {
            try {
                var assetName = string.Join("\\", r.path);
                var res = content.Load<Texture2D>(assetName);
                r.content("image/png");
                res.SaveAsPng(r.res.OutputStream, res.Width, res.Height);
                return HttpStatusCode.OK;
            } catch {
                return HttpStatusCode.NotFound;
            }
        }
    }
}
