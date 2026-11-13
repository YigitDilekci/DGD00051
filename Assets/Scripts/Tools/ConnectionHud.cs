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
            }
            else
            {
                if (serverStarted && clientStarted)
                {
                    if (GUILayout.Button("Stop Host", GUILayout.Height(30)))
                    {
                        InstanceFinder.ClientManager.StopConnection();
                        InstanceFinder.ServerManager.StopConnection(true);
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (serverStarted && GUILayout.Button("Stop Server"))
                    {
                        InstanceFinder.ServerManager.StopConnection(true);
                    }
                    if (clientStarted && GUILayout.Button("Stop Client"))
                    {
                        InstanceFinder.ClientManager.StopConnection();
                    }
                    GUILayout.EndHorizontal();
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
