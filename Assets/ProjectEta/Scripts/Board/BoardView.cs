using System.Collections.Generic; // List<int>를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Mesh 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class BoardView : MonoBehaviour // BoardState 데이터를 하나로 이어진 체스판 메시로 시각화하는 컴포넌트
    {
        [SerializeField] private float _tileSize = 1f; // 칸 한 개의 크기
        [SerializeField] private Color _idleLightColor = Color.white; // 체스판 밝은 칸 색상
        [SerializeField] private Color _idleDarkColor = new Color(0.12f, 0.12f, 0.12f); // 체스판 어두운 칸 색상
        [SerializeField] private Color _installableHighlightColor = new Color(0.55f, 0.75f, 1f); // 설치 가능(아군 영역) 강조 색상
        [SerializeField] private Color _blockedHighlightColor = new Color(1f, 0.55f, 0.55f); // 설치 불가(적 영역) 강조 색상
        [SerializeField] private Color _gridLineColor = new Color(0.15f, 0.15f, 0.15f); // 칸 경계 격자선 색상
        [SerializeField] private int _gridLineThicknessPx = 3; // 격자선 두께(생성할 텍스처 픽셀 기준)

        public float TileSize => _tileSize; // 외부에서 타일 크기를 읽기 위한 프로퍼티

        private BoardState _boardState; // 이 뷰가 표시하는 보드 데이터
        private Mesh _mesh; // 보드 전체를 하나로 표현하는 메시
        private readonly List<int> _idleLightTriangles = new List<int>(); // 밝은 칸 서브메시에 속한 삼각형 인덱스 목록
        private readonly List<int> _idleDarkTriangles = new List<int>(); // 어두운 칸 서브메시에 속한 삼각형 인덱스 목록
        private readonly List<int> _installableTriangles = new List<int>(); // 설치 가능 강조 서브메시에 속한 삼각형 인덱스 목록
        private readonly List<int> _blockedTriangles = new List<int>(); // 설치 불가 강조 서브메시에 속한 삼각형 인덱스 목록
        private Vector2Int? _highlightedCell; // 현재 강조 표시 중인 칸(없으면 null)

        private void Awake() // 씬 시작 시 자동 호출되는 초기화 메서드
        {
            _boardState = new BoardState(); // 보드 데이터 생성
            BuildBoardMesh(); // 하나로 이어진 체스판 메시 생성
        }

        public TileState GetTile(Vector2Int cell) => _boardState.GetTile(cell); // 외부에서 칸 좌표로 타일 데이터를 조회하기 위한 메서드

        public bool TryGetCellFromWorldPoint(Vector3 worldPoint, out Vector2Int cell) // 월드 좌표를 보드 칸 좌표로 변환하는 메서드
        {
            var local = transform.InverseTransformPoint(worldPoint); // 보드 로컬 좌표로 변환
            float offsetX = (BoardState.Width - 1) / 2f; // 가로 중앙 정렬 오프셋
            float offsetY = (BoardState.Height - 1) / 2f; // 세로 중앙 정렬 오프셋
            int x = Mathf.RoundToInt(local.x / _tileSize + offsetX); // 가로 칸 좌표 역산
            int y = Mathf.RoundToInt(local.z / _tileSize + offsetY); // 세로 칸 좌표 역산
            cell = new Vector2Int(x, y); // 계산한 칸 좌표 구성
            return _boardState.IsInsideBoard(cell); // 보드 범위 안인지 반환
        }

        public void HighlightCell(Vector2Int cell) // 지정한 칸을 강조 표시하는 메서드
        {
            ClearHighlight(); // 이전 강조를 먼저 해제

            var tileState = _boardState.GetTile(cell); // 강조할 칸의 타일 데이터 조회
            if (tileState == null) // 범위 밖 좌표면
            {
                return; // 처리하지 않고 종료
            }

            var indices = GetCellTriangleIndices(cell); // 이 칸에 해당하는 삼각형 인덱스 계산
            var currentIdleList = IsLightSquare(cell) ? _idleLightTriangles : _idleDarkTriangles; // 이 칸이 원래 속했던 체스판 색 목록 선택
            foreach (var index in indices) // 각 인덱스를 순회하며
            {
                currentIdleList.Remove(index); // 원래 속했던 서브메시에서 제거
            }

            var targetList = tileState.IsPlayerPlacementArea ? _installableTriangles : _blockedTriangles; // 아군 영역이면 설치 가능, 아니면 설치 불가 목록 선택
            targetList.AddRange(indices); // 대상 서브메시에 추가

            _highlightedCell = cell; // 현재 강조 칸 갱신
            ApplySubMeshes(); // 변경된 서브메시 구성을 메시에 반영
        }

        public void ClearHighlight() // 현재 강조 표시를 해제하는 메서드
        {
            if (_highlightedCell == null) // 강조된 칸이 없으면
            {
                return; // 할 일이 없으므로 종료
            }

            var cell = _highlightedCell.Value; // 강조 해제할 칸 좌표
            var tileState = _boardState.GetTile(cell); // 해당 칸의 타일 데이터 조회
            var sourceList = tileState.IsPlayerPlacementArea ? _installableTriangles : _blockedTriangles; // 현재 속해있던 서브메시 목록 선택

            var indices = GetCellTriangleIndices(cell); // 이 칸에 해당하는 삼각형 인덱스 계산
            foreach (var index in indices) // 각 인덱스를 순회하며
            {
                sourceList.Remove(index); // 강조 서브메시에서 제거
            }

            var restoreList = IsLightSquare(cell) ? _idleLightTriangles : _idleDarkTriangles; // 되돌아갈 체스판 색 목록 선택
            restoreList.AddRange(indices); // 원래 체스판 색 서브메시로 되돌림
            _highlightedCell = null; // 강조 칸 상태 초기화
            ApplySubMeshes(); // 변경된 서브메시 구성을 메시에 반영
        }

        private void BuildBoardMesh() // 10x10 칸 전체를 이음매 없는 하나의 메시로 체스판 패턴과 함께 만드는 메서드
        {
            _mesh = new Mesh { name = "BoardMesh" }; // 새 메시 객체 생성

            int cellCount = BoardState.Width * BoardState.Height; // 전체 칸 수
            var vertices = new Vector3[cellCount * 4]; // 칸마다 정점 4개씩 사용(칸 사이 공유 없이 독립 착색을 위함)
            var uvs = new Vector2[cellCount * 4]; // 칸마다 0~1 범위 UV를 부여해 격자선 텍스처가 칸 단위로 반복되게 함
            float half = _tileSize / 2f; // 칸 절반 크기(정점 오프셋 계산용)

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 방향으로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 방향으로 순회
                {
                    var cell = new Vector2Int(x, y); // 현재 칸 좌표
                    int vertexOffset = GetCellIndex(cell) * 4; // 이 칸이 사용할 정점 시작 위치
                    var center = BoardToLocalPosition(cell, _tileSize); // 칸 중심의 로컬 위치 계산

                    vertices[vertexOffset + 0] = center + new Vector3(-half, 0f, -half); // 좌하단 정점
                    vertices[vertexOffset + 1] = center + new Vector3(half, 0f, -half); // 우하단 정점
                    vertices[vertexOffset + 2] = center + new Vector3(half, 0f, half); // 우상단 정점
                    vertices[vertexOffset + 3] = center + new Vector3(-half, 0f, half); // 좌상단 정점

                    uvs[vertexOffset + 0] = new Vector2(0f, 0f); // 좌하단 UV
                    uvs[vertexOffset + 1] = new Vector2(1f, 0f); // 우하단 UV
                    uvs[vertexOffset + 2] = new Vector2(1f, 1f); // 우상단 UV
                    uvs[vertexOffset + 3] = new Vector2(0f, 1f); // 좌상단 UV

                    var idleList = IsLightSquare(cell) ? _idleLightTriangles : _idleDarkTriangles; // 좌표 홀짝에 따라 밝은/어두운 칸으로 배정
                    idleList.AddRange(GetCellTriangleIndices(cell)); // 처음에는 모든 칸을 체스판 색 서브메시에 배정
                }
            }

            _mesh.vertices = vertices; // 계산한 정점을 메시에 반영
            _mesh.uv = uvs; // 계산한 UV를 메시에 반영
            _mesh.subMeshCount = 4; // 밝은 칸/어두운 칸/설치가능/설치불가 4개 서브메시로 분리
            ApplySubMeshes(); // 초기 서브메시 구성을 메시에 반영

            _mesh.RecalculateNormals(); // 정점 배치를 바탕으로 법선 자동 계산
            _mesh.RecalculateBounds(); // 콜라이더·컬링에 필요한 경계 상자 자동 계산

            var meshFilter = gameObject.AddComponent<MeshFilter>(); // 메시 필터 컴포넌트 추가
            meshFilter.sharedMesh = _mesh; // 생성한 메시 연결

            var gridTexture = CreateGridLineTexture(); // 격자선이 그려진 공용 텍스처 생성

            var meshRenderer = gameObject.AddComponent<MeshRenderer>(); // 메시 렌더러 컴포넌트 추가
            meshRenderer.sharedMaterials = new[] // 서브메시 순서에 맞춰 머티리얼 지정
            {
                CreateBoardMaterial(_idleLightColor, gridTexture), // 서브메시 0: 밝은 칸
                CreateBoardMaterial(_idleDarkColor, gridTexture), // 서브메시 1: 어두운 칸
                CreateBoardMaterial(_installableHighlightColor, gridTexture), // 서브메시 2: 설치 가능 강조
                CreateBoardMaterial(_blockedHighlightColor, gridTexture) // 서브메시 3: 설치 불가 강조
            };

            var meshCollider = gameObject.AddComponent<MeshCollider>(); // 클릭 판정을 위한 메시 콜라이더 추가
            meshCollider.sharedMesh = _mesh; // 같은 메시를 충돌 판정에도 사용
        }

        private void ApplySubMeshes() // 네 서브메시 목록을 실제 메시에 반영하는 메서드
        {
            _mesh.SetTriangles(_idleLightTriangles, 0); // 밝은 칸 서브메시 반영
            _mesh.SetTriangles(_idleDarkTriangles, 1); // 어두운 칸 서브메시 반영
            _mesh.SetTriangles(_installableTriangles, 2); // 설치 가능 강조 서브메시 반영
            _mesh.SetTriangles(_blockedTriangles, 3); // 설치 불가 강조 서브메시 반영
        }

        private static bool IsLightSquare(Vector2Int cell) => (cell.x + cell.y) % 2 == 0; // 좌표 합의 홀짝으로 체스판의 밝은 칸 여부를 판정하는 메서드

        private static int GetCellIndex(Vector2Int cell) => cell.x * BoardState.Height + cell.y; // 칸 좌표를 1차원 인덱스로 변환하는 메서드

        private static int[] GetCellTriangleIndices(Vector2Int cell) // 칸 좌표에 해당하는 6개 삼각형 정점 인덱스를 계산하는 메서드
        {
            int v = GetCellIndex(cell) * 4; // 이 칸의 정점 시작 위치
            return new[] { v, v + 2, v + 1, v, v + 3, v + 2 }; // 위쪽(+Y)을 향하도록 감은 두 삼각형의 인덱스
        }

        public static Vector3 BoardToLocalPosition(Vector2Int boardPosition, float tileSize) // 보드 좌표를 로컬 3D 위치로 변환하는 정적 메서드
        {
            float offsetX = (BoardState.Width - 1) / 2f; // 보드 중앙 정렬을 위한 가로 오프셋
            float offsetY = (BoardState.Height - 1) / 2f; // 보드 중앙 정렬을 위한 세로 오프셋
            return new Vector3((boardPosition.x - offsetX) * tileSize, 0f, (boardPosition.y - offsetY) * tileSize); // 오프셋을 적용한 3D 위치 반환
        }

        private Texture2D CreateGridLineTexture() // 칸 테두리에 격자선을 그린 텍스처를 생성하는 메서드
        {
            const int size = 64; // 생성할 텍스처 한 변의 픽셀 크기
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) // 격자선 전용 텍스처 생성
            {
                wrapMode = TextureWrapMode.Clamp, // 칸마다 독립적인 UV(0~1)를 쓰므로 반복 없이 잘라내기만 함
                filterMode = FilterMode.Bilinear // 확대해도 부드럽게 보이도록 보간 방식 지정
            };

            var pixels = new Color[size * size]; // 픽셀 색상 배열
            for (int y = 0; y < size; y++) // 세로 방향으로 순회
            {
                for (int x = 0; x < size; x++) // 가로 방향으로 순회
                {
                    bool isBorder = x < _gridLineThicknessPx || x >= size - _gridLineThicknessPx // 왼쪽/오른쪽 테두리인지 판정
                        || y < _gridLineThicknessPx || y >= size - _gridLineThicknessPx; // 위/아래 테두리인지 판정
                    pixels[y * size + x] = isBorder ? _gridLineColor : Color.white; // 테두리면 격자선 색, 아니면 흰색(칸 고유 색을 그대로 곱해서 보여줌)
                }
            }

            texture.SetPixels(pixels); // 계산한 픽셀을 텍스처에 반영
            texture.Apply(); // 변경 사항을 GPU에 업로드
            return texture; // 생성한 텍스처 반환
        }

        private static Material CreateBoardMaterial(Color color, Texture2D gridTexture) // 지정한 색상과 격자선 텍스처로 보드용 머티리얼을 생성하는 메서드
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit"); // URP 기본 셰이더 검색
            var material = new Material(shader) { color = color, mainTexture = gridTexture }; // 셰이더로 머티리얼을 만들고 색상·텍스처 지정
            return material; // 생성한 머티리얼 반환
        }
    }
}
