using UnityEngine;

/// <summary>
/// Step 1: Single-player local ball script from the starter project.
/// Handles bounce physics on walls and paddles, and logs scores.
/// </summary>
public class LocalBall : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    private Vector3 velocity;

    private int leftScore = 0;
    private int rightScore = 0;

    private void Start()
    {
        ResetBall();
    }

    public void ResetBall()
    {
        transform.position = new Vector3(0, Court.BallY, 0);
        float dirX = Random.value > 0.5f ? 1f : -1f;
        float randomZ = Random.Range(-0.5f, 0.5f);
        velocity = new Vector3(dirX, 0f, randomZ).normalized * speed;
    }

    private void Update()
    {
        transform.position += velocity * Time.deltaTime;

        // Top/bottom wall bounce
        float limit = Court.HalfHeight - Court.BallRadius;
        if (transform.position.z > limit && velocity.z > 0f)
        {
            velocity.z = -velocity.z;
            transform.position = new Vector3(transform.position.x, transform.position.y, limit);
        }
        else if (transform.position.z < -limit && velocity.z < 0f)
        {
            velocity.z = -velocity.z;
            transform.position = new Vector3(transform.position.x, transform.position.y, -limit);
        }

        // Paddle collisions (single-player mode)
        PaddleMovement[] paddles = FindObjectsByType<PaddleMovement>(FindObjectsSortMode.None);
        foreach (var paddle in paddles)
        {
            Vector3 pPos = paddle.transform.position;
            bool isLeft = paddle.IsLeftPaddle;

            float xDist = Mathf.Abs(transform.position.x - pPos.x);
            if (xDist <= (Court.BallRadius + 0.25f))
            {
                if (Mathf.Abs(transform.position.z - pPos.z) <= (Court.PaddleHalfLength + Court.BallRadius))
                {
                    if (isLeft && velocity.x < 0f)
                    {
                        velocity.x = -velocity.x;
                        float offset = (transform.position.z - pPos.z) / Court.PaddleHalfLength;
                        velocity.z = offset * speed * 0.75f;
                        velocity = velocity.normalized * speed;
                    }
                    else if (!isLeft && velocity.x > 0f)
                    {
                        velocity.x = -velocity.x;
                        float offset = (transform.position.z - pPos.z) / Court.PaddleHalfLength;
                        velocity.z = offset * speed * 0.75f;
                        velocity = velocity.normalized * speed;
                    }
                }
            }
        }

        // Left / Right score
        if (transform.position.x < -Court.HalfWidth)
        {
            rightScore++;
            Debug.Log($"[Local] RIGHT scored. {leftScore} : {rightScore} .");
            ResetBall();
        }
        else if (transform.position.x > Court.HalfWidth)
        {
            leftScore++;
            Debug.Log($"[Local] LEFT scored. {leftScore} : {rightScore} .");
            ResetBall();
        }
    }
}
