using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 테스트 코드를 모아두는 네임스페이스
{
    public class PieceRuntimeStateTests // PieceRuntimeState 동작을 검증하는 테스트 클래스
    {
        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void CurrentHp_Does_Not_Go_Below_Zero() // 체력이 음수로 내려가지 않는지 확인하는 테스트
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 임시 기물 정의 생성
            var runtimeState = new PieceRuntimeState(definition, Vector2Int.zero, isPlayerPiece: true); // 테스트용 런타임 상태 생성

            runtimeState.CurrentHp = -5; // 음수 체력을 대입 시도

            Assert.AreEqual(0, runtimeState.CurrentHp); // 0으로 제한됐는지 검증
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void IsDead_True_When_Hp_Is_Zero() // 체력이 0일 때 사망 판정이 맞는지 확인하는 테스트
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 임시 기물 정의 생성
            var runtimeState = new PieceRuntimeState(definition, Vector2Int.zero, isPlayerPiece: true) // 테스트용 런타임 상태 생성
            {
                CurrentHp = 0 // 체력을 0으로 설정
            };

            Assert.IsTrue(runtimeState.IsDead); // 사망 상태로 판정되는지 검증
        }
    }
}
