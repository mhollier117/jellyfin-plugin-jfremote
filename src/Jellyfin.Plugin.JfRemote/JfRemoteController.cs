using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.JfRemote
{
    /// <summary>
    /// Serves the remote page and its icon from embedded resources.
    /// Anonymous by design: the page performs its own Jellyfin login
    /// (the same model as the stock /web/ client shell).
    /// </summary>
    [ApiController]
    [Route("JfRemote")]
    public class JfRemoteController : ControllerBase
    {
        private static readonly byte[] _html = LoadResource("remote.html");
        private static readonly byte[] _icon = LoadResource("remote-icon.png");

        private static byte[] LoadResource(string suffix)
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = asm.GetManifestResourceNames()
                .First(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            using var stream = asm.GetManifestResourceStream(name);
            using var ms = new MemoryStream();
            stream!.CopyTo(ms);
            return ms.ToArray();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            // Relative asset URLs (icon) need a trailing slash to resolve under /JfRemote/.
            var path = Request.Path.Value ?? string.Empty;
            if (!path.EndsWith("/", StringComparison.Ordinal))
            {
                return Redirect(Request.PathBase + path + "/");
            }

            return File(_html, "text/html; charset=utf-8");
        }

        [HttpGet("remote-icon.png")]
        [AllowAnonymous]
        public IActionResult Icon() => File(_icon, "image/png");
    }
}
