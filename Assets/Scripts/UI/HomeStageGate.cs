using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class HomeStageGate : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button prologButton;     // 항상 활성/실행
    [SerializeField] Button[] stageButtons;   // Stage 1..N

    [Header("Optional Lock Icons (same length as stageButtons)")]
    [SerializeField] GameObject[] lockIcons;

    [Header("Prologue")]
    [SerializeField] string prologSceneName = "Prologue";

    void Awake()
    {
        if (prologButton)
        {
            prologButton.onClick.RemoveAllListeners();
            prologButton.onClick.AddListener(() =>
            {
                SceneManager.LoadScene(prologSceneName);
            });
        }
    }

    void OnEnable()
    {
        // 바로 Refresh() 대신, 다음 프레임에 갱신
        StartCoroutine(RefreshNextFrame());
    }

    IEnumerator RefreshNextFrame()
    {
        // 한 프레임 대기: GameManager.Awake()에서 LoadBestStars()가 끝나도록 여유를 줌
        yield return null;

        // 혹시라도 늦게 뜨면, Instance 생성까지 대기
        if (GameManager.Instance == null)
            yield return new WaitUntil(() => GameManager.Instance != null);

        Refresh();
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;

        if (prologButton)
        {
            prologButton.gameObject.SetActive(true);
            prologButton.interactable = true; // 프롤로그는 항상 다시 보기 가능
        }

        for (int i = 0; i < stageButtons.Length; i++)
        {
            bool unlocked = (gm != null) && gm.IsStageUnlocked(i);
            var btn = stageButtons[i];
            if (btn)
            {
                btn.gameObject.SetActive(true);
                btn.interactable = unlocked; // Stage1은 프로로그 클리어 시 true
            }

            if (lockIcons != null && i < lockIcons.Length && lockIcons[i] != null)
                lockIcons[i].SetActive(!unlocked);
        }


        // HomeStageGate.Refresh() 마지막 부분에 추가
        foreach (var e in GetComponentsInChildren<StageEntry>(true))
            e.Refresh();

        foreach (var r in GetComponentsInChildren<StageRecordEntry>(true))
            r.Refresh();

    }
}
