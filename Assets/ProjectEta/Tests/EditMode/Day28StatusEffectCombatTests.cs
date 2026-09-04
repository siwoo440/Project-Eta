using UnityEditor; // AssetDatabase, SerializedObject를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // StatusEffectTickResolver를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState, MovementResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState, StatusEffectDefinition 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day28StatusEffectCombatTests // 28일차 독·화상 틱 피해와 기절·속박 이동 제한을 검증하는 테스트 모음
    {
        private const string PieceDatabasePath = "Assets/ProjectEta/Data/PieceDatabase.asset"; // 실제 기물 DB 경로(기절·속박 이동 제한 검증에 실제 기물을 사용)

        [Test] // 독(중첩형)은 중첩 수에 비례해 턴 종료 피해가 커지는지 검증
        public void ResolveTurnEndDamage_Poison_ScalesWithStackCount()
        {
            var poison = CreateStatusDefinition(StatusEffectType.Poison, StatusStackMode.StacksAdd, maxStacks: 3, durationTurns: 3, tickDamagePerStack: 1); // 중첩당 1피해 독(임시값)
            var piece = new PieceRuntimeState(CreatePieceDefinition("test_target", hp: 10), Vector2Int.zero, true); // 체력 10짜리 테스트용 기물

            piece.ApplyStatus(poison); // 1중첩
            piece.ApplyStatus(poison); // 2중첩

            int damage = StatusEffectTickResolver.ResolveTurnEndDamage(piece); // 이번 턴 종료 피해 계산

            Assert.AreEqual(2, damage, "2중첩 독은 중첩당 1피해씩 총 2피해를 줘야 합니다."); // 2중첩 * 1피해
            Assert.AreEqual(8, piece.CurrentHp, "실제 HP에 피해가 반영되어야 합니다."); // 10 - 2
        }

        [Test] // 화상(갱신형)은 중첩과 무관하게 고정 피해를 매 턴 반복하는지 검증
        public void ResolveTurnEndDamage_Burn_DealsFixedDamageRegardlessOfReapply()
        {
            var burn = CreateStatusDefinition(StatusEffectType.Burn, StatusStackMode.RefreshDuration, maxStacks: 1, durationTurns: 3, tickDamagePerStack: 1); // 고정 1피해 화상(임시값)
            var piece = new PieceRuntimeState(CreatePieceDefinition("test_target", hp: 10), Vector2Int.zero, true); // 체력 10짜리 테스트용 기물

            piece.ApplyStatus(burn); // 최초 적용
            piece.ApplyStatus(burn); // 재적용(갱신형이므로 중첩되지 않음)

            int damage = StatusEffectTickResolver.ResolveTurnEndDamage(piece); // 이번 턴 종료 피해 계산

            Assert.AreEqual(1, damage, "갱신형 화상은 재적용해도 1피해로 고정되어야 합니다."); // 중첩 없이 고정 1피해
        }

        [Test] // 독과 화상이 동시에 걸려 있으면 같은 턴에 피해가 합산되는지 검증
        public void ResolveTurnEndDamage_PoisonAndBurnTogether_StackDamageAdditively()
        {
            var poison = CreateStatusDefinition(StatusEffectType.Poison, StatusStackMode.StacksAdd, maxStacks: 3, durationTurns: 3, tickDamagePerStack: 1); // 독 정의
            var burn = CreateStatusDefinition(StatusEffectType.Burn, StatusStackMode.RefreshDuration, maxStacks: 1, durationTurns: 3, tickDamagePerStack: 1); // 화상 정의
            var piece = new PieceRuntimeState(CreatePieceDefinition("test_target", hp: 10), Vector2Int.zero, true); // 체력 10짜리 테스트용 기물

            piece.ApplyStatus(poison); // 독 1중첩
            piece.ApplyStatus(poison); // 독 2중첩
            piece.ApplyStatus(burn); // 화상 적용

            int damage = StatusEffectTickResolver.ResolveTurnEndDamage(piece); // 이번 턴 종료 피해 계산(독 2 + 화상 1)

            Assert.AreEqual(3, damage, "독과 화상 피해가 같은 턴에 합산되어야 합니다."); // 2 + 1
        }

        [Test] // 지속 턴이 없는(만료) 독은 더 이상 피해를 주지 않는지 검증
        public void ResolveTurnEndDamage_AfterStatusExpires_NoLongerDealsDamage()
        {
            var poison = CreateStatusDefinition(StatusEffectType.Poison, StatusStackMode.StacksAdd, maxStacks: 3, durationTurns: 1, tickDamagePerStack: 1); // 1턴만 지속되는 독
            var piece = new PieceRuntimeState(CreatePieceDefinition("test_target", hp: 10), Vector2Int.zero, true); // 테스트용 기물

            piece.ApplyStatus(poison); // 독 적용
            StatusEffectTickResolver.ResolveTurnEndDamage(piece); // 1턴차 피해 정산(HP 9)
            piece.TickStatusEffects(); // 지속 턴 소진 -> 상태 제거

            int damageAfterExpiry = StatusEffectTickResolver.ResolveTurnEndDamage(piece); // 만료 이후 정산 시도

            Assert.AreEqual(0, damageAfterExpiry, "만료된 독은 더 이상 피해를 주지 않아야 합니다."); // 상태가 없으므로 0
            Assert.AreEqual(9, piece.CurrentHp); // 만료 전 1턴치 피해만 반영
        }

        [Test] // 기절 상태의 기물은 이동·공격 후보가 모두 사라지는지 검증
        public void GetReachableTiles_StunnedPiece_HasNoMoveOrAttackCandidates()
        {
            var database = LoadDatabase(); // 실제 PieceDatabase 로드
            var rookDefinition = database.FindById("rook"); // 슬라이드형 대표 기물
            var stun = CreateStatusDefinition(StatusEffectType.Stun, StatusStackMode.RefreshDuration, maxStacks: 1, durationTurns: 1, tickDamagePerStack: 0); // 기절 정의(피해 없음)

            var board = new BoardState(); // 빈 보드
            var origin = new Vector2Int(4, 4); // 중앙 좌표
            var rook = new PieceRuntimeState(rookDefinition, origin, true); // 테스트용 룩
            board.GetTile(origin).OccupyingPiece = rook; // 보드에 배치
            var enemy = new PieceRuntimeState(rookDefinition, new Vector2Int(4, 5), false); // 바로 위 인접 적
            board.GetTile(enemy.BoardPosition).OccupyingPiece = enemy; // 적 배치

            Assert.Greater(MovementResolver.GetReachableTiles(rook, board).AttackTiles.Count, 0, "기절 전에는 정상적으로 공격 후보가 있어야 합니다."); // 사전 조건 확인

            rook.ApplyStatus(stun); // 기절 적용
            var result = MovementResolver.GetReachableTiles(rook, board); // 기절 이후 이동·공격 후보 재계산

            Assert.AreEqual(0, result.MoveTiles.Count, "기절 중에는 이동 후보가 없어야 합니다."); // 이동 후보 없음
            Assert.AreEqual(0, result.AttackTiles.Count, "기절 중에는 공격 후보도 없어야 합니다."); // 공격 후보도 없음
        }

        [Test] // 속박 상태의 기물은 이동 후보만 사라지고 공격 후보는 유지되는지 검증
        public void GetReachableTiles_RootedPiece_KeepsAttackTilesButClearsMoveTiles()
        {
            var database = LoadDatabase(); // 실제 PieceDatabase 로드
            var rookDefinition = database.FindById("rook"); // 슬라이드형 대표 기물
            var root = CreateStatusDefinition(StatusEffectType.Root, StatusStackMode.RefreshDuration, maxStacks: 1, durationTurns: 1, tickDamagePerStack: 0); // 속박 정의(피해 없음)

            var board = new BoardState(); // 빈 보드
            var origin = new Vector2Int(4, 4); // 중앙 좌표
            var rook = new PieceRuntimeState(rookDefinition, origin, true); // 테스트용 룩
            board.GetTile(origin).OccupyingPiece = rook; // 보드에 배치
            var enemy = new PieceRuntimeState(rookDefinition, new Vector2Int(4, 5), false); // 바로 위 인접 적
            board.GetTile(enemy.BoardPosition).OccupyingPiece = enemy; // 적 배치

            var beforeRoot = MovementResolver.GetReachableTiles(rook, board); // 속박 전 기준값
            Assert.Greater(beforeRoot.MoveTiles.Count, 0, "속박 전에는 이동 후보가 있어야 합니다."); // 사전 조건 확인
            Assert.Contains(enemy.BoardPosition, beforeRoot.AttackTiles); // 사전 조건: 인접 적 공격 가능

            rook.ApplyStatus(root); // 속박 적용
            var result = MovementResolver.GetReachableTiles(rook, board); // 속박 이후 재계산

            Assert.AreEqual(0, result.MoveTiles.Count, "속박 중에는 이동 후보가 없어야 합니다."); // 이동 금지
            Assert.Contains(enemy.BoardPosition, result.AttackTiles, "속박 중에도 현재 위치에서 가능한 공격은 유지되어야 합니다."); // 공격은 허용
        }

        [Test] // 기절이 지속 턴 만료로 풀리면 다시 정상적으로 행동할 수 있는지 검증
        public void TickStatusEffects_StunExpires_RestoresMovementAndAttack()
        {
            var database = LoadDatabase(); // 실제 PieceDatabase 로드
            var rookDefinition = database.FindById("rook"); // 슬라이드형 대표 기물
            var stun = CreateStatusDefinition(StatusEffectType.Stun, StatusStackMode.RefreshDuration, maxStacks: 1, durationTurns: 1, tickDamagePerStack: 0); // 1턴짜리 기절

            var board = new BoardState(); // 빈 보드
            var origin = new Vector2Int(4, 4); // 중앙 좌표
            var rook = new PieceRuntimeState(rookDefinition, origin, true); // 테스트용 룩
            board.GetTile(origin).OccupyingPiece = rook; // 보드에 배치

            rook.ApplyStatus(stun); // 기절 적용
            Assert.IsFalse(rook.CanMove); // 기절 중에는 이동 불가
            Assert.IsFalse(rook.CanAttack); // 기절 중에는 공격도 불가

            rook.TickStatusEffects(); // 1턴 경과로 기절 만료

            Assert.IsFalse(rook.HasStatus(StatusEffectType.Stun), "지속 턴이 끝나면 기절이 제거되어야 합니다."); // 상태 제거 확인
            Assert.IsTrue(rook.CanMove, "기절이 풀리면 다시 이동할 수 있어야 합니다."); // 이동 가능 복구
            Assert.IsTrue(rook.CanAttack, "기절이 풀리면 다시 공격할 수 있어야 합니다."); // 공격 가능 복구
            Assert.Greater(MovementResolver.GetReachableTiles(rook, board).MoveTiles.Count, 0, "기절이 풀리면 이동 후보가 다시 계산되어야 합니다."); // 실제 후보 복구 확인
        }

        private static PieceDatabase LoadDatabase() // 실제 DB 로드 공통 도우미(Day26 테스트와 동일한 패턴)
        {
            var database = AssetDatabase.LoadAssetAtPath<PieceDatabase>(PieceDatabasePath); // 지정 경로에서 DB 로드
            Assert.IsNotNull(database, "PieceDatabase.asset이 존재해야 합니다."); // DB 누락 시 즉시 실패
            return database; // 정상 DB 반환
        }

        private static StatusEffectDefinition CreateStatusDefinition(StatusEffectType statusType, StatusStackMode stackMode, int maxStacks, int durationTurns, int tickDamagePerStack) // 테스트 전용 상태 이상 정의 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<StatusEffectDefinition>(); // 인스턴스만 생성(에셋으로 저장하지 않음)
            var serialized = new SerializedObject(definition); // private 직렬화 필드에 접근하기 위한 SerializedObject
            serialized.FindProperty("_statusType").intValue = (int)statusType; // 상태 종류 설정
            serialized.FindProperty("_displayName").stringValue = statusType.ToString(); // 표시 이름 설정
            serialized.FindProperty("_stackMode").enumValueIndex = (int)stackMode; // 중첩 방식 설정
            serialized.FindProperty("_maxStacks").intValue = maxStacks; // 최대 중첩 수 설정
            serialized.FindProperty("_defaultDurationTurns").intValue = durationTurns; // 기본 지속 턴 설정
            serialized.FindProperty("_tickDamagePerStack").intValue = tickDamagePerStack; // 28일차: 중첩당 틱 피해(임시값) 설정
            serialized.FindProperty("_description").stringValue = "28일차 테스트용 임시 상태 이상 정의."; // 설명 설정
            serialized.ApplyModifiedPropertiesWithoutUndo(); // Undo 기록 없이 즉시 반영
            return definition; // 완성된 테스트용 정의 반환
        }

        private static PieceDefinition CreatePieceDefinition(string pieceId, int hp) // 테스트 전용 기물 정의 생성 도우미(면역 없음, 임의 체력)
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 인스턴스만 생성(에셋으로 저장하지 않음)
            var serialized = new SerializedObject(definition); // private 직렬화 필드에 접근하기 위한 SerializedObject
            serialized.FindProperty("_pieceId").stringValue = pieceId; // 식별자 설정
            serialized.FindProperty("_displayName").stringValue = pieceId; // 표시 이름 설정
            serialized.FindProperty("_baseHp").intValue = hp; // 테스트용 체력
            serialized.FindProperty("_baseAtk").intValue = 1; // 테스트용 임시 공격력
            serialized.FindProperty("_occupancySize").vector2IntValue = Vector2Int.one; // 1칸 점유
            serialized.FindProperty("_description").stringValue = "28일차 테스트용 임시 기물 정의."; // 설명 설정
            serialized.ApplyModifiedPropertiesWithoutUndo(); // Undo 기록 없이 즉시 반영
            return definition; // 완성된 테스트용 정의 반환
        }
    }
}
