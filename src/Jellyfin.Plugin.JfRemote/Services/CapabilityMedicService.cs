using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JfRemote.Services
{
    /// <summary>
    /// Restores lost session capabilities after a server restart.
    ///
    /// The web client posts its capabilities exactly once, at page load. A tab
    /// that survives a server restart reconnects its WebSocket but never
    /// re-posts them, so the server sees an active session with EMPTY
    /// capabilities and silently refuses to relay any remote-control command
    /// to it (pause, PlayNow, ...) until the page is fully reloaded. This
    /// service detects that state - an active session from a known
    /// remote-controllable client with no capabilities - and re-registers the
    /// client's standard capability set server-side.
    /// </summary>
    public class CapabilityMedicService : IHostedService, IDisposable
    {
        private static readonly string[] KnownControllableClients = { "Jellyfin Web" };

        private static readonly GeneralCommandType[] WebCommands =
        {
            GeneralCommandType.MoveUp, GeneralCommandType.MoveDown, GeneralCommandType.MoveLeft,
            GeneralCommandType.MoveRight, GeneralCommandType.PageUp, GeneralCommandType.PageDown,
            GeneralCommandType.PreviousLetter, GeneralCommandType.NextLetter, GeneralCommandType.ToggleOsd,
            GeneralCommandType.ToggleContextMenu, GeneralCommandType.Select, GeneralCommandType.Back,
            GeneralCommandType.SendKey, GeneralCommandType.SendString, GeneralCommandType.GoHome,
            GeneralCommandType.GoToSettings, GeneralCommandType.VolumeUp, GeneralCommandType.VolumeDown,
            GeneralCommandType.Mute, GeneralCommandType.Unmute, GeneralCommandType.ToggleMute,
            GeneralCommandType.SetVolume, GeneralCommandType.SetAudioStreamIndex,
            GeneralCommandType.SetSubtitleStreamIndex, GeneralCommandType.DisplayContent,
            GeneralCommandType.GoToSearch, GeneralCommandType.DisplayMessage, GeneralCommandType.SetRepeatMode,
            GeneralCommandType.SetShuffleQueue, GeneralCommandType.ChannelUp, GeneralCommandType.ChannelDown,
            GeneralCommandType.PlayMediaSource, GeneralCommandType.PlayTrailers
        };

        private readonly ISessionManager _sessionManager;
        private readonly ILogger<CapabilityMedicService> _logger;
        private Timer? _timer;

        public CapabilityMedicService(ISessionManager sessionManager, ILogger<CapabilityMedicService> logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(_ => Sweep(), null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void Sweep()
        {
            try
            {
                foreach (var session in _sessionManager.Sessions.ToList())
                {
                    if (!KnownControllableClients.Contains(session.Client, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!session.IsActive)
                    {
                        continue;
                    }

                    var caps = session.Capabilities;
                    if (caps is not null && caps.SupportsMediaControl)
                    {
                        continue;
                    }

                    _sessionManager.ReportCapabilities(session.Id, new ClientCapabilities
                    {
                        PlayableMediaTypes = new[] { MediaType.Audio, MediaType.Video },
                        SupportedCommands = WebCommands,
                        SupportsMediaControl = true,
                        SupportsPersistentIdentifier = false
                    });

                    _logger.LogInformation(
                        "JfRemote capability medic: restored lost capabilities for {Client} session {Session} (user {User}) - reconnected after a server restart without re-posting them.",
                        session.Client,
                        session.Id,
                        session.UserName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "JfRemote capability medic sweep failed.");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
