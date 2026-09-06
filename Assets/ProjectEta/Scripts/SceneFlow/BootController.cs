using System.Collections; // IEnumerator 사용
using UnityEngine; // MonoBehaviour·Camera·Color 사용
using ProjectEta.Meta; // MetaProgressService 사용
using ProjectEta.Run; // RunSaveSystem 사용

namespace ProjectEta.SceneFlow
{
    [DefaultExecutionOrder(-900)]
    public sealed class BootController : MonoBehaviour
    {
        private IEnumerator Start()
        {
            PrepareCamera(); // 빈 Boot 씬 배경 정리
            MetaProgressState progress = MetaProgressService.Current; // 영구 성장 데이터 최초 로드
            bool canContinue = RunSaveSystem.CanContinue; // 런 세이브 유효성 미리 확인
            Debug.Log($"54일차 Boot 초기화: MetaToken={progress.MetaTokens} / Continue={canContinue}"); // 초기화 결과 기록

            yield return null; // 첫 프레임 초기화 완료 대기

            if (!SceneFlowController.LoadMainMenu())
            {
                Debug.LogError("54일차 Boot 전환 실패: MainMenu 씬을 Build Settings에서 확인하세요."); // 메인 메뉴 전환 실패 기록
            }
        }

        private static void PrepareCamera()
        {
            Camera camera = Camera.main; // Boot 기본 카메라 조회
            if (camera == null) return; // 카메라 누락 허용

            camera.clearFlags = CameraClearFlags.SolidColor; // 단색 초기화 화면 적용
            camera.backgroundColor = new Color(0.015f, 0.018f, 0.022f, 1f); // 어두운 부트 배경 적용
        }
    }
}
