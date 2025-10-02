using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StartMenu : MonoBehaviour
{
    [SerializeField] private string homeSceneName = "Home";

    // ★ GameManager, UI 등을 담은 프리팹(Inside: GameManager + UI + EventSystem 권장)
    [SerializeField] private GameObject systemsPrefab;

    public void OnClickStart()
    {
        // ★ 부트스트랩: 아직 없다면 한 번만 생성 (DontDestroyOnLoad 권장)
        if (GameManager.Instance == null && systemsPrefab != null)
        {
            Instantiate(systemsPrefab);
        }

        // Home 씬으로 이동
        SceneManager.LoadScene(homeSceneName);
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
