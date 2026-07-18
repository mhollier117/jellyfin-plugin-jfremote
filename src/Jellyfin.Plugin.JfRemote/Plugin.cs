using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.JfRemote
{
    /// <summary>
    /// JF Remote - a standalone mobile remote-control page served at {BaseUrl}/JfRemote/.
    /// Controls whatever is playing on any controllable session: playstate, seeking with
    /// trickplay previews, media-segment skip, audio/subtitle/quality/speed, episode
    /// browsing, and a synced watch-along stream.
    /// </summary>
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
        }

        public override string Name => "JF Remote";

        public override Guid Id => Guid.Parse("e3d9b3a1-52c7-4b6e-9d1a-6f4a8c2b7e91");

        public override string Description =>
            "Standalone mobile remote UI served at /JfRemote/ - control any session from your phone.";
    }
}
