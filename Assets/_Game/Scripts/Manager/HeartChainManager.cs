using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HeartChainManager : MonoBehaviour
{
    [Header("Danh sách Heart (0 = leader)")]
    public List<Transform> hearts = new List<Transform>();

    [Header("Tâm quỹ đạo (trùng với center của HeartWithEnergy)")]
    public Transform center;

    [Header("Khoảng cách góc giữa các heart (độ)")]
    public float angleStep = 15f;

    [Header("Độ mượt khi bình thường")]
    public float normalFollowPosLerp = 10f;
    public float normalFollowRotLerp = 10f;

    [Header("Độ mượt khi BOOST (bám sát hơn)")]
    public float boostFollowPosLerp = 40f;
    public float boostFollowRotLerp = 40f;

    void LateUpdate()
    {
        if (hearts.Count == 0 || center == null) return;

        Transform leader = hearts[0];

        Vector3 centerPos = center.position;
        Vector3 leaderOffset = leader.position - centerPos;
        float radius = leaderOffset.magnitude;

        if (radius < 0.0001f) return;

        // hướng chuẩn từ tâm tới leader
        Vector3 baseDir = leaderOffset.normalized;

        // 🔹 xem hiện tại có đang boost không
        bool isBoosting = HeartWithEnergy.IsBoostingGlobal;

        float posLerp = isBoosting ? boostFollowPosLerp : normalFollowPosLerp;
        float rotLerp = isBoosting ? boostFollowRotLerp : normalFollowRotLerp;

        for (int i = 1; i < hearts.Count; i++)
        {
            Transform follower = hearts[i];

            // mỗi heart lệch thêm angleStep độ quanh trục Y
            float angle = angleStep * i;
            Quaternion rotAround = Quaternion.AngleAxis(-angle, Vector3.up); // -hay + tùy chiều

            Vector3 targetOffset = rotAround * baseDir * radius;
            Vector3 targetPos = centerPos + targetOffset;

            // 🔹 nội suy cho mềm, nhưng khi boost thì Lerp rất nhanh → gần như dính target
            follower.position = Vector3.Lerp(
                follower.position,
                targetPos,
                posLerp * Time.deltaTime
            );

            follower.rotation = Quaternion.Slerp(
                follower.rotation,
                leader.rotation,
                rotLerp * Time.deltaTime
            );
        }
    }

    // gọi khi spawn thêm heart
    public void RegisterHeart(Transform newHeart)
    {
        if (!hearts.Contains(newHeart))
        {
            hearts.Add(newHeart);
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
