using System.Collections.Generic; // 표시 마스크 소유 목록
using UnityEngine; // Canvas·씬 객체
using UnityEngine.UI; // 독립 표시 마스크 대상

namespace ProjectEta.Environment // 걷기 화면 표시 영역
{ // 영역 시작
    [DisallowMultipleComponent] // 표시 소유자 중복 방지
    [DefaultExecutionOrder(20000)] // 기존 HUD 갱신 이후 표시 억제
    public sealed class RoomWalkingHudVisibility : MonoBehaviour // 게임 진행과 분리된 걷기 표시 소유자
    { // 타입 시작
        private static readonly HashSet<string> GameplayCanvasNames = new HashSet<string> // 걷기 중 숨길 실제 게임 화면
        { // 이름 목록 시작
            "BattleHUDCanvas_Day62", // 공통 전투 상태
            "BattleInteractionStatusCanvas_Day63", // 전투 조작 안내
            "HandCanvas", // 손패 카드
            "Day66RouteMapCanvas", // 경로 지도
            "Day72RouteMapSeedCanvas", // 지도 시드 정보
            "PieceInfoPanelCanvas", // 기물 상세 정보
            "PieceAbilityOverlayCanvas", // 기물 능력 설명
            "DeckPanelCanvas", // 덱 상세 정보
            "FusionPanelCanvas", // 기존 합성 패널
            "FusionPanelCanvas_Day64", // 현재 합성 패널
            "BossHealthCanvas", // 보스 체력
            "BossPhaseStatusCanvas", // 보스 단계 상태
            "KingCombatHUDCanvas_Day61", // 킹 전투 상태
            "CombatLogCanvas", // 전투 로그
            "CombatFloatingTextCanvas_Day63", // 전투 피해 글자
            "RoundSummaryCanvas", // 기존 라운드 정보
            "TurnStatusCanvas", // 기존 턴 상태
            "DeploymentTurnBannerCanvas", // 기존 배치 배너
            "Day65StageActivityCanvas", // 스테이지 공통 상태
            "Day67BattleAnnouncementCanvas", // 전투 순간 알림
            "SystemToastCanvas_Day68" // 일반 조작 토스트
        }; // 이름 목록 종료
        private readonly Dictionary<Graphic, RoomWalkingGraphicMask> _masks = new Dictionary<Graphic, RoomWalkingGraphicMask>(); // 직접 추가한 표시 마스크
        private bool _walking; // 현재 표시 억제 상태
        public void SetWalking(bool walking) // 걷기와 좌석 표시 전환
        { // 메서드 시작
            _walking = walking; // 새 표시 상태 기록
            Canvas.preWillRenderCanvases -= SuppressGameplayHud; // 이전 표시 콜백 해제
            if (!walking) // 좌석 복귀 확인
            { // 조건 시작
                Restore(); // 전용 표시 마스크만 해제
                return; // 좌석 전환 종료
            } // 조건 종료
            Canvas.preWillRenderCanvases += SuppressGameplayHud; // 렌더 직전 동적 화면 포착
            SuppressGameplayHud(); // 일어나기 프레임 표시 억제
        } // 메서드 종료
        private void LateUpdate() // 화면 갱신 이후 표시 확인
        { // 메서드 시작
            if (_walking) SuppressGameplayHud(); // 걷기 중 신규 화면 숨김
        } // 메서드 종료
        private void SuppressGameplayHud() // 게임 Canvas의 실제 표시 요소 억제
        { // 메서드 시작
            if (!_walking) return; // 좌석 상태 콜백 제외
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None); // 비활성·동적 Canvas 포함 조회
            foreach (Canvas canvas in canvases) // 현재 씬 화면 순회
            { // 반복 시작
                if (!GameplayCanvasNames.Contains(canvas.name)) continue; // 시스템·필수 진행 화면 제외
                Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true); // 비활성·중첩·동적 표시 요소 조회
                foreach (Graphic graphic in graphics) // 실제 표시 요소 순회
                { // 반복 시작
                    AddMask(graphic); // 기존 그룹의 부모 무시 정책과 독립된 마스크
                } // 반복 종료
            } // 반복 종료
        } // 메서드 종료
        private void AddMask(Graphic target) // 기존 표시 소유자와 독립된 마스크 추가
        { // 메서드 시작
            if (!_masks.TryGetValue(target, out RoomWalkingGraphicMask mask) || mask == null) // 신규 또는 제거된 마스크 확인
            { // 조건 시작
                mask = target.gameObject.AddComponent<RoomWalkingGraphicMask>(); // 원본 색상·그룹·Canvas와 별도 표시 소유
                _masks[target] = mask; // 생성 컴포넌트 정리 책임 기록
            } // 조건 종료
            mask.enabled = true; // 생성된 정점의 투명도와 클릭 판정 억제
        } // 메서드 종료
        private void Restore() // 직접 소유한 표시 상태만 해제
        { // 메서드 시작
            foreach (RoomWalkingGraphicMask mask in _masks.Values) // 남은 전용 마스크 순회
            { // 반복 시작
                if (mask == null) continue; // 먼저 파괴된 화면 제외
                mask.enabled = false; // 원본 표시 데이터로 정점 재생성 예약
            } // 반복 종료
        } // 메서드 종료
        private void OnDisable() // 호스트 비활성화 표시 정리
        { // 메서드 시작
            SetWalking(false); // 표시 소유와 렌더 콜백 해제
        } // 메서드 종료
        private void OnDestroy() // 씬 종료 표시 정리
        { // 메서드 시작
            SetWalking(false); // 남은 표시 소유와 콜백 해제
            foreach (RoomWalkingGraphicMask mask in _masks.Values) // 직접 소유한 마스크 최종 정리
            { // 반복 시작
                if (mask != null) Destroy(mask); // 소유자 종료 시에만 컴포넌트 제거
            } // 반복 종료
            _masks.Clear(); // 이전 씬 화면 참조 해제
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
