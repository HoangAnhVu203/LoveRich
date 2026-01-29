using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFlyIconFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Canvas rootCanvas;          // canvas chứa UI
    [SerializeField] RectTransform fxRoot;       // nơi spawn icon (thường là 1 RectTransform trên canvas)
    [SerializeField] Image iconPrefab;           // prefab UI_RoseFly

    [Header("Anim")]
    [SerializeField] float duration = 0.55f;
    [SerializeField] float arcHeight = 180f;     // độ cong (px)
    [SerializeField] float startScale = 1.0f;
    [SerializeField] float endScale = 0.6f;

    [Header("Spawn Count")]
    [SerializeField] int minIcons = 3;
    [SerializeField] int maxIcons = 6;
    [SerializeField] float scatterPx = 35f;      // lệch nhẹ khi bay

    readonly Queue<Image> _pool = new();

    void Awake()
    {
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        if (fxRoot == null && rootCanvas != null) fxRoot = rootCanvas.transform as RectTransform;
    }

    Image GetOne()
    {
        if (_pool.Count > 0)
        {
            var img = _pool.Dequeue();
            img.gameObject.SetActive(true);
            img.rectTransform.localScale = Vector3.one;   
            return img;
        }

        var ins = Instantiate(iconPrefab, fxRoot);
        ins.rectTransform.localScale = Vector3.one;      
        return ins;
    }

    void Return(Image img)
    {
        img.gameObject.SetActive(false);
        _pool.Enqueue(img);
    }

    /// <summary>
    /// Bay icon từ 1 RectTransform (button) lên 1 RectTransform (rose UI)
    /// </summary>
    public void Play(RectTransform from, RectTransform to, int? countOverride = null)
    {
        if (from == null || to == null || iconPrefab == null || rootCanvas == null) return;

        int count = countOverride ?? Random.Range(minIcons, maxIcons + 1);

        for (int i = 0; i < count; i++)
        {
            var img = GetOne();
            img.transform.SetAsLastSibling();

            // convert from/to -> local point in fxRoot
            Vector2 start = WorldToLocalPoint(from);
            Vector2 end = WorldToLocalPoint(to);

            // scatter nhẹ cho đẹp
            Vector2 s = start + Random.insideUnitCircle * scatterPx;
            Vector2 e = end + Random.insideUnitCircle * (scatterPx * 0.2f);

            StartCoroutine(CoFly(img.rectTransform, img, s, e, i * 0.03f));
        }
    }

    Vector2 WorldToLocalPoint(RectTransform rt)
    {
        // lấy screen point từ world của rect
        Vector3 world = rt.TransformPoint(rt.rect.center);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(rootCanvas.worldCamera, world);

        RectTransform parent = fxRoot != null ? fxRoot : (RectTransform)rootCanvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, rootCanvas.worldCamera, out var local);
        return local;
    }

    IEnumerator CoFly(RectTransform iconRT, Image iconImg, Vector2 start, Vector2 end, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        iconRT.anchoredPosition = start;
        iconRT.localScale = Vector3.one * startScale;
        iconImg.color = new Color(1, 1, 1, 1);

        // control point tạo đường cong (Bezier)
        Vector2 mid = (start + end) * 0.5f;
        mid.y += arcHeight;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.001f, duration);
            float u = EaseOutCubic(Mathf.Clamp01(t));

            // Quadratic Bezier
            Vector2 p = Bezier2(start, mid, end, u);
            iconRT.anchoredPosition = p;

            float sc = Mathf.Lerp(startScale, endScale, u);
            iconRT.localScale = Vector3.one * sc;

            // fade nhẹ cuối hành trình
            float a = (u < 0.85f) ? 1f : Mathf.Lerp(1f, 0f, (u - 0.85f) / 0.15f);
            iconImg.color = new Color(1, 1, 1, a);

            yield return null;
        }

        Return(iconImg);
    }

    static Vector2 Bezier2(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float it = 1f - t;
        return it * it * a + 2f * it * t * b + t * t * c;
    }

    static float EaseOutCubic(float x)
    {
        float a = 1f - x;
        return 1f - a * a * a;
    }
}
