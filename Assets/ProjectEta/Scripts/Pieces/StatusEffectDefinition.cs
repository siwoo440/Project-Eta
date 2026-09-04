using UnityEngine; // ScriptableObject, SerializeField, Mathf 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    [CreateAssetMenu(fileName = "StatusEffectDefinition", menuName = "ProjectEta/Status Effect Definition")] // 에디터 메뉴에서 상태 이상 정의 에셋을 생성할 수 있게 등록
    public class StatusEffectDefinition : ScriptableObject // 27일차: 상태 이상 1종의 고정 규칙을 담는 ScriptableObject
    {
        [Header("식별")] // 인스펙터 식별 정보 구분선
        [SerializeField] private StatusEffectType _statusType; // 이 정의가 나타내는 단일 상태 이상 종류
        [SerializeField] private string _displayName; // 화면에 표시할 상태 이상 이름

        [Header("중첩·지속")] // 인스펙터 중첩·지속 정보 구분선
        [SerializeField] private StatusStackMode _stackMode; // 재적용 시 중첩할지 지속 턴만 갱신할지 결정
        [SerializeField] private int _maxStacks = 1; // 최대 중첩 수 (RefreshDuration 상태는 사실상 1 고정)
        [SerializeField] private int _defaultDurationTurns = 1; // 최초 적용 시 기본 지속 턴 수

        [Header("틱 피해")] // 28일차: 독·화상처럼 턴 종료 시 피해를 주는 상태 전용 구분선
        [SerializeField] private int _tickDamagePerStack; // 중첩 1당 턴 종료 시 입히는 고정 피해(기절·속박처럼 피해가 없는 상태는 0 유지)

        [Header("설명")] // 인스펙터 설명 구분선
        [TextArea] // 여러 줄 설명 편집 허용
        [SerializeField] private string _description; // 상태 이상 상세 설명

        public StatusEffectType StatusType => _statusType; // 외부에서 읽는 상태 이상 종류
        public string DisplayName => _displayName; // 외부에서 읽는 표시 이름
        public StatusStackMode StackMode => _stackMode; // 외부에서 읽는 중첩 방식
        public int MaxStacks => Mathf.Max(1, _maxStacks); // 최소 1 이상으로 보정된 최대 중첩 수
        public int DefaultDurationTurns => Mathf.Max(1, _defaultDurationTurns); // 최소 1 이상으로 보정된 기본 지속 턴
        public int TickDamagePerStack => Mathf.Max(0, _tickDamagePerStack); // 28일차: 음수 방지된 중첩당 틱 피해
        public string Description => _description; // 외부에서 읽는 설명
    }
}
