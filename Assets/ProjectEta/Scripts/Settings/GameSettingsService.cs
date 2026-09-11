using System; // Exception 사용
using System.IO; // 설정 파일 읽기·쓰기 사용
using UnityEngine; // Application·Screen·JsonUtility·CanvasScaler 사용
using UnityEngine.UI; // CanvasScaler 사용

namespace ProjectEta.Settings
{
    public static class GameSettingsService
    {
        private const string SettingsFileName = "settings.json"; // 설정 저장 파일 이름
        private static GameSettingsData _current; // 현재 적용 설정
        private static bool _loaded; // 설정 로드 완료 여부

        private static string SettingsPath => Path.Combine(Application.persistentDataPath, SettingsFileName); // 설정 파일 실제 경로

        public static GameSettingsData Current
        {
            get
            {
                EnsureLoaded(); // 현재 설정 로드 보장
                return _current.Clone(); // 외부 변경 방지 복제 반환
            }
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return; // 중복 설정 로드 차단

            _current = LoadFromDisk() ?? GameSettingsData.CreateDefault(); // 저장 설정 또는 기본 설정 선택
            _current = _current.Normalized(); // 저장 설정 안전 범위·버전 보정
            _loaded = true; // 설정 로드 완료 기록
            ApplyRuntime(_current); // 최초 런타임 설정 적용
        }

        public static void ApplyAndSave(GameSettingsData data)
        {
            EnsureLoaded(); // 설정 서비스 준비 보장
            _current = (data ?? GameSettingsData.CreateDefault()).Normalized(); // 적용 설정 안전 보정
            ApplyRuntime(_current); // 화면·UI·오디오 설정 즉시 적용
            SaveToDisk(_current); // 적용 설정 영구 저장
        }

        public static void MarkFirstTutorialCompleted()
        {
            EnsureLoaded(); // 설정 서비스 준비 보장
            if (_current.FirstTutorialCompleted) return; // 이미 완료한 튜토리얼 중복 저장 차단

            _current.FirstTutorialCompleted = true; // 최초 튜토리얼 완료 상태 적용
            _current.SettingsVersion = GameSettingsData.CurrentVersion; // 최신 저장 버전 유지
            SaveToDisk(_current); // 튜토리얼 완료 상태 즉시 저장
        }

        public static void PreviewUiScale(float uiScale)
        {
            float normalized = Mathf.Clamp(uiScale, GameSettingsData.MinimumUiScale, GameSettingsData.MaximumUiScale); // 미리보기 UI 배율 보정
            ApplyUiScaleToAllCanvases(normalized); // 현재 Canvas 전체 UI 배율 미리보기
        }

        public static void PreviewAudio(float masterVolume, float bgmVolume, float sfxVolume)
        {
            GameAudioService.Apply(masterVolume, bgmVolume, sfxVolume); // 미저장 오디오 편집값 즉시 미리보기
        }

        public static void ReapplyUiScale()
        {
            EnsureLoaded(); // 설정 서비스 준비 보장
            ApplyUiScaleToAllCanvases(_current.UiScale); // 현재 저장 UI 배율 재적용
        }

        public static void ReapplyAudio()
        {
            EnsureLoaded(); // 설정 서비스 준비 보장
            GameAudioService.Apply(_current); // 현재 저장 오디오 설정 재적용
        }

        public static void ReapplyRuntimeSettings()
        {
            EnsureLoaded(); // 설정 서비스 준비 보장
            ApplyUiScaleToAllCanvases(_current.UiScale); // 새 Canvas UI Scale 재적용
            GameAudioService.Apply(_current); // 새 Scene 오디오 설정 재적용
            GameInputBindingService.Apply(_current); // 새 Scene 저장 조작키 재적용
        }

        public static void ResetRuntimeState()
        {
            _current = null; // 정적 현재 설정 초기화
            _loaded = false; // 정적 로드 상태 초기화
            GameAudioService.ResetRuntimeState(); // 오디오 런타임 상태 초기화
            GameInputBindingService.ResetRuntimeState(); // 조작키 런타임 상태 초기화
        }

        private static GameSettingsData LoadFromDisk()
        {
            if (!File.Exists(SettingsPath)) return null; // 저장 파일 없음 처리

            try
            {
                string json = File.ReadAllText(SettingsPath); // 설정 JSON 읽기
                if (string.IsNullOrWhiteSpace(json)) return null; // 빈 설정 파일 fallback
                return JsonUtility.FromJson<GameSettingsData>(json); // 설정 데이터 역직렬화
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"57일차 설정 읽기 실패: {exception.Message}"); // 설정 읽기 오류 기록
                return null; // 기본 설정 fallback 허용
            }
        }

        private static void SaveToDisk(GameSettingsData data)
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsPath); // 설정 저장 폴더 조회
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory); // 저장 폴더 생성 보장
                string json = JsonUtility.ToJson(data, true); // 설정 JSON 생성
                File.WriteAllText(SettingsPath, json); // 설정 파일 저장
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"57일차 설정 저장 실패: {exception.Message}"); // 설정 저장 오류 기록
            }
        }

        private static void ApplyRuntime(GameSettingsData data)
        {
            FullScreenMode mode = (FullScreenMode)data.ScreenMode; // 저장 화면 모드 변환
            Screen.SetResolution(data.ResolutionWidth, data.ResolutionHeight, mode); // 해상도·화면 모드 적용
            ApplyUiScaleToAllCanvases(data.UiScale); // 현재 Canvas UI 배율 적용
            GameAudioService.Apply(data); // Master·BGM·SFX 오디오 설정 적용
            GameInputBindingService.Apply(data); // 저장 조작키 런타임 입력에 적용
        }

        private static void ApplyUiScaleToAllCanvases(float uiScale)
        {
            CanvasScaler[] scalers = UnityEngine.Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None); // 현재 CanvasScaler 전체 조회

            for (int i = 0; i < scalers.Length; i++)
            {
                CanvasScaler scaler = scalers[i]; // 현재 CanvasScaler 조회
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue; // 비대상 CanvasScaler 제외

                GameSettingsCanvasScaleMarker marker = scaler.GetComponent<GameSettingsCanvasScaleMarker>(); // 기준 해상도 보관 마커 조회
                if (marker == null)
                {
                    marker = scaler.gameObject.AddComponent<GameSettingsCanvasScaleMarker>(); // 기준 해상도 보관 마커 추가
                    marker.BaseReferenceResolution = scaler.referenceResolution; // 현재 기준 해상도 최초 저장
                }

                Vector2 baseResolution = marker.BaseReferenceResolution; // 원본 기준 해상도 조회
                if (baseResolution.x <= 0f || baseResolution.y <= 0f) baseResolution = new Vector2(1920f, 1080f); // 잘못된 기준 해상도 fallback
                scaler.referenceResolution = baseResolution / uiScale; // UI 배율에 맞춘 기준 해상도 적용
            }
        }
    }

    public sealed class GameSettingsCanvasScaleMarker : MonoBehaviour
    {
        public Vector2 BaseReferenceResolution = new Vector2(1920f, 1080f); // Canvas 원본 기준 해상도
    }
}
