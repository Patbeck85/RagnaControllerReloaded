using System.Reflection;

namespace RagnaController.Core
{
    /// <summary>
    /// Zentrale Versionsquelle — liest exakt den &lt;Version&gt;-Tag aus der .csproj.
    /// Use only <c>AppVersion.Current</c> everywhere in code.
    /// Update version number: only needed in RagnaController.csproj.
    /// </summary>
    public static class AppVersion
    {
        public static string Current { get; }

        static AppVersion()
        {
            // AssemblyInformationalVersionAttribute contains <Version> exactly as in .csproj
            var attr = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            if (attr != null && !string.IsNullOrEmpty(attr.InformationalVersion))
            {
                // .NET 8 appends Git commit hash: "1.6.2+a1b2c3" → "1.6.2"
                string v = attr.InformationalVersion;
                int plus = v.IndexOf('+');
                Current  = plus > 0 ? v[..plus] : v;
            }
            else
            {
                // Absoluter Fallback about AssemblyName
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                Current  = ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "2.0";
            }
        }
    }
}
