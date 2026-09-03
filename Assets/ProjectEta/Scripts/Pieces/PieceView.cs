using UnityEngine;
using ProjectEta.Board;

namespace ProjectEta.Pieces
{
    public class PieceView : MonoBehaviour
    {
        [SerializeField] private Color _playerColor = new Color(0.15f, 0.4f, 0.9f);
        [SerializeField] private Color _enemyColor = new Color(0.9f, 0.2f, 0.2f);

        public PieceRuntimeState RuntimeState { get; private set; }

        public void Initialize(PieceRuntimeState runtimeState, float tileSize)
        {
            RuntimeState = runtimeState;
            name = $"Piece_{runtimeState.Definition.DisplayName}_{runtimeState.BoardPosition.x}_{runtimeState.BoardPosition.y}";
            transform.localPosition = BoardView.BoardToLocalPosition(runtimeState.BoardPosition, tileSize);

            var material = CreatePieceMaterial(runtimeState.IsPlayerPiece ? _playerColor : _enemyColor);
            BuildModel(runtimeState.Definition.MovementType, material);
            AttachSelectionCollider();
        }

        private void BuildModel(PieceMovementType movementType, Material material)
        {
            var model = new GameObject("Model");
            model.transform.SetParent(transform, false);

            if (movementType == PieceMovementType.King)
            {
                BuildKingModel(model.transform, material);
            }
            else
            {
                BuildPawnModel(model.transform, material);
            }
        }

        private static void BuildPawnModel(Transform parent, Material material)
        {
            // 받침 - 몸통 - 머리 순서로 쌓아 폰 실루엣을 구성한다.
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.3f, 0.04f, 0.3f), material);
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.14f, 0.22f, 0.14f), material);
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.61f, 0f), new Vector3(0.18f, 0.18f, 0.18f), material);
        }

        private static void BuildKingModel(Transform parent, Material material)
        {
            // 받침 - 기둥 - 머리 - 십자가(세로/가로) 순서로 쌓아 킹 실루엣을 구성한다.
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.05f, 0.36f), material);
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.2f, 0.35f, 0.2f), material);
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.92f, 0f), new Vector3(0.24f, 0.24f, 0.24f), material);
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 1.14f, 0f), new Vector3(0.06f, 0.2f, 0.06f), material);
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 1.14f, 0f), new Vector3(0.18f, 0.06f, 0.06f), material);
        }

        private static void CreatePart(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            var partCollider = part.GetComponent<Collider>();
            if (partCollider != null)
            {
                Destroy(partCollider);
            }

            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void AttachSelectionCollider()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var selectionCollider = gameObject.AddComponent<CapsuleCollider>();
            selectionCollider.center = transform.InverseTransformPoint(bounds.center);
            selectionCollider.height = bounds.size.y;
            selectionCollider.radius = Mathf.Max(bounds.size.x, bounds.size.z) / 2f;
        }

        private static Material CreatePieceMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            return new Material(shader) { color = color };
        }
    }
}
