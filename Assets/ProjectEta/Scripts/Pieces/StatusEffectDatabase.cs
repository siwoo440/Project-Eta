using System.Collections.Generic; // List<T>와 IReadOnlyList<T>를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject, SerializeField 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    [CreateAssetMenu(fileName = "StatusEffectDatabase", menuName = "ProjectEta/Status Effect Database")] // 에디터 메뉴에서 에셋 생성 가능하게 등록
    public class StatusEffectDatabase : ScriptableObject // StatusEffectType으로 StatusEffectDefinition을 찾아주는 조회용 데이터 에셋
    {
        [SerializeField] private List<StatusEffectDefinition> _definitions = new List<StatusEffectDefinition>(); // 등록된 상태 이상 정의 목록

        public IReadOnlyList<StatusEffectDefinition> Definitions => _definitions; // 외부에서 읽는 전체 목록

        public StatusEffectDefinition FindByType(StatusEffectType statusType) // 단일 상태 종류로 정의를 찾는 메서드
        {
            for (int i = 0; i < _definitions.Count; i++) // 등록된 정의를 처음부터 순회
            {
                if (_definitions[i] != null && _definitions[i].StatusType == statusType) // 비어있지 않고 종류가 일치하면
                {
                    return _definitions[i]; // 해당 정의를 반환
                }
            }

            return null; // 못 찾으면 null 반환
        }
    }
}
