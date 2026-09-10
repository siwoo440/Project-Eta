using System.IO; // 소스 회귀 검사 사용
using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // Application 경로 사용
using ProjectEta.King; // King 전투 HUD 표시 정보 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day61KingCombatHudTests
    {
        [Test]
        public void KingCombatHudPresentation_AttackShowsRageAndNextAttackBonus()
        {
            var state = new KingRunState(); // King 런 상태 생성
            state.Select(KingArchetype.Attack); // 공격형 King 선택
            state.TryAddRage(); // 격노 1 획득
            state.TryAddRage(); // 격노 2 획득

            KingCombatHudPresentation presentation = KingCombatHudPresentation.Build(state, 3, false); // 공격형 HUD 표시 정보 생성

            Assert.AreEqual("공격형 킹", presentation.KingName); // 공격형 이름 검증
            Assert.AreEqual("처형의 연쇄", presentation.PassiveName); // 공격형 패시브 이름 검증
            StringAssert.Contains("● ●", presentation.PrimaryStatus); // 격노 2칸 활성 표시 검증
            StringAssert.Contains("ATK +2", presentation.SecondaryStatus); // 다음 공격 보너스 검증
        }

        [Test]
        public void KingCombatHudPresentation_DefenseShowsBarrierAndMoveState()
        {
            var state = new KingRunState(); // King 런 상태 생성
            state.Select(KingArchetype.Defense); // 방어형 King 선택
            state.BeginPlayerTurn(); // 플레이어 턴 이동 상태 초기화
            state.TryGainBarrier(); // 방벽 획득

            KingCombatHudPresentation presentation = KingCombatHudPresentation.Build(state, 2, false); // 방어형 HUD 표시 정보 생성

            Assert.AreEqual("방어형 킹", presentation.KingName); // 방어형 이름 검증
            StringAssert.Contains("◆", presentation.PrimaryStatus); // 활성 방벽 표시 검증
            StringAssert.Contains("이동 안 함", presentation.SecondaryStatus); // 이동 상태 표시 검증
        }

        [Test]
        public void KingCombatHudPresentation_StrategyShowsPendingState()
        {
            var state = new KingRunState(); // King 런 상태 생성
            state.Select(KingArchetype.Strategy); // 전략형 King 선택
            state.TryBeginStrategyPreparation(5); // 전술적 준비 선택 대기 시작

            KingCombatHudPresentation presentation = KingCombatHudPresentation.Build(state, 3, true); // 전략형 HUD 표시 정보 생성

            Assert.AreEqual("전략형 킹", presentation.KingName); // 전략형 이름 검증
            StringAssert.Contains("카드 선택 중", presentation.PrimaryStatus); // 전술적 준비 선택 중 표시 검증
        }

        [Test]
        public void KingSelectionUI_NoLongerOwnsCombatStatusHud()
        {
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/King/KingSelectionUI.cs"); // King 선택 UI 소스 경로
            string source = File.ReadAllText(sourcePath); // 현재 King 선택 UI 구현 읽기

            StringAssert.DoesNotContain("격노 {kingState.RageStacks}", source); // 선택 UI 공격형 전투 상태 제거 검증
            StringAssert.DoesNotContain("방벽 {(kingState.BarrierActive", source); // 선택 UI 방어형 전투 상태 제거 검증
            StringAssert.Contains("보드에 King을 배치하세요", source); // 선택 확정 후 배치 안내 유지 검증
        }

        [Test]
        public void DebugBattleResultButtons_ConnectsVictoryAndDefeatToBattleController()
        {
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/UI/DebugBattleResultButtons.cs"); // 현재 단일 승리·패배 버튼 UI 소스 경로
            string source = File.ReadAllText(sourcePath); // 승리·패배 버튼 구현 읽기

            StringAssert.Contains("EndBattle(BattleOutcome.Victory)", source); // 승리 버튼 공통 전투 종료 흐름 연결 검증
            StringAssert.Contains("EndBattle(BattleOutcome.Defeat)", source); // 패배 버튼 공통 전투 종료 흐름 연결 검증
            StringAssert.Contains("\"승리\"", source); // 승리 버튼 문구 검증
            StringAssert.Contains("\"패배\"", source); // 패배 버튼 문구 검증
        }

        [Test]
        public void SceneRuntimeBootstrap_InjectsKingCombatHudAndOutcomeButtons()
        {
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs"); // 씬 부트스트랩 소스 경로
            string source = File.ReadAllText(sourcePath); // 씬 부트스트랩 구현 읽기

            StringAssert.Contains("KingCombatHUD", source); // King 전투 HUD 정리·호환 처리 유지 검증
            StringAssert.Contains("EnsureComponent<DebugBattleResultButtons>", source); // 현재 단일 승리·패배 개발 버튼 주입 검증
            StringAssert.DoesNotContain("EnsureComponent<BattleOutcomeDebugUI>", source); // 삭제한 중복 승패 UI 재주입 차단 검증
        }
    }
}
