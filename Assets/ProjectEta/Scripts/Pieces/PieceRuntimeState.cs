using System.Collections.Generic; // List<T>와 IReadOnlyList<T> 사용
using UnityEngine; // Vector2Int, Mathf 사용

namespace ProjectEta.Pieces
{
    public class PieceRuntimeState
    {
        private int _currentHp;
        private Vector2Int _boardPosition;
        private int _movementCycleIndex;
        private readonly List<RuntimeStatusEffect> _statusEffects = new List<RuntimeStatusEffect>();
        private readonly HashSet<string> _usedBattleAbilityIds = new HashSet<string>();
        private int _lastMoveDistance;
        private bool _movedSinceOwnTurnStart;
        private bool _bastionFortifyReady;
        private bool _bastionFortifySpent;

        public PieceDefinition Definition { get; }

        public Vector2Int BoardPosition
        {
            get => _boardPosition;
            set
            {
                if (_boardPosition == value) return;
                _lastMoveDistance = Mathf.Abs(value.x - _boardPosition.x) + Mathf.Abs(value.y - _boardPosition.y);
                _movedSinceOwnTurnStart = true;
                _boardPosition = value;
                AdvanceMovementCycle();
            }
        }

        public bool IsPlayerPiece { get; set; }
        public bool IsSelected { get; set; }
        public bool CanMove { get; set; } = true;
        public bool CanAttack { get; set; } = true;
        public bool IsDead => _currentHp <= 0;
        public bool IsTemporarySummon { get; private set; } // 91일차: 전투 한정 임시 소환물 여부
        public int MovementCycleIndex => _movementCycleIndex;
        public int LastMoveDistance => _lastMoveDistance;
        public bool MovedSinceOwnTurnStart => _movedSinceOwnTurnStart;
        public bool BastionFortifyReady => _bastionFortifyReady;
        public bool BastionFortifySpent => _bastionFortifySpent;
        public IReadOnlyList<RuntimeStatusEffect> StatusEffects => _statusEffects;

        public int CurrentHp
        {
            get => _currentHp;
            set => _currentHp = Mathf.Max(0, value);
        }

        public PieceRuntimeState(PieceDefinition definition, Vector2Int boardPosition, bool isPlayerPiece)
        {
            Definition = definition;
            _boardPosition = boardPosition;
            IsPlayerPiece = isPlayerPiece;
            _currentHp = definition != null ? definition.BaseHp : 0;
            _movementCycleIndex = 0;
            _movedSinceOwnTurnStart = false;
            _bastionFortifyReady = false;
            _bastionFortifySpent = false;
            IsTemporarySummon = definition != null && definition.IsRuntimeTemporarySummonDefinition;
        }

        public void MarkAsTemporarySummon()
        {
            IsTemporarySummon = true;
        }


        public bool HasUsedBattleAbility(string abilityId)
        {
            return !string.IsNullOrWhiteSpace(abilityId) && _usedBattleAbilityIds.Contains(abilityId);
        }

        public bool TryMarkBattleAbilityUsed(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId)) return false;
            return _usedBattleAbilityIds.Add(abilityId);
        }

        public void ClearLastMoveDistance()
        {
            _lastMoveDistance = 0;
        }


        public void ResetOwnTurnMovement()
        {
            _movedSinceOwnTurnStart = false;
            _lastMoveDistance = 0;
        }

        public void SetBastionFortifyReady(bool ready)
        {
            _bastionFortifyReady = ready;
        }

        public void ResetBastionFortifyCycle()
        {
            _bastionFortifyReady = false;
            _bastionFortifySpent = false;
        }

        public bool TryConsumeBastionFortify()
        {
            if (!_bastionFortifyReady || _bastionFortifySpent) return false;
            _bastionFortifyReady = false;
            _bastionFortifySpent = true;
            return true;
        }

        public bool TryUseLazyBastionFortify()
        {
            if (_bastionFortifySpent || _movedSinceOwnTurnStart) return false;
            _bastionFortifySpent = true;
            return true;
        }

        public void ResetBattleAbilityUsage(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId)) return;
            _usedBattleAbilityIds.Remove(abilityId);
        }

        public void AdvanceMovementCycle()
        {
            int cycleLength = GetMovementCycleLength();
            if (cycleLength <= 1) return;
            _movementCycleIndex = (_movementCycleIndex + 1) % cycleLength;
        }

        public void RestoreMovementCycleIndex(int index)
        {
            int cycleLength = GetMovementCycleLength();
            if (cycleLength <= 1)
            {
                _movementCycleIndex = 0;
                return;
            }

            _movementCycleIndex = Mathf.Abs(index) % cycleLength;
        }

        private int GetMovementCycleLength()
        {
            if (Definition == null || string.IsNullOrEmpty(Definition.PieceId)) return 1;

            switch (Definition.PieceId)
            {
                case "chameleon":
                    return 4;
                case "illusionist":
                    return 3;
                case "phantom_general":
                    return 5;
                default:
                    return 1;
            }
        }

        public bool ApplyStatus(StatusEffectDefinition statusDefinition)
        {
            if (statusDefinition == null) return false;

            if (Definition != null && (Definition.ImmuneStatusTags & statusDefinition.StatusType) != 0)
            {
                return false;
            }

            RuntimeStatusEffect existing = FindStatus(statusDefinition.StatusType);
            if (existing != null)
            {
                existing.Reapply();
            }
            else
            {
                _statusEffects.Add(new RuntimeStatusEffect(statusDefinition));
            }

            RefreshActionFlags();
            return true;
        }

        public bool HasStatus(StatusEffectType statusType)
        {
            return FindStatus(statusType) != null;
        }

        public RuntimeStatusEffect FindStatus(StatusEffectType statusType)
        {
            for (int i = 0; i < _statusEffects.Count; i++)
            {
                if (_statusEffects[i].Definition.StatusType == statusType)
                {
                    return _statusEffects[i];
                }
            }

            return null;
        }

        public void RemoveStatus(StatusEffectType statusType)
        {
            _statusEffects.RemoveAll(effect => effect.Definition.StatusType == statusType);
            RefreshActionFlags();
        }

        public void TickStatusEffects()
        {
            for (int i = _statusEffects.Count - 1; i >= 0; i--)
            {
                if (_statusEffects[i].Tick())
                {
                    _statusEffects.RemoveAt(i);
                }
            }

            RefreshActionFlags();
        }

        public void RestoreStatusEffect(StatusEffectDefinition statusDefinition, int remainingTurns, int stackCount)
        {
            if (statusDefinition == null) return;

            var effect = new RuntimeStatusEffect(statusDefinition);
            effect.RestoreState(remainingTurns, stackCount);
            _statusEffects.Add(effect);
            RefreshActionFlags();
        }

        private void RefreshActionFlags()
        {
            bool isStunned = HasStatus(StatusEffectType.Stun);
            bool isRooted = HasStatus(StatusEffectType.Root);

            CanAttack = !isStunned;
            CanMove = !isStunned && !isRooted;
        }
    }
}
