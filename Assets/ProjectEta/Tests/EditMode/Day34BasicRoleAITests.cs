using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using System.Reflection; // PieceDefinition private 필드에 테스트 값을 넣기 위한 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject와 Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.AI; // 34일차 역할별 AI 타입을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementRuleData를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day34BasicRoleAITests // 34일차 근접·슬라이더·도약형 AI 성격을 검증하는 테스트 모음
    {
        [Test] // 근접형은 킹과의 거리를 줄이는 이동에 추가 점수를 받는지 검증
        public void MeleeRole_GivesHigherBonusWhenMovingCloserToKing()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemy = Place(board, CreateDefinition("mann", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 7), false); // 근접형 적 기물 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 1), true); // 플레이어 킹 배치

            var closer = new AIActionCandidate(enemy, enemy.BoardPosition, new Vector2Int(4, 6), AIActionType.Move, null, 10); // 킹 쪽으로 한 칸 접근하는 후보
            var sideways = new AIActionCandidate(enemy, enemy.BoardPosition, new Vector2Int(5, 7), AIActionType.Move, null, 10); // 거리가 줄지 않는 옆 이동 후보

            int closerBonus = EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(closer, board); // 접근 후보 역할 보너스 계산
            int sidewaysBonus = EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(sideways, board); // 옆 이동 후보 역할 보너스 계산

            Assert.Greater(closerBonus, sidewaysBonus); // 근접형은 킹에 가까워지는 선택을 더 선호해야 함
        }

        [Test] // 슬라이더는 이동 후 킹을 직접 겨눌 수 있는 열린 공격선을 높은 점수로 평가하는지 검증
        public void SliderRole_PrefersMoveThatCreatesDirectKingLine()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemyRook = Place(board, CreateDefinition("rook", PieceMovementType.Rook, PieceCategory.Basic, PieceRoleTag.Slider), new Vector2Int(1, 8), false); // 적 룩 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 4), true); // 플레이어 킹 배치

            var lineMove = new AIActionCandidate(enemyRook, enemyRook.BoardPosition, new Vector2Int(4, 8), AIActionType.Move, null, 10); // 이동 후 킹과 같은 열이 되는 후보
            var neutralMove = new AIActionCandidate(enemyRook, enemyRook.BoardPosition, new Vector2Int(2, 8), AIActionType.Move, null, 10); // 킹 공격선이 열리지 않는 후보

            int lineBonus = EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(lineMove, board); // 공격선 확보 후보 평가
            int neutralBonus = EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(neutralMove, board); // 일반 위치 후보 평가

            Assert.Greater(lineBonus, neutralBonus); // 슬라이더는 다음 행동에 킹을 겨눌 수 있는 위치를 더 선호해야 함
        }

        [Test] // 도약형은 이동 후 킹을 바로 위협할 수 있는 착지점을 높은 점수로 평가하는지 검증
        public void JumperRole_PrefersLandingThatThreatensKing()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemyKnight = Place(board, CreateDefinition("knight", PieceMovementType.Knight, PieceCategory.Basic, PieceRoleTag.Jumper), new Vector2Int(2, 6), false); // 적 나이트 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 2), true); // 플레이어 킹 배치

            var threatMove = new AIActionCandidate(enemyKnight, enemyKnight.BoardPosition, new Vector2Int(3, 4), AIActionType.Move, null, 10); // 착지 후 킹을 나이트로 공격할 수 있는 후보
            var neutralMove = new AIActionCandidate(enemyKnight, enemyKnight.BoardPosition, new Vector2Int(4, 5), AIActionType.Move, null, 10); // 착지 후 킹을 바로 공격하지 못하는 후보

            int threatBonus = EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(threatMove, board); // 킹 위협 착지점 평가
            int neutralBonus = EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(neutralMove, board); // 일반 착지점 평가

            Assert.Greater(threatBonus, neutralBonus); // 도약형은 다음 공격이 가능한 위치를 더 선호해야 함
        }

        [Test] // Special 기물은 35일차 전용 평가 전까지 34일차 기본 역할 보정에서 제외되는지 검증
        public void SpecialPieces_DoNotReceiveDay34BasicRoleBonus()
        {
            var board = new BoardState(); // 빈 보드 생성
            var grasshopper = Place(board, CreateDefinition("grasshopper", PieceMovementType.Custom, PieceCategory.Special, PieceRoleTag.Jumper), new Vector2Int(4, 7), false); // Special이지만 Jumper 태그를 가진 기물 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 1), true); // 플레이어 킹 배치

            var move = new AIActionCandidate(grasshopper, grasshopper.BoardPosition, new Vector2Int(4, 6), AIActionType.Move, null, 10); // 임의 이동 후보 생성

            Assert.AreEqual(EnemyAIBasicRole.None, EnemyAIRoleClassifier.GetBasicRole(grasshopper.Definition)); // Special 기물은 기본 역할 분류에서 제외
            Assert.AreEqual(0, EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(move, board)); // 34일차 역할 보너스도 적용하지 않음
        }

        [Test] // 실제 Role Planner가 공통 33일차 점수에 역할 보너스를 더해 최종 후보를 선택하는지 검증
        public void RolePlanner_AddsRoleScoreWithoutReplacingBasePlanner()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemyMann = Place(board, CreateDefinition("mann", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 7), false); // 근접형 적 기물 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 1), true); // 플레이어 킹 배치

            var basePlanner = new EnemyAIPlanner(); // 33일차 공통 플래너 생성
            var rolePlanner = new EnemyAIRolePlanner(); // 34일차 역할 보정 플래너 생성

            var baseCandidates = basePlanner.BuildCandidates(board); // 공통 점수 후보 생성
            var roleCandidates = rolePlanner.BuildCandidates(board); // 역할 보정 후보 생성

            var baseForward = FindCandidate(baseCandidates, enemyMann, new Vector2Int(4, 6)); // 공통 플래너의 전진 후보 조회
            var roleForward = FindCandidate(roleCandidates, enemyMann, new Vector2Int(4, 6)); // 역할 플래너의 같은 후보 조회

            Assert.IsNotNull(baseForward); // 공통 후보가 실제로 존재해야 함
            Assert.IsNotNull(roleForward); // 역할 후보도 존재해야 함
            Assert.Greater(roleForward.Score, baseForward.Score); // 역할 플래너는 공통 점수를 유지하면서 추가 보너스를 더해야 함
        }

        [Test] // 즉시 공격 같은 33일차 공통 우선순위는 역할 보정 이후에도 유지되는지 검증
        public void RolePlanner_StillPrefersImmediateAttack()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemyRook = Place(board, CreateDefinition("rook", PieceMovementType.Rook, PieceCategory.Basic, PieceRoleTag.Slider), new Vector2Int(4, 8), false); // 적 룩 배치
            var playerPawn = Place(board, CreateDefinition("pawn", PieceMovementType.Pawn, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 5), true); // 바로 공격할 아군 폰 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(1, 1), true); // 멀리 플레이어 킹 배치

            var planner = new EnemyAIRolePlanner(); // 역할 보정 플래너 생성
            bool selected = planner.TryChooseAction(board, out var action); // 최종 행동 선택

            Assert.IsTrue(selected); // 행동을 선택해야 함
            Assert.AreEqual(AIActionType.Attack, action.ActionType); // 역할 보정 때문에 즉시 공격이 일반 이동보다 밀리면 안 됨
            Assert.AreSame(enemyRook, action.Actor); // 공격 주체 확인
            Assert.AreSame(playerPawn, action.TargetPiece); // 공격 대상 확인
        }

        private static AIActionCandidate FindCandidate(System.Collections.Generic.List<AIActionCandidate> candidates, PieceRuntimeState actor, Vector2Int target) // 특정 행동 후보를 찾는 테스트 도우미
        {
            for (int i = 0; i < candidates.Count; i++) // 후보 목록 순회
            {
                if (candidates[i].Actor == actor && candidates[i].Target == target) return candidates[i]; // 행동 주체와 목표가 모두 일치하면 반환
            }

            return null; // 찾지 못하면 null 반환
        }

        private static PieceRuntimeState Place(BoardState board, PieceDefinition definition, Vector2Int position, bool isPlayerPiece) // 테스트용 기물 배치 도우미
        {
            var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 런타임 기물 생성
            board.GetTile(position).OccupyingPiece = piece; // 실제 보드에 점유 등록
            return piece; // 생성 기물 반환
        }

        private static PieceDefinition CreateDefinition(string pieceId, PieceMovementType movementType, PieceCategory category, PieceRoleTag roleTags) // 테스트용 PieceDefinition 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 메모리 안에 임시 기물 정의 생성
            SetPrivateField(definition, "_pieceId", pieceId); // PieceId 설정
            SetPrivateField(definition, "_displayName", pieceId); // 표시 이름 설정
            SetPrivateField(definition, "_category", category); // 기물 분류 설정
            SetPrivateField(definition, "_grade", PieceGrade.OneStar); // 최소 등급 설정
            SetPrivateField(definition, "_movementType", movementType); // 이동 타입 설정
            SetPrivateField(definition, "_roleTags", roleTags); // 역할 태그 설정
            SetPrivateField(definition, "_baseHp", 3); // 테스트용 HP 설정
            SetPrivateField(definition, "_baseAtk", 2); // 테스트용 ATK 설정
            SetPrivateField(definition, "_occupancySize", Vector2Int.one); // 1x1 점유 설정
            SetPrivateField(definition, "_movementRules", Array.Empty<MovementRuleData>()); // Legacy 이동 규칙 경로 사용
            return definition; // 완성된 정의 반환
        }

        private static void SetPrivateField<T>(PieceDefinition target, string fieldName, T value) // private 직렬화 필드 주입 공통 도우미
        {
            var field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 지정 필드 탐색
            Assert.IsNotNull(field, $"PieceDefinition.{fieldName} 필드를 찾을 수 있어야 합니다."); // 구조 변경 시 명확하게 실패
            field.SetValue(target, value); // 테스트 값 주입
        }
    }
}
