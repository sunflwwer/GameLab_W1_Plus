using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public static UI Instance; // 싱글톤

    [Header("큰 버전 UI")]
    public TextMeshProUGUI deathCountText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI starCountText;

    [Header("작은 버전 UI (기본 비활성화)")]
    public TextMeshProUGUI deathCountTextSmall;
    public TextMeshProUGUI timeTextSmall;
    public TextMeshProUGUI starCountTextSmall;

    [Header("기타 UI")]
    public TextMeshProUGUI stageOneText;
    public TextMeshProUGUI clearText;
    public TextMeshProUGUI failText;

    [Header("Explain Panel")]
    [SerializeField] GameObject explainPanel;
    [SerializeField] bool toggleMode = false; // 토글 모드(true) / 홀드 모드(false)

    [Header("Cooldown UI")]
    [SerializeField] GameObject dashCooldownGO;
    [SerializeField] GameObject flashCooldownGO;

    [Header("End Overlay/Buttons")]
    [SerializeField] GameObject endPanel;   // 클리어/실패 오버레이 그룹
    [SerializeField] Button homeButton;
    [SerializeField] Button restartButton;

    // UI 클래스 상단 다른 TMP 레퍼런스들 옆
    [Header("Extra HUD Text")]
    [SerializeField] TextMeshProUGUI extraInfoText;  // ← 새로 추가: 숨기고 싶은 그 텍스트

    [Header("Overlay Snapshot")]
    [SerializeField] bool useOverlayStats = false;
    [SerializeField] float overlayTime = 0f;
    [SerializeField] int overlayDeaths = 0;
    [SerializeField] int overlayStars = 0;

    [Header("Big HUD Root (optional)")]
    [SerializeField] GameObject bigHudRoot;   // ← 큰 시간/죽음/스타/쿨타임/추가 텍스트가 들어있는 공통 부모


    public void SetOverlayStats(float time, int deaths, int stars)
    {
        overlayTime = time;
        overlayDeaths = deaths;
        overlayStars = stars;
        useOverlayStats = true;
    }

    public void ClearOverlayStats()
    {
        useOverlayStats = false;
    }



    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(ShowStageOneText());
        if (explainPanel) explainPanel.SetActive(false);

        InitEndUI();

        // 버튼 리스너는 UI 쪽에서 연결(씬 전환 후에도 유지됨)
        if (homeButton)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(() => GameManager.Instance.OnClickHomeButton());
        }
        if (restartButton)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => GameManager.Instance.OnClickRestartButton());
        }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // 오버레이(클리어/실패) 열려 있고 스냅샷이 있으면 스냅샷 우선 표시
        bool overlayActive = (endPanel && endPanel.activeSelf) && useOverlayStats;

        float timeToShow = overlayActive ? overlayTime : gm.PlayTime;
        int deathsToShow = overlayActive ? overlayDeaths : gm.DeathCount;
        int starsToShow = overlayActive ? overlayStars : gm.GetCurrentRunStars();

        string formattedTime = timeToShow.ToString("F3");

        // 큰 HUD
        if (deathCountText) deathCountText.text = "죽음 : " + deathsToShow;
        if (timeText) timeText.text = "시간 : " + formattedTime;

        // 작은 HUD
        if (deathCountTextSmall) deathCountTextSmall.text = "죽음 : " + deathsToShow;
        if (timeTextSmall) timeTextSmall.text = "시간 : " + formattedTime;

        // ★ 이번 러닝 별(해당 스테이지에서만 표시)
        if (gm.CurrentStageIndex >= 0)
        {
            int max = GameManager.MaxStarsPerStage;

            // 사망/클리어 오버레이 중엔 큰 별 텍스트 숨김
            bool showBigStar = !(gm.isRestarting || gm.isClearing);

            if (starCountText)
            {
                starCountText.gameObject.SetActive(showBigStar);
                if (showBigStar)
                    starCountText.text = $"별 : {starsToShow} / {max}";
            }

            if (starCountTextSmall && starCountTextSmall.gameObject.activeSelf)
                starCountTextSmall.text = $"별 : {starsToShow} / {max}";
        }
        else
        {
            if (starCountText) starCountText.gameObject.SetActive(false);
        }



        HandleTabExplain();
    }

    public void InitEndUI()
    {
        if (endPanel) endPanel.SetActive(false);
        if (homeButton) homeButton.gameObject.SetActive(false);
        if (restartButton) restartButton.gameObject.SetActive(false);

        if (clearText) clearText.gameObject.SetActive(false);
        if (failText) failText.gameObject.SetActive(false);

        if (deathCountTextSmall) deathCountTextSmall.gameObject.SetActive(false);
        if (timeTextSmall) timeTextSmall.gameObject.SetActive(false);
        if (starCountTextSmall) starCountTextSmall.gameObject.SetActive(false);

        ClearOverlayStats();                 // ★ 추가
    }



    public void ShowClearOverlayAndButtons(string message)
    {
        ShowClearText(message);
        if (endPanel) endPanel.SetActive(true);
        if (homeButton) homeButton.gameObject.SetActive(true);
        if (restartButton) restartButton.gameObject.SetActive(true);
    }

    public void ShowFailOverlayAndButtons(string message)
    {
        ShowFailText(message);
        if (endPanel) endPanel.SetActive(true);
        if (homeButton) homeButton.gameObject.SetActive(true);
        if (restartButton) restartButton.gameObject.SetActive(true);
    }

    void HandleTabExplain()
    {
        if (explainPanel == null || Keyboard.current == null) return;

        if (toggleMode)
        {
            // 탭 키로 토글
            if (Keyboard.current.tabKey.wasPressedThisFrame)
                explainPanel.SetActive(!explainPanel.activeSelf);
        }
        else
        {
            // 탭 키 홀드 동안만 표시
            bool show = Keyboard.current.tabKey.isPressed;
            if (explainPanel.activeSelf != show)
                explainPanel.SetActive(show);
        }
    }

    public void SetStandardHudActive(bool on)
    {
        if (bigHudRoot)
        {
            bigHudRoot.SetActive(on);  // 부모 한 번에 on/off → 같은 부모의 추가 텍스트도 함께 숨김
            return;
        }

        // (fallback) 부모를 안 넣었을 때는 기존처럼 개별 on/off
        if (deathCountText) deathCountText.gameObject.SetActive(on);
        if (timeText) timeText.gameObject.SetActive(on);
        if (starCountText) starCountText.gameObject.SetActive(on);
        if (dashCooldownGO) dashCooldownGO.SetActive(on);
        if (flashCooldownGO) flashCooldownGO.SetActive(on);
        if (extraInfoText) extraInfoText.gameObject.SetActive(on);
    }




    IEnumerator ShowStageOneText()
    {
        if (stageOneText != null)
        {
            stageOneText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f);
            if (stageOneText != null)
                stageOneText.gameObject.SetActive(false);
        }
    }

    public void ShowClearText(string message)
    {
        if (clearText != null)
        {
            // 큰 HUD 통째로 숨김 (부모 기준)
            SetStandardHudActive(false);

            // 작은 HUD 표시
            if (deathCountTextSmall) deathCountTextSmall.gameObject.SetActive(true);
            if (timeTextSmall) timeTextSmall.gameObject.SetActive(true);
            if (starCountTextSmall) starCountTextSmall.gameObject.SetActive(true);

            clearText.text = message;
            clearText.gameObject.SetActive(true);
        }
    }

    public void ShowFailText(string message)
    {
        if (failText != null)
        {
            // 큰 HUD 통째로 숨김 (부모 기준)
            SetStandardHudActive(false);

            // 작은 HUD 표시
            if (deathCountTextSmall) deathCountTextSmall.gameObject.SetActive(true);
            if (timeTextSmall) timeTextSmall.gameObject.SetActive(true);
            if (starCountTextSmall) starCountTextSmall.gameObject.SetActive(true);

            failText.text = message;
            failText.gameObject.SetActive(true);
        }
    }
}
