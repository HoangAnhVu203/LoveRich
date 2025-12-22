using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollSnapToCenter : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [Header("Refs")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;

    [Header("Items (Thumbs)")]
    [SerializeField] private List<RectTransform> itemRects = new();

    [Header("Optional: Update info when centered")]
    [SerializeField] private CharacterInfoPanelUI infoPanel;
    [SerializeField] private List<CharacterThumbItemUI> itemUIs = new(); // cùng thứ tự với itemRects

    [Header("Snap Settings")]
    [SerializeField] private float snapDuration = 0.18f;     // 0.12 - 0.25 tuỳ cảm giác
    [SerializeField] private float snapThreshold = 5f;       // px: nếu gần rồi thì thôi
    [SerializeField] private bool updateWhileDragging = true; // kéo là đổi info luôn
    [SerializeField] private bool highlightFocused = true;

    public System.Action<CharacterData> OnCenteredChanged;


    private bool isDragging;
    private Coroutine snapCR;
    private int currentIndex = -1;

    void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            viewport = scrollRect.viewport;
            content = scrollRect.content;
        }
    }

    void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (viewport == null && scrollRect != null) viewport = scrollRect.viewport;
        if (content == null && scrollRect != null) content = scrollRect.content;
    }

    void Start()
    {
        // nếu bạn đã set itemUIs bằng code rồi thì sẽ gọi SetItems(...) từ ngoài
        // còn nếu set sẵn trong inspector thì init lần đầu:
        RefreshImmediate();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        StopSnap();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (updateWhileDragging)
            RefreshImmediate();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        SnapToClosest();
    }

    /// <summary>
    /// Gọi sau khi spawn xong.
    /// itemUIs: list CharacterThumbItemUI theo đúng thứ tự.
    /// </summary>
    public void SetItems(List<CharacterThumbItemUI> uis)
    {
        itemUIs = uis;
        itemRects = new List<RectTransform>(uis.Count);
        for (int i = 0; i < uis.Count; i++)
            itemRects.Add((RectTransform)uis[i].transform);

        currentIndex = -1;
        RefreshImmediate();
        SnapToClosest(); // optional: vào panel là snap luôn cho đẹp
    }

    private void SnapToClosest()
    {
        if (itemRects == null || itemRects.Count == 0) return;

        int idx = FindClosestIndexToCenter();
        Vector2 target = GetAnchoredPosToCenterItem(idx);

        // nếu đã gần mục tiêu thì chỉ cần update state
        if (Vector2.Distance(content.anchoredPosition, target) <= snapThreshold)
        {
            ApplyFocused(idx);
            return;
        }

        StopSnap();
        snapCR = StartCoroutine(SnapCoroutine(target, idx));
    }

    private IEnumerator SnapCoroutine(Vector2 targetAnchoredPos, int targetIndex)
    {
        // dập inertia để không “trôi”
        scrollRect.velocity = Vector2.zero;

        Vector2 start = content.anchoredPosition;
        float t = 0f;

        // ease out
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.001f, snapDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // cubic ease out
            content.anchoredPosition = Vector2.LerpUnclamped(start, targetAnchoredPos, eased);
            yield return null;
        }

        content.anchoredPosition = targetAnchoredPos;
        ApplyFocused(targetIndex);
        snapCR = null;
    }

    private void StopSnap()
    {
        if (snapCR != null)
        {
            StopCoroutine(snapCR);
            snapCR = null;
        }
    }

    private void RefreshImmediate()
    {
        if (itemRects == null || itemRects.Count == 0) return;
        int idx = FindClosestIndexToCenter();
        ApplyFocused(idx);
    }

    private int FindClosestIndexToCenter()
    {
        Vector3 viewportCenterWorld = viewport.TransformPoint(viewport.rect.center);

        float best = float.MaxValue;
        int bestIdx = 0;

        for (int i = 0; i < itemRects.Count; i++)
        {
            RectTransform rt = itemRects[i];
            Vector3 itemCenterWorld = rt.TransformPoint(rt.rect.center);

            float d = (itemCenterWorld - viewportCenterWorld).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestIdx = i;
            }
        }

        return bestIdx;
    }

    /// <summary>
    /// Tính anchoredPosition cần thiết để đưa item target vào đúng giữa viewport.
    /// </summary>
    private Vector2 GetAnchoredPosToCenterItem(int idx)
    {
        Vector3 viewportCenterWorld = viewport.TransformPoint(viewport.rect.center);
        Vector3 itemCenterWorld = itemRects[idx].TransformPoint(itemRects[idx].rect.center);

        // chuyển chênh lệch world -> local của content
        Vector3 deltaWorld = viewportCenterWorld - itemCenterWorld;
        Vector3 deltaLocal = content.InverseTransformVector(deltaWorld);

        // content.anchoredPosition dùng local XY
        return content.anchoredPosition + new Vector2(deltaLocal.x, 0f);
    }

    private void ApplyFocused(int idx)
    {
        if (idx == currentIndex) return;
        currentIndex = idx;

        // Update info panel
        if (infoPanel != null && itemUIs != null && idx >= 0 && idx < itemUIs.Count)
            infoPanel.Show(itemUIs[idx].Data);

        if (itemUIs != null && idx >= 0 && idx < itemUIs.Count)
        {
            OnCenteredChanged?.Invoke(itemUIs[idx].Data);
        }

        // Highlight
        if (highlightFocused && itemUIs != null)
        {
            for (int i = 0; i < itemUIs.Count; i++)
                itemUIs[i].SetFocused(i == idx);
        }
    }
}
