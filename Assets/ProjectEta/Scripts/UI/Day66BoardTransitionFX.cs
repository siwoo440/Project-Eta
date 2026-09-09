using System.Collections; // Coroutine 사용
using System.Collections.Generic; // 전환 대상·렌더러 목록 사용
using UnityEngine; // MonoBehaviour·Transform·Renderer 사용
using UnityEngine.EventSystems; // 전환 중 UI 입력 차단 사용
using UnityEngine.InputSystem.UI; // 새 Input System EventSystem 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 사용
using UnityEngine.UI; // 투명 입력 차단 Canvas 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Board; // BoardView 사용
using ProjectEta.Pieces; // PieceView 사용
using ProjectEta.Run; // RunState·BoardMode 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(900)]
    public sealed class Day66BoardTransitionFX : MonoBehaviour
    {
        private const float OutgoingHeightTiles = 3.4f; // 나가는 오브젝트 상승 거리
        private const float IncomingHeightTiles = 2.9f; // 들어오는 오브젝트 낙하 시작 거리
        private const float IncomingSearchTimeout = 1.4f; // 새 상태 시각 생성 최대 대기 시간
        private const string BuildingPrefix = "Day66Building_"; // 66일차 지도 건물 루트 접두사
        private const string MapKingName = "RouteMap_King"; // 지도 전용 King 루트 이름

        private sealed class RendererState
        {
            public Renderer Renderer; // 원본 렌더러 참조
            public bool WasEnabled; // 전환 전 활성 상태
        }

        private sealed class TransitionProxy
        {
            public GameObject ProxyRoot; // 시각 전환용 복제 루트
            public Vector3 BaseWorldPosition; // 실제 배치 위치
            public Quaternion BaseWorldRotation; // 실제 배치 회전
            public Vector3 BaseWorldScale; // 실제 배치 스케일
            public List<RendererState> SourceRendererStates; // 숨긴 실제 렌더러 상태
        }

        private readonly List<GameObject> _activeProxyRoots = new List<GameObject>(); // 현재 살아 있는 전환 프록시
        private readonly List<RendererState> _hiddenRendererStates = new List<RendererState>(); // 복원해야 할 실제 렌더러 목록
        private readonly List<TransitionProxy> _pendingIncomingProxies = new List<TransitionProxy>(); // 낙하 대기 중 새 상태 프록시

        private BattleController _battleController; // 현재 RunState 제공 전투 컨트롤러
        private BoardView _boardView; // 지도·전투 시각 탐색 기준 보드
        private RunState _runState; // 현재 런 상태
        private BoardMode _lastBoardMode; // 직전 프레임 보드 모드
        private BoardMode _pendingIncomingMode; // 현재 낙하 대기 대상 모드
        private bool _hasBoardMode; // 최초 모드 기준점 생성 여부
        private bool _pendingIncoming; // 새 상태 오브젝트 대기 여부
        private bool _incomingCaptured; // 새 상태 시각 프록시 생성 여부
        private float _incomingNotBefore; // 낙하 시작 가능 시간
        private float _incomingDeadline; // 새 상태 탐색 종료 시간
        private int _transitionGeneration; // 이전 전환 Coroutine 무효화 번호
        private Canvas _blockerCanvas; // 전환 중 입력 차단 Canvas
        private GameObject _blockerRoot; // 투명 전체 화면 차단 루트
        private EventSystem _createdEventSystem; // 직접 생성 EventSystem

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<Day66BoardTransitionFX>() != null) return; // 중복 전환 관리자 차단

            GameObject host = new GameObject("Day66BoardTransitionFX"); // 66일차 보드 전환 연출 호스트 생성
            host.AddComponent<Day66BoardTransitionFX>(); // 자동 전환 연출 컴포넌트 추가
        }

        private void Awake()
        {
            EnsureInputBlocker(); // 전환 중 클릭 차단 UI 준비
        }

        private void Update()
        {
            ResolveBindings(); // 현재 런·보드 연결 보장
            if (_runState == null || _boardView == null) return; // 필수 상태 준비 전 처리 차단

            BoardMode currentMode = _runState.CurrentBoardMode; // 현재 보드 모드 조회

            if (!_hasBoardMode)
            {
                _lastBoardMode = currentMode; // 최초 보드 모드 기준 저장
                _hasBoardMode = true; // 최초 기준점 생성 완료
                return; // 씬 시작은 전환 연출 없이 유지
            }

            if (currentMode == _lastBoardMode) return; // 보드 모드 변화 없음 처리 생략

            BoardMode previousMode = _lastBoardMode; // 이전 모드 저장
            _lastBoardMode = currentMode; // 새 모드를 다음 비교 기준으로 저장
            BeginModeTransition(previousMode, currentMode); // Battle↔Map 시각 전환 시작
        }

        private void LateUpdate()
        {
            if (!_pendingIncoming || _runState == null || _boardView == null) return; // 새 상태 대기 없음 처리 생략
            if (_runState.CurrentBoardMode != _pendingIncomingMode) return; // 연출 도중 다시 상태 변경된 경우 현재 대기 유지 차단

            if (!_incomingCaptured)
            {
                List<Transform> incomingRoots = CollectTransitionRoots(_pendingIncomingMode); // 새 상태 실제 시각 루트 탐색

                if (incomingRoots.Count > 0)
                {
                    CaptureIncomingProxies(incomingRoots); // 새 상태를 숨기고 낙하 프록시 생성
                    _incomingCaptured = _pendingIncomingProxies.Count > 0; // 실제 프록시 생성 여부 기록
                }
            }

            if (_incomingCaptured && Time.unscaledTime >= _incomingNotBefore)
            {
                int generation = _transitionGeneration; // 현재 전환 번호 고정
                _pendingIncoming = false; // 중복 낙하 시작 차단
                StartCoroutine(AnimateIncomingSequence(generation)); // 새 오브젝트 순차 낙하 시작
                return;
            }

            if (Time.unscaledTime >= _incomingDeadline)
            {
                _pendingIncoming = false; // 새 상태 탐색 종료
                EndTransition(_transitionGeneration); // 대상 생성 실패 시 입력 차단 안전 해제
            }
        }

        private void ResolveBindings()
        {
            if (_battleController == null) _battleController = Object.FindFirstObjectByType<BattleController>(); // BattleController 지연 탐색
            if (_boardView == null) _boardView = Object.FindFirstObjectByType<BoardView>(); // BoardView 지연 탐색
            if (_battleController == null || _battleController.RunState == null) return; // RunState 준비 전 종료

            if (_runState != _battleController.RunState)
            {
                ResetTransitionState(); // 이전 런 전환 시각 안전 정리
                _runState = _battleController.RunState; // 새 RunState 연결
                _hasBoardMode = false; // 새 런 첫 모드 재기준 처리
            }
        }

        private void BeginModeTransition(BoardMode fromMode, BoardMode toMode)
        {
            if (!IsSupportedMode(fromMode) || !IsSupportedMode(toMode) || fromMode == toMode) return; // Battle↔Map 외 전환 제외

            ResetTransitionState(); // 이전 미완료 연출 정리
            _transitionGeneration++; // 새 전환 번호 발급
            int generation = _transitionGeneration; // Coroutine용 전환 번호 고정
            ShowInputBlocker(); // 전환 완료 전 클릭 입력 차단

            List<Transform> outgoingRoots = CollectTransitionRoots(fromMode); // 현재 상태 시각 대상 수집
            List<TransitionProxy> outgoingProxies = CreateProxies(outgoingRoots, true); // 실제 시각을 숨기고 나감 프록시 생성
            float tileSize = Mathf.Max(0.1f, _boardView.TileSize); // 보드 타일 크기 보정
            float outgoingHeight = tileSize * OutgoingHeightTiles; // 실제 상승 거리 계산
            StartCoroutine(RestoreOutgoingSourcesNextFrame(outgoingProxies, generation)); // 상태 변경 뒤 원본 렌더러 속성 안전 복원
            StartCoroutine(AnimateOutgoingSequence(outgoingProxies, outgoingHeight, generation)); // 하나씩 위로 쓩 연출 시작

            float outgoingDuration = Day66TransitionMotion.GetSequenceDuration(outgoingProxies.Count); // 전체 나감 연출 시간 계산
            _pendingIncomingMode = toMode; // 다음 낙하 대상 모드 저장
            _pendingIncoming = true; // 새 상태 시각 탐색 활성화
            _incomingCaptured = false; // 새 상태 프록시 미생성 상태
            _incomingNotBefore = Time.unscaledTime + outgoingDuration + Day66TransitionMotion.TransitionGap; // 나감 종료 뒤 낙하 시작 시간 설정
            _incomingDeadline = _incomingNotBefore + IncomingSearchTimeout; // 새 상태 생성 최대 대기 시간 설정
        }

        private List<Transform> CollectTransitionRoots(BoardMode mode)
        {
            var result = new List<Transform>(); // 전환 대상 루트 목록 생성

            if (mode == BoardMode.Battle)
            {
                PieceView[] pieceViews = Object.FindObjectsByType<PieceView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None); // 현재 활성 전투 기물 조회

                for (int i = 0; i < pieceViews.Length; i++)
                {
                    PieceView pieceView = pieceViews[i]; // 현재 기물 View 조회
                    if (pieceView == null || !pieceView.gameObject.activeInHierarchy) continue; // 비활성·제거된 기물 제외
                    result.Add(pieceView.transform); // 기물 전체 모델을 전환 대상으로 등록
                }
            }
            else if (mode == BoardMode.Map && _boardView != null)
            {
                Transform[] transforms = _boardView.GetComponentsInChildren<Transform>(false); // 현재 활성 지도 시각 전체 탐색

                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform target = transforms[i]; // 현재 지도 Transform 조회
                    if (target == null || target == _boardView.transform) continue; // 보드 자체 제외
                    bool isBuilding = target.name.StartsWith(BuildingPrefix, System.StringComparison.Ordinal); // 66일차 상세 건물 여부 확인
                    bool isKing = string.Equals(target.name, MapKingName, System.StringComparison.Ordinal); // 지도 King 여부 확인
                    if (!isBuilding && !isKing) continue; // 건물·King 외 경로선/기단 제외
                    result.Add(target); // 지도 전환 대상 등록
                }
            }

            SortTransitionRoots(result); // 화면 기준 자연스러운 순서로 정렬
            return result; // 최종 전환 대상 반환
        }

        private static void SortTransitionRoots(List<Transform> roots)
        {
            roots.Sort((left, right) =>
            {
                if (left == null && right == null) return 0; // 둘 다 빈 대상 동일 처리
                if (left == null) return 1; // 빈 대상 뒤로 정렬
                if (right == null) return -1; // 유효 대상 앞으로 정렬

                int zCompare = right.position.z.CompareTo(left.position.z); // 카메라 먼쪽부터 순차 처리
                if (zCompare != 0) return zCompare; // 깊이 차이 우선 적용
                return left.position.x.CompareTo(right.position.x); // 같은 줄은 좌측부터 정렬
            });
        }

        private List<TransitionProxy> CreateProxies(List<Transform> roots, bool hideSources)
        {
            var result = new List<TransitionProxy>(); // 생성된 프록시 목록

            for (int i = 0; i < roots.Count; i++)
            {
                Transform root = roots[i]; // 현재 원본 루트 조회
                if (root == null || !root.gameObject.activeInHierarchy) continue; // 제거·비활성 대상 제외

                GameObject proxyRoot = CreateVisualProxy(root); // 렌더링 전용 복제 루트 생성
                if (proxyRoot == null) continue; // 표시 가능한 Mesh 없음 제외

                List<RendererState> rendererStates = hideSources ? HideSourceRenderers(root) : new List<RendererState>(); // 실제 원본 시각 숨김
                var proxy = new TransitionProxy
                {
                    ProxyRoot = proxyRoot, // 복제 시각 저장
                    BaseWorldPosition = root.position, // 실제 배치 위치 저장
                    BaseWorldRotation = root.rotation, // 실제 배치 회전 저장
                    BaseWorldScale = root.lossyScale, // 실제 배치 스케일 저장
                    SourceRendererStates = rendererStates // 실제 렌더러 복원 정보 저장
                };

                _activeProxyRoots.Add(proxyRoot); // 전체 정리 목록 등록
                result.Add(proxy); // 전환 대상 등록
            }

            return result; // 완성 프록시 목록 반환
        }

        private void CaptureIncomingProxies(List<Transform> incomingRoots)
        {
            _pendingIncomingProxies.Clear(); // 이전 낙하 대기 목록 초기화
            List<TransitionProxy> proxies = CreateProxies(incomingRoots, true); // 새 실제 오브젝트를 숨기고 프록시 생성

            for (int i = 0; i < proxies.Count; i++)
            {
                TransitionProxy proxy = proxies[i]; // 현재 새 상태 프록시 조회
                if (proxy?.ProxyRoot == null) continue; // 생성 실패 대상 제외
                proxy.ProxyRoot.SetActive(false); // 나감 연출 완료 전 새 오브젝트 숨김
                _pendingIncomingProxies.Add(proxy); // 순차 낙하 대기 목록 등록
            }
        }

        private IEnumerator RestoreOutgoingSourcesNextFrame(List<TransitionProxy> outgoingProxies, int generation)
        {
            yield return null; // 기존 RouteMap/Battle 전환 로직이 원본을 숨기거나 제거할 시간 확보
            if (generation != _transitionGeneration) yield break; // 더 새로운 전환 발생 시 이전 복원 중단

            for (int i = 0; i < outgoingProxies.Count; i++)
            {
                RestoreRendererStates(outgoingProxies[i]?.SourceRendererStates); // 향후 재사용될 원본 렌더러 속성 복원
            }
        }

        private IEnumerator AnimateOutgoingSequence(List<TransitionProxy> proxies, float height, int generation)
        {
            for (int i = 0; i < proxies.Count; i++)
            {
                if (generation != _transitionGeneration) yield break; // 새 전환 발생 시 이전 연출 중단
                TransitionProxy proxy = proxies[i]; // 현재 나감 프록시 조회
                StartCoroutine(AnimateWhoosh(proxy, height, Day66TransitionMotion.GetStaggerDelay(i), generation)); // 개별 순차 상승 연출 시작
            }

            float duration = Day66TransitionMotion.GetSequenceDuration(proxies.Count); // 전체 나감 연출 길이 계산
            if (duration > 0f) yield return new WaitForSecondsRealtime(duration); // 마지막 오브젝트 종료까지 대기
        }

        private IEnumerator AnimateWhoosh(TransitionProxy proxy, float height, float delay, int generation)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay); // 앞 오브젝트와 순차 간격 적용
            if (generation != _transitionGeneration || proxy?.ProxyRoot == null) yield break; // 무효 전환·제거 프록시 차단

            GameObject root = proxy.ProxyRoot; // 현재 복제 루트 조회
            root.SetActive(true); // 위로 날아갈 시각 표시
            float elapsed = 0f; // 개별 연출 경과 시간 초기화
            Vector3 startPosition = proxy.BaseWorldPosition; // 실제 시작 위치 저장
            Quaternion startRotation = proxy.BaseWorldRotation; // 실제 시작 회전 저장
            Vector3 startScale = root.transform.localScale; // 복제 시작 스케일 저장

            while (elapsed < Day66TransitionMotion.WhooshDuration)
            {
                if (generation != _transitionGeneration || root == null) yield break; // 연출 취소·제거 대응
                elapsed += Time.unscaledDeltaTime; // 게임 배속과 무관한 시간 누적
                float t = Mathf.Clamp01(elapsed / Day66TransitionMotion.WhooshDuration); // 0~1 진행률 계산
                float y = Day66TransitionMotion.EvaluateWhooshY(startPosition.y, height, t); // 가속 상승 높이 계산
                root.transform.position = new Vector3(startPosition.x, y, startPosition.z); // 위쪽으로 쓩 이동 적용
                root.transform.rotation = startRotation * Quaternion.Euler(0f, t * 24f, t * -7f); // 이동 중 약한 회전감 적용
                root.transform.localScale = Vector3.Lerp(startScale, startScale * 0.88f, t); // 멀어지는 느낌의 축소 적용
                yield return null; // 다음 프레임 진행
            }

            DestroyProxyRoot(root); // 화면 밖으로 나간 프록시 제거
        }

        private IEnumerator AnimateIncomingSequence(int generation)
        {
            float tileSize = _boardView != null ? Mathf.Max(0.1f, _boardView.TileSize) : 1f; // 현재 타일 크기 보정
            float height = tileSize * IncomingHeightTiles; // 실제 낙하 시작 높이 계산

            for (int i = 0; i < _pendingIncomingProxies.Count; i++)
            {
                if (generation != _transitionGeneration) yield break; // 새 전환 발생 시 현재 낙하 중단
                TransitionProxy proxy = _pendingIncomingProxies[i]; // 현재 낙하 프록시 조회
                StartCoroutine(AnimateDrop(proxy, height, Day66TransitionMotion.GetStaggerDelay(i), generation)); // 하나씩 탁 낙하 연출 시작
            }

            float duration = _pendingIncomingProxies.Count > 0
                ? Day66TransitionMotion.DropDuration + Day66TransitionMotion.GetStaggerDelay(_pendingIncomingProxies.Count - 1)
                : 0f; // 마지막 낙하 완료 시간 계산

            if (duration > 0f) yield return new WaitForSecondsRealtime(duration); // 마지막 오브젝트 착지까지 대기
            if (generation != _transitionGeneration) yield break; // 새 전환 발생 시 이전 종료 처리 차단
            _pendingIncomingProxies.Clear(); // 낙하 대기 목록 비우기
            EndTransition(generation); // 모든 착지 뒤 입력 차단 해제
        }

        private IEnumerator AnimateDrop(TransitionProxy proxy, float height, float delay, int generation)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay); // 앞 오브젝트와 순차 간격 적용
            if (generation != _transitionGeneration || proxy?.ProxyRoot == null) yield break; // 취소·제거 프록시 차단

            GameObject root = proxy.ProxyRoot; // 현재 낙하 프록시 조회
            root.SetActive(true); // 실제 낙하 시작 시 시각 표시
            float elapsed = 0f; // 개별 낙하 경과 시간 초기화
            Vector3 landingPosition = proxy.BaseWorldPosition; // 최종 배치 위치 저장
            Quaternion landingRotation = proxy.BaseWorldRotation; // 최종 회전 저장
            Vector3 normalScale = root.transform.localScale; // 최종 시각 스케일 저장
            root.transform.position = landingPosition + Vector3.up * height; // 화면 위 낙하 시작 위치 적용
            root.transform.localScale = normalScale * 0.93f; // 공중에서 약간 작은 시작 크기 적용

            while (elapsed < Day66TransitionMotion.DropDuration)
            {
                if (generation != _transitionGeneration || root == null) yield break; // 연출 취소·제거 대응
                elapsed += Time.unscaledDeltaTime; // 배속과 무관한 시간 누적
                float t = Mathf.Clamp01(elapsed / Day66TransitionMotion.DropDuration); // 낙하 진행률 계산
                float y = Day66TransitionMotion.EvaluateDropY(landingPosition.y, height, t); // 충돌·복원이 포함된 Y 계산
                root.transform.position = new Vector3(landingPosition.x, y, landingPosition.z); // 위에서 아래로 탁 이동 적용
                root.transform.rotation = Quaternion.Slerp(landingRotation * Quaternion.Euler(-5f, 0f, 4f), landingRotation, t); // 착지하며 정렬되는 회전 적용
                float scaleT = 1f - Mathf.Pow(1f - t, 3f); // 착지 크기 복원 곡선 계산
                root.transform.localScale = Vector3.Lerp(normalScale * 0.93f, normalScale, scaleT); // 원래 크기로 복원
                yield return null; // 다음 프레임 진행
            }

            RestoreRendererStates(proxy.SourceRendererStates); // 실제 새 상태 오브젝트 렌더러 표시
            DestroyProxyRoot(root); // 착지 프록시 제거
        }

        private GameObject CreateVisualProxy(Transform sourceRoot)
        {
            if (sourceRoot == null) return null; // 원본 루트 누락 방어
            if (!ContainsVisibleMesh(sourceRoot)) return null; // 복제할 표시 Mesh 없음 제외

            GameObject proxyRoot = new GameObject($"{sourceRoot.name}_TransitionProxy"); // 전환 전용 렌더링 루트 생성
            proxyRoot.transform.position = sourceRoot.position; // 원본 월드 위치 복제
            proxyRoot.transform.rotation = sourceRoot.rotation; // 원본 월드 회전 복제
            proxyRoot.transform.localScale = sourceRoot.lossyScale; // 원본 월드 크기 복제
            CopyVisualComponents(sourceRoot, proxyRoot); // 루트 자체 Mesh 복제

            for (int i = 0; i < sourceRoot.childCount; i++)
            {
                CopyVisualHierarchy(sourceRoot.GetChild(i), proxyRoot.transform); // 자식 렌더링 계층 재귀 복제
            }

            return proxyRoot; // 완성 프록시 반환
        }

        private static void CopyVisualHierarchy(Transform source, Transform parent)
        {
            if (source == null || !source.gameObject.activeSelf) return; // 비활성 자식 제외

            GameObject clone = new GameObject(source.name); // 렌더링 전용 자식 생성
            clone.transform.SetParent(parent, false); // 프록시 계층 연결
            clone.transform.localPosition = source.localPosition; // 원본 로컬 위치 복제
            clone.transform.localRotation = source.localRotation; // 원본 로컬 회전 복제
            clone.transform.localScale = source.localScale; // 원본 로컬 크기 복제
            CopyVisualComponents(source, clone); // 현재 Transform의 Mesh·Material 복제

            for (int i = 0; i < source.childCount; i++)
            {
                CopyVisualHierarchy(source.GetChild(i), clone.transform); // 하위 렌더링 계층 재귀 복제
            }
        }

        private static void CopyVisualComponents(Transform source, GameObject target)
        {
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>(); // 원본 MeshFilter 조회
            MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>(); // 원본 MeshRenderer 조회
            if (sourceFilter == null || sourceRenderer == null || !sourceRenderer.enabled) return; // 표시 가능한 정적 Mesh 없음 제외

            MeshFilter targetFilter = target.AddComponent<MeshFilter>(); // 프록시 MeshFilter 생성
            targetFilter.sharedMesh = sourceFilter.sharedMesh; // 원본 Mesh 재사용
            MeshRenderer targetRenderer = target.AddComponent<MeshRenderer>(); // 프록시 MeshRenderer 생성
            targetRenderer.sharedMaterials = sourceRenderer.sharedMaterials; // 원본 머티리얼 배열 재사용
            targetRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode; // 원본 그림자 설정 복제
            targetRenderer.receiveShadows = sourceRenderer.receiveShadows; // 원본 그림자 수신 설정 복제
        }

        private static bool ContainsVisibleMesh(Transform root)
        {
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(false); // 활성 정적 MeshRenderer 조회

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i]; // 현재 렌더러 조회
                if (renderer == null || !renderer.enabled) continue; // 숨김 렌더러 제외
                if (renderer.GetComponent<MeshFilter>() != null) return true; // 복제 가능한 Mesh 존재 확인
            }

            return false; // 표시 Mesh 없음 반환
        }

        private List<RendererState> HideSourceRenderers(Transform root)
        {
            var states = new List<RendererState>(); // 현재 원본 렌더러 복원 목록
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true); // 원본 전체 렌더러 조회

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i]; // 현재 원본 렌더러 조회
                if (renderer == null) continue; // 제거 렌더러 제외

                var state = new RendererState
                {
                    Renderer = renderer, // 원본 렌더러 저장
                    WasEnabled = renderer.enabled // 기존 표시 상태 저장
                };

                states.Add(state); // 개별 복원 목록 등록
                _hiddenRendererStates.Add(state); // 전체 안전 복원 목록 등록
                renderer.enabled = false; // 전환 프록시만 보이도록 원본 숨김
            }

            return states; // 현재 루트 복원 상태 반환
        }

        private void RestoreRendererStates(List<RendererState> states)
        {
            if (states == null) return; // 복원 정보 없음 처리 생략

            for (int i = 0; i < states.Count; i++)
            {
                RendererState state = states[i]; // 현재 렌더러 복원 정보 조회
                if (state?.Renderer != null) state.Renderer.enabled = state.WasEnabled; // 전환 전 표시 상태 복원
                _hiddenRendererStates.Remove(state); // 전체 안전 복원 목록에서 제거
            }

            states.Clear(); // 중복 복원 방지
        }

        private void EnsureInputBlocker()
        {
            if (_blockerCanvas != null) return; // 이미 입력 차단 UI 생성됨

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 새 Input System EventSystem 생성
                _createdEventSystem = eventSystemObject.GetComponent<EventSystem>(); // 직접 생성 EventSystem 저장
            }

            GameObject canvasObject = new GameObject("Day66TransitionBlockerCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 전환 입력 차단 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 전환 호스트 자식 연결
            _blockerCanvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 저장
            _blockerCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 전체 오버레이 적용
            _blockerCanvas.sortingOrder = 1000; // 모든 게임 UI보다 위에서 입력 차단

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 대응 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 적용
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 프로젝트 기준 해상도 적용
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 균형 적용

            _blockerRoot = new GameObject("TransitionInputBlocker", typeof(RectTransform), typeof(Image)); // 투명 전체 화면 입력 차단 루트 생성
            _blockerRoot.transform.SetParent(canvasObject.transform, false); // Canvas 자식 연결
            RectTransform rect = _blockerRoot.GetComponent<RectTransform>(); // 차단 영역 RectTransform 조회
            rect.anchorMin = Vector2.zero; // 화면 좌하단 Stretch 시작
            rect.anchorMax = Vector2.one; // 화면 우상단 Stretch 끝
            rect.offsetMin = Vector2.zero; // 좌하단 여백 제거
            rect.offsetMax = Vector2.zero; // 우상단 여백 제거
            Image image = _blockerRoot.GetComponent<Image>(); // 투명 차단 Image 조회
            image.color = new Color(0f, 0f, 0f, 0.001f); // 보이지 않는 최소 알파 적용
            image.raycastTarget = true; // 전환 중 포인터 입력 차단
            _blockerRoot.SetActive(false); // 기본 입력 허용 상태
        }

        private void ShowInputBlocker()
        {
            EnsureInputBlocker(); // 입력 차단 UI 생성 보장
            if (_blockerRoot != null) _blockerRoot.SetActive(true); // 전환 중 모든 클릭 차단
        }

        private void HideInputBlocker()
        {
            if (_blockerRoot != null) _blockerRoot.SetActive(false); // 전환 종료 뒤 입력 허용
        }

        private void EndTransition(int generation)
        {
            if (generation != _transitionGeneration) return; // 이전 전환 종료 요청 무시
            RestoreAllHiddenRenderers(); // 남아 있는 실제 시각 안전 복원
            DestroyAllProxies(); // 남은 프록시 안전 제거
            _pendingIncomingProxies.Clear(); // 낙하 목록 초기화
            _incomingCaptured = false; // 낙하 캡처 상태 초기화
            HideInputBlocker(); // 게임 입력 다시 허용
        }

        private void ResetTransitionState()
        {
            StopAllCoroutines(); // 이전 순차 연출 즉시 종료
            RestoreAllHiddenRenderers(); // 이전 전환에서 숨긴 실제 시각 복원
            DestroyAllProxies(); // 이전 프록시 전체 제거
            _pendingIncomingProxies.Clear(); // 낙하 대기 목록 제거
            _pendingIncoming = false; // 새 상태 대기 해제
            _incomingCaptured = false; // 새 상태 캡처 해제
            HideInputBlocker(); // 입력 차단 안전 해제
        }

        private void RestoreAllHiddenRenderers()
        {
            for (int i = _hiddenRendererStates.Count - 1; i >= 0; i--)
            {
                RendererState state = _hiddenRendererStates[i]; // 남은 숨김 렌더러 조회
                if (state?.Renderer != null) state.Renderer.enabled = state.WasEnabled; // 기존 표시 상태 복원
            }

            _hiddenRendererStates.Clear(); // 전체 복원 목록 초기화
        }

        private void DestroyAllProxies()
        {
            for (int i = _activeProxyRoots.Count - 1; i >= 0; i--)
            {
                GameObject proxy = _activeProxyRoots[i]; // 현재 전환 프록시 조회
                if (proxy != null) Destroy(proxy); // 남은 프록시 제거
            }

            _activeProxyRoots.Clear(); // 프록시 목록 초기화
        }

        private void DestroyProxyRoot(GameObject proxy)
        {
            if (proxy == null) return; // 이미 제거된 프록시 제외
            _activeProxyRoots.Remove(proxy); // 전체 정리 목록에서 제거
            Destroy(proxy); // 현재 프록시 제거
        }

        private static bool IsSupportedMode(BoardMode mode)
        {
            return mode == BoardMode.Battle || mode == BoardMode.Map; // 전투·지도 보드 전환만 연출 대상
        }

        private void OnDestroy()
        {
            ResetTransitionState(); // 씬 종료 시 숨김·프록시·입력 차단 정리
            if (_createdEventSystem != null) Destroy(_createdEventSystem.gameObject); // 직접 만든 EventSystem 제거
        }
    }
}
