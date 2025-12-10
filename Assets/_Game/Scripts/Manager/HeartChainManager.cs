using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeartChainManager : MonoBehaviour
{
    [Header("Danh sách Heart (0 = leader)")]
    public List<Transform> hearts = new List<Transform>();

    [Header("History theo khoảng cách")]
    [Tooltip("Khoảng cách (world units) giữa 2 mẫu history của leader")]
    public float sampleDistance = 0.05f;      // 5cm

    [Tooltip("Số mẫu history giữa 2 heart liên tiếp")]
    public float pointsPerHeart = 10f;        // tim cách nhau ~ sampleDistance * pointsPerHeart

    [Header("Độ mượt follower bám theo (bình thường)")]
    public float normalFollowPosLerp = 15f;
    public float normalFollowRotLerp = 15f;

    [Header("Độ mượt follower bám theo (khi BOOST)")]
    public float boostFollowPosLerp = 25f;
    public float boostFollowRotLerp = 25f;

    [Header("Smoothing giữa normal ↔ boost lerp")]
    public float followLerpBlendSpeed = 10f;

    struct Pose
    {
        public Vector3 pos;
        public Quaternion rot;
    }

    List<Pose> _history = new List<Pose>();

    // cho distance-based history
    Vector3 _lastRecordPos;
    bool _hasLastRecordPos;

    // lerp hiện tại (smooth giữa normal & boost)
    float _currentPosLerp;
    float _currentRotLerp;

    void Start()
    {
        InitHistory();

        _currentPosLerp = normalFollowPosLerp;
        _currentRotLerp = normalFollowRotLerp;
    }

    void InitHistory()
    {
        _history.Clear();
        _hasLastRecordPos = false;

        if (hearts.Count == 0)
            return;

        Transform leader = hearts[0];

        Pose p;
        p.pos = leader.position;
        p.rot = leader.rotation;

        _history.Add(p);
        _lastRecordPos = leader.position;
        _hasLastRecordPos = true;
    }

    void Update()
    {
        if (hearts.Count == 0)
            return;

        Transform leader = hearts[0];

        // 1) Ghi history theo QUÃNG ĐƯỜNG
        RecordLeaderHistoryByDistance(leader);

        if (_history.Count < 2)
            return;

        // 2) Chọn lerp theo trạng thái BOOST (không đụng vào spacing nữa)
        bool isBoosting = HeartWithEnergy.IsBoostingGlobal;

        float targetPosLerp = isBoosting ? boostFollowPosLerp : normalFollowPosLerp;
        float targetRotLerp = isBoosting ? boostFollowRotLerp : normalFollowRotLerp;

        _currentPosLerp = Mathf.Lerp(_currentPosLerp, targetPosLerp, followLerpBlendSpeed * Time.deltaTime);
        _currentRotLerp = Mathf.Lerp(_currentRotLerp, targetRotLerp, followLerpBlendSpeed * Time.deltaTime);

        // 3) Follower bám theo history với spacing CỐ ĐỊNH
        for (int i = 1; i < hearts.Count; i++)
        {
            Transform follower = hearts[i];

            // index history mà tim thứ i nên theo
            float fIndex = i * pointsPerHeart;

            if (fIndex >= _history.Count - 1)
                fIndex = _history.Count - 1.001f;

            int idx0 = Mathf.FloorToInt(fIndex);
            int idx1 = Mathf.Clamp(idx0 + 1, 0, _history.Count - 1);
            float t = fIndex - idx0;

            Pose p0 = _history[idx0];
            Pose p1 = _history[idx1];

            Vector3 targetPos = Vector3.Lerp(p0.pos, p1.pos, t);
            Quaternion targetRot = Quaternion.Slerp(p0.rot, p1.rot, t);

            // follower trôi dần tới target (mượt, không giật)
            follower.position = Vector3.Lerp(
                follower.position,
                targetPos,
                _currentPosLerp * Time.deltaTime
            );

            follower.rotation = Quaternion.Slerp(
                follower.rotation,
                targetRot,
                _currentRotLerp * Time.deltaTime
            );
        }
    }

    void RecordLeaderHistoryByDistance(Transform leader)
    {
        if (!_hasLastRecordPos)
        {
            _lastRecordPos = leader.position;
            _hasLastRecordPos = true;

            Pose first;
            first.pos = leader.position;
            first.rot = leader.rotation;
            _history.Insert(0, first);
            return;
        }

        Vector3 currentPos = leader.position;
        float sqrDist = (currentPos - _lastRecordPos).sqrMagnitude;
        float minSqr = sampleDistance * sampleDistance;

        // chỉ khi leader đi đủ xa mới thêm 1 mẫu history mới
        if (sqrDist >= minSqr)
        {
            Pose p;
            p.pos = currentPos;
            p.rot = leader.rotation;

            _history.Insert(0, p); // mới nhất ở đầu
            _lastRecordPos = currentPos;

            // giữ history vừa đủ dài
            int maxPoints = Mathf.CeilToInt(hearts.Count * pointsPerHeart) + 5;
            if (_history.Count > maxPoints)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }
    }

    // gọi khi spawn thêm heart
    public void RegisterHeart(Transform newHeart)
    {

        if (newHeart.parent != transform)
        {
            newHeart.SetParent(transform);
        }
        
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

    public Transform GetLeader()
    {
        return hearts.Count > 0 ? hearts[0] : null;
    }

    public Transform GetLastHeart()
    {
        return hearts.Count > 0 ? hearts[hearts.Count - 1] : null;
    }
}
