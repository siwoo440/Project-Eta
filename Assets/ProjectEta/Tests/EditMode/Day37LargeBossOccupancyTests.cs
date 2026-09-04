using System; // Array.Empty<T>와 InvalidOperationException을 사용하기 위한 네임스페이스
using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using System.Reflection; // PieceDefinition private 직렬화 필드 주입에 사용하는 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject와 Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.AI; // EnemyAIPlanner를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementRuleData를 사용하기 위한 네임스페이스
using ProjectEta.Boss; // 37일차 대형 기물 점유·시각 보정 유틸리티를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day37LargeBossOccupancyTests // 37일차 2x2 보스 점유 기반을 검증하는 테스트 모음
    {
        [Test] // 2x2 보스가 정확히 4칸을 같은 런타임 기물 하나로 점유하는지 검증
        public void LargeBoss_OccupiesFourCellsWithOneRuntimeState()
        {
            var board = new BoardState(); // 빈 10x10 보드 생성
            var bossDefinition = CreateDefinition("prototype_boss_37", new Vector2Int(2, 2), PieceMovementType.Custom, PieceCategory.Boss); // 2x2 보스 정의 생성
            var boss = new PieceRuntimeState(bossDefinition, new Vector2Int(4, 7), false); // 기준 좌표가 (4,7)인 적 보스 생성

            bool placed = LargePieceBoardUtility.TryPlace(board, boss, boss.BoardPosition); // 2x2 전체 점유 시도

            Assert.IsTrue(placed); // 네 칸 모두 비어 있으므로 점유에 성공해야 함

            var expectedCells = new[] // 보스가 차지해야 하는 네 칸 목록
            {
                new Vector2Int(4, 7), // 좌하단 기준 칸
                new Vector2Int(5, 7), // 우하단 칸
                new Vector2Int(4, 8), // 좌상단 칸
                new Vector2Int(5, 8)  // 우상단 칸
            };

            for (int i = 0; i < expectedCells.Length; i++) // 네 점유 칸 순회
            {
                Assert.AreSame(boss, board.GetTile(expectedCells[i]).OccupyingPiece); // 모든 칸이 같은 PieceRuntimeState 하나를 가리켜야 함
            }
        }

        [Test] // 2x2 영역 중 한 칸이라도 이미 점유되어 있으면 전체 배치가 원자적으로 실패하는지 검증
        public void LargeBoss_FailsWithoutPartialOccupancyWhenOneCellIsBlocked()
        {
            var board = new BoardState(); // 빈 보드 생성
            var blockerDefinition = CreateDefinition("blocker", Vector2Int.one, PieceMovementType.Pawn, PieceCategory.Basic); // 방해 기물 정의 생성
            var blocker = new PieceRuntimeState(blockerDefinition, new Vector2Int(5, 8), true); // 보스 영역 안 한 칸에 플레이어 기물 생성
            board.GetTile(blocker.BoardPosition).OccupyingPiece = blocker; // 실제 점유 등록

            var bossDefinition = CreateDefinition("prototype_boss_37", new Vector2Int(2, 2), PieceMovementType.Custom, PieceCategory.Boss); // 2x2 보스 정의
            var boss = new PieceRuntimeState(bossDefinition, new Vector2Int(4, 7), false); // 보스 생성

            bool placed = LargePieceBoardUtility.TryPlace(board, boss, boss.BoardPosition); // 겹치는 영역에 배치 시도

            Assert.IsFalse(placed); // 한 칸이라도 막혀 있으므로 전체 배치 실패
            Assert.IsNull(board.GetTile(new Vector2Int(4, 7)).OccupyingPiece); // 좌하단에 보스가 부분 배치되면 안 됨
            Assert.IsNull(board.GetTile(new Vector2Int(5, 7)).OccupyingPiece); // 우하단도 비어 있어야 함
            Assert.IsNull(board.GetTile(new Vector2Int(4, 8)).OccupyingPiece); // 좌상단도 비어 있어야 함
            Assert.AreSame(blocker, board.GetTile(new Vector2Int(5, 8)).OccupyingPiece); // 기존 방해 기물은 그대로 유지돼야 함
        }

        [Test] // 보드 밖으로 2x2 영역이 튀어나가면 배치가 실패하는지 검증
        public void LargeBoss_FailsWhenFootprintExitsBoard()
        {
            var board = new BoardState(); // 빈 보드 생성
            var bossDefinition = CreateDefinition("prototype_boss_37", new Vector2Int(2, 2), PieceMovementType.Custom, PieceCategory.Boss); // 2x2 보스 정의
            var boss = new PieceRuntimeState(bossDefinition, new Vector2Int(9, 9), false); // 우상단 끝 칸을 기준점으로 설정

            bool placed = LargePieceBoardUtility.TryPlace(board, boss, boss.BoardPosition); // 보드 밖으로 나가는 배치 시도

            Assert.IsFalse(placed); // 10x10 범위를 넘어가므로 실패해야 함
            Assert.IsNull(board.GetTile(new Vector2Int(9, 9)).OccupyingPiece); // 기준 칸도 부분 점유되지 않아야 함
        }

        [Test] // 보스를 제거하면 기준 칸뿐 아니라 같은 보스를 참조하는 네 칸이 모두 해제되는지 검증
        public void ClearPiece_RemovesEveryOccupiedCell()
        {
            var board = new BoardState(); // 빈 보드 생성
            var bossDefinition = CreateDefinition("prototype_boss_37", new Vector2Int(2, 2), PieceMovementType.Custom, PieceCategory.Boss); // 2x2 보스 정의
            var boss = new PieceRuntimeState(bossDefinition, new Vector2Int(3, 7), false); // 적 보스 생성
            Assert.IsTrue(LargePieceBoardUtility.TryPlace(board, boss, boss.BoardPosition)); // 보스 4칸 점유

            int cleared = board.ClearPiece(boss); // 보드 전체에서 같은 보스 참조를 제거

            Assert.AreEqual(4, cleared); // 정확히 네 칸이 해제돼야 함
            Assert.IsNull(board.GetTile(new Vector2Int(3, 7)).OccupyingPiece); // 기준 칸 해제 확인
            Assert.IsNull(board.GetTile(new Vector2Int(4, 7)).OccupyingPiece); // 우하단 해제 확인
            Assert.IsNull(board.GetTile(new Vector2Int(3, 8)).OccupyingPiece); // 좌상단 해제 확인
            Assert.IsNull(board.GetTile(new Vector2Int(4, 8)).OccupyingPiece); // 우상단 해제 확인
        }

        [Test] // BoardState.CountPieces가 2x2 보스를 4기가 아니라 1기로 세는지 검증
        public void CountPieces_CountsLargeBossOnce()
        {
            var board = new BoardState(); // 빈 보드 생성
            var bossDefinition = CreateDefinition("prototype_boss_37", new Vector2Int(2, 2), PieceMovementType.Custom, PieceCategory.Boss); // 2x2 보스 정의
            var pawnDefinition = CreateDefinition("pawn", Vector2Int.one, PieceMovementType.Pawn, PieceCategory.Basic); // 일반 적 폰 정의
            var boss = new PieceRuntimeState(bossDefinition, new Vector2Int(3, 7), false); // 적 보스 생성
            var pawn = new PieceRuntimeState(pawnDefinition, new Vector2Int(8, 8), false); // 일반 적 폰 생성

            Assert.IsTrue(LargePieceBoardUtility.TryPlace(board, boss, boss.BoardPosition)); // 보스 4칸 점유
            board.GetTile(pawn.BoardPosition).OccupyingPiece = pawn; // 폰 1칸 점유

            Assert.AreEqual(2, board.CountPieces(false)); // 보스 1기 + 폰 1기 = 총 2기로 계산돼야 함
        }

        [Test] // 죽은 대형 기물의 잔여 점유가 승리 조건 계산에 포함되지 않는지 검증
        public void CountPieces_IgnoresDeadLargeBossEvenBeforeCleanup()
        {
            var board = new BoardState(); // 빈 보드 생성
            var bossDefinition = CreateDefinition("prototype_boss_37", new Vector2Int(2, 2), PieceMovementType.Custom, PieceCategory.Boss); // 2x2 보스 정의
            var boss = new PieceRuntimeState(bossDefinition, new Vector2Int(3, 7), false); // 적 보스 생성
            Assert.IsTrue(LargePieceBoardUtility.TryPlace(board, boss, boss.BoardPosition)); // 네 칸 점유

            boss.CurrentHp = 0; // 사망 상태로 변경하되 일부 점유가 남은 상황을 재현

            Assert.AreEqual(0, board.CountPieces(false)); // 사망 기물은 남은 적 수에 포함되면 안 됨
        }

        [Test] // 2x2 기물 모델의 기준 위치가 네 칸 중앙으로 계산되는지 검증
        public void VisualCenter_IsMiddleOfTwoByTwoFootprint()
        {
            Vector3 center = LargePieceVisualUtility.CalculateFootprintLocalPosition(new Vector2Int(4, 7), new Vector2Int(2, 2), 1f); // 2x2 보스 시각 중심 계산
            Vector3 anchor = BoardView.BoardToLocalPosition(new Vector2Int(4, 7), 1f); // 기준 칸 중심 계산

            Assert.AreEqual(anchor.x + 0.5f, center.x, 0.0001f); // 2칸 폭의 중앙이므로 X가 반 칸 오른쪽이어야 함
            Assert.AreEqual(anchor.z + 0.5f, center.z, 0.0001f); // 2칸 높이의 중앙이므로 Z도 반 칸 위쪽이어야 함
        }

        [Test] // AI가 2x2 동일 런타임 보스를 네 번 읽어 같은 행동 후보를 중복 생성하지 않는지 검증
        public void EnemyAIPlanner_DoesNotDuplicateCandidatesForLargePiece()
        {
            var board = new BoardState(); // 빈 보드 생성
            var bossDefinition = CreateDefinition("large_rook_boss", new Vector2Int(2, 2), PieceMovementType.Rook, PieceCategory.Boss); // 테스트용 룩형 2x2 보스 정의
            var kingDefinition = CreateDefinition("king", Vector2Int.one, PieceMovementType.King, PieceCategory.Basic); // 플레이어 킹 정의
            var boss = new PieceRuntimeState(bossDefinition, new Vector2Int(4, 7), false); // 적 보스 생성
            var king = new PieceRuntimeState(kingDefinition, new Vector2Int(9, 0), true); // 멀리 플레이어 킹 생성

            Assert.IsTrue(LargePieceBoardUtility.TryPlace(board, boss, boss.BoardPosition)); // 보스 네 칸 점유
            board.GetTile(king.BoardPosition).OccupyingPiece = king; // 킹 점유 등록

            var candidates = new EnemyAIPlanner().BuildCandidates(board); // 현재 AI 후보 생성
            var uniqueKeys = new HashSet<string>(); // 같은 행동이 두 번 생성되는지 확인할 키 집합

            for (int i = 0; i < candidates.Count; i++) // 모든 AI 후보 순회
            {
                var candidate = candidates[i]; // 현재 후보
                if (candidate.Actor != boss) continue; // 보스 행동만 검사
                string key = $"{candidate.ActionType}:{candidate.Target.x}:{candidate.Target.y}"; // 행동 종류와 목표 좌표로 고유 키 생성
                Assert.IsTrue(uniqueKeys.Add(key), $"중복 AI 후보 발견: {key}"); // 같은 후보가 두 번 나오면 실패
            }
        }

        [Test] // Resources에 포함할 프로토타입 보스가 실제 Boss/2x2 데이터인지 검증
        public void PrototypeBoss37Asset_LoadsAsTwoByTwoBoss()
        {
            var definition = Resources.Load<PieceDefinition>("PrototypeBoss37"); // 37일차 프로토타입 보스 에셋 로드

            Assert.IsNotNull(definition); // 별도 Inspector 작업 없이 Resources에서 로드돼야 함
            Assert.AreEqual(PieceCategory.Boss, definition.Category); // Boss 분류여야 함
            Assert.AreEqual(new Vector2Int(2, 2), definition.OccupancySize); // 정확히 2x2 점유 크기여야 함
            Assert.Greater(definition.BaseHp, 0); // 테스트 전투가 가능하도록 HP가 있어야 함
        }

        private static PieceDefinition CreateDefinition(string pieceId, Vector2Int occupancySize, PieceMovementType movementType, PieceCategory category) // 테스트용 기물 정의 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 메모리 안에 임시 PieceDefinition 생성
            SetPrivateField(definition, "_pieceId", pieceId); // PieceId 설정
            SetPrivateField(definition, "_displayName", pieceId); // 표시 이름 설정
            SetPrivateField(definition, "_category", category); // 기물 분류 설정
            SetPrivateField(definition, "_grade", PieceGrade.FiveStar); // 보스 테스트는 5성으로 설정
            SetPrivateField(definition, "_movementType", movementType); // Legacy 이동 타입 설정
            SetPrivateField(definition, "_roleTags", PieceRoleTag.Tanker | PieceRoleTag.Attacker); // 탱커+공격자 역할 설정
            SetPrivateField(definition, "_immuneStatusTags", StatusEffectType.None); // 테스트에서는 상태 면역 없음
            SetPrivateField(definition, "_movementRules", Array.Empty<MovementRuleData>()); // Legacy 이동 타입 경로를 사용
            SetPrivateField(definition, "_baseHp", 30); // 테스트용 보스 HP
            SetPrivateField(definition, "_baseAtk", 4); // 테스트용 보스 ATK
            SetPrivateField(definition, "_occupancySize", occupancySize); // 점유 크기 설정
            return definition; // 완성된 정의 반환
        }

        private static void SetPrivateField<T>(PieceDefinition target, string fieldName, T value) // PieceDefinition private 필드 주입 공통 도우미
        {
            var field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 지정 필드 탐색
            Assert.IsNotNull(field, $"PieceDefinition.{fieldName} 필드를 찾을 수 있어야 합니다."); // 데이터 구조 변경 시 명확하게 실패
            field.SetValue(target, value); // 테스트 값 주입
        }
    }
}
