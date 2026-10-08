using System.Collections; // 프레임 단위 검증 사용
using System.Reflection; // 실제 화면 구성 경로 접근
using NUnit.Framework; // 동작 검증 사용
using ProjectEta.Meta; // 실제 성장 화면 사용
using UnityEngine; // 검증 오브젝트 사용
using UnityEngine.EventSystems; // 실제 스크롤 입력 사용
using UnityEngine.TestTools; // 플레이 모드 검증 사용
using UnityEngine.UI; // 목록과 버튼 사용

namespace ProjectEta.Tests.PlayMode // 런타임 UI 검증 범위
{ // 네임스페이스 시작
    public sealed class Day98MenuUiPlayModeTests // 성장 목록 입력 회귀 검증
    { // 클래스 시작
        private GameObject _root; // 검증 화면 루트
        private MetaProgressPanelController _ui; // 실제 성장 화면

        [UnitySetUp] // 검증 준비 표시
        public IEnumerator SetUp() // 실제 성장 화면 구성
        { // 메서드 시작
            _root = new GameObject("Day98MetaTest", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 실제 화면 Canvas 생성
            _root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; // 화면 기준 입력 영역
            CanvasScaler scaler = _root.GetComponent<CanvasScaler>(); // 해상도 보정 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 보정 방식
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 실제 기준 해상도
            _ui = _root.AddComponent<MetaProgressPanelController>(); // 실제 성장 컨트롤러 생성
            Invoke("BuildUI"); // 실제 화면 구성
            MetaProgressState progress = new MetaProgressState(); // 파일 저장 없는 진행 생성
            progress.AddTokens(100); // 해금 가능 검증 토큰
            Field("_progress").SetValue(_ui, progress); // 검증 진행 연결
            Invoke("RebuildItems"); // 실제 전체 해금 목록 구성
            Invoke("RefreshCategoryButtons"); // 실제 카테고리 표시 구성
            Invoke("RefreshDetails"); // 실제 상세 정보 구성
            yield return null; // 런타임 레이아웃 반영 대기
        } // 메서드 종료

        [UnityTearDown] // 검증 정리 표시
        public IEnumerator TearDown() // 검증 화면 정리
        { // 메서드 시작
            Object.Destroy(_root); // 검증 Canvas 제거
            yield return null; // 삭제 완료 대기
        } // 메서드 종료

        [UnityTest] // 실제 입력 검증 표시
        public IEnumerator WheelAndSelection_KeepLastUnlockReachableAndCategoryStartsAtTop() // 휠과 항목 선택 및 카테고리 전환 검증
        { // 메서드 시작
            ScrollRect scroll = (ScrollRect)Field("_itemScroll").GetValue(_ui); // 실제 목록 스크롤 조회
            GameObject eventHost = new GameObject("MetaScrollEvents", typeof(EventSystem)); // 휠 이벤트 호스트 생성
            eventHost.transform.SetParent(_root.transform, false); // 검증 루트 연결
            PointerEventData wheel = new PointerEventData(eventHost.GetComponent<EventSystem>()); // 실제 휠 이벤트 생성
            wheel.scrollDelta = new Vector2(0f, -40f); // 목록 아래쪽 이동 입력
            scroll.OnScroll(wheel); // 실제 휠 처리 경로 실행
            yield return null; // 스크롤 경계 보정 대기
            Assert.AreEqual(0f, scroll.verticalNormalizedPosition, 0.01f); // 목록 맨 아래 이동 확인
            Transform last = scroll.content.GetChild(scroll.content.childCount - 1); // 마지막 실제 해금 항목
            string selectedName = last.name; // 선택 항목 식별자 보관
            last.GetComponent<Button>().onClick.Invoke(); // 마지막 항목 상세 선택
            yield return null; // 이전 카드 삭제와 새 카드 생성 대기
            Assert.AreEqual(10, scroll.content.childCount); // 재생성 후 목록 중복 없음 확인
            Assert.AreEqual(0f, scroll.verticalNormalizedPosition, 0.01f); // 상세 선택 후 목록 위치 보존 확인
            Assert.AreEqual(selectedName, scroll.content.GetChild(9).name); // 마지막 항목 접근 유지 확인
            Assert.AreEqual("전략형 킹", ((Text)Field("_detailTitleText").GetValue(_ui)).text); // 실제 상세 선택 결과 확인
            Transform category = _root.transform.Find("MetaProgressPanel_Day58/CategoryNavigation/Category_King"); // King 카테고리 버튼 조회
            category.GetComponent<Button>().onClick.Invoke(); // 실제 카테고리 전환
            yield return null; // 필터 목록 재생성 대기
            Assert.AreEqual(3, scroll.content.childCount); // King 3개 필터 결과 확인
            Bounds first = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, scroll.content.GetChild(0)); // 첫 항목 경계 계산
            Assert.LessOrEqual(first.max.y, scroll.viewport.rect.yMax); // 카테고리 첫 항목 표시 확인
            Assert.GreaterOrEqual(first.min.y, scroll.viewport.rect.yMin); // 첫 항목 입력 영역 확인
        } // 메서드 종료

        private FieldInfo Field(string name) // 실제 화면 필드 조회 보조
        { // 메서드 시작
            return typeof(MetaProgressPanelController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic); // 화면 참조 조회
        } // 메서드 종료

        private object Invoke(string method) // 실제 화면 메서드 호출 보조
        { // 메서드 시작
            return typeof(MetaProgressPanelController).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_ui, null); // 실제 화면 구성 경로 실행
        } // 메서드 종료
    } // 클래스 종료
} // 네임스페이스 종료
