using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RagnaController.Models;

namespace RagnaController.Core
{
    public static class TelemetryService
    {
        // The developer will replace this with their actual Discord Webhook URL
        private const string WebhookUrl = "YOUR_WEBHOOK_URL_HERE";
        
        private static readonly HttpClient _http;
        private static Settings? _settings;

        static TelemetryService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        public static void Initialize(Settings settings)
        {
            _settings = settings;
        }

        public static void SendAppStartPing()
        {
            if (_settings == null || !_settings.EnableTelemetry) return;
            if (WebhookUrl == "YOUR_WEBHOOK_URL_HERE") return;

#pragma warning disable CS0162 // Unreachable code - fire-and-forget telemetry
            // Fire-and-forget telemetry ping - ignore failures silently
            _ = Task.Run(async () =>
            {
                try
                {
                    string os = Environment.OSVersion.VersionString;
                    string payload = $$"""
                    {
                        "content": null,
                        "embeds": [{
                            "title": "🚀 App Started",
                            "color": 4038254,
                            "fields": [
                                { "name": "Version", "value": "{{AppVersion.Current}}", "inline": true },
                                { "name": "OS", "value": "{{os}}", "inline": true },
                                { "name": "Language", "value": "{{_settings.AppLanguage}}", "inline": true }
                            ]
                        }]
                    }
                    """;

                    var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    await _http.PostAsync(WebhookUrl, content);
                }
                catch { /* Ignore telemetry failures silently */ }
            });
#pragma warning restore CS0162
        }

        public static void Dispose()
        {
            _http?.Dispose();
        }

        public static void SendCrashReport(string source, Exception ex)
        {
            if (_settings == null || !_settings.EnableTelemetry) return;
            if (WebhookUrl == "YOUR_WEBHOOK_URL_HERE") return;

#pragma warning disable CS0162 // Unreachable code - fire-and-forget crash report
            // Fire-and-forget crash report - ignore failures silently before app dies
            try
            {
                string safeStackTrace = ex.StackTrace != null 
                    ? (ex.StackTrace.Length > 1000 ? ex.StackTrace[..1000] + "..." : ex.StackTrace) 
                    : "No StackTrace";

                string payload = $$"""
                {
                    "content": "<@&YOUR_DISCORD_ROLE_ID_OPTIONAL>",
                    "embeds": [{
                        "title": "❌ CRASH REPORT",
                        "description": "An unhandled exception occurred in RagnaController.",
                        "color": 16726582,
                        "fields": [
                            { "name": "Source", "value": "{{source}}", "inline": true },
                            { "name": "Version", "value": "{{AppVersion.Current}}", "inline": true },
                            { "name": "Message", "value": "{{ex.Message.Replace("\"", "\\\"")}}" },
                            { "name": "StackTrace", "value": "```csharp\n{{safeStackTrace.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n")}}\n```" }
                        ]
                    }]
                }
                """;

                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                // Wait briefly for the request to fire before the OS kills the process
                _http.PostAsync(WebhookUrl, content).Wait(2000);
            }
            catch { }
#pragma warning restore CS0162
        }
    }
}
