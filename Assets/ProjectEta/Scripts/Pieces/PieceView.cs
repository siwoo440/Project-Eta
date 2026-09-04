using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, GameObject, PrimitiveType 등을 사용하기 위한 네임스페이스
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
            BuildModel(runtimeState.Definition, material); // PieceId 기준으로 어울리는 3D 모델 생성
            AttachSelectionCollider(); // 클릭 판정용 단일 콜라이더 부착
        }

        public void MoveTo(Vector2Int boardPosition, float tileSize) // 실제 이동 실행 시 화면 위치를 새 좌표로 갱신하는 메서드
        {
            ApplyBoardPosition(boardPosition, tileSize); // 이름과 3D 위치를 새 좌표 기준으로 갱신
        }

        private void ApplyBoardPosition(Vector2Int boardPosition, float tileSize) // 좌표에 맞춰 이름과 3D 위치를 함께 갱신하는 공통 메서드
        {
            string displayName = RuntimeState != null && RuntimeState.Definition != null ? RuntimeState.Definition.DisplayName : "Piece"; // 안전한 표시 이름 계산
            name = $"Piece_{displayName}_{boardPosition.x}_{boardPosition.y}"; // 계층창에서 구분되도록 이름 지정
            transform.localPosition = BoardView.BoardToLocalPosition(boardPosition, tileSize); // 보드 좌표를 3D 위치로 변환해 배치
        }

        public static string GetModelKey(PieceDefinition definition) // 테스트와 디버그에서 현재 기물의 모델 분기를 확인하는 메서드
        {
            if (definition == null) return "pawn"; // 정의가 없으면 안전한 기본 모델 사용
            if (!string.IsNullOrWhiteSpace(definition.PieceId)) return definition.PieceId.ToLowerInvariant(); // PieceId 우선 사용
            if (!string.IsNullOrWhiteSpace(definition.name)) return definition.name.ToLowerInvariant(); // 에셋 이름 대체 사용
            return "pawn"; // 아무 정보도 없으면 폰 모델 사용
        }

        private void BuildModel(PieceDefinition definition, Material material) // 기물 id별 모델을 만드는 메서드
        {
            var model = new GameObject("Model"); // 모델 파츠를 담을 빈 오브젝트 생성
            model.transform.SetParent(transform, false); // 이 컴포넌트의 자식으로 배치

            switch (GetModelKey(definition)) // 26일차: 26종 전체의 전용 실루엣 분기
            {
                case "king":
                    BuildKingModel(model.transform, material); // 킹 모델 생성
                    break;
                case "pawn":
                    BuildPawnModel(model.transform, material); // 폰 모델 생성
                    break;
                case "knight":
                    BuildKnightModel(model.transform, material); // 나이트 모델 생성
                    break;
                case "bishop":
                    BuildBishopModel(model.transform, material); // 비숍 모델 생성
                    break;
                case "rook":
                    BuildRookModel(model.transform, material); // 룩 모델 생성
                    break;
                case "queen":
                    BuildQueenModel(model.transform, material); // 퀸 모델 생성
                    break;
                case "archbishop":
                    BuildArchbishopModel(model.transform, material); // 아크비숍 모델 생성
                    break;
                case "chancellor":
                    BuildChancellorModel(model.transform, material); // 챈슬러 모델 생성
                    break;
                case "amazon":
                    BuildAmazonModel(model.transform, material); // 아마존 모델 생성
                    break;
                case "wazir":
                    BuildWazirModel(model.transform, material); // 와지르 모델 생성
                    break;
                case "ferz":
                    BuildFerzModel(model.transform, material); // 페르즈 모델 생성
                    break;
                case "mann":
                    BuildMannModel(model.transform, material); // 만 모델 생성
                    break;
                case "dabbaba":
                    BuildDabbabaModel(model.transform, material); // 다바바 모델 생성
                    break;
                case "alfil":
                    BuildAlfilModel(model.transform, material); // 알필 모델 생성
                    break;
                case "camel":
                    BuildCamelModel(model.transform, material); // 카멜 모델 생성
                    break;
                case "zebra":
                    BuildZebraModel(model.transform, material); // 제브라 모델 생성
                    break;
                case "centaur":
                    BuildCentaurModel(model.transform, material); // 센타우르 모델 생성
                    break;
                case "waffle":
                    BuildWaffleModel(model.transform, material); // 와플 모델 생성
                    break;
                case "nightrider":
                    BuildNightriderModel(model.transform, material); // 나이트라이더 모델 생성
                    break;
                case "camelrider":
                    BuildCamelriderModel(model.transform, material); // 카멜라이더 모델 생성
                    break;
                case "grasshopper":
                    BuildGrasshopperModel(model.transform, material); // 그래스호퍼 모델 생성
                    break;
                case "cannon":
                    BuildCannonModel(model.transform, material); // 캐논 모델 생성
                    break;
                case "canvasser":
                    BuildCanvasserModel(model.transform, material); // 캔버서 모델 생성
                    break;
                case "caliph":
                    BuildCaliphModel(model.transform, material); // 칼리프 모델 생성
                    break;
                case "squirrel":
                    BuildSquirrelModel(model.transform, material); // 스쿼럴 모델 생성
                    break;
                case "chameleon":
                    BuildChameleonModel(model.transform, material); // 카멜레온 모델 생성
                    break;
                default:
                    BuildPawnModel(model.transform, material); // 알 수 없는 기물은 폰 모델로 대체
                    break;
            }
        }

        private static void BuildPawnModel(Transform parent, Material material) // 폰 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.3f, 0.04f, 0.3f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.14f, 0.22f, 0.14f), material); // 몸통 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.61f, 0f), new Vector3(0.18f, 0.18f, 0.18f), material); // 머리 파츠 생성
        }

        private static void BuildKingModel(Transform parent, Material material) // 킹 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.05f, 0.36f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.2f, 0.35f, 0.2f), material); // 기둥 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.92f, 0f), new Vector3(0.24f, 0.24f, 0.24f), material); // 머리 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 1.14f, 0f), new Vector3(0.06f, 0.2f, 0.06f), material); // 십자가 세로 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 1.14f, 0f), new Vector3(0.18f, 0.06f, 0.06f), material); // 십자가 가로 파츠 생성
        }

        private static void BuildKnightModel(Transform parent, Material material) // 나이트 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.34f, 0.05f, 0.34f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(0.16f, 0.2f, 0.16f), material); // 몸통 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.56f, 0.02f), new Vector3(0.16f, 0.3f, 0.22f), material, Quaternion.Euler(20f, 0f, 0f)); // 머리 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.66f, 0.16f), new Vector3(0.1f, 0.1f, 0.18f), material, Quaternion.Euler(-15f, 0f, 0f)); // 주둥이 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.05f, 0.74f, -0.02f), new Vector3(0.05f, 0.1f, 0.05f), material, Quaternion.Euler(25f, 0f, -20f)); // 귀 파츠 생성
        }

        private static void BuildBishopModel(Transform parent, Material material) // 비숍 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.32f, 0.05f, 0.32f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0f), new Vector3(0.15f, 0.37f, 0.15f), material); // 몸통 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.86f, 0f), new Vector3(0.2f, 0.2f, 0.2f), material); // 머리 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 1.04f, 0f), new Vector3(0.08f, 0.08f, 0.08f), material); // 꼭대기 구슬 생성
        }

        private static void BuildRookModel(Transform parent, Material material) // 룩 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.05f, 0.36f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(0.26f, 0.3f, 0.26f), material); // 몸통 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.68f, 0f), new Vector3(0.32f, 0.04f, 0.32f), material); // 상단 원판 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.22f, 0.78f, 0f), new Vector3(0.1f, 0.12f, 0.1f), material); // 흉벽 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.22f, 0.78f, 0f), new Vector3(0.1f, 0.12f, 0.1f), material); // 흉벽 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.22f), new Vector3(0.1f, 0.12f, 0.1f), material); // 흉벽 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.78f, -0.22f), new Vector3(0.1f, 0.12f, 0.1f), material); // 흉벽 파츠 생성
        }

        private static void BuildQueenModel(Transform parent, Material material) // 퀸 모델을 프리미티브로 구성하는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.36f, 0.05f, 0.36f), material); // 받침 파츠 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.19f, 0.45f, 0.19f), material); // 몸통 파츠 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 1.0f, 0f), new Vector3(0.26f, 0.26f, 0.26f), material); // 머리 파츠 생성

            const int spikeCount = 5; // 왕관 스파이크 개수
            const float radius = 0.14f; // 스파이크 반경
            const float spikeY = 1.16f; // 스파이크 높이
            for (int i = 0; i < spikeCount; i++) // 스파이크를 원형으로 배치
            {
                float angle = i * Mathf.PI * 2f / spikeCount; // 이번 스파이크의 각도 계산
                var spikePosition = new Vector3(Mathf.Cos(angle) * radius, spikeY, Mathf.Sin(angle) * radius); // 위치 계산
                CreatePart(parent, PrimitiveType.Sphere, spikePosition, new Vector3(0.07f, 0.07f, 0.07f), material); // 스파이크 파츠 생성
            }
        }

        private static void BuildArchbishopModel(Transform parent, Material material) // 비숍과 나이트의 느낌을 섞은 모델을 만드는 메서드
        {
            BuildBishopModel(parent, material); // 비숍 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.66f, 0.17f), new Vector3(0.11f, 0.1f, 0.18f), material, Quaternion.Euler(-10f, 0f, 0f)); // 말머리 느낌의 주둥이 추가
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.06f, 0.76f, -0.02f), new Vector3(0.05f, 0.11f, 0.05f), material, Quaternion.Euler(20f, 0f, -18f)); // 귀 추가
        }

        private static void BuildChancellorModel(Transform parent, Material material) // 룩과 나이트의 느낌을 섞은 모델을 만드는 메서드
        {
            BuildRookModel(parent, material); // 룩 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0.12f), new Vector3(0.16f, 0.16f, 0.2f), material, Quaternion.Euler(16f, 0f, 0f)); // 전면 말머리 파츠 추가
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.7f, 0.2f), new Vector3(0.1f, 0.08f, 0.16f), material, Quaternion.Euler(-18f, 0f, 0f)); // 주둥이 파츠 추가
        }

        private static void BuildAmazonModel(Transform parent, Material material) // 퀸과 나이트의 느낌을 섞은 모델을 만드는 메서드
        {
            BuildQueenModel(parent, material); // 퀸 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.73f, 0.2f), new Vector3(0.12f, 0.11f, 0.18f), material, Quaternion.Euler(-12f, 0f, 0f)); // 전면 말머리 파츠 추가
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.05f, 0.86f, 0.04f), new Vector3(0.05f, 0.1f, 0.05f), material, Quaternion.Euler(22f, 0f, -12f)); // 귀 파츠 추가
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.22f, 0.9f, 0f), new Vector3(0.06f, 0.18f, 0.2f), material, Quaternion.Euler(0f, 0f, 26f)); // 오른쪽 날개 장식
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.22f, 0.9f, 0f), new Vector3(0.06f, 0.18f, 0.2f), material, Quaternion.Euler(0f, 0f, -26f)); // 왼쪽 날개 장식
        }

        private static void BuildWazirModel(Transform parent, Material material) // 십자 한 칸의 짧고 단단한 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.28f, 0.05f, 0.28f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.26f, 0f), new Vector3(0.14f, 0.18f, 0.14f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.48f, 0f), new Vector3(0.28f, 0.08f, 0.08f), material); // 가로 팔 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.48f, 0f), new Vector3(0.08f, 0.08f, 0.28f), material); // 세로 팔 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.66f, 0f), new Vector3(0.14f, 0.14f, 0.14f), material); // 머리 구슬 생성
        }

        private static void BuildFerzModel(Transform parent, Material material) // 대각 한 칸의 날카로운 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.28f, 0.05f, 0.28f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.24f, 0f), new Vector3(0.12f, 0.16f, 0.12f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.52f, 0f), new Vector3(0.1f, 0.34f, 0.1f), material, Quaternion.Euler(0f, 0f, 45f)); // 대각 기둥 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.78f, 0f), new Vector3(0.12f, 0.12f, 0.12f), material); // 머리 구슬 생성
        }

        private static void BuildMannModel(Transform parent, Material material) // 모든 방향 한 칸의 인간형 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.3f, 0.05f, 0.3f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.16f, 0.22f, 0.16f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.64f, 0f), new Vector3(0.16f, 0.16f, 0.16f), material); // 머리 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.17f, 0.36f, 0f), new Vector3(0.12f, 0.08f, 0.08f), material); // 오른팔 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.17f, 0.36f, 0f), new Vector3(0.12f, 0.08f, 0.08f), material); // 왼팔 생성
        }

        private static void BuildDabbabaModel(Transform parent, Material material) // 두 칸 도약의 묵직한 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(0.42f, 0.08f, 0.42f), material); // 큰 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f), new Vector3(0.24f, 0.18f, 0.24f), material); // 두꺼운 몸체 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0.15f, 0.54f, 0f), new Vector3(0.1f, 0.12f, 0.1f), material); // 오른쪽 북 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(-0.15f, 0.54f, 0f), new Vector3(0.1f, 0.12f, 0.1f), material); // 왼쪽 북 생성
        }

        private static void BuildAlfilModel(Transform parent, Material material) // 고전 코끼리 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(0.33f, 0.06f, 0.33f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(0.26f, 0.18f, 0.22f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.54f, 0.14f), new Vector3(0.16f, 0.16f, 0.18f), material); // 머리 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.11f, 0.48f, 0.26f), new Vector3(0.04f, 0.14f, 0.04f), material, Quaternion.Euler(0f, 0f, 20f)); // 오른쪽 엄니 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.11f, 0.48f, 0.26f), new Vector3(0.04f, 0.14f, 0.04f), material, Quaternion.Euler(0f, 0f, -20f)); // 왼쪽 엄니 생성
        }

        private static void BuildCamelModel(Transform parent, Material material) // 낙타 등 형태를 가진 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.34f, 0.05f, 0.34f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.24f, 0f), new Vector3(0.28f, 0.12f, 0.18f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(-0.08f, 0.43f, 0f), new Vector3(0.16f, 0.16f, 0.16f), material); // 첫 번째 혹 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0.08f, 0.43f, 0f), new Vector3(0.16f, 0.16f, 0.16f), material); // 두 번째 혹 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.56f, 0.14f), new Vector3(0.08f, 0.22f, 0.08f), material, Quaternion.Euler(18f, 0f, 0f)); // 목 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.7f, 0.22f), new Vector3(0.1f, 0.1f, 0.12f), material); // 머리 생성
        }

        private static void BuildZebraModel(Transform parent, Material material) // 얼룩말의 줄무늬 느낌을 가진 모델을 만드는 메서드
        {
            BuildKnightModel(parent, material); // 말 계열 실루엣을 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.38f, -0.08f), new Vector3(0.18f, 0.03f, 0.24f), material, Quaternion.Euler(0f, 0f, 90f)); // 등줄기 무늬 1 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.5f, -0.02f), new Vector3(0.18f, 0.03f, 0.24f), material, Quaternion.Euler(0f, 0f, 90f)); // 등줄기 무늬 2 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.04f), new Vector3(0.16f, 0.03f, 0.2f), material, Quaternion.Euler(0f, 0f, 90f)); // 등줄기 무늬 3 생성
        }

        private static void BuildCentaurModel(Transform parent, Material material) // 말 몸체와 상체를 결합한 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.34f, 0.04f, 0.34f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.34f, 0.14f, 0.2f), material); // 말 몸체 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.42f, -0.02f), new Vector3(0.14f, 0.22f, 0.12f), material); // 인간 상체 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.66f, -0.02f), new Vector3(0.14f, 0.14f, 0.14f), material); // 머리 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.48f, 0.18f), new Vector3(0.06f, 0.36f, 0.06f), material, Quaternion.Euler(-25f, 0f, 0f)); // 창 생성
        }

        private static void BuildWaffleModel(Transform parent, Material material) // 격자 타일 느낌을 가진 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f), new Vector3(0.4f, 0.06f, 0.4f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.24f, 0f), new Vector3(0.32f, 0.12f, 0.32f), material); // 본체 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f), new Vector3(0.34f, 0.04f, 0.08f), material); // 가로 홈 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f), new Vector3(0.08f, 0.04f, 0.34f), material); // 세로 홈 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.48f, 0f), new Vector3(0.1f, 0.1f, 0.1f), material); // 상단 구슬 생성
        }

        private static void BuildNightriderModel(Transform parent, Material material) // 연속 나이트 도약의 길쭉한 기사 느낌을 주는 모델을 만드는 메서드
        {
            BuildKnightModel(parent, material); // 나이트 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0f), new Vector3(0.08f, 0.16f, 0.08f), material); // 긴 깃대 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.14f, 1.02f, 0f), new Vector3(0.22f, 0.12f, 0.04f), material); // 깃발 생성
        }

        private static void BuildCamelriderModel(Transform parent, Material material) // 카멜 위에 기수가 올라탄 모델을 만드는 메서드
        {
            BuildCamelModel(parent, material); // 카멜 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.62f, -0.02f), new Vector3(0.1f, 0.14f, 0.08f), material); // 기수 몸통 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.76f, -0.02f), new Vector3(0.08f, 0.08f, 0.08f), material); // 기수 머리 생성
        }

        private static void BuildGrasshopperModel(Transform parent, Material material) // 긴 다리와 도약 감각을 가진 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.28f, 0.04f, 0.28f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f), new Vector3(0.18f, 0.12f, 0.18f), material); // 몸체 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.12f, 0.18f, 0f), new Vector3(0.04f, 0.24f, 0.04f), material, Quaternion.Euler(0f, 0f, -28f)); // 오른쪽 다리 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(-0.12f, 0.18f, 0f), new Vector3(0.04f, 0.24f, 0.04f), material, Quaternion.Euler(0f, 0f, 28f)); // 왼쪽 다리 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.52f, 0.1f), new Vector3(0.04f, 0.16f, 0.04f), material, Quaternion.Euler(24f, 0f, 0f)); // 더듬이 1 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.52f, -0.1f), new Vector3(0.04f, 0.16f, 0.04f), material, Quaternion.Euler(-24f, 0f, 0f)); // 더듬이 2 생성
        }

        private static void BuildCannonModel(Transform parent, Material material) // 포신과 바퀴를 가진 원거리 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(0.34f, 0.08f, 0.26f), material); // 포대 받침 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.26f, 0f), new Vector3(0.12f, 0.28f, 0.12f), material, Quaternion.Euler(90f, 0f, 0f)); // 포신 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0.22f, 0.12f, 0f), new Vector3(0.12f, 0.04f, 0.12f), material, Quaternion.Euler(90f, 0f, 0f)); // 오른쪽 바퀴 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(-0.22f, 0.12f, 0f), new Vector3(0.12f, 0.04f, 0.12f), material, Quaternion.Euler(90f, 0f, 0f)); // 왼쪽 바퀴 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.26f, 0.16f), new Vector3(0.08f, 0.08f, 0.08f), material); // 포탄 장식 생성
        }

        private static void BuildCanvasserModel(Transform parent, Material material) // 룩형 구조에 깃발 장식을 더한 모델을 만드는 메서드
        {
            BuildRookModel(parent, material); // 룩 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.98f, 0f), new Vector3(0.04f, 0.24f, 0.04f), material); // 장대 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.13f, 1.02f, 0f), new Vector3(0.2f, 0.14f, 0.04f), material); // 깃발 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 1.18f, 0f), new Vector3(0.08f, 0.08f, 0.08f), material); // 꼭대기 구슬 생성
        }

        private static void BuildCaliphModel(Transform parent, Material material) // 돔과 초승달 장식을 더한 모델을 만드는 메서드
        {
            BuildBishopModel(parent, material); // 비숍 몸체를 기본으로 사용
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 1.12f, 0f), new Vector3(0.16f, 0.04f, 0.04f), material); // 초승달 가로 파츠 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0.06f, 1.08f, 0f), new Vector3(0.04f, 0.14f, 0.04f), material); // 초승달 세로 파츠 생성
        }

        private static void BuildSquirrelModel(Transform parent, Material material) // 꼬리가 말린 작은 짐승 느낌을 주는 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.28f, 0.04f, 0.28f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.24f, 0f), new Vector3(0.22f, 0.18f, 0.18f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.44f, 0.08f), new Vector3(0.12f, 0.12f, 0.12f), material); // 머리 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(-0.16f, 0.42f, -0.08f), new Vector3(0.08f, 0.22f, 0.08f), material, Quaternion.Euler(0f, 0f, -42f)); // 꼬리 바닥 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(-0.28f, 0.62f, -0.02f), new Vector3(0.16f, 0.16f, 0.16f), material); // 꼬리 끝 생성
        }

        private static void BuildChameleonModel(Transform parent, Material material) // 눈과 말린 꼬리를 가진 카멜레온 느낌의 모델을 만드는 메서드
        {
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.28f, 0.04f, 0.28f), material); // 받침 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.24f, 0f), new Vector3(0.28f, 0.12f, 0.18f), material); // 몸통 생성
            CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.38f, 0.14f), new Vector3(0.16f, 0.1f, 0.16f), material); // 머리 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(0.08f, 0.46f, 0.2f), new Vector3(0.07f, 0.07f, 0.07f), material); // 오른쪽 눈 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(-0.08f, 0.46f, 0.2f), new Vector3(0.07f, 0.07f, 0.07f), material); // 왼쪽 눈 생성
            CreatePart(parent, PrimitiveType.Cylinder, new Vector3(-0.2f, 0.3f, -0.1f), new Vector3(0.08f, 0.16f, 0.08f), material, Quaternion.Euler(0f, 0f, 40f)); // 말린 꼬리 바닥 생성
            CreatePart(parent, PrimitiveType.Sphere, new Vector3(-0.28f, 0.46f, -0.04f), new Vector3(0.11f, 0.11f, 0.11f), material); // 꼬리 끝 생성
        }

        private void AttachSelectionCollider() // 모델 전체를 덮는 단일 BoxCollider를 부착하는 메서드
        {
            var renderers = GetComponentsInChildren<Renderer>(); // 자식 렌더러를 모두 수집
            if (renderers == null || renderers.Length == 0) return; // 렌더러가 없으면 종료

            var bounds = renderers[0].bounds; // 첫 렌더러의 월드 Bounds를 기준으로 시작
            for (int i = 1; i < renderers.Length; i++) // 나머지 렌더러까지 순회하며
            {
                bounds.Encapsulate(renderers[i].bounds); // 전체 모델을 덮는 Bounds로 확장
            }

            var existingCollider = GetComponent<BoxCollider>(); // 기존 BoxCollider가 있는지 확인
            if (existingCollider != null) Destroy(existingCollider); // 중복 콜라이더가 있으면 제거

            var collider = gameObject.AddComponent<BoxCollider>(); // 클릭 판정용 단일 BoxCollider 추가
            collider.center = transform.InverseTransformPoint(bounds.center); // 월드 Bounds 중심을 로컬 기준 중심으로 변환
            collider.size = bounds.size + new Vector3(0.02f, 0.02f, 0.02f); // 살짝 크게 잡아 클릭이 쉬워지게 조정
        }

        private static Material CreatePieceMaterial(Color color) // 기물에 적용할 단색 머티리얼을 만드는 메서드
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit"); // URP Lit 셰이더 우선 탐색
            if (shader == null) shader = Shader.Find("Standard"); // 실패 시 Standard 셰이더 사용
            var material = new Material(shader); // 머티리얼 생성
            material.color = color; // 팀 색상 적용
            return material; // 완성 머티리얼 반환
        }

        private static GameObject CreatePart(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material) // 기본 회전으로 파츠 하나를 생성하는 보조 메서드
        {
            return CreatePart(parent, type, localPosition, localScale, material, Quaternion.identity); // 회전 없는 오버로드로 위임
        }

        private static GameObject CreatePart(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation) // 프리미티브 파츠 하나를 생성하는 보조 메서드
        {
            var part = GameObject.CreatePrimitive(type); // 지정한 타입의 기본 도형 생성
            part.name = type.ToString(); // 디버그 구분용 이름 지정
            part.transform.SetParent(parent, false); // 부모에 연결
            part.transform.localPosition = localPosition; // 로컬 위치 적용
            part.transform.localRotation = localRotation; // 로컬 회전 적용
            part.transform.localScale = localScale; // 로컬 크기 적용

            var renderer = part.GetComponent<Renderer>(); // 렌더러 확보
            if (renderer != null) renderer.sharedMaterial = material; // 팀 색상 머티리얼 적용

            var collider = part.GetComponent<Collider>(); // 기본으로 붙는 콜라이더 확보
            if (collider != null)
            {
                if (Application.isPlaying) Object.Destroy(collider); // 플레이 중이면 Destroy 사용
                else Object.DestroyImmediate(collider); // 에디터 상태면 DestroyImmediate 사용
            }

            return part; // 생성한 파츠 반환
        }
    }
}
