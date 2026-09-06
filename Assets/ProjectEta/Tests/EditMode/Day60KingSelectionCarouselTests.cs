using System.IO; // 소스 회귀 검사 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // Application 경로 사용
using ProjectEta.King; // 60일차 King 캐러셀 상태·정보 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day60KingSelectionCarouselTests
    {
        [Test]
        public void KingSelectionCarouselState_WrapsNextAndPrevious()
        {
            var state = new KingSelectionCarouselState(KingArchetype.Default); // 기본 King에서 캐러셀 시작

            state.Move(-1); // 첫 항목에서 이전 이동
            Assert.AreEqual(KingArchetype.Strategy, state.CurrentArchetype); // 마지막 전략형으로 순환 검증

            state.Move(1); // 마지막에서 다음 이동
            Assert.AreEqual(KingArchetype.Default, state.CurrentArchetype); // 첫 기본형으로 순환 검증
        }

        [Test]
        public void KingSelectionCarouselState_ProvidesPreviousAndNextPreview()
        {
            var state = new KingSelectionCarouselState(KingArchetype.Attack); // 공격형을 중앙 King으로 설정

            Assert.AreEqual(KingArchetype.Default, state.Peek(-1)); // 왼쪽 미리보기 기본 King 검증
            Assert.AreEqual(KingArchetype.Attack, state.Peek(0)); // 중앙 공격형 King 검증
            Assert.AreEqual(KingArchetype.Defense, state.Peek(1)); // 오른쪽 미리보기 방어형 King 검증
            Assert.AreEqual(2, state.PageNumber); // 1부터 시작하는 페이지 번호 검증
            Assert.AreEqual(4, state.Count); // 전체 King 수 검증
        }

        [Test]
        public void KingSelectionPresentationCatalog_ProvidesDetailedInformationForEveryKing()
        {
            KingArchetype[] archetypes =
            {
                KingArchetype.Default, // 기본 King
                KingArchetype.Attack, // 공격형 King
                KingArchetype.Defense, // 방어형 King
                KingArchetype.Strategy // 전략형 King
            };

            for (int i = 0; i < archetypes.Length; i++)
            {
                KingSelectionPresentation presentation = KingSelectionPresentationCatalog.Get(archetypes[i]); // 현재 King 표시 정보 조회

                Assert.AreEqual(archetypes[i], presentation.Archetype); // King 타입 일치 검증
                Assert.IsFalse(string.IsNullOrWhiteSpace(presentation.DisplayName)); // 표시 이름 존재 검증
                Assert.IsFalse(string.IsNullOrWhiteSpace(presentation.PassiveName)); // 패시브 이름 존재 검증
                Assert.IsFalse(string.IsNullOrWhiteSpace(presentation.PassiveDescription)); // 패시브 설명 존재 검증
                Assert.IsFalse(string.IsNullOrWhiteSpace(presentation.PlayStyle)); // 플레이 스타일 설명 존재 검증
            }
        }

        [Test]
        public void KingSelectionUI_UsesSidePreviewCardsAndSlideAnimation()
        {
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/King/KingSelectionUI.cs"); // King 선택 UI 소스 경로
            string source = File.ReadAllText(sourcePath); // 현재 King 선택 UI 구현 읽기

            StringAssert.Contains("CarouselCardView[] _carouselCards", source); // 다중 캐러셀 카드 슬롯 검증
            StringAssert.Contains("CreateCarouselCards", source); // 좌우 미리보기 카드 생성 검증
            StringAssert.Contains("AnimateCarousel", source); // 슬라이드 전환 애니메이션 검증
            StringAssert.Contains("Time.unscaledDeltaTime", source); // TimeScale과 무관한 UI 슬라이드 검증
            StringAssert.Contains("PreviousButton", source); // 이전 화살표 버튼 검증
            StringAssert.Contains("NextButton", source); // 다음 화살표 버튼 검증
        }

        [Test]
        public void KingSelectionUI_RequiresConfirmationBeforeInitialPlacement()
        {
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/King/KingSelectionUI.cs"); // King 선택 UI 소스 경로
            string source = File.ReadAllText(sourcePath); // 현재 King 선택 UI 구현 읽기

            StringAssert.Contains("_selectionConfirmed", source); // King 선택 확정 상태 검증
            StringAssert.Contains("이 King으로 시작", source); // 선택 확정 버튼 문구 검증
            StringAssert.Contains("보드에 King을 배치하세요", source); // 확정 후 배치 안내 검증
        }
    }
}
