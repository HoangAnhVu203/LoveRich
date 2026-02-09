using UnityEngine;

public class HeartWindAlign : MonoBehaviour
{
    [SerializeField] Transform windRoot;   
    [SerializeField] float rotSmooth = 12f;

    Vector3 _lastPos;
    bool _inited;

    void LateUpdate()
    {
        if (!windRoot) return;

        Vector3 pos = transform.position;

        if (!_inited)
        {
            _inited = true;
            _lastPos = pos;
            return;
        }

        Vector3 v = (pos - _lastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPos = pos;


        if (v.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(v.normalized, Vector3.up);

        windRoot.rotation = Quaternion.Slerp(windRoot.rotation, targetRot, rotSmooth * Time.deltaTime);
    }
}
