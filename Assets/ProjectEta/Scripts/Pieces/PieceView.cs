using System; // Action 콜백을 사용하기 위한 네임스페이스
using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, PrimitiveType 등을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView.BoardToLocalPosition을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public class PieceView : MonoBehaviour // 기물 데이터를 3D 화면에 표시하는 컴포넌트
    {
        [SerializeField] private Color _playerColor = new Color(0.15f, 0.4f, 0.9f); // 아군 기물 색상
        [SerializeField] private Color _enemyColor = new Color(0.9f, 0.2f, 0.2f); // 적군 기물 색상

        [Header("연출 임시값(30일차)")] // 인스펙터 연출 값 구분선(모두 테스트용 임시값)
        [SerializeField] private float _moveRiseDuration = 0.08f; // 이동 시작 시 떠오르는 데 걸리는 시간
        [SerializeField] private float _moveTranslateDuration = 0.16f; // 뜬 채로 목표 칸까지 이동하는 시간
        [SerializeField] private float _moveLandDuration = 0.07f; // 목표 칸 위에서 착지하며 내려앉는 시간
        [SerializeField] private float _moveHopHeight = 0.18f; // 이동 중 공중에 떠 있는 높이
        [SerializeField] private float _strikeHopHeight = 0.4f; // 근접 공격 연출에서 높게 떠오르는 높이
        [SerializeField] private float _strikeApproachFraction = 0.55f; // 목표 쪽으로 다가가는 비율(목표 칸까지는 가지 않음)

        private Coroutine _positionAnimationCoroutine; // 이동·타격 연출처럼 위치를 다루는 코루틴(동시에 하나만 실행)
        private Coroutine _reactionCoroutine; // 피격 흔들림 연출 코루틴

        public PieceRuntimeState RuntimeState { get; private set; } // 이 뷰가 표시하는 런타임 상태

        public void Initialize(PieceRuntimeState runtimeState, float tileSize) // 외부에서 데이터를 주입해 초기화하는 메서드
        {
            RuntimeState = runtimeState; // 런타임 상태 저장
            ApplyBoardPosition(runtimeState.BoardPosition, tileSize); // 계층창 이름과 3D 위치를 현재 좌표에 맞춤(최초 배치는 연출 없이 즉시)

            var material = CreatePieceMaterial(runtimeState.IsPlayerPiece ? _playerColor : _enemyColor); // 아군/적군에 따라 머티리얼 생성
            BuildModel(runtimeState.Definition, material); // PieceId 기준으로 어울리는 3D 모델 생성
            AttachSelectionCollider(); // 클릭 판정용 단일 콜라이더 부착
        }

        public void MoveTo(Vector2Int boardPosition, float tileSize) // 실제 이동 실행 시 화면 위치를 새 좌표로 갱신하는 메서드(살짝 떠서 이동한 뒤 착지하는 연출)
        {
            string displayName = RuntimeState != null && RuntimeState.Definition != null ? RuntimeState.Definition.DisplayName : "Piece"; // 안전한 표시 이름 계산
            name = $"Piece_{displayName}_{boardPosition.x}_{boardPosition.y}"; // 계층창 이름은 연출과 무관하게 최종 좌표 기준으로 즉시 갱신

            Vector3 targetLocalPosition = BoardView.BoardToLocalPosition(boardPosition, tileSize); // 최종 로컬 위치 계산
            RestartPositionCoroutine(AnimateHoveringMove(targetLocalPosition)); // 기존 위치 연출을 멈추고 부양 이동 시작
        }

        public void SnapTo(Vector2Int boardPosition, float tileSize) // 카드 드래그 고스트처럼 매 프레임 마우스를 그대로 따라가야 할 때 연출 없이 즉시 위치를 갱신하는 메서드
        {
            if (_positionAnimationCoroutine != null) // 혹시 위치 연출이 재생 중이었다면
            {
                StopCoroutine(_positionAnimationCoroutine); // 즉시 위치 갱신과 겹치지 않도록 먼저 중단
                _positionAnimationCoroutine = null; // 참조 정리
            }

            ApplyBoardPosition(boardPosition, tileSize); // 이름과 3D 위치를 새 좌표 기준으로 즉시 갱신(연출 없음)
        }

        public void PlayNonLethalStrikeAndReturn(Vector2Int targetBoardPosition, float tileSize) // 30일차: 비치명 공격 시 목표 쪽으로 다가가 타격한 뒤 원위치로 복귀하는 연출
        {
            RestartPositionCoroutine(AnimateNonLethalStrike(targetBoardPosition, tileSize)); // 기존 위치 연출을 멈추고 타격 연출 시작
        }

        public void PlayHitReaction(float duration = 0.18f) // 30일차: 공격을 받고 생존했을 때 짧게 흔들리는 피격 반응 연출
        {
            if (_reactionCoroutine != null) // 이전 피격 반응이 아직 재생 중이면
            {
                StopCoroutine(_reactionCoroutine); // 중복 재생을 막기 위해 먼저 중단
            }

            _reactionCoroutine = StartCoroutine(AnimateHitReaction(duration)); // 새 피격 반응 시작
        }

        public void PlayDeathTogglingThenDestroy(Action onDestroyed, float duration = 0.35f) // 30일차: 사망 시 무작위 방향으로 쓰러진 뒤 콜백으로 실제 제거를 위임하는 연출
        {
            StartCoroutine(AnimateDeathToppling(onDestroyed, duration)); // 쓰러짐 연출 시작(완료 후 콜백 호출)
        }

        private void RestartPositionCoroutine(IEnumerator routine) // 위치를 다루는 연출을 안전하게 교체 실행하는 공통 도우미
        {
            if (_positionAnimationCoroutine != null) // 이전 위치 연출이 아직 재생 중이면
            {
                StopCoroutine(_positionAnimationCoroutine); // 서로 다른 두 연출이 같은 위치를 동시에 제어하지 않도록 먼저 중단
            }

            _positionAnimationCoroutine = StartCoroutine(routine); // 새 위치 연출 시작
        }

        private IEnumerator AnimateHoveringMove(Vector3 targetLocalPosition) // 살짝 떠오른 뒤 그 높이를 유지한 채 이동하고 마지막에 착지하는 코루틴
        {
            Vector3 startLocalPosition = transform.localPosition; // 이동 시작 시점의 현재 위치

            float elapsed = 0f; // 1) 상승 구간: 제자리에서 떠오름
            while (elapsed < _moveRiseDuration)
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                float t = _moveRiseDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / _moveRiseDuration); // 0~1 진행률
                transform.localPosition = startLocalPosition + Vector3.up * (_moveHopHeight * t); // 제자리에서 서서히 상승
                yield return null; // 다음 프레임까지 대기
            }

            Vector3 hoverStart = startLocalPosition + Vector3.up * _moveHopHeight; // 뜬 높이에서의 출발 위치
            Vector3 hoverEnd = targetLocalPosition + Vector3.up * _moveHopHeight; // 뜬 높이에서의 도착 위치(목표 칸 바로 위)

            elapsed = 0f; // 2) 이동 구간: 뜬 높이를 유지한 채 수평 이동
            while (elapsed < _moveTranslateDuration)
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                float t = _moveTranslateDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / _moveTranslateDuration); // 0~1 진행률
                transform.localPosition = Vector3.Lerp(hoverStart, hoverEnd, t); // 같은 높이를 유지하며 목표 칸 위까지 이동
                yield return null; // 다음 프레임까지 대기
            }

            elapsed = 0f; // 3) 착지 구간: 목표 칸 위에서 내려앉음
            while (elapsed < _moveLandDuration)
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                float t = _moveLandDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / _moveLandDuration); // 0~1 진행률
                transform.localPosition = Vector3.Lerp(hoverEnd, targetLocalPosition, t); // 목표 칸 위에서 바닥까지 하강
                yield return null; // 다음 프레임까지 대기
            }

            transform.localPosition = targetLocalPosition; // 오차 누적 방지를 위해 최종 위치를 정확히 고정("착" 착지)
            _positionAnimationCoroutine = null; // 완료 후 참조 정리
        }

        private IEnumerator AnimateNonLethalStrike(Vector2Int targetBoardPosition, float tileSize) // 상승→접근→타격→복귀 단계를 실제로 재생하는 코루틴
        {
            var stateMachine = new AttackAnimationStateMachine(); // 30일차: 단계 전이만 담당하는 순수 상태 머신
            stateMachine.Start(); // 상승 단계부터 시작

            Vector3 originLocalPosition = transform.localPosition; // 복귀 기준이 되는 현재(원래) 위치
            Vector3 targetLocalPosition = BoardView.BoardToLocalPosition(targetBoardPosition, tileSize); // 목표 칸의 3D 위치
            Vector3 approachLocalPosition = Vector3.Lerp(originLocalPosition, targetLocalPosition, _strikeApproachFraction); // 실제로 다가갈 지점(목표 칸까지는 가지 않음)

            while (!stateMachine.IsComplete) // 연출이 모두 끝날 때까지
            {
                stateMachine.Advance(Time.deltaTime); // 상태 머신에 경과 시간 전달
                float progress = stateMachine.GetPhaseProgress01(); // 현재 단계 안에서의 진행률

                switch (stateMachine.CurrentPhase) // 현재 단계에 따라 위치 계산
                {
                    case AttackAnimationPhase.Rising: // 제자리에서 높이 떠오르는 단계
                        transform.localPosition = originLocalPosition + Vector3.up * (_strikeHopHeight * progress); // 원위치 기준으로 서서히 최고 높이까지 상승
                        break;
                    case AttackAnimationPhase.Approaching: // 목표 쪽으로 포물선을 그리며 내려찍는 단계
                        float diveHeight = _strikeHopHeight * (1f - progress * progress); // 최고 높이에서 목표 지점까지 가속하며 하강(포물선의 내려오는 절반)
                        transform.localPosition = Vector3.Lerp(originLocalPosition, approachLocalPosition, progress) + Vector3.up * diveHeight; // 접근하면서 동시에 하강
                        break;
                    case AttackAnimationPhase.Striking: // 접근 지점(바닥)에서 짧게 멈춰 타격하는 단계
                        transform.localPosition = approachLocalPosition; // 완전히 내려찍은 상태로 타격 순간을 표현
                        break;
                    case AttackAnimationPhase.Recovering: // 원위치까지 다시 포물선으로 뛰어 복귀하는 단계
                        float returnArc = _strikeHopHeight * 4f * progress * (1f - progress); // 0에서 시작해 중간에 최고 높이를 찍고 다시 0으로 내려오는 완전한 포물선
                        transform.localPosition = Vector3.Lerp(approachLocalPosition, originLocalPosition, progress) + Vector3.up * returnArc; // 복귀 이동과 포물선 궤적을 함께 적용
                        break;
                }

                yield return null; // 다음 프레임까지 대기
            }

            transform.localPosition = originLocalPosition; // 오차 누적 방지를 위해 정확히 원위치로 고정
            _positionAnimationCoroutine = null; // 완료 후 참조 정리
        }

        private IEnumerator AnimateHitReaction(float duration) // 짧게 기울었다 돌아오는 피격 반응을 재생하는 코루틴
        {
            Quaternion originalRotation = transform.localRotation; // 원래 회전값
            Quaternion tiltRotation = originalRotation * Quaternion.Euler(0f, 0f, 12f); // 살짝 기우는 흔들림(임시값)
            float half = duration * 0.5f; // 기울어지는 절반과 돌아오는 절반으로 구성
            float elapsed = 0f; // 경과 시간

            while (elapsed < half) // 기우는 절반 구간
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                transform.localRotation = Quaternion.Slerp(originalRotation, tiltRotation, Mathf.Clamp01(elapsed / half)); // 원래 회전에서 기운 회전으로 보간
                yield return null; // 다음 프레임까지 대기
            }

            elapsed = 0f; // 돌아오는 절반을 위해 경과 시간 재사용
            while (elapsed < half) // 돌아오는 절반 구간
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                transform.localRotation = Quaternion.Slerp(tiltRotation, originalRotation, Mathf.Clamp01(elapsed / half)); // 기운 회전에서 원래 회전으로 보간
                yield return null; // 다음 프레임까지 대기
            }

            transform.localRotation = originalRotation; // 오차 누적 방지를 위해 정확히 원래 회전으로 고정
            _reactionCoroutine = null; // 완료 후 참조 정리
        }

        private IEnumerator AnimateDeathToppling(Action onDestroyed, float duration) // 무작위 방향으로 쓰러진 뒤 콜백을 호출하는 코루틴
        {
            Vector3[] fallAxes = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right }; // 기획서 8.6: 여러 방향 중 하나로 쓰러지는 애니메이션
            Vector3 axis = fallAxes[UnityEngine.Random.Range(0, fallAxes.Length)]; // 4방향 중 무작위 전도 축 선택
            Quaternion startRotation = transform.localRotation; // 쓰러지기 전 원래 회전값
            Quaternion fallenRotation = startRotation * Quaternion.AngleAxis(85f, axis); // 완전히 쓰러진 회전값(임시값)
            float elapsed = 0f; // 경과 시간

            while (elapsed < duration) // 목표 소요 시간에 도달할 때까지
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                transform.localRotation = Quaternion.Slerp(startRotation, fallenRotation, Mathf.Clamp01(elapsed / duration)); // 서서히 쓰러지도록 보간
                yield return null; // 다음 프레임까지 대기
            }

            onDestroyed?.Invoke(); // 쓰러짐 연출이 끝난 뒤 실제 제거를 호출부에 위임
        }

        private void ApplyBoardPosition(Vector2Int boardPosition, float tileSize) // 좌표에 맞춰 이름과 3D 위치를 함께 갱신하는 공통 메서드
        {
            string displayName = RuntimeState != null && RuntimeState.Definition != null ? RuntimeState.Definition.DisplayName : "Piece"; // 안전한 표시 이름 계산
            name = $"Piece_{displayName}_{boardPosition.x}_{boardPosition.y}"; // 계층창에서 구분되도록 이름 지정
            transform.localPosition = BoardView.BoardToLocalPosition(boardPosition, tileSize); // 보드 좌표를 3D 위치로 변환해 배치
        }

        public static string GetModelKey(PieceDefinition definition) // 테스트와 디버그에서 현재 기물의 모델 분기를 확인하는 메서드
        {
            if (definition == null) return "pawn"; // 정의가 없으면 안전한 기본 모델 사용
            if (!string.IsNullOrWhiteSpace(definition.PieceId)) return definition.PieceId.ToLowerInvariant(); // PieceId 우선 사용
            if (!string.IsNullOrWhiteSpace(definition.name)) return definition.name.ToLowerInvariant(); // 에셋 이름 대체 사용
            return "pawn"; // 아무 정보도 없으면 폰 모델 사용
        }

        private void BuildModel(PieceDefinition definition, Material material) // 기물 id별 모델을 만드는 메서드
        {
            var model = new GameObject("Model"); // 모델 파츠를 담을 빈 오브젝트 생성
            model.transform.SetParent(transform, false); // 이 컴포넌트의 자식으로 배치

            switch (GetModelKey(definition)) // 26일차: 26종 전체의 전용 실루엣 분기
            {
                case "king":
                    BuildKingModel(model.transform, material); // 킹 모델 생성
                    break;
                case "pawn":
                    BuildPawnModel(model.transform, material); // 폰 모델 생성
                    break;
                case "knight":
                    BuildKnightModel(model.transform, material); // 나이트 모델 생성
                    break;
                case "bishop":
                    BuildBishopModel(model.transform, material); // 비숍 모델 생성
                    break;
                case "rook":
                    BuildRookModel(model.transform, material); // 룩 모델 생성
                    break;
                case "queen":
                    BuildQueenModel(model.transform, material); // 퀸 모델 생성
                    break;
                case "archbishop":
                    BuildArchbishopModel(model.transform, material); // 아크비숍 모델 생성
                    break;
                case "chancellor":
                    BuildChancellorModel(model.transform, material); // 챈슬러 모델 생성
                    break;
                case "amazon":
                    BuildAmazonModel(model.transform, material); // 아마존 모델 생성
                    break;
                case "wazir":
                    BuildWazirModel(model.transform, material); // 와지르 모델 생성
                    break;
                case "ferz":
                    BuildFerzModel(model.transform, material); // 페르즈 모델 생성
                    break;
                case "mann":
                    BuildMannModel(model.transform, material); // 만 모델 생성
                    break;
                case "dabbaba":
                    BuildDabbabaModel(model.transform, material); // 다바바 모델 생성
                    break;
                case "alfil":
                    BuildAlfilModel(model.transform, material); // 알필 모델 생성
                    break;
                case "camel":
                    BuildCamelModel(model.transform, material); // 카멜 모델 생성
                    break;
                case "zebra":
                    BuildZebraModel(model.transform, material); // 제브라 모델 생성
                    break;
                case "centaur":
                    BuildCentaurModel(model.transform, material); // 센타우르 모델 생성
                    break;
                case "waffle":
                    BuildWaffleModel(model.transform, material); // 와플 모델 생성
                    break;
                case "nightrider":
                    BuildNightriderModel(model.transform, material); // 나이트라이더 모델 생성
                    break;
                case "camelrider":
                    BuildCamelriderModel(model.transform, material); // 카멜라이더 모델 생성
                    break;
                case "grasshopper":
                    BuildGrasshopperModel(model.transform, material); // 그래스호퍼 모델 생성
                    break;
                case "cannon":
                    BuildCannonModel(model.transform, material); // 캐논 모델 생성
                    break;
                case "canvasser":
                    BuildCanvasserModel(model.transform, material); // 캔버서 모델 생성
                    break;
                case "caliph":
                    BuildCaliphModel(model.transform, material); // 칼리프 모델 생성
                    break;
                case "squirrel":
                    BuildSquirrelModel(model.transform, material); // 스쿼럴 모델 생성
                    break;
                case "chameleon":
                    BuildChameleonModel(model.transform, material); // 카멜레온 모델 생성
                    break;
                default:
                    BuildPawnModel(model.transform, material); // 알 수 없는 기물은 폰 모델로 대체
                    break;
            }
        }

        private static void BuildPawnModel(Transform parent, Material material) // 폰 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.3f, 0.04f, 0.3f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.14f, 0.22f, 0.14f), material); // 몸통 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.61f, 0f), new Vector3(0.18f, 0.18f, 0.18f), material); // 머리 파츠 생성
        }

        private static void BuildKingModel(Transform parent, Material material) // 킹 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.05f, 0.36f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.2f, 0.35f, 0.2f), material); // 기둥 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.92f, 0f), new Vector3(0.24f, 0.24f, 0.24f), material); // 머리 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 1.14f, 0f), new Vector3(0.06f, 0.2f, 0.06f), material); // 십자가 세로 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 1.14f, 0f), new Vector3(0.18f, 0.06f, 0.06f), material); // 십자가 가로 파츠 생성
        }

        private static void BuildKnightModel(Transform parent, Material material) // 나이트 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.34f, 0.05f, 0.34f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(0.16f, 0.2f, 0.16f), material); // 몸통 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.56f, 0.02f), new Vector3(0.16f, 0.3f, 0.22f), material, Quaternion.Euler(20f, 0f, 0f)); // 머리 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.66f, 0.16f), new Vector3(0.1f, 0.1f, 0.18f), material, Quaternion.Euler(-15f, 0f, 0f)); // 주둥이 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.05f, 0.74f, -0.02f), new Vector3(0.05f, 0.1f, 0.05f), material, Quaternion.Euler(25f, 0f, -20f)); // 귀 파츠 생성
        }

        private static void BuildBishopModel(Transform parent, Material material) // 비숍 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.32f, 0.05f, 0.32f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0f), new Vector3(0.15f, 0.37f, 0.15f), material); // 몸통 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.86f, 0f), new Vector3(0.2f, 0.2f, 0.2f), material); // 머리 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 1.04f, 0f), new Vector3(0.08f, 0.08f, 0.08f), material); // 꼭대기 구슬 생성
        }

        private static void BuildRookModel(Transform parent, Material material) // 룩 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.05f, 0.36f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(0.26f, 0.3f, 0.26f), material); // 몸통 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.68f, 0f), new Vector3(0.32f, 0.04f, 0.32f), material); // 상단 원판 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.22f, 0.78f, 0f), new Vector3(0.1f, 0.12f, 0.1f), material); // 흉벽 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.22f, 0.78f, 0f), new Vector3(0.1f, 0.12f, 0.1f), material); // 흉벽 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.22f), new Vector3(0.1f, 0.12f, 0.1f), material); // 흉벽 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.78f, -0.22f), new Vector3(0.1f, 0.12f, 0.1f), material); // 흉벽 파츠 생성
        }

        private static void BuildQueenModel(Transform parent, Material material) // 퀸 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.05f, 0.36f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.19f, 0.45f, 0.19f), material); // 몸통 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 1.0f, 0f), new Vector3(0.26f, 0.26f, 0.26f), material); // 머리 파츠 생성

            const int spikeCount = 5; // 왕관 스파이크 개수
            const float radius = 0.14f; // 스파이크 반경
            const float spikeY = 1.16f; // 스파이크 높이
            for (int i = 0; i < spikeCount; i++) // 스파이크를 원형으로 배치
            {
                float angle = i * Mathf.PI * 2f / spikeCount; // 이번 스파이크의 각도 계산
                var spikePosition = new Vector3(Mathf.Cos(angle) * radius, spikeY, Mathf.Sin(angle) * radius); // 위치 계산
                CreatePart(parent, PrimitiveType.Sphere, spikePosition, new Vector3(0.07f, 0.07f, 0.07f), material); // 스파이크 파츠 생성
            }
        }

        private static void BuildArchbishopModel(Transform parent, Material material) // 비숍과 나이트의 느낌을 섞은 모델을 만드는 메서드
        {
            BuildBishopModel(parent, material); // 비숍 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.66f, 0.17f), new Vector3(0.11f, 0.1f, 0.18f), material, Quaternion.Euler(-10f, 0f, 0f)); // 말머리 느낌의 주둥이 추가
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.06f, 0.76f, -0.02f), new Vector3(0.05f, 0.11f, 0.05f), material, Quaternion.Euler(20f, 0f, -18f)); // 귀 추가
        }

        private static void BuildChancellorModel(Transform parent, Material material) // 룩과 나이트의 느낌을 섞은 모델을 만드는 메서드
        {
            BuildRookModel(parent, material); // 룩 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0.12f), new Vector3(0.16f, 0.16f, 0.2f), material, Quaternion.Euler(16f, 0f, 0f)); // 전면 말머리 파츠 추가
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.7f, 0.2f), new Vector3(0.1f, 0.08f, 0.16f), material, Quaternion.Euler(-18f, 0f, 0f)); // 주둥이 파츠 추가
        }

        private static void BuildAmazonModel(Transform parent, Material material) // 퀸과 나이트의 느낌을 섞은 모델을 만드는 메서드
        {
            BuildQueenModel(parent, material); // 퀸 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.73f, 0.2f), new Vector3(0.12f, 0.11f, 0.18f), material, Quaternion.Euler(-12f, 0f, 0f)); // 전면 말머리 파츠 추가
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.05f, 0.86f, 0.04f), new Vector3(0.05f, 0.1f, 0.05f), material, Quaternion.Euler(22f, 0f, -12f)); // 귀 파츠 추가
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.22f, 0.9f, 0f), new Vector3(0.06f, 0.18f, 0.2f), material, Quaternion.Euler(0f, 0f, 26f)); // 오른쪽 날개 장식
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.22f, 0.9f, 0f), new Vector3(0.06f, 0.18f, 0.2f), material, Quaternion.Euler(0f, 0f, -26f)); // 왼쪽 날개 장식
        }

        private static void BuildWazirModel(Transform parent, Material material) // 십자 한 칸의 짧고 단단한 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.28f, 0.05f, 0.28f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.26f, 0f), new Vector3(0.14f, 0.18f, 0.14f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.48f, 0f), new Vector3(0.28f, 0.08f, 0.08f), material); // 가로 팔 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.48f, 0f), new Vector3(0.08f, 0.08f, 0.28f), material); // 세로 팔 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.66f, 0f), new Vector3(0.14f, 0.14f, 0.14f), material); // 머리 구슬 생성
        }

        private static void BuildFerzModel(Transform parent, Material material) // 대각 한 칸의 날카로운 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.28f, 0.05f, 0.28f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.24f, 0f), new Vector3(0.12f, 0.16f, 0.12f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.52f, 0f), new Vector3(0.1f, 0.34f, 0.1f), material, Quaternion.Euler(0f, 0f, 45f)); // 대각 기둥 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.78f, 0f), new Vector3(0.12f, 0.12f, 0.12f), material); // 머리 구슬 생성
        }

        private static void BuildMannModel(Transform parent, Material material) // 모든 방향 한 칸의 인간형 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.3f, 0.05f, 0.3f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.16f, 0.22f, 0.16f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.64f, 0f), new Vector3(0.16f, 0.16f, 0.16f), material); // 머리 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.17f, 0.36f, 0f), new Vector3(0.12f, 0.08f, 0.08f), material); // 오른팔 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.17f, 0.36f, 0f), new Vector3(0.12f, 0.08f, 0.08f), material); // 왼팔 생성
        }

        private static void BuildDabbabaModel(Transform parent, Material material) // 두 칸 도약의 묵직한 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(0.42f, 0.08f, 0.42f), material); // 큰 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f), new Vector3(0.24f, 0.18f, 0.24f), material); // 두꺼운 몸체 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0.15f, 0.54f, 0f), new Vector3(0.1f, 0.12f, 0.1f), material); // 오른쪽 북 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(-0.15f, 0.54f, 0f), new Vector3(0.1f, 0.12f, 0.1f), material); // 왼쪽 북 생성
        }

        private static void BuildAlfilModel(Transform parent, Material material) // 고전 코끼리 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(0.33f, 0.06f, 0.33f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(0.26f, 0.18f, 0.22f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.54f, 0.14f), new Vector3(0.16f, 0.16f, 0.18f), material); // 머리 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.11f, 0.48f, 0.26f), new Vector3(0.04f, 0.14f, 0.04f), material, Quaternion.Euler(0f, 0f, 20f)); // 오른쪽 엄니 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.11f, 0.48f, 0.26f), new Vector3(0.04f, 0.14f, 0.04f), material, Quaternion.Euler(0f, 0f, -20f)); // 왼쪽 엄니 생성
        }

        private static void BuildCamelModel(Transform parent, Material material) // 낙타 등 형태를 가진 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.34f, 0.05f, 0.34f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.24f, 0f), new Vector3(0.28f, 0.12f, 0.18f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(-0.08f, 0.43f, 0f), new Vector3(0.16f, 0.16f, 0.16f), material); // 첫 번째 혹 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0.08f, 0.43f, 0f), new Vector3(0.16f, 0.16f, 0.16f), material); // 두 번째 혹 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.56f, 0.14f), new Vector3(0.08f, 0.22f, 0.08f), material, Quaternion.Euler(18f, 0f, 0f)); // 목 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.7f, 0.22f), new Vector3(0.1f, 0.1f, 0.12f), material); // 머리 생성
        }

        private static void BuildZebraModel(Transform parent, Material material) // 얼룩말의 줄무늬 느낌을 가진 모델을 만드는 메서드
        {
            BuildKnightModel(parent, material); // 말 계열 실루엣을 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.38f, -0.08f), new Vector3(0.18f, 0.03f, 0.24f), material, Quaternion.Euler(0f, 0f, 90f)); // 등줄기 무늬 1 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.5f, -0.02f), new Vector3(0.18f, 0.03f, 0.24f), material, Quaternion.Euler(0f, 0f, 90f)); // 등줄기 무늬 2 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.04f), new Vector3(0.16f, 0.03f, 0.2f), material, Quaternion.Euler(0f, 0f, 90f)); // 등줄기 무늬 3 생성
        }

        private static void BuildCentaurModel(Transform parent, Material material) // 말 몸체와 상체를 결합한 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.34f, 0.04f, 0.34f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.34f, 0.14f, 0.2f), material); // 말 몸체 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.42f, -0.02f), new Vector3(0.14f, 0.22f, 0.12f), material); // 인간 상체 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.66f, -0.02f), new Vector3(0.14f, 0.14f, 0.14f), material); // 머리 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.48f, 0.18f), new Vector3(0.06f, 0.36f, 0.06f), material, Quaternion.Euler(-25f, 0f, 0f)); // 창 생성
        }

        private static void BuildWaffleModel(Transform parent, Material material) // 격자 타일 느낌을 가진 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f), new Vector3(0.4f, 0.06f, 0.4f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.24f, 0f), new Vector3(0.32f, 0.12f, 0.32f), material); // 본체 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f), new Vector3(0.34f, 0.04f, 0.08f), material); // 가로 홈 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f), new Vector3(0.08f, 0.04f, 0.34f), material); // 세로 홈 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.48f, 0f), new Vector3(0.1f, 0.1f, 0.1f), material); // 상단 구슬 생성
        }

        private static void BuildNightriderModel(Transform parent, Material material) // 연속 나이트 도약의 길쭉한 기사 느낌을 주는 모델을 만드는 메서드
        {
            BuildKnightModel(parent, material); // 나이트 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0f), new Vector3(0.08f, 0.16f, 0.08f), material); // 긴 깃대 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.14f, 1.02f, 0f), new Vector3(0.22f, 0.12f, 0.04f), material); // 깃발 생성
        }

        private static void BuildCamelriderModel(Transform parent, Material material) // 카멜 위에 기수가 올라탄 모델을 만드는 메서드
        {
            BuildCamelModel(parent, material); // 카멜 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.62f, -0.02f), new Vector3(0.1f, 0.14f, 0.08f), material); // 기수 몸통 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.76f, -0.02f), new Vector3(0.08f, 0.08f, 0.08f), material); // 기수 머리 생성
        }

        private static void BuildGrasshopperModel(Transform parent, Material material) // 긴 다리와 도약 감각을 가진 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.28f, 0.04f, 0.28f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f), new Vector3(0.18f, 0.12f, 0.18f), material); // 몸체 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.12f, 0.18f, 0f), new Vector3(0.04f, 0.24f, 0.04f), material, Quaternion.Euler(0f, 0f, -28f)); // 오른쪽 다리 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.12f, 0.18f, 0f), new Vector3(0.04f, 0.24f, 0.04f), material, Quaternion.Euler(0f, 0f, 28f)); // 왼쪽 다리 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.52f, 0.1f), new Vector3(0.04f, 0.16f, 0.04f), material, Quaternion.Euler(24f, 0f, 0f)); // 더듬이 1 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.52f, -0.1f), new Vector3(0.04f, 0.16f, 0.04f), material, Quaternion.Euler(-24f, 0f, 0f)); // 더듬이 2 생성
        }

        private static void BuildCannonModel(Transform parent, Material material) // 포신과 바퀴를 가진 원거리 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(0.34f, 0.08f, 0.26f), material); // 포대 받침 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.26f, 0f), new Vector3(0.12f, 0.28f, 0.12f), material, Quaternion.Euler(90f, 0f, 0f)); // 포신 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0.22f, 0.12f, 0f), new Vector3(0.12f, 0.04f, 0.12f), material, Quaternion.Euler(90f, 0f, 0f)); // 오른쪽 바퀴 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(-0.22f, 0.12f, 0f), new Vector3(0.12f, 0.04f, 0.12f), material, Quaternion.Euler(90f, 0f, 0f)); // 왼쪽 바퀴 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.26f, 0.16f), new Vector3(0.08f, 0.08f, 0.08f), material); // 포탄 장식 생성
        }

        private static void BuildCanvasserModel(Transform parent, Material material) // 룩형 구조에 깃발 장식을 더한 모델을 만드는 메서드
        {
            BuildRookModel(parent, material); // 룩 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.98f, 0f), new Vector3(0.04f, 0.24f, 0.04f), material); // 장대 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.13f, 1.02f, 0f), new Vector3(0.2f, 0.14f, 0.04f), material); // 깃발 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 1.18f, 0f), new Vector3(0.08f, 0.08f, 0.08f), material); // 꼭대기 구슬 생성
        }

        private static void BuildCaliphModel(Transform parent, Material material) // 돔과 초승달 장식을 더한 모델을 만드는 메서드
        {
            BuildBishopModel(parent, material); // 비숍 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 1.12f, 0f), new Vector3(0.16f, 0.04f, 0.04f), material); // 초승달 가로 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.06f, 1.08f, 0f), new Vector3(0.04f, 0.14f, 0.04f), material); // 초승달 세로 파츠 생성
        }

        private static void BuildSquirrelModel(Transform parent, Material material) // 꼬리가 말린 작은 짐승 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.28f, 0.04f, 0.28f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.24f, 0f), new Vector3(0.22f, 0.18f, 0.18f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.44f, 0.08f), new Vector3(0.12f, 0.12f, 0.12f), material); // 머리 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(-0.16f, 0.42f, -0.08f), new Vector3(0.08f, 0.22f, 0.08f), material, Quaternion.Euler(0f, 0f, -42f)); // 꼬리 바닥 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(-0.28f, 0.62f, -0.02f), new Vector3(0.16f, 0.16f, 0.16f), material); // 꼬리 끝 생성
        }

        private static void BuildChameleonModel(Transform parent, Material material) // 눈과 말린 꼬리를 가진 카멜레온 느낌의 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.28f, 0.04f, 0.28f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.24f, 0f), new Vector3(0.28f, 0.12f, 0.18f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.38f, 0.14f), new Vector3(0.16f, 0.1f, 0.16f), material); // 머리 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0.08f, 0.46f, 0.2f), new Vector3(0.07f, 0.07f, 0.07f), material); // 오른쪽 눈 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(-0.08f, 0.46f, 0.2f), new Vector3(0.07f, 0.07f, 0.07f), material); // 왼쪽 눈 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(-0.2f, 0.3f, -0.1f), new Vector3(0.08f, 0.16f, 0.08f), material, Quaternion.Euler(0f, 0f, 40f)); // 말린 꼬리 바닥 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(-0.28f, 0.46f, -0.04f), new Vector3(0.11f, 0.11f, 0.11f), material); // 꼬리 끝 생성
        }

        private void AttachSelectionCollider() // 모델 전체를 덮는 단일 BoxCollider를 부착하는 메서드
        {
            var renderers = GetComponentsInChildren<Renderer>(); // 자식 렌더러를 모두 수집
            if (renderers == null || renderers.Length == 0) return; // 렌더러가 없으면 종료

            var bounds = renderers[0].bounds; // 첫 렌더러의 월드 Bounds를 기준으로 시작
            for (int i = 1; i < renderers.Length; i++) // 나머지 렌더러까지 순회하며
            {
                bounds.Encapsulate(renderers[i].bounds); // 전체 모델을 덮는 Bounds로 확장
            }

            var existingCollider = GetComponent<BoxCollider>(); // 기존 BoxCollider가 있는지 확인
            if (existingCollider != null) Destroy(existingCollider); // 중복 콜라이더가 있으면 제거

            var collider = gameObject.AddComponent<BoxCollider>(); // 클릭 판정용 단일 BoxCollider 추가
            collider.center = transform.InverseTransformPoint(bounds.center); // 월드 Bounds 중심을 로컬 기준 중심으로 변환
            collider.size = bounds.size + new Vector3(0.02f, 0.02f, 0.02f); // 살짝 크게 잡아 클릭이 쉬워지게 조정
        }

        private static Material CreatePieceMaterial(Color color) // 기물에 적용할 단색 머티리얼을 만드는 메서드
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit"); // URP Lit 셰이더 우선 탐색
            if (shader == null) shader = Shader.Find("Standard"); // 실패 시 Standard 셰이더 사용
            var material = new Material(shader); // 머티리얼 생성
            material.color = color; // 팀 색상 적용
            return material; // 완성 머티리얼 반환
        }

        private static GameObject CreatePart(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material) // 기본 회전으로 파츠 하나를 생성하는 보조 메서드
        {
            return CreatePart(parent, type, localPosition, localScale, material, Quaternion.identity); // 회전 없는 오버로드로 위임
        }

        private static GameObject CreatePart(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation) // 프리미티브 파츠 하나를 생성하는 보조 메서드
        {
            var part = GameObject.CreatePrimitive(type); // 지정한 타입의 기본 도형 생성
            part.name = type.ToString(); // 디버그 구분용 이름 지정
            part.transform.SetParent(parent, false); // 부모에 연결
            part.transform.localPosition = localPosition; // 로컬 위치 적용
            part.transform.localRotation = localRotation; // 로컬 회전 적용
            part.transform.localScale = localScale; // 로컬 크기 적용

            var renderer = part.GetComponent<Renderer>(); // 렌더러 확보
            if (renderer != null) renderer.sharedMaterial = material; // 팀 색상 머티리얼 적용

            var collider = part.GetComponent<Collider>(); // 기본으로 붙는 콜라이더 확보
            if (collider != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(collider); // 플레이 중이면 Destroy 사용(30일차: System.Action 추가로 Object가 모호해져 명시적 한정)
                else UnityEngine.Object.DestroyImmediate(collider); // 에디터 상태면 DestroyImmediate 사용
            }

            return part; // 생성한 파츠 반환
        }
    }
}
