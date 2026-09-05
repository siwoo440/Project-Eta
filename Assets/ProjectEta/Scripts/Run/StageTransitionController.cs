using System.Collections; // 스테이지 전환 코루틴 사용
using UnityEngine; // MonoBehaviour·GameObject·WaitForSecondsRealtime 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 판정
using ProjectEta.Battle; // BattleController·TurnManager 사용
using ProjectEta.Board; // RouteMapBoardController·BoardInputController 사용
using ProjectEta.Pieces; // PieceDefinition·PieceMovementType 사용
using ProjectEta.Round; // 기존 RoundRuntimeController 제거 사용
using ProjectEta.UI; // StagePlaceholderUI 사용

namespace ProjectEta.Run // 로그라이트 스테이지 전환 네임스페이스
{
    [DefaultExecutionOrder(1020)] // 지도 킹 이동 컨트롤러 준비 뒤 실제 스테이지 진입 연결
    public sealed class StageTransitionController : MonoBehaviour // StageNode 선택을 Battle·Reward·Shop·Event 실제 흐름으로 연결하는 45일차 관리자
    {
        private const int NextBattleOpeningHandSize = 5; // 새 BattleState 시작 손패 테스트 장수
        private BattleController _battleController; // 같은 Battle 씬의 전투 상태 소유자
        private BoardInputController _boardInputController; // 카드·적 스폰 입력 시스템
        private RouteMapBoardController _routeMapBoardController; // 지도 킹 선택 완료 이벤트 소유자
        private RunState _runState; // 전체 로그라이트 런 상태
        private StagePlaceholderUI _placeholderUI; // Shop·Event 임시 진입 화면
        private Coroutine _transitionCoroutine; // 현재 스테이지 전환 코루틴

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 후 자동 생성
        private static void AutoCreateForBattleScene() // 씬·Inspector 수정 없이 45일차 전환기 자동 주입
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (UnityEngine.Object.FindFirstObjectByType<StageTransitionController>() != null) return; // 중복 생성 차단

            var host = new GameObject("StageTransitionController_Day45"); // 스테이지 전환 호스트 생성
            host.AddComponent<StageTransitionController>(); // 전환 관리자 컴포넌트 추가
        }

        private IEnumerator Start() // BattleController·RouteMapBoardController 준비 후 이벤트 연결
        {
            const int maxWaitFrames = 240; // 최대 초기화 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames) // 필수 런타임 객체 준비 대기
            {
                _battleController = UnityEngine.Object.FindFirstObjectByType<BattleController>(); // 전투 컨트롤러 탐색
                _boardInputController = UnityEngine.Object.FindFirstObjectByType<BoardInputController>(); // 보드 입력 탐색
                _routeMapBoardController = UnityEngine.Object.FindFirstObjectByType<RouteMapBoardController>(); // 지도 컨트롤러 탐색

                if (_battleController != null && _battleController.RunState != null && _boardInputController != null && _routeMapBoardController != null) // 필수 객체 준비 확인
                {
                    _runState = _battleController.RunState; // 현재 런 상태 저장
                    Bind(); // 지도 선택 완료 이벤트 연결
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임 대기
            }

            Debug.LogError("45일차 StageTransitionController 초기화 실패: BattleController·BoardInputController·RouteMapBoardController를 확인하세요."); // 초기화 실패 기록
        }

        private void Bind() // 지도 킹 선택 이벤트와 임시 비전투 UI 연결
        {
            _routeMapBoardController.StageNodeSelected -= HandleStageNodeSelected; // 중복 구독 제거
            _routeMapBoardController.StageNodeSelected += HandleStageNodeSelected; // 킹 이동 완료 후 실제 스테이지 진입 구독
            _placeholderUI = GetComponent<StagePlaceholderUI>(); // 같은 호스트의 기존 임시 UI 탐색
            if (_placeholderUI == null) _placeholderUI = gameObject.AddComponent<StagePlaceholderUI>(); // 없으면 비전투 스테이지 임시 UI 추가
        }

        private void HandleStageNodeSelected(StageNode stageNode) // 지도 킹 이동이 끝난 실제 선택 노드 수신
        {
            if (stageNode == null || _transitionCoroutine != null) return; // 잘못된 노드·중복 전환 차단
            _transitionCoroutine = StartCoroutine(EnterSelectedStage(stageNode)); // 선택 StageDefinition 기반 전환 시작
        }

        private IEnumerator EnterSelectedStage(StageNode stageNode) // StageDefinition을 읽고 전투·비전투 흐름으로 분기
        {
            yield return new WaitForSecondsRealtime(0.05f); // 킹 이동 마지막 프레임과 판 전환을 시각적으로 분리

            StageDefinition definition = StageDefinitionCatalog.Resolve(stageNode.StageDefinitionId, stageNode.Depth); // 실제 스테이지 설정 조회
            if (definition == null) // 정의 조회 실패 처리
            {
                Debug.LogError($"45일차 스테이지 진입 실패: StageDefinition '{stageNode.StageDefinitionId}'를 만들지 못했습니다."); // 오류 기록
                _transitionCoroutine = null; // 다음 선택을 위해 코루틴 상태 해제
                yield break; // 전환 중단
            }

            _runState.CurrentRound = stageNode.Depth; // 선택 노드 깊이를 현재 1~10 진행 단계에 반영
            yield return CleanupPreviousBattleRuntime(); // 이전 라운드 증원·보스 런타임 이벤트 제거

            if (definition.RequiresBattle) // 일반·엘리트·보스 전투 스테이지인지 확인
            {
                EnterBattleStage(definition); // 새 BattleState·RoundDefinition 적용
            }
            else // Reward·Shop·Event 비전투 스테이지 처리
            {
                EnterNonBattleStage(definition); // 지도 배경을 유지한 임시 선택 화면 진입
            }

            _transitionCoroutine = null; // 현재 전환 완료 기록
        }

        private IEnumerator CleanupPreviousBattleRuntime() // 이전 스테이지의 RoundRuntime 이벤트가 새 전투에 간섭하지 않도록 제거
        {
            bool destroyed = false; // 실제 제거 대상 존재 여부
            var legacyRuntime = UnityEngine.Object.FindFirstObjectByType<RoundRuntimeController>(); // 40일차 초기 라운드 런타임 탐색
            if (legacyRuntime != null) // 초기 라운드 런타임이 남아 있으면
            {
                Destroy(legacyRuntime.gameObject); // 다음 스테이지부터 StageDefinition 런타임으로 교체
                destroyed = true; // 프레임 대기 필요 기록
            }

            var stageRuntime = UnityEngine.Object.FindFirstObjectByType<StageBattleRuntimeController>(); // 이전 45일차 전투 런타임 탐색
            if (stageRuntime != null) // 이전 스테이지 런타임이 남아 있으면
            {
                Destroy(stageRuntime.gameObject); // 이전 증원·턴 이벤트와 함께 제거
                destroyed = true; // 프레임 대기 필요 기록
            }

            if (destroyed) yield return null; // OnDestroy 이벤트 해제가 끝난 다음 새 TurnManager 상태 통지
        }

        private void EnterBattleStage(StageDefinition definition) // 선택한 전투 StageDefinition으로 같은 씬의 새 BattleState 구성
        {
            _runState.ResetBattleState(); // 이전 전투 Board·Hand를 새 임시 전투 상태로 교체
            PrepareNextBattleHand(); // 런 전체 OwnedCardPool 기반 새 시작 손패 재구성
            _battleController.Initialize(_runState); // BoardView·BoardInputController·카드 UI를 새 BattleState에 재바인딩

            var runtimeObject = new GameObject($"StageBattleRuntime_Day45_{_runState.CurrentRound}"); // 현재 스테이지 전투 데이터 관리자 생성
            var runtime = runtimeObject.AddComponent<StageBattleRuntimeController>(); // StageDefinition 적용 컴포넌트 추가

            if (!runtime.Configure(_battleController, definition)) // RoundDefinition·적 구성 적용 실패 확인
            {
                Debug.LogError($"45일차 전투 스테이지 구성 실패: {definition.StageId}"); // 실패 스테이지 기록
                Destroy(runtimeObject); // 불완전 런타임 제거
                return; // 전투 시작 중단
            }

            _runState.StartCurrentRound(); // RoundState 진행 중·Flow Battle·BoardMode Battle 적용
            _battleController.TurnManager.ResetForNewBattle(); // 기존 BattleEnded TurnManager를 시작 배치 턴으로 초기화

            Debug.Log($"45일차 전투 스테이지 진입: {definition.DisplayName} / Round={_runState.CurrentRound} / Type={definition.StageType}"); // 전투 전환 결과 기록
        }

        private void PrepareNextBattleHand() // 새 BattleState.Hand를 런 전체 보유 카드 풀에서 다시 구성
        {
            if (_runState.Deck.DeadCardPile.Count > 0) _runState.Deck.ReturnDeadPileToOwnedPool(); // 디버그 승리 등에서도 죽은 카드 소유권 복구
            _runState.Deck.RebuildDrawPileFromOwnedPool(); // 현재 보유 카드 전체로 다음 전투 드로우 더미 재구성·셔플

            PieceDefinition kingDefinition = FindOwnedKingDefinition(); // 시작 배치 필수 킹 카드 조회
            if (kingDefinition != null) _runState.Deck.TryMoveSpecificToHand(kingDefinition, _runState.Hand); // 킹을 새 손패에 우선 배치

            while (_runState.Hand.Hand.Count < NextBattleOpeningHandSize && _runState.Deck.TryDrawToHand(_runState.Hand)) // 나머지 카드를 랜덤 드로우로 채움
            {
            }

            Debug.Log($"45일차 다음 전투 손패: Hand={_runState.Hand.Hand.Count}, Draw={_runState.Deck.DrawPile.Count}, Owned={_runState.Deck.OwnedCardPool.Count}"); // 카드 재구성 결과 기록
        }

        private PieceDefinition FindOwnedKingDefinition() // 런 OwnedCardPool에서 플레이어 킹 카드 조회
        {
            for (int i = 0; i < _runState.Deck.OwnedCardPool.Count; i++) // 보유 카드 풀 순회
            {
                PieceDefinition definition = _runState.Deck.OwnedCardPool[i]; // 현재 카드 정의 조회
                if (definition == null) continue; // 빈 카드 제외
                if (definition.MovementType == PieceMovementType.King) return definition; // 첫 킹 카드 반환
            }

            return null; // 킹 카드 없음 반환
        }

        private void EnterNonBattleStage(StageDefinition definition) // Reward는 실제 카드 보상, Shop·Event는 다음 일차 임시 흐름으로 연결
        {
            _runState.Round.Begin(); // 선택한 비전투 스테이지 진행 중 상태 지정

            if (definition.StageType == StageType.Reward) // 카드 보상 노드 확인
            {
                _runState.Flow.EnterReward(); // CardRewardController가 감지할 실제 보상 흐름 진입
                Debug.Log($"46일차 카드 보상 스테이지 진입: {definition.DisplayName}"); // 실제 보상 시스템 진입 기록
                return; // Placeholder UI 표시 차단
            }

            if (definition.StageType == StageType.Shop) _runState.Flow.EnterShop(); // 상점 흐름 진입
            else _runState.Flow.EnterEvent(); // 이벤트 흐름 진입

            _placeholderUI.Show(definition, CompleteNonBattleStage); // 47일차 전까지 Shop·Event 개발용 완료 UI 표시
            Debug.Log($"45일차 비전투 스테이지 진입: {definition.DisplayName} / Type={definition.StageType}"); // 비전투 전환 결과 기록
        }

        private void CompleteNonBattleStage() // Reward·Shop·Event 임시 처리를 완료하고 다음 지도 분기 준비
        {
            _runState.Round.Restore(_runState.CurrentRound, RoundProgressStatus.Cleared, BattleOutcome.Victory); // 비전투 스테이지 완료 상태 기록

            if (_runState.CurrentRound >= RoundState.FinalRound) // 안전한 최종 깊이 완료 처리
            {
                _runState.Flow.CompleteRun(); // 최종 깊이라면 런 완료
                return; // 다음 지도 생성 없음
            }

            _runState.RouteMap.PreparePrototypeAfterBattle(_runState.CurrentRound); // 현재 킹 위치 기준 다음 깊이 2~3분기 생성
            _runState.Flow.EnterMap(); // 다시 경로 지도 선택 흐름 진입
            _routeMapBoardController.RefreshMapVisuals(); // BoardMode가 계속 Map이어도 새 분기를 즉시 화면에 반영
            Debug.Log($"45일차 비전투 스테이지 완료 -> Map / Depth={_runState.CurrentRound}"); // 다음 경로 복귀 결과 기록
        }

        private void OnDestroy() // 전환 관리자 제거 시 이벤트·코루틴 정리
        {
            if (_routeMapBoardController != null) _routeMapBoardController.StageNodeSelected -= HandleStageNodeSelected; // 지도 선택 이벤트 구독 해제
            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine); // 진행 중 전환 코루틴 정리
        }
    }
}
