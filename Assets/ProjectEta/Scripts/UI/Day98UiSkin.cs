using System.Collections.Generic; // 읽기 전용 자산 경로 목록 사용
using UnityEngine; // Texture2D·Sprite·Resources 사용
using UnityEngine.UI; // Image와 9-Slice 타입 사용

namespace ProjectEta.UI // 프로젝트 UI 네임스페이스
{ // 네임스페이스 시작
    public static class Day98UiSkin // 98일차 공통 이미지 스킨 적용기
    { // 클래스 시작
        public const string PanelDarkResourcePath = "UI/Day98/UiPanelDark"; // 공통 어두운 패널 경로
        public const string PanelGoldResourcePath = "UI/Day98/UiPanelGold"; // 공통 금색 패널 경로
        public const string HeaderPlaqueResourcePath = "UI/Day98/UiHeaderPlaque"; // 제목 명패 경로
        public const string ButtonBaseResourcePath = "UI/Day98/UiButtonBase"; // 기본 버튼 경로
        public const string ButtonDangerResourcePath = "UI/Day98/UiButtonDanger"; // 위험 버튼 경로
        public const string DividerResourcePath = "UI/Day98/UiDivider"; // 구분선 경로
        public const string SelectionGlowResourcePath = "UI/Day98/UiSelectionGlow"; // 선택 강조 경로
        public const string LockedOverlayResourcePath = "UI/Day98/UiLockedOverlay"; // 잠금 오버레이 경로
        public const string BossBarResourcePath = "UI/Day98/BattleBossBarFrame"; // 보스 체력바 경로
        public const string PhaseBadgeResourcePath = "UI/Day98/BattlePhaseBadge"; // 보스 페이즈 배지 경로
        public const string BattleStatusResourcePath = "UI/Day98/BattleStatusFrame"; // 전투 상태 프레임 경로
        public const string BattleSidePanelResourcePath = "UI/Day98/BattleSidePanel"; // 전투 보조 패널 경로
        public const string HandTrayResourcePath = "UI/Day98/BattleHandTray"; // 손패 받침 경로
        public const string PileButtonResourcePath = "UI/Day98/BattlePileButton"; // 카드 더미 버튼 경로
        public const string CardFrameResourcePath = "UI/Day98/BattleCardFrame"; // 카드 외곽선 경로
        public const string BattleLogTabResourcePath = "UI/Day98/BattleLogTab"; // 전투 로그 탭 경로
        public const string RouteSidePanelResourcePath = "UI/Day98/RouteSidePanel"; // 지도 우측 패널 경로
        public const string RouteNodeTooltipResourcePath = "UI/Day98/RouteNodeTooltip"; // 지도 노드 설명 경로
        public const string RouteNodeHighlightRingResourcePath = "UI/Day98/RouteNodeHighlightRing"; // 이동 가능 고리 경로
        public const string RouteNodeLockedRingResourcePath = "UI/Day98/RouteNodeLockedRing"; // 잠금 노드 고리 경로
        public const string ChoiceCardFrameResourcePath = "UI/Day98/ChoiceCardFrame"; // 선택 카드 경로
        public const string ChoiceCardSelectedResourcePath = "UI/Day98/ChoiceCardSelected"; // 선택 강조 경로
        public const string ShopPriceTagResourcePath = "UI/Day98/ShopPriceTag"; // 상점 가격표 경로
        public const string ShopSoldOutOverlayResourcePath = "UI/Day98/ShopSoldOutOverlay"; // 구매 완료 경로
        public const string FusionMaterialSlotResourcePath = "UI/Day98/FusionMaterialSlot"; // 합성 재료 경로
        public const string FusionResultSlotResourcePath = "UI/Day98/FusionResultSlot"; // 합성 결과 경로
        public const string PieceInfoFrameResourcePath = "UI/Day98/PieceInfoFrame"; // 기물 정보 경로
        public const string AbilityTagResourcePath = "UI/Day98/AbilityTag"; // 능력 태그 경로
        public const string SystemToastFrameResourcePath = "UI/Day98/SystemToastFrame"; // 시스템 알림 경로
        public const string BattleAnnouncementFrameResourcePath = "UI/Day98/BattleAnnouncementFrame"; // 전투 알림 경로
        public const string VictoryDefeatPlaqueResourcePath = "UI/Day98/VictoryDefeatPlaque"; // 승패 팻말 경로
        public const string MainMenuBackdropResourcePath = "UI/Day98/MainMenuBackdrop"; // 4단계 MainMenuBackdrop 자산 경로
        public const string MainMenuEmblemResourcePath = "UI/Day98/MainMenuEmblem"; // 4단계 MainMenuEmblem 자산 경로
        public const string KingCardFrameResourcePath = "UI/Day98/KingCardFrame"; // 4단계 KingCardFrame 자산 경로
        public const string KingEmblemFrameResourcePath = "UI/Day98/KingEmblemFrame"; // 4단계 KingEmblemFrame 자산 경로
        public const string TutorialPageFrameResourcePath = "UI/Day98/TutorialPageFrame"; // 4단계 TutorialPageFrame 자산 경로
        public const string UiModalFrameResourcePath = "UI/Day98/UiModalFrame"; // 4단계 UiModalFrame 자산 경로
        public const string UiCategoryTabResourcePath = "UI/Day98/UiCategoryTab"; // 4단계 UiCategoryTab 자산 경로
        public const string UiSliderTrackResourcePath = "UI/Day98/UiSliderTrack"; // 4단계 UiSliderTrack 자산 경로
        public const string UiSliderHandleResourcePath = "UI/Day98/UiSliderHandle"; // 4단계 UiSliderHandle 자산 경로
        public const string MetaUnlockTileResourcePath = "UI/Day98/MetaUnlockTile"; // 4단계 MetaUnlockTile 자산 경로
        public const string RunResultFrameResourcePath = "UI/Day98/RunResultFrame"; // 4단계 RunResultFrame 자산 경로
        public const string UiCloseIconResourcePath = "UI/Day98/UiCloseIcon"; // 4단계 UiCloseIcon 자산 경로
        public const string UiArrowLeftResourcePath = "UI/Day98/UiArrowLeft"; // 4단계 UiArrowLeft 자산 경로
        public const string UiArrowRightResourcePath = "UI/Day98/UiArrowRight"; // 4단계 UiArrowRight 자산 경로
        private static readonly Dictionary<string, Sprite> ResourceSprites = new Dictionary<string, Sprite>(); // 리소스 Sprite 재사용 목록
        private const float PanelBorderRatio = 0.12f; // 모서리 장식 보존 비율
        private const float SpritePixelsPerUnit = 100f; // UI Sprite 픽셀 비율

        public static IReadOnlyList<string> StageTwoResourcePaths { get; } = new[] // 압축 2단계 자산 경로 목록
        { // 목록 시작
            HeaderPlaqueResourcePath, // 제목 명패 경로 등록
            ButtonBaseResourcePath, // 기본 버튼 경로 등록
            ButtonDangerResourcePath, // 위험 버튼 경로 등록
            DividerResourcePath, // 구분선 경로 등록
            SelectionGlowResourcePath, // 선택 강조 경로 등록
            LockedOverlayResourcePath, // 잠금 오버레이 경로 등록
            BossBarResourcePath, // 보스 체력바 경로 등록
            PhaseBadgeResourcePath, // 페이즈 배지 경로 등록
            BattleStatusResourcePath, // 전투 상태 프레임 경로 등록
            BattleSidePanelResourcePath, // 전투 보조 패널 경로 등록
            HandTrayResourcePath, // 손패 받침 경로 등록
            PileButtonResourcePath, // 카드 더미 버튼 경로 등록
            CardFrameResourcePath, // 카드 외곽선 경로 등록
            BattleLogTabResourcePath, // 전투 로그 탭 경로 등록
            RouteSidePanelResourcePath, // 지도 우측 패널 경로 등록
            RouteNodeTooltipResourcePath, // 지도 노드 설명 경로 등록
            RouteNodeHighlightRingResourcePath, // 이동 가능 고리 경로 등록
            RouteNodeLockedRingResourcePath // 잠금 노드 고리 경로 등록
        }; // 목록 종료

        public static IReadOnlyList<string> StageThreeResourcePaths { get; } = new[] // 압축 3단계 자산 목록
        { // 목록 시작
            ChoiceCardFrameResourcePath, // 개별 이미지 경로 등록
            ChoiceCardSelectedResourcePath, // 개별 이미지 경로 등록
            ShopPriceTagResourcePath, // 개별 이미지 경로 등록
            ShopSoldOutOverlayResourcePath, // 개별 이미지 경로 등록
            FusionMaterialSlotResourcePath, // 개별 이미지 경로 등록
            FusionResultSlotResourcePath, // 개별 이미지 경로 등록
            PieceInfoFrameResourcePath, // 개별 이미지 경로 등록
            AbilityTagResourcePath, // 개별 이미지 경로 등록
            SystemToastFrameResourcePath, // 개별 이미지 경로 등록
            BattleAnnouncementFrameResourcePath, // 개별 이미지 경로 등록
            VictoryDefeatPlaqueResourcePath, // 개별 이미지 경로 등록
        }; // 목록 종료

        public static IReadOnlyList<string> StageFourResourcePaths { get; } = new[] // 압축 4단계 자산 목록
        { // 목록 시작
            MainMenuBackdropResourcePath, // 개별 이미지 경로 등록
            MainMenuEmblemResourcePath, // 개별 이미지 경로 등록
            KingCardFrameResourcePath, // 개별 이미지 경로 등록
            KingEmblemFrameResourcePath, // 개별 이미지 경로 등록
            TutorialPageFrameResourcePath, // 개별 이미지 경로 등록
            UiModalFrameResourcePath, // 개별 이미지 경로 등록
            UiCategoryTabResourcePath, // 개별 이미지 경로 등록
            UiSliderTrackResourcePath, // 개별 이미지 경로 등록
            UiSliderHandleResourcePath, // 개별 이미지 경로 등록
            MetaUnlockTileResourcePath, // 개별 이미지 경로 등록
            RunResultFrameResourcePath, // 개별 이미지 경로 등록
            UiCloseIconResourcePath, // 개별 이미지 경로 등록
            UiArrowLeftResourcePath, // 개별 이미지 경로 등록
            UiArrowRightResourcePath, // 개별 이미지 경로 등록
        }; // 목록 종료

        public static bool TryApplyButton(Button button, string path) // 버튼 이미지와 상태 색상 적용
        { // 메서드 시작
            if (button == null || !TryApplyResource(button.targetGraphic as Image, path)) // 버튼 자산 적용 확인
            { // 조건 시작
                return false; // 기존 버튼 표시 보존
            } // 조건 종료
            ColorBlock colors = button.colors; // 기존 상태 설정 조회
            colors.normalColor = Color.white; // 기본 이미지 색상 유지
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f); // 마우스 강조 밝기
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f); // 누름 상태 밝기
            colors.selectedColor = colors.highlightedColor; // 선택 상태 강조
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.72f); // 비활성 상태 흐림
            colors.colorMultiplier = 1f; // 원본 밝기 배수
            button.colors = colors; // 버튼 상태 저장
            return true; // 정상 적용 결과
        } // 메서드 종료

        public static Image CreateIcon(string name, Transform parent, string path, Vector2 position, Vector2 size) // 입력 간섭 없는 고정 아이콘 생성
        { // 메서드 시작
            Image image = CreateDecoration(name, parent, path, false); // 단순 장식 생성
            RectTransform rect = image.rectTransform; // 장식 영역 조회
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 시작 앵커
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 끝 앵커
            rect.pivot = new Vector2(0.5f, 0.5f); // 중앙 기준점
            rect.anchoredPosition = position; // 아이콘 위치 적용
            rect.sizeDelta = size; // 아이콘 크기 적용
            image.preserveAspect = true; // 원본 비율 보존
            return image; // 아이콘 반환
        } // 메서드 종료

        public static Color GetStateTint(Color stateColor) // 상태별 이미지 색상 보정
        { // 메서드 시작
            return Color.Lerp(Color.white, stateColor, 0.32f); // 금색 테두리 밝기 보존
        } // 메서드 종료

        public static Image CreateDecoration(string name, Transform parent, string resourcePath, bool sliced = true) // 클릭 간섭 없는 장식 생성
        { // 메서드 시작
            GameObject host = new GameObject(name, typeof(RectTransform), typeof(Image)); // 장식 오브젝트 생성
            host.transform.SetParent(parent, false); // 부모 연결
            RectTransform rect = host.GetComponent<RectTransform>(); // 장식 영역 조회
            rect.anchorMin = Vector2.zero; // 전체 영역 시작
            rect.anchorMax = Vector2.one; // 전체 영역 끝
            rect.offsetMin = Vector2.zero; // 아래쪽 여백 제거
            rect.offsetMax = Vector2.zero; // 위쪽 여백 제거
            Image image = host.GetComponent<Image>(); // 장식 이미지 조회
            image.raycastTarget = false; // 입력 간섭 차단
            image.enabled = TryApplyResource(image, resourcePath, sliced); // 자산 존재 시 표시
            return image; // 장식 참조 반환
        } // 메서드 종료

        public static bool TryApplyDarkPanel(Image image) // 공통 어두운 패널 적용
        { // 메서드 시작
            return TryApplyResource(image, PanelDarkResourcePath, true); // 9-Slice 어두운 패널 적용
        } // 메서드 종료

        public static bool ApplyDarkPanel(Image image, Texture2D texture) // 전달 어두운 텍스처 적용
        { // 메서드 시작
            return ApplyTexture(image, texture, true); // 9-Slice 어두운 텍스처 적용
        } // 메서드 종료

        public static bool TryApplyGoldPanel(Image image) // 공통 금색 패널 적용
        { // 메서드 시작
            return TryApplyResource(image, PanelGoldResourcePath, true); // 9-Slice 금색 패널 적용
        } // 메서드 종료

        public static bool ApplyGoldPanel(Image image, Texture2D texture) // 전달 금색 텍스처 적용
        { // 메서드 시작
            return ApplyTexture(image, texture, true); // 9-Slice 금색 텍스처 적용
        } // 메서드 종료

        public static bool TryApplyResource(Image image, string resourcePath, bool sliced = true) // Resources 텍스처 적용
        { // 메서드 시작
            if (image == null || string.IsNullOrWhiteSpace(resourcePath)) // 입력 유효성 확인
            { // 조건 시작
                return false; // 잘못된 입력 결과 반환
            } // 조건 종료

            string cacheKey = resourcePath + (sliced ? "|sliced" : "|simple"); // 표시 방식별 캐시 키 생성
            if (!ResourceSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null) // 재사용 Sprite 존재 확인
            { // 조건 시작
                Texture2D texture = Resources.Load<Texture2D>(resourcePath); // 지정 텍스처 로드
                if (texture == null) // 자산 누락 확인
                { // 조건 시작
                    return false; // 기존 이미지 보존
                } // 조건 종료
                sprite = CreateResourceSprite(texture, resourcePath, sliced); // 자산 영역별 Sprite 생성
                ResourceSprites[cacheKey] = sprite; // 재사용 Sprite 저장
            } // 조건 종료
            image.sprite = sprite; // 캐시 Sprite 연결
            float referenceSide = resourcePath == AbilityTagResourcePath ? 20f : (sprite.rect.width > sprite.rect.height * 2f ? 64f : 200f); // 작은 태그와 가로 명패 테두리 기준
            image.pixelsPerUnitMultiplier = Mathf.Max(1f, Mathf.Min(sprite.rect.width, sprite.rect.height) / referenceSide); // 고해상도 테두리 표시 배율 보정
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple; // 표시 방식 적용
            image.color = Color.white; // 원본 이미지 색상 유지
            return true; // 정상 적용 결과 반환
        } // 메서드 종료

        public static bool ApplyTexture(Image image, Texture2D texture, bool sliced) // 전달 텍스처 표시 적용
        { // 메서드 시작
            if (image == null || texture == null) // 입력 유효성 확인
            { // 조건 시작
                return false; // 잘못된 입력 결과 반환
            } // 조건 종료

            Sprite sprite = sliced ? CreatePanelSprite(texture) : CreateSimpleSprite(texture); // 표시 방식별 Sprite 생성
            image.sprite = sprite; // 생성 Sprite 연결
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple; // 표시 방식 적용
            image.color = Color.white; // 원본 이미지 색상 유지
            return true; // 정상 적용 결과 반환
        } // 메서드 종료

        public static Sprite CreatePanelSprite(Texture2D texture) // 9-Slice 패널 Sprite 생성
        { // 메서드 시작
            if (texture == null) // 텍스처 존재 확인
            { // 조건 시작
                return null; // 누락 결과 반환
            } // 조건 종료

            float shortestSide = Mathf.Min(texture.width, texture.height); // 최단 변 길이 계산
            float maximumBorder = Mathf.Max(0f, Mathf.Floor(shortestSide * 0.5f) - 1f); // 최대 테두리 계산
            float desiredBorder = Mathf.Floor(shortestSide * PanelBorderRatio); // 목표 테두리 계산
            float border = Mathf.Min(desiredBorder, maximumBorder); // 안전 테두리 결정
            Rect rect = new Rect(0f, 0f, texture.width, texture.height); // 전체 텍스처 영역 설정
            Vector4 borders = new Vector4(border, border, border, border); // 동일 테두리 설정
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), SpritePixelsPerUnit, 0u, SpriteMeshType.FullRect, borders); // 9-Slice Sprite 생성
            sprite.name = $"{texture.name}_Day98SlicedSprite"; // 런타임 Sprite 이름 지정
            return sprite; // 생성 Sprite 반환
        } // 메서드 종료

        private static Sprite CreateResourceSprite(Texture2D texture, string path, bool sliced) // UI 자산 사용 영역 설정
        { // 메서드 시작
            Rect area = new Rect(0f, 0f, 1f, 1f); // 원본 전체 영역 설정
            bool horizontal = false; // 가로 명패 여부 초기화
            if (path == ShopPriceTagResourcePath) // 가격표 여백 확인
            { // 조건 시작
                area = new Rect(0.01f, 0.18f, 0.98f, 0.64f); // 가격표 실제 프레임 영역
                horizontal = true; // 가로 명패 표시
            } // 조건 종료
            else if (path == AbilityTagResourcePath || path == SystemToastFrameResourcePath || path == BattleAnnouncementFrameResourcePath || path == VictoryDefeatPlaqueResourcePath) // 가로 알림 여백 확인
            { // 조건 시작
                area = new Rect(0.01f, 0.12f, 0.98f, 0.76f); // 가로 프레임 실제 영역
                horizontal = true; // 가로 명패 표시
            } // 조건 종료
            else if (path == HeaderPlaqueResourcePath) // 제목 명패 여백 확인
            { // 조건 시작
                area = new Rect(0.02f, 0.25f, 0.96f, 0.5f); // 제목 명패 실제 영역
                horizontal = true; // 가로 명패 표시
            } // 조건 종료
            else if (path == ButtonBaseResourcePath || path == ButtonDangerResourcePath) // 공통 버튼 여백 확인
            { // 조건 시작
                area = new Rect(0.02f, 0.16f, 0.96f, 0.68f); // 버튼 실제 프레임 영역
                horizontal = true; // 가로 명패 표시
            } // 조건 종료
            else if (path == UiCategoryTabResourcePath) // 탭 바깥 여백 확인
            { // 조건 시작
                area = new Rect(0.02f, 0.16f, 0.96f, 0.66f); // 탭 실제 프레임 영역
                horizontal = true; // 가로 탭 표시
            } // 조건 종료
            else if (path == MetaUnlockTileResourcePath) // 해금 카드 여백 확인
            { // 조건 시작
                area = new Rect(0.02f, 0.14f, 0.96f, 0.7f); // 해금 카드 실제 영역
                horizontal = true; // 가로 카드 표시
            } // 조건 종료
            else if (path == UiSliderTrackResourcePath) // 슬라이더 트랙 여백 확인
            { // 조건 시작
                area = new Rect(0.015f, 0.3f, 0.97f, 0.4f); // 트랙 실제 레일 영역
                horizontal = true; // 가로 트랙 표시
            } // 조건 종료
            Rect rect = new Rect(area.x * texture.width, area.y * texture.height, area.width * texture.width, area.height * texture.height); // Sprite 사용 영역 계산
            float border = sliced ? Mathf.Floor(Mathf.Min(rect.width, rect.height) * (horizontal ? 0.28f : PanelBorderRatio)) : 0f; // 자산 모서리 보존 길이
            Vector4 borders = new Vector4(border, border, border, border); // 자산 테두리 구성
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), SpritePixelsPerUnit, 0u, SpriteMeshType.FullRect, borders); // 사용 영역 Sprite 생성
            sprite.name = texture.name + (sliced ? "_Day98SlicedSprite" : "_Day98SimpleSprite"); // Sprite 이름 지정
            return sprite; // 생성 Sprite 반환
        } // 메서드 종료

        private static Sprite CreateSimpleSprite(Texture2D texture) // 단순 Sprite 생성
        { // 메서드 시작
            Rect rect = new Rect(0f, 0f, texture.width, texture.height); // 전체 텍스처 영역 설정
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), SpritePixelsPerUnit); // 단순 Sprite 생성
            sprite.name = $"{texture.name}_Day98SimpleSprite"; // 런타임 Sprite 이름 지정
            return sprite; // 생성 Sprite 반환
        } // 메서드 종료
    } // 클래스 종료
} // 네임스페이스 종료
