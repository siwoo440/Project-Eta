using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using System.Reflection; // PieceDefinition private 필드 주입에 사용하는 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject와 Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.AI; // AI 점수 디버그 스냅샷 타입을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementRuleData를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day34AIDebugScoreTests // F1 디버그 창 1페이지에서 사용할 점수 로그 데이터를 검증하는 테스트 모음
    {
        [Test] // 기본 점수와 역할 보너스가 분리되어 최종 점수로 합산되는지 검증
        public void Snapshot_SeparatesBaseRoleAndFinalScores()
        {
            var board = new BoardState(); // 빈 보드 생성
            var enemy = Place(board, CreateDefinition("mann", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 7), false); // 근접형 적 기물 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 1), true); // 플레이어 킹 배치

            var builder = new AIDebugScoreSnapshotBuilder(); // 디버그 점수 스냅샷 빌더 생성
            var snapshot = builder.Build(board); // 현재 보드의 AI 점수 로그 생성

            Assert.Greater(snapshot.Entries.Count, 0); // 최소 하나 이상의 행동 후보가 있어야 함

            AIDebugScoreEntry forward = null; // 전진 후보 저장용 변수
            for (int i = 0; i < snapshot.Entries.Count; i++) // 로그 항목 순회
            {
                if (snapshot.Entries[i].Actor == enemy && snapshot.Entries[i].Target == new Vector2Int(4, 6)) // 킹 쪽 전진 후보를 찾으면
                {
                    forward = snapshot.Entries[i]; // 해당 항목 저장
                    break; // 탐색 종료
                }
            }

            Assert.IsNotNull(forward); // 전진 후보가 로그에 존재해야 함
            Assert.Greater(forward.RoleBonus, 0); // 근접형 전진에는 역할 보너스가 있어야 함
            Assert.AreEqual(forward.BaseScore + forward.RoleBonus, forward.FinalScore); // 최종 점수는 공통 점수와 역할 보너스의 합이어야 함
            Assert.AreEqual(EnemyAIBasicRole.Melee, forward.Role); // 로그에 역할 분류도 포함돼야 함
        }

        [Test] // 실제 AI가 선택한 행동이 디버그 로그에서 선택 상태로 표시되는지 검증
        public void Snapshot_MarksSelectedAction()
        {
            var board = new BoardState(); // 빈 보드 생성
            Place(board, CreateDefinition("rook", PieceMovementType.Rook, PieceCategory.Basic, PieceRoleTag.Slider), new Vector2Int(4, 8), false); // 적 룩 배치
            Place(board, CreateDefinition("pawn", PieceMovementType.Pawn, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 5), true); // 즉시 공격 가능한 아군 폰 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(1, 1), true); // 플레이어 킹 배치

            var builder = new AIDebugScoreSnapshotBuilder(); // 스냅샷 빌더 생성
            var snapshot = builder.Build(board); // 디버그 로그 생성

            Assert.IsNotNull(snapshot.SelectedEntry); // 실제 선택 행동이 존재해야 함
            Assert.IsTrue(snapshot.SelectedEntry.IsSelected); // 선택 행동은 선택 표시가 true여야 함
            Assert.AreEqual(AIActionType.Attack, snapshot.SelectedEntry.ActionType); // 현재 상황에서는 즉시 공격이 선택돼야 함
        }

        [Test] // Special 기물은 34일차 역할 보너스가 0으로 로그에 표시되는지 검증
        public void Snapshot_ShowsZeroRoleBonusForSpecialPiece()
        {
            var board = new BoardState(); // 빈 보드 생성
            var special = Place(board, CreateDefinition("special_rook", PieceMovementType.Rook, PieceCategory.Special, PieceRoleTag.Jumper), new Vector2Int(4, 7), false); // Special 적 기물 배치
            Place(board, CreateDefinition("king", PieceMovementType.King, PieceCategory.Basic, PieceRoleTag.Melee), new Vector2Int(4, 1), true); // 플레이어 킹 배치

            var builder = new AIDebugScoreSnapshotBuilder(); // 스냅샷 빌더 생성
            var snapshot = builder.Build(board); // 점수 로그 생성

            bool foundSpecialEntry = false; // Special 기물 후보가 실제 로그에 있었는지 기록
            for (int i = 0; i < snapshot.Entries.Count; i++) // 로그 항목 순회
            {
                if (snapshot.Entries[i].Actor != special) continue; // 해당 Special 기물 후보만 확인
                foundSpecialEntry = true; // 실제 후보를 찾았음을 기록
                Assert.AreEqual(EnemyAIBasicRole.None, snapshot.Entries[i].Role); // 34일차 기본 역할 없음으로 표시
                Assert.AreEqual(0, snapshot.Entries[i].RoleBonus); // 역할 보너스도 0이어야 함
            }

            Assert.IsTrue(foundSpecialEntry); // 후보가 하나도 없는 상태에서 테스트가 통과하는 것을 방지
        }

        private static PieceRuntimeState Place(BoardState board, PieceDefinition definition, Vector2Int position, bool isPlayerPiece) // 테스트용 기물 배치 도우미
        {
            var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 런타임 기물 생성
            board.GetTile(position).OccupyingPiece = piece; // 실제 보드 점유 등록
            return piece; // 생성한 기물 반환
        }

        private static PieceDefinition CreateDefinition(string pieceId, PieceMovementType movementType, PieceCategory category, PieceRoleTag roleTags) // 테스트용 PieceDefinition 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 임시 ScriptableObject 생성
            SetPrivateField(definition, "_pieceId", pieceId); // PieceId 설정
            SetPrivateField(definition, "_displayName", pieceId); // 표시 이름 설정
            SetPrivateField(definition, "_category", category); // 분류 설정
            SetPrivateField(definition, "_grade", PieceGrade.OneStar); // 등급 설정
            SetPrivateField(definition, "_movementType", movementType); // 이동 타입 설정
            SetPrivateField(definition, "_roleTags", roleTags); // 역할 태그 설정
            SetPrivateField(definition, "_baseHp", 3); // 테스트용 HP 설정
            SetPrivateField(definition, "_baseAtk", 2); // 테스트용 ATK 설정
            SetPrivateField(definition, "_occupancySize", Vector2Int.one); // 1x1 점유 설정
            SetPrivateField(definition, "_movementRules", Array.Empty<MovementRuleData>()); // Legacy 이동 규칙 사용
            return definition; // 정의 반환
        }

        private static void SetPrivateField<T>(PieceDefinition target, string fieldName, T value) // private 직렬화 필드 주입 도우미
        {
            var field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 필드 탐색
            Assert.IsNotNull(field, $"PieceDefinition.{fieldName} 필드를 찾을 수 있어야 합니다."); // 구조 변경 시 명확하게 실패
            field.SetValue(target, value); // 값 주입
        }
    }
}
