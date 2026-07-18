using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.JfRemote.Tasks
{
    /// <summary>
    /// Registers the still-watching activity script with the JavaScript Injector
    /// plugin at server startup. The script makes remote-control commands (and
    /// local "Start now" clicks) count as viewer activity for the web client's
    /// "Are you still watching?" prompt. The feature only exists on Jellyfin
    /// 12.0+ web clients; on older versions the script no-ops safely.
    /// Without the JavaScript Injector plugin the remote itself still works.
    /// </summary>
    public class StartupTask : IScheduledTask
    {
        private const string ScriptSuffix = "stillwatching-activity.js";

        private readonly ILogger<StartupTask> _logger;

        public StartupTask(ILogger<StartupTask> logger)
        {
            _logger = logger;
        }

        public string Name => "JF Remote Startup";

        public string Key => "JfRemoteStartup";

        public string Description => "Registers the still-watching activity script with the JavaScript Injector plugin.";

        public string Category => "JF Remote";

        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return Task.Run(RegisterClientScript, cancellationToken);
        }

        private void RegisterClientScript()
        {
            try
            {
                var injectorAssembly = AssemblyLoadContext.All
                    .SelectMany(x => x.Assemblies)
                    .FirstOrDefault(x => x.FullName?.Contains(".JavaScriptInjector", StringComparison.Ordinal) ?? false);

                if (injectorAssembly is null)
                {
                    _logger.LogWarning("JfRemote: JavaScript Injector not found - still-watching activity integration disabled.");
                    return;
                }

                var pluginInterfaceType = injectorAssembly.GetType("Jellyfin.Plugin.JavaScriptInjector.PluginInterface");
                var registerMethod = pluginInterfaceType?.GetMethod("RegisterScript", BindingFlags.Public | BindingFlags.Static);
                if (pluginInterfaceType is null || registerMethod is null)
                {
                    _logger.LogWarning("JfRemote: JavaScript Injector plugin interface not found - still-watching activity integration disabled.");
                    return;
                }

                var assembly = GetType().Assembly;
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(ScriptSuffix, StringComparison.OrdinalIgnoreCase));
                if (resourceName is null)
                {
                    _logger.LogError("JfRemote: embedded still-watching script resource missing.");
                    return;
                }

                string script;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream!))
                {
                    script = reader.ReadToEnd();
                }

                var payload = new JObject
                {
                    { "id", "jfremote-stillwatching" },
                    { "name", "JF Remote" },
                    { "script", script },
                    { "enabled", true },
                    { "requiresAuthentication", true },
                    { "pluginId", "e3d9b3a1-52c7-4b6e-9d1a-6f4a8c2b7e91" },
                    { "pluginName", "JF Remote" },
                    { "pluginVersion", assembly.GetName().Version?.ToString() ?? "1.0.0.0" }
                };

                registerMethod.Invoke(null, new object?[] { payload });
                _logger.LogInformation("JfRemote: still-watching activity script registered with JavaScript Injector.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JfRemote: failed to register still-watching activity script.");
            }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
#if JELLYFIN_10_10
            // 10.10 has no TaskTriggerInfoType enum; triggers use string constants.
            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerStartup };
#else
            yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger };
#endif
        }
    }
}
