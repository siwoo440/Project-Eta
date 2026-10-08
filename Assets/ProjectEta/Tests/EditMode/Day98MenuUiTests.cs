using System; // 타입 조회 사용
using System.Reflection; // 실제 UI 생성 경로 접근
using NUnit.Framework; // 검증 기능 사용
using ProjectEta.King; // King 선택 화면 사용
using ProjectEta.Settings; // 실제 설정 화면 사용
using ProjectEta.UI; // 공통 이미지 스킨 사용
using UnityEngine; // 검증 오브젝트 사용
using UnityEngine.UI; // 버튼과 슬라이더 사용
using Object = UnityEngine.Object; // Unity 오브젝트 별칭

namespace ProjectEta.Tests.EditMode // 검증 네임스페이스
{ // 네임스페이스 시작
    public sealed class Day98MenuUiTests // 메뉴 이미지 적용 회귀 검증
    { // 클래스 시작
        private GameObject _host; // 검증 루트 보관

        [SetUp] // 검증 준비 표시
        public void SetUp() // 검증 루트 준비
        { // 메서드 시작
            _host = new GameObject("MenuSkinTest", typeof(RectTransform)); // 독립 검증 루트 생성
        } // 메서드 종료

        [TearDown] // 검증 정리 표시
        public void TearDown() // 검증 오브젝트 정리
        { // 메서드 시작
            Object.DestroyImmediate(_host); // 검증 루트 제거
        } // 메서드 종료

        [Test] // 자산 임포트 검증 표시
        public void StageFourAssets_KeepAlphaAndUiImportSettings() // 14개 자산 임포트 검증
        { // 메서드 시작
            Assert.AreEqual(14, Day98UiSkin.StageFourResourcePaths.Count); // 자산 수 확인
            CollectionAssert.AllItemsAreUnique(Day98UiSkin.StageFourResourcePaths); // 자산 중복 확인
            foreach (string path in Day98UiSkin.StageFourResourcePaths) // 개별 자산 순회
            { // 반복 시작
                Texture2D texture = Resources.Load<Texture2D>(path); // 실제 자산 로드
                Assert.IsNotNull(texture, path); // 자산 존재 확인
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(texture); // 임포트 경로 조회
                UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter; // 임포트 설정 조회
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha(), path); // 투명 채널 확인
                Assert.IsTrue(importer.alphaIsTransparency, path); // 투명 테두리 보정 확인
                Assert.IsFalse(importer.mipmapEnabled, path); // UI 밉맵 비활성 확인
                Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapModeU, path); // 가장자리 반복 방지 확인
            } // 반복 종료
        } // 메서드 종료

        [Test] // 버튼 입력 보존 검증 표시
        public void ButtonSkin_PreservesCallbackAndDisabledState() // 버튼 상태와 입력 연결 보존
        { // 메서드 시작
            Button button = _host.AddComponent<Button>(); // 검증 버튼 생성
            Image image = _host.AddComponent<Image>(); // 버튼 이미지 생성
            button.targetGraphic = image; // 버튼 그래픽 연결
            int clicks = 0; // 콜백 횟수 초기화
            button.onClick.AddListener(() => clicks++); // 기존 클릭 연결
            button.interactable = false; // 기존 비활성 상태 지정
            Assert.IsTrue(Day98UiSkin.TryApplyButton(button, Day98UiSkin.ButtonDangerResourcePath)); // 위험 버튼 이미지 적용
            Assert.IsFalse(button.interactable); // 기존 비활성 상태 보존 확인
            Assert.AreSame(image, button.targetGraphic); // 입력 대상 그래픽 보존 확인
            Assert.IsTrue(image.raycastTarget); // 버튼 입력 영역 보존 확인
            Assert.Less(button.colors.disabledColor.a, button.colors.normalColor.a); // 비활성 표시 구분 확인
            button.onClick.Invoke(); // 연결 콜백 직접 검증
            Assert.AreEqual(1, clicks); // 기존 콜백 유지 확인
        } // 메서드 종료

        [Test] // 누락 자산 검증 표시
        public void MissingButtonAsset_PreservesFallbackColorsAndGraphic() // 자산 누락 시 기존 표시 유지
        { // 메서드 시작
            Image image = _host.AddComponent<Image>(); // 검증 이미지 생성
            Button button = _host.AddComponent<Button>(); // 검증 버튼 생성
            button.targetGraphic = image; // 기존 그래픽 연결
            image.color = Color.red; // 기존 이미지 색상 지정
            ColorBlock original = button.colors; // 기존 상태 색상 보관
            Assert.IsFalse(Day98UiSkin.TryApplyButton(button, "UI/Day98/MissingMenuAsset")); // 누락 결과 확인
            Assert.AreEqual(Color.red, image.color); // 기존 이미지 색상 확인
            Assert.AreEqual(original, button.colors); // 기존 상태 색상 확인
            Assert.IsNull(image.sprite); // 잘못된 Sprite 연결 없음 확인
        } // 메서드 종료

        [TestCase("PreviousButton", "UiArrowLeft")] // 왼쪽 화살표 검증
        [TestCase("NextButton", "UiArrowRight")] // 오른쪽 화살표 검증
        public void KingArrow_UsesCorrectGlyphAndKeepsInput(string name, string textureName) // 이동 버튼 중복 표시와 입력 확인
        { // 메서드 시작
            Button button = (Button)InvokeStatic(typeof(KingSelectionUI), "CreateArrowButton", name, _host.transform, "ARROW", Vector2.zero); // 실제 이동 버튼 생성
            Image image = button.GetComponent<Image>(); // 이동 이미지 조회
            Assert.AreEqual(textureName, image.sprite.texture.name); // 방향별 이미지 확인
            Assert.AreEqual(Image.Type.Simple, image.type); // 화살표 변형 방지 확인
            Assert.IsTrue(image.preserveAspect); // 화살표 비율 보존 확인
            Assert.IsTrue(image.raycastTarget); // 이동 입력 영역 확인
            Assert.IsFalse(button.GetComponentInChildren<Text>(true).enabled); // 중복 문자 숨김 확인
            Assert.AreSame(image, button.targetGraphic); // 버튼 입력 그래픽 확인
        } // 메서드 종료

        [Test] // 슬라이더 입력 검증 표시
        public void SettingsSlider_UpdatesFillHandleAndExistingValueCallback() // 실제 슬라이더 값 변경 경로 검증
        { // 메서드 시작
            Slider slider = (Slider)InvokeStatic(typeof(SettingsPanelController), "CreateSlider", "Volume", _host.transform, Vector2.zero, new Vector2(300f, 40f)); // 실제 슬라이더 생성
            int changes = 0; // 값 변경 횟수 초기화
            float received = 0f; // 전달 값 초기화
            slider.onValueChanged.AddListener(value => // 기존 값 연결 검증
            { // 콜백 시작
                changes++; // 값 변경 횟수 기록
                received = value; // 실제 전달 값 기록
            }); // 콜백 연결 종료
            slider.value = 0.25f; // 낮은 값 적용
            float firstPosition = slider.handleRect.anchorMin.x; // 초기 손잡이 위치 보관
            slider.value = 0.75f; // 높은 값 적용
            Assert.AreEqual(2, changes); // 값 변경 콜백 횟수 확인
            Assert.AreEqual(0.75f, received); // 실제 전달 값 확인
            Assert.AreEqual(0.75f, slider.fillRect.anchorMax.x); // 채움 비율 확인
            Assert.Greater(slider.handleRect.anchorMin.x, firstPosition); // 손잡이 이동 확인
            Image handle = slider.targetGraphic as Image; // 입력 손잡이 조회
            Assert.AreEqual("UiSliderHandle", handle.sprite.texture.name); // 손잡이 자산 확인
            Assert.IsTrue(handle.raycastTarget); // 손잡이 입력 영역 확인
            Assert.AreEqual("UiSliderTrack", slider.transform.Find("Background").GetComponent<Image>().sprite.texture.name); // 트랙 자산 확인
        } // 메서드 종료

        [Test] // 설정 버튼 검증 표시
        public void SettingsCategoryAndArrow_UseDistinctSkinsAndKeepLabels() // 탭과 화살표 표시 구분
        { // 메서드 시작
            Button category = (Button)InvokeStatic(typeof(SettingsPanelController), "CreateButton", "Category_Sound", _host.transform, "사운드", Vector2.zero, new Vector2(220f, 78f)); // 실제 탭 버튼 생성
            Button arrow = (Button)InvokeStatic(typeof(SettingsPanelController), "CreateButton", "Next", _host.transform, "›", Vector2.zero, new Vector2(60f, 54f)); // 실제 값 이동 버튼 생성
            Assert.AreEqual("UiCategoryTab", category.GetComponent<Image>().sprite.texture.name); // 탭 자산 확인
            Assert.IsTrue(category.GetComponentInChildren<Text>(true).enabled); // 탭 문구 유지 확인
            Assert.AreEqual("사운드", category.GetComponentInChildren<Text>(true).text); // 한글 탭 문구 확인
            Assert.AreEqual("UiArrowRight", arrow.GetComponent<Image>().sprite.texture.name); // 화살표 자산 확인
            Assert.IsFalse(arrow.GetComponentInChildren<Text>(true).enabled); // 중복 화살표 제거 확인
        } // 메서드 종료

        [Test] // 장식 입력 검증 표시
        public void Decoration_DoesNotBlockInputsAndRetainsRequestedBounds() // 고정 아이콘 입력 간섭 방지
        { // 메서드 시작
            Image icon = Day98UiSkin.CreateIcon("Close", _host.transform, Day98UiSkin.UiCloseIconResourcePath, new Vector2(-90f, 0f), new Vector2(26f, 26f)); // 닫기 장식 생성
            Assert.IsFalse(icon.raycastTarget); // 장식 입력 간섭 없음 확인
            Assert.AreEqual(new Vector2(26f, 26f), icon.rectTransform.sizeDelta); // 장식 크기 확인
            Assert.AreEqual(new Vector2(-90f, 0f), icon.rectTransform.anchoredPosition); // 장식 위치 확인
            Assert.IsTrue(icon.preserveAspect); // 장식 원본 비율 확인
        } // 메서드 종료

         [Test] // 설정 배치 회귀 검증 표시
        public void SettingsRowsAndStatus_StayInsideFramesWithoutCoveringButtons() // 설정 문구와 버튼 경계 검증
        { // 메서드 시작
            SettingsPanelController ui = _host.AddComponent<SettingsPanelController>(); // 실제 설정 화면 생성
            InvokeInstance(ui, "BuildUI"); // 실제 설정 배치 생성
            RectTransform panel = (RectTransform)_host.transform.Find("SettingsPanel_Day57"); // 중앙 패널 조회
            RectTransform status = ((Text)typeof(SettingsPanelController).GetField("_statusText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui)).rectTransform; // 실제 변경 상태 문구 조회
            Bounds statusBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, status); // 패널 기준 상태 문구 경계
            foreach (string name in new[] { "Reset", "Cancel", "Apply" }) // 하단 버튼 순회
            { // 반복 시작
                Bounds buttonBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, panel.Find(name)); // 패널 기준 버튼 경계
                Assert.IsFalse(statusBounds.Intersects(buttonBounds), name); // 문구와 버튼 겹침 방지
            } // 반복 종료
            foreach (Text text in panel.GetComponentsInChildren<Text>(true)) // 설정 항목 문구 순회
            { // 반복 시작
                if (text.name != "Value") // 설정 값 문구 확인
                { // 조건 시작
                    continue; // 다른 문구 제외
                } // 조건 종료
                RectTransform row = text.transform.parent as RectTransform; // 값 표시 행 조회
                AssertInside(row, text.rectTransform); // 행 테두리 안쪽 값 표시 확인
            } // 반복 종료
            foreach (Button button in panel.GetComponentsInChildren<Button>(true)) // 설정 버튼 순회
            { // 반복 시작
                if (button.name == "Next" || button.name == "Previous") // 행 이동 화살표 확인
                { // 조건 시작
                    AssertInside(button.transform.parent as RectTransform, button.transform as RectTransform); // 행 안쪽 화살표 확인
                } // 조건 종료
            } // 반복 종료
        } // 메서드 종료

        [Test] // 성장 목록 회귀 검증 표시
        public void UnlockList_ClipsOverflowAndCanScrollToLastEntry() // 마지막 해금 항목 접근 검증
        { // 메서드 시작
            ProjectEta.Meta.MetaProgressPanelController ui = _host.AddComponent<ProjectEta.Meta.MetaProgressPanelController>(); // 실제 성장 화면 생성
            InvokeInstance(ui, "BuildUI"); // 실제 성장 배치 생성
            typeof(ProjectEta.Meta.MetaProgressPanelController).GetField("_progress", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ui, new ProjectEta.Meta.MetaProgressState()); // 파일 저장 없는 검증 진행 연결
            InvokeInstance(ui, "RebuildItems"); // 실제 전체 해금 목록 생성
            ScrollRect scroll = _host.GetComponentInChildren<ScrollRect>(true); // 목록 스크롤 조회
            Assert.IsNotNull(scroll, "해금 목록 스크롤 누락"); // 스크롤 존재 확인
            Assert.IsNotNull(scroll.viewport.GetComponent<RectMask2D>()); // 목록 바깥 표시 차단 확인
            AssertInside(scroll.viewport, scroll.content.GetChild(0) as RectTransform); // 최초 진입 첫 항목 접근 확인
            Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height); // 전체 목록 높이 확보 확인
            Assert.AreEqual(ProjectEta.Meta.MetaUnlockCatalog.All.Count, scroll.content.childCount); // 해금 항목 누락 없음 확인
            scroll.verticalNormalizedPosition = 0f; // 목록 맨 아래 이동
            RectTransform last = scroll.content.GetChild(scroll.content.childCount - 1) as RectTransform; // 마지막 해금 항목 조회
            AssertInside(scroll.viewport, last); // 마지막 항목 전체 접근 확인
        } // 메서드 종료

        [Test] // 조작법 배치 회귀 검증 표시
        public void ControlsHeading_StaysInsideItsPanel() // 조작법 제목 패널 이탈 방지
        { // 메서드 시작
            BattleSettingsOverlayController ui = _host.AddComponent<BattleSettingsOverlayController>(); // 실제 조작법 화면 생성
            InvokeInstance(ui, "BuildControlsScreen", _host.transform); // 실제 조작법 배치 생성
            RectTransform panel = _host.transform.Find("ControlsRoot_Day68/ControlsPanel") as RectTransform; // 조작법 패널 조회
            AssertInside(panel, panel.Find("Section") as RectTransform); // 분류 제목 경계 확인
            AssertInside(panel, panel.Find("Title") as RectTransform); // 큰 제목 경계 확인
        } // 메서드 종료

        private static void AssertInside(RectTransform parent, RectTransform child) // UI 경계 포함 검증 보조
        { // 메서드 시작
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, child); // 부모 기준 자식 경계 계산
            Assert.GreaterOrEqual(bounds.min.x, parent.rect.xMin + 4f, child.name); // 왼쪽 여백 확인
            Assert.LessOrEqual(bounds.max.x, parent.rect.xMax - 4f, child.name); // 오른쪽 여백 확인
            Assert.GreaterOrEqual(bounds.min.y, parent.rect.yMin + 4f, child.name); // 아래쪽 여백 확인
            Assert.LessOrEqual(bounds.max.y, parent.rect.yMax - 4f, child.name); // 위쪽 여백 확인
        } // 메서드 종료

        private static object InvokeInstance(object target, string method, params object[] args) // 실제 인스턴스 생성 경로 호출
        { // 메서드 시작
            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance); // 실제 내부 경로 조회
            Assert.IsNotNull(info, method); // 생성 경로 존재 확인
            return info.Invoke(target, args); // 실제 생성 경로 실행
        } // 메서드 종료

        private static object InvokeStatic(Type type, string method, params object[] args) // 실제 생성 메서드 호출 보조
        { // 메서드 시작
            MethodInfo info = type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static); // 실제 내부 생성 경로 조회
            Assert.IsNotNull(info, method); // 생성 경로 존재 확인
            return info.Invoke(null, args); // 실제 생성 경로 실행
        } // 메서드 종료
    } // 클래스 종료
} // 네임스페이스 종료
