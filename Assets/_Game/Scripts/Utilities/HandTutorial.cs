using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HandTutorial : MonoBehaviour
{
    private const string PREF_KEY = "TUTORIAL_HAND_SHOWN_V1";
    [Header("Hand UI")]
    [SerializeField] private RectTransform hand;          // RectTransform của object bàn tay
    [SerializeField] private CanvasGroup handGroup;       // optional (để fade). Không có cũng OK.

    [Header("Targets")]
    [SerializeField] private Button addHeartBtn;
    [SerializeField] private Button newFlirtBtn;

    [Header("Center Message")]
    [SerializeField] private RectTransform centerAnchor;  
    [SerializeField] private Text tutorialText;       
    [TextArea] [SerializeField] private string message = "Hold";
    [SerializeField] private float showTextSeconds = 3f;

    [Header("Motion")]
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private Vector2 handOffset = new Vector2(40f, -40f); // lệch cho đẹp (tùy chỉnh)
    [SerializeField] private bool disableRaycastBlock = true; // để hand không chặn click

    private Coroutine _flowCR;

    void Awake()
    {
        if (hand != null && disableRaycastBlock)
        {
            // Nếu hand có Image/Graphic thì tắt RaycastTarget để không chặn nút
            var g = hand.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = false;
        }

        if (tutorialText != null)
            tutorialText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (PlayerPrefs.GetInt(PREF_KEY, 0) == 1)
        {
            // HideAll();
            gameObject.SetActive(false); // optional: tắt luôn object tutorial
            return;
        }

        // Đánh dấu đã hiện (đặt sớm để tránh bật/tắt panel nhiều lần bị hiện lại)
        PlayerPrefs.SetInt(PREF_KEY, 1);
        PlayerPrefs.Save();

        StartTutorial();
    }

    void OnDisable()
    {
        StopTutorial();
    }

    public void StartTutorial()
    {
        StopTutorial();
        _flowCR = StartCoroutine(TutorialFlow());
    }

    public void StopTutorial()
    {
        if (_flowCR != null)
        {
            StopCoroutine(_flowCR);
            _flowCR = null;
        }
    }

    IEnumerator TutorialFlow()
    {
        if (hand == null || addHeartBtn == null || newFlirtBtn == null)
            yield break;

        // đảm bảo hand hiện
        SetHandVisible(true);

        // ===== STEP 1: Trỏ AddHeart, chờ click =====
        yield return MoveHandTo(addHeartBtn.transform as RectTransform, handOffset);

        bool clickedAddHeart = false;
        void OnAddHeartClick() => clickedAddHeart = true;

        addHeartBtn.onClick.AddListener(OnAddHeartClick);
        yield return new WaitUntil(() => clickedAddHeart);
        addHeartBtn.onClick.RemoveListener(OnAddHeartClick);

        // ===== STEP 2: Trỏ New Flirt, chờ click =====
        yield return MoveHandTo(newFlirtBtn.transform as RectTransform, handOffset);

        bool clickedNewFlirt = false;
        void OnNewFlirtClick() => clickedNewFlirt = true;

        newFlirtBtn.onClick.AddListener(OnNewFlirtClick);
        yield return new WaitUntil(() => clickedNewFlirt);
        newFlirtBtn.onClick.RemoveListener(OnNewFlirtClick);

        // ===== STEP 3: Ra giữa màn hình + hiện text =====
        if (centerAnchor != null)
            yield return MoveHandTo(centerAnchor, Vector2.zero);

        if (tutorialText != null)
        {
            tutorialText.text = message;
            tutorialText.gameObject.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(showTextSeconds);

        // Ẩn tất cả
        if (tutorialText != null)
            tutorialText.gameObject.SetActive(false);

        SetHandVisible(false);
    }

    IEnumerator MoveHandTo(RectTransform target, Vector2 offset)
    {
        if (hand == null || target == null) yield break;

        // Đưa hand về cùng parent canvas space: dùng anchoredPosition theo parent của hand
        // Mẹo: lấy vị trí world của target rồi convert về local của parent hand
        RectTransform parent = hand.parent as RectTransform;
        if (parent == null) yield break;

        Vector3 worldPos = target.TransformPoint(target.rect.center);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out localPoint
        );

        Vector2 start = hand.anchoredPosition;
        Vector2 end = localPoint + offset;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, moveDuration);
            float s = EaseOutCubic(Mathf.Clamp01(t));
            hand.anchoredPosition = Vector2.LerpUnclamped(start, end, s);
            yield return null;
        }

        hand.anchoredPosition = end;
    }

    void SetHandVisible(bool visible)
    {
        if (handGroup != null)
        {
            handGroup.alpha = visible ? 1f : 0f;
            handGroup.interactable = false;
            handGroup.blocksRaycasts = false;
        }
        else
        {
            hand.gameObject.SetActive(visible);
        }
    }

    static float EaseOutCubic(float x)
    {
        x = Mathf.Clamp01(x);
        float a = 1f - x;
        return 1f - a * a * a;
    }

    // public void HideAll()
    // {
    //     SetHandVisible(false);
    // }
}
