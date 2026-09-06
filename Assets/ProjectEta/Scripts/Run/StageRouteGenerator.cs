using System.Collections.Generic; // List<T>·HashSet<T> 사용
using UnityEngine; // Vector2Int·Mathf 사용
using ProjectEta.Board; // BoardState 크기 상수 사용

namespace ProjectEta.Run
{
    public static class StageRouteGenerator
    {
        private static readonly StageType[] LegacyOptionalStageTypes =
        {
            StageType.Elite, // 45일차 단일 다음 층 생성 호환용 엘리트
            StageType.Reward, // 45일차 단일 다음 층 생성 호환용 보상
            StageType.Shop, // 45일차 단일 다음 층 생성 호환용 상점
            StageType.Event // 45일차 단일 다음 층 생성 호환용 이벤트
        };

        private static readonly StageType[] FullRouteOptionalStageTypes =
        {
            StageType.Reward, // 52일차 전체 경로 보상 노드
            StageType.Shop, // 52일차 전체 경로 상점 노드
            StageType.Event // 52일차 전체 경로 이벤트 노드
        };

        public static IReadOnlyList<StageNode> CreateFullRoute(int mapSeed)
        {
            var random = new RouteRandom(mapSeed); // MapSeed 기반 결정론적 난수 생성기 준비
            int centerX = Mathf.Clamp((BoardState.Width - 1) / 2, 1, BoardState.Width - 2); // Day45 호환 중앙 X=4를 전체 런 기준 열로 유지
            var result = new List<StageNode>(); // 1~10단계 전체 노드 목록 생성
            var previousLayer = new List<StageNode>(); // 직전 깊이 연결 대상 목록 생성

            StageNode root = CreateStageNode(1, 0, centerX, StageType.Battle); // 1단계 고정 시작 전투 노드 생성
            result.Add(root); // 전체 그래프에 시작 노드 추가
            previousLayer.Add(root); // 2단계 연결용 직전 레이어 등록

            for (int depth = 2; depth <= RoundState.FinalRound; depth++)
            {
                List<StageNode> currentLayer = CreateLayer(depth, centerX, ref random); // 현재 깊이 2~3분기 또는 보스 노드 생성
                ConnectLayers(previousLayer, currentLayer); // 킹 1칸 규칙으로 직전 깊이와 현재 깊이 연결

                for (int i = 0; i < currentLayer.Count; i++)
                {
                    result.Add(currentLayer[i]); // 현재 깊이 노드를 전체 그래프에 순서대로 추가
                }

                previousLayer = currentLayer; // 다음 깊이 연결 기준을 현재 레이어로 교체
            }

            return result; // 완성된 1~10단계 전체 경로 반환
        }

        public static IReadOnlyList<StageNode> CreateNextNodes(int clearedDepth, Vector2Int currentPosition)
        {
            int safeDepth = Mathf.Clamp(clearedDepth, RoundState.FirstRound, RoundState.FinalRound - 1); // 1~9 완료 깊이 보정
            int nextDepth = safeDepth + 1; // 다음 스테이지 깊이 계산
            int nextY = nextDepth - 1; // 깊이를 10×10 보드 Y 좌표로 변환

            if (nextDepth == 5) return CreateLegacyForcedBossNode(nextDepth, currentPosition.x, nextY, StageType.MidBoss); // 45일차 중간 보스 강제 규칙 유지
            if (nextDepth == 10) return CreateLegacyForcedBossNode(nextDepth, currentPosition.x, nextY, StageType.FinalBoss); // 45일차 최종 보스 강제 규칙 유지

            int branchCount = nextDepth % 2 == 0 ? 3 : 2; // 기존 짝수 3분기·홀수 2분기 규칙 유지
            List<int> xPositions = BuildAdjacentXPositions(currentPosition.x, branchCount); // 현재 킹 인접 X 좌표 생성
            var nodes = new List<StageNode>(xPositions.Count); // 다음 깊이 노드 목록 생성

            for (int i = 0; i < xPositions.Count; i++)
            {
                StageType stageType = i == 0 ? StageType.Battle : LegacyOptionalStageTypes[(nextDepth + i - 1) % LegacyOptionalStageTypes.Length]; // 기존 단일 층 스테이지 타입 규칙 유지
                string definitionId = StageDefinitionCatalog.CreateDefinitionId(nextDepth, stageType); // StageDefinition ID 생성
                string nodeId = $"depth_{nextDepth}_{i}_{stageType.ToString().ToLowerInvariant()}"; // 기존 노드 ID 규칙 유지
                nodes.Add(new StageNode(nodeId, new Vector2Int(xPositions[i], nextY), nextDepth, definitionId)); // 다음 층 노드 추가
            }

            return nodes; // 기존 다음 한 층 생성 결과 반환
        }

        private static List<StageNode> CreateLayer(int depth, int centerX, ref RouteRandom random)
        {
            int y = depth - 1; // 현재 깊이를 보드 Y 좌표로 변환

            if (depth == 5)
            {
                return CreateFullRouteBossLayer(depth, centerX, y, StageType.MidBoss); // 5단계 중간 보스 단일 노드 생성
            }

            if (depth == 10)
            {
                return CreateFullRouteBossLayer(depth, centerX, y, StageType.FinalBoss); // 10단계 최종 보스 단일 노드 생성
            }

            int branchCount = depth % 2 == 0 ? 3 : 2; // Day45 호환 짝수 깊이 3분기·홀수 깊이 2분기 유지
            List<int> xPositions = BuildFullRouteXPositions(centerX, branchCount, ref random); // 동일 중심 열 안에서 인접 가능한 X 좌표 생성
            int battleIndex = random.NextInt(branchCount); // 일반 전투가 배치될 분기 위치 결정
            var optionalTypes = new List<StageType>(FullRouteOptionalStageTypes); // 비전투 타입 후보 복사
            Shuffle(optionalTypes, ref random); // Seed 기반 타입 순서 섞기
            var nodes = new List<StageNode>(branchCount); // 현재 깊이 노드 목록 생성
            int optionalIndex = 0; // 비전투 타입 순회 인덱스 초기화

            for (int i = 0; i < xPositions.Count; i++)
            {
                StageType stageType = i == battleIndex ? StageType.Battle : optionalTypes[optionalIndex++]; // 깊이마다 최소 일반 전투 1개 보장
                nodes.Add(CreateStageNode(depth, i, xPositions[i], stageType)); // 현재 분기 StageNode 생성
            }

            return nodes; // 현재 일반 깊이 노드 반환
        }

        private static StageNode CreateStageNode(int depth, int index, int x, StageType stageType)
        {
            int y = depth - 1; // 깊이 기반 Y 좌표 계산
            string definitionId = StageDefinitionCatalog.CreateDefinitionId(depth, stageType); // 안정적인 StageDefinition ID 생성
            string nodeId = depth == 1
                ? "depth_1_root_battle"
                : $"depth_{depth}_{index}_{stageType.ToString().ToLowerInvariant()}"; // 저장 가능한 안정적 노드 ID 생성
            return new StageNode(nodeId, new Vector2Int(x, y), depth, definitionId); // 실제 StageNode 반환
        }

        private static List<StageNode> CreateFullRouteBossLayer(int depth, int centerX, int y, StageType stageType)
        {
            string definitionId = StageDefinitionCatalog.CreateDefinitionId(depth, stageType); // 보스 StageDefinition ID 생성
            string nodeId = $"depth_{depth}_boss_{stageType.ToString().ToLowerInvariant()}"; // 보스 고정 노드 ID 생성
            return new List<StageNode>
            {
                new StageNode(nodeId, new Vector2Int(centerX, y), depth, definitionId) // 전체 경로 중심 열 보스 노드 추가
            };
        }

        private static void ConnectLayers(IReadOnlyList<StageNode> previousLayer, IReadOnlyList<StageNode> currentLayer)
        {
            for (int previousIndex = 0; previousIndex < previousLayer.Count; previousIndex++)
            {
                StageNode source = previousLayer[previousIndex]; // 현재 출발 노드 조회
                var nextIds = new List<string>(); // 출발 노드의 다음 연결 ID 목록 생성

                for (int currentIndex = 0; currentIndex < currentLayer.Count; currentIndex++)
                {
                    StageNode target = currentLayer[currentIndex]; // 현재 도착 후보 조회
                    if (!RouteMapState.IsKingStep(source.Position, target.Position)) continue; // 킹 1칸 범위 밖 연결 제외
                    nextIds.Add(target.NodeId); // 합법적인 인접 노드 연결 추가
                }

                if (nextIds.Count == 0)
                {
                    StageNode fallback = FindNearestNode(source.Position.x, currentLayer); // 설계 예외 시 가장 가까운 다음 노드 탐색
                    if (fallback != null) nextIds.Add(fallback.NodeId); // 진행 불가 방지용 최소 연결 추가
                }

                source.SetNextNodeIds(nextIds); // 출발 노드 다음 경로 확정
            }
        }

        private static StageNode FindNearestNode(int sourceX, IReadOnlyList<StageNode> nodes)
        {
            StageNode best = null; // 가장 가까운 후보 초기화
            int bestDistance = int.MaxValue; // 최소 X 거리 초기화

            for (int i = 0; i < nodes.Count; i++)
            {
                StageNode node = nodes[i]; // 현재 후보 조회
                int distance = Mathf.Abs(node.Position.x - sourceX); // 가로 거리 계산

                if (distance >= bestDistance) continue; // 더 멀거나 같은 후보 제외
                bestDistance = distance; // 최소 거리 갱신
                best = node; // 가장 가까운 노드 갱신
            }

            return best; // 가장 가까운 다음 노드 반환
        }

        private static List<int> BuildFullRouteXPositions(int centerX, int branchCount, ref RouteRandom random)
        {
            var result = new List<int>(branchCount); // 현재 깊이 X 좌표 목록 생성

            if (branchCount >= 3)
            {
                result.Add(centerX - 1); // 왼쪽 분기 추가
                result.Add(centerX); // 중앙 분기 추가
                result.Add(centerX + 1); // 오른쪽 분기 추가
                return result; // 3분기 좌표 반환
            }

            int sideOffset = random.NextInt(2) == 0 ? -1 : 1; // 2분기 보조 방향 결정
            result.Add(centerX); // 중앙 경로 항상 유지
            result.Add(centerX + sideOffset); // Seed 기반 좌·우 보조 분기 추가
            result.Sort(); // 노드 인덱스가 좌→우 순서를 유지하도록 정렬
            return result; // 2분기 좌표 반환
        }

        private static IReadOnlyList<StageNode> CreateLegacyForcedBossNode(int depth, int currentX, int y, StageType stageType)
        {
            int safeX = Mathf.Clamp(currentX, 0, BoardState.Width - 1); // 현재 킹 X를 보드 범위로 보정
            string definitionId = StageDefinitionCatalog.CreateDefinitionId(depth, stageType); // 보스 StageDefinition ID 생성
            string nodeId = $"depth_{depth}_boss_{stageType.ToString().ToLowerInvariant()}"; // 보스 노드 ID 생성
            return new[] { new StageNode(nodeId, new Vector2Int(safeX, y), depth, definitionId) }; // 기존 한 층 보스 노드 반환
        }

        private static List<int> BuildAdjacentXPositions(int currentX, int branchCount)
        {
            int safeCurrentX = Mathf.Clamp(currentX, 0, BoardState.Width - 1); // 현재 X 보드 범위 보정
            int[] preferredOffsets = branchCount >= 3 ? new[] { -1, 0, 1 } : new[] { -1, 1 }; // 기존 분기 오프셋 구성
            var result = new List<int>(branchCount); // 결과 X 목록 생성
            var used = new HashSet<int>(); // 경계 보정 중 중복 X 방지

            for (int i = 0; i < preferredOffsets.Length; i++)
            {
                int x = Mathf.Clamp(safeCurrentX + preferredOffsets[i], 0, BoardState.Width - 1); // 인접 X 보드 범위 보정
                if (used.Add(x)) result.Add(x); // 새로운 X만 분기 목록에 추가
            }

            for (int offset = -1; result.Count < branchCount && offset <= 1; offset++)
            {
                int x = Mathf.Clamp(safeCurrentX + offset, 0, BoardState.Width - 1); // 부족한 인접 X 후보 계산
                if (used.Add(x)) result.Add(x); // 미사용 인접 X 추가
            }

            return result; // 기존 최대 2~3개 인접 X 반환
        }

        private static void Shuffle(List<StageType> values, ref RouteRandom random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = random.NextInt(i + 1); // Seed 기반 교환 인덱스 선택
                StageType temporary = values[i]; // 현재 타입 임시 저장
                values[i] = values[swapIndex]; // 선택 타입 현재 위치로 이동
                values[swapIndex] = temporary; // 임시 타입 교환 위치로 이동
            }
        }

        private static int PositiveMod(int value, int modulus)
        {
            if (modulus <= 0) return 0; // 잘못된 나눗셈 범위 방어
            int result = value % modulus; // 기본 나머지 계산
            return result < 0 ? result + modulus : result; // 음수 Seed도 양수 인덱스로 보정
        }

        private struct RouteRandom
        {
            private uint _state; // 플랫폼 독립 결정론적 난수 상태

            public RouteRandom(int seed)
            {
                _state = unchecked((uint)seed) ^ 0xA341316Cu; // 입력 Seed를 내부 상태로 혼합
                if (_state == 0u) _state = 0x9E3779B9u; // xorshift 정지 상태 방지
            }

            public int NextInt(int maxExclusive)
            {
                if (maxExclusive <= 1) return 0; // 단일 선택 범위 즉시 반환
                uint value = NextUInt(); // 다음 결정론적 난수 생성
                return (int)(value % (uint)maxExclusive); // 요청 범위 인덱스로 축소
            }

            private uint NextUInt()
            {
                uint x = _state; // 현재 난수 상태 복사
                x ^= x << 13; // xorshift 첫 혼합
                x ^= x >> 17; // xorshift 두 번째 혼합
                x ^= x << 5; // xorshift 세 번째 혼합
                _state = x; // 다음 호출 상태 저장
                return x; // 혼합 난수 반환
            }
        }
    }
}
