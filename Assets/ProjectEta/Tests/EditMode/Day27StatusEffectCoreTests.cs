using UnityEditor; // SerializedObject로 private 직렬화 필드를 테스트용으로 설정하기 위한 네임스페이스
using UnityEngine; // ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState, StatusEffectDefinition 등을 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState, RunSaveData를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day27StatusEffectCoreTests // 27일차 상태 효과 코어(프레임워크 + 면역)를 검증하는 테스트 모음
    {
        [Test] // 상태를 걸면 보유 상태가 되고, 지속 턴이 0이 되면 자동으로 제거되는지 검증
        public void ApplyStatus_TicksDownAndExpiresWhenDurationReachesZero()
        {
            var poison = CreateStatusDefinition(StatusEffectType.Poison, StatusStackMode.StacksAdd, maxStacks: 3, durationTurns: 2); // 2턴 지속 독 정의
            var piece = new PieceRuntimeState(CreatePieceDefinition("test_target", immuneTags: StatusEffectType.None), Vector2Int.zero, true); // 면역 없는 테스트용 기물

            Assert.IsTrue(piece.ApplyStatus(poison)); // 정상적으로 부여되어야 함
            Assert.IsTrue(piece.HasStatus(StatusEffectType.Poison)); // 보유 상태 확인

            piece.TickStatusEffects(); // 1턴 경과
            Assert.IsTrue(piece.HasStatus(StatusEffectType.Poison), "지속 턴이 남아 있으면 유지되어야 합니다."); // 아직 유지

            piece.TickStatusEffects(); // 2턴 경과
            Assert.IsFalse(piece.HasStatus(StatusEffectType.Poison), "지속 턴이 소진되면 제거되어야 합니다."); // 만료 후 제거
        }

        [Test] // 중첩형(StacksAdd) 상태는 재적용 시 최대 중첩까지 쌓이고 지속 턴이 갱신되는지 검증
        public void ApplyStatus_StacksAddMode_AccumulatesUpToMaxStacksAndRefreshesDuration()
        {
            var poison = CreateStatusDefinition(StatusEffectType.Poison, StatusStackMode.StacksAdd, maxStacks: 3, durationTurns: 2); // 최대 3중첩 독
            var piece = new PieceRuntimeState(CreatePieceDefinition("test_target", immuneTags: StatusEffectType.None), Vector2Int.zero, true); // 테스트용 기물

            piece.ApplyStatus(poison); // 1회 적용
            piece.ApplyStatus(poison); // 2회 적용
            piece.ApplyStatus(poison); // 3회 적용
            piece.ApplyStatus(poison); // 4회 적용(상한 초과 시도)

            var status = piece.FindStatus(StatusEffectType.Poison); // 현재 상태 조회
            Assert.IsNotNull(status); // 존재해야 함
            Assert.AreEqual(3, status.StackCount, "최대 중첩 수를 넘어서는 안 됩니다."); // 상한 3에서 멈춤
            Assert.AreEqual(2, status.RemainingTurns, "재적용 시 지속 턴이 갱신되어야 합니다."); // 마지막 적용 기준 지속 턴 갱신
        }

        [Test] // 갱신형(RefreshDuration) 상태는 재적용해도 중첩되지 않고 지속 턴만 갱신되는지 검증
        public void ApplyStatus_RefreshDurationMode_DoesNotStackButRefreshesDuration()
        {
            var burn = CreateStatusDefinition(StatusEffectType.Burn, StatusStackMode.RefreshDuration, maxStacks: 1, durationTurns: 3); // 갱신형 화상
            var piece = new PieceRuntimeState(CreatePieceDefinition("test_target", immuneTags: StatusEffectType.None), Vector2Int.zero, true); // 테스트용 기물

            piece.ApplyStatus(burn); // 최초 적용
            piece.TickStatusEffects(); // 1턴 경과 (남은 턴 2)
            piece.ApplyStatus(burn); // 재적용

            var status = piece.FindStatus(StatusEffectType.Burn); // 현재 상태 조회
            Assert.IsNotNull(status); // 존재해야 함
            Assert.AreEqual(1, status.StackCount, "갱신형 상태는 중첩되지 않아야 합니다."); // 중첩 없음
            Assert.AreEqual(3, status.RemainingTurns, "재적용 시 지속 턴이 다시 최대치로 갱신되어야 합니다."); // 지속 턴 리셋
        }

        [Test] // 면역 태그가 있는 기물에는 해당 상태가 아예 부여되지 않는지 검증
        public void ApplyStatus_ImmunePiece_RejectsMatchingStatus()
        {
            var stun = CreateStatusDefinition(StatusEffectType.Stun, StatusStackMode.RefreshDuration, maxStacks: 1, durationTurns: 1); // 기절 정의
            var immunePiece = new PieceRuntimeState(CreatePieceDefinition("test_boss", immuneTags: StatusEffectType.Stun), Vector2Int.zero, false); // 기절 면역 보스용 정의

            bool applied = immunePiece.ApplyStatus(stun); // 부여 시도

            Assert.IsFalse(applied, "면역 상태는 부여에 실패해야 합니다."); // 실패 반환
            Assert.IsFalse(immunePiece.HasStatus(StatusEffectType.Stun), "면역 상태는 목록에 남지 않아야 합니다."); // 목록에도 없어야 함
        }

        [Test] // 면역이 없는 다른 상태는 정상적으로 부여되는지 검증(면역 태그가 특정 종류에만 한정되는지 확인)
        public void ApplyStatus_ImmunePiece_StillAcceptsNonImmuneStatus()
        {
            var root = CreateStatusDefinition(StatusEffectType.Root, StatusStackMode.RefreshDuration, maxStacks: 1, durationTurns: 1); // 속박 정의
            var stunImmunePiece = new PieceRuntimeState(CreatePieceDefinition("test_boss", immuneTags: StatusEffectType.Stun), Vector2Int.zero, false); // 기절만 면역인 보스

            bool applied = stunImmunePiece.ApplyStatus(root); // 기절이 아닌 속박 부여 시도

            Assert.IsTrue(applied, "면역 대상이 아닌 상태는 정상적으로 부여되어야 합니다."); // 성공해야 함
            Assert.IsTrue(stunImmunePiece.HasStatus(StatusEffectType.Root)); // 실제 보유 확인
        }

        [Test] // 상태 이상이 걸린 기물을 저장 후 복원해도 종류·지속 턴·중첩 수가 그대로 유지되는지 검증
        public void RunState_SaveAndRestore_PreservesStatusEffectSnapshot()
        {
            var poisonDefinition = CreateStatusDefinition(StatusEffectType.Poison, StatusStackMode.StacksAdd, maxStacks: 3, durationTurns: 4); // 독 정의
            var statusDatabase = CreateStatusEffectDatabase(poisonDefinition); // 독만 등록된 상태 이상 DB

            var pieceDefinition = CreatePieceDefinition("test_target", immuneTags: StatusEffectType.None); // 테스트용 기물 정의
            var pieceDatabase = CreatePieceDatabase(pieceDefinition); // 해당 정의만 등록된 기물 DB

            var runState = new RunState(3); // 초기 킹 체력 3으로 런 생성
            var boardPosition = new Vector2Int(2, 3); // 임의의 보드 좌표
            var runtimePiece = new PieceRuntimeState(pieceDefinition, boardPosition, true); // 보드에 올릴 기물 생성
            runtimePiece.ApplyStatus(poisonDefinition); // 독 2중첩까지 걸어보기 위해 두 번 적용
            runtimePiece.ApplyStatus(poisonDefinition);
            runState.Board.GetTile(boardPosition).OccupyingPiece = runtimePiece; // 보드에 배치

            var saveData = runState.ToSaveData(); // 저장 데이터로 변환
            var restored = RunState.FromSaveData(saveData, pieceDatabase, statusDatabase); // 상태 이상 DB까지 함께 전달해 복원

            var restoredPiece = restored.Board.GetTile(boardPosition).OccupyingPiece; // 복원된 기물 조회
            Assert.IsNotNull(restoredPiece); // 기물 자체가 복원되어야 함

            var restoredStatus = restoredPiece.FindStatus(StatusEffectType.Poison); // 복원된 독 상태 조회
            Assert.IsNotNull(restoredStatus, "저장된 상태 이상이 복원되어야 합니다."); // 상태 자체 존재 확인
            Assert.AreEqual(2, restoredStatus.StackCount, "중첩 수가 그대로 복원되어야 합니다."); // 중첩 수 일치
            Assert.AreEqual(4, restoredStatus.RemainingTurns, "지속 턴이 그대로 복원되어야 합니다."); // 지속 턴 일치
        }

        [Test] // 상태 이상 DB 없이 복원해도(구버전 호출 호환) 예외 없이 기물만 정상 복원되는지 검증
        public void RunState_RestoreWithoutStatusEffectDatabase_StillRestoresPieceSafely()
        {
            var poisonDefinition = CreateStatusDefinition(StatusEffectType.Poison, StatusStackMode.StacksAdd, maxStacks: 3, durationTurns: 4); // 독 정의
            var pieceDefinition = CreatePieceDefinition("test_target", immuneTags: StatusEffectType.None); // 테스트용 기물 정의
            var pieceDatabase = CreatePieceDatabase(pieceDefinition); // 해당 정의만 등록된 기물 DB

            var runState = new RunState(3); // 초기 킹 체력 3으로 런 생성
            var boardPosition = new Vector2Int(1, 1); // 임의의 보드 좌표
            var runtimePiece = new PieceRuntimeState(pieceDefinition, boardPosition, true); // 보드에 올릴 기물 생성
            runtimePiece.ApplyStatus(poisonDefinition); // 독 적용
            runState.Board.GetTile(boardPosition).OccupyingPiece = runtimePiece; // 보드에 배치

            var saveData = runState.ToSaveData(); // 저장 데이터로 변환
            var restored = RunState.FromSaveData(saveData, pieceDatabase); // 기존 호출 방식(상태 이상 DB 생략)으로 복원

            var restoredPiece = restored.Board.GetTile(boardPosition).OccupyingPiece; // 복원된 기물 조회
            Assert.IsNotNull(restoredPiece); // 기물 자체는 정상 복원되어야 함
            Assert.IsFalse(restoredPiece.HasStatus(StatusEffectType.Poison), "DB 없이는 상태 이상을 복원하지 않아야 합니다."); // 상태는 복원되지 않음(구버전 호환)
        }

        private static StatusEffectDefinition CreateStatusDefinition(StatusEffectType statusType, StatusStackMode stackMode, int maxStacks, int durationTurns) // 테스트 전용 상태 이상 정의 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<StatusEffectDefinition>(); // 인스턴스만 생성(에셋으로 저장하지 않음)
            var serialized = new SerializedObject(definition); // private 직렬화 필드에 접근하기 위한 SerializedObject
            serialized.FindProperty("_statusType").intValue = (int)statusType; // 상태 종류 설정
            serialized.FindProperty("_displayName").stringValue = statusType.ToString(); // 표시 이름 설정
            serialized.FindProperty("_stackMode").enumValueIndex = (int)stackMode; // 중첩 방식 설정
            serialized.FindProperty("_maxStacks").intValue = maxStacks; // 최대 중첩 수 설정
            serialized.FindProperty("_defaultDurationTurns").intValue = durationTurns; // 기본 지속 턴 설정
            serialized.FindProperty("_description").stringValue = "27일차 테스트용 임시 상태 이상 정의."; // 설명 설정
            serialized.ApplyModifiedPropertiesWithoutUndo(); // Undo 기록 없이 즉시 반영
            return definition; // 완성된 테스트용 정의 반환
        }

        private static PieceDefinition CreatePieceDefinition(string pieceId, StatusEffectType immuneTags) // 테스트 전용 기물 정의 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 인스턴스만 생성(에셋으로 저장하지 않음)
            var serialized = new SerializedObject(definition); // private 직렬화 필드에 접근하기 위한 SerializedObject
            serialized.FindProperty("_pieceId").stringValue = pieceId; // 식별자 설정
            serialized.FindProperty("_displayName").stringValue = pieceId; // 표시 이름 설정
            serialized.FindProperty("_baseHp").intValue = 5; // 테스트용 임시 체력
            serialized.FindProperty("_baseAtk").intValue = 1; // 테스트용 임시 공격력
            serialized.FindProperty("_occupancySize").vector2IntValue = Vector2Int.one; // 1칸 점유
            serialized.FindProperty("_description").stringValue = "27일차 테스트용 임시 기물 정의."; // 설명 설정
            serialized.FindProperty("_immuneStatusTags").intValue = (int)immuneTags; // 상태 면역 태그 설정
            serialized.ApplyModifiedPropertiesWithoutUndo(); // Undo 기록 없이 즉시 반영
            return definition; // 완성된 테스트용 정의 반환
        }

        private static PieceDatabase CreatePieceDatabase(params PieceDefinition[] definitions) // 테스트 전용 PieceDatabase 생성 도우미
        {
            var database = ScriptableObject.CreateInstance<PieceDatabase>(); // 인스턴스만 생성(에셋으로 저장하지 않음)
            var serialized = new SerializedObject(database); // private 직렬화 필드에 접근하기 위한 SerializedObject
            var listProperty = serialized.FindProperty("_definitions"); // 정의 목록 프로퍼티 조회
            listProperty.arraySize = definitions.Length; // 목록 크기 설정
            for (int i = 0; i < definitions.Length; i++) // 전달된 정의를 순서대로
            {
                listProperty.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i]; // 목록에 채워 넣기
            }
            serialized.ApplyModifiedPropertiesWithoutUndo(); // Undo 기록 없이 즉시 반영
            return database; // 완성된 테스트용 DB 반환
        }

        private static StatusEffectDatabase CreateStatusEffectDatabase(params StatusEffectDefinition[] definitions) // 테스트 전용 StatusEffectDatabase 생성 도우미
        {
            var database = ScriptableObject.CreateInstance<StatusEffectDatabase>(); // 인스턴스만 생성(에셋으로 저장하지 않음)
            var serialized = new SerializedObject(database); // private 직렬화 필드에 접근하기 위한 SerializedObject
            var listProperty = serialized.FindProperty("_definitions"); // 정의 목록 프로퍼티 조회
            listProperty.arraySize = definitions.Length; // 목록 크기 설정
            for (int i = 0; i < definitions.Length; i++) // 전달된 정의를 순서대로
            {
                listProperty.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i]; // 목록에 채워 넣기
            }
            serialized.ApplyModifiedPropertiesWithoutUndo(); // Undo 기록 없이 즉시 반영
            return database; // 완성된 테스트용 DB 반환
        }
    }
}
