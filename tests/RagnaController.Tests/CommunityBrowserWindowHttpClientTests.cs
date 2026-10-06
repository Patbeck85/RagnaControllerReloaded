using System.Net.Http;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// TECH-018: CommunityBrowserWindow statischer HttpClient — Singleton-Semantik.
    /// Der Client ist application-lifetime (wird NICHT beim Fenster-Close disposed,
    /// sondern erst beim App-Shutdown via DisposeRegistryClient). Diese Tests prüfen
    /// die Wiederverwendbarkeit ohne ObjectDisposedException.
    /// </summary>
    public class CommunityBrowserWindowHttpClientTests
    {
        [Fact]
        public void RegistryHttpClient_IsNotNull_AndConfigured()
        {
            var client = CommunityBrowserWindow.RegistryHttpClient;

            Assert.NotNull(client);
            // Timeout-Konfiguration aus static ctor muss erhalten sein
            Assert.Equal(TimeSpan.FromSeconds(10), client.Timeout);
        }

        [Fact]
        public void RegistryHttpClient_IsSingleton_SameInstanceAcrossAccesses()
        {
            // Singleton-Garantie: Wiederveröffnung des Fensters MUSS denselben Client sehen,
            // sonst ObjectDisposedException (das ursprüngliche Bug-Szenario).
            var first = CommunityBrowserWindow.RegistryHttpClient;
            var second = CommunityBrowserWindow.RegistryHttpClient;

            Assert.Same(first, second);
        }

        [Fact]
        public void RegistryHttpClient_IsUsable_AfterMultipleAccesses_NoObjectDisposed()
        {
            // Liveness-Probe: HttpClient-Werteigenschaften werfen ObjectDisposedException,
            // wenn der Client bereits disposed wurde. Mehrfacher Zugriff + Property-Lesezugriff
            // muss fehlerfrei bleiben (Wiederverwendung ohne Dispose).
            for (int i = 0; i < 5; i++)
            {
                var client = CommunityBrowserWindow.RegistryHttpClient;
                Assert.NotNull(client);
                Assert.Equal(TimeSpan.FromSeconds(10), client.Timeout);
            }
        }

        [Fact]
        public void DisposeRegistryClient_MethodExists_AsShutdownHook()
        {
            // API-Vertrag: public static Dispose-Methode für App-Shutdown muss existieren
            // (wird von App.OnExit aufgerufen — wird hier NICHT aufgerufen, da der
            //  Singleton danach für andere Tests unbrauchbar wäre).
            var method = typeof(CommunityBrowserWindow)
                .GetMethod("DisposeRegistryClient", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.NotNull(method);
        }
    }
}
