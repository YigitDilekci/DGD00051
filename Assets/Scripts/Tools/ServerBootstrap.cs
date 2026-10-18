using UnityEngine;
using FishNet;

/// <summary>
/// ServerBootstrap tool to automatically start server under headless or dedicated mode.
/// </summary>
public class ServerBootstrap : MonoBehaviour
{
    [SerializeField] private bool startOnHeadless = false;

    private void Start()
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            if (startOnHeadless && InstanceFinder.ServerManager != null)
            {
                InstanceFinder.ServerManager.StartConnection();
            }
        }
    }
}
