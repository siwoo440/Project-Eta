using System.Reflection; // ScriptableObject의 private 직렬화 필드를 테스트에서 직접 채우기 위한 네임스페이스
using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // CombatResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class CombatResolverTests // 12일차: 보드·입력과 무관하게 HP·ATK 판정 공식 자체만 검증하는 테스트 모음
    {
        private static PieceDefinition CreateDefinition(int baseHp, int baseAtk) // 테스트용 HP·ATK 값을 가진 기물 정의를 만드는 도우미 메서드
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 인스펙터 없이 사용할 임시 기물 정의 생성
            typeof(PieceDefinition).GetField("_baseHp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(definition, baseHp); // private HP 필드에 테스트 값 대입
            typeof(PieceDefinition).GetField("_baseAtk", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(definition, baseAtk); // private ATK 필드에 테스트 값 대입
            return definition; // 완성된 정의 반환
        }

        [Test] // 공격자의 ATK만큼 대상 HP가 정확히 감소하는지 확인하는 테스트
        public void ResolveAttack_ReducesDefenderHpByAttackerAtk()
        {
            var attacker = new PieceRuntimeState(CreateDefinition(baseHp: 3, baseAtk: 2), Vector2Int.zero, isPlayerPiece: true); // ATK 2 공격자
            var defender = new PieceRuntimeState(CreateDefinition(baseHp: 5, baseAtk: 0), Vector2Int.one, isPlayerPiece: false); // HP 5 대상

            var result = CombatResolver.ResolveAttack(attacker, defender); // 공격 판정 실행

            Assert.AreEqual(2, result.DamageDealt); // 실제 적용된 피해량이 공격자 ATK와 같아야 함
            Assert.AreEqual(3, defender.CurrentHp); // 5 - 2 = 3
            Assert.IsFalse(result.DefenderDied); // 아직 생존 상태여야 함
        }

        [Test] // 대상 HP가 정확히 0이 되면 사망으로 판정되는지 확인하는 테스트
        public void ResolveAttack_MarksDefenderDied_WhenHpReachesExactlyZero()
        {
            var attacker = new PieceRuntimeState(CreateDefinition(baseHp: 3, baseAtk: 3), Vector2Int.zero, isPlayerPiece: true); // ATK 3 공격자
            var defender = new PieceRuntimeState(CreateDefinition(baseHp: 3, baseAtk: 0), Vector2Int.one, isPlayerPiece: false); // HP 3 대상(정확히 0이 됨)

            var result = CombatResolver.ResolveAttack(attacker, defender); // 공격 판정 실행

            Assert.AreEqual(0, defender.CurrentHp); // HP가 정확히 0이어야 함
            Assert.IsTrue(result.DefenderDied); // 사망으로 판정돼야 함
            Assert.IsTrue(defender.IsDead); // PieceRuntimeState.IsDead도 true여야 함
        }

        [Test] // 초과 피해를 입어도 HP가 음수로 내려가지 않는지 확인하는 테스트
        public void ResolveAttack_OverkillDamage_DoesNotGoBelowZero()
        {
            var attacker = new PieceRuntimeState(CreateDefinition(baseHp: 3, baseAtk: 10), Vector2Int.zero, isPlayerPiece: true); // ATK 10(과다 피해) 공격자
            var defender = new PieceRuntimeState(CreateDefinition(baseHp: 2, baseAtk: 0), Vector2Int.one, isPlayerPiece: false); // HP 2 대상

            var result = CombatResolver.ResolveAttack(attacker, defender); // 공격 판정 실행

            Assert.AreEqual(0, defender.CurrentHp); // 음수 대신 0에서 멈춰야 함(PieceRuntimeState.CurrentHp의 보정)
            Assert.IsTrue(result.DefenderDied); // 사망으로 판정돼야 함
        }
    }
}
