using UnityEngine;

public class StarRotate : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;
    // 1초에 90도 회전 (자연스럽게 보이려면 60~120 정도 추천)

    void Update()
    {
        // Y축 기준으로 회전
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }
}
