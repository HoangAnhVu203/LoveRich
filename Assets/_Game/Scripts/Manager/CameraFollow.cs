using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Follow Settings")]
    [SerializeField] Vector3 offset = new Vector3(0, 5f, -10f);
    [SerializeField] float moveSpeed = 5f;

    [Header("Target (tự động = Heart leader)")]
    [SerializeField] Transform target;
    [SerializeField] HeartChainManager chainManager;

    // Giữ offset thiết kế (Inspector) để dùng khi load lại
    Vector3 _designOffset;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (chainManager == null)
            chainManager = FindObjectOfType<HeartChainManager>();

        // Lưu offset thiết kế ngay từ đầu
        _designOffset = offset;
    }

    void Start()
    {
        // Start chỉ nên “bind target”, KHÔNG recompute offset theo vị trí camera runtime (dễ lệch khi load)
        if (target == null)
            UpdateTargetToLeader();

        // Nếu đây là lần chơi mới (không load), snap 1 lần theo offset thiết kế để chắc chắn thấy leader
        if (target != null)
            SnapToTargetUsingDesignOffset();
    }

    void LateUpdate()
    {
        AutoUpdateTargetFromLeader();

        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, moveSpeed * Time.deltaTime);
    }

    // ================= API =================

    /// <summary>
    /// Set target nhưng KHÔNG tự ý đổi offset (tránh offset bị chốt sai sau load).
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void UpdateTargetToLeader()
    {
        if (chainManager == null) return;

        Transform leader = chainManager.GetLeader();
        if (leader != null)
            target = leader;
    }

    void AutoUpdateTargetFromLeader()
    {
        if (chainManager == null)
        {
            chainManager = FindObjectOfType<HeartChainManager>();
            if (chainManager == null) return;
        }

        Transform leader = chainManager.GetLeader();
        if (leader == null) return;

        // Nếu target null/đã destroy/khác leader => rebind
        if (target == null || !target || target != leader)
            target = leader;
    }

    /// <summary>
    /// Gọi sau khi Load + chain đã Snap xong.
    /// Dùng offset thiết kế để đảm bảo leader nằm trong màn hình.
    /// </summary>
    public void RebindToLeaderSnap()
    {
        if (chainManager == null) chainManager = FindObjectOfType<HeartChainManager>();
        if (chainManager == null) return;

        Transform leader = chainManager.GetLeader();
        if (leader == null) return;

        target = leader;

        // QUAN TRỌNG: dùng offset thiết kế, không recompute theo camera hiện tại
        offset = _designOffset;

        // Snap ngay
        transform.position = leader.position + offset;
    }

    void SnapToTargetUsingDesignOffset()
    {
        if (target == null) return;
        offset = _designOffset;
        transform.position = target.position + offset;
    }
}
