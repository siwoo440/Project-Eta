using System; // Action 콜백을 사용하기 위한 네임스페이스
using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Vector3, Quaternion 등을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView.BoardToLocalPosition을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // PieceView와 같은 네임스페이스에 확장 메서드를 배치
{
    public static class PieceViewCombatAnimationExtensions // 33일차 회귀 복구: 구버전 PieceView에 30일차 연출 API를 다시 제공하는 호환 확장
    {
        public static void SnapTo(this PieceView view, Vector2Int boardPosition, float tileSize) // 카드 드래그 고스트를 연출 없이 즉시 목표 칸으로 이동
        {
            if (view == null) return; // 뷰가 없으면 처리하지 않음
            var bridge = PieceViewAnimationBridge.GetOrAdd(view); // 연출 브리지 확보
            bridge.StopPositionAnimation(); // 진행 중 위치 연출이 있다면 중단
            ApplyBoardPosition(view, boardPosition, tileSize); // 이름과 위치를 즉시 갱신
        }

        public static void PlayNonLethalStrikeAndReturn(this PieceView view, Vector2Int targetBoardPosition, float tileSize) // 비치명 근접 공격의 접근·복귀 연출
        {
            if (view == null) return; // 뷰가 없으면 처리하지 않음
            PieceViewAnimationBridge.GetOrAdd(view).PlayNonLethalStrikeAndReturn(targetBoardPosition, tileSize); // 전용 코루틴에 위임
        }

        public static void PlayHitReaction(this PieceView view, float duration = 0.18f) // 생존 피격 시 짧게 흔들리는 반응
        {
            if (view == null) return; // 뷰가 없으면 처리하지 않음
            PieceViewAnimationBridge.GetOrAdd(view).PlayHitReaction(duration); // 전용 코루틴에 위임
        }

        public static void PlayDeathTogglingThenDestroy(this PieceView view, Action onDestroyed, float duration = 0.35f) // 사망 시 쓰러진 뒤 호출부가 전달한 제거 콜백 실행
        {
            if (view == null) // 뷰가 이미 없으면
            {
                onDestroyed?.Invoke(); // 제거 콜백만 안전하게 실행
                return; // 더 이상 연출하지 않음
            }

            PieceViewAnimationBridge.GetOrAdd(view).PlayDeathToppling(onDestroyed, duration); // 전도 연출 브리지에 위임
        }

        private static void ApplyBoardPosition(PieceView view, Vector2Int boardPosition, float tileSize) // SnapTo에서 사용하는 즉시 위치 반영 도우미
        {
            string displayName = view.RuntimeState != null && view.RuntimeState.Definition != null ? view.RuntimeState.Definition.DisplayName : "Piece"; // 안전한 표시 이름 계산
            view.name = $"Piece_{displayName}_{boardPosition.x}_{boardPosition.y}"; // 기존 PieceView 계층 이름 규칙 유지
            view.transform.localPosition = BoardView.BoardToLocalPosition(boardPosition, tileSize); // 보드 좌표를 로컬 위치로 변환해 즉시 반영
        }
    }

    internal sealed class PieceViewAnimationBridge : MonoBehaviour // 구버전 PieceView를 직접 수정하지 않고 30일차 연출 코루틴을 제공하는 보조 컴포넌트
    {
        private const float StrikeHopHeight = 0.4f; // 기존 30일차 근접 타격 상승 높이와 같은 임시값
        private const float StrikeApproachFraction = 0.55f; // 기존 30일차 목표 접근 비율과 같은 임시값
        private Coroutine _positionCoroutine; // 위치를 바꾸는 타격 연출 코루틴
        private Coroutine _reactionCoroutine; // 피격 회전 연출 코루틴

        public static PieceViewAnimationBridge GetOrAdd(PieceView view) // PieceView 오브젝트에 브리지를 하나만 확보하는 메서드
        {
            var bridge = view.GetComponent<PieceViewAnimationBridge>(); // 기존 브리지 탐색
            if (bridge == null) bridge = view.gameObject.AddComponent<PieceViewAnimationBridge>(); // 없으면 런타임에 추가
            return bridge; // 확보한 브리지 반환
        }

        public void StopPositionAnimation() // SnapTo가 즉시 위치를 제어하기 전에 진행 중 연출을 정리
        {
            if (_positionCoroutine == null) return; // 진행 중 위치 코루틴이 없으면 종료
            StopCoroutine(_positionCoroutine); // 기존 위치 연출 중단
            _positionCoroutine = null; // 참조 초기화
        }

        public void PlayNonLethalStrikeAndReturn(Vector2Int targetBoardPosition, float tileSize) // 비치명 공격 연출 시작
        {
            StopPositionAnimation(); // 이전 위치 연출과 겹치지 않게 정리
            _positionCoroutine = StartCoroutine(AnimateNonLethalStrike(targetBoardPosition, tileSize)); // 새 접근·복귀 코루틴 시작
        }

        public void PlayHitReaction(float duration) // 피격 흔들림 연출 시작
        {
            if (_reactionCoroutine != null) StopCoroutine(_reactionCoroutine); // 이전 흔들림이 남아 있으면 먼저 중단
            _reactionCoroutine = StartCoroutine(AnimateHitReaction(Mathf.Max(0.01f, duration))); // 0초 나눗셈을 막고 새 연출 시작
        }

        public void PlayDeathToppling(Action onDestroyed, float duration) // 사망 전도 연출 시작
        {
            StartCoroutine(AnimateDeathToppling(onDestroyed, Mathf.Max(0.01f, duration))); // 최소 지속 시간을 보장해 연출 시작
        }

        private IEnumerator AnimateNonLethalStrike(Vector2Int targetBoardPosition, float tileSize) // 상승→접근→복귀의 간단한 30일차 호환 연출
        {
            Vector3 origin = transform.localPosition; // 공격 시작 원위치 저장
            Vector3 target = BoardView.BoardToLocalPosition(targetBoardPosition, tileSize); // 공격 대상 칸 위치 계산
            Vector3 approach = Vector3.Lerp(origin, target, StrikeApproachFraction); // 대상 칸을 완전히 침범하지 않는 접근 지점 계산

            float duration = 0.12f; // 상승 구간 임시 지속 시간
            float elapsed = 0f; // 구간 경과 시간
            while (elapsed < duration) // 제자리 상승
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                float t = Mathf.Clamp01(elapsed / duration); // 0~1 진행률 계산
                transform.localPosition = origin + Vector3.up * (StrikeHopHeight * t); // 원위치에서 위로 상승
                yield return null; // 다음 프레임까지 대기
            }

            Vector3 raised = origin + Vector3.up * StrikeHopHeight; // 상승 완료 위치
            duration = 0.12f; // 접근 구간 임시 지속 시간
            elapsed = 0f; // 경과 시간 초기화
            while (elapsed < duration) // 목표 쪽으로 내려찍기
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                float t = Mathf.Clamp01(elapsed / duration); // 진행률 계산
                transform.localPosition = Vector3.Lerp(raised, approach, t); // 상승 위치에서 접근 위치로 이동
                yield return null; // 다음 프레임까지 대기
            }

            duration = 0.16f; // 복귀 구간 임시 지속 시간
            elapsed = 0f; // 경과 시간 초기화
            while (elapsed < duration) // 원위치 복귀
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                float t = Mathf.Clamp01(elapsed / duration); // 진행률 계산
                float arc = StrikeHopHeight * 4f * t * (1f - t); // 중간에 높아지는 간단한 포물선 계산
                transform.localPosition = Vector3.Lerp(approach, origin, t) + Vector3.up * arc; // 포물선을 그리며 복귀
                yield return null; // 다음 프레임까지 대기
            }

            transform.localPosition = origin; // 최종 위치를 정확히 원위치로 고정
            _positionCoroutine = null; // 위치 코루틴 참조 초기화
        }

        private IEnumerator AnimateHitReaction(float duration) // 짧게 기울었다 원래 회전으로 돌아오는 피격 연출
        {
            Quaternion original = transform.localRotation; // 원래 회전 저장
            Quaternion tilted = original * Quaternion.Euler(0f, 0f, 12f); // 한쪽으로 살짝 기운 회전 계산
            float half = duration * 0.5f; // 기울기와 복귀에 절반씩 사용
            float elapsed = 0f; // 경과 시간

            while (elapsed < half) // 기울어지는 구간
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                transform.localRotation = Quaternion.Slerp(original, tilted, Mathf.Clamp01(elapsed / half)); // 원래 회전에서 기운 회전으로 보간
                yield return null; // 다음 프레임 대기
            }

            elapsed = 0f; // 복귀 구간을 위해 초기화
            while (elapsed < half) // 원래 회전으로 돌아오는 구간
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                transform.localRotation = Quaternion.Slerp(tilted, original, Mathf.Clamp01(elapsed / half)); // 원래 회전으로 보간
                yield return null; // 다음 프레임 대기
            }

            transform.localRotation = original; // 최종 회전값 정확히 복구
            _reactionCoroutine = null; // 반응 코루틴 참조 초기화
        }

        private IEnumerator AnimateDeathToppling(Action onDestroyed, float duration) // 무작위 방향으로 90도 쓰러진 뒤 제거 콜백 실행
        {
            Vector3[] fallAxes = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right }; // 네 방향 후보
            Vector3 axis = fallAxes[UnityEngine.Random.Range(0, fallAxes.Length)]; // 이번 사망의 전도 방향 무작위 선택
            Quaternion start = transform.localRotation; // 시작 회전 저장
            Quaternion end = start * Quaternion.AngleAxis(90f, axis); // 선택 방향으로 90도 쓰러진 최종 회전 계산
            float elapsed = 0f; // 경과 시간

            while (elapsed < duration) // 지정 시간 동안 쓰러짐 연출
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                transform.localRotation = Quaternion.Slerp(start, end, Mathf.Clamp01(elapsed / duration)); // 부드럽게 최종 회전으로 보간
                yield return null; // 다음 프레임까지 대기
            }

            transform.localRotation = end; // 최종 전도 상태 고정
            yield return new WaitForSeconds(0.08f); // 짧게 쓰러진 상태를 보여줌
            onDestroyed?.Invoke(); // 호출부가 전달한 실제 제거 처리 실행
        }
    }
}
