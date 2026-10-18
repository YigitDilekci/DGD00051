using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// Homework 1, Step 7: Server-authoritative network ball.
/// Ball position is synchronized to clients via NetworkTransform (Client Authoritative = OFF).
/// </summary>
public class NetworkBall : NetworkBehaviour
{
    public static NetworkBall Instance { get; private set; }

    [SerializeField] private float speed = 10f;
    public readonly SyncVar<bool> InPlay = new SyncVar<bool>();

    private Vector3 velocity;
    private Renderer ballRenderer;

    private void Awake()
    {
        Instance = this;
        ballRenderer = GetComponent<Renderer>();
        InPlay.OnChange += OnInPlayChange;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        InPlay.OnChange -= OnInPlayChange;
    }

    private void OnInPlayChange(bool prev, bool next, bool asServer)
    {
        if (ballRenderer != null)
        {
            ballRenderer.material.color = next ? Color.white : Color.gray;
        }
    }

    /// <summary>
    /// Server only: Launch the ball toward towardSide (0 = toward left, 1 = toward right).
    /// </summary>
    public void Serve(int towardSide)
    {
        if (!IsServerInitialized) return;

        float dirX = (towardSide == 0) ? -1f : 1f;
        float randomZ = Random.Range(-0.5f, 0.5f);
        velocity = new Vector3(dirX, 0f, randomZ).normalized * speed;
        InPlay.Value = true;

        if (GameState.Instance != null)
        {
            GameState.Announce($"Ball served toward {(towardSide == 0 ? "LEFT" : "RIGHT")}");
        }
    }

    private void Update()
    {
        // First line: server guard
        if (!IsServerInitialized) return;
        if (!InPlay.Value) return;

        // Move ball
        transform.position += velocity * Time.deltaTime;

        // Wall bounce (top and bottom)
        float topLimit = Court.HalfHeight - Court.BallRadius;
        if (transform.position.z > topLimit && velocity.z > 0f)
        {
            velocity.z = -velocity.z;
            transform.position = new Vector3(transform.position.x, transform.position.y, topLimit);
        }
        else if (transform.position.z < -topLimit && velocity.z < 0f)
        {
            velocity.z = -velocity.z;
            transform.position = new Vector3(transform.position.x, transform.position.y, -topLimit);
        }

        // Paddle collisions using PaddleController.All
        foreach (var paddle in PaddleController.All)
        {
            if (paddle.Side.Value != 0 && paddle.Side.Value != 1) continue;

            Vector3 pPos = paddle.transform.position;
            bool isLeft = paddle.Side.Value == 0;

            float xDist = Mathf.Abs(transform.position.x - pPos.x);
            if (xDist <= (Court.BallRadius + 0.25f))
            {
                if (Mathf.Abs(transform.position.z - pPos.z) <= (Court.PaddleHalfLength + Court.BallRadius))
                {
                    if (isLeft && velocity.x < 0f)
                    {
                        Deflect(pPos.z);
                    }
                    else if (!isLeft && velocity.x > 0f)
                    {
                        Deflect(pPos.z);
                    }
                }
            }
        }

        // Goal line check
        if (transform.position.x < -Court.HalfWidth)
        {
            // Ball passed left goal line -> RIGHT scored (1)
            ScoreGoal(1);
        }
        else if (transform.position.x > Court.HalfWidth)
        {
            // Ball passed right goal line -> LEFT scored (0)
            ScoreGoal(0);
        }
    }

    private void Deflect(float paddleZ)
    {
        velocity.x = -velocity.x;
        float offset = (transform.position.z - paddleZ) / Court.PaddleHalfLength;
        velocity.z = offset * speed * 0.75f;
        velocity = velocity.normalized * speed;
    }

    private void ScoreGoal(int scorerSide)
    {
        transform.position = new Vector3(0, Court.BallY, 0);
        velocity = Vector3.zero;
        InPlay.Value = false;

        if (GameState.Instance != null)
        {
            GameState.Instance.Goal(scorerSide);
        }
    }
}
