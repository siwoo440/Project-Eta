using NUnit.Framework; // EditMode 단위 테스트 사용
using UnityEngine; // GameObject 사용
using ProjectEta.Board; // 66일차 지도 표시 규칙·노드 아이콘 사용
using ProjectEta.Run; // BoardMode·RunFlowPhase·StageType 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day66PresentationRegressionTests
    {
        [Test]
        public void RouteBuildings_RewardMap에서도_표시한다()
        {
            bool visible = Day66MapPresentationRules.ShouldShowRouteBuildings(BoardMode.Map, RunFlowPhase.Reward); // 전투 보상 중 지도 배경 표시 판정

            Assert.IsTrue(visible); // Reward 전환 순간에도 건물 Root 유지 검증
        }

        [Test]
        public void RouteBuildings_ShopEvent에서는_숨긴다()
        {
            Assert.IsFalse(Day66MapPresentationRules.ShouldShowRouteBuildings(BoardMode.Map, RunFlowPhase.Shop)); // 상점 오버레이 중 건물 숨김 검증
            Assert.IsFalse(Day66MapPresentationRules.ShouldShowRouteBuildings(BoardMode.Map, RunFlowPhase.Event)); // 이벤트 오버레이 중 건물 숨김 검증
        }

        [Test]
        public void BossHealth_전투상태에서만_표시한다()
        {
            Assert.IsTrue(Day66MapPresentationRules.ShouldShowBossHealth(BoardMode.Battle, RunFlowPhase.Battle)); // 실제 전투 중 보스바 표시 검증
            Assert.IsFalse(Day66MapPresentationRules.ShouldShowBossHealth(BoardMode.Map, RunFlowPhase.Map)); // 지도 전환 후 보스바 숨김 검증
            Assert.IsFalse(Day66MapPresentationRules.ShouldShowBossHealth(BoardMode.Map, RunFlowPhase.Reward)); // 보상 흐름 보스바 숨김 검증
        }

        [Test]
        public void RouteNodeIcon_상점노드는_단일아이콘을_즉시생성한다()
        {
            GameObject host = new GameObject("RouteNodeIconTestHost"); // 아이콘 테스트 Host 생성
            RouteNodeIconPresenter presenter = host.AddComponent<RouteNodeIconPresenter>(); // 실제 노드 아이콘 표시기 추가

            presenter.Initialize(null, StageType.Shop, 1f); // 상점 아이콘 즉시 생성

            Assert.IsNotNull(presenter.VisualRoot); // Host 자식 아이콘 Root 생성 검증
            Assert.AreEqual(host.transform, presenter.VisualRoot.transform.parent); // 아이콘 Root의 Host 직접 연결 검증
            Assert.AreEqual(1, presenter.PartCount); // 기존 입체 모델 대신 단일 아이콘 표면 검증
            Assert.AreEqual("RouteShopIcon", presenter.VisualRoot.GetComponentInChildren<Renderer>().sharedMaterial.mainTexture.name); // 상점 아이콘 텍스처 검증

            Object.DestroyImmediate(host); // 테스트 오브젝트 정리
        }
    }
}
