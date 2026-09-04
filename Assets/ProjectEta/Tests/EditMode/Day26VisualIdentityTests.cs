using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject 생성을 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceView를 사용하기 위한 네임스페이스
using ProjectEta.UI; // CardView 리플렉션 테스트를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day26VisualIdentityTests // 카드 약칭과 모델 키를 검증하는 테스트 모음
    {
        [Test] // 카드 약칭이 이름 앞 3글자로 표시되는지 검증
        public void CardPlaceholder_UsesFirstThreeLettersOfPieceId()
        {
            var pawn = CreateDefinition("pawn"); // 폰 정의 생성
            var cannon = CreateDefinition("cannon"); // 캐논 정의 생성
            var king = CreateDefinition("king"); // 킹 정의 생성

            Assert.AreEqual("paw", InvokePlaceholder(pawn)); // pawn은 paw여야 함
            Assert.AreEqual("can", InvokePlaceholder(cannon)); // cannon은 can이어야 함
            Assert.AreEqual("kin", InvokePlaceholder(king)); // king은 kin이어야 함
        }

        [Test] // 전용 모델 키가 PieceId를 그대로 사용하도록 검증
        public void PieceView_ModelKey_UsesPieceId()
        {
            Assert.AreEqual("cannon", PieceView.GetModelKey(CreateDefinition("cannon"))); // 캐논은 cannon 분기 사용
            Assert.AreEqual("chameleon", PieceView.GetModelKey(CreateDefinition("chameleon"))); // 카멜레온은 chameleon 분기 사용
            Assert.AreEqual("squirrel", PieceView.GetModelKey(CreateDefinition("squirrel"))); // 스쿼럴은 squirrel 분기 사용
        }

        private static PieceDefinition CreateDefinition(string pieceId) // 테스트용 PieceDefinition 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 런타임 임시 정의 생성
            var field = typeof(PieceDefinition).GetField("_pieceId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); // private _pieceId 필드 탐색
            field.SetValue(definition, pieceId); // PieceId 값 주입
            return definition; // 완성 정의 반환
        }

        private static string InvokePlaceholder(PieceDefinition definition) // private placeholder 메서드를 리플렉션으로 호출하는 도우미
        {
            var method = typeof(CardView).GetMethod("GetPortraitPlaceholder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); // private 정적 메서드 탐색
            return (string)method.Invoke(null, new object[] { definition }); // 약칭 결과 반환
        }
    }
}
