using UnityEngine; // MonoBehaviour·Time 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.UI; // 시스템 저장 Toast 사용

namespace ProjectEta.Run
{
    [DefaultExecutionOrder(1070)]
    public sealed class RunPersistenceController : MonoBehaviour
    {
        private const float SavePollIntervalSeconds = 0.25f; // 안전 지점 변경 감지 주기

        private BattleController _battleController; // 현재 BattleController 참조
        private RunState _runState; // 현재 저장 감시 런
        private string _lastCheckpointKey = string.Empty; // 마지막 저장 안전 지점 식별값
        private float _nextSavePollTime; // 다음 저장 검사 시간
        private bool _terminalSaveCleared; // 현재 종료 런 세이브 삭제 완료 여부

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<RunPersistenceController>() != null) return; // 중복 저장 관리자 차단

            var host = new GameObject("RunPersistenceController_Day51"); // 런 저장 호스트 생성
            host.AddComponent<RunPersistenceController>(); // 자동 저장 관리자 추가
        }

        private void Update()
        {
            ResolveRunState(); // 현재 BattleController·RunState 연결 확인
            if (_runState == null) return; // 런 준비 전 저장 처리 지연

            if (_runState.Flow.IsRunFinished)
            {
                ClearTerminalSave(); // 패배·최종 클리어 런 세이브 삭제
                return; // 종료 런 저장 차단
            }

            if (Time.unscaledTime < _nextSavePollTime) return; // 저장 검사 주기 이전 프레임 생략
            _nextSavePollTime = Time.unscaledTime + SavePollIntervalSeconds; // 다음 저장 검사 시간 예약

            TrySaveCheckpoint(); // 새 안전 지점 진입 시 자동 저장
        }

        private void ResolveRunState()
        {
            if (_battleController == null)
            {
                _battleController = Object.FindFirstObjectByType<BattleController>(); // 현재 BattleController 탐색
            }

            RunState current = _battleController != null ? _battleController.RunState : null; // 현재 BattleController 런 조회
            if (ReferenceEquals(current, _runState)) return; // 같은 런 재연결 차단

            _runState = current; // 새 런 상태 연결
            bool restoredSafeCheckpoint = _runState != null && RunSaveSystem.CanContinue && RunSaveSystem.IsSafeCheckpoint(_runState); // 디스크에서 복원된 안전 지점 여부 판정
            _lastCheckpointKey = restoredSafeCheckpoint ? CreateCheckpointKey(_runState) : string.Empty; // 복원 직후 같은 지점 중복 저장 Toast 차단
            _terminalSaveCleared = false; // 새 런 종료 삭제 상태 초기화
            _nextSavePollTime = 0f; // 새 런 즉시 안전 지점 검사 허용
        }

        private void TrySaveCheckpoint()
        {
            if (!RunSaveSystem.IsSafeCheckpoint(_runState)) return; // 전투 중·전환 중 자동 저장 차단

            string checkpointKey = CreateCheckpointKey(_runState); // 현재 안전 지점 고유 식별값 생성
            if (string.Equals(checkpointKey, _lastCheckpointKey, System.StringComparison.Ordinal)) return; // 같은 Shop/Event/Reward 내부 선택 중 재저장 차단

            if (!RunSaveSystem.TrySave(_runState)) return; // 실제 디스크 저장 실패 시 식별값 갱신 차단

            _lastCheckpointKey = checkpointKey; // 저장 성공 안전 지점 기록
            Debug.Log($"68일차 런 자동 저장: Phase={_runState.CurrentFlowPhase} / Stage={_runState.CurrentRound} / Node={_runState.RouteMap.CurrentNodeId}"); // 자동 저장 결과 출력
            SystemToastUI.Push("저장 완료", $"STAGE {_runState.CurrentRound} · {GetFlowLabel(_runState.CurrentFlowPhase)}", 1.8f); // 화면 우상단 저장 완료 안내 등록
        }

        private static string CreateCheckpointKey(RunState runState)
        {
            return $"{runState.CurrentFlowPhase}|{runState.CurrentRound}|{runState.RouteMap.CurrentNodeId}|{runState.RouteMap.SelectedNodeId}"; // 진행 위치 중심 안전 지점 키 생성
        }

        private static string GetFlowLabel(RunFlowPhase phase)
        {
            switch (phase)
            {
                case RunFlowPhase.Map:
                    return "경로 지도"; // Map 한글 표시 반환
                case RunFlowPhase.Reward:
                    return "보상"; // Reward 한글 표시 반환
                case RunFlowPhase.Shop:
                    return "상점"; // Shop 한글 표시 반환
                case RunFlowPhase.Event:
                    return "이벤트"; // Event 한글 표시 반환
                default:
                    return phase.ToString(); // 기타 흐름 원본 표시 반환
            }
        }

        private void ClearTerminalSave()
        {
            if (_terminalSaveCleared) return; // 같은 종료 런 중복 삭제 차단

            RunSaveSystem.DeleteSave(); // 런 종료 시 진행 세이브 제거
            _terminalSaveCleared = true; // 현재 종료 런 삭제 처리 완료
            _lastCheckpointKey = string.Empty; // 이전 안전 지점 기록 제거
            Debug.Log($"51일차 런 세이브 종료 정리: {_runState.CurrentFlowPhase}"); // 삭제 처리 결과 출력
        }

        private void OnApplicationQuit()
        {
            if (_runState == null || !RunSaveSystem.IsSafeCheckpoint(_runState)) return; // 안전 지점 외 종료 저장 차단
            TrySaveCheckpoint(); // 아직 저장하지 않은 안전 지점만 종료 직전 저장
        }
    }
}
