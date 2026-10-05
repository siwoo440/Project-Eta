using System; // Array.Empty 사용
using UnityEngine; // ScriptableObject·SerializeField 사용

namespace ProjectEta.Abilities
{
    [CreateAssetMenu(fileName = "PieceAbilityDefinition", menuName = "ProjectEta/Piece Ability Definition")]
    public sealed class PieceAbilityDefinition : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string _abilityId;
        [SerializeField] private string _displayName;

        [Header("실행")]
        [SerializeField] private AbilityTrigger _trigger;
        [SerializeField] private AbilityActionCost _actionCost;
        [SerializeField] private AbilityEffectData[] _effects = Array.Empty<AbilityEffectData>();

        [Header("설명")]
        [TextArea]
        [SerializeField] private string _description;

        public string AbilityId => string.IsNullOrWhiteSpace(_abilityId) ? name : _abilityId;
        public string DisplayName => _displayName;
        public AbilityTrigger Trigger => _trigger;
        public AbilityActionCost ActionCost => _actionCost;
        public AbilityEffectData[] Effects => _effects ?? Array.Empty<AbilityEffectData>();
        public string Description => _description;
    }
}
