using System.Collections.Generic; // IReadOnlyList<T>·List<T> 사용

namespace ProjectEta.Run
{
    public sealed class EnemyEncounterResult
    {
        private readonly List<EnemyEncounterSpawn> _spawns; // 생성된 적 배치 목록

        public int Seed { get; } // Encounter 재현 Seed
        public StageType StageType { get; } // 전투 Stage 타입
        public int Phase { get; } // 현재 Phase
        public int Stage { get; } // 현재 Stage
        public string NodeId { get; } // 현재 RouteMap 노드 ID
        public int ThreatScore { get; } // 편성 전체 위협도
        public IReadOnlyList<EnemyEncounterSpawn> Spawns => _spawns; // 적 배치 읽기 전용 노출

        public EnemyEncounterResult(
            int seed,
            StageType stageType,
            int phase,
            int stage,
            string nodeId,
            IReadOnlyList<EnemyEncounterSpawn> spawns,
            int threatScore)
        {
            Seed = seed; // Seed 저장
            StageType = stageType; // Stage 타입 저장
            Phase = phase; // Phase 저장
            Stage = stage; // Stage 저장
            NodeId = nodeId ?? string.Empty; // 노드 ID 저장
            ThreatScore = threatScore < 0 ? 0 : threatScore; // 위협도 저장
            _spawns = new List<EnemyEncounterSpawn>(); // 배치 목록 복사본 생성

            if (spawns == null) return; // 배치 누락 시 빈 목록 유지

            for (int i = 0; i < spawns.Count; i++)
            {
                _spawns.Add(spawns[i]); // 배치 항목 복사
            }
        }
    }
}
