using System;

namespace RagnaController.Models
{
    /// <summary>
    /// Informationen zu einem verfügbaren Update von der GitHub Releases API.
    /// </summary>
    public class UpdateInfo
    {
        /// <summary>Versions-String (z.B. "v2.5.1").</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>Release-Titel.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Release-Notes (Markdown).</summary>
        public string ReleaseNotes { get; set; } = string.Empty;

        /// <summary>Download-URL für das ZIP-Asset.</summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>Veröffentlichungsdatum.</summary>
        public DateTime PublishedAt { get; set; }

        /// <summary>Ob das Update zwingend empfohlen wird.</summary>
        public bool IsMandatory { get; set; }

        /// <summary>Ob eine neuere Version als die aktuelle verfügbar ist.</summary>
        public bool IsNewerThan(string currentVersion)
        {
            if (string.IsNullOrWhiteSpace(Version) || string.IsNullOrWhiteSpace(currentVersion))
                return false;

            try
            {
                var vNew = new Version(Version.TrimStart('v'));
                var vCur = new Version(currentVersion.TrimStart('v'));
                return vNew > vCur;
            }
            catch
            {
                return false;
            }
        }
    }
}
