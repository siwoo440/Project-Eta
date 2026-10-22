using System.Collections.Generic; // 대상과 재질 소유
using UnityEngine.InputSystem; // 실제 F·닫기 키
using UnityEngine.EventSystems; // 조사 전 UI 포커스
using ProjectEta.UI; // 기존 모달 입력 규칙
using ProjectEta.Debugging; // 상위 디버그 화면
using UnityEngine; // 실제 방 생성과 수명

namespace ProjectEta.Environment // 방 조사 연결
{ // 영역 시작
    [DefaultExecutionOrder(-400)] // Pause 다음·걷기 이전 조사 입력
    [DisallowMultipleComponent] // 중복 담당자 방지
    public sealed class RoomInvestigationController : MonoBehaviour // 방 조사 자동 연결
    { // 타입 시작
        private static RoomInvestigationController _openOwner; // 현재 조사 모달 소유
        private RoomInvestigationDefinition _current; // 열린 조사 내용
        private RoomInvestigationUI _ui; // 생성한 조사 화면
        private readonly HashSet<string> _viewed = new HashSet<string>(System.StringComparer.Ordinal); // 현재 방의 고유 확인 이력
        private CursorLockMode _previousCursorLock; // 조사 전 포인터 잠금
        private bool _previousCursorVisible; // 조사 전 포인터 표시
        private GameObject _previousSelection; // 조사 전 선택 참조
        private RoomWalkingController _walker; // 기존 걷기 상태
        private GameObject _worldRoot; // 카메라와 독립된 방 모델
        private readonly List<Material> _materials = new List<Material>(); // 전용 재질 수명
        public RoomInvestigationTarget[] Targets { get; private set; } // 실제 자동 설치 대상
        public RoomInvestigationTarget FocusTarget { get; private set; } // 현재 시선 대상
        public static bool IsAnyOpen => _openOwner != null && _openOwner.IsOpen; // 공통 입력 차단용 모달 상태
        public bool IsOpen => _current != null; // 현재 조사 열림
        public int ViewedCount => _viewed.Count; // 이 방에서 확인한 고유 대상 수
        public RoomInvestigationDefinition CurrentDefinition => _current; // 실제 표시 내용 조회
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 재실행 초기화
        private static void ResetRuntimeState() // 이전 방의 정적 참조 정리
        { // 메서드 시작
            _openOwner = null; // 남은 모달 소유 해제
        } // 메서드 종료
        public void Configure(RoomWalkingController walker) // 기존 리그에서 자동 설치
        { // 메서드 시작
            _walker = walker; // 실제 이동기 연결
            if (_worldRoot != null) return; // 재연결 중복 생성 차단
            _worldRoot = new GameObject("RoomInvestigationWorld_Day104"); // 시점과 독립된 월드 좌표
            Targets = RoomInvestigationLayout.Build(_worldRoot.transform, _materials); // 방의 기초 조사 네 곳
            _ui = gameObject.AddComponent<RoomInvestigationUI>(); // 자동 조사 UI 연결
            _ui.Configure(this); // 모달 콜백 소유 연결
        } // 메서드 종료
        private void Update() // Pause 다음·걷기 이전의 조사 입력
        { // 메서드 시작
            if (_walker == null || !_walker.isActiveAndEnabled || !_walker.IsWalking) // 좌석·이동기 비활성 확인
            { // 조건 시작
                ForceClose(); // 남은 조사 모달 해제
                SetFocus(null); // 남은 강조 제거
                return; // 이동 중에만 조사 허용
            } // 조건 종료
            Day69UiModalState modal = Day69UiPresentationRules.CaptureCurrent(); // 필수 진행 화면 상태
            if (modal.RewardOpen || modal.ActivityOpen || modal.RunResultOpen || modal.TutorialOpen) // 필수 선택 우선
            { // 조건 시작
                ForceClose(); // 조사보다 진행 화면 우선
                SetFocus(null); // 이전 대상 강조 정리
                return; // 좌석 복귀는 기존 이동기에서 처리
            } // 조건 종료
            Keyboard keyboard = Keyboard.current; // 현재 입력 장치
            if (IsOpen) // 열린 조사창의 닫기 입력
            { // 조건 시작
                if (keyboard != null && (keyboard.fKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)) TryClose(); // 같은 모달만 닫기
                return; // 조사 중 이동·새 대상 입력 제외
            } // 조건 종료
            if (!Day69UiPresentationRules.CanUseWorldInput()) // 다른 화면 입력 소유
            { // 조건 시작
                SetFocus(null); // 모달 뒤 조사 강조 해제
                return; // 상위 입력 차단
            } // 조건 종료
            SetFocus(_walker.CanSit ? null : FindFocus(_walker.EyePosition, _walker.LookRotation * Vector3.forward)); // 가까운 의자 우선
            if (keyboard == null || !keyboard.fKey.wasPressedThisFrame) return; // 조사 키 미입력 제외
            if (_walker.CanSit) // 동일 키의 좌석 복귀 우선
            { // 조건 시작
                if (_walker.TrySit()) Day69UiPresentationRules.ConsumeCurrentFrame(); // 앉기 후 같은 입력으로 재기립 방지
                return; // 물건 조사와 중복 처리 금지
            } // 조건 종료
            TryInspect(FocusTarget); // 실제 시선 대상만 조사
        } // 메서드 종료
        public bool TryInspect(RoomInvestigationTarget target) // 실제 조사 최종 입력
        { // 메서드 시작
            if (!isActiveAndEnabled || _walker == null || !_walker.IsWalking || _walker.CanSit || _ui == null || !_ui.isActiveAndEnabled || IsAnyOpen || target == null || !Day69UiPresentationRules.CanUseWorldInput()) return false; // 좌석·상위 모달·중복 입력 차단
            if (FindFocus(_walker.EyePosition, _walker.LookRotation * Vector3.forward) != target) return false; // 오래된 대상·외부 대상·바라보기 재검사
            _previousCursorLock = Cursor.lockState; // 원래 잠금 보관
            _previousCursorVisible = Cursor.visible; // 원래 표시 보관
            _previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null; // 원래 UI 선택 보관
            _current = target.Definition; // 현재 물건 정의 고정
            _openOwner = this; // 공통 모달 입력 소유
            _viewed.Add(target.Definition.Id); // 같은 방의 중복 없는 확인 이력
            SetFocus(null); // 모델 강조 해제
            _ui.Show(_current); // 실제 조사 화면
            Cursor.lockState = CursorLockMode.None; // 조사 클릭 즉시 허용
            Cursor.visible = true; // 조사 포인터 표시
            Day69UiPresentationRules.ConsumeCurrentFrame(); // 열기 입력의 닫기 재사용 방지
            return true; // 조사 진입 완료
        } // 메서드 종료
        public bool TryClose() // 키·버튼의 정상 닫기 입력
        { // 메서드 시작
            if (!isActiveAndEnabled || !IsOpen || !Day69UiPresentationRules.CanUseInvestigationInput()) return false; // 상위 모달·이전 입력 차단
            ForceClose(); // 열린 조사만 닫기
            return true; // 닫기 완료
        } // 메서드 종료
        public void ForceClose() // 좌석·필수 진행·생명주기의 정리
        { // 메서드 시작
            if (!IsOpen) return; // 중복 복원 차단
            _current = null; // 조사 입력 소유 해제
            if (_openOwner == this) _openOwner = null; // 정적 모달 소유 해제
            if (_ui != null) _ui.Hide(); // 남은 조사 화면 제거
            Day69UiModalState modal = Day69UiPresentationRules.CaptureCurrent(); // 상위 커서 소유 확인
            if (!modal.PauseOpen && !ProjectEtaDebugWindow.IsOpen) // Pause·F1의 포커스 유지
            { // 조건 시작
                Cursor.lockState = _previousCursorLock; // 이전 잠금 복원
                Cursor.visible = _previousCursorVisible; // 이전 표시 복원
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_walker != null && !_walker.IsWalking && _previousSelection != null && _previousSelection.activeInHierarchy ? _previousSelection : null); // 걷기 중 숨은 게임 UI Submit 방지
            } // 조건 종료
            _previousSelection = null; // 이전 화면 참조 해제
            Day69UiPresentationRules.ConsumeCurrentFrame(); // 닫기 입력의 뒤 화면 전달 차단
        } // 메서드 종료
        public bool HasViewed(RoomInvestigationTarget target) // 현재 방의 확인 안내
        { // 메서드 시작
            return target != null && target.Definition != null && _viewed.Contains(target.Definition.Id); // 재조사 보상과 분리한 표시
        } // 메서드 종료
        private void SetFocus(RoomInvestigationTarget target) // 같은 시각 대상의 단일 강조
        { // 메서드 시작
            if (FocusTarget == target) return; // 같은 대상 재설정 생략
            if (FocusTarget != null) FocusTarget.SetFocused(false); // 이전 강조 제거
            FocusTarget = target; // 새 시선 대상
            if (FocusTarget != null) FocusTarget.SetFocused(true); // 현재 금빛 테두리 표시
        } // 메서드 종료
        private void OnDisable() // 담당자 비활성화 정리
        { // 메서드 시작
            ForceClose(); // 조사·커서 소유 복원
            SetFocus(null); // 남은 강조 제거
        } // 메서드 종료
        public RoomInvestigationTarget FindFocus(Vector3 eye, Vector3 forward) // 실제 시선과 가림의 대상 조회
        { // 메서드 시작
            if (Targets == null) return null; // 자동 연결 전 입력 제외
            RoomInvestigationTarget best = null; // 현재 가장 가까운 유효 대상
            float nearest = float.PositiveInfinity; // 최소 거리
            foreach (RoomInvestigationTarget target in Targets) // 설치 대상 순회
            { // 반복 시작
                if (target == null || !target.CanInspectFrom(eye, forward)) continue; // 비활성·거리·시선·가림 제외
                float distance = (target.Anchor - eye).sqrMagnitude; // 실제 거리 비교
                if (distance >= nearest) continue; // 더 먼 후보 제외
                nearest = distance; // 새 최소 거리
                best = target; // 새 시선 대상
            } // 반복 종료
            return best; // 현재 조사 가능 대상
        } // 메서드 종료
        private void OnDestroy() // 생성 모델·재질 정리
        { // 메서드 시작
            ForceClose(); // 남은 모달 소유 정리
            if (_ui != null) Destroy(_ui); // 조사 UI 수명 정리
            if (_worldRoot != null) Destroy(_worldRoot); // 별도 좌표의 모델 수명 정리
            foreach (Material material in _materials) if (material != null) Destroy(material); // 소유 재질 해제
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
