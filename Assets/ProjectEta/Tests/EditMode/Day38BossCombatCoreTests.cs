using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using System.Reflection; // PieceDefinition private 필드 주입에 사용하는 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject와 Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // BattleHooks, TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementRuleData를 사용하기 위한 네임스페이스
using ProjectEta.Boss; // 38일차 보스 행동 후보·플래너·실행기를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day38BossCombatCoreTests // 38일차 2x2 보스 기본 전투를 검증하는 테스트 모음
    {
        [Test] // 2x2 외곽 어느 쪽에 플레이어가 있어도 공격 후보가 생성되는지 검증
        public void Planner_FindsAdjacentTargetAroundWholeTwoByTwoFootprint()
        {
            var board = new BoardState(); // 빈 보드 생성
            var boss = CreateAndPlaceBoss(board, new Vector2Int(4, 6)); // 중앙 부근에 2x2 보스 배치
            var player = Place(board, CreateDefinition("pawn", Vector2Int.one, PieceMovementType.Pawn, PieceCategory.Basic, 8, 1), new Vector2Int(6, 7), true); // 보스 오른쪽 외곽에 플레이어 배치

            var planner = new BossActionPlanner(); // 보스 행동 플래너 생성
            var candidates = planner.BuildCandidates(board); // 현재 보드 행동 후보 생성

            bool foundAttack = false; // 대상 공격 후보 발견 여부
            for (int i = 0; i < candidates.Count; i++) // 모든 후보 순회
            {
                if (candidates[i].Actor == boss && candidates[i].ActionType == BossActionType.Attack && candidates[i].TargetPiece == player) // 같은 보스가 오른쪽 외곽 플레이어를 공격한다면
                {
                    foundAttack = true; // 공격 후보 발견
                    break; // 더 볼 필요 없음
                }
            }

            Assert.IsTrue(foundAttack); // 기준 Anchor 한 칸이 아니라 2x2 전체 외곽을 공격 범위로 봐야 함
        }

        [Test] // 플레이어 King이 보스 외곽에 있으면 다른 일반 기물보다 우선 공격하는지 검증
        public void Planner_PrioritizesAdjacentKing()
        {
            var board = new BoardState(); // 빈 보드 생성
            var boss = CreateAndPlaceBoss(board, new Vector2Int(4, 6)); // 2x2 보스 배치
            var pawn = Place(board, CreateDefinition("pawn", Vector2Int.one, PieceMovementType.Pawn, PieceCategory.Basic, 8, 1), new Vector2Int(3, 6), true); // 보스 왼쪽에 일반 플레이어
            var king = Place(board, CreateDefinition("king", Vector2Int.one, PieceMovementType.King, PieceCategory.Basic, 12, 1), new Vector2Int(6, 7), true); // 보스 오른쪽에 King

            var planner = new BossActionPlanner(); // 보스 행동 플래너 생성
            bool selected = planner.TryChooseAction(board, out var action); // 최고 행동 선택

            Assert.IsTrue(selected); // 행동이 있어야 함
            Assert.AreEqual(BossActionType.Attack, action.ActionType); // 인접 플레이어가 있으므로 공격이어야 함
            Assert.AreSame(boss, action.Actor); // 보스가 행동 주체여야 함
            Assert.AreSame(king, action.TargetPiece); // 일반 Pawn보다 King을 우선해야 함
            Assert.AreNotSame(pawn, action.TargetPiece); // 낮은 우선순위 Pawn은 선택되지 않아야 함
        }

        [Test] // 공격할 대상이 없으면 King과 가까워지는 유효한 2x2 이동을 선택하는지 검증
        public void Planner_MovesTwoByTwoBossTowardKingWhenNoAttackExists()
        {
            var board = new BoardState(); // 빈 보드 생성
            var boss = CreateAndPlaceBoss(board, new Vector2Int(1, 7)); // 왼쪽 위에 보스 배치
            Place(board, CreateDefinition("king", Vector2Int.one, PieceMovementType.King, PieceCategory.Basic, 12, 1), new Vector2Int(8, 1), true); // 멀리 오른쪽 아래에 플레이어 King 배치

            var planner = new BossActionPlanner(); // 보스 플래너 생성
            bool selected = planner.TryChooseAction(board, out var action); // 최고 행동 선택

            Assert.IsTrue(selected); // 이동 후보가 있어야 함
            Assert.AreEqual(BossActionType.Move, action.ActionType); // 인접 대상이 없으므로 이동이어야 함
            Assert.AreSame(boss, action.Actor); // 보스가 이동 주체여야 함

            int beforeDistance = BossActionPlanner.DistanceFromFootprintToCell(boss.BoardPosition, boss.Definition.OccupancySize, new Vector2Int(8, 1)); // 이동 전 King 거리
            int afterDistance = BossActionPlanner.DistanceFromFootprintToCell(action.Target, boss.Definition.OccupancySize, new Vector2Int(8, 1)); // 이동 후 King 거리

            Assert.Less(afterDistance, beforeDistance); // 선택 이동은 King과 거리를 실제로 줄여야 함
        }

        [Test] // 이동하려는 2x2 영역에 다른 기물이 있으면 해당 방향 후보가 생성되지 않는지 검증
        public void Planner_DoesNotMoveIntoBlockedTwoByTwoFootprint()
        {
            var board = new BoardState(); // 빈 보드 생성
            var boss = CreateAndPlaceBoss(board, new Vector2Int(3, 6)); // 2x2 보스 배치
            Place(board, CreateDefinition("blocker", Vector2Int.one, PieceMovementType.Pawn, PieceCategory.Basic, 5, 1), new Vector2Int(5, 6), true); // 오른쪽 이동 시 새 점유 영역에 걸리는 방해 기물 배치
            Place(board, CreateDefinition("king", Vector2Int.one, PieceMovementType.King, PieceCategory.Basic, 12, 1), new Vector2Int(9, 1), true); // 이동 목표용 King 배치

            var planner = new BossActionPlanner(); // 보스 플래너 생성
            var candidates = planner.BuildCandidates(board); // 후보 생성

            for (int i = 0; i < candidates.Count; i++) // 후보 순회
            {
                bool illegalRightMove = candidates[i].Actor == boss // 같은 보스의 후보이고
                    && candidates[i].ActionType == BossActionType.Move // 이동 행동이며
                    && candidates[i].Target == new Vector2Int(4, 6); // 오른쪽 한 칸 이동이라면
                Assert.IsFalse(illegalRightMove); // 방해 기물 때문에 이 후보는 존재하면 안 됨
            }
        }

        [Test] // 보스 이동 실행 시 기존 4칸을 비우고 새 위치 4칸 전체를 같은 런타임 기물로 점유하는지 검증
        public void Executor_MoveRelocatesWholeFootprintAndCompletesEnemyTurn()
        {
            var runState = new RunState(3); // 테스트용 런 상태 생성
            var board = runState.Board; // 런 상태 보드 참조
            var boss = CreateAndPlaceBoss(board, new Vector2Int(2, 7)); // 보스 배치
            var turnManager = CreateEnemyTurnManager(); // 실제 EnemyTurn 상태의 턴 매니저 생성
            var hooks = new BattleHooks(); // 전투 훅 버스 생성
            var action = new BossActionCandidate(boss, BossActionType.Move, boss.BoardPosition, new Vector2Int(3, 7), null, 1000); // 오른쪽 이동 후보 생성

            bool executed = BossActionExecutor.TryExecute(action, runState, turnManager, hooks, null, out var combatResult); // 화면 뷰 없이 논리 이동 실행

            Assert.IsTrue(executed); // 이동 실행에 성공해야 함
            Assert.IsNull(combatResult); // 이동에는 CombatResult가 없음
            Assert.AreEqual(new Vector2Int(3, 7), boss.BoardPosition); // 기준 좌표가 새 Anchor로 이동해야 함
            Assert.IsNull(board.GetTile(new Vector2Int(2, 7)).OccupyingPiece); // 이전 영역의 왼쪽 아래 칸은 비어야 함
            Assert.IsNull(board.GetTile(new Vector2Int(2, 8)).OccupyingPiece); // 이전 영역의 왼쪽 위 칸도 비어야 함
            Assert.AreSame(boss, board.GetTile(new Vector2Int(3, 7)).OccupyingPiece); // 새 좌하단 점유 확인
            Assert.AreSame(boss, board.GetTile(new Vector2Int(4, 7)).OccupyingPiece); // 새 우하단 점유 확인
            Assert.AreSame(boss, board.GetTile(new Vector2Int(3, 8)).OccupyingPiece); // 새 좌상단 점유 확인
            Assert.AreSame(boss, board.GetTile(new Vector2Int(4, 8)).OccupyingPiece); // 새 우상단 점유 확인
            Assert.AreNotEqual(TurnState.EnemyTurn, turnManager.CurrentState); // 행동 후 적 턴이 정상 종료되어야 함
        }

        [Test] // 보스 공격이 기존 CombatResolver를 통해 실제 HP를 감소시키고 적 턴을 종료하는지 검증
        public void Executor_AttackUsesCombatResolverAndCompletesEnemyTurn()
        {
            var runState = new RunState(3); // 테스트용 런 상태 생성
            var board = runState.Board; // 현재 보드 참조
            var boss = CreateAndPlaceBoss(board, new Vector2Int(4, 6)); // 2x2 보스 배치
            var target = Place(board, CreateDefinition("pawn", Vector2Int.one, PieceMovementType.Pawn, PieceCategory.Basic, 10, 1), new Vector2Int(6, 6), true); // 오른쪽 외곽에 HP 10 플레이어 배치
            var turnManager = CreateEnemyTurnManager(); // EnemyTurn 상태 생성
            var hooks = new BattleHooks(); // 전투 훅 생성
            int beforeHp = target.CurrentHp; // 공격 전 HP 저장
            var action = new BossActionCandidate(boss, BossActionType.Attack, boss.BoardPosition, target.BoardPosition, target, 3000); // 공격 후보 생성

            bool executed = BossActionExecutor.TryExecute(action, runState, turnManager, hooks, null, out var combatResult); // 기존 전투 판정으로 공격 실행

            Assert.IsTrue(executed); // 공격이 실행되어야 함
            Assert.IsNotNull(combatResult); // 공격 결과가 반환되어야 함
            Assert.AreEqual(beforeHp - boss.Definition.BaseAtk, target.CurrentHp); // 고정 ATK만큼 HP가 감소해야 함
            Assert.AreNotEqual(TurnState.EnemyTurn, turnManager.CurrentState); // 공격 후 적 턴이 끝나야 함
        }

        [Test] // 2x2 네 칸을 스캔해도 보스 행동 후보가 동일 목표 기준으로 중복 생성되지 않는지 검증
        public void Planner_DoesNotDuplicateBossActionsFromFourOccupiedCells()
        {
            var board = new BoardState(); // 빈 보드 생성
            var boss = CreateAndPlaceBoss(board, new Vector2Int(4, 6)); // 2x2 보스 배치
            Place(board, CreateDefinition("king", Vector2Int.one, PieceMovementType.King, PieceCategory.Basic, 12, 1), new Vector2Int(9, 1), true); // 이동 평가용 King 배치

            var candidates = new BossActionPlanner().BuildCandidates(board); // 보스 후보 생성
            var keys = new HashSet<string>(); // 중복 검사용 키 집합

            for (int i = 0; i < candidates.Count; i++) // 후보 순회
            {
                if (candidates[i].Actor != boss) continue; // 같은 보스 행동만 확인
                string key = $"{candidates[i].ActionType}:{candidates[i].Target.x}:{candidates[i].Target.y}"; // 행동 종류+목표 좌표 키 생성
                Assert.IsTrue(keys.Add(key), $"중복 보스 행동 후보 발견: {key}"); // 동일 후보가 두 번 나오면 실패
            }
        }

        private static PieceRuntimeState CreateAndPlaceBoss(BoardState board, Vector2Int anchor) // 테스트용 2x2 보스를 만들고 실제 네 칸에 배치하는 도우미
        {
            var definition = CreateDefinition("prototype_boss_37", new Vector2Int(2, 2), PieceMovementType.Custom, PieceCategory.Boss, 30, 4); // 37일차 프로토타입과 같은 보스 정의 생성
            var boss = new PieceRuntimeState(definition, anchor, false); // 적 보스 런타임 상태 생성
            Assert.IsTrue(LargePieceBoardUtility.TryPlace(board, boss, anchor)); // 네 칸 전체 점유
            return boss; // 완성된 보스 반환
        }

        private static PieceRuntimeState Place(BoardState board, PieceDefinition definition, Vector2Int position, bool isPlayerPiece) // 일반 테스트 기물을 한 칸에 배치하는 도우미
        {
            var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 런타임 기물 생성
            board.GetTile(position).OccupyingPiece = piece; // 실제 타일 점유
            return piece; // 생성 기물 반환
        }

        private static TurnManager CreateEnemyTurnManager() // 테스트용 TurnManager를 정상 API만으로 EnemyTurn까지 진행시키는 도우미
        {
            var manager = new TurnManager(); // 시작 배치 상태의 턴 매니저 생성
            manager.MarkInitialKingPlaced(); // 시작 배치 킹 조건 충족
            Assert.IsTrue(manager.TryEndDeploymentTurn()); // 첫 PlayerTurn 진입
            Assert.IsTrue(manager.TryCompletePlayerAction()); // 플레이어 행동 완료 후 EnemyTurn 진입
            Assert.AreEqual(TurnState.EnemyTurn, manager.CurrentState); // 실제 적 턴 상태 확인
            return manager; // 준비된 턴 매니저 반환
        }

        private static PieceDefinition CreateDefinition(string pieceId, Vector2Int occupancySize, PieceMovementType movementType, PieceCategory category, int hp, int atk) // 테스트용 PieceDefinition 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 임시 기물 정의 생성
            SetPrivateField(definition, "_pieceId", pieceId); // PieceId 설정
            SetPrivateField(definition, "_displayName", pieceId); // 표시 이름 설정
            SetPrivateField(definition, "_category", category); // 분류 설정
            SetPrivateField(definition, "_grade", PieceGrade.FiveStar); // 테스트 편의를 위해 5성 설정
            SetPrivateField(definition, "_movementType", movementType); // Legacy 이동 타입 설정
            SetPrivateField(definition, "_roleTags", PieceRoleTag.Tanker | PieceRoleTag.Attacker); // 보스/테스트 역할 태그 설정
            SetPrivateField(definition, "_immuneStatusTags", StatusEffectType.None); // 테스트에서는 면역 없음
            SetPrivateField(definition, "_movementRules", Array.Empty<MovementRuleData>()); // 보스는 MovementResolver가 아닌 BossActionPlanner가 이동 처리
            SetPrivateField(definition, "_baseHp", hp); // HP 설정
            SetPrivateField(definition, "_baseAtk", atk); // ATK 설정
            SetPrivateField(definition, "_occupancySize", occupancySize); // 점유 크기 설정
            return definition; // 완성 정의 반환
        }

        private static void SetPrivateField<T>(PieceDefinition target, string fieldName, T value) // private 직렬화 필드를 테스트 값으로 채우는 도우미
        {
            var field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 지정 필드 탐색
            Assert.IsNotNull(field, $"PieceDefinition.{fieldName} 필드를 찾을 수 있어야 합니다."); // 구조 변경 시 명확한 실패 메시지 제공
            field.SetValue(target, value); // 테스트 값 적용
        }
    }
}
