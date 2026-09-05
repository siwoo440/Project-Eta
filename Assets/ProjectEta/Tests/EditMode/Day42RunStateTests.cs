using NUnit.Framework; // EditMode 테스트·Assert 사용
using ProjectEta.Battle; // BattleOutcome 사용
using ProjectEta.Run; // RunState·RoundState 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public class Day42RunStateTests // 42일차 10라운드 런 상태 회귀 테스트
    {
        [Test] // 새 런 기본 상태 검증
        public void NewRun_StartsAtRound1NotStarted() // 1라운드 시작 전 상태 확인
        {
            var runState = new RunState(3); // 테스트 런 생성

            Assert.AreEqual(1, runState.CurrentRound); // 첫 라운드 번호 검증
            Assert.AreEqual(RoundProgressStatus.NotStarted, runState.CurrentRoundStatus); // 시작 전 상태 검증
            Assert.AreEqual(BattleOutcome.None, runState.LastBattleOutcome); // 결과 없음 검증
            Assert.IsFalse(runState.IsBossRound); // 1라운드 일반 라운드 검증
        }

        [TestCase(1, false)] // 1라운드 일반 판정
        [TestCase(4, false)] // 4라운드 일반 판정
        [TestCase(5, true)] // 5라운드 보스 판정
        [TestCase(6, false)] // 6라운드 일반 판정
        [TestCase(9, false)] // 9라운드 일반 판정
        [TestCase(10, true)] // 10라운드 보스 판정
        public void CurrentRound_TracksBossFlag(int roundNumber, bool expectedBoss) // 1~10 보스 플래그 규칙 확인
        {
            var runState = new RunState(3); // 테스트 런 생성
            runState.CurrentRound = roundNumber; // 검사 라운드 지정

            Assert.AreEqual(roundNumber, runState.CurrentRound); // 라운드 번호 유지 검증
            Assert.AreEqual(expectedBoss, runState.IsBossRound); // 보스 라운드 여부 검증
        }

        [Test] // 라운드 승리 상태 흐름 검증
        public void RoundLifecycle_VictoryBecomesCleared() // 시작→승리→클리어 상태 확인
        {
            var runState = new RunState(3); // 테스트 런 생성
            runState.CurrentRound = 5; // 보스 라운드 지정

            runState.StartCurrentRound(); // 라운드 시작
            Assert.AreEqual(RoundProgressStatus.InProgress, runState.CurrentRoundStatus); // 진행 중 상태 검증

            runState.RecordBattleOutcome(BattleOutcome.Victory); // 승리 결과 기록

            Assert.AreEqual(RoundProgressStatus.Cleared, runState.CurrentRoundStatus); // 클리어 상태 검증
            Assert.AreEqual(BattleOutcome.Victory, runState.LastBattleOutcome); // 승리 결과 검증
            Assert.IsTrue(runState.IsBossRound); // 보스 플래그 유지 검증
        }

        [Test] // 라운드 패배 상태 흐름 검증
        public void RoundLifecycle_DefeatBecomesFailed() // 시작→패배→실패 상태 확인
        {
            var runState = new RunState(3); // 테스트 런 생성
            runState.CurrentRound = 7; // 일반 라운드 지정
            runState.StartCurrentRound(); // 라운드 시작
            runState.RecordBattleOutcome(BattleOutcome.Defeat); // 패배 결과 기록

            Assert.AreEqual(RoundProgressStatus.Failed, runState.CurrentRoundStatus); // 실패 상태 검증
            Assert.AreEqual(BattleOutcome.Defeat, runState.LastBattleOutcome); // 패배 결과 검증
        }

        [Test] // 전투 임시 상태 분리 검증
        public void ResetBattleState_ReplacesBoardAndHandButPreservesRunProgress() // 전투 초기화 시 런 데이터 유지 확인
        {
            var runState = new RunState(3); // 테스트 런 생성
            runState.CurrentRound = 8; // 현재 라운드 지정
            runState.MetaCurrency = 12; // 메타 재화 지정
            runState.StartCurrentRound(); // 라운드 시작
            var originalBoard = runState.Board; // 기존 보드 참조 저장
            var originalHand = runState.Hand; // 기존 손패 참조 저장
            var originalDeck = runState.Deck; // 기존 덱 참조 저장

            runState.ResetBattleState(); // 전투 임시 상태 초기화

            Assert.That(runState.Board, Is.Not.SameAs(originalBoard)); // 보드 교체 검증
            Assert.That(runState.Hand, Is.Not.SameAs(originalHand)); // 손패 교체 검증
            Assert.That(runState.Deck, Is.SameAs(originalDeck)); // 런 덱 유지 검증
            Assert.AreEqual(8, runState.CurrentRound); // 현재 라운드 유지 검증
            Assert.AreEqual(12, runState.MetaCurrency); // 메타 재화 유지 검증
            Assert.AreEqual(RoundProgressStatus.InProgress, runState.CurrentRoundStatus); // 라운드 상태 유지 검증
        }

        [Test] // 새 라운드 번호 변경 초기화 검증
        public void ChangingRound_ResetsRoundProgressOnly() // 다음 라운드 번호 지정 시 상태 초기화 확인
        {
            var runState = new RunState(3); // 테스트 런 생성
            runState.CurrentRound = 5; // 보스 라운드 지정
            runState.StartCurrentRound(); // 라운드 시작
            runState.RecordBattleOutcome(BattleOutcome.Victory); // 승리 완료

            runState.CurrentRound = 6; // 다음 라운드 지정

            Assert.AreEqual(6, runState.CurrentRound); // 새 라운드 번호 검증
            Assert.AreEqual(RoundProgressStatus.NotStarted, runState.CurrentRoundStatus); // 진행 상태 초기화 검증
            Assert.AreEqual(BattleOutcome.None, runState.LastBattleOutcome); // 이전 전투 결과 제거 검증
            Assert.IsFalse(runState.IsBossRound); // 일반 라운드 플래그 검증
        }

        [Test] // 저장·복원 라운드 상태 검증
        public void SaveRestore_PreservesRoundLifecycleAndBossFlag() // 라운드 진행 상태 저장 왕복 확인
        {
            var original = new RunState(3); // 원본 런 생성
            original.CurrentRound = 10; // 최종 보스 라운드 지정
            original.StartCurrentRound(); // 라운드 시작
            original.RecordBattleOutcome(BattleOutcome.Victory); // 승리 완료

            var saveData = original.ToSaveData(); // 저장 데이터 생성
            var restored = RunState.FromSaveData(saveData, null); // 저장 데이터 복원

            Assert.AreEqual(10, saveData.currentRound); // 저장 라운드 번호 검증
            Assert.AreEqual((int)RoundProgressStatus.Cleared, saveData.roundStatus); // 저장 진행 상태 검증
            Assert.AreEqual((int)BattleOutcome.Victory, saveData.battleOutcome); // 저장 전투 결과 검증
            Assert.IsTrue(saveData.isBossRound); // 저장 보스 플래그 검증
            Assert.AreEqual(10, restored.CurrentRound); // 복원 라운드 번호 검증
            Assert.AreEqual(RoundProgressStatus.Cleared, restored.CurrentRoundStatus); // 복원 진행 상태 검증
            Assert.AreEqual(BattleOutcome.Victory, restored.LastBattleOutcome); // 복원 전투 결과 검증
            Assert.IsTrue(restored.IsBossRound); // 복원 보스 플래그 검증
        }

        [Test] // 구버전 저장 호환 검증
        public void LegacySave_UsesSafeRoundDefaults() // 신규 필드가 없는 기존 저장 복원 확인
        {
            var legacyData = new RunSaveData // 구버전 형태 저장 데이터 생성
            {
                kingHp = 3, // 킹 체력 지정
                currentRound = 5, // 기존 라운드 번호 지정
                metaCurrency = 4 // 기존 메타 재화 지정
            };

            var restored = RunState.FromSaveData(legacyData, null); // 구버전 저장 복원

            Assert.AreEqual(5, restored.CurrentRound); // 기존 라운드 번호 보존 검증
            Assert.AreEqual(RoundProgressStatus.NotStarted, restored.CurrentRoundStatus); // 신규 상태 기본값 검증
            Assert.AreEqual(BattleOutcome.None, restored.LastBattleOutcome); // 신규 결과 기본값 검증
            Assert.IsTrue(restored.IsBossRound); // 라운드 번호 기반 보스 플래그 재계산 검증
        }

        [TestCase(0, 1)] // 최소 범위 보정
        [TestCase(-5, 1)] // 음수 범위 보정
        [TestCase(11, 10)] // 최대 범위 보정
        [TestCase(99, 10)] // 과대 범위 보정
        public void RoundNumber_ClampsToRunRange(int requestedRound, int expectedRound) // 1~10 라운드 범위 제한 확인
        {
            var runState = new RunState(3); // 테스트 런 생성
            runState.CurrentRound = requestedRound; // 범위 밖 라운드 지정

            Assert.AreEqual(expectedRound, runState.CurrentRound); // 보정된 라운드 번호 검증
        }
    }
}
