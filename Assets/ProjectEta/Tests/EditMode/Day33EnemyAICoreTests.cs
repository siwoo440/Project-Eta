using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using System.Reflection; // 테스트용 PieceDefinition private 필드 주입에 사용하는 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject와 Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.AI; // 33일차 AI 코어 타입을 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager와 BattleHooks를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementRuleData를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day33EnemyAICoreTests // 33일차 공통 적 AI 후보 생성·평가·실행을 검증하는 테스트 모음
    {
        [Test] // 플레이어 기물이 AI 행동 주체로 섞이지 않는지 검증
        public void Planner_GeneratesCandidatesOnlyForEnemyPieces()
        {
            var board = new BoardState(); // 빈 10x10 보드 생성
            var playerRook = Place(board, CreateDefinition("player_rook", PieceMovementType.Rook, 3, 2), new Vector2Int(2, 2), true); // 아군 룩 배치
            var enemyRook = Place(board, CreateDefinition("enemy_rook", PieceMovementType.Rook, 3, 2), new Vector2Int(7, 7), false); // 적 룩 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, 3, 1), new Vector2Int(4, 2), true); // AI 목표인 아군 킹 배치

            var planner = new EnemyAIPlanner(); // 공통 AI 플래너 생성
            var candidates = planner.BuildCandidates(board); // 현재 보드의 모든 적 행동 후보 생성

            Assert.Greater(candidates.Count, 0); // 적 룩은 최소 하나 이상의 행동 후보를 가져야 함
            foreach (var candidate in candidates) // 모든 후보를 순회
            {
                Assert.AreSame(enemyRook, candidate.Actor); // 행동 주체는 적 룩만 가능
                Assert.AreNotSame(playerRook, candidate.Actor); // 아군 룩은 절대 AI 후보 주체가 되면 안 됨
            }
        }

        [Test] // 공격 가능한 상황에서 단순 이동보다 공격을 우선하는지 검증
        public void Planner_PrefersImmediateAttackOverMove()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemyRook = Place(board, CreateDefinition("enemy_rook", PieceMovementType.Rook, 3, 2), new Vector2Int(4, 8), false); // 적 룩 배치
            var playerPawn = Place(board, CreateDefinition("pawn", PieceMovementType.Pawn, 2, 1), new Vector2Int(4, 5), true); // 같은 열에 공격 가능한 아군 폰 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, 3, 1), new Vector2Int(1, 1), true); // 멀리 떨어진 아군 킹 배치

            var planner = new EnemyAIPlanner(); // 플래너 생성
            bool selected = planner.TryChooseAction(board, out var action); // 최고 점수 행동 선택

            Assert.IsTrue(selected); // 행동 선택에 성공해야 함
            Assert.AreEqual(AIActionType.Attack, action.ActionType); // 공격 가능한 상황에서는 공격을 우선
            Assert.AreSame(enemyRook, action.Actor); // 적 룩이 행동 주체
            Assert.AreSame(playerPawn, action.TargetPiece); // 공격 대상은 같은 열의 아군 폰
            Assert.AreEqual(new Vector2Int(4, 5), action.Target); // 실제 공격 대상 좌표 확인
        }

        [Test] // 킹을 직접 공격할 수 있으면 일반 기물 공격보다 더 높은 우선순위를 주는지 검증
        public void Planner_PrioritizesDirectKingAttack()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemyRookA = Place(board, CreateDefinition("enemy_rook_a", PieceMovementType.Rook, 3, 2), new Vector2Int(4, 8), false); // 킹 라인 적 룩
            Place(board, CreateDefinition("king", PieceMovementType.King, 3, 1), new Vector2Int(4, 2), true); // 킹을 같은 열에 배치
            Place(board, CreateDefinition("pawn", PieceMovementType.Pawn, 2, 1), new Vector2Int(7, 5), true); // 다른 적이 공격할 일반 기물
            Place(board, CreateDefinition("enemy_rook_b", PieceMovementType.Rook, 3, 2), new Vector2Int(7, 8), false); // 일반 기물을 공격할 두 번째 적 룩

            var planner = new EnemyAIPlanner(); // 플래너 생성
            bool selected = planner.TryChooseAction(board, out var action); // 행동 선택

            Assert.IsTrue(selected); // 행동 선택 성공
            Assert.AreEqual(AIActionType.Attack, action.ActionType); // 킹 공격도 공격 행동
            Assert.AreSame(enemyRookA, action.Actor); // 킹 라인의 룩이 선택돼야 함
            Assert.IsNotNull(action.TargetPiece); // 공격 대상 존재 확인
            Assert.AreEqual("king", action.TargetPiece.Definition.PieceId); // 일반 기물보다 킹 공격을 우선
        }

        [Test] // 기절처럼 이동·공격 권한이 모두 없는 적은 후보를 만들지 않는지 검증
        public void Planner_SkipsEnemyWithNoActionPermission()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemyRook = Place(board, CreateDefinition("enemy_rook", PieceMovementType.Rook, 3, 2), new Vector2Int(4, 8), false); // 적 룩 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, 3, 1), new Vector2Int(4, 2), true); // 아군 킹 배치
            enemyRook.CanMove = false; // 상태 이상처럼 이동 금지
            enemyRook.CanAttack = false; // 상태 이상처럼 공격 금지

            var planner = new EnemyAIPlanner(); // 플래너 생성
            var candidates = planner.BuildCandidates(board); // 후보 생성

            Assert.AreEqual(0, candidates.Count); // 행동 권한이 없으면 후보가 없어야 함
            Assert.IsFalse(planner.TryChooseAction(board, out _)); // 선택 가능한 행동도 없어야 함
        }

        [Test] // 동일한 보드에서 매번 같은 행동을 선택하는 결정론적 동점 규칙을 검증
        public void Planner_IsDeterministicForSameBoardState()
        {
            var board = new BoardState(); // 빈 보드 생성
            Place(board, CreateDefinition("enemy_knight_a", PieceMovementType.Knight, 3, 2), new Vector2Int(2, 7), false); // 첫 적 나이트
            Place(board, CreateDefinition("enemy_knight_b", PieceMovementType.Knight, 3, 2), new Vector2Int(7, 7), false); // 둘째 적 나이트
            Place(board, CreateDefinition("king", PieceMovementType.King, 3, 1), new Vector2Int(4, 1), true); // 중앙 아군 킹

            var planner = new EnemyAIPlanner(); // 동일 플래너 사용

            Assert.IsTrue(planner.TryChooseAction(board, out var first)); // 첫 선택
            Assert.IsTrue(planner.TryChooseAction(board, out var second)); // 같은 상태에서 두 번째 선택

            Assert.AreSame(first.Actor, second.Actor); // 행동 주체 동일
            Assert.AreEqual(first.ActionType, second.ActionType); // 행동 종류 동일
            Assert.AreEqual(first.Target, second.Target); // 목표 좌표 동일
            Assert.AreEqual(first.Score, second.Score); // 점수도 동일
        }

        [Test] // 적 이동 실행이 보드 점유와 턴 상태를 함께 갱신하는지 검증
        public void Executor_MoveUpdatesBoardAndCompletesEnemyTurn()
        {
            var run = new RunState(3); // 실제 런 상태 생성
            var enemyKnight = Place(run.Board, CreateDefinition("enemy_knight", PieceMovementType.Knight, 3, 2), new Vector2Int(4, 7), false); // 적 나이트 배치
            Place(run.Board, CreateDefinition("king", PieceMovementType.King, 3, 1), new Vector2Int(4, 1), true); // 아군 킹 배치
            var turnManager = CreateEnemyTurnManager(); // EnemyTurn 상태의 턴 매니저 준비
            var hooks = new BattleHooks(); // 실제 전투 훅 버스 생성
            var planner = new EnemyAIPlanner(); // 플래너 생성

            Assert.IsTrue(planner.TryChooseAction(run.Board, out var action)); // 적 행동 하나 선택
            Assert.AreEqual(AIActionType.Move, action.ActionType); // 현재 배치에서는 공격보다 이동 행동이어야 함

            bool executed = EnemyAIActionExecutor.TryExecute(action, run, turnManager, hooks, null, out var combatResult); // 화면 뷰 없이 순수 보드 실행

            Assert.IsTrue(executed); // 행동 실행 성공
            Assert.IsNull(combatResult); // 이동은 CombatResult가 없음
            Assert.IsNull(run.Board.GetTile(new Vector2Int(4, 7)).OccupyingPiece); // 원래 칸은 비어야 함
            Assert.AreSame(enemyKnight, run.Board.GetTile(action.Target).OccupyingPiece); // 목표 칸에 적 나이트가 있어야 함
            Assert.AreEqual(action.Target, enemyKnight.BoardPosition); // 런타임 좌표도 동일해야 함
            Assert.AreEqual(TurnState.PlayerTurn, turnManager.CurrentState); // 적 행동 후 다음 플레이어 턴으로 넘어가야 함
        }

        [Test] // 적이 킹을 처치하면 전투가 즉시 패배로 끝나는지 검증
        public void Executor_KingKillEndsBattleAsDefeat()
        {
            var run = new RunState(3); // 실제 런 상태 생성
            Place(run.Board, CreateDefinition("enemy_rook", PieceMovementType.Rook, 3, 5), new Vector2Int(4, 8), false); // 킹을 한 번에 처치 가능한 적 룩
            var playerKing = Place(run.Board, CreateDefinition("king", PieceMovementType.King, 3, 1), new Vector2Int(4, 2), true); // HP 3 아군 킹
            var turnManager = CreateEnemyTurnManager(); // EnemyTurn 상태 준비
            var hooks = new BattleHooks(); // 전투 훅 버스 생성
            var planner = new EnemyAIPlanner(); // 플래너 생성

            Assert.IsTrue(planner.TryChooseAction(run.Board, out var action)); // 킹 공격 행동 선택
            Assert.AreEqual(AIActionType.Attack, action.ActionType); // 공격 행동 확인
            Assert.AreSame(playerKing, action.TargetPiece); // 대상이 킹인지 확인

            bool executed = EnemyAIActionExecutor.TryExecute(action, run, turnManager, hooks, null, out var combatResult); // 실제 공격 실행

            Assert.IsTrue(executed); // 실행 성공
            Assert.IsNotNull(combatResult); // 공격 결과 존재
            Assert.IsTrue(combatResult.DefenderDied); // 킹이 사망해야 함
            Assert.AreEqual(0, run.KingHp); // RunState 킹 HP도 0으로 동기화
            Assert.AreEqual(TurnState.BattleEnded, turnManager.CurrentState); // 전투 종료 상태
            Assert.AreEqual(BattleOutcome.Defeat, turnManager.Outcome); // 패배 결과
        }

        [Test] // 적 기물이 하나도 없으면 안전하게 행동 없음으로 처리하는지 검증
        public void Planner_ReturnsFalseWhenNoEnemyActionExists()
        {
            var board = new BoardState(); // 빈 보드 생성
            Place(board, CreateDefinition("king", PieceMovementType.King, 3, 1), new Vector2Int(4, 2), true); // 아군 킹만 배치

            var planner = new EnemyAIPlanner(); // 플래너 생성

            Assert.IsFalse(planner.TryChooseAction(board, out var action)); // 선택할 적 행동이 없어야 함
            Assert.IsNull(action); // 반환 후보도 null이어야 함
        }

        private static TurnManager CreateEnemyTurnManager() // EnemyTurn 상태를 만드는 공통 도우미
        {
            var turnManager = new TurnManager(); // 시작 배치 턴 상태로 생성
            turnManager.MarkInitialKingPlaced(); // 시작 킹 배치 조건을 만족시킴
            Assert.IsTrue(turnManager.TryEndDeploymentTurn()); // 첫 PlayerTurn 진입
            Assert.IsTrue(turnManager.TryCompletePlayerAction()); // 플레이어 행동 완료로 EnemyTurn 진입
            Assert.AreEqual(TurnState.EnemyTurn, turnManager.CurrentState); // 테스트 전제 확인
            return turnManager; // 준비된 턴 매니저 반환
        }

        private static PieceRuntimeState Place(BoardState board, PieceDefinition definition, Vector2Int position, bool isPlayerPiece) // 테스트용 기물 배치 도우미
        {
            var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 런타임 기물 생성
            board.GetTile(position).OccupyingPiece = piece; // 보드 타일에 실제 점유 등록
            return piece; // 생성한 기물 반환
        }

        private static PieceDefinition CreateDefinition(string pieceId, PieceMovementType movementType, int hp, int atk) // 테스트용 PieceDefinition 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 메모리 안에 임시 ScriptableObject 생성
            SetPrivateField(definition, "_pieceId", pieceId); // 고유 id 설정
            SetPrivateField(definition, "_displayName", pieceId); // 표시 이름 설정
            SetPrivateField(definition, "_movementType", movementType); // Legacy 이동 타입 설정
            SetPrivateField(definition, "_roleTags", PieceRoleTag.Attacker); // 기본 공격 역할 태그 설정
            SetPrivateField(definition, "_baseHp", hp); // 기본 HP 설정
            SetPrivateField(definition, "_baseAtk", atk); // 기본 ATK 설정
            SetPrivateField(definition, "_occupancySize", Vector2Int.one); // 1x1 점유 설정
            SetPrivateField(definition, "_movementRules", Array.Empty<MovementRuleData>()); // 데이터 규칙을 비워 Legacy 이동 규칙을 사용
            return definition; // 완성 정의 반환
        }

        private static void SetPrivateField<T>(PieceDefinition target, string fieldName, T value) // private 직렬화 필드 주입 공통 도우미
        {
            var field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 지정 이름의 private 필드 탐색
            Assert.IsNotNull(field, $"PieceDefinition.{fieldName} 필드를 찾을 수 있어야 합니다."); // 프로젝트 구조 변경 시 명확히 실패
            field.SetValue(target, value); // 테스트 값 주입
        }
    }
}
