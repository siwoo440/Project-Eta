using UnityEngine; // ScriptableObject·SerializeField 사용
using ProjectEta.Round; // RoundDefinition 사용

namespace ProjectEta.Run // 스테이지 데이터 네임스페이스
{
    public enum StageType // 경로 지도에서 선택 가능한 스테이지 종류
    {
        Battle = 0, // 일반 전투
        Elite = 1, // 강화 전투
        Reward = 2, // 카드 보상
        Shop = 3, // 상점
        Event = 4, // 이벤트
        MidBoss = 5, // 5단계 중간 보스
        FinalBoss = 6 // 10단계 최종 보스
    }

    [CreateAssetMenu(fileName = "StageDefinition", menuName = "ProjectEta/Stage Definition")] // 에디터 생성 메뉴 등록
    public class StageDefinition : ScriptableObject // 노드 진입 시 적용할 스테이지 설정 에셋
    {
        [SerializeField] private string _stageId = "Stage"; // 스테이지 고유 ID
        [SerializeField] private string _displayName = "Stage"; // 사용자 표시 이름
        [SerializeField] private StageType _stageType = StageType.Battle; // 스테이지 종류
        [SerializeField] private RoundDefinition _roundDefinition; // 전투 스테이지용 라운드 설정
        [SerializeField] private string _rewardProfileId = "PrototypeReward"; // 이후 보상 시스템이 읽을 프로필 ID

        public string StageId => _stageId; // 스테이지 ID 공개
        public string DisplayName => _displayName; // 표시 이름 공개
        public StageType StageType => _stageType; // 스테이지 종류 공개
        public RoundDefinition RoundDefinition => _roundDefinition; // 연결 라운드 데이터 공개
        public string RewardProfileId => _rewardProfileId; // 보상 프로필 ID 공개
        public bool RequiresBattle => _stageType == StageType.Battle || _stageType == StageType.Elite || _stageType == StageType.MidBoss || _stageType == StageType.FinalBoss; // 전투판 필요 여부

        public void ConfigureRuntime(string stageId, string displayName, StageType stageType, RoundDefinition roundDefinition, string rewardProfileId) // 45일차 런타임 생성용 데이터 설정
        {
            _stageId = stageId ?? string.Empty; // null ID 방지
            _displayName = string.IsNullOrWhiteSpace(displayName) ? _stageId : displayName; // 표시 이름 기본값 보정
            _stageType = stageType; // 스테이지 종류 저장
            _roundDefinition = roundDefinition; // 기존 RoundDefinition 재사용
            _rewardProfileId = rewardProfileId ?? string.Empty; // 이후 보상 연결 ID 저장
        }
    }
}
