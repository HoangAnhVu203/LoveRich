using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HeartChainManager : MonoBehaviour
{
    [Header("Heart list (0 = leader)")]
    public List<Transform> hearts = new List<Transform>();

    [Header("Leader movement")]
    [Tooltip("Ưu tiên dùng splinePath (RoadManager cấp). Nếu false sẽ quay quanh center.")]
    public bool useSplinePath = true;

    public SplinePath splinePath;

    [Tooltip("Chạy ngược chiều")]
    public bool reverseDirection = true;

    [Tooltip("Tốc độ leader khi bình thường (m/s trên spline)")]
    public float normalSpeed = 30f;

    [Tooltip("Tốc độ leader khi boost (m/s trên spline)")]
    public float boostSpeed = 100f;

    public float speedLerp = 5f;

    [Tooltip("Offset để model nằm đúng hướng")]
    public Vector3 modelEulerOffset = new Vector3(0f, 0f, 0f);

    [Tooltip("Khóa hướng chạy nằm trên mặt phẳng XZ")]
    public bool lockToXZPlane = true;

    [Header("History sampling")]
    public float sampleDistance = 0.05f;
    public float pointsPerHeart = 10f;

    [Header("Follow Lerp")]
    public float normalFollowPosLerp = 15f;
    public float normalFollowRotLerp = 15f;
    public float boostFollowPosLerp = 25f;
    public float boostFollowRotLerp = 25f;
    public float followLerpBlendSpeed = 10f;

    [Header("Center (fallback rotate-around)")]
    public Transform center;

    [Header("Shared Energy UI")]
    public RectTransform sharedEnergyBar;
    public Transform sharedBarRoot;
    public Vector3 energyBarWorldOffset = new Vector3(0, -1.5f, 0);

    [Header("Runtime spline state")]
    public float leaderDistance;

    // ================= INTERNAL =================

    struct Pose { public Vector3 pos; public Quaternion rot; }
    readonly List<Pose> _history = new List<Pose>();

    Vector3 _lastRecordPos;
    bool _hasLastRecordPos;

    float _currentPosLerp;
    float _currentRotLerp;

    float _currentLeaderSpeed;

    Camera _cam;

    // ================= UNITY =================

    void Start()
    {
        _cam = Camera.main;

        _currentPosLerp = normalFollowPosLerp;
        _currentRotLerp = normalFollowRotLerp;
        _currentLeaderSpeed = normalSpeed;

        InitHistory();

        EnsureEnergyOnLeaderOnly();
        BindEnergyToLeader(true);

        // snap leader lên spline ngay từ đầu (nếu có)
        if (useSplinePath && splinePath != null && splinePath.TotalLength > 0f && GetLeader() != null)
        {
            splinePath.Rebuild();
            leaderDistance = splinePath.FindClosestDistance(GetLeader().position);
            SampleAndApplyLeaderPose(GetLeader(), leaderDistance);
            ForceRecordLeaderPose();
            SnapAllHeartsToHistory();
        }
    }

    void Update()
    {
        if (hearts == null || hearts.Count == 0) return;

        Transform leader = hearts[0];
        if (leader == null) return;

        bool isBoosting = HeartWithEnergy.IsBoostingGlobal;
        float speedTarget = isBoosting ? boostSpeed : normalSpeed;
        _currentLeaderSpeed = Mathf.Lerp(_currentLeaderSpeed, speedTarget, speedLerp * Time.deltaTime);

        // ---- MOVE LEADER (ƯU TIÊN SPLINE) ----
        if (useSplinePath && splinePath != null && splinePath.TotalLength > 0f)
        {
            float dir = reverseDirection ? -1f : 1f;
            leaderDistance += dir * _currentLeaderSpeed * Time.deltaTime;

            SampleAndApplyLeaderPose(leader, leaderDistance);
        }
        else if (center != null)
        {
            // fallback: quay quanh center
            float dir = reverseDirection ? 1f : -1f; // vì RotateAround dùng Vector3.down
            leader.RotateAround(center.position, Vector3.down, dir * _currentLeaderSpeed * Time.deltaTime);

            ApplyLeaderRotationFixOnly(leader);
        }

        // ---- HISTORY / FOLLOW ----
        RecordLeaderHistoryByDistance(leader);
        if (_history.Count < 2) return;

        float posTarget = isBoosting ? boostFollowPosLerp : normalFollowPosLerp;
        float rotTarget = isBoosting ? boostFollowRotLerp : normalFollowRotLerp;

        _currentPosLerp = Mathf.Lerp(_currentPosLerp, posTarget, followLerpBlendSpeed * Time.deltaTime);
        _currentRotLerp = Mathf.Lerp(_currentRotLerp, rotTarget, followLerpBlendSpeed * Time.deltaTime);

        for (int i = 1; i < hearts.Count; i++)
        {
            Transform follower = hearts[i];
            if (follower == null) continue;

            float fIndex = i * pointsPerHeart;
            if (fIndex >= _history.Count - 1) fIndex = _history.Count - 1.001f;

            int idx0 = Mathf.FloorToInt(fIndex);
            int idx1 = Mathf.Clamp(idx0 + 1, 0, _history.Count - 1);
            float t = fIndex - idx0;

            Pose p0 = _history[idx0];
            Pose p1 = _history[idx1];

            Vector3 targetPos = Vector3.Lerp(p0.pos, p1.pos, t);
            Quaternion targetRot = Quaternion.Slerp(p0.rot, p1.rot, t);

            follower.position = Vector3.Lerp(follower.position, targetPos, _currentPosLerp * Time.deltaTime);
            follower.rotation = Quaternion.Slerp(follower.rotation, targetRot, _currentRotLerp * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null || sharedEnergyBar == null) return;

        Transform leader = GetLeader();
        if (leader == null) return;

        Vector3 worldPos = leader.position + energyBarWorldOffset;
        Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);

        sharedEnergyBar.position = screenPos;
        if (sharedBarRoot != null) sharedBarRoot.position = screenPos;
    }

    // ================= SPLINE SAMPLE =================

    void SampleAndApplyLeaderPose(Transform leader, float distance)
    {
        if (leader == null || splinePath == null) return;

        splinePath.SampleAtDistance(distance, out var pos, out var fwd);
        ApplyLeaderPose(leader, pos, fwd);
    }

    // ================= LEADER POSE HELPERS =================

    void ApplyLeaderPose(Transform leader, Vector3 pos, Vector3 fwd)
    {
        leader.position = pos;

        Vector3 desiredFwd = reverseDirection ? -fwd : fwd;

        if (lockToXZPlane)
        {
            Vector3 flat = new Vector3(desiredFwd.x, 0f, desiredFwd.z);
            if (flat.sqrMagnitude < 1e-6f) flat = leader.forward;
            desiredFwd = flat.normalized;
        }
        else
        {
            if (desiredFwd.sqrMagnitude < 1e-6f) desiredFwd = leader.forward;
            desiredFwd.Normalize();
        }

        Quaternion look = Quaternion.LookRotation(desiredFwd, Vector3.up);
        leader.rotation = look * Quaternion.Euler(modelEulerOffset);
    }

    void ApplyLeaderRotationFixOnly(Transform leader)
    {
        leader.rotation = leader.rotation * Quaternion.Euler(modelEulerOffset);
    }

    // ================= HISTORY =================

    void RecordLeaderHistoryByDistance(Transform leader)
    {
        if (!_hasLastRecordPos)
        {
            _lastRecordPos = leader.position;
            _hasLastRecordPos = true;
            _history.Insert(0, new Pose { pos = leader.position, rot = leader.rotation });
            return;
        }

        Vector3 cur = leader.position;
        if ((cur - _lastRecordPos).sqrMagnitude >= sampleDistance * sampleDistance)
        {
            _history.Insert(0, new Pose { pos = cur, rot = leader.rotation });
            _lastRecordPos = cur;

            int max = Mathf.CeilToInt(hearts.Count * pointsPerHeart) + 20;
            if (_history.Count > max)
                _history.RemoveRange(max, _history.Count - max);
        }
    }

    public void InitHistory()
    {
        _history.Clear();
        _hasLastRecordPos = false;

        if (hearts == null || hearts.Count == 0) return;
        Transform leader = hearts[0];
        if (leader == null) return;

        _history.Add(new Pose { pos = leader.position, rot = leader.rotation });
        _lastRecordPos = leader.position;
        _hasLastRecordPos = true;
    }

    void ForceRecordLeaderPose()
    {
        Transform leader = GetLeader();
        if (leader == null) return;

        _history.Clear();
        _history.Add(new Pose { pos = leader.position, rot = leader.rotation });

        _lastRecordPos = leader.position;
        _hasLastRecordPos = true;
    }

    public void SnapAllHeartsToHistory()
    {
        if (_history.Count < 2 || hearts == null || hearts.Count == 0) return;

        for (int i = 0; i < hearts.Count; i++)
        {
            Transform tf = hearts[i];
            if (tf == null) continue;

            float fIndex = i * pointsPerHeart;
            if (fIndex >= _history.Count - 1) fIndex = _history.Count - 1.001f;

            int idx0 = Mathf.FloorToInt(fIndex);
            int idx1 = Mathf.Clamp(idx0 + 1, 0, _history.Count - 1);
            float t = fIndex - idx0;

            Pose p0 = _history[idx0];
            Pose p1 = _history[idx1];

            tf.position = Vector3.Lerp(p0.pos, p1.pos, t);
            tf.rotation = Quaternion.Slerp(p0.rot, p1.rot, t);
        }
    }

    public void RebuildHistoryByChainSegments()
    {
        _history.Clear();
        _hasLastRecordPos = false;

        if (hearts == null || hearts.Count == 0) return;

        int need = Mathf.CeilToInt(hearts.Count * pointsPerHeart) + 20;

        _history.Add(new Pose { pos = hearts[0].position, rot = hearts[0].rotation });

        for (int i = 1; i < hearts.Count && _history.Count < need; i++)
        {
            Transform a = hearts[i - 1];
            Transform b = hearts[i];
            if (a == null || b == null) continue;

            int kCount = Mathf.Max(1, Mathf.RoundToInt(pointsPerHeart));
            for (int k = 1; k <= kCount && _history.Count < need; k++)
            {
                float t = k / (float)kCount;
                _history.Add(new Pose
                {
                    pos = Vector3.Lerp(a.position, b.position, t),
                    rot = Quaternion.Slerp(a.rotation, b.rotation, t)
                });
            }
        }

        Pose pad = _history[_history.Count - 1];
        while (_history.Count < need) _history.Add(pad);

        _lastRecordPos = hearts[0].position;
        _hasLastRecordPos = true;
    }

    // ================= ENERGY =================

    void BindEnergyToLeader(bool forceCenterBind)
    {
        Transform leader = GetLeader();
        if (leader == null) return;

        var e = leader.GetComponent<HeartWithEnergy>();
        if (e == null) e = leader.gameObject.AddComponent<HeartWithEnergy>();

        // HeartChainManager điều khiển movement, HeartWithEnergy chỉ quản lý energy/boost state
        e.driveMovement = false;

        if (forceCenterBind || e.center == null)
            e.center = center;

        e.BindUI(sharedEnergyBar, sharedBarRoot, center);
        e.enabled = true;
    }

    public void EnsureEnergyOnLeaderOnly()
    {
        if (hearts == null || hearts.Count == 0) return;

        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] == null) continue;

            var e = hearts[i].GetComponent<HeartWithEnergy>();
            if (i == 0)
            {
                if (e == null) e = hearts[i].gameObject.AddComponent<HeartWithEnergy>();
                e.enabled = true;
            }
            else
            {
                if (e != null) e.enabled = false;
            }
        }
    }

    // ================= PUBLIC API =================

    public Transform GetLeader() => (hearts != null && hearts.Count > 0) ? hearts[0] : null;
    public Transform GetLastHeart() => (hearts != null && hearts.Count > 0) ? hearts[hearts.Count - 1] : null;

    public void RegisterHeart(Transform newHeart)
    {
        if (newHeart == null) return;
        if (hearts == null) hearts = new List<Transform>();

        if (!hearts.Contains(newHeart))
        {
            hearts.Add(newHeart);

            if (_history.Count == 0)
            {
                InitHistory();
                _currentPosLerp = normalFollowPosLerp;
                _currentRotLerp = normalFollowRotLerp;
            }
        }
    }

    public void RecalculateLeaderByWeight()
    {
        if (hearts == null || hearts.Count == 0) return;

        int bestIndex = 0;
        int bestWeight = int.MinValue;

        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] == null) continue;
            var stats = hearts[i].GetComponent<HeartStats>();
            int w = (stats != null) ? stats.weight : 0;

            if (w > bestWeight)
            {
                bestWeight = w;
                bestIndex = i;
            }
        }

        if (bestIndex == 0)
        {
            BindEnergyToLeader(false);
            EnsureEnergyOnLeaderOnly();
            return;
        }

        List<Transform> newList = new List<Transform>(hearts.Count);
        for (int i = 0; i < hearts.Count; i++)
            newList.Add(hearts[(bestIndex + i) % hearts.Count]);
        hearts = newList;

        BindEnergyToLeader(false);
        EnsureEnergyOnLeaderOnly();

        RebuildHistoryByChainSegments();
        SnapAllHeartsToHistory();

        _lastRecordPos = hearts[0].position;
        _hasLastRecordPos = true;

        Debug.Log($"[Leader] New leader: {hearts[0].name} (weight={bestWeight})");
    }

    // ================= ROAD SWITCH API =================
    // RoadManager sẽ gọi hàm này

    public void SetSplinePathKeepState(SplinePath newPath)
    {
        if (newPath == null) return;

        newPath.Rebuild();
        splinePath = newPath;
        useSplinePath = true;

        Transform leader = GetLeader();
        if (leader == null) return;

        // project leader hiện tại lên spline mới -> giữ “vị trí tương đương”
        leaderDistance = splinePath.FindClosestDistance(leader.position);

        // apply ngay pose theo spline mới -> tránh “vẫn chạy theo spline cũ”
        SampleAndApplyLeaderPose(leader, leaderDistance);

        // reset history để follower không giật
        ForceRecordLeaderPose();
        SnapAllHeartsToHistory();

        // đảm bảo energy vẫn đúng
        BindEnergyToLeader(false);
        EnsureEnergyOnLeaderOnly();
    }
}
