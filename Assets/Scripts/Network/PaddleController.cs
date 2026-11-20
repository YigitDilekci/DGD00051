// Pong — the networked paddle, Homework 3 version (The Host Cheats, and the Join Code).
// Combines Homework 2's server-authoritative movement & rate limits with Week 5 listen-server hosting.
// Keys C, X, T, G: cheat tests (rate limit spam, speed clamp, teleport snapback, SyncVar direct write).
// Space: rate-limited serve request (1 per second, TargetRpc "TOO FAST").
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleController : NetworkBehaviour
{
    public const int Left = 0, Right = 1, Spectator = 2;

    /// The paddle this machine controls (null on a headless server, or before spawn).
    public static PaddleController Local { get; private set; }

    /// Every paddle this process knows about, in spawn order (server: all; client: all it observes).
    public static readonly List<PaddleController> All = new List<PaddleController>();

    private static readonly Dictionary<int, PaddleController> ByOwner = new Dictionary<int, PaddleController>();   // server-side lookup

    [SerializeField] private float speed      = 8f;
    [SerializeField] private Color leftColor  = new Color(0.35f, 0.65f, 1f);
    [SerializeField] private Color rightColor = new Color(1f, 0.60f, 0.25f);

    // ---- replicated state: server writes, everyone reads ----
    public readonly SyncVar<int> Side = new SyncVar<int>(Spectator);

    // ---- server-only state for authoritative movement ----
    private float _serverAxis;

    // Homework 2: Input rate limit (30 ms)
    private const double MinInputGap = 0.030;
    private double _lastInputTime = -1;
    public int RejectedInputs;

    // Homework 3 (Step 2): Serve rate limit (1.0 s)
    private const double MinServeGap = 1.0;
    private double _lastServeTime = -1;

    private void Awake()
    {
        Side.OnChange += (prev, next, asServer) => Place();
    }

    public override void OnStartNetwork()
    {
        TimeManager.OnTick += OnTick;
    }

    public override void OnStopNetwork()
    {
        TimeManager.OnTick -= OnTick;
    }

    // ---- lifecycle callbacks ----

    public override void OnStartServer()
    {
        // Guarded: a host runs OnStartServer AND OnStartClient on the same object
        if (!All.Contains(this)) All.Add(this);
        ByOwner[OwnerId] = this;

        // The server decides the side: first free of Left, Right; otherwise Spectator.
        bool leftTaken = false, rightTaken = false;
        foreach (PaddleController p in All)
        {
            if (p == this) continue;
            if (p.Side.Value == Left)  leftTaken  = true;
            if (p.Side.Value == Right) rightTaken = true;
        }
        Side.Value = !leftTaken ? Left : (!rightTaken ? Right : Spectator);

        Debug.Log($"[Server] Spawned paddle #{ObjectId} for client {OwnerId}: {SideName(Side.Value)}.");
        Debug.Log($"[Server] PaddleController.All.Count = {All.Count}.");
        GameState.Announce($"Player {OwnerId} joined as {SideName(Side.Value)}");
        GameState.Instance?.ServerPlayersChanged();
    }

    public override void OnStopServer()
    {
        All.Remove(this);
        ByOwner.Remove(OwnerId);
        GameState.Announce($"Player {OwnerId} ({SideName(Side.Value)}) left");
        GameState.Instance?.ServerPlayersChanged();
    }

    public override void OnStartClient()
    {
        if (!All.Contains(this)) All.Add(this);
        bool mine = IsOwner;
        gameObject.name = mine ? $"Paddle {OwnerId} (me)" : $"Paddle {OwnerId}";
        if (mine)
        {
            Local = this;
            Debug.Log($"[Client] I am client {OwnerId}, playing {SideName(Side.Value)}. My paddle is #{ObjectId}.");
        }
        else
        {
            Debug.Log($"[Client] Another paddle appeared: client {OwnerId}, {SideName(Side.Value)}, object #{ObjectId}.");
        }
        Debug.Log($"[Client] PaddleController.All.Count = {All.Count}.");
        Place();
    }

    public override void OnStopClient()
    {
        All.Remove(this);
        if (Local == this) Local = null;
    }

    // ---- server-side helpers ----

    public static PaddleController ForConnection(NetworkConnection conn)
    {
        if (conn == null) return null;
        ByOwner.TryGetValue(conn.ClientId, out PaddleController p);
        return p;
    }

    public static PaddleController ForSide(int side)
    {
        foreach (PaddleController p in All) if (p != null && p.Side.Value == side) return p;
        return null;
    }

    public static string SideName(int side) => side == Left ? "LEFT" : side == Right ? "RIGHT" : "SPECTATOR";

    // ---- authoritative tick movement ----

    private void OnTick()
    {
        // 1. Owner sends input
        if (IsOwner && Side.Value != Spectator)
        {
            float axis = 0f;
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   axis += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) axis -= 1f;
            }
            else
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) axis += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) axis -= 1f;
            }
            ServerSetAxis(axis);
        }

        // 2. Server moves the paddle
        if (IsServerInitialized)
        {
            transform.position += Vector3.forward * _serverAxis * speed * (float)TimeManager.TickDelta;
            Court.ClampPaddle(transform);
        }
    }

    [ServerRpc]
    private void ServerSetAxis(float axis)
    {
        if (float.IsNaN(axis)) return;
        axis = Mathf.Clamp(axis, -1f, 1f);
        if (Side.Value == Spectator) return;

        // Rate limit: 30 ms
        double now = TimeManager.TicksToTime(TimeManager.Tick);
        if (_lastInputTime >= 0 && now - _lastInputTime < MinInputGap)
        {
            RejectedInputs++;
            Debug.Log($"[Server] Rejected input from client {OwnerId}: too fast.");
            return;
        }
        _lastInputTime = now;

        _serverAxis = axis;
    }

    // ---- cheat keys & serve in Update ----

    private void Update()
    {
        if (!IsOwner) return;
        if (Side.Value == Spectator) return;

        Keyboard kb = Keyboard.current;

        // Key C (spam): send 20 inputs in one frame
        bool cPressed = (kb != null && kb.cKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.C);
        if (cPressed)
        {
            for (int i = 0; i < 20; i++)
            {
                ServerSetAxis(1f);
            }
        }

        // Key X (speed hack): send axis = 50f
        bool xPressed = (kb != null && kb.xKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.X);
        if (xPressed)
        {
            ServerSetAxis(50f);
        }

        // Key T (teleport): client direct position write, no RPC
        bool tPressed = (kb != null && kb.tKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.T);
        if (tPressed)
        {
            if (NetworkBall.Instance != null)
            {
                transform.position = NetworkBall.Instance.transform.position;
            }
        }

        // Key G (score edit): direct write to SyncVar, no RPC
        bool gPressed = (kb != null && kb.gKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.G);
        if (gPressed)
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.LeftScore.Value = 9;
            }
        }

        // Serve request
        bool spacePressed = (kb != null && kb.spaceKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.Space);
        if (spacePressed)
        {
            ServerRequestServe();
        }
    }

    [ServerRpc]
    private void ServerRequestServe()
    {
        GameState gs = GameState.Instance;
        if (gs == null || NetworkBall.Instance == null) return;

        // Step 2: Limit the serve to one request per second
        double now = TimeManager.TicksToTime(TimeManager.Tick);
        if (_lastServeTime >= 0 && now - _lastServeTime < MinServeGap)
        {
            TargetRefused(Owner, "TOO FAST");
            return;
        }
        _lastServeTime = now;

        if (gs.ServingSide.Value != Side.Value) return;
        if (NetworkBall.Instance.InPlay.Value) return;

        NetworkBall.Instance.ServerServe(towardSide: 1 - Side.Value);
        GameState.Announce($"Player {OwnerId} ({SideName(Side.Value)}) served");
    }

    [TargetRpc]
    private void TargetRefused(NetworkConnection target, string why)
    {
        ConnectionHud.Flash(why);
    }

    [ObserversRpc]
    public void ObserversScored(int left, int right)
    {
        Debug.Log($"[Client] Goal! Player {OwnerId} ({SideName(Side.Value)}) scored. {left} : {right}");
        ConnectionHud.Flash($"{SideName(Side.Value)} scored!  {left} : {right}");
    }

    [TargetRpc]
    public void TargetYouServe(NetworkConnection target)
    {
        Debug.Log("[Client] YOU serve. Press Space.");
        ConnectionHud.Flash("YOU SERVE — press Space");
    }

    // ---- presentation ----

    private void Place()
    {
        int side = Side.Value;
        Renderer r = GetComponent<Renderer>();
        if (side == Spectator)
        {
            if (r != null) r.enabled = false;
            transform.position = new Vector3(0, -5f, 0);
            return;
        }
        if (r != null) r.enabled = true;
        transform.position = new Vector3(side == Left ? -Court.PaddleX : Court.PaddleX, 0.5f, transform.position.z);
        Court.Paint(gameObject, side == Left ? leftColor : rightColor);
    }
}
