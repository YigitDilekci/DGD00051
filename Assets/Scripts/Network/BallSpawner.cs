using UnityEngine;
using FishNet.Object;

/// <summary>
/// Homework 1, Step 7: Spawns the server-authoritative network ball.
/// Attached to the Court GameObject.
/// </summary>
public class BallSpawner : NetworkBehaviour
{
    public GameObject ballPrefab;

    public override void OnStartServer()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("[BallSpawner] ballPrefab is not assigned in the Inspector!");
            return;
        }

        GameObject ball = Instantiate(ballPrefab, new Vector3(0, Court.BallY, 0), Quaternion.identity);
        Spawn(ball);
    }
}
