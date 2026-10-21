using UnityEngine;

/// <summary>
/// Court dimensions and geometry constants defined for Pong.
/// </summary>
public class Court : MonoBehaviour
{
    public const float PaddleX = 7f;
    public const float HalfWidth = 8f;
    public const float HalfHeight = 5f;
    public const float BallY = 0.25f;
    public const float BallRadius = 0.25f;
    public const float PaddleHalfLength = 1f;

    [SerializeField] public MonoBehaviour localBall;

    public static void ClampPaddle(Transform t)
    {
        Vector3 pos = t.position;
        float limit = HalfHeight - PaddleHalfLength;
        pos.z = Mathf.Clamp(pos.z, -limit, limit);
        t.position = pos;
    }
}
