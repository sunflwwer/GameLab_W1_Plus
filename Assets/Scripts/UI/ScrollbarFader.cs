using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 스크롤이 실제로 움직일 때만 수직 스크롤바를 보여준다.
/// - 마우스를 올려둘 필요 없음
/// - 콘텐츠가 짧아 스크롤 불가면 자동으로 숨김(ScrollRect의 Auto Hide와 병행)
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class ScrollbarFader : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] ScrollRect scrollRect;     // Scroll View의 ScrollRect
    [SerializeField] Scrollbar verticalBar;     // Scrollbar Vertical
    [SerializeField] CanvasGroup barGroup;      // Scrollbar Vertical의 CanvasGroup

    [Header("Behavior")]
    [SerializeField] float velocityThreshold = 5f; // 이 속도 이상이면 “스크롤 중”으로 판단
    [SerializeField] float posDeltaThreshold = 0.5f; // 위치 변화로도 판단(휠/키보드 대비)
    [SerializeField] float hideDelay = 0.6f;        // 멈춘 뒤 숨기기까지 대기
    [SerializeField] float fadeTime = 0.15f;       // 페이드 시간

    float lastActiveTime;
    float prevPosY;
    Coroutine fadeCo;

    void Reset()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    void OnEnable()
    {
        if (scrollRect && scrollRect.content)
            prevPosY = scrollRect.content.anchoredPosition.y;

        // 시작은 숨김
        SetVisibleImmediate(false);
    }

    void Update()
    {
        if (!scrollRect || !scrollRect.content) return;

        // 콘텐츠가 뷰포트보다 커야 스크롤바가 의미 있음
        bool scrollable = IsScrollable();
        if (!scrollable)
        {
            SetVisibleImmediate(false);
            return;
        }

        // 스크롤 중인지 판정 (속도 또는 위치 변화)
        float posY = scrollRect.content.anchoredPosition.y;
        bool moving = Mathf.Abs(scrollRect.velocity.y) > velocityThreshold
                      || Mathf.Abs(posY - prevPosY) > posDeltaThreshold;

        if (moving)
        {
            lastActiveTime = Time.unscaledTime;
            SetVisible(true);
        }
        else if (Time.unscaledTime - lastActiveTime > hideDelay)
        {
            SetVisible(false);
        }

        prevPosY = posY;
    }

    bool IsScrollable()
    {
        var vp = scrollRect.viewport.rect.height;
        var ct = scrollRect.content.rect.height;
        return ct > vp + 1f; // 약간 여유치
    }

    void SetVisible(bool v)
    {
        if (!barGroup || !verticalBar) return;

        if (fadeCo != null) StopCoroutine(fadeCo);
        verticalBar.gameObject.SetActive(true); // 페이드 동안은 켜둠
        fadeCo = StartCoroutine(FadeTo(v ? 1f : 0f, v));
    }

    void SetVisibleImmediate(bool v)
    {
        if (!barGroup || !verticalBar) return;
        verticalBar.gameObject.SetActive(v);
        barGroup.alpha = v ? 1f : 0f;
        barGroup.interactable = v;
        barGroup.blocksRaycasts = v;
    }

    IEnumerator FadeTo(float a, bool enable)
    {
        float from = barGroup.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / fadeTime;
            barGroup.alpha = Mathf.Lerp(from, a, t);
            yield return null;
        }
        barGroup.alpha = a;
        barGroup.interactable = enable;
        barGroup.blocksRaycasts = enable;
        verticalBar.gameObject.SetActive(enable);
    }
}
