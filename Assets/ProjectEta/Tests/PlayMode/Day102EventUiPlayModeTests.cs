using System; // 저장 콜백
using System.Collections; // 프레임 대기
using System.Collections.Generic; // 실제 카드 목록
using System.Linq; // 실제 버튼과 선택 조회
using NUnit.Framework; // 결과 검사
using UnityEngine; // 실제 화면 객체
using UnityEngine.UI; // 실제 버튼 입력
using UnityEngine.TestTools; // PlayMode 검사
using ProjectEta.Run; // 이벤트 실행
using ProjectEta.Pieces; // 실제 카드
using ProjectEta.Cards; // 리소스 카탈로그
using ProjectEta.Board; // 실제 보드 UI 기준
using ProjectEta.UI; // 실제 이벤트 오버레이
using ProjectEta.Meta; // 독립 영구 상태

namespace ProjectEta.Tests.PlayMode // 실제 이벤트 입력 검사
{ // 영역 시작
    public sealed class Day102EventUiPlayModeTests // 취소·중복·저장 실패·화면 연결
    { // 타입 시작
        private GameObject _root; // 시험 화면
        private StageBoardOverlayUI _overlay; // 실제 카드형 오버레이
        private RunState _run; // 독립 런
        private MetaProgressState _meta; // 디스크 없는 상태
        private IReadOnlyList<PieceDefinition> _cards; // 실제 원본 카드
        private RunEventPresenter _presenter; // 실제 화면 흐름
        [UnitySetUp] // 실제 UI 구성
        public IEnumerator SetUp() // 독립 시험 준비
        { // 메서드 시작
            _root = new GameObject("Day102EventUi"); // 화면 소유 루트
            BoardView board = _root.AddComponent<BoardView>(); // 실제 보드 좌표
            _overlay = _root.AddComponent<StageBoardOverlayUI>(); // 실제 표시 컴포넌트
            _overlay.Initialize(board); // 실제 선택지 Canvas 생성
            _meta = new MetaProgressState(); // 기존 영구 저장 보호
            _cards = Resources.Load<PlayerStartingDeckCatalog>("PlayerStartingDeck26").Cards; // 실제 카드 리소스
            _run = new RunState(2); // HP 비용과 회복 허용
            _run.CurrentRound = 4; // 이벤트 깊이
            var before = new StageNode("phase_2_before", new Vector2Int(4, 2), 3, "stage_3_battle"); // 이전 위치
            var target = new StageNode("phase_2_event_ui", new Vector2Int(4, 3), 4, "stage_4_event"); // 이벤트 위치
            var connections = new List<string>(); // 합법 연결 목록
            connections.Add(target.NodeId); // 이벤트 연결
            before.SetNextNodeIds(connections); // 실제 이동 규칙
            var nodes = new List<StageNode>(); // 실제 다음 노드 목록
            nodes.Add(target); // 이벤트 위치 등록
            _run.RouteMap.Configure(3, before, nodes); // 실제 경로
            Assert.That(_run.RouteMap.TryMoveKingTo(target.NodeId), Is.True); // 선택 잠금
            _run.Flow.EnterEvent(); // 실제 이벤트 흐름
            var unlocks = RunContentUnlockSnapshot.Capture(_run.RunId, _meta); // 기본 해금
            List<PieceDefinition> rewards = RunEventCardRules.Rewards(_run, _cards, unlocks, null, false); // 실제 유효 1성
            Assert.That(rewards.Count, Is.GreaterThanOrEqualTo(2)); // 교환·강화 시험 전제
            _run.Deck.AddToOwnedPool(rewards[0]); // 첫 대상
            _run.Deck.AddToOwnedPool(rewards[1]); // 다른 종류 대상
            RunEconomyService.Restore(_run, 100); // 비용 기준
            yield return null; // 실제 Canvas 생성 대기
        } // 메서드 종료
        [UnityTearDown] // 시험 소유 자원 정리
        public IEnumerator TearDown() // 다음 검사 상태 보호
        { // 메서드 시작
            _presenter?.Close(); // 남은 콜백 차단
            foreach (PieceDefinition card in _run.Deck.OwnedCardPool) // 강화 복제 정리
            { // 반복 시작
                if (card != null && (card.hideFlags & HideFlags.DontSave) != 0) UnityEngine.Object.Destroy(card); // 런타임 복제만 제거
            } // 반복 종료
            UnityEngine.Object.Destroy(_root); // 실제 오버레이와 이벤트 시스템 정리
            RunEconomyService.ResetForTests(); // 독립 경제 상태 정리
            yield return null; // 객체 제거 대기
        } // 메서드 종료
        [UnityTest] // 실제 버튼 취소 검사
        public IEnumerator TargetCancel_PreservesResourcesThenHpOnlyUpgradeAppliesOnce() // 대상 확정 전 지불 금지
        { // 메서드 시작
            Open("EV02", () => true, null); // 실제 HP·ATK 개별 강화
            Click("갑옷 보강"); // 강화 대상 선택 진입
            yield return null; // 대상 버튼 반영
            Click("취소"); // 첫 선택으로 복귀
            yield return null; // 취소 화면 반영
            Assert.That(RunEconomyService.GetOrCreate(_run).Currency, Is.EqualTo(100)); // 취소 비용 없음
            Assert.That(_run.BalanceData.eventHistory102.Count, Is.Zero); // 취소 완료 이력 없음
            Click("갑옷 보강"); // 다시 대상 선택
            yield return null; // 실제 카드 표시
            PieceDefinition card = RunEventCardRules.Targets(_run, _run.BalanceData.activeEvent102.definition.choices[0])[0]; // 실제 대상
            Click(card.DisplayName); // 카드 확정
            yield return null; // 결과 UI 표시
            Assert.That(_presenter.CurrentSubtitle, Is.EqualTo("선택 결과")); // 자동 종료 없이 결과 확인
            Assert.That(RunEconomyService.GetOrCreate(_run).Currency, Is.EqualTo(75)); // 정확한 비용
            Assert.That(_run.Deck.OwnedCardPool.Last().BaseHp, Is.EqualTo(card.BaseHp + 1)); // HP만 증가
            Assert.That(_run.Deck.OwnedCardPool.Last().BaseAtk, Is.EqualTo(card.BaseAtk)); // ATK 보존
        } // 메서드 종료
        [UnityTest] // 저장 실패와 중복 콜백 검사
        public IEnumerator SaveFailure_RetryKeepsRewardAndContinueOnlyRunsOnce() // 결과 재지급 방지
        { // 메서드 시작
            int saves = 0; // 저장 호출 순서
            int completions = 0; // 진행 호출 횟수
            Open("EV01", () => ++saves != 2, () => completions++); // 결과 첫 저장 실패
            Action repeatedChoice = _presenter.CurrentOptions[2].Callback; // 이전 선택 콜백
            Click("보급품을 챙긴다"); // Gold 보상
            yield return null; // 실패 안내 표시
            Assert.That(_presenter.CurrentTitle, Is.EqualTo("저장 확인 필요")); // 저장 실패 명시
            Assert.That(RunEconomyService.GetOrCreate(_run).Currency, Is.EqualTo(115)); // 메모리 효과 한 번
            repeatedChoice.Invoke(); // 같은 프레임 중복 입력
            Assert.That(RunEconomyService.GetOrCreate(_run).Currency, Is.EqualTo(115)); // 재지급 없음
            Click("저장 재시도"); // 같은 결과 저장
            yield return null; // 결과 화면 복원
            Action continueAgain = _presenter.CurrentOptions.Last().Callback; // 진행 콜백 보관
            Click("다음 경로"); // 실제 진행 확정
            continueAgain.Invoke(); // 남은 오래된 진행 콜백
            Assert.That(completions, Is.EqualTo(1)); // 중복 진행 차단
            Assert.That(_run.BalanceData.eventHistory102.Count, Is.EqualTo(1)); // 완료 이력 한 번
        } // 메서드 종료
        [UnityTest] // 실제 교환 화면 검사
        public IEnumerator Exchange_OnlyReplacesSelectedCardAndShowsResult() // 카드 선택과 결과 연결
        { // 메서드 시작
            Open("EV17", () => true, null); // 기획 교환 이벤트
            Click("거울에 기물을 비춘다"); // 대상 선택
            yield return null; // 대상 UI 생성
            PieceDefinition target = _run.Deck.OwnedCardPool[0]; // 실제 선택 원본
            int before = _run.Deck.OwnedCardPool.Count; // 카드 수
            Click(target.DisplayName); // 교환 확정
            yield return null; // 결과 UI
            RunEventRecord result = _run.BalanceData.activeEvent102.result; // 실제 손익
            Assert.That(result.removedCardId, Is.EqualTo(target.PieceId)); // 선택 카드 제거
            Assert.That(result.addedCardId, Is.Not.EqualTo(target.PieceId)); // 다른 카드 지급
            Assert.That(_run.Deck.OwnedCardPool.Count, Is.EqualTo(before)); // 수량 보존
            Assert.That(_presenter.CurrentOptions[0].Description, Does.Contain("제거:").And.Contain("획득:")); // 실제 교환 손익 표시
        } // 메서드 종료
        [UnityTest] // 35종의 실제 화면 구성
        public IEnumerator EveryDefinition_RendersThreeChoicesWithSafeExit() // 데이터만 추가한 누락 방지
        { // 메서드 시작
            foreach (RunEventDefinition definition in RunEventCatalog.All) // 전체 기획 이벤트
            { // 반복 시작
                _presenter?.Close(); // 이전 화면 소유 종료
                Open(definition.id, () => true, null); // 실제 같은 보드 UI에 표시
                Assert.That(_overlay.IsVisible, Is.True); // 오버레이 표시
                Assert.That(_presenter.CurrentOptions.Count, Is.EqualTo(3)); // 실제 A·B·C 버튼
                foreach (StageOverlayOption option in _presenter.CurrentOptions) Assert.That(option.Description, Is.Not.Empty); // 비용·효과·불가 이유
                Assert.That(_presenter.CurrentOptions.Any(option => option.Interactable), Is.True); // 진행 막힘 없음
                yield return null; // 재생성·레이아웃 반영
            } // 반복 종료
        } // 메서드 종료
        [UnityTest] // 비용과 결과의 항상 표시
        public IEnumerator EventDetails_AreVisibleWithoutHover() // 마우스 위치와 독립된 안내
        { // 메서드 시작
            Open("EV01", () => true, null); // 회복·강화·Gold 선택
            yield return null; // 실제 글자 생성
            string description = _run.BalanceData.activeEvent102.definition.choices[2].description; // 실제 Gold 효과
            Assert.That(_root.GetComponentsInChildren<Text>(true).Any(text => text.gameObject.activeInHierarchy && text.text == description), Is.True); // Hover 없이 비용·효과 노출
            Click("보급품을 챙긴다"); // 결과 확정
            yield return null; // 결과 글자 생성
            Assert.That(_root.GetComponentsInChildren<Text>(true).Any(text => text.gameObject.activeInHierarchy && text.text.Contains("Gold 100 → 115")), Is.True); // 실제 손익 상시 노출
        } // 메서드 종료
        [UnityTest] // 상세 카드에서 진행 버튼으로 재사용
        public IEnumerator ResultNavigation_RestoresCenteredTitleAfterDetailCard() // 이전 카드 위치 잔존 방지
        { // 메서드 시작
            Open("EV01", () => true, null); // 세 개 상세 카드 생성
            Click("보급품을 챙긴다"); // 두 개 결과 카드로 전환
            yield return null; // 재사용 UI 배치 반영
            Text label = _root.GetComponentsInChildren<Text>(true).First(text => text.gameObject.activeInHierarchy && text.text == "다음 경로"); // 실제 진행 제목
            Assert.That(label.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero)); // 기존 중앙 제목 복원
        } // 메서드 종료
        [UnityTest] // 대상 탐색 버튼의 실제 배치 검사
        public IEnumerator TargetPages_KeepButtonsSeparatedAndReachEveryCard() // 마지막 카드까지 가림 없는 탐색
        { // 메서드 시작
            PieceDefinition source = _run.Deck.OwnedCardPool[0]; // 강화 가능한 실제 원본
            for (int index = 0; index < 7; index++) _run.Deck.AddToOwnedPool(source); // 여러 페이지 대상 구성
            Open("EV02", () => true, null); // 대상 강화 이벤트
            Click("갑옷 보강"); // 실제 대상 화면 진입
            int visited = 0; // 확인한 대상 수
            int pages = 0; // 무한 탐색 방어
            while (true) // 첫 페이지부터 끝까지 확인
            { // 반복 시작
                yield return null; // 실제 레이아웃 반영
                Canvas.ForceUpdateCanvases(); // 글자와 카드 좌표 갱신
                Button[] buttons = _root.GetComponentsInChildren<Button>(true).Where(button => button.gameObject.activeInHierarchy).ToArray(); // 현재 화면의 실제 버튼
                Transform canvas = _root.GetComponentInChildren<Canvas>(true).transform; // 공통 비교 좌표
                for (int first = 0; first < buttons.Length; first++) // 모든 버튼 쌍 확인
                { // 반복 시작
                    Bounds left = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas, buttons[first].GetComponent<RectTransform>()); // 첫 카드 영역
                    for (int second = first + 1; second < buttons.Length; second++) // 다른 카드와 비교
                    { // 반복 시작
                        Bounds right = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas, buttons[second].GetComponent<RectTransform>()); // 다른 카드 영역
                        Assert.That(left.Intersects(right), Is.False, "대상·탐색·취소 버튼 겹침: " + first + "/" + second); // 클릭 영역과 글자 가림 방지
                    } // 반복 종료
                } // 반복 종료
                visited += _presenter.CurrentOptions.Count(option => option.ShowDescriptionOnCard); // 현재 페이지의 실제 카드 수
                StageOverlayOption next = _presenter.CurrentOptions.FirstOrDefault(option => option.Title == "다음 ▶"); // 다음 페이지 확인
                if (next == null) break; // 마지막 페이지 도달
                Assert.That(++pages, Is.LessThan(10)); // 잘못된 반복 차단
                Click("다음 ▶"); // 실제 다음 버튼 입력
            } // 반복 종료
            Assert.That(visited, Is.EqualTo(_run.Deck.OwnedCardPool.Count)); // 모든 보유 대상 도달
            Assert.That(_presenter.CurrentOptions.Any(option => option.Title == "◀ 이전"), Is.True); // 이전 이동 경로 유지
            Click("취소"); // 마지막 페이지에서도 비용 없는 복귀
            Assert.That(RunEconomyService.GetOrCreate(_run).Currency, Is.EqualTo(100)); // 탐색 중 비용 없음
        } // 메서드 종료
        private void Open(string id, Func<bool> save, Action complete) // 원하는 기획 화면 연결
        { // 메서드 시작
            RunEventSession session = RunEventService.GetOrCreate(_run); // 실제 저장 세션
            session.definition = RunEventCatalog.Find(id).Copy(); // 독립 기획 정의
            _presenter = new RunEventPresenter(_run, RunEconomyService.GetOrCreate(_run), _cards, null, RunContentUnlockSnapshot.Capture(_run.RunId, _meta), _meta, _overlay, save, complete); // 디스크 없는 실제 화면 흐름
            _presenter.Open(); // 저장 후 화면 진입
        } // 메서드 종료
        private void Click(string title) // 실제 생성 버튼 입력
        { // 메서드 시작
            Button button = _root.GetComponentsInChildren<Button>(true).FirstOrDefault(item => item.gameObject.activeInHierarchy && item.interactable && item.GetComponentsInChildren<Text>(true).Any(text => text.text == title)); // 표시된 활성 버튼
            Assert.That(button, Is.Not.Null, title); // 실제 UI 버튼 연결 확인
            button.onClick.Invoke(); // 실제 버튼 콜백 실행
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
