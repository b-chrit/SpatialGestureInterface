// Assets/GestureReceiver.cs
using UnityEngine;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class GestureReceiver : MonoBehaviour
{
    public UIManager uiManager;

    private ClientWebSocket ws;
    private CancellationTokenSource cts;
    private readonly Uri serverUri = new Uri("ws://127.0.0.1:8765/");

    void Start()
    {
        cts = new CancellationTokenSource();
        _ = Run(); // background connection loop
    }

    private async Task Run()
    {
        while (!cts.IsCancellationRequested)
        {
            ws = new ClientWebSocket();
            try
            {
                Debug.Log("🟡 Attempting connection to Python WebSocket...");

                using (var connectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(connectTimeout.Token, cts.Token))
                {
                    await ws.ConnectAsync(serverUri, linked.Token);
                }

                Debug.Log("✅ Connected to Python WebSocket");
                await ReceiveLoop();
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ WebSocket error: {ex.GetType().Name} | {ex.Message}");
            }

            await Task.Delay(1000); // retry delay
        }
    }

    private async Task ReceiveLoop()
    {
        Debug.Log("🔵 Listening for gestures...");
        var buffer = new byte[1024];

        while (ws.State == WebSocketState.Open && !cts.IsCancellationRequested)
        {
            try
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.LogWarning($"🔴 Server closed connection: {result.CloseStatus} | {result.CloseStatusDescription}");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
                    break;
                }

                if (result.Count == 0)
                    continue;

                string msg = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();
                Debug.Log($"🎯 Gesture received: {msg}");

                UnityMainThreadDispatcher.Instance().Enqueue(() => HandleGesture(msg));
            }
            catch (Exception e)
            {
                Debug.LogError($"⚠️ Receive error: {e.GetType().Name} | {e.Message}");
                break;
            }
        }

        Debug.LogWarning($"🔴 Receive loop exited. State: {ws.State}");
    }

    private void HandleGesture(string msg)
    {
        switch (msg)
        {
            // directional swipes
            case "SWIPE_UP": uiManager.ShowMessages(); break;
            case "SWIPE_DOWN": uiManager.ShowHome(); break;
            case "SWIPE_LEFT": uiManager.ShowSettings(); break;
            case "SWIPE_RIGHT": uiManager.ShowNotifications(); break;

            // hand shapes
            case "PINCH": uiManager.ConfirmSelection(); break;
            case "OPEN_PALM": uiManager.SetDarkMode(true); break;   // enable dark mode
            case "FIST": uiManager.SetDarkMode(false); break;       // enable light mode

            default:
                Debug.Log($"⚪ Unknown gesture: {msg}");
                break;
        }
    }

    void OnDestroy()
    {
        try { cts?.Cancel(); } catch {}
        try { ws?.Dispose(); } catch {}
    }
}
