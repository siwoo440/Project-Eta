using System.Collections; // 런타임 프레임 대기
using System.Reflection; // 저장 파일 없는 전투 연결
using System.Linq; // 실제 왕 데이터 조회
using NUnit.Framework; // 실제 전투 검사
using UnityEngine; // 보드·런타임 객체
using UnityEngine.TestTools; // PlayMode 검사
using ProjectEta.Run; // 스테이지·런 저장
using ProjectEta.Board; // 실제 보드와 입력
using ProjectEta.Battle; // 실제 턴·전투 훅
using ProjectEta.Boss; // 선택·스폰·보스 AI
using ProjectEta.Pieces; // 실제 기물
using ProjectEta.Cards; // 왕 카탈로그

namespace ProjectEta.Tests.PlayMode // 보스 실제 전투 검사 영역
{ // 영역 시작
    public sealed class Day101BossRuntimePlayModeTests // 크기가 다른 보스의 공통 경로 확인
    { // 타입 시작
        [UnityTest] // 실제 런타임 검사
        public IEnumerator SelectedSmallAndLargeBosses_SpawnActAndRestoreWithoutDuplicate() // 선택 데이터 실제 소비 확인
        { // 메서드 시작
            foreach (var testCase in new[] { (bossId: "serdin", phase: 1, depth: 5), (bossId: "vargas", phase: 1, depth: 10), (bossId: "astragon", phase: 5, depth: 5), (bossId: "mordrain", phase: 5, depth: 10) }) // 작은 보스·강화형·고정 보스 확인
            { // 반복 시작
                string bossId = testCase.bossId; // 현재 검사 보스
                RunState run = null; // 해당 보스가 선택되는 독립 런
                for (int seed = 0; seed < 128; seed++) // 고정 Seed 후보 탐색
                { // 반복 시작
                    var candidate = new RunState(3); // 독립 런 생성
                    candidate.RouteMap.Restore(new RouteMapState(seed).ToSaveData()); // 지도 Seed 적용
                    if (BossSelectionService.Select(candidate, testCase.phase, testCase.depth).bossId != bossId) continue; // 원하는 실제 선택만 사용
                    run = candidate; // 검증할 런 선택
                    break; // 탐색 종료
                } // 반복 종료
                Assert.That(run, Is.Not.Null); // 실제 후보 선택 가능 확인
                run.CurrentRound = testCase.depth; // 실제 보스 깊이
                string stageId = testCase.phase == 5 && testCase.depth == 10 ? "stage_10_finalboss" : "stage_" + testCase.depth + "_midboss"; // 실제 단계별 스테이지
                run.RouteMap.Configure(testCase.depth, new StageNode("phase_" + testCase.phase + "_boss", Vector2Int.zero, testCase.depth, stageId), new StageNode[0]); // 실제 보스 노드 연결
                var boardRoot = new GameObject("Day101BossBoard"); // 실제 보드 호스트
                var battleRoot = new GameObject("Day101BossBattle"); // 자동 저장 접근 없는 전투 호스트
                battleRoot.SetActive(false); // 전투 Awake 자동 저장 읽기 차단
                try // 실제 전투 검사 범위
                { // 검사 시작
                    var turns = new TurnManager(); // 실제 턴 상태
                    var hooks = new BattleHooks(); // 실제 전투 훅
                    BoardView view = boardRoot.AddComponent<BoardView>(); // 실제 보드 표시
                    BoardInputController input = boardRoot.AddComponent<BoardInputController>(); // 실제 스폰 경로
                    view.Bind(run.Board); // 실제 보드 연결
                    input.Bind(run, view, turns, hooks); // 입력·스폰 연결
                    BattleController battle = battleRoot.AddComponent<BattleController>(); // 실제 전투 컨트롤러
                    typeof(BattleController).GetField("_runState", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, run); // 독립 런 주입
                    typeof(BattleController).GetField("_turnManager", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, turns); // 실제 턴 주입
                    StageDefinition definition = StageDefinitionCatalog.Resolve(stageId, testCase.depth); // 실제 보스 스테이지
                    StageRuleSnapshot rules = RunStageRuleService.GetOrCreate(run, definition, testCase.phase); // 지도와 같은 선택 규칙
                    StageBattleRuntimeController runtime = boardRoot.AddComponent<StageBattleRuntimeController>(); // 실제 스테이지 스폰
                    Assert.That(runtime.Configure(battle, definition), Is.True); // 선택 보스 스폰 실행
                    PieceRuntimeState boss = run.Board.GetTile(rules.bossAnchor).OccupyingPiece; // 실제 스폰된 보스
                    Assert.That(boss.Definition.PieceId, Is.EqualTo(rules.bossSelection.pieceId)); // 안내와 실제 ID 일치
                    Assert.That(boss.Definition.BaseHp, Is.EqualTo(rules.bossSelection.maxHp)); // 실제 선택 체력 적용
                    Assert.That(boss.Definition.OccupancySize, Is.EqualTo(BossCatalog.Find(bossId).size)); // 1칸·4칸 점유 적용
                    Assert.That(LargePieceBoardUtility.IsFootprintComplete(run.Board, boss), Is.True); // 전체 점유 무결성
                    PieceDefinition kingDefinition = Resources.Load<PlayerStartingDeckCatalog>("PlayerStartingDeck26").Cards.First(piece => piece.PieceId == "king"); // 실제 왕 기물
                    var king = new PieceRuntimeState(kingDefinition, new Vector2Int(0, 2), true); // 접근 목표 왕
                    Assert.That(LargePieceBoardUtility.TryPlace(run.Board, king, king.BoardPosition), Is.True); // 왕 점유 연결
                    turns.MarkInitialKingPlaced(); // 초기 배치 조건 충족
                    Assert.That(turns.TryEndDeploymentTurn(), Is.True); // 첫 플레이어 턴
                    Assert.That(turns.TryCompletePlayerAction(), Is.True); // 실제 적 턴 진입
                    Assert.That(new BossActionPlanner().TryChooseAction(run.Board, out BossActionCandidate action), Is.True); // 공통 보스 AI 후보 생성
                    Assert.That(action.Actor, Is.SameAs(boss)); // 선택 보스가 실제 행동 주체
                    Assert.That(BossActionExecutor.TryExecute(action, run, turns, hooks, view, out _), Is.True); // 크기와 무관한 실제 행동 실행
                    if (rules.bossSelection.variant == BossVariant.Enhanced) // 강화형 실제 동작 확인
                    { // 조건 시작
                        BossDefinition source = BossCatalog.Find(bossId); // 기본 데이터 조회
                        Assert.That(boss.Definition.BaseHp, Is.EqualTo(Mathf.CeilToInt(source.baseHp * 1.25f))); // 강화 체력 정확값
                        Assert.That(boss.Definition.BaseAtk, Is.EqualTo(source.baseAtk + 1)); // 강화 공격력 정확값
                        while (turns.TurnNumber < 4) // 실제 4턴 증원 시점까지 진행
                        { // 반복 시작
                            Assert.That(turns.TryCompletePlayerAction(), Is.True); // 플레이어 행동 종료
                            Assert.That(turns.CompleteEnemyTurn(), Is.True); // 증원 이벤트 발생
                        } // 반복 종료
                        Vector2Int extraCell = rules.reinforcements.Last().Position; // 강화형 추가 병사 칸
                        Assert.That(run.Board.GetTile(extraCell).OccupyingPiece?.Definition.PieceId, Is.EqualTo("pawn")); // 실제 추가 병사 스폰 확인
                    } // 조건 종료
                    boss.CurrentHp = boss.Definition.BaseHp - 1; // 전투 중 손상 상태
                    RunSaveData saved = JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())); // 실제 런 저장
                    RunState restored = RunState.FromSaveData(saved, null); // 실제 보스 복원
                    PieceRuntimeState recovered = restored.Board.GetTile(boss.BoardPosition).OccupyingPiece; // 이동 후 보스 복원 위치
                    Assert.That(recovered.Definition.PieceId, Is.EqualTo(boss.Definition.PieceId)); // 같은 보스 유지
                    Assert.That(recovered.CurrentHp, Is.EqualTo(boss.CurrentHp)); // 손상 체력 유지
                    Assert.That(LargePieceBoardUtility.IsFootprintComplete(restored.Board, recovered), Is.True); // 복원 점유 무결성
                } // 검사 종료
                finally // 실제 생성물 정리
                { // 정리 시작
                    Object.Destroy(boardRoot); // 보드·스폰·라운드 정리
                    Object.Destroy(battleRoot); // 전투 호스트 정리
                } // 정리 종료
                yield return null; // 파괴 완료 대기
                yield return null; // 소유 라운드 제거 대기
                BossRuntimeDefinitionFactory.Clear(); // 생성 보스 정의 정리
            } // 반복 종료
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
