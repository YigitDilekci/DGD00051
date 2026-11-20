using System;
using System.Collections.Generic;
using UnityEngine;
using FishNet;
using FishNet.Managing.Statistic;

/// <summary>
/// Connection HUD from course Tools.
/// Displays Start Server / Start Client buttons, connection stats, panels, and flash messages.
/// </summary>
public class ConnectionHud : MonoBehaviour
{
    public static readonly List<Func<string>> Panels = new List<Func<string>>();
    private static string flashMessage = "";
    private static float flashTimer = 0f;

    [SerializeField] private string serverAddress = "127.0.0.1";

    // Homework 3: Relay fields
    private string _joinCode = "";
    private string _relayCode = "";
    private bool _relayBusy = false;

    private async void HostViaRelay()
    {
        _relayBusy = true;
        try
        {
            _relayCode = await RelayLink.StartRelayHost(2);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Relay] Host failed: {ex.Message}");
        }
        finally
        {
            _relayBusy = false;
        }
    }

    private async void JoinViaRelay()
    {
        _relayBusy = true;
        try
        {
            await RelayLink.JoinRelay(_joinCode.Trim());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Relay] Join failed: {ex.Message}");
        }
        finally
        {
            _relayBusy = false;
        }
    }

    public static void Flash(string message)
    {
        flashMessage = message;
        flashTimer = 3.5f;
    }

    private void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                flashMessage = "";
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(15, 15, 340, Screen.height - 30));

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };

        if (InstanceFinder.NetworkManager != null)
        {
            bool serverStarted = InstanceFinder.IsServerStarted;
            bool clientStarted = InstanceFinder.IsClientStarted;

            if (!serverStarted && !clientStarted)
            {
                GUILayout.Label("Pong Network Control", headerStyle);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                GUILayout.Label("IP:", GUILayout.Width(30));
                serverAddress = GUILayout.TextField(serverAddress);
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Start Server", GUILayout.Height(30)))
                {
                    InstanceFinder.ServerManager.StartConnection();
                }

                if (GUILayout.Button("Start Host", GUILayout.Height(30)))
                {
                    InstanceFinder.ServerManager.StartConnection();
                    InstanceFinder.ClientManager.StartConnection("127.0.0.1");
                }

                if (GUILayout.Button("Start Client", GUILayout.Height(30)))
                {
                    InstanceFinder.ClientManager.StartConnection(serverAddress);
                }

                if (!_relayBusy)
                {
                    GUILayout.Space(8);
                    if (GUILayout.Button("Host via Relay", GUILayout.Height(30)))
                    {
                        HostViaRelay();
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Join Code:", GUILayout.Width(70));
                    _joinCode = GUILayout.TextField(_joinCode);
                    if (GUILayout.Button("Join via Relay", GUILayout.Height(30)))
                    {
                        JoinViaRelay();
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                if (serverStarted && clientStarted)
                {
                    if (GUILayout.Button("Stop Host", GUILayout.Height(30)))
                    {
                        _relayCode = "";
                        InstanceFinder.ClientManager.StopConnection();
                        InstanceFinder.ServerManager.StopConnection(true);
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (serverStarted && GUILayout.Button("Stop Server"))
                    {
                        _relayCode = "";
                        InstanceFinder.ServerManager.StopConnection(true);
                    }
                    if (clientStarted && GUILayout.Button("Stop Client"))
                    {
                        InstanceFinder.ClientManager.StopConnection();
                    }
                    GUILayout.EndHorizontal();
                }

                if (!string.IsNullOrEmpty(_relayCode) && serverStarted)
                {
                    GUILayout.Space(6);
                    GUIStyle bigCodeStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 18,
                        fontStyle = FontStyle.Bold
                    };
                    GUILayout.Label($"<b>JOIN CODE: {_relayCode}</b>", bigCodeStyle);
                }

                GUILayout.Space(5);
                GUILayout.Label($"Tick: {InstanceFinder.TimeManager.Tick}", headerStyle);
                GUILayout.Label($"RTT: {InstanceFinder.TimeManager.RoundTripTime} ms");
                GUILayout.Label($"Paddles (All.Count): {PaddleController.All.Count}");
            }
        }

        // Flash message banner
        if (!string.IsNullOrEmpty(flashMessage))
        {
            GUILayout.Space(10);
            GUI.color = Color.yellow;
            GUILayout.Box($"*** {flashMessage} ***", GUILayout.Height(35));
            GUI.color = Color.white;
        }

        // Custom Panels (e.g. GameState score & event feed)
        if (Panels.Count > 0)
        {
            GUILayout.Space(10);
            foreach (var panel in Panels)
            {
                string text = panel?.Invoke();
                if (!string.IsNullOrEmpty(text))
                {
                    GUILayout.Box(text, GUILayout.ExpandWidth(true));
                }
            }
        }

        GUILayout.EndArea();
    }
}
