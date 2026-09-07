using System; // 문자열 탐색 사용
using System.IO; // 소스 파일 읽기·쓰기 사용
using System.Text; // UTF-8 인코딩 사용
using UnityEditor; // 에디터 로드·에셋 재임포트 사용
using UnityEngine; // 패치 결과 로그 사용

namespace ProjectEta.EditorTools
{
    [InitializeOnLoad]
    public static class Day62StartingDeckPatcher
    {
        private const string RuntimeTargetPath = "Assets/ProjectEta/Scripts/Board/BoardInputController.cs"; // 시작 덱 구성 원본 경로
        private const string RuntimeMethodStartToken = "        private PieceDefinition[] GetPrototypeStartingCards()"; // 시작 카드 배열 메서드 시작 토큰
        private const string RuntimeNextMethodToken = "        private void HandleTurnChangedForCardDraw("; // 다음 메서드 시작 토큰
        private const string RuntimePatchedMarker = "시작 기본 카드 15장 + 킹 구성"; // 런타임 중복 패치 확인 문구
        private const string TestTargetPath = "Assets/ProjectEta/Tests/EditMode/CardFlowTests.cs"; // 기존 카드 흐름 테스트 경로
        private const string TestOldAssertion = "                Assert.AreEqual(1, context.RunState.Deck.DrawPile.Count); // 나머지 카드 1장은 드로우 더미"; // 기존 6장 시작 풀 검증 문구
        private const string TestPatchedMarker = "시작 기본 카드 구성 16장 검증"; // 테스트 중복 패치 확인 문구

        static Day62StartingDeckPatcher()
        {
            EditorApplication.delayCall += ApplyStartingDeckPatch; // 에디터 초기화 뒤 시작 덱 패치 예약
        }

        private static void ApplyStartingDeckPatch()
        {
            bool runtimeChanged = PatchRuntimeSource(); // 실제 시작 덱 구성 소스 패치
            bool testChanged = PatchCardFlowTests(); // 기존 시작 덱 테스트 기대값 패치

            if (!runtimeChanged && !testChanged)
            {
                return; // 변경할 소스가 없으면 재임포트 생략
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); // 변경된 소스 전체 재임포트
            Debug.Log("Day62 시작 덱 정리: Pawn 8, Rook 2, Bishop 2, Queen 1, Knight 2와 King 1장을 새 런 시작 풀에 적용했습니다."); // 자동 패치 완료 기록
        }

        private static bool PatchRuntimeSource()
        {
            if (!File.Exists(RuntimeTargetPath))
            {
                return false; // 대상 런타임 소스가 없으면 패치 생략
            }

            string source = File.ReadAllText(RuntimeTargetPath); // 현재 BoardInputController 소스 읽기

            if (source.IndexOf(RuntimePatchedMarker, StringComparison.Ordinal) >= 0)
            {
                return false; // 이미 시작 덱 구성이 적용됐으면 재패치 차단
            }

            int methodStart = source.IndexOf(RuntimeMethodStartToken, StringComparison.Ordinal); // 시작 카드 메서드 위치 탐색

            if (methodStart < 0)
            {
                return false; // 시작 카드 메서드를 찾지 못하면 안전하게 중단
            }

            int nextMethodStart = source.IndexOf(RuntimeNextMethodToken, methodStart, StringComparison.Ordinal); // 다음 메서드 위치 탐색

            if (nextMethodStart < 0)
            {
                return false; // 예상 소스 구조가 아니면 안전하게 중단
            }

            const string replacement =
                "        private PieceDefinition[] GetPrototypeStartingCards() // 시작 기본 카드 15장 + 킹 구성 배열을 반환하는 메서드\n" +
                "        {\n" +
                "            return new[] // 새 런의 기본 체스 카드 구성 16장 생성\n" +
                "            {\n" +
                "                _kingDefinition, // 필수 킹 1장\n" +
                "                _pawnDefinition, // 폰 1/8\n" +
                "                _pawnDefinition, // 폰 2/8\n" +
                "                _pawnDefinition, // 폰 3/8\n" +
                "                _pawnDefinition, // 폰 4/8\n" +
                "                _pawnDefinition, // 폰 5/8\n" +
                "                _pawnDefinition, // 폰 6/8\n" +
                "                _pawnDefinition, // 폰 7/8\n" +
                "                _pawnDefinition, // 폰 8/8\n" +
                "                _rookDefinition, // 룩 1/2\n" +
                "                _rookDefinition, // 룩 2/2\n" +
                "                _bishopDefinition, // 비숍 1/2\n" +
                "                _bishopDefinition, // 비숍 2/2\n" +
                "                _queenDefinition, // 퀸 1장\n" +
                "                _knightDefinition, // 나이트 1/2\n" +
                "                _knightDefinition // 나이트 2/2\n" +
                "            };\n" +
                "        }\n\n"; // 새 런 시작 카드 배열 교체 코드 구성

            string patchedSource = source.Substring(0, methodStart) + replacement + source.Substring(nextMethodStart); // 기존 6종 배열을 16장 구성으로 교체
            File.WriteAllText(RuntimeTargetPath, patchedSource, new UTF8Encoding(false)); // BOM 없는 UTF-8로 런타임 소스 저장
            AssetDatabase.ImportAsset(RuntimeTargetPath, ImportAssetOptions.ForceUpdate); // 수정된 런타임 소스 즉시 재임포트
            return true; // 런타임 소스 변경 성공 반환
        }

        private static bool PatchCardFlowTests()
        {
            if (!File.Exists(TestTargetPath))
            {
                return false; // 테스트 소스가 없으면 런타임 패치만 유지
            }

            string source = File.ReadAllText(TestTargetPath); // 현재 CardFlowTests 소스 읽기

            if (source.IndexOf(TestPatchedMarker, StringComparison.Ordinal) >= 0)
            {
                return false; // 이미 새 시작 덱 기대값이 적용됐으면 재패치 차단
            }

            int assertionIndex = source.IndexOf(TestOldAssertion, StringComparison.Ordinal); // 기존 드로우 더미 1장 검증 위치 탐색

            if (assertionIndex < 0)
            {
                return false; // 기존 테스트 구조를 찾지 못하면 테스트 수정 생략
            }

            const string replacement =
                "                Assert.AreEqual(16, context.RunState.Deck.OwnedCardPool.Count); // 시작 기본 카드 구성 16장 검증\n" +
                "                Assert.AreEqual(11, context.RunState.Deck.DrawPile.Count); // 킹 포함 시작 손패 5장 이후 드로우 더미 11장 검증\n" +
                "                Assert.AreEqual(8, context.RunState.Deck.OwnedCardPool.Count(card => card == context.Definitions[1])); // 폰 8장 검증\n" +
                "                Assert.AreEqual(2, context.RunState.Deck.OwnedCardPool.Count(card => card == context.Definitions[4])); // 룩 2장 검증\n" +
                "                Assert.AreEqual(2, context.RunState.Deck.OwnedCardPool.Count(card => card == context.Definitions[3])); // 비숍 2장 검증\n" +
                "                Assert.AreEqual(1, context.RunState.Deck.OwnedCardPool.Count(card => card == context.Definitions[5])); // 퀸 1장 검증\n" +
                "                Assert.AreEqual(2, context.RunState.Deck.OwnedCardPool.Count(card => card == context.Definitions[2])); // 나이트 2장 검증"; // 새 시작 덱 검증 코드 구성

            string patchedSource = source.Remove(assertionIndex, TestOldAssertion.Length).Insert(assertionIndex, replacement); // 기존 1장 기대값을 새 카드 수량 검증으로 교체
            File.WriteAllText(TestTargetPath, patchedSource, new UTF8Encoding(false)); // BOM 없는 UTF-8로 테스트 소스 저장
            AssetDatabase.ImportAsset(TestTargetPath, ImportAssetOptions.ForceUpdate); // 수정된 테스트 소스 즉시 재임포트
            return true; // 테스트 소스 변경 성공 반환
        }
    }
}
