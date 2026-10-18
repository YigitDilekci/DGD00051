using UnityEngine;

/// <summary>
/// Step 1: Single-player paddle movement script.
/// Controls movement with W/S (Left paddle) or Arrow keys (Right paddle).
/// </summary>
public class PaddleMovement : MonoBehaviour
{
    [SerializeField] private bool isLeftPaddle = true;
    [SerializeField] private float speed = 10f;

    public bool IsLeftPaddle
    {
        get => isLeftPaddle;
        set => isLeftPaddle = value;
    }

    private void Update()
    {
        float move = 0f;
        if (isLeftPaddle)
        {
            if (Input.GetKey(KeyCode.W)) move += 1f;
            if (Input.GetKey(KeyCode.S)) move -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow)) move += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) move -= 1f;
        }

        if (move != 0f)
        {
            Vector3 pos = transform.position;
            pos.z += move * speed * Time.deltaTime;
            float limit = Court.HalfHeight - Court.PaddleHalfLength;
            pos.z = Mathf.Clamp(pos.z, -limit, limit);
            transform.position = pos;
        }
    }
}
