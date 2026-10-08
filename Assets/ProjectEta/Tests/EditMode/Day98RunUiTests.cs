using System; // 콜백 타입 사용
using System.Reflection; // UI 생성 경로 검증
using NUnit.Framework; // 검증 기능 사용
using ProjectEta.Pieces; // 기물 정의 사용
using ProjectEta.UI; // 런 진행 UI 사용
using UnityEngine; // 테스트 오브젝트 사용
using UnityEngine.UI; // 버튼과 이미지 사용
using Object = UnityEngine.Object; // Unity 오브젝트 별칭

namespace ProjectEta.Tests.EditMode // 테스트 네임스페이스
{ // 네임스페이스 시작
    public sealed class Day98RunUiTests // 런 진행 UI 통합 검증
    { // 클래스 시작
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance; // 비공개 UI 접근 범위

        [Test] // 반복 적용 검증 표시
        public void ResourceApplication_ReusesSpriteAndPreservesInputSettings() // 반복 적용 Sprite 재사용 검증
        { // 테스트 시작
            GameObject host = new GameObject("SkinReuse", typeof(RectTransform), typeof(Image)); // 테스트 이미지 생성
            Image image = host.GetComponent<Image>(); // 이미지 조회
            image.raycastTarget = false; // 장식 입력 비활성
            try // 자원 정리 보장
            { // 검증 시작
                Assert.IsTrue(Day98UiSkin.TryApplyResource(image, Day98UiSkin.ChoiceCardFrameResourcePath)); // 최초 적용 확인
                Sprite first = image.sprite; // 최초 Sprite 보관
                Assert.IsTrue(Day98UiSkin.TryApplyResource(image, Day98UiSkin.ChoiceCardFrameResourcePath)); // 반복 적용 확인
                Assert.AreSame(first, image.sprite); // Sprite 중복 생성 없음 확인
                Assert.LessOrEqual(image.sprite.border.x / image.pixelsPerUnitMultiplier, 24f); // 고해상도 테두리 과대 표시 방지
                Assert.IsFalse(image.raycastTarget); // 입력 설정 보존 확인
                Assert.IsFalse(Day98UiSkin.TryApplyResource(image, "UI/Day98/MissingAsset")); // 누락 자산 처리 확인
                Assert.AreSame(first, image.sprite); // 누락 시 기존 Sprite 보존 확인
            } // 검증 종료
            finally // 자원 정리
            { // 정리 시작
                Object.DestroyImmediate(host); // 테스트 오브젝트 제거
            } // 정리 종료
        } // 테스트 종료

        [Test] // 생성 자산 로드 검증 표시
        public void StageThreeAssets_AreAvailableWithTransparentOuterPixels() // 11개 자산과 투명 채널 검증
        { // 테스트 시작
            Assert.AreEqual(11, Day98UiSkin.StageThreeResourcePaths.Count); // 자산 수 확인
            CollectionAssert.AllItemsAreUnique(Day98UiSkin.StageThreeResourcePaths); // 중복 자산 없음 확인
            foreach (string path in Day98UiSkin.StageThreeResourcePaths) // 생성 자산 순회
            { // 반복 시작
                Texture2D texture = Resources.Load<Texture2D>(path); // 실제 Resources 로드
                Assert.IsNotNull(texture, path); // 자산 존재 확인
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(texture); // 임포트 경로 조회
                UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter; // 임포트 정보 조회
                Assert.IsTrue(importer.DoesSourceTextureHaveAlpha(), path); // 원본 알파 채널 확인
                Assert.IsFalse(importer.mipmapEnabled, path); // 작은 UI 테두리 번짐 방지
                Assert.IsTrue(importer.alphaIsTransparency, path); // 투명 테두리 색상 보정 확인
            } // 반복 종료
        } // 테스트 종료

        [Test] // 보상 선택 동작 검증 표시
        public void RewardSelection_MovesHighlightWithoutGrantingBeforeConfirmation() // 미리 선택과 실제 지급 분리 검증
        { // 테스트 시작
            GameObject host = new GameObject("RewardSkin"); // 테스트 루트 생성
            PieceDefinition first = ScriptableObject.CreateInstance<PieceDefinition>(); // 첫 후보 생성
            PieceDefinition second = ScriptableObject.CreateInstance<PieceDefinition>(); // 두 번째 후보 생성
            try // 자원 정리 보장
            { // 검증 시작
                SetField(first, "_pieceId", "first"); // 첫 후보 식별자 설정
                SetField(second, "_pieceId", "second"); // 두 번째 후보 식별자 설정
                CardRewardUI ui = host.AddComponent<CardRewardUI>(); // 보상 UI 생성
                SetField(ui, "_root", host); // 테스트 카드 부모 연결
                int grants = 0; // 지급 횟수 초기화
                SetField(ui, "_selectionCallback", (Action<PieceDefinition>)(card => grants++)); // 지급 횟수 기록
                Invoke(ui, "CreateRewardCard", first, -180f); // 첫 후보 화면 생성
                Invoke(ui, "CreateRewardCard", second, 180f); // 두 번째 후보 화면 생성
                Transform firstCard = host.transform.Find("RewardCard_first"); // 첫 카드 조회
                Transform secondCard = host.transform.Find("RewardCard_second"); // 두 번째 카드 조회
                firstCard.GetComponent<Button>().onClick.Invoke(); // 첫 카드 선택
                Assert.IsTrue(firstCard.Find("ChoiceSelection").gameObject.activeSelf); // 첫 카드 강조 확인
                Assert.IsFalse(secondCard.Find("ChoiceSelection").gameObject.activeSelf); // 두 번째 강조 숨김 확인
                secondCard.GetComponent<Button>().onClick.Invoke(); // 선택 카드 변경
                Assert.IsFalse(firstCard.Find("ChoiceSelection").gameObject.activeSelf); // 이전 강조 해제 확인
                Assert.IsTrue(secondCard.Find("ChoiceSelection").gameObject.activeSelf); // 새 선택 강조 확인
                Assert.AreEqual(0, grants); // 확정 전 지급 없음 확인
                Assert.IsFalse(secondCard.Find("ChoiceSelection").GetComponent<Image>().raycastTarget); // 강조 입력 간섭 없음 확인
            } // 검증 종료
            finally // 자원 정리
            { // 정리 시작
                Object.DestroyImmediate(host); // 테스트 UI 제거
                Object.DestroyImmediate(first); // 첫 후보 제거
                Object.DestroyImmediate(second); // 두 번째 후보 제거
            } // 정리 종료
        } // 테스트 종료

        [Test] // 구매 상태 전환 검증 표시
        public void ShopCard_DistinguishesUnaffordablePurchasedAndEventOptions() // 구매 불가와 완료 상태 구분 검증
        { // 테스트 시작
            GameObject host = new GameObject("ShopSkin"); // 테스트 루트 생성
            try // 자원 정리 보장
            { // 검증 시작
                StageBoardOverlayUI ui = host.AddComponent<StageBoardOverlayUI>(); // 상점 UI 생성
                object view = Invoke(ui, "CreateOptionView", host.transform, 0); // 재사용 선택지 생성
                Configure(ui, view, new StageOverlayOption("기물", "Gold 부족", false, null, 75)); // Gold 부족 상품 설정
                Transform card = host.transform.Find("Option_0"); // 상품 카드 조회
                Assert.IsTrue(card.Find("ShopPrice").gameObject.activeSelf); // 가격표 표시 확인
                Assert.AreEqual("75 Gold", card.Find("ShopPrice/PriceText").GetComponent<Text>().text); // 실제 가격 확인
                Assert.IsFalse(card.Find("ShopPurchased").gameObject.activeSelf); // 구매 완료 오표시 없음 확인
                Assert.IsFalse(card.GetComponent<Button>().interactable); // 구매 불가 입력 차단 확인
                Configure(ui, view, new StageOverlayOption("기물", "구매 완료", false, null, 75, true)); // 구매 완료 상품 설정
                Assert.IsTrue(card.Find("ShopPurchased").gameObject.activeSelf); // 실제 구매 완료 표시 확인
                Assert.AreEqual("구매 완료", card.Find("ShopPrice/PriceText").GetComponent<Text>().text); // 완료 문구 확인
                Configure(ui, view, new StageOverlayOption("이벤트", "선택 설명", true, null)); // 이벤트로 선택지 재사용
                Assert.IsFalse(card.Find("ShopPrice").gameObject.activeSelf); // 이전 가격표 제거 확인
                Assert.IsFalse(card.Find("ShopPurchased").gameObject.activeSelf); // 이전 완료 표시 제거 확인
                Assert.IsTrue(card.GetComponent<Button>().interactable); // 이벤트 입력 복원 확인
                Assert.IsFalse(card.Find("ShopPurchased").GetComponent<Image>().raycastTarget); // 완료 표시 클릭 간섭 없음 확인
            } // 검증 종료
            finally // 자원 정리
            { // 정리 시작
                Object.DestroyImmediate(host); // 테스트 UI 제거
            } // 정리 종료
        } // 테스트 종료

        [TestCase(Day67AnnouncementKind.BattleStart, "BattleAnnouncementFrame")] // 전투 시작 배너 검증
        [TestCase(Day67AnnouncementKind.Victory, "VictoryDefeatPlaque")] // 승리 팻말 검증
        [TestCase(Day67AnnouncementKind.Defeat, "VictoryDefeatPlaque")] // 패배 팻말 검증
        [TestCase(Day67AnnouncementKind.RunCompleted, "VictoryDefeatPlaque")] // 런 완료 팻말 검증
        [TestCase(Day67AnnouncementKind.RunFailed, "VictoryDefeatPlaque")] // 런 실패 팻말 검증
        public void Announcement_UsesCorrectAssetWithoutBlockingBattle(Day67AnnouncementKind kind, string textureName) // 알림별 자산과 입력 유지 검증
        { // 테스트 시작
            GameObject host = new GameObject("AnnouncementSkin"); // 테스트 루트 생성
            try // 자원 정리 보장
            { // 검증 시작
                Day67BattleAnnouncementUI ui = host.AddComponent<Day67BattleAnnouncementUI>(); // 전투 알림 생성
                Invoke(ui, "EnsureUI"); // 배너 생성 보장
                Invoke(ui, "ApplyAnnouncement", new Day67BattleAnnouncement(kind, "TEST", "TEST", 1f)); // 종류별 배너 적용
                Image image = (Image)typeof(Day67BattleAnnouncementUI).GetField("_panelImage", PrivateInstance).GetValue(ui); // 배너 이미지 조회
                Assert.AreEqual(textureName, image.sprite.texture.name); // 적용 자산 확인
                Assert.IsFalse(image.raycastTarget); // 배너 입력 간섭 없음 확인
                Assert.IsFalse(image.GetComponent<CanvasGroup>().blocksRaycasts); // 배너 그룹 입력 간섭 없음 확인
                Assert.AreEqual(Color.white, image.color); // 원본 프레임 색상 유지 확인
            } // 검증 종료
            finally // 자원 정리
            { // 정리 시작
                Object.DestroyImmediate(host); // 테스트 배너 제거
            } // 정리 종료
        } // 테스트 종료

        private static void Configure(StageBoardOverlayUI ui, object view, StageOverlayOption option) // 상품 상태 적용 보조
        { // 메서드 시작
            Invoke(ui, "ConfigureOptionView", view, option, Vector2.zero, new Vector2(390f, 170f), 0f, false); // 실제 선택지 갱신 경로 실행
        } // 메서드 종료

        private static object Invoke(object target, string method, params object[] args) // 내부 UI 경로 호출 보조
        { // 메서드 시작
            MethodInfo info = target.GetType().GetMethod(method, PrivateInstance); // 내부 메서드 조회
            Assert.IsNotNull(info, method); // 호출 경로 존재 확인
            return info.Invoke(target, args); // 실제 UI 경로 실행
        } // 메서드 종료

        private static void SetField(object target, string field, object value) // 테스트 입력 연결 보조
        { // 메서드 시작
            FieldInfo info = target.GetType().GetField(field, PrivateInstance); // 내부 필드 조회
            Assert.IsNotNull(info, field); // 테스트 입력 경로 확인
            info.SetValue(target, value); // 테스트 값 연결
        } // 메서드 종료
    } // 클래스 종료
} // 네임스페이스 종료
