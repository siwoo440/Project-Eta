using UnityEngine; // MonoBehaviour, GameObject 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class BoardView : MonoBehaviour // BoardState 데이터를 3D 보드로 시각화하는 컴포넌트
    {
        [SerializeField] private float _tileSize = 1f; // 타일 한 칸의 크기
        [SerializeField] private float _tileGap = 0.05f; // 타일 사이 간격
        [SerializeField] private Color _idleColor = Color.white; // 선택되지 않은 타일 색상
        [SerializeField] private Color _installableHighlightColor = new Color(0.55f, 0.75f, 1f); // 설치 가능(아군 영역) 강조 색상
        [SerializeField] private Color _blockedHighlightColor = new Color(1f, 0.55f, 0.55f); // 설치 불가(적 영역) 강조 색상

        public float TileSize => _tileSize; // 외부에서 타일 크기를 읽기 위한 프로퍼티

        private BoardState _boardState; // 이 뷰가 표시하는 보드 데이터
        private Material _idleMaterial; // 기본 상태 공유 머티리얼
        private Material _installableHighlightMaterial; // 설치 가능 강조 공유 머티리얼
        private Material _blockedHighlightMaterial; // 설치 불가 강조 공유 머티리얼

        private void Awake() // 씬 시작 시 자동 호출되는 초기화 메서드
        {
            _boardState = new BoardState(); // 보드 데이터 생성
            _idleMaterial = CreateTileMaterial(_idleColor); // 기본 머티리얼 생성
            _installableHighlightMaterial = CreateTileMaterial(_installableHighlightColor); // 설치 가능 머티리얼 생성
            _blockedHighlightMaterial = CreateTileMaterial(_blockedHighlightColor); // 설치 불가 머티리얼 생성
            BuildTiles(); // 타일 오브젝트 100개 생성
        }

        private void BuildTiles() // 보드 데이터를 기반으로 타일 오브젝트를 만드는 메서드
        {
            for (int x = 0; x < BoardState.Width; x++) // 가로 방향으로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 세로 방향으로 순회
                {
                    var boardPosition = new Vector2Int(x, y); // 현재 칸 좌표 생성
                    var tileState = _boardState.GetTile(boardPosition); // 해당 좌표의 타일 데이터 조회
                    CreateTileObject(boardPosition, tileState); // 타일 오브젝트 생성
                }
            }
        }

        private void CreateTileObject(Vector2Int boardPosition, TileState tileState) // 타일 오브젝트 하나를 생성하는 메서드
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Quad); // 사각형 기본 도형 생성
            tile.name = $"Tile_{boardPosition.x}_{boardPosition.y}"; // 계층창에서 구분되도록 이름 지정
            tile.transform.SetParent(transform, false); // 이 컴포넌트의 자식으로 배치(로컬 좌표 유지)
            tile.transform.localPosition = BoardToLocalPosition(boardPosition, _tileSize); // 보드 좌표를 3D 위치로 변환해 배치
            tile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // 바닥에 눕도록 회전
            tile.transform.localScale = Vector3.one * (_tileSize - _tileGap); // 간격을 뺀 크기로 스케일 지정

            var highlightMaterial = tileState.IsPlayerPlacementArea ? _installableHighlightMaterial : _blockedHighlightMaterial; // 아군 영역이면 설치 가능 색, 아니면 설치 불가 색 선택
            var tileView = tile.AddComponent<TileView>(); // 타일 뷰 컴포넌트 부착
            tileView.Initialize(tileState, _idleMaterial, highlightMaterial); // 타일 뷰에 데이터와 머티리얼 주입
        }

        public static Vector3 BoardToLocalPosition(Vector2Int boardPosition, float tileSize) // 보드 좌표를 로컬 3D 위치로 변환하는 정적 메서드
        {
            float offsetX = (BoardState.Width - 1) / 2f; // 보드 중앙 정렬을 위한 가로 오프셋
            float offsetY = (BoardState.Height - 1) / 2f; // 보드 중앙 정렬을 위한 세로 오프셋
            return new Vector3((boardPosition.x - offsetX) * tileSize, 0f, (boardPosition.y - offsetY) * tileSize); // 오프셋을 적용한 3D 위치 반환
        }

        private static Material CreateTileMaterial(Color color) // 지정한 색상의 타일용 머티리얼을 생성하는 메서드
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit"); // URP 기본 셰이더 검색
            var material = new Material(shader) { color = color }; // 셰이더로 머티리얼을 만들고 색상 지정
            return material; // 생성한 머티리얼 반환
        }
    }
}
