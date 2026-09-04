using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using System.Reflection; // PieceDefinition private 필드에 테스트 데이터를 넣기 위한 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject와 Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.AI; // 35일차 위협·특수 AI 타입을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementRuleData를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day35ThreatAndSpecialAITests // 35일차 위협 맵·특수 기물 점수·최종 합산을 검증하는 테스트 모음
    {
        [Test] // 빈 칸이라도 플레이어가 실제 공격할 수 있는 위치라면 위협 칸으로 표시되는지 검증
        public void ThreatMap_MarksEmptySquaresThatPlayerCouldAttack()
        {
            var board = new BoardState(); // 빈 보드 생성
            Place(board, CreateDefinition("player_rook", PieceMovementType.Rook, PieceCategory.Basic, PieceRoleTag.Slider), new Vector2Int(4, 4), true); // 플레이어 룩 배치

            var threatMap = EnemyAIThreatMap.Build(board); // 현재 플레이어 공격 범위로 위협 맵 생성

            Assert.Greater(threatMap.GetThreatCount(new Vector2Int(4, 8)), 0); // 같은 열의 빈 칸은 룩이 공격 가능한 위험 칸이어야 함
            Assert.AreEqual(0, threatMap.GetThreatCount(new Vector2Int(5, 8))); // 룩 공격선 밖의 칸은 안전해야 함
        }

        [Test] // 같은 기본 행동이라도 위협 칸으로 이동하면 안전한 칸보다 낮은 위협 점수를 받는지 검증
        public void ThreatEvaluator_PenalizesMoveIntoThreatenedSquare()
        {
            var board = new BoardState(); // 빈 보드 생성
            Place(board, CreateDefinition("player_rook", PieceMovementType.Rook, PieceCategory.Basic, PieceRoleTag.Slider), new Vector2Int(4, 4), true); // 플레이어 룩 배치
            var enemy = Place(board, CreateDefinition("enemy_mann", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(3, 8), false); // 적 근접 기물 배치

            var threatMap = EnemyAIThreatMap.Build(board); // 위협 맵 생성
            var dangerMove = new AIActionCandidate(enemy, enemy.BoardPosition, new Vector2Int(4, 8), AIActionType.Move, null, 10); // 룩 공격선으로 이동하는 후보
            var safeMove = new AIActionCandidate(enemy, enemy.BoardPosition, new Vector2Int(2, 8), AIActionType.Move, null, 10); // 안전한 칸으로 이동하는 후보

            int dangerScore = EnemyAIThreatScoreEvaluator.Evaluate(dangerMove, board, threatMap); // 위험 이동 점수 계산
            int safeScore = EnemyAIThreatScoreEvaluator.Evaluate(safeMove, board, threatMap); // 안전 이동 점수 계산

            Assert.Less(dangerScore, safeScore); // 위험한 이동은 더 낮은 점수를 받아야 함
        }

        [TestCase("cannon")] // Cannon 특수 공격 사용 보너스 검증
        [TestCase("grasshopper")] // Grasshopper 특수 공격 사용 보너스 검증
        [TestCase("nightrider")] // Nightrider 특수 공격 사용 보너스 검증
        public void SpecialEvaluator_GivesPositiveBonusForSignatureAttack(string pieceId)
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemy = Place(board, CreateDefinition(pieceId, PieceMovementType.Rook, PieceCategory.Special, PieceRoleTag.Ranged), new Vector2Int(4, 8), false); // 특수 기물 배치
            var target = Place(board, CreateDefinition("pawn", PieceMovementType.Pawn, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 5), true); // 공격 대상 배치
            var attack = new AIActionCandidate(enemy, enemy.BoardPosition, target.BoardPosition, AIActionType.Attack, target, 1000); // 즉시 공격 후보 생성

            int bonus = EnemyAISpecialScoreEvaluator.Evaluate(attack, board); // 특수 기물 점수 계산

            Assert.Greater(bonus, 0); // 특수 공격을 실제로 사용할 수 있는 행동은 추가 점수를 받아야 함
        }

        [Test] // Chameleon이 이동 후 다음 형태로 King을 위협할 수 있으면 추가 점수를 받는지 검증
        public void Chameleon_PreviewsNextMovementCycleForKingThreat()
        {
            var board = new BoardState(); // 빈 보드 생성
            var chameleon = Place(board, CreateDefinition("chameleon", PieceMovementType.Knight, PieceCategory.Special, PieceRoleTag.Jumper), new Vector2Int(2, 6), false); // 초기 Knight 단계 Chameleon 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 3), true); // 이동 후 Bishop 단계에서 위협 가능한 King 배치
            var move = new AIActionCandidate(chameleon, chameleon.BoardPosition, new Vector2Int(3, 4), AIActionType.Move, null, 10); // Knight 형태의 이동 후보

            int bonus = EnemyAISpecialScoreEvaluator.Evaluate(move, board); // 다음 이동 단계까지 고려한 특수 점수 계산

            Assert.Greater(bonus, 0); // 이동 후 Bishop으로 King을 위협할 수 있으므로 추가 점수가 있어야 함
        }

        [Test] // 최종 플래너가 Base + Role + Threat + Special을 정확히 합산하는지 검증
        public void AdvancedPlanner_FinalScoreEqualsAllScoreLayers()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemy = Place(board, CreateDefinition("mann", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 7), false); // 근접형 적 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 1), true); // 플레이어 King 배치

            var basePlanner = new EnemyAIPlanner(); // 33일차 공통 플래너 생성
            var advancedPlanner = new EnemyAIAdvancedPlanner(); // 35일차 최종 플래너 생성
            var threatMap = EnemyAIThreatMap.Build(board); // 동일 보드의 위협 맵 생성
            var baseCandidates = basePlanner.BuildCandidates(board); // Base 후보 생성
            var advancedCandidates = advancedPlanner.BuildCandidates(board); // 최종 후보 생성

            var baseCandidate = FindCandidate(baseCandidates, enemy, new Vector2Int(4, 6)); // King 쪽 전진 Base 후보 찾기
            var advancedCandidate = FindCandidate(advancedCandidates, enemy, new Vector2Int(4, 6)); // 같은 최종 후보 찾기

            Assert.IsNotNull(baseCandidate); // Base 후보 존재 확인
            Assert.IsNotNull(advancedCandidate); // 최종 후보 존재 확인

            int role = EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(baseCandidate, board); // 역할 보너스 계산
            int threat = EnemyAIThreatScoreEvaluator.Evaluate(baseCandidate, board, threatMap); // 위협 점수 계산
            int special = EnemyAISpecialScoreEvaluator.Evaluate(baseCandidate, board); // 특수 점수 계산

            Assert.AreEqual(baseCandidate.Score + role + threat + special, advancedCandidate.Score); // 네 점수 계층 합이 최종 점수와 같아야 함
        }

        [Test] // 디버그 스냅샷이 35일차 점수 계층을 분리해 기록하는지 검증
        public void DebugSnapshot_ContainsThreatAndSpecialScores()
        {
            var board = new BoardState(); // 빈 보드 생성
            Place(board, CreateDefinition("cannon", PieceMovementType.Rook, PieceCategory.Special, PieceRoleTag.Ranged), new Vector2Int(4, 8), false); // Cannon 역할의 특수 기물 배치
            Place(board, CreateDefinition("pawn", PieceMovementType.Pawn, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 5), true); // 공격 대상 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(1, 1), true); // 플레이어 King 배치

            var snapshot = new AIDebugScoreSnapshotBuilder().Build(board); // F1 창이 사용할 최신 점수 스냅샷 생성

            Assert.Greater(snapshot.Entries.Count, 0); // 로그 후보 존재 확인
            bool foundSpecial = false; // Cannon 후보 발견 여부

            for (int i = 0; i < snapshot.Entries.Count; i++) // 모든 로그 후보 순회
            {
                var entry = snapshot.Entries[i]; // 현재 로그 항목
                if (entry.Actor?.Definition?.PieceId != "cannon") continue; // Cannon 후보만 검사
                foundSpecial = true; // Cannon 로그를 찾았음을 기록
                Assert.AreEqual(entry.BaseScore + entry.RoleBonus + entry.ThreatScore + entry.SpecialBonus, entry.FinalScore); // 디버그 표시값 합계 확인
            }

            Assert.IsTrue(foundSpecial); // 비어 있는 검사로 통과하지 않도록 실제 Cannon 후보 존재를 요구
        }

        private static AIActionCandidate FindCandidate(System.Collections.Generic.List<AIActionCandidate> candidates, PieceRuntimeState actor, Vector2Int target) // 특정 행동 후보 탐색 도우미
        {
            for (int i = 0; i < candidates.Count; i++) // 모든 후보 순회
            {
                if (candidates[i].Actor == actor && candidates[i].Target == target) return candidates[i]; // 행동 주체와 목표가 일치하면 반환
            }

            return null; // 없으면 null 반환
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
            Assert.IsNotNull(field, $"PieceDefinition.{fieldName} 필드를 찾을 수 있어야 합니다."); // 구조가 바뀌면 명확하게 테스트 실패
            field.SetValue(target, value); // 테스트 값 주입
        }
    }
}
