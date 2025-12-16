using System.Collections.Generic;
using UnityEngine;

public class SplinePath : MonoBehaviour
{
    public List<Transform> points = new List<Transform>();

    [Header("Mode")]
    public bool loop = false;

    [Header("Sampling")]
    [Tooltip("Khoảng cách mục tiêu giữa các điểm baked theo chiều dài (nhỏ = mượt hơn, nặng hơn)")]
    public float bakeStep = 0.2f;

    [Header("Centripetal Catmull-Rom")]
    [Tooltip("0.5 = centripetal (khuyến nghị). 0 = uniform (dễ overshoot), 1 = chordal.")]
    [Range(0f, 1f)] public float alpha = 0.5f;

    [Tooltip("Tangent smoothing trên baked (0 = off). 0.2~0.5 thường ổn.")]
    [Range(0f, 1f)] public float tangentSmooth = 0.35f;

    readonly List<Vector3> _bakedPos = new List<Vector3>();
    readonly List<Vector3> _bakedTan = new List<Vector3>();
    readonly List<float> _cum = new List<float>();
    float _total;

    public float TotalLength => _total;

    void Awake() => Rebuild();

    public void Rebuild()
    {
        _bakedPos.Clear();
        _bakedTan.Clear();
        _cum.Clear();
        _total = 0f;

        if (points == null || points.Count < 2) return;

        // ===== Bake position theo từng segment, sample theo khoảng cách mục tiêu =====
        const int minStepsPerSeg = 8;
        float step = Mathf.Max(0.01f, bakeStep);

        int n = points.Count;
        int segCount = loop ? n : (n - 1);

        Vector3 prev = GetPosSafe(0);
        _bakedPos.Add(prev);
        _cum.Add(0f);

        float dist = 0f;

        for (int seg = 0; seg < segCount; seg++)
        {
            float approx = ApproxSegmentLength(seg);
            int steps = Mathf.Max(minStepsPerSeg, Mathf.CeilToInt(approx / step));

            for (int i = 1; i <= steps; i++)
            {
                float u = i / (float)steps;
                Vector3 p = GetPointCentripetal(seg, u);

                dist += Vector3.Distance(prev, p);
                _bakedPos.Add(p);
                _cum.Add(dist);

                prev = p;
            }
        }

        _total = dist;
        if (_bakedPos.Count < 2 || _total <= 1e-6f) return;

        // ===== Tính tangent từ baked positions (mượt seam) =====
        BuildTangentsFromBaked();
    }

    // Sample theo distance
    public void SampleAtDistance(float distance, out Vector3 pos, out Vector3 forward)
    {
        pos = Vector3.zero;
        forward = Vector3.forward;

        if (_bakedPos.Count < 2 || _total <= 0f) return;

        distance = loop ? Mathf.Repeat(distance, _total) : Mathf.Clamp(distance, 0f, _total);

        // binary search
        int lo = 0;
        int hi = _cum.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (_cum[mid] < distance) lo = mid + 1;
            else hi = mid;
        }

        int i1 = Mathf.Clamp(lo, 1, _cum.Count - 1);
        int i0 = i1 - 1;

        float d0 = _cum[i0];
        float d1 = _cum[i1];
        float t = (d1 - d0) > 1e-6f ? (distance - d0) / (d1 - d0) : 0f;

        pos = Vector3.Lerp(_bakedPos[i0], _bakedPos[i1], t);

        // tangent mượt: lerp baked tangent (đã xử lý seam)
        Vector3 f = Vector3.Slerp(_bakedTan[i0], _bakedTan[i1], t);
        if (f.sqrMagnitude < 1e-6f) f = (_bakedPos[i1] - _bakedPos[i0]);
        forward = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
    }

    // ===================== Tangent from baked =====================

    void BuildTangentsFromBaked()
    {
        _bakedTan.Clear();
        int m = _bakedPos.Count;

        // tangent thô = (next - prev)
        for (int i = 0; i < m; i++)
        {
            int im1 = i - 1;
            int ip1 = i + 1;

            if (loop)
            {
                if (im1 < 0) im1 = m - 2;         // m-1 thường trùng gần seam do bake, dùng m-2 ổn hơn
                if (ip1 >= m) ip1 = 1;            // 0 là đầu, 1 là kế
            }
            else
            {
                im1 = Mathf.Clamp(im1, 0, m - 1);
                ip1 = Mathf.Clamp(ip1, 0, m - 1);
            }

            Vector3 dir = _bakedPos[ip1] - _bakedPos[im1];
            if (dir.sqrMagnitude < 1e-6f)
                dir = (ip1 != i) ? (_bakedPos[ip1] - _bakedPos[i]) : Vector3.forward;

            _bakedTan.Add(dir.normalized);
        }

        // smooth tangent nhẹ để tránh “gãy” tại seam
        if (tangentSmooth > 0f && m >= 3)
        {
            float s = tangentSmooth;
            var tmp = new List<Vector3>(_bakedTan);

            for (int i = 0; i < m; i++)
            {
                int im1 = i - 1;
                int ip1 = i + 1;

                if (loop)
                {
                    if (im1 < 0) im1 = m - 1;
                    if (ip1 >= m) ip1 = 0;
                }
                else
                {
                    im1 = Mathf.Clamp(im1, 0, m - 1);
                    ip1 = Mathf.Clamp(ip1, 0, m - 1);
                }

                Vector3 blended = (tmp[im1] + tmp[i] + tmp[ip1]);
                if (blended.sqrMagnitude > 1e-6f)
                    _bakedTan[i] = Vector3.Slerp(tmp[i], blended.normalized, s);
            }
        }
    }

    // ===================== Centripetal Catmull-Rom =====================

    Vector3 GetPointCentripetal(int seg, float u)
    {
        int n = points.Count;

        int i1 = seg;
        int i2 = (seg + 1);

        int i0 = i1 - 1;
        int i3 = i2 + 1;

        if (loop)
        {
            i0 = WrapIndex(i0, n);
            i1 = WrapIndex(i1, n);
            i2 = WrapIndex(i2, n);
            i3 = WrapIndex(i3, n);
        }
        else
        {
            i0 = Mathf.Clamp(i0, 0, n - 1);
            i1 = Mathf.Clamp(i1, 0, n - 1);
            i2 = Mathf.Clamp(i2, 0, n - 1);
            i3 = Mathf.Clamp(i3, 0, n - 1);
        }

        Vector3 p0 = GetPosSafe(i0);
        Vector3 p1 = GetPosSafe(i1);
        Vector3 p2 = GetPosSafe(i2);
        Vector3 p3 = GetPosSafe(i3);

        return CatmullRomCentripetal(p0, p1, p2, p3, u, alpha);
    }

    static Vector3 CatmullRomCentripetal(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float alpha)
    {
        // parameterize by chord length^alpha
        float t0 = 0f;
        float t1 = t0 + Mathf.Pow(Vector3.Distance(p0, p1), alpha);
        float t2 = t1 + Mathf.Pow(Vector3.Distance(p1, p2), alpha);
        float t3 = t2 + Mathf.Pow(Vector3.Distance(p2, p3), alpha);

        // tránh chia 0
        if (Mathf.Abs(t1 - t0) < 1e-6f) t1 = t0 + 1e-3f;
        if (Mathf.Abs(t2 - t1) < 1e-6f) t2 = t1 + 1e-3f;
        if (Mathf.Abs(t3 - t2) < 1e-6f) t3 = t2 + 1e-3f;

        // remap t in [t1, t2]
        float tt = Mathf.Lerp(t1, t2, Mathf.Clamp01(t));

        Vector3 A1 = (t1 - tt) / (t1 - t0) * p0 + (tt - t0) / (t1 - t0) * p1;
        Vector3 A2 = (t2 - tt) / (t2 - t1) * p1 + (tt - t1) / (t2 - t1) * p2;
        Vector3 A3 = (t3 - tt) / (t3 - t2) * p2 + (tt - t2) / (t3 - t2) * p3;

        Vector3 B1 = (t2 - tt) / (t2 - t0) * A1 + (tt - t0) / (t2 - t0) * A2;
        Vector3 B2 = (t3 - tt) / (t3 - t1) * A2 + (tt - t1) / (t3 - t1) * A3;

        Vector3 C = (t2 - tt) / (t2 - t1) * B1 + (tt - t1) / (t2 - t1) * B2;
        return C;
    }

    // ===================== Helpers =====================

    Vector3 GetPosSafe(int idx)
    {
        if (points == null || points.Count == 0) return Vector3.zero;
        idx = Mathf.Clamp(idx, 0, points.Count - 1);
        return points[idx] ? points[idx].position : Vector3.zero;
    }

    int WrapIndex(int i, int n)
    {
        i %= n;
        if (i < 0) i += n;
        return i;
    }

    float ApproxSegmentLength(int seg)
    {
        if (points == null || points.Count < 2) return 0f;
        Vector3 a = points[seg].position;
        Vector3 b = points[(seg + 1) % points.Count].position;
        return Vector3.Distance(a, b);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (points == null || points.Count < 2) return;

        Gizmos.color = Color.yellow;

        int steps = 120;
        int n = points.Count;
        int segCount = loop ? n : n - 1;
        if (segCount <= 0) return;

        Vector3 prev = points[0].position;
        for (int i = 1; i <= steps; i++)
        {
            float tt = i / (float)steps;
            float segT = tt * segCount;
            int seg = Mathf.Clamp(Mathf.FloorToInt(segT), 0, segCount - 1);
            float u = segT - seg;

            Vector3 p = Application.isPlaying
                ? GetPointCentripetal(seg, u)
                : GetPointCentripetal(seg, u);

            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
#endif

    public float FindClosestDistance(Vector3 worldPos)
    {
        if (_bakedPos == null || _bakedPos.Count == 0 || _cum == null || _cum.Count != _bakedPos.Count)
            return 0f;

        int best = 0;
        float bestSqr = float.MaxValue;

        // tìm point baked gần nhất
        for (int i = 0; i < _bakedPos.Count; i++)
        {
            float s = (worldPos - _bakedPos[i]).sqrMagnitude;
            if (s < bestSqr)
            {
                bestSqr = s;
                best = i;
            }
        }

        // trả distance của điểm gần nhất đó
        return _cum[Mathf.Clamp(best, 0, _cum.Count - 1)];
    }

    public float FindNearestDistanceOnSpline(Vector3 worldPos)
    {
        if (_bakedPos == null || _bakedPos.Count < 2 || _cum == null || _cum.Count != _bakedPos.Count)
            return 0f;

        int best = 0;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < _bakedPos.Count; i++)
        {
            float d = (worldPos - _bakedPos[i]).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = i;
            }
        }

        return _cum[best];
    }


}
