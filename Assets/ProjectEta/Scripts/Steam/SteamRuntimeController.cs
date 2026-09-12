using System.IO;
using UnityEngine;

namespace ProjectEta.Steam
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class SteamRuntimeController : MonoBehaviour
    {
        private static SteamRuntimeController instance; // Runtime 단일 인스턴스

        [Header("Steam Runtime")]
        [SerializeField] private uint appId = 480; // Steam AppID
        [SerializeField] private bool dontDestroyOnLoad = true; // 씬 전환 유지 여부

        [Header("Steam Cloud")]
        [SerializeField] private bool enableCloudSync = true; // Cloud 동기화 사용 여부
        [SerializeField, Min(0.5f)] private float cloudSyncIntervalSeconds = 2f; // Cloud 업로드 확인 주기

        private SteamCloudSaveSync cloudSaveSync; // Cloud 저장 동기화 서비스
        private float nextCloudSyncTime; // 다음 Cloud 동기화 시각
        private bool shuttingDown; // 중복 종료 방지 상태

        public SteamCloudSyncResult LastCloudSyncResult { get; private set; } = SteamCloudSyncResult.Unavailable; // 마지막 Cloud 동기화 결과

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate() // Runtime Controller 자동 생성
        {
            if (FindFirstObjectByType<SteamRuntimeController>() != null)
            {
                return;
            }

            GameObject host = new GameObject(nameof(SteamRuntimeController)); // Runtime 호스트 생성
            host.AddComponent<SteamRuntimeController>(); // Runtime Controller 연결
        }

        private void Awake() // Steam Runtime 초기화
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject); // 중복 Runtime 제거
                return;
            }

            instance = this; // Runtime 인스턴스 등록

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject); // 씬 전환에도 Runtime 유지
            }

            SteamPlatformService.Configure(appId); // Steam AppID 설정

            if (SteamPlatformService.TryInitialize())
            {
                InitializeCloudSync(); // Steam Cloud 동기화 준비
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GetComponent<SteamOverlayDebugShortcut>() == null)
            {
                gameObject.AddComponent<SteamOverlayDebugShortcut>(); // 개발 환경 Overlay 테스트 연결
            }
#endif
        }

        private void Update() // Steam Callback 및 Cloud 주기 처리
        {
            SteamPlatformService.PumpCallbacks(); // Steam Callback 처리

            if (cloudSaveSync == null || Time.unscaledTime < nextCloudSyncTime)
            {
                return;
            }

            LastCloudSyncResult = cloudSaveSync.UploadLocalIfChanged(); // 변경된 로컬 저장 업로드
            nextCloudSyncTime = Time.unscaledTime + Mathf.Max(0.5f, cloudSyncIntervalSeconds); // 다음 동기화 시각 갱신
        }

        private void OnDestroy() // Runtime 객체 제거 처리
        {
            if (instance != this)
            {
                return;
            }

            ShutdownRuntime(); // 실제 Runtime 인스턴스 종료
            instance = null; // Runtime 인스턴스 해제
        }

        private void OnApplicationQuit() // 애플리케이션 종료 처리
        {
            ShutdownRuntime(); // 종료 전 Cloud 업로드 및 Steam 종료
        }

        private void InitializeCloudSync() // Cloud 동기화 초기화
        {
            if (!enableCloudSync)
            {
                return;
            }

            const string saveFileName = "run_save.json"; // Cloud 저장 파일 이름
            string localSavePath = Path.Combine(Application.persistentDataPath, saveFileName); // 로컬 저장 경로
            cloudSaveSync = new SteamCloudSaveSync(localSavePath, saveFileName); // Cloud 동기화 서비스 생성
            LastCloudSyncResult = cloudSaveSync.SyncAtStartup(); // 시작 시 로컬·원격 저장 동기화
            nextCloudSyncTime = Time.unscaledTime + Mathf.Max(0.5f, cloudSyncIntervalSeconds); // 다음 동기화 시각 설정
        }

        private void ShutdownRuntime() // Steam Runtime 안전 종료
        {
            if (shuttingDown)
            {
                return;
            }

            shuttingDown = true; // 중복 종료 차단

            if (cloudSaveSync != null)
            {
                LastCloudSyncResult = cloudSaveSync.UploadLocalIfChanged(); // 종료 전 마지막 Cloud 업로드
            }

            SteamPlatformService.Shutdown(); // Steam Runtime 종료
        }
    }
}
