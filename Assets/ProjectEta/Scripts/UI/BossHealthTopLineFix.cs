using UnityEngine; // MonoBehaviour·GameObject·RectTransform 사용
using UnityEngine.SceneManagement; // 현재 Battle Scene 확인 사용

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 타입 네임스페이스
{
    [DisallowMultipleComponent] // 보스바 보정 컴포넌트 중복 부착 방지
    [DefaultExecutionOrder(11000)] // 기존 Day63 레이아웃 보정 이후 최종 위치 적용
    public sealed class BossHealthTopLineFix : MonoBehaviour // 보스 체력바를 상단 UI 기준선에 고정하는 컴포넌트
    {
        private const string HostName = "BossHealthTopLineFix_Day63"; // 런타임 보정 호스트 이름
        private const string BossPanelName = "BossHealthPanel"; // BossHealthUI가 실제 생성하는 패널 이름
        private static readonly Vector2 TopCenterAnchor = new Vector2(0.5f, 1f); // 화면 상단 중앙 공통 앵커
        private static readonly Vector2 TopLinePosition = new Vector2(0f, -18f); // 좌측 HUD와 동일한 상단 기준 위치

        private RectTransform _bossPanel; // 현재 보스 체력 패널 RectTransform 캐시

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Scene 로드 직후 자동 설치
        private static void Install() // Battle Scene에서 보정 호스트를 자동 생성하는 메서드
        {
            if (SceneManager.GetActiveScene().name != "Battle") // 현재 Scene이 Battle이 아니면
            {
                return; // 다른 Scene에는 보정 호스트를 생성하지 않음
            }

            BossHealthTopLineFix existing = Object.FindFirstObjectByType<BossHealthTopLineFix>(); // 기존 보정 컴포넌트 조회

            if (existing != null) // 이미 보정 컴포넌트가 존재하면
            {
                return; // 중복 생성을 차단
            }

            GameObject host = new GameObject(HostName); // 보스바 상단 정렬 전용 호스트 생성
            host.AddComponent<BossHealthTopLineFix>(); // 최종 레이아웃 보정 컴포넌트 추가
        }

        private void LateUpdate() // 다른 UI 배치가 끝난 뒤 매 프레임 최종 위치를 보장하는 메서드
        {
            ResolveBossPanel(); // 늦게 생성되는 BossHealthPanel 참조 확보

            if (_bossPanel == null) // 아직 보스 체력 패널이 없으면
            {
                return; // 보스 등장 전에는 처리하지 않음
            }

            ApplyTopLineLayout(); // 보스 체력 패널을 상단 중앙 기준선에 고정
        }

        private void ResolveBossPanel() // 실제 BossHealthPanel을 이름으로 직접 찾는 메서드
        {
            if (_bossPanel != null) // 이미 유효한 패널을 찾았으면
            {
                return; // 반복 탐색을 생략
            }

            GameObject panelObject = GameObject.Find(BossPanelName); // BossHealthUI가 생성한 실제 패널 조회

            if (panelObject == null) // 아직 보스 패널이 생성되지 않았으면
            {
                return; // 다음 프레임에 다시 탐색
            }

            _bossPanel = panelObject.GetComponent<RectTransform>(); // 실제 패널 RectTransform 캐시
        }

        private void ApplyTopLineLayout() // 좌측 HUD와 같은 높이에 보스바를 배치하는 메서드
        {
            _bossPanel.anchorMin = TopCenterAnchor; // 상단 중앙 최소 앵커 적용
            _bossPanel.anchorMax = TopCenterAnchor; // 상단 중앙 최대 앵커 적용
            _bossPanel.pivot = TopCenterAnchor; // 상단 중앙 피벗 적용
            _bossPanel.anchoredPosition = TopLinePosition; // 좌측 HUD와 동일한 Y -18 위치 적용
            _bossPanel.sizeDelta = new Vector2(620f, 54f); // 기존 보스바 크기 유지
        }
    }
}
