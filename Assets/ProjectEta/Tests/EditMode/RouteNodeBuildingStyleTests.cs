using NUnit.Framework; // NUnit 테스트 기능
using ProjectEta.Board; // RouteNodeBuildingStyle 사용
using ProjectEta.Run; // StageType 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class RouteNodeBuildingStyleTests
    {
        [Test]
        public void Resolve_AllStageTypes_UsesDedicatedBuildingKinds()
        {
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Battle).Kind, Is.EqualTo(RouteNodeBuildingKind.Keep)); // 일반 전투 성채 확인
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Elite).Kind, Is.EqualTo(RouteNodeBuildingKind.EliteKeep)); // 엘리트 요새 확인
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Reward).Kind, Is.EqualTo(RouteNodeBuildingKind.Treasury)); // 보상 보물고 확인
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Shop).Kind, Is.EqualTo(RouteNodeBuildingKind.MerchantHouse)); // 상점 건물 확인
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Event).Kind, Is.EqualTo(RouteNodeBuildingKind.ArcaneTower)); // 이벤트 탑 확인
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.MidBoss).Kind, Is.EqualTo(RouteNodeBuildingKind.BossFortress)); // 중간 보스 요새 확인
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.FinalBoss).Kind, Is.EqualTo(RouteNodeBuildingKind.FinalCitadel)); // 최종 보스 성채 확인
        }

        [Test]
        public void Resolve_BossStages_UseLargerScaleAndBossEmphasis()
        {
            RouteNodeBuildingStyle battle = RouteNodeBuildingStyle.Resolve(StageType.Battle); // 일반 전투 프로필 조회
            RouteNodeBuildingStyle midBoss = RouteNodeBuildingStyle.Resolve(StageType.MidBoss); // 중간 보스 프로필 조회
            RouteNodeBuildingStyle finalBoss = RouteNodeBuildingStyle.Resolve(StageType.FinalBoss); // 최종 보스 프로필 조회

            Assert.That(midBoss.Scale, Is.GreaterThan(battle.Scale)); // 중간 보스 확대 확인
            Assert.That(finalBoss.Scale, Is.GreaterThan(midBoss.Scale)); // 최종 보스 추가 확대 확인
            Assert.That(midBoss.BossEmphasis, Is.True); // 중간 보스 강조 확인
            Assert.That(finalBoss.BossEmphasis, Is.True); // 최종 보스 강조 확인
        }

        [Test]
        public void Resolve_NonBossStages_DoNotUseBossEmphasis()
        {
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Shop).BossEmphasis, Is.False); // 상점 일반 강조 확인
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Event).BossEmphasis, Is.False); // 이벤트 일반 강조 확인
            Assert.That(RouteNodeBuildingStyle.Resolve(StageType.Reward).BossEmphasis, Is.False); // 보상 일반 강조 확인
        }
    }
}
