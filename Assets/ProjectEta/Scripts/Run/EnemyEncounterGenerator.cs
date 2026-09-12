using System.Collections.Generic; // IEnumerable<T>·List<T>·HashSet<T> 사용
using UnityEngine; // Mathf·Vector2Int 사용
using ProjectEta.Board; // BoardState 크기 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.Round; // RoundDefinition 사용

namespace ProjectEta.Run
{
    public static class EnemyEncounterGenerator
    {
        public static EnemyEncounterResult Generate(
            IEnumerable<PieceDefinition> sourcePool,
            RoundDefinition roundDefinition,
            StageType stageType,
            int mapSeed,
            int phase,
            int stage,
            string nodeId)
        {
            int safePhase = Mathf.Clamp(phase, RunPhaseProgressService.FirstPhase, RunPhaseProgressService.TotalPhases); // Phase 범위 보정
            int safeStage = Mathf.Clamp(stage, RoundState.FirstRound, RoundState.FinalRound); // Stage 범위 보정
            int seed = CreateStableSeed(mapSeed, safePhase, safeStage, nodeId, stageType); // 재현 가능한 Encounter Seed 생성
            List<PieceDefinition> candidates = BuildCandidatePool(sourcePool); // 적 후보 Pool 생성
            var spawns = new List<EnemyEncounterSpawn>(); // 결과 배치 목록 생성

            if (roundDefinition == null || candidates.Count <= 0)
            {
                return new EnemyEncounterResult(seed, stageType, safePhase, safeStage, nodeId, spawns, 0); // 생성 불가 시 빈 Encounter 반환
            }

            int baseCount = CountAuthoredEnemies(roundDefinition); // 기존 Round 적 수 조회
            bool elite = stageType == StageType.Elite; // Elite 여부 계산
            int requestedCount = Mathf.Max(1, baseCount) + (elite ? EnemyEncounterRules.GetEliteExtraEnemyCount(safePhase, safeStage) : 0); // 최종 적 수 계산
            var usedCells = new HashSet<int>(); // Encounter 점유 Cell 집합 생성
            int threatScore = 0; // 전체 위협도 초기화

            for (int i = 0; i < requestedCount; i++)
            {
                PieceDefinition piece = SelectPiece(candidates, seed, safePhase, elite, i); // 진행도·Seed 기반 적 선택
                if (piece == null) continue; // 선택 실패 항목 제외

                if (!TryResolveSpawnCell(roundDefinition, usedCells, seed, i, baseCount, elite, out Vector2Int position))
                {
                    break; // 더 이상 배치할 칸이 없으면 생성 종료
                }

                usedCells.Add(ToCellKey(position)); // 점유 Cell 등록
                spawns.Add(new EnemyEncounterSpawn(piece, position)); // Encounter 배치 등록
                threatScore += EnemyEncounterRules.GetPieceThreatScore(piece); // 위협도 누적
            }

            return new EnemyEncounterResult(seed, stageType, safePhase, safeStage, nodeId, spawns, threatScore); // 생성 결과 반환
        }

        public static int CreateStableSeed(int mapSeed, int phase, int stage, string nodeId, StageType stageType)
        {
            unchecked
            {
                int hash = 17; // 안정 해시 초기값
                hash = hash * 31 + mapSeed; // Map Seed 반영
                hash = hash * 31 + phase; // Phase 반영
                hash = hash * 31 + stage; // Stage 반영
                hash = hash * 31 + (int)stageType; // Stage 타입 반영

                string safeNodeId = nodeId ?? string.Empty; // 노드 ID 보정
                for (int i = 0; i < safeNodeId.Length; i++)
                {
                    hash = hash * 31 + safeNodeId[i]; // 노드 문자열 문자 반영
                }

                return hash; // 안정 Encounter Seed 반환
            }
        }

        private static List<PieceDefinition> BuildCandidatePool(IEnumerable<PieceDefinition> sourcePool)
        {
            var candidates = new List<PieceDefinition>(); // 후보 목록 생성
            if (sourcePool == null) return candidates; // 외부 Pool 누락 시 빈 목록 반환

            foreach (PieceDefinition piece in sourcePool)
            {
                if (!EnemyEncounterRules.CanUsePiece(piece)) continue; // 사용할 수 없는 적 제외
                if (candidates.Contains(piece)) continue; // 같은 에셋 중복 제외
                candidates.Add(piece); // 적 후보 등록
            }

            candidates.Sort(ComparePieces); // 위협도 기준 후보 정렬
            return candidates; // 정렬된 후보 Pool 반환
        }

        private static int ComparePieces(PieceDefinition left, PieceDefinition right)
        {
            int threatCompare = EnemyEncounterRules.GetPieceThreatScore(left).CompareTo(EnemyEncounterRules.GetPieceThreatScore(right)); // 위협도 비교
            if (threatCompare != 0) return threatCompare; // 위협도 순서 반환

            string leftId = left != null ? left.PieceId ?? string.Empty : string.Empty; // 왼쪽 ID 보정
            string rightId = right != null ? right.PieceId ?? string.Empty : string.Empty; // 오른쪽 ID 보정
            return string.CompareOrdinal(leftId, rightId); // 동일 위협도 ID 순 정렬
        }

        private static int CountAuthoredEnemies(RoundDefinition roundDefinition)
        {
            if (roundDefinition == null || roundDefinition.InitialEnemies == null) return 0; // Round·목록 누락 시 0 반환

            int count = 0; // 유효 적 수 초기화
            for (int i = 0; i < roundDefinition.InitialEnemies.Count; i++)
            {
                EnemySpawnDefinition spawn = roundDefinition.InitialEnemies[i]; // 현재 적 배치 조회
                if (spawn == null || string.IsNullOrWhiteSpace(spawn.PieceId)) continue; // 빈 배치 제외
                count++; // 유효 적 수 누적
            }

            return count; // 기존 유효 적 수 반환
        }

        private static PieceDefinition SelectPiece(List<PieceDefinition> candidates, int seed, int phase, bool elite, int index)
        {
            if (candidates == null || candidates.Count <= 0) return null; // 후보 누락 시 선택 실패

            int minimum = EnemyEncounterRules.GetMinimumCandidateIndex(candidates.Count, phase, elite); // 현재 진행도 최소 후보 인덱스 조회
            int maximum = EnemyEncounterRules.GetMaximumCandidateIndex(candidates.Count, phase); // 현재 진행도 최대 후보 인덱스 조회
            if (minimum > maximum) minimum = maximum; // 잘못된 범위 보정

            if (elite && index == 0)
            {
                return candidates[maximum]; // Elite 핵심 적은 현재 범위 최상위 기물 선택
            }

            int width = maximum - minimum + 1; // 선택 후보 폭 계산
            int mixed = unchecked(seed + index * 1103515245 + phase * 97); // 항목별 결정 Seed 혼합
            int offset = PositiveModulo(mixed, width); // 범위 내 결정 오프셋 계산
            return candidates[minimum + offset]; // 결정된 적 기물 반환
        }

        private static bool TryResolveSpawnCell(
            RoundDefinition roundDefinition,
            HashSet<int> usedCells,
            int seed,
            int index,
            int baseCount,
            bool elite,
            out Vector2Int position)
        {
            if (index < baseCount && index < roundDefinition.InitialEnemies.Count)
            {
                EnemySpawnDefinition authored = roundDefinition.InitialEnemies[index]; // 같은 순번 기존 적 배치 조회
                if (authored != null && IsEnemyBoardCell(authored.Position))
                {
                    int key = ToCellKey(authored.Position); // 기존 배치 Cell 키 계산
                    if (!usedCells.Contains(key))
                    {
                        position = authored.Position; // 기존 배치 좌표 유지
                        return true; // 기존 배치 Cell 사용 성공
                    }
                }
            }

            int startRow = elite && index >= baseCount ? EnemyEncounterRules.EliteFallbackStartRow : EnemyEncounterRules.EnemyFallbackStartRow; // 대체 배치 영역 결정
            int availableRows = BoardState.Height - startRow; // 탐색 행 수 계산
            int cellCount = BoardState.Width * availableRows; // 탐색 Cell 수 계산
            if (cellCount <= 0)
            {
                position = default; // 잘못된 보드 크기 기본값 지정
                return false; // 배치 영역 없음 반환
            }

            int startIndex = PositiveModulo(unchecked(seed + index * 53), cellCount); // 탐색 시작 위치 계산
            for (int i = 0; i < cellCount; i++)
            {
                int cellIndex = (startIndex + i) % cellCount; // 현재 탐색 위치 계산
                int col = cellIndex % BoardState.Width; // 후보 X 좌표 계산
                int row = startRow + cellIndex / BoardState.Width; // 후보 Y 좌표 계산
                var candidate = new Vector2Int(col, row); // 후보 좌표 생성
                int key = ToCellKey(candidate); // 후보 Cell 키 계산
                if (usedCells.Contains(key)) continue; // 이미 점유된 Cell 제외

                position = candidate; // 대체 좌표 반환
                return true; // 대체 배치 Cell 사용 성공
            }

            position = default; // 실패 기본값
            return false; // 배치 가능 Cell 탐색 실패
        }

        private static bool IsEnemyBoardCell(Vector2Int position)
        {
            return position.x >= 0
                && position.x < BoardState.Width
                && position.y >= EnemyEncounterRules.EnemyFallbackStartRow
                && position.y < BoardState.Height; // 적 진영 내부 좌표 여부 반환
        }

        private static int ToCellKey(Vector2Int position)
        {
            return position.y * BoardState.Width + position.x; // 좌표를 단일 Cell 키로 변환
        }

        private static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 1) return 0; // 단일 범위 오프셋 반환
            return (value & int.MaxValue) % modulo; // 음수 없는 결정 오프셋 반환
        }
    }
}
