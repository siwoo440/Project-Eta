using NUnit.Framework; // EditMode 테스트 사용
using ProjectEta.Meta; // 메타 진행·보상·해금 시스템 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day48MetaProgressTests
    {
        [Test]
        public void MetaProgress_AddAndSpend_NeverDropsBelowZero()
        {
            var progress = new MetaProgressState(); // 빈 영구 진행 상태 생성
            progress.AddTokens(25); // 메타 토큰 지급

            bool spent = progress.TrySpendTokens(10); // 정상 비용 지불
            bool rejected = progress.TrySpendTokens(20); // 잔액 초과 비용 지불

            Assert.IsTrue(spent); // 정상 지불 성공 검증
            Assert.IsFalse(rejected); // 잔액 초과 지불 차단 검증
            Assert.AreEqual(15, progress.MetaTokens); // 최종 토큰 보존 검증
        }

        [Test]
        public void Reward_FailedAtStageThree_ReturnsParticipationAndDepthReward()
        {
            int reward = MetaRewardCalculator.Calculate(3, false, false, false); // 3단계 패배 보상 계산
            Assert.AreEqual(8, reward); // 기본 2 + 단계 6 검증
        }

        [Test]
        public void Reward_AfterMidBoss_ReturnsMidBossBonus()
        {
            int reward = MetaRewardCalculator.Calculate(6, true, false, false); // 중간 보스 처치 후 6단계 패배 보상 계산
            Assert.AreEqual(24, reward); // 기본 2 + 단계 12 + 중간 보스 10 검증
        }

        [Test]
        public void Reward_CompletedRun_ReturnsFullClearBonuses()
        {
            int reward = MetaRewardCalculator.Calculate(10, true, true, true); // 10단계 런 클리어 보상 계산
            Assert.AreEqual(72, reward); // 전체 보상 검증
        }

        [Test]
        public void Unlock_SufficientTokens_SpendsAndPersistsFlag()
        {
            var progress = new MetaProgressState(); // 영구 진행 상태 생성
            progress.AddTokens(100); // 해금 비용 준비
            var definition = new MetaUnlockDefinition("king_attack", "공격형 킹", MetaUnlockType.King, 60); // 테스트 해금 정의 생성

            bool unlocked = MetaUnlockService.TryUnlock(progress, definition); // 영구 해금 시도

            Assert.IsTrue(unlocked); // 해금 성공 검증
            Assert.AreEqual(40, progress.MetaTokens); // 비용 차감 검증
            Assert.IsTrue(progress.IsUnlocked(MetaUnlockType.King, "king_attack")); // 영구 해금 플래그 검증
        }

        [Test]
        public void Unlock_Duplicate_DoesNotSpendAgain()
        {
            var progress = new MetaProgressState(); // 영구 진행 상태 생성
            progress.AddTokens(100); // 해금 비용 준비
            var definition = new MetaUnlockDefinition("king_attack", "공격형 킹", MetaUnlockType.King, 60); // 테스트 해금 정의 생성
            MetaUnlockService.TryUnlock(progress, definition); // 최초 해금 처리

            bool duplicate = MetaUnlockService.TryUnlock(progress, definition); // 중복 해금 시도

            Assert.IsFalse(duplicate); // 중복 해금 차단 검증
            Assert.AreEqual(40, progress.MetaTokens); // 중복 비용 미차감 검증
        }

        [Test]
        public void Unlock_InsufficientTokens_IsRejected()
        {
            var progress = new MetaProgressState(); // 영구 진행 상태 생성
            progress.AddTokens(10); // 부족한 토큰 지급
            var definition = new MetaUnlockDefinition("passive_guard", "수호 패시브", MetaUnlockType.Passive, 20); // 테스트 해금 정의 생성

            bool unlocked = MetaUnlockService.TryUnlock(progress, definition); // 비용 부족 해금 시도

            Assert.IsFalse(unlocked); // 비용 부족 차단 검증
            Assert.AreEqual(10, progress.MetaTokens); // 토큰 미차감 검증
            Assert.IsFalse(progress.IsUnlocked(MetaUnlockType.Passive, "passive_guard")); // 해금 플래그 미등록 검증
        }

        [Test]
        public void SaveRoundTrip_RestoresTokensAndUnlocks()
        {
            var progress = new MetaProgressState(); // 원본 영구 진행 상태 생성
            progress.AddTokens(77); // 저장 토큰 설정
            progress.Unlock(MetaUnlockType.Piece, "piece_unlock_01"); // 기물 해금 설정
            progress.Unlock(MetaUnlockType.King, "king_attack"); // 킹 해금 설정
            progress.Unlock(MetaUnlockType.Passive, "passive_unlock_01"); // 패시브 해금 설정

            string json = MetaProgressSaveService.Serialize(progress); // JSON 문자열 직렬화
            MetaProgressState restored = MetaProgressSaveService.Deserialize(json); // JSON 문자열 역직렬화

            Assert.IsNotNull(restored); // 복원 상태 생성 검증
            Assert.AreEqual(77, restored.MetaTokens); // 토큰 복원 검증
            Assert.IsTrue(restored.IsUnlocked(MetaUnlockType.Piece, "piece_unlock_01")); // 기물 해금 복원 검증
            Assert.IsTrue(restored.IsUnlocked(MetaUnlockType.King, "king_attack")); // 킹 해금 복원 검증
            Assert.IsTrue(restored.IsUnlocked(MetaUnlockType.Passive, "passive_unlock_01")); // 패시브 해금 복원 검증
        }
    }
}
