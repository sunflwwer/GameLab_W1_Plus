using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // 저장을 할지 말지(에디터 테스트 땐 꺼두세요)
    [SerializeField] private bool persistProgress = true;

    // 저장 슬롯(접두사). 테스트는 "dev", 릴리즈는 "prod"처럼 분리 가능
    [SerializeField]
    string saveSlot =
#if UNITY_EDITOR
        "dev";
#else
    "prod";
#endif

    private string Key(int i) => $"{saveSlot}_stageBest_{i}";


    // 타이머/상태
    private bool shouldCountTime = false;
    public int DeathCount { get; private set; } = 0;
    public float PlayTime { get; private set; } = 0f;
    public bool isRestarting = false;
    public bool isClearing = false;

    // 스테이지/별 설정
    public const int NumStages = 10;
    public const int MaxStarsPerStage = 5;

    // 스테이지별 최대 별 기록(영구)
    public int[] StageBestStars = new int[NumStages];

    // 현재 스테이지/현재 러닝 별 수
    private int currentStageIndex = -1; // -1이면 스테이지가 아닌 씬(Home 등)
    private int currentRunStars = 0;  // 씬 로드/재시작마다 0으로

    // GameManager.cs 상단 필드들 근처에 추가
    public const int StarsToUnlockNext = 3;

    // per-run(현재 러닝) 기록: 씬 재시작해도 이어지게 유지
    private float[] stageRunTime = new float[NumStages];
    private int[] stageRunDeaths = new int[NumStages];


    [SerializeField] private bool prologCleared = false; // 프롤로그 클리어 여부
    private string PrologKey => $"{saveSlot}_prologCleared";

    public bool PrologCleared => prologCleared || IsPrologForced();

    private bool IsPrologForced()
    {
#if UNITY_EDITOR
        return forcePrologClearedInEditor;
#else
    return false;
#endif
    }


    // GameManager 클래스 내부 어딘가 적당한 곳 (예: 저장 관련 필드 아래)
    [Header("Debug / Test")]
    [Tooltip("에디터에서만: 체크하면 프롤로그를 이미 본 것으로 간주(세이브엔 기록하지 않음).")]
    [SerializeField] private bool forcePrologClearedInEditor = false;


    // keys
    private string BestRunTimeKey(int i) => $"{saveSlot}_bestRunTime_{i}";
    private string BestRunDeathsKey(int i) => $"{saveSlot}_bestRunDeaths_{i}";

    // records (Best Run 한 쌍)
    public float[] StageBestRunTime = new float[NumStages];
    public int[] StageBestRunDeaths = new int[NumStages];

    // getters
    public float GetBestRunTimeForStage(int idx) => (idx >= 0 && idx < NumStages) ? StageBestRunTime[idx] : 0f;
    public int GetBestRunDeathsForStage(int idx) => (idx >= 0 && idx < NumStages) ? StageBestRunDeaths[idx] : 0;


    // 조회용 (과거 API 유지하려면 BestRun으로 라우팅)
    public float GetBestTimeForStage(int idx) => GetBestRunTimeForStage(idx);
    public int GetBestDeathsForStage(int idx) => GetBestRunDeathsForStage(idx);

    public float GetCurrentRunTime() => (currentStageIndex >= 0) ? stageRunTime[currentStageIndex] : 0f;
    public int GetCurrentRunDeaths() => (currentStageIndex >= 0) ? stageRunDeaths[currentStageIndex] : 0;


    // 외부(프롤로그 엔딩에서) 호출: 프롤로그 클리어 표시 + 저장
    public void MarkPrologCleared()
    {
        prologCleared = true;
        SaveProgress(); // 아래 새 함수
    }

    // 스테이지 해금 규칙: idx는 0-based (Stage 1 => 0)
    public bool IsStageUnlocked(int idx)
    {
        if (idx < 0 || idx >= NumStages) return false;

        // Stage 1(=0)은 프롤로그 클리어 시 해금
        if (idx == 0) return PrologCleared;


        // 그 외는 바로 이전 스테이지의 별이 StarsToUnlockNext개 이상일 때 해금
        return StageBestStars[idx - 1] >= StarsToUnlockNext;
    }



    public int CurrentStageIndex => currentStageIndex;
    public int GetCurrentRunStars() => currentRunStars;
    public int GetBestStarsForStage(int idx) => (idx >= 0 && idx < NumStages) ? StageBestStars[idx] : 0;

    private int SceneToStageIndex(string sceneName)
    {
        // "Stage 1" ~ "Stage 10"
        if (sceneName.StartsWith("Stage "))
        {
            if (int.TryParse(sceneName.Substring(6), out int n))
                if (n >= 1 && n <= NumStages) return n - 1;
        }
        return -1;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBestStars();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        isRestarting = false;
        isClearing = false;

        string sceneName = scene.name;

        // 현재 스테이지 인덱스 계산 ("Stage 1" ~ "Stage 10"만 0~9 반환)
        currentStageIndex = SceneToStageIndex(sceneName);
        bool isStageScene = (currentStageIndex >= 0);

        // 러닝 별 수는 씬 로드시 0으로
        currentRunStars = 0;

        // 스타트/홈/타이틀 등: 커서 보이기 + 타이머 중지
        // 스테이지 씬: 커서 숨기기 + 타이머 시작
        if (isStageScene)
        {
            // ▶ 같은 스테이지 재로드여도 "이어 달리기"
            PlayTime = stageRunTime[currentStageIndex];
            DeathCount = stageRunDeaths[currentStageIndex];

            shouldCountTime = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            UI.Instance?.SetStandardHudActive(true);
        }
        else
        {
            PlayTime = 0f;
            DeathCount = 0;
            shouldCountTime = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UI.Instance?.SetStandardHudActive(false); // HUD를 숨기고 싶으면
        }

        EnsureEventSystem();
        UI.Instance?.InitEndUI();
    }


    private void Update()
    {
        if (shouldCountTime && !isRestarting && !isClearing)
        {
            PlayTime += Time.deltaTime;

            if (currentStageIndex >= 0)
                stageRunTime[currentStageIndex] = PlayTime; // ▶ 러닝 시간 저장
        }
    }


public void SpawnPlayer()
{
    if (isRestarting || isClearing) return;
    
    UI.Instance?.ShowFailOverlayAndButtons("Game Over");

    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;

    DeathCount++;
    if (currentStageIndex >= 0)
        stageRunDeaths[currentStageIndex] = DeathCount; // ▶ 러닝 데스 저장

    ExplodePlayer();

    isRestarting = true; // 자동 재시작 없음 (버튼으로만)
}



    private void ExplodePlayer()
    {
        var effect = FindFirstObjectByType<PlayerEffect>();
        if (effect) effect.TriggerParticle(EffectType.Explosion);
    }
    private void NextEffect()
    {
        var effect = FindFirstObjectByType<PlayerEffect>();
        if (effect) effect.TriggerParticle(EffectType.NextStage);
    }
    private void ClearEffect()
    {
        var effect = FindFirstObjectByType<PlayerEffect>();
        if (effect) effect.TriggerParticle(EffectType.Clear);
    }


    public void StageClear()
    {
        if (isClearing || isRestarting) return;

        isClearing = true;
        shouldCountTime = false;
        NextEffect();

        if (currentStageIndex >= 0)
        {
            if (currentRunStars > StageBestStars[currentStageIndex])
            {
                StageBestStars[currentStageIndex] = Mathf.Clamp(currentRunStars, 0, MaxStarsPerStage);
            }

            float runTime = stageRunTime[currentStageIndex];
            int runDeaths = stageRunDeaths[currentStageIndex];
            int curStars = currentRunStars;

            // Best Run: 시간 우선, 동률이면 죽음 적은 게 우선
            bool betterRun =
                (StageBestRunTime[currentStageIndex] <= 0f) ||
                (runTime < StageBestRunTime[currentStageIndex]) ||
                (Mathf.Approximately(runTime, StageBestRunTime[currentStageIndex]) &&
                 runDeaths < StageBestRunDeaths[currentStageIndex]);

            if (betterRun)
            {
                StageBestRunTime[currentStageIndex] = runTime;
                StageBestRunDeaths[currentStageIndex] = runDeaths;
            }

            SaveBestStars();
            UI.Instance?.SetOverlayStats(runTime, runDeaths, curStars);

        }

        // ▶ 다음 러닝을 위해 현재 기록 리셋
        if (currentStageIndex >= 0)
        {
            stageRunTime[currentStageIndex] = 0f;
            stageRunDeaths[currentStageIndex] = 0;
        }
        PlayTime = 0f;
        DeathCount = 0;

        UI.Instance?.ShowClearOverlayAndButtons("Clear!");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }



    public void OnClickRestartButton()
    {
        UI.Instance?.ClearOverlayStats();   // 선택: ‘스냅샷 잔상’ 방지
        Time.timeScale = 1f;
        shouldCountTime = true;
        var current = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(current);
    }
    
    public void FinalClear()
    {
        if (isClearing || isRestarting) return;
        isClearing = true;

        var playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput) playerInput.enabled = false;

        var controller = FindFirstObjectByType<PlayerController>();
        if (controller) controller.enabled = false;

        ClearEffect();
        shouldCountTime = false;

        if (currentStageIndex >= 0)
        {
            if (currentRunStars > StageBestStars[currentStageIndex])
            {
                StageBestStars[currentStageIndex] = Mathf.Clamp(currentRunStars, 0, MaxStarsPerStage);
            }

            float runTime = stageRunTime[currentStageIndex];
            int runDeaths = stageRunDeaths[currentStageIndex];

            bool betterRun =
                (StageBestRunTime[currentStageIndex] <= 0f) ||
                (runTime < StageBestRunTime[currentStageIndex]) ||
                (Mathf.Approximately(runTime, StageBestRunTime[currentStageIndex]) &&
                 runDeaths < StageBestRunDeaths[currentStageIndex]);

            if (betterRun)
            {
                StageBestRunTime[currentStageIndex] = runTime;
                StageBestRunDeaths[currentStageIndex] = runDeaths;
            }

            SaveBestStars();

        }
        // ▶ 현재 기록 리셋
        if (currentStageIndex >= 0)
        {
            stageRunTime[currentStageIndex] = 0f;
            stageRunDeaths[currentStageIndex] = 0;
        }
        PlayTime = 0f;
        DeathCount = 0;

        UI.Instance?.ShowClearOverlayAndButtons("Clear!");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ===== 아이템(별) 획득: 이번 러닝 개수만 증가 =====
    public void CollectStar(GameObject item)
    {
        if (currentStageIndex >= 0 && currentRunStars < MaxStarsPerStage)
            currentRunStars++;

        if (item) Destroy(item);
        // 베스트 저장은 클리어 시점에서만
    }

    public void OnClickHomeButton()
    {
        UI.Instance?.ClearOverlayStats();   // 선택: 동일 이유
        Time.timeScale = 1f;
        shouldCountTime = false;
        if (currentStageIndex >= 0)
        {
            stageRunTime[currentStageIndex] = 0f;
            stageRunDeaths[currentStageIndex] = 0;
        }
        PlayTime = 0f;
        SaveBestStars();
        SceneManager.LoadScene("Home");
    }


    // EventSystem 보장
    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }
    }
    public void SaveProgress()
    {
        if (!persistProgress) return;

        for (int i = 0; i < NumStages; i++)
            PlayerPrefs.SetInt(Key(i), StageBestStars[i]);

        for (int i = 0; i < NumStages; i++)
        {
            PlayerPrefs.SetFloat(BestRunTimeKey(i), StageBestRunTime[i]);
            PlayerPrefs.SetInt(BestRunDeathsKey(i), StageBestRunDeaths[i]);
        }


        PlayerPrefs.SetInt(PrologKey, prologCleared ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SaveBestStars()
    {
        SaveProgress();
    }


    public void LoadBestStars()
    {
        if (!persistProgress)
        {
            for (int i = 0; i < NumStages; i++)
            {
                StageBestStars[i] = 0;
                StageBestRunTime[i] = 0f;    // ★ BestRun으로 변경
                StageBestRunDeaths[i] = 0;     // ★ BestRun으로 변경
            }
            prologCleared = false;
            return;
        }


        for (int i = 0; i < NumStages; i++)
            StageBestStars[i] = PlayerPrefs.GetInt(Key(i), 0);

        for (int i = 0; i < NumStages; i++)
        {
            StageBestRunTime[i] = PlayerPrefs.GetFloat(BestRunTimeKey(i), 0f);
            StageBestRunDeaths[i] = PlayerPrefs.GetInt(BestRunDeathsKey(i), 0);
        }


        prologCleared = PlayerPrefs.GetInt(PrologKey, 0) == 1;
    }

    [ContextMenu("Clear Saved Stars (Current Slot)")]
    public void ClearSavedStars()
    {
        for (int i = 0; i < NumStages; i++)
        {
            PlayerPrefs.DeleteKey(Key(i));                 // ★별
            PlayerPrefs.DeleteKey(BestRunTimeKey(i));
            PlayerPrefs.DeleteKey(BestRunDeathsKey(i));

        }
        PlayerPrefs.DeleteKey(PrologKey);
        PlayerPrefs.Save();
    }


}
