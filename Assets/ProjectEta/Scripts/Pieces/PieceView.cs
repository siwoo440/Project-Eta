using UnityEngine; // MonoBehaviour, GameObject 등을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView.BoardToLocalPosition을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public class PieceView : MonoBehaviour // 기물 데이터를 3D 화면에 표시하는 컴포넌트
    {
        [SerializeField] private Color _playerColor = new Color(0.15f, 0.4f, 0.9f); // 아군 기물 색상
        [SerializeField] private Color _enemyColor = new Color(0.9f, 0.2f, 0.2f); // 적군 기물 색상

        public PieceRuntimeState RuntimeState { get; private set; } // 이 뷰가 표시하는 런타임 상태

        public void Initialize(PieceRuntimeState runtimeState, float tileSize) // 외부에서 데이터를 주입해 초기화하는 메서드
        {
            RuntimeState = runtimeState; // 런타임 상태 저장
            ApplyBoardPosition(runtimeState.BoardPosition, tileSize); // 계층창 이름과 3D 위치를 현재 좌표에 맞춤

            var material = CreatePieceMaterial(runtimeState.IsPlayerPiece ? _playerColor : _enemyColor); // 아군/적군에 따라 머티리얼 생성
            BuildModel(runtimeState.Definition.MovementType, material); // 이동 타입에 맞는 3D 모델 생성
            AttachSelectionCollider(); // 클릭 판정용 콜라이더 부착
        }

        public void MoveTo(Vector2Int boardPosition, float tileSize) // 11일차: 실제 이동 실행 시 화면 위치를 새 좌표로 갱신하는 메서드
        {
            ApplyBoardPosition(boardPosition, tileSize); // 이름과 3D 위치를 새 좌표 기준으로 갱신(모델은 그대로 재사용)
        }

        private void ApplyBoardPosition(Vector2Int boardPosition, float tileSize) // 좌표에 맞춰 이름과 3D 위치를 함께 갱신하는 공통 메서드
        {
            name = $"Piece_{RuntimeState.Definition.DisplayName}_{boardPosition.x}_{boardPosition.y}"; // 계층창에서 구분되도록 이름 지정
            transform.localPosition = BoardView.BoardToLocalPosition(boardPosition, tileSize); // 보드 좌표를 3D 위치로 변환해 배치
        }

        private void BuildModel(PieceMovementType movementType, Material material) // 이동 타입별 모델을 만드는 메서드
        {
            var model = new GameObject("Model"); // 모델 파츠를 담을 빈 오브젝트 생성
            model.transform.SetParent(transform, false); // 이 컴포넌트의 자식으로 배치(로컬 좌표 유지)

            if (movementType == PieceMovementType.King) // 킹형이면
            {
                BuildKingModel(model.transform, material); // 킹 모델 생성
            }
            else // 그 외(현재는 폰만)이면
            {
                BuildPawnModel(model.transform, material); // 폰 모델 생성
            }
        }

        private static void BuildPawnModel(Transform parent, Material material) // 폰 모델을 프리미티브로 구성하는 메서드
        {
            // 받침 - 몸통 - 머리 순서로 쌓아 폰 실루엣을 구성한다.
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.3f, 0.04f, 0.3f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.14f, 0.22f, 0.14f), material); // 몸통 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.61f, 0f), new Vector3(0.18f, 0.18f, 0.18f), material); // 머리 파츠 생성
        }

        private static void BuildKingModel(Transform parent, Material material) // 킹 모델을 프리미티브로 구성하는 메서드
        {
            // 받침 - 기둥 - 머리 - 십자가(세로/가로) 순서로 쌓아 킹 실루엣을 구성한다.
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.05f, 0.36f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.2f, 0.35f, 0.2f), material); // 기둥 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.92f, 0f), new Vector3(0.24f, 0.24f, 0.24f), material); // 머리 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 1.14f, 0f), new Vector3(0.06f, 0.2f, 0.06f), material); // 십자가 세로 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 1.14f, 0f), new Vector3(0.18f, 0.06f, 0.06f), material); // 십자가 가로 파츠 생성
        }

        private static void CreatePart(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material) // 프리미티브 파츠 하나를 생성하는 메서드
        {
            var part = GameObject.CreatePrimitive(type); // 지정한 타입의 기본 도형 생성
            var partCollider = part.GetComponent<Collider>(); // 기본으로 붙는 콜라이더 참조
            if (partCollider != null) // 콜라이더가 있으면
            {
                Destroy(partCollider); // 파츠 개별 콜라이더는 제거(루트에만 콜라이더를 둘 예정)
            }

            part.transform.SetParent(parent, false); // 부모 아래로 배치(로컬 좌표 유지)
            part.transform.localPosition = localPosition; // 로컬 위치 지정
            part.transform.localScale = localScale; // 로컬 크기 지정
            part.GetComponent<Renderer>().sharedMaterial = material; // 공유 머티리얼 적용
        }

        private void AttachSelectionCollider() // 모델 전체를 감싸는 선택용 콜라이더를 붙이는 메서드
        {
            var renderers = GetComponentsInChildren<Renderer>(); // 자식에 있는 모든 렌더러 수집
            if (renderers.Length == 0) // 렌더러가 하나도 없으면
            {
                return; // 콜라이더를 붙일 수 없으므로 종료
            }

            var bounds = renderers[0].bounds; // 첫 렌더러의 경계값으로 시작
            for (int i = 1; i < renderers.Length; i++) // 나머지 렌더러를 순회하며
            {
                bounds.Encapsulate(renderers[i].bounds); // 전체를 감싸도록 경계값 확장
            }

            var selectionCollider = gameObject.AddComponent<CapsuleCollider>(); // 루트에 캡슐 콜라이더 추가
            selectionCollider.center = transform.InverseTransformPoint(bounds.center); // 콜라이더 중심을 로컬 좌표로 변환해 지정
            selectionCollider.height = bounds.size.y; // 콜라이더 높이를 모델 전체 높이로 지정
            selectionCollider.radius = Mathf.Max(bounds.size.x, bounds.size.z) / 2f; // 콜라이더 반지름을 가로/세로 중 큰 값 기준으로 지정
        }

        private static Material CreatePieceMaterial(Color color) // 기물용 머티리얼을 생성하는 메서드
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit"); // URP 기본 셰이더 검색
            return new Material(shader) { color = color }; // 셰이더로 머티리얼을 만들고 색상 지정 후 반환
        }
    }
}
