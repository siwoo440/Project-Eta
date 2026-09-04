using System.Reflection; // ScriptableObject의 private 직렬화 필드를 테스트에서 직접 채우기 위한 네임스페이스
using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEditor; // SerializedObject로 private 직렬화 필드를 테스트용으로 설정하기 위한 네임스페이스
using UnityEngine; // GameObject, ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // BattleHooks, CombatResult, TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, StatusEffectDefinition 등을 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day29BattleHooksTests // 29일차 이동/공격/피해/턴 훅이 실제 전투 흐름에 정확히 연결됐는지 검증하는 테스트 모음
    {
        private static (GameObject Root, BoardInputController Input, RunState RunState, TurnManager TurnManager, BattleHooks Hooks) CreateBoundContext() // AttackExecutionTests와 동일한 패턴에 BattleHooks를 추가한 초기화 도우미
        {
            var root = new GameObject("Day29HooksTestRoot"); // 테스트용 오브젝트 생성
            var boardView = root.AddComponent<BoardView>(); // 보드 뷰 추가
            var boardInput = root.AddComponent<BoardInputController>(); // 입력 컨트롤러 추가
            var runState = new RunState(3); // 실제 전투와 같은 방식의 런 상태 생성
            var turnManager = new TurnManager(); // 실제 전투와 같은 방식의 턴 매니저 생성
            var hooks = new BattleHooks(); // 29일차: 이번 테스트 전투가 사용할 훅 버스

            boardView.Bind(runState.Board); // 보드 뷰에 실제 보드 연결
            boardInput.Bind(runState, boardView, turnManager, hooks); // 입력에 실제 런 상태·턴 매니저·훅 버스 연결

            turnManager.MarkInitialKingPlaced(); // 시작 배치는 킹을 놓아야만 끝나므로 필수 조건을 먼저 충족
            turnManager.TryEndDeploymentTurn(); // 일반 턴의 이동·공격을 검증하므로 시작 배치 턴을 명시적으로 종료해 PlayerTurn에서 시작

            return (root, boardInput, runState, turnManager, hooks); // 테스트에서 바로 쓸 수 있도록 묶어서 반환
        }

        private static PieceDefinition CreateDefinition(int baseHp, int baseAtk) // 테스트용 HP·ATK 값을 가진 King형 이동 기물 정의를 만드는 도우미 메서드
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 인스펙터 없이 사용할 임시 기물 정의 생성(기본 이동 타입은 King)
            SetPrivateField(definition, "_baseHp", baseHp); // private 직렬화 필드에 테스트용 HP 직접 대입
            SetPrivateField(definition, "_baseAtk", baseAtk); // private 직렬화 필드에 테스트용 ATK 직접 대입
            return definition; // 완성된 정의 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value) // 리플렉션으로 private 필드 값을 설정하는 공용 도우미 메서드
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance); // 대상 타입에서 지정한 이름의 private 인스턴스 필드 조회
            field.SetValue(target, value); // 조회한 필드에 값 대입
        }

        [Test] // 일반 이동 시 BeforeMove/AfterMove가 정확히 한 번씩, 올바른 순서와 좌표로 발행되는지 검증
        public void MovingPiece_RaisesBeforeMoveThenAfterMove_WithCorrectCoordinates()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var origin = new Vector2Int(4, 1); // 이동할 기물의 시작 좌표
                var destination = new Vector2Int(4, 2); // King형 이동으로 도달 가능한 인접 좌표
                var piece = new PieceRuntimeState(CreateDefinition(baseHp: 3, baseAtk: 1), origin, isPlayerPiece: true); // 테스트용 아군 기물
                context.RunState.Board.GetTile(origin).OccupyingPiece = piece; // 보드에 배치

                int beforeCount = 0; // BeforeMove 발행 횟수
                int afterCount = 0; // AfterMove 발행 횟수
                Vector2Int beforeOrigin = default, beforeDestination = default; // BeforeMove 시점 인자 기록
                Vector2Int afterOrigin = default, afterDestination = default; // AfterMove 시점 인자 기록

                context.Hooks.BeforeMove += (p, o, d) => // BeforeMove 구독
                {
                    beforeCount++; // 발행 횟수 누적
                    beforeOrigin = o; // 인자 기록
                    beforeDestination = d; // 인자 기록
                    Assert.AreEqual(origin, p.BoardPosition, "BeforeMove 시점에는 아직 좌표가 바뀌지 않아야 합니다."); // 이동 전 상태 확인
                };
                context.Hooks.AfterMove += (p, o, d) => // AfterMove 구독
                {
                    afterCount++; // 발행 횟수 누적
                    afterOrigin = o; // 인자 기록
                    afterDestination = d; // 인자 기록
                    Assert.AreEqual(destination, p.BoardPosition, "AfterMove 시점에는 이미 좌표가 바뀌어 있어야 합니다."); // 이동 후 상태 확인
                };

                context.Input.TrySelectPieceAt(origin); // 기물 선택
                bool moved = context.Input.TryMoveSelectedPieceTo(destination); // 이동 실행

                Assert.IsTrue(moved); // 이동이 성공해야 함
                Assert.AreEqual(1, beforeCount, "BeforeMove는 정확히 한 번만 발행되어야 합니다."); // 중복 발행 방지 확인
                Assert.AreEqual(1, afterCount, "AfterMove는 정확히 한 번만 발행되어야 합니다."); // 중복 발행 방지 확인
                Assert.AreEqual(origin, beforeOrigin); // BeforeMove 인자 검증
                Assert.AreEqual(destination, beforeDestination); // BeforeMove 인자 검증
                Assert.AreEqual(origin, afterOrigin); // AfterMove 인자 검증
                Assert.AreEqual(destination, afterDestination); // AfterMove 인자 검증
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 공격 시 BeforeAttack/AfterAttack이 순서대로 발행되고 AfterAttack의 결과가 실제 판정과 일치하는지 검증
        public void AttackingPiece_RaisesBeforeAttackThenAfterAttack_WithMatchingResult()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var attackerOrigin = new Vector2Int(4, 1); // 공격자 시작 좌표
                var defenderOrigin = new Vector2Int(4, 2); // 대상 좌표(공격자와 인접)
                var attacker = new PieceRuntimeState(CreateDefinition(baseHp: 3, baseAtk: 2), attackerOrigin, isPlayerPiece: true); // 아군 공격자
                var defender = new PieceRuntimeState(CreateDefinition(baseHp: 5, baseAtk: 0), defenderOrigin, isPlayerPiece: false); // 적 대상(2 피해로는 생존)
                context.RunState.Board.GetTile(attackerOrigin).OccupyingPiece = attacker; // 보드에 공격자 배치
                context.RunState.Board.GetTile(defenderOrigin).OccupyingPiece = defender; // 보드에 대상 배치

                bool beforeFired = false, afterFired = false; // 발행 여부 플래그
                CombatResult afterResult = null; // AfterAttack으로 전달된 결과

                context.Hooks.BeforeAttack += (a, d) => // BeforeAttack 구독
                {
                    beforeFired = true; // 발행 확인
                    Assert.AreEqual(5, d.CurrentHp, "BeforeAttack 시점에는 아직 피해가 적용되지 않아야 합니다."); // 피해 전 상태 확인
                };
                context.Hooks.AfterAttack += result => // AfterAttack 구독
                {
                    afterFired = true; // 발행 확인
                    afterResult = result; // 결과 기록
                };

                context.Input.TrySelectPieceAt(attackerOrigin); // 공격자 선택
                bool attacked = context.Input.TryAttackSelectedPieceTarget(defenderOrigin); // 공격 실행

                Assert.IsTrue(attacked); // 공격이 실행돼야 함
                Assert.IsTrue(beforeFired, "BeforeAttack이 발행되어야 합니다."); // 사전 훅 발행 확인
                Assert.IsTrue(afterFired, "AfterAttack이 발행되어야 합니다."); // 사후 훅 발행 확인
                Assert.IsNotNull(afterResult); // 결과가 실제로 전달되어야 함
                Assert.AreEqual(2, afterResult.DamageDealt); // 전달된 결과가 실제 판정과 일치해야 함
                Assert.AreEqual(3, defender.CurrentHp); // 5 - 2 = 3
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // BeforeDamage 구독자가 피해량을 0으로 줄이면(가짜 보호막) 실제 HP가 깎이지 않는지 검증
        public void BeforeDamageHook_CanInterceptAndReduceIncomingDamage()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var attackerOrigin = new Vector2Int(4, 1); // 공격자 시작 좌표
                var defenderOrigin = new Vector2Int(4, 2); // 대상 좌표(공격자와 인접)
                var attacker = new PieceRuntimeState(CreateDefinition(baseHp: 3, baseAtk: 2), attackerOrigin, isPlayerPiece: true); // 아군 공격자
                var defender = new PieceRuntimeState(CreateDefinition(baseHp: 5, baseAtk: 0), defenderOrigin, isPlayerPiece: false); // 적 대상
                context.RunState.Board.GetTile(attackerOrigin).OccupyingPiece = attacker; // 보드에 공격자 배치
                context.RunState.Board.GetTile(defenderOrigin).OccupyingPiece = defender; // 보드에 대상 배치

                context.Hooks.BeforeDamage += ctx => ctx.Amount = 0; // 가짜 보호막: 들어오는 모든 피해를 0으로 무효화

                context.Input.TrySelectPieceAt(attackerOrigin); // 공격자 선택
                bool attacked = context.Input.TryAttackSelectedPieceTarget(defenderOrigin); // 공격 실행

                Assert.IsTrue(attacked); // 공격 자체는 정상적으로 실행돼야 함
                Assert.AreEqual(5, defender.CurrentHp, "BeforeDamage가 피해를 0으로 줄였으면 HP가 그대로여야 합니다."); // 피해 무효화 확인
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // TurnEnd 훅을 직접 발행하면 28일차 상태 이상 정산(ApplyTurnEndStatusEffects)이 실제로 구독·실행되는지 검증
        public void RaisingTurnEnd_TriggersSubscribedStatusEffectResolution()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var position = new Vector2Int(4, 1); // 상태를 걸어둘 기물 좌표
                var piece = new PieceRuntimeState(CreateDefinition(baseHp: 10, baseAtk: 0), position, isPlayerPiece: true); // 체력 10짜리 테스트용 기물
                context.RunState.Board.GetTile(position).OccupyingPiece = piece; // 보드에 배치

                var poison = ScriptableObject.CreateInstance<StatusEffectDefinition>(); // 테스트용 독 정의
                var serialized = new SerializedObject(poison); // private 필드 설정용
                serialized.FindProperty("_statusType").intValue = (int)StatusEffectType.Poison; // 독으로 설정
                serialized.FindProperty("_stackMode").enumValueIndex = (int)StatusStackMode.StacksAdd; // 중첩형
                serialized.FindProperty("_maxStacks").intValue = 3; // 최대 3중첩
                serialized.FindProperty("_defaultDurationTurns").intValue = 3; // 지속 3턴
                serialized.FindProperty("_tickDamagePerStack").intValue = 1; // 중첩당 1피해(임시값)
                serialized.ApplyModifiedPropertiesWithoutUndo(); // 즉시 반영

                piece.ApplyStatus(poison); // 독 1중첩 적용

                context.Hooks.RaiseTurnEnd(TurnState.EnemyTurn, context.TurnManager.TurnNumber); // 29일차: BattleController 대신 직접 훅 발행

                Assert.AreEqual(9, piece.CurrentHp, "TurnEnd 훅 발행만으로도 28일차 독 틱 피해가 실제로 적용되어야 합니다."); // 구독을 통한 정산 확인
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }
    }
}
