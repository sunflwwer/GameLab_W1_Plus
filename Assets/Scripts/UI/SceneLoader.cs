using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoader : MonoBehaviour
{
    [SerializeField] string stagePrefix = "Stage ";
    [SerializeField] string startSceneName = "Start"; // ← Start 씬 이름

    public void LoadByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] sceneName is empty");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    public void LoadStage(int stageNumber)
    {
        if (stageNumber <= 0)
        {
            Debug.LogError("[SceneLoader] stageNumber must be > 0");
            return;
        }
        SceneManager.LoadScene($"{stagePrefix}{stageNumber}");
    }

    // ← Back 버튼용: Start 씬으로
    public void LoadStart()
    {
        SceneManager.LoadScene(startSceneName);
    }

    // ← Exit 버튼용: 종료
    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
