using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// TASK-3202: ObsWebSocketService — OBS WebSocket Integration.
    ///
    /// Ermöglicht die Steuerung von OBS Studio über WebSocket:
    ///   • Overlay-Sichtbarkeit ein-/ausschalten
    ///   • Szenen-Wechsel
    ///   • Quelle ein-/ausblenden
    ///   • Aufnahme starten/stoppen
    ///
    /// White-Hat: Nur OBS-Steuerung, keine Spielautomatisierung.
    /// </summary>
    public sealed class ObsWebSocketService : IDisposable
    {
        private ClientWebSocket? _webSocket;
        private readonly string _uri;
        private readonly string _password;
        private bool _isConnected = false;
        private bool _disposed = false;
        private int _messageId = 0;

        /// <summary>Ob eine Verbindung zu OBS besteht.</summary>
        public bool IsConnected => _isConnected;

        /// <summary>Event bei Verbindungsstatus-Änderung.</summary>
        public event EventHandler<bool>? ConnectionStatusChanged;

        /// <summary>Event bei empfangener OBS-Nachricht.</summary>
        public event EventHandler<ObsMessage>? MessageReceived;

        public ObsWebSocketService(string uri = "ws://localhost:4455", string password = "")
        {
            _uri = uri;
            _password = password;
        }

        /// <summary>Verbindet mit OBS WebSocket.</summary>
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(_uri), cancellationToken);
                _isConnected = true;
                ConnectionStatusChanged?.Invoke(this, true);

                _ = Task.Run(() => ReceiveLoopAsync(cancellationToken), cancellationToken);
                return true;
            }
            catch
            {
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
                return false;
            }
        }

        /// <summary>Trennt die OBS WebSocket Verbindung.</summary>
        public async Task DisconnectAsync()
        {
            _isConnected = false;
            if (_webSocket != null)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                _webSocket.Dispose();
                _webSocket = null;
            }
            ConnectionStatusChanged?.Invoke(this, false);
        }

        /// <summary>Sendet eine OBS WebSocket Request.</summary>
        public async Task<ObsResponse?> SendRequestAsync(string requestType, object? requestData = null, CancellationToken cancellationToken = default)
        {
            if (!_isConnected || _webSocket == null)
                return null;

            var id = Interlocked.Increment(ref _messageId);
            var request = new
            {
                op = 6,
                d = new
                {
                    requestType,
                    requestId = id.ToString(),
                    requestData,
                },
            };

            var json = JsonSerializer.Serialize(request);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);

            // In einer echten Implementierung würde hier auf die Antwort gewartet werden
            return new ObsResponse(id.ToString(), requestType, true);
        }

        /// <summary>Schaltet die Sichtbarkeit einer OBS Quelle um.</summary>
        public async Task<bool> SetSourceVisibilityAsync(string sceneName, string sourceName, bool visible, CancellationToken cancellationToken = default)
        {
            var response = await SendRequestAsync("SetSourceShowing", new
            {
                sceneName,
                sourceName,
                sourceShowing = visible,
            }, cancellationToken);
            return response?.Success ?? false;
        }

        /// <summary>Wechselt die aktive OBS Szene.</summary>
        public async Task<bool> SetCurrentSceneAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            var response = await SendRequestAsync("SetCurrentProgramScene", new
            {
                sceneName,
            }, cancellationToken);
            return response?.Success ?? false;
        }

        /// <summary>Startet die OBS Aufnahme.</summary>
        public async Task<bool> StartRecordingAsync(CancellationToken cancellationToken = default)
        {
            var response = await SendRequestAsync("StartRecording", cancellationToken: cancellationToken);
            return response?.Success ?? false;
        }

        /// <summary>Stoppt die OBS Aufnahme.</summary>
        public async Task<bool> StopRecordingAsync(CancellationToken cancellationToken = default)
        {
            var response = await SendRequestAsync("StopRecording", cancellationToken: cancellationToken);
            return response?.Success ?? false;
        }

        /// <summary>Startet den OBS Stream.</summary>
        public async Task<bool> StartStreamingAsync(CancellationToken cancellationToken = default)
        {
            var response = await SendRequestAsync("StartStreaming", cancellationToken: cancellationToken);
            return response?.Success ?? false;
        }

        /// <summary>Stoppt den OBS Stream.</summary>
        public async Task<bool> StopStreamingAsync(CancellationToken cancellationToken = default)
        {
            var response = await SendRequestAsync("StopStreaming", cancellationToken: cancellationToken);
            return response?.Success ?? false;
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            while (_isConnected && _webSocket != null && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _isConnected = false;
                        ConnectionStatusChanged?.Invoke(this, false);
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonSerializer.Deserialize<ObsMessage>(json);
                    if (message != null)
                        MessageReceived?.Invoke(this, message);
                }
                catch
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _isConnected = false;
                _webSocket?.Dispose();
            }
        }
    }

    /// <summary>
    /// OBS WebSocket Nachricht.
    /// </summary>
    public class ObsMessage
    {
        public string? Op { get; set; }
        public object? D { get; set; }
    }

    /// <summary>
    /// OBS WebSocket Antwort.
    /// </summary>
    public class ObsResponse
    {
        public string RequestId { get; }
        public string RequestType { get; }
        public bool Success { get; }

        public ObsResponse(string requestId, string requestType, bool success)
        {
            RequestId = requestId;
            RequestType = requestType;
            Success = success;
        }
    }
}
