using System; // 문자열 순서 비교 사용
using System.Collections.Generic; // 노드 층·좌표 사전 사용
using UnityEngine; // Vector2Int·Vector3·Mathf 사용
using ProjectEta.Run; // RouteMapState·StageNode 사용

namespace ProjectEta.Board // 경로 지도 배치 네임스페이스
{ // 네임스페이스 시작
    public static class RouteMapVisualLayout // 논리 경로와 분리된 지도 시각 배치
    { // 클래스 시작
        private const float StartDepth = -4f; // 시작 노드 아래 끝 위치
        private const float FinalDepth = 4f; // 최종 보스 위 끝 위치
        private const float TwoNodeSlot = 1.3f; // 두 갈래 좌우 슬롯 간격
        private const float ThreeNodeSlot = 2.4f; // 세 갈래 바깥 슬롯 간격
        private const float HorizontalLimit = 3.8f; // 보드 내부 가로 한계
        private const float VerticalLimit = 4f; // 보드 내부 세로 한계
        private const float CollisionGap = 0.12f; // 노드 테두리 사이 최소 여백
        private const float CurrentNodeRadius = 0.63f; // 현재 노드 축소 원판 반지름
        private const float BossNodeRadius = 0.483f; // 보스 노드 축소 원판 반지름
        private const float RegularNodeRadius = 0.462f; // 일반 노드 축소 원판 반지름
        private const float SearchStep = 0.12f; // 충돌 해소 좌표 탐색 단위
        private const int HorizontalSearchSteps = 20; // 좌우 최대 탐색 칸 수
        private const int VerticalSearchSteps = 5; // 앞뒤 최대 탐색 칸 수

        public static IReadOnlyDictionary<string, Vector3> Build(RouteMapState route, float tileSize) // 전체 경로 공통 좌표표 생성
        { // 좌표표 생성 시작
            var result = new Dictionary<string, Vector3>(); // 최종 노드 좌표 사전 생성
            if (route == null) return result; // 빈 지도 상태 처리

            float safeTileSize = Mathf.Max(0.01f, tileSize); // 잘못된 타일 크기 보정
            var layers = BuildLayers(route.Nodes); // 깊이별 노드 목록 생성
            var placedNodes = new List<PlacedNode>(); // 충돌 검사 완료 노드 목록 생성

            for (int depth = RoundState.FirstRound; depth <= RoundState.FinalRound; depth++) // 전체 깊이 순회
            { // 단일 층 순회 시작
                if (!layers.TryGetValue(depth, out List<StageNode> layer)) continue; // 없는 깊이 제외
                if (layer.Count != 1) continue; // 시작·보스 단일 층만 우선 처리
                StageNode node = layer[0]; // 단일 노드 조회
                Vector3 basePosition = CreateBasePosition(depth, 1, 0); // 중앙 고정 좌표 생성
                AddPosition(result, placedNodes, node, route.CurrentNodeId, basePosition, safeTileSize); // 단일 노드 좌표 등록
            } // 단일 층 순회 종료

            for (int depth = RoundState.FirstRound; depth <= RoundState.FinalRound; depth++) // 전체 깊이 재순회
            { // 분기 층 순회 시작
                if (!layers.TryGetValue(depth, out List<StageNode> layer)) continue; // 없는 깊이 제외
                if (layer.Count == 1) continue; // 이미 처리한 단일 층 제외

                for (int index = 0; index < layer.Count; index++) // 층 내부 슬롯 순회
                { // 슬롯 순회 시작
                    StageNode node = layer[index]; // 현재 슬롯 노드 조회
                    Vector3 basePosition = CreateBasePosition(depth, layer.Count, index); // 층별 기본 슬롯 좌표 생성
                    float radius = ResolveRadius(node, route.CurrentNodeId); // 실제 표시 반지름 조회
                    Vector3 resolvedPosition = ResolveCollision(basePosition, radius, placedNodes); // 기존 노드와 겹치지 않는 좌표 탐색
                    AddPosition(result, placedNodes, node, route.CurrentNodeId, resolvedPosition, safeTileSize); // 분기 노드 좌표 등록
                } // 슬롯 순회 종료
            } // 분기 층 순회 종료

            return result; // 모든 표시 요소가 공유할 좌표표 반환
        } // 좌표표 생성 종료

        public static Vector3 GetNodeLocalPosition(Vector2Int cell, float tileSize) // 구버전 좌표 테스트용 호환 계산
        { // 호환 좌표 계산 시작
            float safeTileSize = Mathf.Max(0.01f, tileSize); // 잘못된 타일 크기 보정
            if (cell.y == 0) return new Vector3(0f, 0f, StartDepth * safeTileSize); // 시작 노드 아래 끝 중앙 배치
            if (cell.y == 4) return Vector3.zero; // 중간 보스 중앙 배치
            if (cell.y == BoardState.Height - 1) return new Vector3(0f, 0f, FinalDepth * safeTileSize); // 최종 보스 위 끝 중앙 배치
            float centerColumn = (BoardState.Width - 2) * 0.5f; // 기존 2×2 중심 열 계산
            float centerDepth = (BoardState.Height - 1) * 0.5f; // 전체 깊이 중앙 계산
            float wave = ResolveLegacyWave(cell.y); // 일반 행 좌우 굴곡 계산
            float stagger = ResolveLegacyStagger(cell.x); // 열별 고정 앞뒤 편차 계산
            float rawX = (cell.x - centerColumn) * 1.8f + wave; // 구버전 1.8배 가로 좌표 계산
            float x = Mathf.Clamp(rawX, -HorizontalLimit, HorizontalLimit) * safeTileSize; // 보드 내부 가로 위치 제한
            float z = ((cell.y - centerDepth) * 0.70f + stagger) * safeTileSize; // 구버전 깊이·엇갈림 위치 계산
            return new Vector3(x, 0f, z); // 호환 로컬 위치 반환
        } // 호환 좌표 계산 종료

        private static Dictionary<int, List<StageNode>> BuildLayers(IReadOnlyList<StageNode> nodes) // 깊이별 정렬 목록 생성
        { // 층 목록 생성 시작
            var layers = new Dictionary<int, List<StageNode>>(); // 깊이별 노드 사전 생성
            if (nodes == null) return layers; // 빈 노드 목록 처리

            for (int index = 0; index < nodes.Count; index++) // 전체 노드 순회
            { // 노드 순회 시작
                StageNode node = nodes[index]; // 현재 노드 조회
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId)) continue; // 손상 노드 제외

                if (!layers.TryGetValue(node.Depth, out List<StageNode> layer)) // 현재 깊이 목록 확인
                { // 새 깊이 처리 시작
                    layer = new List<StageNode>(); // 새 깊이 목록 생성
                    layers[node.Depth] = layer; // 깊이별 목록 등록
                } // 새 깊이 처리 종료

                layer.Add(node); // 현재 깊이에 노드 추가
            } // 노드 순회 종료

            foreach (KeyValuePair<int, List<StageNode>> pair in layers) // 모든 깊이 목록 순회
            { // 목록 정렬 시작
                pair.Value.Sort(CompareNodes); // 논리 X와 ID 기준 안정 정렬
            } // 목록 정렬 종료

            return layers; // 깊이별 정렬 목록 반환
        } // 층 목록 생성 종료

        private static int CompareNodes(StageNode left, StageNode right) // 층 내부 안정 순서 비교
        { // 노드 비교 시작
            int positionComparison = left.Position.x.CompareTo(right.Position.x); // 논리 X 우선 비교
            if (positionComparison != 0) return positionComparison; // 다른 X 순서 반환
            return string.Compare(left.NodeId, right.NodeId, StringComparison.Ordinal); // 동일 X에서 ID 순서 반환
        } // 노드 비교 종료

        private static Vector3 CreateBasePosition(int depth, int nodeCount, int index) // 층별 슬롯 기본 좌표 생성
        { // 기본 좌표 계산 시작
            float depthRatio = (depth - RoundState.FirstRound) / (float)(RoundState.FinalRound - RoundState.FirstRound); // 시작부터 최종까지 깊이 비율 계산
            float z = Mathf.Lerp(StartDepth, FinalDepth, depthRatio) + ResolveLayerStagger(nodeCount, index); // 세로 위치와 엇갈림 합산
            float x = ResolveSlotX(nodeCount, index); // 노드 수별 가로 슬롯 계산
            return new Vector3(x, 0f, z); // 층 기본 좌표 반환
        } // 기본 좌표 계산 종료

        private static float ResolveSlotX(int nodeCount, int index) // 노드 수별 가로 슬롯 계산
        { // 가로 슬롯 계산 시작
            if (nodeCount <= 1) return 0f; // 단일 노드 중앙 배치
            if (nodeCount == 2) return index == 0 ? -TwoNodeSlot : TwoNodeSlot; // 두 갈래 대칭 배치
            if (nodeCount == 3) return index == 0 ? -ThreeNodeSlot : index == 1 ? 0f : ThreeNodeSlot; // 세 갈래 좌·중·우 배치
            float ratio = index / (float)(nodeCount - 1); // 다중 분기 가로 비율 계산
            return Mathf.Lerp(-3.2f, 3.2f, ratio); // 네 갈래 이상 보드 폭 분산
        } // 가로 슬롯 계산 종료

        private static float ResolveLayerStagger(int nodeCount, int index) // 같은 층 수평선 완화 편차 계산
        { // 층 편차 계산 시작
            if (nodeCount == 2) return index == 0 ? -0.10f : 0.10f; // 두 갈래 앞뒤 엇갈림
            if (nodeCount == 3) return index == 0 ? -0.12f : index == 1 ? 0.12f : 0f; // 세 갈래 서로 다른 깊이 배치
            return 0f; // 단일·기타 층 기본 깊이 유지
        } // 층 편차 계산 종료

        private static Vector3 ResolveCollision(Vector3 basePosition, float radius, IReadOnlyList<PlacedNode> placedNodes) // 최소 거리 만족 좌표 탐색
        { // 충돌 해소 시작
            Vector3 bestPosition = basePosition; // 실패 대비 기본 좌표 보관
            float bestScore = float.MaxValue; // 최소 이동 점수 초기화
            bool found = false; // 유효 좌표 발견 여부

            for (int xStep = -HorizontalSearchSteps; xStep <= HorizontalSearchSteps; xStep++) // 좌우 후보 순회
            { // 좌우 탐색 시작
                for (int zStep = -VerticalSearchSteps; zStep <= VerticalSearchSteps; zStep++) // 앞뒤 후보 순회
                { // 앞뒤 탐색 시작
                    float xOffset = xStep * SearchStep; // 현재 좌우 이동량 계산
                    float zOffset = zStep * SearchStep; // 현재 앞뒤 이동량 계산
                    float score = (xOffset * xOffset) + (zOffset * zOffset); // 기본 슬롯 이탈 거리 계산
                    if (score >= bestScore) continue; // 더 먼 후보 제외

                    Vector3 candidate = basePosition + new Vector3(xOffset, 0f, zOffset); // 현재 후보 좌표 생성
                    if (Mathf.Abs(candidate.x) > HorizontalLimit) continue; // 보드 가로 한계 초과 제외
                    if (candidate.z < StartDepth || candidate.z > VerticalLimit) continue; // 보드 세로 한계 초과 제외
                    if (Overlaps(candidate, radius, placedNodes)) continue; // 기존 노드와 겹치는 후보 제외

                    bestPosition = candidate; // 가장 가까운 유효 좌표 저장
                    bestScore = score; // 최소 이동 점수 갱신
                    found = true; // 유효 좌표 발견 기록
                } // 앞뒤 탐색 종료
            } // 좌우 탐색 종료

            return found ? bestPosition : basePosition; // 충돌 없는 좌표 또는 안전 기본값 반환
        } // 충돌 해소 종료

        private static bool Overlaps(Vector3 candidate, float radius, IReadOnlyList<PlacedNode> placedNodes) // 기존 노드와 최소 거리 검사
        { // 겹침 검사 시작
            for (int index = 0; index < placedNodes.Count; index++) // 배치 완료 노드 순회
            { // 완료 노드 순회 시작
                PlacedNode placed = placedNodes[index]; // 현재 완료 노드 조회
                float minimumDistance = radius + placed.Radius + CollisionGap; // 두 반지름과 여백 합산
                if (Vector3.Distance(candidate, placed.Position) < minimumDistance) return true; // 최소 거리 미달 확인
            } // 완료 노드 순회 종료

            return false; // 모든 기존 노드와 분리 확인
        } // 겹침 검사 종료

        private static void AddPosition(Dictionary<string, Vector3> result, List<PlacedNode> placedNodes, StageNode node, string currentNodeId, Vector3 position, float tileSize) // 좌표표와 충돌 목록 동시 등록
        { // 좌표 등록 시작
            float radius = ResolveRadius(node, currentNodeId); // 노드 표시 반지름 계산
            placedNodes.Add(new PlacedNode(position, radius)); // 논리 좌표 충돌 목록 등록
            result[node.NodeId] = position * tileSize; // 타일 크기 반영 최종 좌표 등록
        } // 좌표 등록 종료

        private static float ResolveRadius(StageNode node, string currentNodeId) // 노드별 표시 반지름 계산
        { // 반지름 계산 시작
            if (node != null && string.Equals(node.NodeId, currentNodeId, StringComparison.Ordinal)) return CurrentNodeRadius; // 현재 노드 원판 반지름 반환
            if (node != null && (node.Depth == 5 || node.Depth == RoundState.FinalRound)) return BossNodeRadius; // 보스 원판 반지름 반환
            return RegularNodeRadius; // 일반 선택 원판 반지름 반환
        } // 반지름 계산 종료

        private static float ResolveLegacyWave(int depthIndex) // 구버전 깊이별 좌우 굴곡 계산
        { // 구버전 굴곡 계산 시작
            int pattern = PositiveMod(depthIndex * 3, 5) - 2; // -2~2 고정 패턴 생성
            return pattern * 0.55f; // 좌우 굴곡 반환
        } // 구버전 굴곡 계산 종료

        private static float ResolveLegacyStagger(int column) // 구버전 열별 앞뒤 편차 계산
        { // 구버전 편차 계산 시작
            int pattern = PositiveMod(column, 3) - 1; // 열별 -1~1 고정 패턴 생성
            return pattern * 0.12f; // 앞뒤 편차 반환
        } // 구버전 편차 계산 종료

        private static int PositiveMod(int value, int modulus) // 음수 안전 나머지 계산
        { // 나머지 계산 시작
            int result = value % modulus; // 기본 나머지 계산
            return result < 0 ? result + modulus : result; // 양수 범위 보정
        } // 나머지 계산 종료

        private readonly struct PlacedNode // 충돌 검사 전용 배치 정보
        { // 구조체 시작
            public Vector3 Position { get; } // 노드 논리 좌표
            public float Radius { get; } // 노드 표시 반지름

            public PlacedNode(Vector3 position, float radius) // 배치 정보 생성자
            { // 생성자 시작
                Position = position; // 논리 좌표 저장
                Radius = radius; // 표시 반지름 저장
            } // 생성자 종료
        } // 구조체 종료
    } // 클래스 종료
} // 네임스페이스 종료
