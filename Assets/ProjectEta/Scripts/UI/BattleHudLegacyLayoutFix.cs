using UnityEngine; // MonoBehaviour·GameObject·Vector2 사용
using UnityEngine.UI; // Canvas·Text·RectTransform 사용

namespace ProjectEta.UI
{
    [DisallowMultipleComponent] // 중복 레이아웃 보정 방지
    [DefaultExecutionOrder(10000)] // 기존 HUD 배치 갱신 이후 최종 위치 적용
    public sealed class BattleHudLegacyLayoutFix : MonoBehaviour
    {
        private const string HostName = "BattleHudLegacyLayoutFix_Day63"; // 런타임 보정 호스트 이름
        private const string BattleHudRootName = "BattleHUDRoot_Day62"; // 62일차 전투 HUD 루트 이름
        private const string KingPlacementRootName = "KingPlacementRoot_Day61"; // 기존 King 배치 안내 루트 이름
        private const string InteractionPanelName = "BattleInteractionPanel"; // 63일차 행동 안내 패널 이름
        private const string RoundSummaryCanvasName = "RoundSummaryCanvas"; // 기존 중앙 중복 라운드 Canvas 이름
        private const string TurnStatusCanvasName = "TurnStatusCanvas"; // 기존 중앙 턴 상태 Canvas 이름
        private const string DeploymentBannerCanvasName = "DeploymentTurnBannerCanvas"; // 기존 중앙 배치 배너 Canvas 이름
        private const string BossLabel = "BOSS"; // 보스 체력바 식별용 고정 라벨
        private static readonly Vector2 TopLeftAnchor = new Vector2(0f, 1f); // 좌측 상단 공통 앵커 값

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            BattleHudLegacyLayoutFix existing = Object.FindFirstObjectByType<BattleHudLegacyLayoutFix>(); // 기존 보정 컴포넌트 조회

            if (existing != null)
            {
                return; // 중복 호스트 생성 차단
            }

            GameObject host = new GameObject(HostName); // 전역 레이아웃 보정 호스트 생성
            Object.DontDestroyOnLoad(host); // 씬 전환 뒤에도 보정 호스트 유지
            host.AddComponent<BattleHudLegacyLayoutFix>(); // HUD 레이아웃 보정 컴포넌트 연결
        }

        private void LateUpdate()
        {
            ApplyBattleHudLayout(); // 핵심 Battle HUD를 좌측 상단으로 정렬
            ApplyKingPlacementLayout(); // King 배치 안내를 Battle HUD 아래로 정렬
            ApplyInteractionLayout(); // 행동 안내를 King 안내 아래로 정렬
            ApplyBossBarLayout(); // 보스 체력바를 상단 중앙 같은 선으로 정렬
            SuppressLegacyOverlays(); // 중앙 중복 UI와 조작 범례 숨김
        }

        private static void ApplyBattleHudLayout()
        {
            GameObject hudRoot = GameObject.Find(BattleHudRootName); // 활성 Battle HUD 루트 조회

            if (hudRoot == null)
            {
                return; // HUD 생성 전 대기
            }

            RectTransform rootRect = hudRoot.GetComponent<RectTransform>(); // HUD 루트 RectTransform 조회

            if (rootRect == null)
            {
                return; // 잘못된 HUD 구성 방어
            }

            rootRect.anchorMin = TopLeftAnchor; // HUD 좌측 상단 앵커 시작 적용
            rootRect.anchorMax = TopLeftAnchor; // HUD 좌측 상단 앵커 끝 적용
            rootRect.pivot = TopLeftAnchor; // HUD 좌측 상단 피벗 적용
            rootRect.anchoredPosition = new Vector2(18f, -18f); // 화면 좌측 상단 여백 적용
            rootRect.sizeDelta = new Vector2(400f, 164f); // 좌측 상단 세로형 HUD 크기 적용

            ApplyTextLayout(hudRoot.transform, "StageText", new Vector2(14f, -8f), new Vector2(218f, 34f), TextAnchor.MiddleLeft, 22, HorizontalWrapMode.Overflow); // Stage 문구 좌상단 배치
            ApplyTextLayout(hudRoot.transform, "GoldText", new Vector2(236f, -8f), new Vector2(150f, 34f), TextAnchor.MiddleRight, 22, HorizontalWrapMode.Overflow); // Gold 문구 우측 배치
            ApplyTextLayout(hudRoot.transform, "BattleText", new Vector2(14f, -44f), new Vector2(96f, 28f), TextAnchor.MiddleLeft, 16, HorizontalWrapMode.Overflow); // Battle 문구 두 번째 줄 배치
            ApplyTextLayout(hudRoot.transform, "TurnStateText", new Vector2(112f, -44f), new Vector2(274f, 28f), TextAnchor.MiddleLeft, 20, HorizontalWrapMode.Overflow); // 턴 상태 두 번째 줄 배치
            ApplyTextLayout(hudRoot.transform, "TurnNumberText", new Vector2(14f, -76f), new Vector2(372f, 28f), TextAnchor.MiddleLeft, 18, HorizontalWrapMode.Overflow); // 턴 번호 세 번째 줄 배치
            ApplyTextLayout(hudRoot.transform, "DeploymentText", new Vector2(14f, -106f), new Vector2(372f, 46f), TextAnchor.MiddleLeft, 15, HorizontalWrapMode.Wrap); // 배치 안내 네 번째 줄 배치
        }

        private static void ApplyKingPlacementLayout()
        {
            GameObject placementRoot = GameObject.Find(KingPlacementRootName); // 활성 King 배치 안내 루트 조회

            if (placementRoot == null)
            {
                return; // 배치 안내 생성 전 대기
            }

            RectTransform rect = placementRoot.GetComponent<RectTransform>(); // 배치 안내 RectTransform 조회

            if (rect == null)
            {
                return; // 잘못된 UI 구성 방어
            }

            rect.anchorMin = TopLeftAnchor; // King 안내 좌측 상단 앵커 시작 적용
            rect.anchorMax = TopLeftAnchor; // King 안내 좌측 상단 앵커 끝 적용
            rect.pivot = TopLeftAnchor; // King 안내 좌측 상단 피벗 적용
            rect.anchoredPosition = new Vector2(18f, -194f); // Battle HUD 아래 12픽셀 간격 적용
            rect.sizeDelta = new Vector2(400f, 56f); // Battle HUD와 동일한 폭 적용

            Text placementText = placementRoot.GetComponentInChildren<Text>(true); // King 배치 안내 Text 조회

            if (placementText == null)
            {
                return; // 안내 Text 누락 방어
            }

            RectTransform textRect = placementText.rectTransform; // King 안내 Text RectTransform 조회
            textRect.anchorMin = Vector2.zero; // 안내 문구 전체 영역 시작 앵커 적용
            textRect.anchorMax = Vector2.one; // 안내 문구 전체 영역 끝 앵커 적용
            textRect.offsetMin = new Vector2(12f, 6f); // 안내 문구 좌하단 내부 여백 적용
            textRect.offsetMax = new Vector2(-12f, -6f); // 안내 문구 우상단 내부 여백 적용
            placementText.alignment = TextAnchor.MiddleLeft; // King 안내 문구 좌측 정렬 적용
            placementText.fontSize = 15; // 좁은 패널에 맞는 안내 글자 크기 적용
            placementText.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 King 안내 줄바꿈 허용
            placementText.verticalOverflow = VerticalWrapMode.Truncate; // 패널 높이 밖 문구 차단
        }

        private static void ApplyInteractionLayout()
        {
            GameObject interactionPanel = GameObject.Find(InteractionPanelName); // 활성 행동 안내 패널 조회

            if (interactionPanel == null)
            {
                return; // 행동 안내 생성 전 대기
            }

            RectTransform panelRect = interactionPanel.GetComponent<RectTransform>(); // 행동 안내 패널 RectTransform 조회

            if (panelRect == null)
            {
                return; // 잘못된 행동 안내 구성 방어
            }

            panelRect.anchorMin = TopLeftAnchor; // 행동 안내 좌측 상단 앵커 시작 적용
            panelRect.anchorMax = TopLeftAnchor; // 행동 안내 좌측 상단 앵커 끝 적용
            panelRect.pivot = TopLeftAnchor; // 행동 안내 좌측 상단 피벗 적용
            panelRect.anchoredPosition = new Vector2(18f, -262f); // King 안내 아래 12픽셀 간격 적용
            panelRect.sizeDelta = new Vector2(400f, 58f); // 좌측 상단 정보 열 폭 적용

            Transform instructionTransform = interactionPanel.transform.Find("InstructionText"); // 행동 안내 Text 자식 조회

            if (instructionTransform != null)
            {
                Text instructionText = instructionTransform.GetComponent<Text>(); // 행동 안내 Text 컴포넌트 조회

                if (instructionText != null)
                {
                    RectTransform instructionRect = instructionText.rectTransform; // 행동 안내 RectTransform 조회
                    instructionRect.anchorMin = Vector2.zero; // 행동 안내 전체 영역 시작 앵커 적용
                    instructionRect.anchorMax = Vector2.one; // 행동 안내 전체 영역 끝 앵커 적용
                    instructionRect.offsetMin = new Vector2(12f, 6f); // 행동 안내 좌하단 내부 여백 적용
                    instructionRect.offsetMax = new Vector2(-12f, -6f); // 행동 안내 우상단 내부 여백 적용
                    instructionText.alignment = TextAnchor.MiddleLeft; // 행동 안내 좌측 정렬 적용
                    instructionText.fontSize = 15; // 좁은 패널에 맞는 글자 크기 적용
                    instructionText.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 행동 안내 줄바꿈 허용
                    instructionText.verticalOverflow = VerticalWrapMode.Truncate; // 패널 높이 밖 문구 차단
                }
            }

            Transform legendTransform = interactionPanel.transform.Find("LegendText"); // 조작·색상 범례 Text 자식 조회

            if (legendTransform != null && legendTransform.gameObject.activeSelf)
            {
                legendTransform.gameObject.SetActive(false); // 불필요한 조작·색상 범례 Text 제거
            }
        }

        private static void ApplyBossBarLayout()
        {
            Text[] texts = Object.FindObjectsByType<Text>(FindObjectsSortMode.None); // 활성 Text 전체 조회

            for (int index = 0; index < texts.Length; index++)
            {
                Text text = texts[index]; // 현재 Text 조회

                if (text == null || text.text.Trim() != BossLabel)
                {
                    continue; // BOSS 라벨이 아니면 건너뜀
                }

                RectTransform bossRoot = FindBossPanelRoot(text.transform); // BOSS 라벨이 속한 패널 루트 조회

                if (bossRoot == null)
                {
                    continue; // 보스 패널 루트를 찾지 못하면 다음 후보 확인
                }

                bossRoot.anchorMin = new Vector2(0.5f, 1f); // 화면 상단 중앙 앵커 시작 적용
                bossRoot.anchorMax = new Vector2(0.5f, 1f); // 화면 상단 중앙 앵커 끝 적용
                bossRoot.pivot = new Vector2(0.5f, 1f); // 패널 상단 중앙 피벗 적용
                bossRoot.anchoredPosition = new Vector2(0f, -18f); // 좌측 HUD와 같은 상단 기준선 적용
                return; // 첫 활성 보스 체력바 보정 후 종료
            }
        }

        private static RectTransform FindBossPanelRoot(Transform bossLabelTransform)
        {
            Transform current = bossLabelTransform.parent; // BOSS 라벨의 부모부터 탐색 시작
            RectTransform highestImageRect = null; // Canvas 아래 가장 바깥 배경 패널 후보 저장

            while (current != null)
            {
                if (current.GetComponent<Canvas>() != null)
                {
                    break; // Canvas 자체는 이동 대상에서 제외
                }

                RectTransform rect = current as RectTransform; // 현재 UI RectTransform 변환
                Image image = current.GetComponent<Image>(); // 현재 배경 Image 조회

                if (rect != null && image != null)
                {
                    highestImageRect = rect; // 더 바깥쪽 Image 패널을 계속 후보로 갱신
                }

                current = current.parent; // 상위 UI 계층으로 이동
            }

            return highestImageRect; // 보스 체력바 최상위 배경 패널 반환
        }

        private static void ApplyTextLayout(Transform parent, string childName, Vector2 position, Vector2 size, TextAnchor alignment, int fontSize, HorizontalWrapMode horizontalOverflow)
        {
            Transform child = parent.Find(childName); // 지정 HUD Text 자식 조회

            if (child == null)
            {
                return; // 대상 Text 생성 전 대기
            }

            Text text = child.GetComponent<Text>(); // 지정 Text 컴포넌트 조회

            if (text == null)
            {
                return; // Text 컴포넌트 누락 방어
            }

            RectTransform rect = text.rectTransform; // Text RectTransform 조회
            rect.anchorMin = TopLeftAnchor; // Text 좌측 상단 앵커 시작 적용
            rect.anchorMax = TopLeftAnchor; // Text 좌측 상단 앵커 끝 적용
            rect.pivot = TopLeftAnchor; // Text 좌측 상단 피벗 적용
            rect.anchoredPosition = position; // Text 지정 위치 적용
            rect.sizeDelta = size; // Text 지정 크기 적용
            text.alignment = alignment; // Text 지정 정렬 적용
            text.fontSize = fontSize; // Text 지정 글자 크기 적용
            text.horizontalOverflow = horizontalOverflow; // Text 지정 가로 흐름 적용
            text.verticalOverflow = VerticalWrapMode.Truncate; // Text 영역 밖 문구 차단
        }

        private static void SuppressLegacyOverlays()
        {
            DisableCanvasByName(RoundSummaryCanvasName); // 중앙 Round·Turn 중복 Canvas 숨김
            DisableObjectByName(TurnStatusCanvasName); // 구형 중앙 턴 상태 Canvas 숨김
            DisableObjectByName(DeploymentBannerCanvasName); // 구형 중앙 배치 배너 Canvas 숨김
        }

        private static void DisableCanvasByName(string objectName)
        {
            GameObject target = GameObject.Find(objectName); // 지정 활성 Canvas 오브젝트 조회

            if (target == null)
            {
                return; // 대상 생성 전 대기
            }

            Canvas canvas = target.GetComponent<Canvas>(); // 대상 Canvas 컴포넌트 조회

            if (canvas != null && canvas.enabled)
            {
                canvas.enabled = false; // 중복 Canvas 렌더링 차단
            }
        }

        private static void DisableObjectByName(string objectName)
        {
            GameObject target = GameObject.Find(objectName); // 지정 활성 UI 오브젝트 조회

            if (target != null)
            {
                target.SetActive(false); // 구형 중복 UI 오브젝트 비활성화
            }
        }
    }
}
