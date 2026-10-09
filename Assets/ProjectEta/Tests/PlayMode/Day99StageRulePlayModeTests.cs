using System.Collections; // 프레임 대기
using System.Reflection; // 저장 접근 없는 전투 참조 주입
using NUnit.Framework; // 동작 검사
using ProjectEta.Battle; // 실제 턴 변경
using ProjectEta.Board; // 실제 보드·입력
using ProjectEta.Run; // 실제 스테이지 적용
using UnityEngine; // 런타임 오브젝트
using UnityEngine.TestTools; // 플레이 모드 검사

namespace ProjectEta.Tests.PlayMode // 99일차 런타임 검사 영역
{ // 영역 시작
    public sealed class Day99StageRulePlayModeTests // 저장 규칙의 실제 전투 적용 검사
    { // 타입 시작
        private GameObject _boardRoot; // 실제 보드 호스트
        private GameObject _battleRoot; // 저장 접근을 막은 전투 호스트
        private StageBattleRuntimeController _runtime; // 실제 스테이지 런타임
        private RunState _run; // 독립 런 상태
        private TurnManager _turns; // 실제 턴 상태
        private BattleController _battle; // 실제 전투 참조
        [UnitySetUp] // 검사 준비 지정
        public IEnumerator SetUp() // 파일 저장 없이 전투 연결
        { // 메서드 시작
            _run = new RunState(3); // 시험 런 생성
            _run.CurrentRound = 2; // 정예 깊이 설정
            _run.RouteMap.Configure(2, new StageNode("phase_1_elite", Vector2Int.zero, 2, "stage_2_elite"), new StageNode[0]); // 현재 정예 노드 연결
            _turns = new TurnManager(); // 실제 턴 생성
            _boardRoot = new GameObject("Day99StageBoard"); // 보드 호스트 생성
            BoardView view = _boardRoot.AddComponent<BoardView>(); // 실제 보드 표시 생성
            BoardInputController input = _boardRoot.AddComponent<BoardInputController>(); // 실제 입력 생성
            view.Bind(_run.Board); // 보드 상태 연결
            input.Bind(_run, view, _turns, new BattleHooks()); // 실제 배치 경로 연결
            _battleRoot = new GameObject("Day99StageBattle"); // 전투 참조 호스트 생성
            _battleRoot.SetActive(false); // 자동 저장 읽기 차단
            _battle = _battleRoot.AddComponent<BattleController>(); // 실제 전투 객체 생성
            SetField("_runState", _run); // 시험 런 연결
            SetField("_turnManager", _turns); // 시험 턴 연결
            _runtime = _boardRoot.AddComponent<StageBattleRuntimeController>(); // 실제 스테이지 런타임 생성
            yield return null; // 보드 초기화 반영
        } // 메서드 종료
        [UnityTearDown] // 검사 정리 지정
        public IEnumerator Cleanup() // 생성한 보드와 전투 정리
        { // 메서드 시작
            Object.Destroy(_boardRoot); // 보드·런타임 정리
            Object.Destroy(_battleRoot); // 전투 참조 정리
            yield return null; // 파괴 완료 대기
        } // 메서드 종료
        [UnityTest] // 실제 전투 규칙 검사
        public IEnumerator Elite_AppliesSavedLimitAndSpawnsEarlyReinforcementsOnce() // 저장 제한·증원 실제 소비 확인
        { // 메서드 시작
            StageDefinition definition = StageDefinitionCatalog.Resolve("stage_2_elite", 2); // 정예 정의 조회
            StageRuleSnapshot rules = RunStageRuleService.GetOrCreate(_run, definition, 1); // 안내 규칙 저장
            rules.turnLimit = 23; // 에셋과 다른 저장 제한 재현
            Assert.That(_runtime.Configure(_battle, definition), Is.True); // 실제 전투 구성 실행
            Assert.That(_runtime.RoundDefinition, Is.Not.SameAs(definition.RoundDefinition)); // 원본 에셋 보호 확인
            Assert.That(_runtime.TurnLimit, Is.EqualTo(23)); // 저장된 제한 적용 확인
            Assert.That((int)typeof(BattleController).GetField("_turnLimitTestValue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_battle), Is.EqualTo(23)); // 전투 종료 판정 제한 연결 확인
            int initial = _run.Board.CountPieces(false); // 시작 편성 수 조회
            _turns.MarkInitialKingPlaced(); // 초기 배치 조건 충족
            Assert.That(_turns.TryEndDeploymentTurn(), Is.True); // 실제 첫 턴 시작
            Assert.That(_run.Board.CountPieces(false), Is.EqualTo(initial)); // 첫 턴 증원 없음 확인
            AdvanceTurn(); // 실제 두 번째 턴 진입
            Assert.That(_run.Board.CountPieces(false), Is.EqualTo(initial + 1)); // 2턴 증원 생성 확인
            AdvanceTurn(); // 실제 세 번째 턴 진입
            Assert.That(_run.Board.CountPieces(false), Is.EqualTo(initial + 1)); // 같은 증원 재생성 차단 확인
            AdvanceTurn(); // 실제 네 번째 턴 진입
            Assert.That(_run.Board.CountPieces(false), Is.EqualTo(initial + 2)); // 4턴 증원 생성 확인
            var runtimeRound = _runtime.RoundDefinition; // 정리 대상 라운드 참조
            Object.Destroy(_runtime); // 스테이지 종료 정리 실행
            yield return null; // 컴포넌트 파괴 대기
            yield return null; // 소유한 라운드 파괴 대기
            Assert.That(runtimeRound == null, Is.True); // 런타임 규칙 자원 해제 확인
        } // 메서드 종료
        private void AdvanceTurn() // 실제 턴 이벤트 발생
        { // 메서드 시작
            Assert.That(_turns.TryCompletePlayerAction(), Is.True); // 플레이어 턴 완료
            Assert.That(_turns.CompleteEnemyTurn(), Is.True); // 적 턴 완료와 증원 이벤트 실행
        } // 메서드 종료
        private void SetField(string name, object value) // 시험 런 참조 연결
        { // 메서드 시작
            typeof(BattleController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_battle, value); // 자동 저장 없는 실제 전투 참조 주입
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
