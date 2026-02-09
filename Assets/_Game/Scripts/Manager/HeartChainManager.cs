using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HeartChainManager : Singleton<HeartChainManager>
{
    [Header("Heart list (0 = leader)")]
    public List<Transform> hearts = new List<Transform>();

    [Header("Road State")]
    public int CurrentRoadIndex { get; private set; } = 0;

    [Header("Leader movement")]
    public bool useSplinePath = true;
    public SplinePath splinePath;
    public bool reverseDirection = true;

    public float normalSpeed = 30f;
    public float boostSpeed = 100f;
    public float speedLerp = 5f;

    public Vector3 modelEulerOffset = new Vector3(0f, 0f, 0f);
    public bool lockToXZPlane = true;

    [Header("Spline Follow Spacing (meters)")]
    [Tooltip("Khoảng cách thật giữa các heart trên spline (đơn vị mét).")]
    public float heartSpacingMeters = 0.8f;

    [Header("Spline Bake (anti-jitter)")]
    [Tooltip("Bật để bake spline thành polyline mịn (khử giật do SampleAtDistance stepping).")]
    public bool useBakedSpline = true;

    [Tooltip("Bước bake (m). Càng nhỏ càng mượt nhưng tốn memory hơn. Khuyến nghị 0.02 - 0.05")]
    public float bakeStepMeters = 0.03f;

    [Header("Follow Smoothing")]
    public float normalFollowPosLerp = 18f;
    public float normalFollowRotLerp = 18f;
    public float boostFollowPosLerp = 28f;
    public float boostFollowRotLerp = 28f;
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

    float _currentPosLerp;
    float _currentRotLerp;
    float _currentLeaderSpeed;

    Camera _cam;

    // baked polyline
    readonly List<Vector3> _bakedPts = new List<Vector3>();
    readonly List<float> _bakedCumDist = new List<float>(); // 0..total
    float _bakedTotal;

    // ================= UNITY =================

    void Start()
    {
        _cam = Camera.main;

        _currentPosLerp = normalFollowPosLerp;
        _currentRotLerp = normalFollowRotLerp;
        _currentLeaderSpeed = normalSpeed;

        EnsureEnergyOnLeaderOnly();
        BindEnergyToLeader(true);

        if (useSplinePath && splinePath != null && splinePath.TotalLength > 0f && GetLeader() != null)
        {
            splinePath.Rebuild();

            if (useBakedSpline) BakeSpline();

            // project leader hiện tại lên spline
            leaderDistance = splinePath.FindClosestDistance(GetLeader().position);
            leaderDistance = WrapDistanceBySource(leaderDistance);

            // snap chain ngay từ đầu để không "rụng" vị trí
            ApplyChainPoseImmediate();
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

        float posTarget = isBoosting ? boostFollowPosLerp : normalFollowPosLerp;
        float rotTarget = isBoosting ? boostFollowRotLerp : normalFollowRotLerp;
        _currentPosLerp = Mathf.Lerp(_currentPosLerp, posTarget, followLerpBlendSpeed * Time.deltaTime);
        _currentRotLerp = Mathf.Lerp(_currentRotLerp, rotTarget, followLerpBlendSpeed * Time.deltaTime);

        // ---- MOVE & FOLLOW ----
        if (useSplinePath && splinePath != null && splinePath.TotalLength > 0f)
        {
            // leaderDistance tiến theo tốc độ
            float dir = reverseDirection ? -1f : 1f;
            leaderDistance += dir * _currentLeaderSpeed * Time.deltaTime;
            leaderDistance = WrapDistanceBySource(leaderDistance);

            ApplyChainPoseSmooth(Time.deltaTime);
        }
        else if (center != null)
        {
            // fallback: rotate around center (ít dùng trong case spline)
            float dir = reverseDirection ? 1f : -1f;
            leader.RotateAround(center.position, Vector3.down, dir * _currentLeaderSpeed * Time.deltaTime);

            // follower bám theo leader bằng khoảng cách world đơn giản (không spline)
            ApplyFallbackFollowSmooth(Time.deltaTime);
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

    // ================= CORE APPLY =================

    void ApplyChainPoseImmediate()
    {
        if (hearts == null || hearts.Count == 0) return;

        // leader
        SampleAt(leaderDistance, out var lpos, out var lfwd);
        ApplyPoseToTransform(hearts[0], lpos, lfwd, immediate: true);

        // followers
        float dir = reverseDirection ? -1f : 1f; // cùng dir với lúc leader chạy
        for (int i = 1; i < hearts.Count; i++)
        {
            Transform t = hearts[i];
            if (t == null) continue;

            // quan trọng: đi lùi "phía sau" theo hướng di chuyển
            float d = leaderDistance - dir * (i * heartSpacingMeters);
            d = WrapDistanceBySource(d);

            SampleAt(d, out var pos, out var fwd);
            ApplyPoseToTransform(t, pos, fwd, immediate: true);
        }
    }

    void ApplyChainPoseSmooth(float dt)
    {
        if (hearts == null || hearts.Count == 0) return;

        // exponential factor: luôn 0..1, không bị nhảy khi spike dt
        float aPos = 1f - Mathf.Exp(-_currentPosLerp * dt);
        float aRot = 1f - Mathf.Exp(-_currentRotLerp * dt);

        // leader
        SampleAt(leaderDistance, out var lpos, out var lfwd);
        ApplyPoseToTransform(hearts[0], lpos, lfwd, aPos, aRot);

        // followers
        float dir = reverseDirection ? -1f : 1f; // cùng dir với lúc leader chạy
        for (int i = 1; i < hearts.Count; i++)
        {
            Transform t = hearts[i];
            if (t == null) continue;

            // quan trọng: đi lùi "phía sau" theo hướng di chuyển
            float d = leaderDistance - dir * (i * heartSpacingMeters);
            d = WrapDistanceBySource(d);

            SampleAt(d, out var pos, out var fwd);
            ApplyPoseToTransform(t, pos, fwd, aPos, aRot);
        }
    }

    void ApplyPoseToTransform(Transform t, Vector3 pos, Vector3 fwd, bool immediate)
    {
        Quaternion rot = BuildRotation(t, fwd);
        t.position = pos;
        t.rotation = rot;
    }

    void ApplyPoseToTransform(Transform t, Vector3 pos, Vector3 fwd, float aPos, float aRot)
    {
        Quaternion rot = BuildRotation(t, fwd);
        t.position = Vector3.Lerp(t.position, pos, aPos);
        t.rotation = Quaternion.Slerp(t.rotation, rot, aRot);
    }

    Quaternion BuildRotation(Transform t, Vector3 fwd)
    {
        Vector3 desiredFwd = reverseDirection ? -fwd : fwd;

        if (lockToXZPlane)
        {
            desiredFwd.y = 0f;
        }

        if (desiredFwd.sqrMagnitude < 1e-6f)
        {
            desiredFwd = t != null ? t.forward : Vector3.forward;
        }
        else
        {
            desiredFwd.Normalize();
        }

        return Quaternion.LookRotation(desiredFwd, Vector3.up) * Quaternion.Euler(modelEulerOffset);
    }

    // ================= SPLINE SAMPLING =================

    void SampleAt(float distance, out Vector3 pos, out Vector3 fwd)
    {
        if (!useBakedSpline || _bakedPts.Count < 2)
        {
            // direct sample
            splinePath.SampleAtDistance(distance, out pos, out fwd);
            return;
        }

        // baked sample
        SampleBaked(distance, out pos, out fwd);
    }

    void BakeSpline()
    {
        _bakedPts.Clear();
        _bakedCumDist.Clear();
        _bakedTotal = 0f;

        if (splinePath == null || splinePath.TotalLength <= 0f) return;

        float len = splinePath.TotalLength;
        float step = Mathf.Max(0.001f, bakeStepMeters);

        // start
        splinePath.SampleAtDistance(0f, out var prev, out _);
        _bakedPts.Add(prev);
        _bakedCumDist.Add(0f);

        float d = 0f;
        float acc = 0f;

        while (d < len)
        {
            d = Mathf.Min(d + step, len);
            splinePath.SampleAtDistance(d, out var p, out _);

            acc += Vector3.Distance(prev, p);
            _bakedPts.Add(p);
            _bakedCumDist.Add(acc);

            prev = p;
        }

        _bakedTotal = acc;
    }

    void SampleBaked(float distance, out Vector3 pos, out Vector3 fwd)
    {
        pos = Vector3.zero;
        fwd = Vector3.forward;

        if (_bakedPts.Count < 2 || _bakedTotal <= 0f)
        {
            // fallback
            splinePath.SampleAtDistance(distance, out pos, out fwd);
            return;
        }

        // wrap theo baked total (loop)
        distance = Wrap(distance, _bakedTotal);

        // binary search đoạn chứa distance
        int lo = 0;
        int hi = _bakedCumDist.Count - 1;

        while (hi - lo > 1)
        {
            int mid = (lo + hi) >> 1;
            if (_bakedCumDist[mid] <= distance) lo = mid;
            else hi = mid;
        }

        float d0 = _bakedCumDist[lo];
        float d1 = _bakedCumDist[hi];
        float t = (d1 > d0) ? (distance - d0) / (d1 - d0) : 0f;

        Vector3 p0 = _bakedPts[lo];
        Vector3 p1 = _bakedPts[hi];

        pos = Vector3.Lerp(p0, p1, t);

        Vector3 dir = (p1 - p0);
        if (lockToXZPlane) dir.y = 0f;

        if (dir.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
        else fwd = dir.normalized;
    }

    float WrapDistanceBySource(float d)
    {
        if (useBakedSpline && _bakedTotal > 0f) return Wrap(d, _bakedTotal);
        if (splinePath != null && splinePath.TotalLength > 0f) return Wrap(d, splinePath.TotalLength);
        return d;
    }

    static float Wrap(float d, float len)
    {
        if (len <= 0f) return 0f;
        d %= len;
        if (d < 0f) d += len;
        return d;
    }

    // ================= FALLBACK FOLLOW (NO SPLINE) =================

    void ApplyFallbackFollowSmooth(float dt)
    {
        if (hearts == null || hearts.Count == 0) return;

        float aPos = 1f - Mathf.Exp(-_currentPosLerp * dt);
        float aRot = 1f - Mathf.Exp(-_currentRotLerp * dt);

        for (int i = 1; i < hearts.Count; i++)
        {
            Transform t = hearts[i];
            Transform prev = hearts[i - 1];
            if (t == null || prev == null) continue;

            Vector3 targetPos = prev.position - prev.forward * heartSpacingMeters;
            t.position = Vector3.Lerp(t.position, targetPos, aPos);

            Quaternion targetRot = prev.rotation;
            t.rotation = Quaternion.Slerp(t.rotation, targetRot, aRot);
        }
    }

    // ================= ENERGY =================

    void BindEnergyToLeader(bool forceCenterBind)
    {
        Transform leader = GetLeader();
        if (leader == null) return;

        var e = leader.GetComponent<HeartWithEnergy>();
        if (e == null) e = leader.gameObject.AddComponent<HeartWithEnergy>();

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

            // snap nhẹ để không bị “kẹt” khi add
            if (useSplinePath && splinePath != null && splinePath.TotalLength > 0f)
                ApplyChainPoseImmediate();
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

        // xoay danh sách để bestIndex lên đầu
        List<Transform> newList = new List<Transform>(hearts.Count);
        for (int i = 0; i < hearts.Count; i++)
            newList.Add(hearts[(bestIndex + i) % hearts.Count]);
        hearts = newList;

        // cập nhật leaderDistance theo leader mới (project lên spline)
        if (useSplinePath && splinePath != null && splinePath.TotalLength > 0f && hearts[0] != null)
        {
            leaderDistance = splinePath.FindClosestDistance(hearts[0].position);
            leaderDistance = WrapDistanceBySource(leaderDistance);
        }

        BindEnergyToLeader(false);
        EnsureEnergyOnLeaderOnly();

        ApplyChainPoseImmediate();

        Debug.Log($"[Leader] New leader: {hearts[0].name} (weight={bestWeight})");
    }

    // RoadManager gọi
    public void SetSplinePathKeepState(SplinePath newPath)
    {
        if (newPath == null) return;

        newPath.Rebuild();
        splinePath = newPath;
        useSplinePath = true;

        if (useBakedSpline) BakeSpline();

        Transform leader = GetLeader();
        if (leader == null) return;

        leaderDistance = splinePath.FindClosestDistance(leader.position);
        leaderDistance = WrapDistanceBySource(leaderDistance);

        ApplyChainPoseImmediate();

        BindEnergyToLeader(false);
        EnsureEnergyOnLeaderOnly();
    }

    public void SnapChainImmediate()
    {
        if (useSplinePath && splinePath != null && splinePath.TotalLength > 0f)
        {
            leaderDistance = splinePath.FindClosestDistance(GetLeader().position);
            leaderDistance = WrapDistanceBySource(leaderDistance);
            ApplyChainPoseImmediate();
        }
        else
        {
            // fallback cũng snap theo logic hiện có
            ApplyFallbackFollowSmooth(Time.deltaTime);
        }
    }
}
