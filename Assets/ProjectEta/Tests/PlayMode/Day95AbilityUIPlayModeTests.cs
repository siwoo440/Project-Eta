using System.Collections; // 플레이 모드 대기
using NUnit.Framework; // 동작 결과 검증
using UnityEngine; // 게임 오브젝트
using UnityEngine.TestTools; // 프레임 단위 테스트
using UnityEngine.UI; // 실제 버튼 클릭
using ProjectEta.Abilities; // 능력 서비스
using ProjectEta.Battle; // 턴 상태
using ProjectEta.Board; // 보드 입력
using ProjectEta.Pieces; // 실제 기물
using ProjectEta.Run; // 런 상태
using ProjectEta.UI; // 실제 능력 UI

namespace ProjectEta.Tests.PlayMode // 런타임 UI 통합 검증
{ // 범위 시작
    public sealed class Day95AbilityUIPlayModeTests // 버튼부터 전투 행동까지 검증
    { // 범위 시작
        private GameObject _root; // 테스트 보드 루트
        private GameObject _uiRoot; // 테스트 능력 UI
        private BoardInputController _input; // 실제 입력 컨트롤러
        private RunState _run; // 테스트 런
        private TurnManager _turns; // 테스트 턴
        private PieceDatabase _database; // 빌드용 원본 데이터

        [UnitySetUp] // 테스트 시작 시 런타임 구성
        public IEnumerator SetUp() // 실제 게임 컴포넌트 연결
        { // 범위 시작
            AbilityBoardRegistry.Clear(); // 다른 테스트 보드 제거
            _database = Resources.Load<RunContentCatalog>("RunContent").PieceDatabase; // 게임용 기물 정의 연결
            _root = new GameObject("Day95PlayModeBoard"); // 테스트 보드 생성
            var view = _root.AddComponent<BoardView>(); // 실제 보드 표시
            _input = _root.AddComponent<BoardInputController>(); // 실제 보드 입력
            _run = new RunState(3); // 외부 저장을 사용하지 않는 런
            _turns = new TurnManager(); // 실제 배치 턴 상태
            view.Bind(_run.Board); // 보드 데이터 연결
            _input.Bind(_run, view, _turns, new BattleHooks()); // 입력과 훅 연결
            _uiRoot = new GameObject("Day95PlayModeAbilityUI"); // 테스트 패널 루트
            _uiRoot.AddComponent<PieceAbilityOverlayUI>(); // 실제 능력 패널 생성
            yield return null; // 패널 초기화 대기
        } // 범위 종료

        [UnityTearDown] // 테스트 종료 후 정리
        public IEnumerator TearDown() // 테스트 오브젝트 제거
        { // 범위 시작
            Object.Destroy(_uiRoot); // 생성한 UI 제거
            Object.Destroy(_root); // 보드와 입력 제거
            yield return null; // 파괴 완료 대기
            AbilityBoardRegistry.Clear(); // 테스트 상태 정리
        } // 범위 종료

        [UnityTest] // 버튼과 대상 클릭 연동 검증
        public IEnumerator Sage_회복버튼선택후아군클릭으로회복하고행동권1회소비() // 실제 선택 UI 행동 검증
        { // 범위 시작
            var sage = Spawn("grand_sage", 3, 3); // 대현자 배치
            var ally = Spawn("grand_rider", 4, 3); // 회복 대상 배치
            ally.CurrentHp = 4; // 회복 가능한 체력
            _turns.MarkInitialKingPlaced(); // 초기 배치 필수 조건 충족
            _turns.TryEndDeploymentTurn(); // 일반 플레이어 턴 진입
            Assert.That(_input.TrySelectPieceAt(sage.BoardPosition), Is.True); // 대현자 선택
            yield return null; // UI 선택 반영 대기
            var button = _uiRoot.transform.Find("PieceAbilityOverlayCanvas/AbilityPanel/Ability_회복").GetComponent<Button>(); // 실제 회복 버튼
            Assert.That(button.interactable, Is.True); // 실행 가능한 버튼 확인
            button.onClick.Invoke(); // 회복 선택 버튼 실행
            Assert.That(_input.PendingAbility.AbilityId, Is.EqualTo(FiveStarAbilityIds.SageHeal)); // 회복 모드 진입 확인
            Assert.That(_input.TryUseSelectedAbilityAt(ally.BoardPosition), Is.True); // 아군 보드 클릭 실행
            Assert.That(ally.CurrentHp, Is.EqualTo(6)); // HP 2 회복 확인
            Assert.That(_turns.CurrentState, Is.EqualTo(TurnState.EnemyTurn)); // 행동권 1회 소비 확인
            yield return null; // 턴 입력 정리 대기
            Assert.That(_input.PendingAbility, Is.Null); // 사용 후 선택 초기화
        } // 범위 종료

        [UnityTest] // 배치 형태 버튼 검증
        public IEnumerator Phantom_배치형태버튼은변환하지만이동하지않음() // 자유 배치 선택 검증
        { // 범위 시작
            var phantom = Spawn("phantom_general", 3, 3); // 환영장군 배치
            Assert.That(_input.TrySelectPieceAt(phantom.BoardPosition), Is.True); // 배치 기물 선택
            yield return null; // 형태 버튼 생성 대기
            var button = _uiRoot.transform.Find("PieceAbilityOverlayCanvas/AbilityPanel/Ability_장군").GetComponent<Button>(); // 장군 형태 버튼
            Assert.That(button.interactable, Is.True); // 배치 선택 권한 확인
            button.onClick.Invoke(); // 실제 형태 변경 클릭
            Assert.That(phantom.MovementCycleIndex, Is.EqualTo(3)); // 장군 형태 적용 확인
            Assert.That(phantom.BoardPosition, Is.EqualTo(new Vector2Int(3, 3))); // 이동 없는 형태 선택 확인
            Assert.That(_input.PendingMovement.MoveTiles, Is.Empty); // 배치 중 이동 후보 차단 확인
        } // 범위 종료

        private PieceRuntimeState Spawn(string id, int x, int y) // 실제 데이터로 아군 배치
        { // 범위 시작
            var piece = new PieceRuntimeState(_database.FindById(id), new Vector2Int(x, y), true); // 런타임 기물 생성
            _run.Board.GetTile(piece.BoardPosition).OccupyingPiece = piece; // 보드 기물 점유
            return piece; // 테스트 기물 반환
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
