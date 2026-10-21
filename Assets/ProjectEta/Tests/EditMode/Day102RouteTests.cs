using System; // 경계 Seed
using System.Collections.Generic; // 전체 경로 검사
using System.Linq; // 노드 조회
using NUnit.Framework; // 진행 계약 검사
using ProjectEta.Run; // 실제 경로 생성
using ProjectEta.Battle; // 실제 승리 진입
using UnityEngine; // JSON 복원

namespace ProjectEta.Tests.EditMode // 전체 경로 표본 검사
{ // 영역 시작
    public sealed class Day102RouteTests // 실제 전투 횟수·합법 분기
    { // 타입 시작
        [Test] // 모든 선택 경로 검사
        public void AllPaths_AreLegalReachableAndStayWithin21To24TotalBattles() // 256개 Seed와 모든 분기
        { // 메서드 시작
            for (int seed = -64; seed < 192; seed++) // 음수 포함 재현 표본
            { // 반복 시작
                int totalMinimum = 0; // 5페이즈 최소 실제 전투
                int totalMaximum = 0; // 5페이즈 최대 실제 전투
                for (int phase = 1; phase <= 5; phase++) // 전체 런
                { // 반복 시작
                    IReadOnlyList<StageNode> nodes = RunPhaseRouteGenerator.CreateFullRoute(seed, phase, true); // 실제 새 경로
                    IReadOnlyList<StageNode> repeat = RunPhaseRouteGenerator.CreateFullRoute(seed, phase, true); // 같은 Seed 재생성
                    Assert.That(Signature(nodes), Is.EqualTo(Signature(repeat))); // 결정론 재현
                    var lookup = nodes.ToDictionary(node => node.NodeId); // 그래프 연결 조회
                    Assert.That(lookup.Count, Is.EqualTo(nodes.Count)); // ID 중복 없음
                    StageNode root = nodes.Single(node => node.Depth == 1); // 시작점
                    var reached = new HashSet<string>(); // 실제 도달 노드
                    int minimum = int.MaxValue; // 페이즈 최소 전투
                    int maximum = 0; // 페이즈 최대 전투
                    Walk(root, lookup, phase, 0, null, 0, 0, 0, reached, ref minimum, ref maximum); // 가능한 모든 선택 순회
                    Assert.That(reached.Count, Is.EqualTo(nodes.Count)); // 고립 노드 없음
                    Assert.That(nodes.Any(node => Kind(node) == StageType.Shop), Is.True); // 상점 기회
                    Assert.That(nodes.Any(node => Kind(node) == StageType.Event), Is.True); // 이벤트 기회
                    totalMinimum += minimum; // 전체 최소 합산
                    totalMaximum += maximum; // 전체 최대 합산
                } // 반복 종료
                Assert.That(totalMinimum, Is.EqualTo(21), "Seed " + seed); // 이후 페이즈 시작점 자동 완료 포함
                Assert.That(totalMaximum, Is.InRange(21, 24), "Seed " + seed); // 어느 길도 목표 범위 초과 금지
            } // 반복 종료
        } // 메서드 종료
        [Test] // 실제 시작과 저장 정책
        public void FirstVictory_UsesNewRulesAndSavedGraphIsNotRegenerated() // API만 추가하는 누락 방지
        { // 메서드 시작
            RunState run = new RunState(3); // 첫 실제 전투
            Assert.That(RunStageFlowService.CompleteBattle(run, BattleOutcome.Victory), Is.True); // 실제 런 실행 경로
            Assert.That(run.RouteMap.Nodes.Where(node => node.Depth == 7).All(node => Kind(node) == StageType.Battle || Kind(node) == StageType.Elite), Is.True); // 강제 전투 깊이 연결
            string before = Signature(run.RouteMap.Nodes); // 저장 지도
            RunState restored = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())), null); // 안전 체크포인트 복원
            Assert.That(RunPhaseProgressService.TryNormalizeFirstPhaseRoute(restored), Is.False); // 정상 5페이즈 그래프 재생성 금지
            Assert.That(Signature(restored.RouteMap.Nodes), Is.EqualTo(before)); // 저장된 분기 유지
            RunEconomyService.ResetForTests(); // 독립 검사 상태 정리
        } // 메서드 종료
        private static void Walk(StageNode node, Dictionary<string, StageNode> lookup, int phase, int battles, StageType? previous, int streak, int shops, int events, HashSet<string> reached, ref int minimum, ref int maximum) // 모든 합법 선택 추적
        { // 메서드 시작
            reached.Add(node.NodeId); // 도달 이력
            StageType type = Kind(node); // 현재 종류
            int currentStreak = previous == type ? streak + 1 : 1; // 같은 종류 연속 횟수
            Assert.That(currentStreak, Is.LessThanOrEqualTo(2)); // 세 번 연속 등장 금지
            shops += type == StageType.Shop ? 1 : 0; // 경로별 상점 횟수
            events += type == StageType.Event ? 1 : 0; // 경로별 이벤트 횟수
            Assert.That(shops, Is.LessThanOrEqualTo(4)); // 임시 상점 상한
            Assert.That(events, Is.LessThanOrEqualTo(3)); // 임시 이벤트 상한
            if ((node.Depth != 1 || phase == 1) && (type == StageType.Battle || type == StageType.Elite || type == StageType.MidBoss || type == StageType.FinalBoss)) battles++; // 자동 완료 시작점 제외
            if (node.Depth == 5) Assert.That(type, Is.EqualTo(StageType.MidBoss)); // 중간 보스 고정
            if (node.Depth == 10) // 페이즈 마지막 도달
            { // 조건 시작
                Assert.That(type, Is.EqualTo(phase == 5 ? StageType.FinalBoss : StageType.MidBoss)); // 최종 보스 구간
                minimum = Math.Min(minimum, battles); // 최소 전투 갱신
                maximum = Math.Max(maximum, battles); // 최대 전투 갱신
                return; // 경로 끝
            } // 조건 종료
            Assert.That(node.NextNodeIds.Count, Is.GreaterThan(0)); // 막다른 길 금지
            foreach (string id in node.NextNodeIds) // 다음 선택
            { // 반복 시작
                Assert.That(lookup.ContainsKey(id), Is.True); // 없는 노드 연결 금지
                StageNode next = lookup[id]; // 실제 도착점
                Assert.That(next.Depth, Is.EqualTo(node.Depth + 1)); // 깊이 건너뛰기 금지
                Assert.That(RouteMapState.IsKingStep(node.Position, next.Position), Is.True); // 불법 긴 이동 금지
                Walk(next, lookup, phase, battles, type, currentStreak, shops, events, reached, ref minimum, ref maximum); // 후속 경로 검사
            } // 반복 종료
        } // 메서드 종료
        private static StageType Kind(StageNode node) // 저장 정의의 실제 종류
        { // 메서드 시작
            Assert.That(StageDefinitionCatalog.TryParseStageType(node.StageDefinitionId, out StageType type), Is.True); // 정의 ID 유효성
            return type; // 실제 종류
        } // 메서드 종료
        private static string Signature(IReadOnlyList<StageNode> nodes) // 좌표·종류·연결 비교
        { // 메서드 시작
            return string.Join("|", nodes.Select(node => node.NodeId + ":" + node.Position + ":" + node.StageDefinitionId + ":" + string.Join(",", node.NextNodeIds))); // 전체 그래프 동일성
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
