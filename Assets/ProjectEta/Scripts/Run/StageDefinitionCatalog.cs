using System; // Enum·StringComparison 사용
using System.Collections.Generic; // Dictionary<T> 사용
using UnityEngine; // Resources·ScriptableObject·HideFlags 사용
using ProjectEta.Round; // RoundDefinition 사용

namespace ProjectEta.Run // 스테이지 런타임 카탈로그 네임스페이스
{
    public static class StageDefinitionCatalog // StageDefinitionId를 실제 StageDefinition으로 변환하는 45일차 런타임 카탈로그
    {
        private const string NormalRoundResourceName = "PrototypeRound36"; // 일반·엘리트 전투 기본 RoundDefinition
        private const string BossRoundResourceName = "PrototypeBossRound40"; // 중간·최종 보스 기본 RoundDefinition
        private static readonly Dictionary<string, StageDefinition> Cache = new Dictionary<string, StageDefinition>(); // 동일 ID 런타임 정의 재사용 캐시

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // Play Mode 시작 시 정적 캐시 초기화
        private static void ResetRuntimeCache() // 이전 세션 StageDefinition 참조 제거
        {
            Cache.Clear(); // 런타임 정의 캐시 비우기
        }

        public static string CreateDefinitionId(int depth, StageType stageType) // 깊이·타입 기반 안정적인 StageDefinition ID 생성
        {
            return $"stage_{depth}_{stageType.ToString().ToLowerInvariant()}"; // 저장·로그에 쓰기 쉬운 소문자 ID 반환
        }

        public static StageDefinition Resolve(string stageDefinitionId, int depth) // 노드 ID를 실제 스테이지 설정으로 변환
        {
            if (string.IsNullOrWhiteSpace(stageDefinitionId)) return null; // 빈 ID 차단
            if (Cache.TryGetValue(stageDefinitionId, out var cached) && cached != null) return cached; // 기존 런타임 정의 재사용
            if (!TryParseStageType(stageDefinitionId, out var stageType)) stageType = StageType.Battle; // 파싱 실패 시 일반 전투 fallback

            RoundDefinition roundDefinition = LoadRoundDefinition(stageType); // 타입에 맞는 기존 라운드 데이터 로드
            var definition = ScriptableObject.CreateInstance<StageDefinition>(); // 런타임 StageDefinition 생성
            definition.hideFlags = HideFlags.HideAndDontSave; // 에셋으로 저장되지 않는 런타임 전용 객체 지정
            definition.ConfigureRuntime(stageDefinitionId, GetDisplayName(stageType, depth), stageType, roundDefinition, GetRewardProfileId(stageType)); // 실제 설정 주입
            Cache[stageDefinitionId] = definition; // 동일 ID 재사용을 위해 캐시 저장
            return definition; // 완성된 StageDefinition 반환
        }

        public static bool TryParseStageType(string stageDefinitionId, out StageType stageType) // StageDefinitionId 마지막 토큰에서 StageType 복원
        {
            stageType = StageType.Battle; // 기본 일반 전투 값 지정
            if (string.IsNullOrWhiteSpace(stageDefinitionId)) return false; // 빈 ID 차단

            int separatorIndex = stageDefinitionId.LastIndexOf('_'); // 마지막 구분자 위치 탐색
            string token = separatorIndex >= 0 ? stageDefinitionId.Substring(separatorIndex + 1) : stageDefinitionId; // 타입 토큰 추출
            return Enum.TryParse(token, true, out stageType); // 대소문자 무시 StageType 변환
        }

        private static RoundDefinition LoadRoundDefinition(StageType stageType) // 전투형 스테이지의 기존 RoundDefinition 선택
        {
            if (stageType == StageType.MidBoss || stageType == StageType.FinalBoss) return Resources.Load<RoundDefinition>(BossRoundResourceName); // 보스 라운드 데이터 로드
            if (stageType == StageType.Battle || stageType == StageType.Elite) return Resources.Load<RoundDefinition>(NormalRoundResourceName); // 일반 라운드 데이터 로드
            return null; // 비전투 스테이지는 전투 데이터 없음
        }

        private static string GetDisplayName(StageType stageType, int depth) // 개발용 스테이지 표시 이름 생성
        {
            switch (stageType) // 스테이지 종류별 이름 분기
            {
                case StageType.Elite: return $"{depth}단계 엘리트 전투"; // 엘리트 전투 표시
                case StageType.Reward: return $"{depth}단계 카드 보상"; // 보상 표시
                case StageType.Shop: return $"{depth}단계 상점"; // 상점 표시
                case StageType.Event: return $"{depth}단계 이벤트"; // 이벤트 표시
                case StageType.MidBoss: return $"{depth}단계 중간 보스"; // 중간 보스 표시
                case StageType.FinalBoss: return $"{depth}단계 최종 보스"; // 최종 보스 표시
                default: return $"{depth}단계 일반 전투"; // 일반 전투 표시
            }
        }

        private static string GetRewardProfileId(StageType stageType) // 이후 46·47일차가 사용할 임시 보상 프로필 ID 생성
        {
            if (stageType == StageType.Reward) return "PrototypeRewardNode"; // 전용 카드 보상 노드 프로필
            if (stageType == StageType.Elite) return "PrototypeEliteReward"; // 엘리트 전투 보상 프로필
            if (stageType == StageType.MidBoss || stageType == StageType.FinalBoss) return "PrototypeBossReward"; // 보스 보상 프로필
            if (stageType == StageType.Shop) return "PrototypeShop"; // 상점 프로필
            if (stageType == StageType.Event) return "PrototypeEvent"; // 이벤트 프로필
            return "PrototypeBattleReward"; // 일반 전투 보상 프로필
        }
    }
}
