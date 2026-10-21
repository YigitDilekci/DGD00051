using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;

/// <summary>
/// Network paddle controller.
/// Step 2: Input up, position down. Server decides paddle position.
/// </summary>
public class PaddleController : NetworkBehaviour
{
    public static readonly List<PaddleController> All = new List<PaddleController>();
    public static PaddleController Local { get; private set; }

    [SerializeField] private float speed = 10f;

    // 0 = LEFT, 1 = RIGHT, 2 = Spectator
    private const int Spectator = 2;
    public readonly SyncVar<int> Side = new SyncVar<int>(2);

    private Renderer paddleRenderer;

    // Step 2: server-only memory — last input the server accepted
    private float _serverAxis;

    private void Awake()
    {
        paddleRenderer = GetComponent<Renderer>();
        Side.OnChange += OnSideChange;
    }

    private void OnDestroy()
    {
        Side.OnChange -= OnSideChange;
    }

    public override void OnStartNetwork()
    {
        TimeManager.OnTick += OnTick;
    }

    public override void OnStopNetwork()
    {
        TimeManager.OnTick -= OnTick;
    }

    public override void OnStartServer()
    {
        All.Add(this);

        bool leftTaken = false;
        bool rightTaken = false;

        foreach (var paddle in All)
        {
            if (paddle == this) continue;
            if (paddle.Side.Value == 0) leftTaken = true;
            if (paddle.Side.Value == 1) rightTaken = true;
        }

        if (!leftTaken)
        {
            Side.Value = 0; // LEFT
        }
        else if (!rightTaken)
        {
            Side.Value = 1; // RIGHT
        }
        else
        {
            Side.Value = Spectator; // spectator
        }

        Debug.Log($"[Server] Assigned side {Side.Value} to client {OwnerId}");
    }

    public override void OnStopServer()
    {
        All.Remove(this);
    }

    public override void OnStartClient()
    {
        All.Add(this);

        if (IsOwner)
        {
            Local = this;
            Debug.Log($"I am client {OwnerId}, side {Side.Value}");
            Place();
        }
    }

    public override void OnStopClient()
    {
        All.Remove(this);
        if (Local == this) Local = null;
    }

    private void OnSideChange(int prev, int next, bool asServer)
    {
        Place();
    }

    public void Place()
    {
        if (paddleRenderer == null)
            paddleRenderer = GetComponent<Renderer>();

        if (Side.Value == 0) // LEFT
        {
            transform.position = new Vector3(-Court.PaddleX, transform.position.y, transform.position.z);
            if (paddleRenderer != null)
            {
                paddleRenderer.enabled = true;
                paddleRenderer.material.color = Color.blue;
            }
        }
        else if (Side.Value == 1) // RIGHT
        {
            transform.position = new Vector3(Court.PaddleX, transform.position.y, transform.position.z);
            if (paddleRenderer != null)
            {
                paddleRenderer.enabled = true;
                paddleRenderer.material.color = new Color(1f, 0.5f, 0f); // orange
            }
        }
        else // spectator
        {
            if (paddleRenderer != null)
            {
                paddleRenderer.enabled = false;
            }
        }
    }

    private void Update()
    {
        // First line: owner guard
        if (!IsOwner) return;

        // Space pressed to request serve
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RequestServe();
        }
    }

    private void OnTick()
    {
        // 1. Owner sends
        if (IsOwner && Side.Value != Spectator)
        {
            float axis = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) axis += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) axis -= 1f;
            ServerSetAxis(axis);
        }

        // 2. Server moves
        if (IsServerInitialized)
        {
            transform.position += Vector3.forward * _serverAxis * speed * (float)TimeManager.TickDelta;
            Court.ClampPaddle(transform);
        }
    }

    [ServerRpc]
    private void ServerSetAxis(float axis)
    {
        _serverAxis = axis;
    }

    [ServerRpc]
    public void RequestServe()
    {
        if (GameState.Instance == null || NetworkBall.Instance == null) return;
        if (Side.Value != GameState.Instance.ServingSide.Value) return;
        if (NetworkBall.Instance.InPlay.Value) return;

        NetworkBall.Instance.Serve(1 - Side.Value);
    }

    [ObserversRpc]
    public void AnnounceGoal(int left, int right)
    {
        ConnectionHud.Flash($"Goal! {left} : {right}");
    }

    [TargetRpc]
    public void YouServe(NetworkConnection target)
    {
        ConnectionHud.Flash("YOU SERVE");
    }
}
