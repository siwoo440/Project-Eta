using System.Collections.Generic; // 전체 그래프와 연결
using UnityEngine; // 보드 좌표

namespace ProjectEta.Run // 페이즈 경로 규칙
{ // 영역 시작
    public static class Day102RouteRules // 새 런의 전투 횟수 제한
    { // 타입 시작
        public static IReadOnlyList<StageNode> Create(int seed, int phase) // 페이즈별 결정론 경로 생성
        { // 메서드 시작
            int safePhase = Mathf.Clamp(phase, 1, 5); // 유효 페이즈 범위
            uint random = unchecked((uint)seed ^ 0xA341316Cu); // 전역 난수와 독립된 상태
            var nodes = new List<StageNode>(); // 완성 그래프
            var previous = new List<StageNode>(); // 이전 깊이 연결
            for (int depth = 1; depth <= 10; depth++) // 전체 깊이 생성
            { // 반복 시작
                bool single = depth == 1 || depth == 5 || depth == 10; // 시작·보스 단일 노드
                int count = single ? 1 : (Next(ref random, 100) < 65 ? 3 : 2); // 임시 3분기 비중 65퍼센트
                int side = Next(ref random, 2) == 0 ? -1 : 1; // 두 갈래 보조 방향
                StageType[] pool = GetTypes(depth, safePhase); // 깊이별 등장 제한
                bool reverse = Next(ref random, 2) == 0; // 선택지 좌우 편향 방지
                var layer = new List<StageNode>(); // 현재 깊이 노드
                for (int index = 0; index < count; index++) // 분기별 생성
                { // 반복 시작
                    int x = single ? 4 : (count == 3 ? 3 + index : (index == 0 ? 4 : 4 + side)); // 모든 깊이 중앙 인접 열
                    StageType type = pool.Length == 1 ? pool[0] : pool[(index + (reverse ? 1 : 0)) % pool.Length]; // 최소 두 종류 선택지 보장
                    if (index == 2 && pool.Length > 1) type = pool[Next(ref random, 100) < PrimaryWeight(depth, safePhase) ? 0 : 1]; // 세 번째 분기 가중치
                    string id = depth == 1 ? "depth_1_root_battle" : (single ? "depth_" + depth + "_boss_" + type.ToString().ToLowerInvariant() : "depth_" + depth + "_" + index + "_" + type.ToString().ToLowerInvariant()); // 저장 가능한 노드 ID
                    var node = new StageNode(id, new Vector2Int(x, depth - 1), depth, StageDefinitionCatalog.CreateDefinitionId(depth, type)); // 실제 스테이지 연결
                    layer.Add(node); // 현재 깊이 등록
                    nodes.Add(node); // 전체 목록 등록
                } // 반복 종료
                foreach (StageNode source in previous) // 이전 깊이 연결
                { // 반복 시작
                    var nextIds = new List<string>(); // 합법 다음 노드
                    foreach (StageNode target in layer) // 도착 후보 순회
                    { // 반복 시작
                        if (RouteMapState.IsKingStep(source.Position, target.Position)) nextIds.Add(target.NodeId); // 킹 인접 이동만 연결
                    } // 반복 종료
                    source.SetNextNodeIds(nextIds); // 최소 중앙 경로로 진행 보장
                } // 반복 종료
                previous = layer; // 다음 깊이 기준
            } // 반복 종료
            return nodes; // 전체 경로
        } // 메서드 종료
        private static StageType[] GetTypes(int depth, int phase) // 깊이별 후보 종류
        { // 메서드 시작
            if (depth == 1 || depth == 5 || depth == 10 || (phase == 1 && depth == 3)) // 종류가 고정된 깊이
            { // 조건 시작
                return new[] // 단일 종류 후보
                { // 배열 시작
                    depth == 5 ? StageType.MidBoss : (depth == 10 ? StageType.FinalBoss : StageType.Battle) // 시작·첫 일반·보스 구분
                }; // 배열 종료
            } // 조건 종료
            StageType first = StageType.Reward; // 기본 성장 후보
            StageType second = phase >= 2 && phase <= 4 ? StageType.Elite : StageType.Shop; // 깊이 8 추가 전투 제한
            if (depth == 3 || depth == 7) // 필수 전투 깊이
            { // 조건 시작
                first = StageType.Battle; // 일반 전투
                second = StageType.Elite; // 위험을 고르는 정예
            } // 조건 종료
            else if (depth == 2 || depth == 4) // 초반 상점·이벤트 기회
            { // 조건 시작
                first = depth == 2 ? StageType.Shop : StageType.Event; // 깊이별 필수 노출
                second = StageType.Reward; // 안전 성장 선택
            } // 조건 종료
            else if (depth == 6 || depth == 9) // 전투 사이 성장 선택
            { // 조건 시작
                first = StageType.Shop; // 카드·회복·강화
                second = StageType.Event; // 기획 상황 선택
            } // 조건 종료
            return new[] // 두 종류 선택 후보
            { // 배열 시작
                first, // 첫 후보
                second // 두 번째 후보
            }; // 배열 종료
        } // 메서드 종료
        private static int PrimaryWeight(int depth, int phase) // 임시 세 번째 분기 비중
        { // 메서드 시작
            if (depth == 3 || depth == 7) return 75 - phase * 5; // 후반 정예 비중 증가
            if (depth == 8) return 70; // 추가 전투보다 성장 기회 우선
            return depth == 2 ? 55 : 50; // 초반 상점과 기타 균형
        } // 메서드 종료
        private static int Next(ref uint state, int maximum) // 플랫폼 고정 난수
        { // 메서드 시작
            state ^= state << 13; // 첫 비트 혼합
            state ^= state >> 17; // 두 번째 비트 혼합
            state ^= state << 5; // 마지막 비트 혼합
            return (int)(state % (uint)maximum); // 안전한 후보 인덱스
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
