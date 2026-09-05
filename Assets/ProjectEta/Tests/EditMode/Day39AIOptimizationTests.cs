using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using System.Reflection; // PieceDefinition private 필드 주입에 사용하는 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject와 Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.AI; // 39일차 AI 최적화 타입을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementRuleData를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day39AIOptimizationTests // Lazy Threat·공유 캐시·후보 예산·디버그 재사용을 검증하는 39일차 테스트
    {
        [Test] // ThreatMap.Build 자체는 100칸 전체를 미리 계산하지 않는지 검증
        public void ThreatMap_IsLazyAndCachesRepeatedPositionProbe()
        {
            var board = new BoardState(); // 빈 보드 생성
            Place(board, CreateDefinition("player_rook", PieceMovementType.Rook, PieceCategory.Basic, PieceRoleTag.Slider), new Vector2Int(4, 4), true); // 플레이어 Rook 배치
            var threatMap = EnemyAIThreatMap.Build(board); // Lazy 위협 맵 생성

            Assert.AreEqual(0, threatMap.ProbeCount); // Build 직후에는 어떤 좌표도 실제 계산하지 않아야 함

            int first = threatMap.GetThreatCount(new Vector2Int(4, 8)); // 실제 필요한 좌표를 처음 요청
            int second = threatMap.GetThreatCount(new Vector2Int(4, 8)); // 같은 좌표를 다시 요청

            Assert.Greater(first, 0); // Rook 직선상 빈 칸은 기존 규칙처럼 위협 칸이어야 함
            Assert.AreEqual(first, second); // 캐시 전후 판정값은 같아야 함
            Assert.AreEqual(1, threatMap.ProbeCount); // 같은 좌표를 두 번 요청해도 실제 좌표 계산은 한 번만 수행해야 함
        }

        [Test] // 같은 후보의 미래 이동 범위를 여러 평가 계층이 요구해도 실제 계산은 한 번만 수행하는지 검증
        public void EvaluationContext_CachesFutureMovementForSameCandidate()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemy = Place(board, CreateDefinition("enemy_rook", PieceMovementType.Rook, PieceCategory.Basic, PieceRoleTag.Slider), new Vector2Int(4, 8), false); // 적 Slider 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 1), true); // 플레이어 King 배치
            var candidate = new AIActionCandidate(enemy, enemy.BoardPosition, new Vector2Int(4, 7), AIActionType.Move, null, 10); // 합법 이동 후보 생성
            var context = new EnemyAIEvaluationContext(board); // 한 번의 AI 평가용 공유 컨텍스트 생성

            Assert.IsTrue(context.TryResolveFutureMovement(candidate, out var first)); // 첫 미래 이동 계산 성공
            Assert.IsTrue(context.TryResolveFutureMovement(candidate, out var second)); // 같은 후보 재요청 성공

            Assert.AreSame(first, second); // 같은 MovementResult 객체를 캐시에서 재사용해야 함
            Assert.AreEqual(1, context.FutureMovementResolveCount); // 실제 MovementResolver 계산은 한 번만 수행해야 함
        }

        [Test] // 예산 상한이 걸려도 즉시 공격을 우선 보존하고 정밀 평가 후보 수를 제한하는지 검증
        public void CandidatePruner_PreservesAttackAndCapsHeavyEvaluationCount()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemy = Place(board, CreateDefinition("enemy_queen", PieceMovementType.Queen, PieceCategory.Basic, PieceRoleTag.Slider), new Vector2Int(4, 8), false); // 적 Queen 배치
            var target = Place(board, CreateDefinition("pawn", PieceMovementType.Pawn, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 5), true); // 플레이어 공격 대상 배치

            var candidates = new List<AIActionCandidate> // 예산보다 많은 정상 후보를 직접 구성
            {
                new AIActionCandidate(enemy, enemy.BoardPosition, target.BoardPosition, AIActionType.Attack, target, 1200), // 즉시 공격 후보
                new AIActionCandidate(enemy, enemy.BoardPosition, new Vector2Int(3, 8), AIActionType.Move, null, 50), // 이동 후보 1
                new AIActionCandidate(enemy, enemy.BoardPosition, new Vector2Int(5, 8), AIActionType.Move, null, 40), // 이동 후보 2
                new AIActionCandidate(enemy, enemy.BoardPosition, new Vector2Int(6, 8), AIActionType.Move, null, 30) // 이동 후보 3
            };

            var pruned = EnemyAICandidatePruner.Prune(board, candidates, 2, out _, out bool budgetCapped); // 정밀 평가 예산을 2로 제한

            Assert.IsTrue(budgetCapped); // 전체 후보가 예산보다 많으므로 상한 적용 표시가 true여야 함
            Assert.AreEqual(2, pruned.Count); // 정밀 평가 후보는 정확히 예산 이하여야 함
            Assert.AreEqual(AIActionType.Attack, pruned[0].ActionType); // 즉시 공격 후보가 이동보다 먼저 보존되어야 함
        }

        [Test] // 작은 예산에서도 AdvancedPlanner가 합법 행동을 하나 반환하고 성능 통계를 남기는지 검증
        public void AdvancedPlanner_RespectsBudgetAndStillChoosesLegalAction()
        {
            var board = new BoardState(); // 빈 보드 생성
            Place(board, CreateDefinition("enemy_queen", PieceMovementType.Queen, PieceCategory.Basic, PieceRoleTag.Slider), new Vector2Int(4, 8), false); // 후보가 많은 적 Queen 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 1), true); // 플레이어 King 배치
            var planner = new EnemyAIAdvancedPlanner(new EnemyAIEvaluationBudget(3)); // 정밀 평가 후보를 3개로 제한한 플래너 생성

            bool chosen = planner.TryChooseAction(board, out var action); // 최적화된 AI 행동 선택

            Assert.IsTrue(chosen); // 합법 행동이 존재하므로 선택 성공해야 함
            Assert.IsNotNull(action); // 실제 행동 후보가 반환되어야 함
            Assert.LessOrEqual(planner.LastPerformanceStats.EvaluatedCandidateCount, 3); // 정밀 평가 수가 지정 예산을 넘지 않아야 함
            Assert.GreaterOrEqual(planner.LastPerformanceStats.TotalCandidateCount, planner.LastPerformanceStats.EvaluatedCandidateCount); // 전체 후보가 평가 후보보다 작을 수 없음
            Assert.IsNotNull(board.GetTile(action.Target)); // 선택된 목표는 실제 보드 안 좌표여야 함
        }

        [Test] // F1 스냅샷이 AdvancedPlanner를 두 번 돌리지 않고 한 번 계산한 평가 후보와 성능 값을 함께 제공하는지 검증
        public void DebugSnapshot_ExposesOptimizedCandidateAndPerformanceCounts()
        {
            var board = new BoardState(); // 빈 보드 생성
            Place(board, CreateDefinition("enemy_rook", PieceMovementType.Rook, PieceCategory.Basic, PieceRoleTag.Slider), new Vector2Int(4, 8), false); // 적 Rook 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 1), true); // 플레이어 King 배치

            var snapshot = new AIDebugScoreSnapshotBuilder().Build(board); // F1 디버그용 Preview 스냅샷 생성

            Assert.Greater(snapshot.TotalCandidateCount, 0); // Base 후보가 존재해야 함
            Assert.AreEqual(snapshot.CandidateCount, snapshot.PerformanceStats.EvaluatedCandidateCount); // 화면에 표시한 후보 수와 실제 정밀 평가 수가 같아야 함
            Assert.GreaterOrEqual(snapshot.TotalCandidateCount, snapshot.CandidateCount); // 전체 후보는 표시·평가 후보보다 작을 수 없음
        }

        private static PieceRuntimeState Place(BoardState board, PieceDefinition definition, Vector2Int position, bool isPlayerPiece) // 테스트용 기물 배치 도우미
        {
            var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 런타임 기물 생성
            board.GetTile(position).OccupyingPiece = piece; // 보드 타일에 실제 점유 등록
            return piece; // 생성 기물 반환
        }

        private static PieceDefinition CreateDefinition(string pieceId, PieceMovementType movementType, PieceCategory category, PieceRoleTag roleTags) // 테스트용 PieceDefinition 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 메모리 안에 임시 기물 정의 생성
            SetPrivateField(definition, "_pieceId", pieceId); // PieceId 설정
            SetPrivateField(definition, "_displayName", pieceId); // 표시 이름 설정
            SetPrivateField(definition, "_category", category); // 기물 분류 설정
            SetPrivateField(definition, "_grade", PieceGrade.OneStar); // 최소 등급 설정
            SetPrivateField(definition, "_movementType", movementType); // Legacy 이동 타입 설정
            SetPrivateField(definition, "_roleTags", roleTags); // 역할 태그 설정
            SetPrivateField(definition, "_baseHp", 3); // 테스트용 HP 설정
            SetPrivateField(definition, "_baseAtk", 2); // 테스트용 ATK 설정
            SetPrivateField(definition, "_occupancySize", Vector2Int.one); // 1x1 점유 설정
            SetPrivateField(definition, "_movementRules", Array.Empty<MovementRuleData>()); // 테스트에서는 Legacy 이동 규칙 경로 사용
            return definition; // 완성 정의 반환
        }

        private static void SetPrivateField<T>(PieceDefinition target, string fieldName, T value) // private 직렬화 필드 주입 도우미
        {
            var field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 지정 필드 탐색
            Assert.IsNotNull(field, $"PieceDefinition.{fieldName} 필드를 찾을 수 있어야 합니다."); // 구조 변경 시 명확한 실패 메시지 제공
            field.SetValue(target, value); // 테스트 값 적용
        }
    }
}
