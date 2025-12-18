using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [Tooltip("시뮬레이션 속도 배율 (1 = 정상 속도)")]
    [Range(1f, 100f)] // 너무 높으면 물리 엔진이 불안정해질 수 있음
    public float timeScale = 10f; // 예: 10배속

    void Awake()
    {
        // 에디터에서 테스트할 때만 적용 (빌드 시에는 Time.timeScale이 유지됨)
#if UNITY_EDITOR
        Time.timeScale = timeScale;
        Debug.Log($"Time Scale set to: {Time.timeScale}");
#endif

        // (선택 사항) 빌드된 환경에서도 항상 특정 배속으로 실행하고 싶다면
        // #if UNITY_EDITOR ... #endif 부분을 지우고 아래 줄만 남기세요.
        // Time.timeScale = timeScale; 
    }

    // (선택 사항) 게임 실행 중 Inspector에서 값을 바꿔도 바로 적용되도록 Update 추가
    void Update()
    {
        // 에디터에서만 실시간 변경 가능하도록
#if UNITY_EDITOR
        if (Time.timeScale != timeScale)
        {
            Time.timeScale = timeScale;
            Debug.Log($"Time Scale changed to: {Time.timeScale}");
        }
#endif
    }
}
