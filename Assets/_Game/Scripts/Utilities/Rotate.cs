using UnityEngine;

public class Rotate : MonoBehaviour
{
    [Header("Rotate Settings")]
    public float rotateSpeed = 90f;

    [Tooltip("Xoay theo chiều kim đồng hồ")]
    public bool clockwise = true;

    [Tooltip("Tự động xoay khi Start")]
    public bool playOnStart = true;

    RectTransform rect;
    bool isPlaying;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        if (playOnStart)
            isPlaying = true;
    }

    void Update()
    {
        if (!isPlaying || rect == null) return;

        float dir = clockwise ? -1f : 1f;
        rect.Rotate(0f, 0f, dir * rotateSpeed * Time.unscaledDeltaTime);
    }

    // ===== PUBLIC API =====

    public void Play()
    {
        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void SetSpeed(float speed)
    {
        rotateSpeed = speed;
    }

    public void SetDirection(bool isClockwise)
    {
        clockwise = isClockwise;
    }
}
