using NUnit.Framework; // NUnit 검증 기능 사용
using ProjectEta.UI; // 98일차 UI 스킨 사용
using UnityEngine; // GameObject·Texture2D 사용
using UnityEngine.UI; // Image 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{ // 네임스페이스 시작
    public sealed class Day98UiSkinTests // 98일차 공통 UI 스킨 테스트
    { // 클래스 시작
        [Test] // 리소스 경로 검증 테스트 표시
        public void PanelDarkResourcePath_UsesDay98ResourceFolder() // 공통 패널 리소스 경로 검증
        { // 테스트 시작
            Assert.AreEqual("UI/Day98/UiPanelDark", Day98UiSkin.PanelDarkResourcePath); // 고정 Resources 경로 확인
        } // 테스트 종료

        [Test] // Sliced 적용 검증 테스트 표시
        public void ApplyDarkPanel_UsesSlicedImageAndOpaqueTint() // 패널 이미지 적용 규칙 검증
        { // 테스트 시작
            GameObject host = new GameObject("Day98UiSkinTest", typeof(RectTransform), typeof(Image)); // 테스트 Image 오브젝트 생성
            Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false); // 테스트용 정사각 텍스처 생성
            Image image = host.GetComponent<Image>(); // 테스트 Image 참조 조회

            try // 테스트 자원 정리 보장 시작
            { // try 시작
                bool applied = Day98UiSkin.ApplyDarkPanel(image, texture); // 공통 어두운 패널 적용

                Assert.IsTrue(applied); // 정상 적용 결과 확인
                Assert.IsNotNull(image.sprite); // 생성 Sprite 확인
                Assert.AreEqual(Image.Type.Sliced, image.type); // 9-Slice 이미지 타입 확인
                Assert.AreEqual(Color.white, image.color); // 원본 이미지 색상 유지 확인
                Assert.AreEqual(new Vector4(30f, 30f, 30f, 30f), image.sprite.border); // 텍스처 크기 비례 테두리 확인
            } // try 종료
            finally // 테스트 자원 정리 시작
            { // finally 시작
                if (image.sprite != null) Object.DestroyImmediate(image.sprite); // 생성 Sprite 제거
                Object.DestroyImmediate(texture); // 테스트 텍스처 제거
                Object.DestroyImmediate(host); // 테스트 오브젝트 제거
            } // finally 종료
        } // 테스트 종료

        [Test] // 실제 패널 자산 검증 테스트 표시
        public void PanelDarkResource_CreatesProductionBorderWithoutCuttingCorners() // 실제 패널 모서리 보존 검증
        { // 테스트 시작
            Texture2D texture = Resources.Load<Texture2D>(Day98UiSkin.PanelDarkResourcePath); // 실제 공통 패널 텍스처 로드
            Assert.IsNotNull(texture); // 실제 Resources 자산 존재 확인
            Sprite sprite = Day98UiSkin.CreatePanelSprite(texture); // 실제 자산 9-Slice Sprite 생성

            try // 생성 Sprite 정리 보장 시작
            { // try 시작
                Assert.IsNotNull(sprite); // 실제 자산 Sprite 생성 확인
                Assert.AreEqual(new Vector4(122f, 122f, 122f, 122f), sprite.border); // Unity Import 크기 기준 모서리 보존 확인
            } // try 종료
            finally // 생성 Sprite 정리 시작
            { // finally 시작
                if (sprite != null) Object.DestroyImmediate(sprite); // 실제 자산 테스트 Sprite 제거
            } // finally 종료
        } // 테스트 종료

        [Test] // 금색 패널 리소스 경로 검증 표시
        public void PanelGoldResourcePath_UsesDay98ResourceFolder() // 금색 패널 Resources 경로 검증
        { // 테스트 시작
            Assert.AreEqual("UI/Day98/UiPanelGold", Day98UiSkin.PanelGoldResourcePath); // 금색 패널 고정 경로 확인
        } // 테스트 종료

        [Test] // 금색 패널 적용 규칙 검증 표시
        public void ApplyGoldPanel_UsesSlicedImageAndOpaqueTint() // 금색 패널 9-Slice 적용 검증
        { // 테스트 시작
            GameObject host = new GameObject("Day98GoldPanelTest", typeof(RectTransform), typeof(Image)); // 금색 패널 테스트 오브젝트 생성
            Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false); // 금색 패널 테스트 텍스처 생성
            Image image = host.GetComponent<Image>(); // 금색 패널 테스트 Image 조회

            try // 테스트 자원 정리 보장 시작
            { // try 시작
                bool applied = Day98UiSkin.ApplyGoldPanel(image, texture); // 금색 패널 직접 적용

                Assert.IsTrue(applied); // 금색 패널 정상 적용 확인
                Assert.IsNotNull(image.sprite); // 금색 패널 Sprite 생성 확인
                Assert.AreEqual(Image.Type.Sliced, image.type); // 금색 패널 9-Slice 타입 확인
                Assert.AreEqual(Color.white, image.color); // 금색 패널 원본 색상 유지 확인
                Assert.AreEqual(new Vector4(30f, 30f, 30f, 30f), image.sprite.border); // 금색 패널 비례 테두리 확인
            } // try 종료
            finally // 테스트 자원 정리 시작
            { // finally 시작
                if (image.sprite != null) Object.DestroyImmediate(image.sprite); // 금색 패널 테스트 Sprite 제거
                Object.DestroyImmediate(texture); // 금색 패널 테스트 텍스처 제거
                Object.DestroyImmediate(host); // 금색 패널 테스트 오브젝트 제거
            } // finally 종료
        } // 테스트 종료

        [Test] // 실제 금색 패널 자산 검증 표시
        public void PanelGoldResource_IsAvailableForRewardDetail() // 카드 보상용 금색 패널 자산 검증
        { // 테스트 시작
            Texture2D texture = Resources.Load<Texture2D>(Day98UiSkin.PanelGoldResourcePath); // 실제 금색 패널 텍스처 로드
            Assert.IsNotNull(texture); // 실제 금색 패널 자산 존재 확인
        } // 테스트 종료

        [Test] // 압축 2단계 자산 목록 검증 표시
        public void StageTwoResourcePaths_ContainsEighteenUniqueAssets() // 압축 2단계 독립 이미지 수 검증
        { // 테스트 시작
            Assert.AreEqual(18, Day98UiSkin.StageTwoResourcePaths.Count); // 압축 2단계 이미지 수 확인
            CollectionAssert.AllItemsAreUnique(Day98UiSkin.StageTwoResourcePaths); // 중복 Resources 경로 없음 확인
        } // 테스트 종료

        [Test] // 압축 2단계 실제 자산 검증 표시
        public void StageTwoResources_AreAllAvailableAsTextures() // 압축 2단계 모든 PNG 로드 검증
        { // 테스트 시작
            foreach (string path in Day98UiSkin.StageTwoResourcePaths) // 압축 2단계 경로 순회
            { // 반복 시작
                Assert.IsNotNull(Resources.Load<Texture2D>(path), path); // 개별 Texture2D Resources 로드 확인
            } // 반복 종료
        } // 테스트 종료

        [Test] // 단순 이미지 적용 규칙 검증 표시
        public void ApplyTexture_UsesSimpleModeWhenSlicingIsDisabled() // 고리·강조 이미지 단순 모드 검증
        { // 테스트 시작
            GameObject host = new GameObject("Day98SimpleImageTest", typeof(RectTransform), typeof(Image)); // 단순 이미지 테스트 오브젝트 생성
            Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false); // 단순 이미지 테스트 텍스처 생성
            Image image = host.GetComponent<Image>(); // 단순 이미지 테스트 Image 조회

            try // 테스트 자원 정리 보장 시작
            { // try 시작
                bool applied = Day98UiSkin.ApplyTexture(image, texture, false); // 단순 이미지 모드 적용

                Assert.IsTrue(applied); // 단순 이미지 정상 적용 확인
                Assert.AreEqual(Image.Type.Simple, image.type); // 단순 Image 타입 확인
                Assert.AreEqual(Color.white, image.color); // 단순 이미지 원본 색상 유지 확인
            } // try 종료
            finally // 테스트 자원 정리 시작
            { // finally 시작
                if (image.sprite != null) Object.DestroyImmediate(image.sprite); // 단순 이미지 테스트 Sprite 제거
                Object.DestroyImmediate(texture); // 단순 이미지 테스트 텍스처 제거
                Object.DestroyImmediate(host); // 단순 이미지 테스트 오브젝트 제거
            } // finally 종료
        } // 테스트 종료
    } // 클래스 종료
} // 네임스페이스 종료
