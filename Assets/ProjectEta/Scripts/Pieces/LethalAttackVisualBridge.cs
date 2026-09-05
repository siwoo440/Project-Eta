using System.Collections; // 초기화·치명타 이동 코루틴 사용
using System.Collections.Generic; // Dictionary<T> 사용
using UnityEngine; // MonoBehaviour·Vector3·RuntimeInitializeOnLoadMethod 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 판정
using ProjectEta.Battle; // BattleHooks·CombatResult 사용
using ProjectEta.Board; // BoardInputController·BoardView 사용

namespace ProjectEta.Pieces // 기물 전투 연출 네임스페이스
{
    [DefaultExecutionOrder(940)] // 전투 시스템 연결 이후 연출 훅 구독
    public sealed class LethalAttackVisualBridge : MonoBehaviour // 치명 공격자의 즉시 순간이동을 떠서 이동하는 연출로 보정
    {
        private const float HopHeight = 0.55f; // 치명 공격 시 상승 높이
        private const float RisingSecondsAtThreeSpeed = 0.12f; // 현재 3배속 기준 상승 시간
        private const float AdvancingSecondsAtThreeSpeed = 0.18f; // 현재 3배속 기준 목표 칸 이동 시간

        private readonly Dictionary<PieceRuntimeState, Vector2Int> _attackOrigins = new Dictionary<PieceRuntimeState, Vector2Int>(); // 공격 판정 전 공격자 원래 좌표 기록
        private BoardInputController _boardInputController; // 기존 전투 입력·훅 접근
        private BattleHooks _battleHooks; // 전투 공격 전후 이벤트 버스
        private BoardView _boardView; // 보드 좌표→로컬 위치 변환 기준

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 후 자동 생성
        private static void AutoCreateForBattleScene() // 씬 수정 없이 치명타 연출 브리지 자동 주입
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<LethalAttackVisualBridge>() != null) return; // 중복 생성 차단

            var host = new GameObject("LethalAttackVisualBridge_Day44"); // 치명타 연출 호스트 생성
            host.AddComponent<LethalAttackVisualBridge>(); // 연출 브리지 컴포넌트 추가
        }

        private IEnumerator Start() // BoardInputController·BattleHooks 준비 후 이벤트 연결
        {
            const int maxWaitFrames = 180; // 최대 초기화 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames) // 전투 시스템 준비 대기
            {
                _boardInputController = Object.FindFirstObjectByType<BoardInputController>(); // 보드 입력 컨트롤러 탐색
                _boardView = Object.FindFirstObjectByType<BoardView>(); // 보드 뷰 탐색

                if (_boardInputController != null && _boardInputController.IsBound && _boardInputController.BattleHooks != null && _boardView != null) // 필수 참조 준비 확인
                {
                    Bind(_boardInputController.BattleHooks); // 공격 전후 훅 연결
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임 대기
            }

            Debug.LogError("44일차 LethalAttackVisualBridge 초기화 실패: BoardInputController·BattleHooks 연결을 확인하세요."); // 초기화 실패 기록
        }

        private void Bind(BattleHooks battleHooks) // 전투 공격 전후 이벤트 구독
        {
            if (_battleHooks != null) // 이전 훅 연결이 있으면
            {
                _battleHooks.BeforeAttack -= HandleBeforeAttack; // 이전 공격 전 이벤트 해제
                _battleHooks.AfterAttack -= HandleAfterAttack; // 이전 공격 후 이벤트 해제
            }

            _battleHooks = battleHooks; // 현재 전투 훅 저장
            _battleHooks.BeforeAttack += HandleBeforeAttack; // 공격 판정 전 원래 좌표 기록
            _battleHooks.AfterAttack += HandleAfterAttack; // 치명 결과 후 시각 이동 재생
        }

        private void HandleBeforeAttack(PieceRuntimeState attacker, PieceRuntimeState defender) // 공격 논리 처리 전 공격자 좌표 저장
        {
            if (attacker == null) return; // 잘못된 공격자 차단
            _attackOrigins[attacker] = attacker.BoardPosition; // 사망 처리·점유 이동 전 원래 좌표 기록
        }

        private void HandleAfterAttack(CombatResult result) // 공격 논리 처리가 끝난 뒤 치명 공격자 이동 연출 실행
        {
            if (result == null || result.Attacker == null) return; // 잘못된 결과 차단

            if (!_attackOrigins.TryGetValue(result.Attacker, out var origin)) return; // 공격 전 좌표 기록 누락 차단
            _attackOrigins.Remove(result.Attacker); // 이번 공격 좌표 기록 정리

            if (!result.DefenderDied) return; // 비치명 공격은 기존 접근·복귀 연출 사용
            if (result.Attacker.Definition != null && (result.Attacker.Definition.RoleTags & PieceRoleTag.Ranged) != 0) return; // 원거리 치명 공격자는 순간 이동 연출 제외
            if (origin == result.Attacker.BoardPosition) return; // 처치 후 실제 점유 이동이 없는 공격 제외

            var attackerView = FindPieceView(result.Attacker); // 공격자 화면 오브젝트 조회
            if (attackerView == null) return; // 화면 표시 누락 방어

            StartCoroutine(AnimateLethalAdvance(attackerView.transform, origin, result.Attacker.BoardPosition)); // 원래 칸→처치 칸 상승 이동 연출 시작
        }

        private IEnumerator AnimateLethalAdvance(Transform attackerTransform, Vector2Int originCell, Vector2Int targetCell) // 치명 공격자의 떠서 전진하는 시각 연출
        {
            if (attackerTransform == null || _boardView == null) yield break; // 필수 참조 누락 방어

            Vector3 origin = BoardView.BoardToLocalPosition(originCell, _boardView.TileSize); // 공격 시작 위치 계산
            Vector3 target = BoardView.BoardToLocalPosition(targetCell, _boardView.TileSize); // 처치 후 실제 점유 위치 계산
            Vector3 raised = origin + Vector3.up * HopHeight; // 상승 완료 위치 계산

            attackerTransform.localPosition = origin; // 논리 이동으로 순간이동한 화면을 원래 위치로 되돌림

            float elapsed = 0f; // 상승 경과 시간 초기화
            while (elapsed < RisingSecondsAtThreeSpeed) // 원래 칸에서 위로 떠오르는 구간
            {
                elapsed += Time.deltaTime; // 현재 배속이 반영된 시간 누적
                float t = Mathf.Clamp01(elapsed / RisingSecondsAtThreeSpeed); // 상승 진행률 계산
                attackerTransform.localPosition = Vector3.Lerp(origin, raised, t); // 수직 상승 적용
                yield return null; // 다음 프레임 대기
            }

            elapsed = 0f; // 전진 구간 경과 시간 초기화
            while (elapsed < AdvancingSecondsAtThreeSpeed) // 떠 있는 상태에서 목표 칸으로 내려가는 구간
            {
                elapsed += Time.deltaTime; // 현재 배속이 반영된 시간 누적
                float t = Mathf.Clamp01(elapsed / AdvancingSecondsAtThreeSpeed); // 전진 진행률 계산
                float arc = HopHeight * (1f - t); // 목표 칸에 가까워질수록 높이 감소
                Vector3 flat = Vector3.Lerp(origin, target, t); // 평면상 목표 칸 이동 계산
                attackerTransform.localPosition = flat + Vector3.up * arc; // 떠서 이동하며 자연스럽게 착지
                yield return null; // 다음 프레임 대기
            }

            attackerTransform.localPosition = target; // 최종 점유 칸 위치 정확히 고정
        }

        private static PieceView FindPieceView(PieceRuntimeState runtimeState) // 런타임 기물 상태에 대응하는 화면 PieceView 조회
        {
            var pieceViews = Object.FindObjectsByType<PieceView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None); // 활성 기물 뷰 전체 조회

            for (int i = 0; i < pieceViews.Length; i++) // 기물 뷰 순회
            {
                if (pieceViews[i] != null && pieceViews[i].RuntimeState == runtimeState) return pieceViews[i]; // 동일 런타임 상태 뷰 반환
            }

            return null; // 대응 뷰 없음 반환
        }

        private void OnDestroy() // 씬 종료 시 전투 훅 이벤트 정리
        {
            if (_battleHooks != null) // 연결된 훅이 있으면
            {
                _battleHooks.BeforeAttack -= HandleBeforeAttack; // 공격 전 이벤트 구독 해제
                _battleHooks.AfterAttack -= HandleAfterAttack; // 공격 후 이벤트 구독 해제
            }

            _attackOrigins.Clear(); // 남은 공격 좌표 기록 정리
        }
    }
}
