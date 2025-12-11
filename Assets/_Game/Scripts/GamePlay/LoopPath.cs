using System.Collections.Generic;
using UnityEngine;

public class LoopPath : MonoBehaviour
{
    public List<Transform> points = new List<Transform>();

    float[] _cumDistances;
    float _totalLength;

    void Awake()
    {
        RebuildDistanceTable();
    }

    public float TotalLength => _totalLength;

    public void RebuildDistanceTable()
    {
        if (points.Count < 2)
        {
            _cumDistances = null;
            _totalLength = 0;
            return;
        }

        int n = points.Count;
        _cumDistances = new float[n];
        _cumDistances[0] = 0f;

        float dist = 0f;
        for (int i = 1; i < n; i++)
        {
            dist += Vector3.Distance(points[i - 1].position, points[i].position);
            _cumDistances[i] = dist;
        }

        // đóng vòng: điểm cuối nối lại điểm đầu
        dist += Vector3.Distance(points[n - 1].position, points[0].position);
        _totalLength = dist;
    }

    /// <summary>
    /// Lấy vị trí + hướng trên path theo khoảng cách dọc đường (distanceAlongPath).
    /// distance sẽ tự wrap quanh vòng.
    /// </summary>
    public void SampleAtDistance(float distance, out Vector3 pos, out Vector3 forward)
    {
        pos = Vector3.zero;
        forward = Vector3.forward;

        if (points.Count < 2 || _totalLength <= 0f) return;

        // đưa distance về [0, totalLength)
        distance = Mathf.Repeat(distance, _totalLength);

        // tìm đoạn [Pi, P(i+1)] chứa distance
        int n = points.Count;
        float segmentStartDist = 0f;

        for (int i = 1; i <= n; i++)
        {
            float segmentEndDist = (i < n) ? _cumDistances[i] : _totalLength;

            if (distance <= segmentEndDist)
            {
                float segmentLength = segmentEndDist - segmentStartDist;
                float t = segmentLength > 0f ? (distance - segmentStartDist) / segmentLength : 0f;

                Transform a = points[(i - 1) % n];
                Transform b = points[i % n];

                Vector3 aPos = a.position;
                Vector3 bPos = b.position;

                pos = Vector3.Lerp(aPos, bPos, t);
                forward = (bPos - aPos).normalized;
                return;
            }

            segmentStartDist = segmentEndDist;
        }

        // fallback
        pos = points[0].position;
        forward = (points[1].position - points[0].position).normalized;
    }

#if UNITY_EDITOR
    // vẽ gizmo để dễ chỉnh đường
    void OnDrawGizmos()
    {
        if (points == null || points.Count < 2) return;

        Gizmos.color = Color.yellow;
        int n = points.Count;
        for (int i = 0; i < n; i++)
        {
            Transform a = points[i];
            Transform b = points[(i + 1) % n];
            if (a != null && b != null)
            {
                Gizmos.DrawLine(a.position, b.position);
            }
        }
    }
#endif
}
