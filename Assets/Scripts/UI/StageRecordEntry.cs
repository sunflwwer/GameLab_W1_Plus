using UnityEngine;
using TMPro;

public class StageRecordEntry : MonoBehaviour
{
    [Range(1, GameManager.NumStages)]
    public int stageNumber = 1;

    [Header("Labels (assign in Inspector)")]
    [SerializeField] TMP_Text bestTimeLabel;     // 예: "Best Time: 00:12.345"
    [SerializeField] TMP_Text bestDeathsLabel;   // 예: "Fewest Deaths: 3"

    [Header("Text format")]
    [SerializeField] string timePrefix = "Best Time: ";
    [SerializeField] string deathsPrefix = "Fewest Deaths: ";
    [SerializeField] string noRecordText = "—";

    void Reset()
    {
        // 편의상 자동 할당 시도(자식에 텍스트 2개만 있는 단순 구조일 때)
        var tmps = GetComponentsInChildren<TMP_Text>();
        if (!bestTimeLabel && tmps.Length > 0) bestTimeLabel = tmps[0];
        if (!bestDeathsLabel && tmps.Length > 1) bestDeathsLabel = tmps[1];
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        int idx = stageNumber - 1;

        float bestTime = GetBestRunTimeSafe(idx);
        int bestDeaths = GetBestRunDeathsSafe(idx);

        if (bestTimeLabel)
            bestTimeLabel.text = timePrefix + (bestTime > 0f ? FormatTime(bestTime) : noRecordText);

        if (bestDeathsLabel)
        {
            bool hasRecord = bestTime > 0f; // 시간 기록이 있어야 클리어한 것
            bestDeathsLabel.text = deathsPrefix + (hasRecord ? bestDeaths.ToString() : noRecordText);
        }

    }


    float GetBestRunTimeSafe(int idx)
    {
        var gm = GameManager.Instance;
        if (gm != null) return gm.GetBestRunTimeForStage(idx);
#if UNITY_EDITOR
        const string slot = "dev";
#else
    const string slot = "prod";
#endif
        return PlayerPrefs.GetFloat($"{slot}_bestRunTime_{idx}", 0f);
    }

    int GetBestRunDeathsSafe(int idx)
    {
        var gm = GameManager.Instance;
        if (gm != null) return gm.GetBestRunDeathsForStage(idx);
#if UNITY_EDITOR
        const string slot = "dev";
#else
    const string slot = "prod";
#endif
        return PlayerPrefs.GetInt($"{slot}_bestRunDeaths_{idx}", 0);
    }


    // mm:ss.mmm 포맷
    string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        float seconds = t - minutes * 60f;
        return $"{minutes:00}:{seconds:00.000}";
    }
}
