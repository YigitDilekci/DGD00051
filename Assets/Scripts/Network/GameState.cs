using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// Homework 1, Steps 7, 8, 9: Synchronizes score, serving turn, and event feed.
/// Attached to the GameState GameObject.
/// </summary>
public class GameState : NetworkBehaviour
{
    public static GameState Instance { get; private set; }

    public readonly SyncVar<int> LeftScore = new SyncVar<int>(0);
    public readonly SyncVar<int> RightScore = new SyncVar<int>(0);
    public readonly SyncVar<int> ServingSide = new SyncVar<int>(0); // 0 = LEFT, 1 = RIGHT

    public readonly SyncList<string> Feed = new SyncList<string>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Add panel to ConnectionHud showing scores, serving side, and the event feed
        ConnectionHud.Panels.Add(() =>
        {
            string serve = ServingSide.Value == 0 ? "LEFT" : "RIGHT";
            string text = $"LEFT {LeftScore.Value} : {RightScore.Value} RIGHT\nServe: {serve}";
            if (Feed.Count > 0)
            {
                text += "\n" + string.Join("\n", Feed);
            }
            return text;
        });
    }

    /// <summary>
    /// Step 9: Server only announcement feed. Keeps the last 6 lines.
    /// </summary>
    public static void Announce(string line)
    {
        if (Instance == null || !Instance.IsServerInitialized) return;

        Instance.Feed.Add(line);
        if (Instance.Feed.Count > 6)
        {
            Instance.Feed.RemoveAt(0);
        }
    }

    /// <summary>
    /// Server only: Updates score and handles serve rotation & RPC dispatch.
    /// </summary>
    public void Goal(int scorerSide)
    {
        if (!IsServerInitialized) return;

        // 1. Update SyncVars first
        if (scorerSide == 0)
        {
            LeftScore.Value++;
        }
        else
        {
            RightScore.Value++;
        }

        int concederSide = 1 - scorerSide;
        ServingSide.Value = concederSide;

        Announce($"Goal! {(scorerSide == 0 ? "LEFT" : "RIGHT")} scored ({LeftScore.Value} : {RightScore.Value})");

        // 2. Dispatch RPCs after SyncVar writes
        PaddleController scorerPaddle = PaddleController.All.Find(p => p.Side.Value == scorerSide);
        PaddleController concederPaddle = PaddleController.All.Find(p => p.Side.Value == concederSide);

        if (scorerPaddle != null)
        {
            scorerPaddle.AnnounceGoal(LeftScore.Value, RightScore.Value);
        }

        if (concederPaddle != null && concederPaddle.Owner != null)
        {
            concederPaddle.YouServe(concederPaddle.Owner);
        }
    }
}
