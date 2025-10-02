using UnityEngine;

public class HomeStageListUI : MonoBehaviour
{
    void OnEnable()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        foreach (var e in GetComponentsInChildren<StageEntry>(true))
            e.Refresh();
    }
}
