using System.IO; // 이미지 파일 검사
using System.Reflection; // 비공개 UI 생성 호출
using NUnit.Framework; // EditMode 테스트 도구
using UnityEngine; // 게임 오브젝트와 좌표 사용
using ProjectEta.Boss; // 보스 체력 UI 사용
using ProjectEta.UI; // 경로 지도 UI 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{ // 네임스페이스 범위 시작
    public sealed class Day82UiReconstructionTests // 82일차 추가 UI 재구성 테스트
    { // 테스트 클래스 범위 시작
        [Test] // 보스 체력바 상단 배치 검증
        public void BossHealthUI_생성직후_상단18픽셀여백을사용한다() // 직접 생성 레이아웃 확인
        { // 테스트 범위 시작
            GameObject root = new GameObject("BossHealthLayoutTest"); // 테스트 호스트 생성
            BossHealthUI ui = root.AddComponent<BossHealthUI>(); // 실제 보스 체력 UI 생성

            try // 테스트 자원 정리 보장
            { // 보호 범위 시작
                InvokePrivateMethod(ui, "EnsureCanvas"); // 실제 패널 생성 호출
                RectTransform panel = GameObject.Find("BossHealthPanel").GetComponent<RectTransform>(); // 생성 패널 조회

                Assert.That(panel.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f))); // 상단 중앙 앵커 검증
                Assert.That(panel.anchorMax, Is.EqualTo(new Vector2(0.5f, 1f))); // 상단 중앙 앵커 범위 검증
                Assert.That(panel.anchoredPosition, Is.EqualTo(new Vector2(0f, -18f))); // 상단 여백 검증
            } // 보호 범위 종료
            finally // 테스트 자원 정리 단계
            { // 정리 범위 시작
                Object.DestroyImmediate(root); // 테스트 호스트 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료

        [Test] // 지도 패널 우측 배치 검증
        public void RouteMapUI_헤더를_우측세로패널로생성한다() // 기존 가로 헤더 제거 확인
        { // 테스트 범위 시작
            GameObject root = new GameObject("RouteMapLayoutTest"); // 테스트 호스트 생성
            Day66RouteMapUI ui = root.AddComponent<Day66RouteMapUI>(); // 실제 지도 UI 생성

            try // 테스트 자원 정리 보장
            { // 보호 범위 시작
                InvokePrivateMethod(ui, "EnsureUI"); // EditMode에서 실제 패널 생성 호출
                Transform panelTransform = root.transform.Find("Day66RouteMapCanvas/RouteMapSidePanel"); // 비활성 우측 지도 패널 조회

                Assert.That(panelTransform, Is.Not.Null); // 우측 패널 생성 검증
                RectTransform rect = panelTransform.GetComponent<RectTransform>(); // 패널 배치 정보 조회
                Assert.That(rect.anchorMin, Is.EqualTo(Vector2.one)); // 우상단 앵커 검증
                Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one)); // 우상단 고정 검증
                Assert.That(rect.pivot, Is.EqualTo(Vector2.one)); // 우상단 피벗 검증
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(-18f, -18f))); // 화면 가장자리 여백 검증
                Assert.That(rect.sizeDelta.x, Is.InRange(300f, 360f)); // 좁은 패널 폭 검증
            } // 보호 범위 종료
            finally // 테스트 자원 정리 단계
            { // 정리 범위 시작
                Object.DestroyImmediate(root); // 테스트 호스트 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료

        [Test] // 지도 범례 행 검증
        public void RouteMapUI_여섯종류_아이콘행을생성한다() // 전투부터 보스까지 범례 확인
        { // 테스트 범위 시작
            GameObject root = new GameObject("RouteMapLegendTest"); // 테스트 호스트 생성
            Day66RouteMapUI ui = root.AddComponent<Day66RouteMapUI>(); // 실제 지도 UI 생성
            string[] rowNames = { "RouteLegendRow_Battle", "RouteLegendRow_Elite", "RouteLegendRow_Reward", "RouteLegendRow_Shop", "RouteLegendRow_Event", "RouteLegendRow_Boss" }; // 필수 범례 행 이름

            try // 테스트 자원 정리 보장
            { // 보호 범위 시작
                InvokePrivateMethod(ui, "EnsureUI"); // EditMode에서 실제 범례 생성 호출
                for (int index = 0; index < rowNames.Length; index++) // 모든 범례 행 순회
                { // 반복 범위 시작
                    Transform row = root.transform.Find($"Day66RouteMapCanvas/RouteMapSidePanel/{rowNames[index]}"); // 비활성 범례 행 조회
                    Assert.That(row, Is.Not.Null, rowNames[index]); // 범례 행 존재 검증
                } // 반복 범위 종료
            } // 보호 범위 종료
            finally // 테스트 자원 정리 단계
            { // 정리 범위 시작
                Object.DestroyImmediate(root); // 테스트 호스트 제거
            } // 정리 범위 종료
        } // 테스트 범위 종료

        [Test] // 지도 아이콘 파일 검증
        public void RouteMapIcons_여섯이미지가_프로젝트에존재한다() // 프로젝트 포함 여부 확인
        { // 테스트 범위 시작
            string root = Path.Combine(Application.dataPath, "ProjectEta/Resources/UI/RouteMap"); // 지도 아이콘 폴더 계산
            string[] fileNames = { "RouteBattleIcon.png", "RouteEliteIcon.png", "RouteRewardIcon.png", "RouteShopIcon.png", "RouteEventIcon.png", "RouteBossIcon.png" }; // 필수 아이콘 파일 이름

            for (int index = 0; index < fileNames.Length; index++) // 모든 이미지 순회
            { // 반복 범위 시작
                Assert.That(File.Exists(Path.Combine(root, fileNames[index])), Is.True, fileNames[index]); // 이미지 존재 검증
            } // 반복 범위 종료
        } // 테스트 범위 종료

        [Test] // 중복 보정 소스 제거 검증
        public void BossHealthLayout_직접배치뒤_별도보정파일을사용하지않는다() // 레이아웃 소유권 단일화 확인
        { // 테스트 범위 시작
            string path = Path.Combine(Application.dataPath, "ProjectEta/Scripts/UI/BossHealthTopLineFix.cs"); // 기존 보정 파일 경로 계산

            Assert.That(File.Exists(path), Is.False); // 중복 보정 파일 제거 검증
        } // 테스트 범위 종료

        private static void InvokePrivateMethod(object target, string methodName) // 비공개 메서드 호출 도우미
        { // 도우미 범위 시작
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic); // 메서드 정보 조회
            Assert.That(method, Is.Not.Null, methodName); // 메서드 존재 검증
            method.Invoke(target, null); // 대상 메서드 호출
        } // 도우미 범위 종료
    } // 테스트 클래스 범위 종료
} // 네임스페이스 범위 종료
