using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PrologueTextPlayer : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] TMP_Text textTarget;     // CanvasGroup의 자식
    [SerializeField] CanvasGroup canvasGroup; // 텍스트 그룹만 페이드

    [Header("Texts (Inspector or Inline)")]
    [TextArea(2, 4)] public string[] lines;
    [SerializeField] bool useInlineLines = true;
    [TextArea(2, 4)]
    [SerializeField]
    string[] inlineLines = new string[]
    {
        "욕심쟁이 커비는 유명한 별 수집가입니다.",
        "근데 커비의 별들이 몽땅 도망갔어요!!!",
        "좋아, 이놈들… 다시 전부 잡아오겠습니다!"
    };

    [Header("Timings (sec)")]
    [SerializeField] float startDelay = 0.25f;     // ← 시작 전 잠깐 정적
    [SerializeField] bool useFirstFadeOverride = true;
    [SerializeField] float firstFadeInTime = 0.9f; // ← 첫 컷만 더 길게
    [SerializeField] float fadeInTime = 0.6f;
    [SerializeField] float holdTime = 2.0f;     // 요청: 유지 2초
    [SerializeField] float fadeOutTime = 0.6f;

    [Header("Behavior")]
    [SerializeField] bool playOnStart = true;
    [SerializeField] bool allowSkip = true;
    [SerializeField] string nextSceneName = "";

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup) canvasGroup.alpha = 0f;

        if ((lines == null || lines.Length == 0) && useInlineLines)
            lines = inlineLines;

        if (!textTarget) Debug.LogWarning("[PrologueTextPlayer] textTarget가 비어 있습니다.");
        if (!canvasGroup) Debug.LogWarning("[PrologueTextPlayer] canvasGroup이 비어 있습니다.");
    }

    void Start()
    {
        if (playOnStart) StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        if (textTarget == null || canvasGroup == null || lines == null || lines.Length == 0)
            yield break;

        // 씬 시작 직후 약간의 정적
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        for (int i = 0; i < lines.Length; i++)
        {
            textTarget.text = lines[i];

            // 첫 컷만 별도 페이드 시간 사용 가능
            float fin = (useFirstFadeOverride && i == 0) ? firstFadeInTime : fadeInTime;

            // Fade In
            yield return FadeTo(1f, fin);

            // Hold (유지) 또는 스킵
            float t = 0f;
            while (t < holdTime)
            {
                if (allowSkip && IsSkipPressed())
                    break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // Fade Out
            yield return FadeTo(0f, fadeOutTime);
        }

        // ✅ 기록을 먼저 남기고, 한 번만 로드
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            var gm = GameManager.Instance;
            if (gm != null && !gm.PrologCleared)
                gm.MarkPrologCleared();   // 프롤로그 완료 저장(여러 번 호출돼도 안전)

            SceneManager.LoadScene(nextSceneName);   // 예: "Home"
        }


    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float start = canvasGroup.alpha;
        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }

    bool IsSkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Gamepad.current != null && (Gamepad.current.aButton.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)) return true;
        return false;
#else
        if (Input.anyKeyDown) return true;
        if (Input.GetMouseButtonDown(0)) return true;
        return false;
#endif
    }
}
