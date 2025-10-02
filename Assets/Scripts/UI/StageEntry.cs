using UnityEngine;
using TMPro;

public class StageEntry : MonoBehaviour
{
    [Range(1, GameManager.NumStages)]
    public int stageNumber = 1;

    public TMP_Text label;
    public bool showPrefix = true;
    public string format = "★ {0}/{1}";

    void Reset()
    {
        if (!label) label = GetComponentInChildren<TMP_Text>();
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        int idx = stageNumber - 1;
        int best = GetBestStarsSafe(idx);
        int max = GameManager.MaxStarsPerStage;

        if (label)
        {
            string head = showPrefix ? $"Stage {stageNumber}   " : "";
            label.text = $"{head}{string.Format(format, best, max)}";
        }
    }

    // GameManager가 아직 없을 수도 있는 Home 첫 진입 대비
    int GetBestStarsSafe(int idx)
    {
        var gm = GameManager.Instance;
        if (gm != null) return gm.GetBestStarsForStage(idx);

        // ★ GM이 아직 없으면 Editor/Build와 동일한 saveSlot 규칙으로 키 맞춤
#if UNITY_EDITOR
        const string slot = "dev";
#else
        const string slot = "prod";
#endif
        return PlayerPrefs.GetInt($"{slot}_stageBest_{idx}", 0);
    }
}
