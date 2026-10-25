using System.Text.RegularExpressions;
using RagnaController.Core;
using RagnaController.Profiles;
using RagnaController.Views.Dialogs;

namespace RagnaController.Core
{
    /// <summary>
    /// UX-013: Validates share-code input for the ProfileLibraryWindow download field.
    /// Mirrors <see cref="ProfileShareService"/> acceptance so the UI never enables a
    /// download for input the service would reject (no dead-end button).
    /// Accepted forms (case-sensitive, matching ProfileShareService.ResolveGistId):
    ///   1. Share code "XX-XXXX" — 2 UPPERCASE letters + '-' + 4 hex chars (e.g. GX-A3F9)
    ///   2. Raw Gist ID — 20+ hex characters
    /// </summary>
    public static class ShareCodeValidator
    {
        private static readonly Regex Pattern = new(
            @"^[A-Z]{2}-[0-9A-Fa-f]{4}$|^[0-9a-fA-F]{20,}$", RegexOptions.Compiled);

        /// <summary>Returns true when the (trimmed) input is a downloadable share code or Gist ID.</summary>
        public static bool IsValid(string? code)
            => !string.IsNullOrWhiteSpace(code) && Pattern.IsMatch(code.Trim());
    }
}
