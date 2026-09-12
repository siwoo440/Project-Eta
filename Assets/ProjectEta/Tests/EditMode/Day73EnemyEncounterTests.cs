using System.Collections.Generic; // List<T> 사용
using System.Reflection; // 테스트용 private 필드 설정 사용
using NUnit.Framework; // NUnit 테스트 사용
using UnityEngine; // ScriptableObject·Vector2Int 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.Round; // RoundDefinition 사용
using ProjectEta.Run; // Enemy Encounter 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day73EnemyEncounterTests
    {
        private readonly List<Object> _createdObjects = new List<Object>(); // 생성 ScriptableObject 정리 목록

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null) Object.DestroyImmediate(_createdObjects[i]); // 테스트 생성 에셋 정리
            }

            _createdObjects.Clear(); // 정리 목록 초기화
        }

        [Test]
        public void SameSeed_CreatesSameEncounter()
        {
            PieceDefinition weak = CreatePiece("weak", 1, 1, PieceCategory.Basic, PieceMovementType.Pawn); // 약한 적 생성
            PieceDefinition medium = CreatePiece("medium", 2, 2, PieceCategory.Basic, PieceMovementType.Knight); // 중간 적 생성
            PieceDefinition strong = CreatePiece("strong", 4, 3, PieceCategory.Monster, PieceMovementType.Rook); // 강한 적 생성
            RoundDefinition round = CreateRound(weak, medium); // 기준 Round 생성
            var pool = new List<PieceDefinition> { weak, medium, strong }; // 후보 Pool 생성

            EnemyEncounterResult first = EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, 12345, 2, 4, "node_a"); // 첫 Encounter 생성
            EnemyEncounterResult second = EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, 12345, 2, 4, "node_a"); // 같은 조건 Encounter 재생성

            Assert.AreEqual(first.Seed, second.Seed); // Seed 동일성 검증
            Assert.AreEqual(first.Spawns.Count, second.Spawns.Count); // 적 수 동일성 검증

            for (int i = 0; i < first.Spawns.Count; i++)
            {
                Assert.AreEqual(first.Spawns[i].Piece.PieceId, second.Spawns[i].Piece.PieceId); // 적 종류 동일성 검증
                Assert.AreEqual(first.Spawns[i].Position, second.Spawns[i].Position); // 배치 좌표 동일성 검증
            }
        }

        [Test]
        public void DifferentPhase_CreatesDifferentSeed()
        {
            PieceDefinition weak = CreatePiece("weak", 1, 1, PieceCategory.Basic, PieceMovementType.Pawn); // 기준 적 생성
            RoundDefinition round = CreateRound(weak); // 기준 Round 생성
            var pool = new List<PieceDefinition> { weak }; // 후보 Pool 생성

            EnemyEncounterResult early = EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, 100, 1, 2, "node_a"); // 초반 Encounter 생성
            EnemyEncounterResult late = EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, 100, 5, 2, "node_a"); // 후반 Encounter 생성

            Assert.AreNotEqual(early.Seed, late.Seed); // Phase별 Seed 차이 검증
        }

        [Test]
        public void EliteEncounter_HasAtLeastNormalCountAndThreat()
        {
            PieceDefinition weak = CreatePiece("weak", 1, 1, PieceCategory.Basic, PieceMovementType.Pawn); // 약한 적 생성
            PieceDefinition medium = CreatePiece("medium", 3, 2, PieceCategory.Special, PieceMovementType.Knight); // 중간 적 생성
            PieceDefinition strong = CreatePiece("strong", 5, 4, PieceCategory.Monster, PieceMovementType.Rook); // 강한 적 생성
            RoundDefinition round = CreateRound(weak, medium); // 기준 Round 생성
            var pool = new List<PieceDefinition> { weak, medium, strong }; // 후보 Pool 생성

            EnemyEncounterResult normal = EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, 777, 4, 7, "node_b"); // 일반 Encounter 생성
            EnemyEncounterResult elite = EnemyEncounterGenerator.Generate(pool, round, StageType.Elite, 777, 4, 7, "node_b"); // Elite Encounter 생성

            Assert.GreaterOrEqual(elite.Spawns.Count, normal.Spawns.Count); // Elite 적 수 검증
            Assert.GreaterOrEqual(elite.ThreatScore, normal.ThreatScore); // Elite 위협도 검증
        }

        [Test]
        public void ForbiddenPieces_AreExcludedFromGeneratedEncounter()
        {
            PieceDefinition normal = CreatePiece("normal", 2, 2, PieceCategory.Basic, PieceMovementType.Pawn); // 정상 적 생성
            PieceDefinition king = CreatePiece("king", 9, 9, PieceCategory.Special, PieceMovementType.King); // 금지 King 생성
            PieceDefinition fusion = CreatePiece("fusion", 9, 9, PieceCategory.Fusion, PieceMovementType.Rook); // 금지 Fusion 생성
            PieceDefinition boss = CreatePiece("boss", 9, 9, PieceCategory.Boss, PieceMovementType.Rook); // 금지 Boss 생성
            RoundDefinition round = CreateRound(normal); // 기준 Round 생성
            var pool = new List<PieceDefinition> { normal, king, fusion, boss }; // 혼합 후보 Pool 생성

            EnemyEncounterResult encounter = EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, 55, 5, 9, "node_c"); // Encounter 생성

            for (int i = 0; i < encounter.Spawns.Count; i++)
            {
                Assert.AreSame(normal, encounter.Spawns[i].Piece); // 금지 기물 제외 검증
            }
        }

        [Test]
        public void Validator_DetectsDuplicateCell()
        {
            PieceDefinition normal = CreatePiece("normal", 2, 2, PieceCategory.Basic, PieceMovementType.Pawn); // 정상 적 생성
            var spawns = new List<EnemyEncounterSpawn>
            {
                new EnemyEncounterSpawn(normal, new Vector2Int(1, 8)), // 첫 배치 생성
                new EnemyEncounterSpawn(normal, new Vector2Int(1, 8)) // 중복 Cell 배치 생성
            };
            var encounter = new EnemyEncounterResult(1, StageType.Battle, 1, 1, "node", spawns, 10); // 중복 Encounter 생성

            IReadOnlyList<EnemyEncounterContentIssue> issues = EnemyEncounterContentValidator.Validate(encounter); // Encounter 검증
            bool foundDuplicate = false; // 중복 발견 여부 초기화

            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].IssueType == EnemyEncounterContentIssueType.DuplicateCell) foundDuplicate = true; // 중복 Cell 문제 확인
            }

            Assert.IsTrue(foundDuplicate); // 중복 Cell 감지 검증
        }

        private PieceDefinition CreatePiece(string pieceId, int hp, int atk, PieceCategory category, PieceMovementType movementType)
        {
            PieceDefinition piece = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트 기물 생성
            _createdObjects.Add(piece); // 정리 목록 등록
            SetPrivateField(piece, "_pieceId", pieceId); // Piece ID 설정
            SetPrivateField(piece, "_displayName", pieceId); // 표시 이름 설정
            SetPrivateField(piece, "_baseHp", hp); // HP 설정
            SetPrivateField(piece, "_baseAtk", atk); // 공격력 설정
            SetPrivateField(piece, "_category", category); // 카테고리 설정
            SetPrivateField(piece, "_movementType", movementType); // 이동 타입 설정
            return piece; // 테스트 기물 반환
        }

        private RoundDefinition CreateRound(params PieceDefinition[] enemies)
        {
            RoundDefinition round = ScriptableObject.CreateInstance<RoundDefinition>(); // 테스트 Round 생성
            _createdObjects.Add(round); // 정리 목록 등록

            var enemySpawns = new List<EnemySpawnDefinition>(); // 적 배치 목록 생성
            for (int i = 0; i < enemies.Length; i++)
            {
                Vector2Int position = new Vector2Int(1 + i * 2, 8 + i % 2); // 적 진영 배치 좌표 생성
                enemySpawns.Add(new EnemySpawnDefinition(enemies[i].PieceId, position, 0)); // 기존 Round 스폰 형식으로 등록
            }

            SetPrivateField(round, "_initialEnemies", enemySpawns); // Round 적 배치 설정
            SetPrivateField(round, "_reinforcements", new List<EnemySpawnDefinition>()); // 빈 증원 목록 설정
            return round; // 테스트 Round 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            System.Type currentType = target.GetType(); // 현재 타입 조회

            while (currentType != null)
            {
                FieldInfo field = currentType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 현재 타입 private 필드 조회
                if (field != null)
                {
                    field.SetValue(target, value); // 테스트 값 주입
                    return; // 주입 완료 종료
                }

                currentType = currentType.BaseType; // 부모 타입으로 이동
            }

            Assert.Fail($"필드를 찾지 못했습니다: {fieldName}"); // 테스트 데이터 구성 실패 처리
        }
    }
}
