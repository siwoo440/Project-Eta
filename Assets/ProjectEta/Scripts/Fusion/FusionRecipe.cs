using UnityEngine;
using ProjectEta.Pieces;

namespace ProjectEta.Fusion
{
    [CreateAssetMenu(fileName = "FusionRecipe", menuName = "ProjectEta/Fusion Recipe")]
    public class FusionRecipe : ScriptableObject
    {
        [SerializeField] private PieceDefinition _materialA;
        [SerializeField] private PieceDefinition _materialB;
        [SerializeField] private PieceDefinition _result;
        [SerializeField] private bool _isHiddenRecipe;

        public PieceDefinition MaterialA => _materialA;
        public PieceDefinition MaterialB => _materialB;
        public PieceDefinition Result => _result;
        public bool IsHiddenRecipe => _isHiddenRecipe;
    }
}
