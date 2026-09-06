using System.Reflection; // private 직렬화 필드 설정
using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // ScriptableObject·Vector2Int 사용
using ProjectEta.Battle; // CombatResult·DamageContext 사용
using ProjectEta.King; // 킹 공통 상태·공격형 패시브 사용
using ProjectEta.Meta; // 48일차 영구 해금 상태 사용
using ProjectEta.Pieces; // PieceDefinition·PieceRuntimeState 사용
using ProjectEta.Run; // RunState 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day49KingAbilityTests
    {
        [Test]
        public void KingRunState_DefaultsToDefaultKing()
        {
            var state = new KingRunState(); // 새 킹 런 상태 생성

            Assert.AreEqual(KingArchetype.Default, state.Archetype); // 기본 킹 선택 상태 검증
            Assert.AreEqual(0, state.RageStacks); // 초기 격노 없음 검증
        }

        [Test]
        public void KingRunStateService_SameRun_ReturnsSameState()
        {
            var runState = new RunState(3); // 테스트 런 생성

            KingRunState first = KingRunStateService.Get(runState); // 첫 킹 상태 조회
            KingRunState second = KingRunStateService.Get(runState); // 같은 런 킹 상태 재조회

            Assert.AreSame(first, second); // 같은 RunState는 동일 킹 상태 유지 검증
        }

        [Test]
        public void AttackKing_DirectKill_AddsOneRage()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Attack); // 공격형 킹 선택
            PieceRuntimeState king = CreatePiece(PieceMovementType.King, true); // 아군 킹 생성
            PieceRuntimeState enemy = CreatePiece(PieceMovementType.Pawn, false); // 적 기물 생성
            var result = new CombatResult(king, enemy, 1, true); // 킹 직접 처치 결과 생성

            bool granted = AttackKingAbility.HandleAfterAttack(state, result); // 공격 종료 패시브 처리

            Assert.IsTrue(granted); // 격노 획득 성공 검증
            Assert.AreEqual(1, state.RageStacks); // 처치 후 격노 1스택 검증
        }

        [Test]
        public void AttackKing_Rage_AddsDamageAndConsumesAllStacks()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Attack); // 공격형 킹 선택
            state.TryAddRage(); // 격노 1스택 추가
            state.TryAddRage(); // 격노 2스택 추가
            PieceRuntimeState king = CreatePiece(PieceMovementType.King, true); // 아군 킹 생성
            PieceRuntimeState enemy = CreatePiece(PieceMovementType.Pawn, false); // 적 기물 생성
            var damageContext = new DamageContext(enemy, king, 1); // 기본 피해 1 공격 컨텍스트 생성

            int bonus = AttackKingAbility.ApplyBeforeDamage(state, damageContext); // 다음 공격 격노 보너스 적용

            Assert.AreEqual(2, bonus); // 격노 2스택 피해 보너스 검증
            Assert.AreEqual(3, damageContext.Amount); // 기본 1 + 격노 2 최종 피해 검증
            Assert.AreEqual(0, state.RageStacks); // 공격 후 격노 전부 소비 검증
        }

        [Test]
        public void AttackKing_NonKingAttack_DoesNotConsumeRage()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Attack); // 공격형 킹 선택
            state.TryAddRage(); // 격노 1스택 추가
            PieceRuntimeState pawn = CreatePiece(PieceMovementType.Pawn, true); // 아군 일반 기물 생성
            PieceRuntimeState enemy = CreatePiece(PieceMovementType.Pawn, false); // 적 기물 생성
            var damageContext = new DamageContext(enemy, pawn, 1); // 일반 기물 피해 컨텍스트 생성

            int bonus = AttackKingAbility.ApplyBeforeDamage(state, damageContext); // 공격형 패시브 적용 시도

            Assert.AreEqual(0, bonus); // 일반 기물 공격 보너스 없음 검증
            Assert.AreEqual(1, damageContext.Amount); // 원래 피해 유지 검증
            Assert.AreEqual(1, state.RageStacks); // 격노 미소비 검증
        }

        [Test]
        public void AttackKing_Rage_IsCappedAtTwo()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Attack); // 공격형 킹 선택

            state.TryAddRage(); // 격노 1스택 추가
            state.TryAddRage(); // 격노 2스택 추가
            bool third = state.TryAddRage(); // 세 번째 격노 추가 시도

            Assert.IsFalse(third); // 최대 스택 초과 차단 검증
            Assert.AreEqual(2, state.RageStacks); // 최대 2스택 유지 검증
        }

        [Test]
        public void KingRunState_ResetBattleScopedState_ClearsRageButKeepsSelection()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Attack); // 공격형 킹 선택
            state.TryAddRage(); // 전투 중 격노 추가

            state.ResetBattleScopedState(); // 전투 종료 임시 상태 초기화

            Assert.AreEqual(KingArchetype.Attack, state.Archetype); // 킹 선택 유지 검증
            Assert.AreEqual(0, state.RageStacks); // 전투 격노 초기화 검증
        }

        [Test]
        public void KingUnlockRules_AttackKing_RequiresPermanentUnlock()
        {
            var progress = new MetaProgressState(); // 빈 영구 진행 상태 생성

            bool beforeUnlock = KingUnlockRules.CanSelect(KingArchetype.Attack, progress); // 공격형 해금 전 선택 가능 여부 조회
            progress.Unlock(MetaUnlockType.King, KingUnlockIds.Attack); // 48일차 공격형 킹 영구 해금
            bool afterUnlock = KingUnlockRules.CanSelect(KingArchetype.Attack, progress); // 공격형 해금 후 선택 가능 여부 조회

            Assert.IsFalse(beforeUnlock); // 해금 전 선택 차단 검증
            Assert.IsTrue(afterUnlock); // 해금 후 선택 허용 검증
        }

        [Test]
        public void KingUnlockRules_DefaultKing_IsAlwaysSelectable()
        {
            var progress = new MetaProgressState(); // 빈 영구 진행 상태 생성

            bool selectable = KingUnlockRules.CanSelect(KingArchetype.Default, progress); // 기본 킹 선택 가능 여부 조회

            Assert.IsTrue(selectable); // 기본 킹 상시 사용 검증
        }

        private static PieceRuntimeState CreatePiece(PieceMovementType movementType, bool isPlayerPiece)
        {
            PieceDefinition definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트 기물 정의 생성
            SetPrivateField(definition, "_movementType", movementType); // 테스트 이동 타입 지정
            SetPrivateField(definition, "_baseHp", 3); // 테스트 기본 HP 지정
            SetPrivateField(definition, "_baseAtk", 1); // 테스트 기본 ATK 지정
            return new PieceRuntimeState(definition, Vector2Int.zero, isPlayerPiece); // 테스트 런타임 기물 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance); // private 인스턴스 필드 조회
            Assert.IsNotNull(field); // 테스트 대상 필드 존재 검증
            field.SetValue(target, value); // 테스트 값 대입
        }
    }
}
