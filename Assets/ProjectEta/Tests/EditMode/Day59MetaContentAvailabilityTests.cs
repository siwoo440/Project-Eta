using System.IO; // 소스 회귀 검사 사용
using System.Reflection; // 테스트용 PieceDefinition 필드 설정
using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // ScriptableObject 사용
using ProjectEta.King; // KingUnlockRules·KingArchetype 사용
using ProjectEta.Meta; // MetaProgressState·MetaUnlockType·가용성 서비스 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.Run; // RunContentUnlockSnapshot·CardRewardGenerator 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day59MetaContentAvailabilityTests
    {
        [Test]
        public void RunContentUnlockSnapshot_CapturesAndRestoresMetaUnlocks()
        {
            var progress = new MetaProgressState(); // 영구 진행 상태 생성
            progress.Unlock(MetaUnlockType.Piece, "piece_future_test"); // 테스트 기물 영구 해금
            progress.Unlock(MetaUnlockType.King, KingUnlockIds.Attack); // 공격형 킹 영구 해금
            progress.Unlock(MetaUnlockType.Passive, "passive_future_test"); // 테스트 패시브 영구 해금

            RunContentUnlockSnapshot snapshot = RunContentUnlockSnapshot.Capture("run_day59", progress); // 새 런 해금 Snapshot 생성
            RunContentUnlockSnapshot restored = RunContentUnlockSnapshot.FromSaveData(snapshot.ToSaveData()); // Snapshot 저장 DTO 왕복

            Assert.AreEqual("run_day59", restored.RunId); // 런 ID 복원 검증
            Assert.IsTrue(restored.IsUnlocked(MetaUnlockType.Piece, "piece_future_test")); // 기물 해금 복원 검증
            Assert.IsTrue(restored.IsUnlocked(MetaUnlockType.King, KingUnlockIds.Attack)); // 킹 해금 복원 검증
            Assert.IsTrue(restored.IsUnlocked(MetaUnlockType.Passive, "passive_future_test")); // 패시브 해금 복원 검증
        }

        [Test]
        public void MetaContentAvailability_BaseContentNeedsNoUnlock()
        {
            RunContentUnlockSnapshot snapshot = RunContentUnlockSnapshot.Capture("run_base", new MetaProgressState()); // 해금 없는 런 Snapshot 생성

            bool available = MetaContentAvailabilityService.IsRequiredUnlockAvailable(MetaUnlockType.Piece, string.Empty, snapshot); // 해금 ID 없는 기본 콘텐츠 확인

            Assert.IsTrue(available); // 기본 콘텐츠 상시 사용 가능 검증
        }

        [Test]
        public void MetaContentAvailability_RequiredUnlockUsesRunSnapshot()
        {
            var progress = new MetaProgressState(); // 영구 진행 상태 생성
            RunContentUnlockSnapshot lockedSnapshot = RunContentUnlockSnapshot.Capture("run_locked", progress); // 해금 전 Snapshot 생성
            progress.Unlock(MetaUnlockType.Piece, "piece_unlock_test"); // Snapshot 생성 후 영구 해금
            RunContentUnlockSnapshot unlockedSnapshot = RunContentUnlockSnapshot.Capture("run_unlocked", progress); // 다음 런 Snapshot 생성

            Assert.IsFalse(MetaContentAvailabilityService.IsRequiredUnlockAvailable(MetaUnlockType.Piece, "piece_unlock_test", lockedSnapshot)); // 기존 런에 소급 적용되지 않음 검증
            Assert.IsTrue(MetaContentAvailabilityService.IsRequiredUnlockAvailable(MetaUnlockType.Piece, "piece_unlock_test", unlockedSnapshot)); // 다음 런 해금 적용 검증
        }

        [Test]
        public void MetaContentAvailability_PieceDefinitionRequirementUsesRunSnapshot()
        {
            PieceDefinition basePiece = CreatePiece("base_piece", string.Empty); // 기본 보상 기물 생성
            PieceDefinition gatedPiece = CreatePiece("gated_piece", "piece_unlock_test"); // 영구 해금 필요 기물 생성
            var progress = new MetaProgressState(); // 영구 진행 상태 생성
            RunContentUnlockSnapshot lockedSnapshot = RunContentUnlockSnapshot.Capture("run_piece_locked", progress); // 해금 전 Snapshot 생성
            progress.Unlock(MetaUnlockType.Piece, "piece_unlock_test"); // 다음 런용 기물 해금
            RunContentUnlockSnapshot unlockedSnapshot = RunContentUnlockSnapshot.Capture("run_piece_unlocked", progress); // 해금 후 Snapshot 생성

            Assert.IsTrue(MetaContentAvailabilityService.IsPieceAvailable(basePiece, lockedSnapshot)); // 기본 기물 상시 사용 검증
            Assert.IsFalse(MetaContentAvailabilityService.IsPieceAvailable(gatedPiece, lockedSnapshot)); // 현재 런 잠긴 기물 차단 검증
            Assert.IsTrue(MetaContentAvailabilityService.IsPieceAvailable(gatedPiece, unlockedSnapshot)); // 다음 런 해금 기물 허용 검증

            Object.DestroyImmediate(basePiece); // 테스트 ScriptableObject 정리
            Object.DestroyImmediate(gatedPiece); // 테스트 ScriptableObject 정리
        }

        [Test]
        public void CardRewardGenerator_UsesMetaContentAvailabilitySnapshot()
        {
            string sourcePath = Path.Combine(Application.dataPath, "ProjectEta/Scripts/Run/CardRewardGenerator.cs"); // 카드 보상 생성기 소스 경로
            string source = File.ReadAllText(sourcePath); // 현재 카드 보상 생성기 소스 읽기

            StringAssert.Contains("RunContentUnlockSnapshotService.GetOrCreateForActiveRun", source); // 현재 런 Snapshot 조회 연결 검증
            StringAssert.Contains("MetaContentAvailabilityService.IsPieceAvailable", source); // 영구 해금 기물 필터 연결 검증
        }

        [Test]
        public void KingUnlockRules_SnapshotDoesNotChangeAfterMetaProgressChanges()
        {
            var progress = new MetaProgressState(); // 영구 진행 상태 생성
            RunContentUnlockSnapshot snapshot = RunContentUnlockSnapshot.Capture("run_king", progress); // 공격형 킹 해금 전 런 Snapshot 생성
            progress.Unlock(MetaUnlockType.King, KingUnlockIds.Attack); // 런 시작 후 영구 진행 상태 변경

            Assert.IsFalse(KingUnlockRules.CanSelect(KingArchetype.Attack, snapshot)); // 진행 중 런에는 공격형 킹 소급 해금 차단 검증
            Assert.IsTrue(KingUnlockRules.CanSelect(KingArchetype.Default, snapshot)); // 기본 킹 상시 선택 검증
        }

        private static PieceDefinition CreatePiece(string pieceId, string requiredMetaUnlockId)
        {
            PieceDefinition definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트 기물 정의 생성
            SetPrivateField(definition, "_pieceId", pieceId); // PieceId 설정
            SetPrivateField(definition, "_requiredMetaUnlockId", requiredMetaUnlockId); // 영구 해금 요구 ID 설정
            return definition; // 테스트 기물 반환
        }

        private static void SetPrivateField<T>(PieceDefinition definition, string fieldName, T value)
        {
            FieldInfo field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 비공개 직렬화 필드 조회
            Assert.IsNotNull(field, $"{fieldName} 필드를 찾지 못했습니다."); // 필드 존재 검증
            field.SetValue(definition, value); // 테스트 값 적용
        }
    }
}
