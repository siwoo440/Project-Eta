using System; // 문자열 비교 사용
using System.IO; // 테스트 소스 읽기·쓰기 사용
using System.Text; // UTF-8 저장 사용
using UnityEditor; // Editor 자동 패치 사용
using UnityEngine; // 개발 로그 사용

namespace ProjectEta.EditorTools // Editor 전용 도구 네임스페이스
{
    [InitializeOnLoad] // Editor 로드 직후 자동 실행
    public static class Day63CardFlowTestFixPatcher // 중복 카드 정의를 고려한 CardFlow 테스트 보정
    {
        private const string TestPath = "Assets/ProjectEta/Tests/EditMode/CardFlowTests.cs"; // 수정 대상 테스트 경로
        private const string OldCountAnchor = "                int drawCountBefore = context.RunState.Deck.DrawPile.Count; // 정리 전 드로우 더미 수 저장\n\n                bool result = context.BoardInput.TryDiscardHandCardToBottom(cardToDiscard); // 실제 손패 정리 실행"; // 기존 수량 저장 구간
        private const string NewCountAnchor = "                int drawCountBefore = context.RunState.Deck.DrawPile.Count; // 정리 전 드로우 더미 수 저장\n                int matchingHandCountBefore = context.RunState.Hand.Hand.Count(card => card == cardToDiscard); // 같은 정의 카드의 정리 전 손패 수 저장\n\n                bool result = context.BoardInput.TryDiscardHandCardToBottom(cardToDiscard); // 실제 손패 정리 실행"; // 중복 정의 수량 저장 구간
        private const string OldAssertion = "                Assert.IsFalse(context.RunState.Hand.Hand.Contains(cardToDiscard)); // 정리한 카드가 손패에 남아 있지 않아야 함"; // 중복 정의에서 잘못 실패하는 기존 검증
        private const string NewAssertion = "                Assert.AreEqual(matchingHandCountBefore - 1, context.RunState.Hand.Hand.Count(card => card == cardToDiscard)); // 같은 정의 카드가 여러 장이어도 정확히 1장만 손패에서 제거돼야 함\n                Assert.AreSame(cardToDiscard, context.RunState.Deck.DrawPile[0]); // 정리한 카드가 실제 드로우 더미 맨 아래에 있어야 함"; // 카드 한 장 이동을 정확히 검증하는 새 검증

        static Day63CardFlowTestFixPatcher() // 자동 패치 초기화
        {
            EditorApplication.delayCall += ApplyPatch; // Editor 준비 후 패치 예약
        }

        private static void ApplyPatch() // CardFlow 테스트 보정 적용
        {
            if (!File.Exists(TestPath)) // 테스트 파일 존재 확인
            {
                return; // 대상 파일이 없으면 종료
            }

            string source = File.ReadAllText(TestPath); // 현재 테스트 소스 읽기
            bool changed = false; // 실제 변경 여부 초기화

            if (source.IndexOf(NewCountAnchor, StringComparison.Ordinal) < 0) // 새 수량 검증이 아직 없으면
            {
                int countIndex = source.IndexOf(OldCountAnchor, StringComparison.Ordinal); // 기존 수량 구간 탐색

                if (countIndex >= 0) // 기존 수량 구간을 찾았으면
                {
                    source = source.Remove(countIndex, OldCountAnchor.Length).Insert(countIndex, NewCountAnchor); // 중복 카드 수량 저장 추가
                    changed = true; // 실제 변경 기록
                }
            }

            if (source.IndexOf(NewAssertion, StringComparison.Ordinal) < 0) // 새 검증 구문이 아직 없으면
            {
                int assertionIndex = source.IndexOf(OldAssertion, StringComparison.Ordinal); // 기존 잘못된 검증 탐색

                if (assertionIndex >= 0) // 기존 검증을 찾았으면
                {
                    source = source.Remove(assertionIndex, OldAssertion.Length).Insert(assertionIndex, NewAssertion); // 정확한 한 장 이동 검증으로 교체
                    changed = true; // 실제 변경 기록
                }
            }

            if (!changed) // 변경할 내용이 없으면
            {
                return; // 중복 저장과 재컴파일 방지
            }

            File.WriteAllText(TestPath, source, new UTF8Encoding(false)); // 수정 테스트 UTF-8 저장
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceUpdate); // 수정 테스트 즉시 재임포트
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); // 테스트 어셈블리 재컴파일 요청
            Debug.Log("Day63 CardFlowTests 수정: 중복 PieceDefinition 카드의 한 장 정리 검증으로 보정했습니다."); // 적용 결과 로그
        }
    }
}
