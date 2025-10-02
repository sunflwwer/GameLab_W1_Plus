using System.Collections;
using UnityEngine;

public class UIToggleEveryHalfSecond : MonoBehaviour
{
    [SerializeField] GameObject target;     // 깜빡일 텍스트 오브젝트
    [SerializeField] float interval = 0.5f; // 간격
    [SerializeField] bool useUnscaledTime = true; // 일시정지 중에도 깜빡이려면 true

    Coroutine routine;

    void OnEnable()
    {
        if (target == null)
        {
            Debug.LogWarning("[UIToggleEveryHalfSecond] target을 지정하세요.");
            return;
        }
        routine = StartCoroutine(Blink());
    }

    void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    IEnumerator Blink()
    {
        while (true)
        {
            target.SetActive(!target.activeSelf);

            if (useUnscaledTime)
            {
                float t = 0f;
                while (t < interval) { t += Time.unscaledDeltaTime; yield return null; }
            }
            else
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
