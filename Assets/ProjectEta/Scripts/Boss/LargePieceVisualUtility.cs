using UnityEngine; // Vector2Int, Vector3, Mathf, BoxCollider, PrimitiveType 등을 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView.BoardToLocalPosition을 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceView와 PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 37일차 이후 대형 보스 기물 기반을 모아두는 네임스페이스
{
    public static class LargePieceVisualUtility // 대형 기물 모델을 점유 영역 중앙에 놓고 클릭 콜라이더를 넓히는 시각 보정 유틸리티
    {
        private const float BossVisualScaleFactor = 0.75f; // 사용자 요청에 따라 현재 절반 버전에서 1.5배 키운 시각 배율
        private const float BossColliderScaleFactor = 0.75f; // 선택 콜라이더도 현재 절반 버전에서 1.5배 키운 배율

        public static Vector3 CalculateFootprintLocalPosition(Vector2Int anchor, Vector2Int size, float tileSize) // 사각 점유 영역의 실제 중앙 로컬 위치를 계산하는 순수 함수
        {
            int width = Mathf.Max(1, size.x); // 가로 점유 크기를 최소 1로 보정
            int height = Mathf.Max(1, size.y); // 세로 점유 크기를 최소 1로 보정
            Vector3 anchorPosition = BoardView.BoardToLocalPosition(anchor, tileSize); // 기준 칸 중심의 로컬 위치 계산
            float offsetX = (width - 1) * tileSize * 0.5f; // 여러 칸의 가운데가 되도록 오른쪽으로 이동할 거리
            float offsetZ = (height - 1) * tileSize * 0.5f; // 여러 칸의 가운데가 되도록 위쪽으로 이동할 거리
            return anchorPosition + new Vector3(offsetX, 0f, offsetZ); // 전체 점유 영역 중앙 위치 반환
        }

        public static void ApplyFootprint(PieceView pieceView, float tileSize) // 현재 PieceView를 RuntimeState.OccupancySize에 맞게 한 번에 보정하는 메서드
        {
            if (pieceView?.RuntimeState?.Definition == null) return; // 뷰나 런타임 정의가 없으면 처리하지 않음

            PieceRuntimeState state = pieceView.RuntimeState; // 현재 뷰의 런타임 상태 참조
            Vector2Int size = LargePieceBoardUtility.GetFootprint(state.Definition); // 정규화한 점유 크기 읽기
            pieceView.transform.localPosition = CalculateFootprintLocalPosition(state.BoardPosition, size, tileSize); // 기준 칸이 아니라 점유 영역 중앙으로 이동

            Transform model = pieceView.transform.Find("Model"); // PieceView.Initialize가 만든 모델 루트 탐색
            if (model != null) // 모델 루트가 있으면
            {
                float horizontalScaleX = (1f + Mathf.Max(0f, size.x - 1f) * 0.85f) * BossVisualScaleFactor; // 기존 수평형 폭에 0.75배 시각 배율 적용
                float verticalScaleY = 0.82f * BossVisualScaleFactor; // 낮은 높이도 같은 비율로 확대
                float horizontalScaleZ = (1f + Mathf.Max(0f, size.y - 1f) * 0.85f) * BossVisualScaleFactor; // 앞뒤 폭도 같은 비율로 확대
                model.localScale = new Vector3(horizontalScaleX, verticalScaleY, horizontalScaleZ); // 전체 실루엣을 0.75배 크기로 적용

                Material sharedMaterial = GetPrimaryMaterial(model); // 기존 팀 색상을 그대로 쓰기 위한 대표 머티리얼 확보
                EnsureHorizontalBossShell(model, size, tileSize, sharedMaterial); // 2x2에 어울리는 수평형 보스 외곽 모델 생성 또는 갱신
            }

            var collider = pieceView.GetComponent<BoxCollider>(); // PieceView가 만든 단일 선택 콜라이더 조회
            if (collider != null) // 클릭 콜라이더가 있으면
            {
                float footprintWidth = Mathf.Max(0.975f, size.x * tileSize * 1.35f * BossColliderScaleFactor); // 현재 절반 버전보다 1.5배 커진 X 클릭 범위
                float footprintDepth = Mathf.Max(0.975f, size.y * tileSize * 1.35f * BossColliderScaleFactor); // 현재 절반 버전보다 1.5배 커진 Z 클릭 범위
                float visualHeight = Mathf.Max(1.05f, 1.4f * BossColliderScaleFactor); // 낮고 넓은 보스에 맞춘 Y 범위 확대
                collider.center = new Vector3(0f, 0.36f, 0f); // 커진 차체 높이에 맞춰 중심도 상향
                collider.size = new Vector3(footprintWidth, visualHeight, footprintDepth); // 확대된 선택 범위 적용
            }
        }

        public static PieceView FindPieceView(PieceRuntimeState state) // 특정 런타임 기물을 표시하는 현재 씬 PieceView를 찾는 메서드
        {
            if (state == null) return null; // 찾을 상태가 없으면 null 반환

            var views = Object.FindObjectsByType<PieceView>(FindObjectsSortMode.None); // 현재 씬의 모든 PieceView 조회
            for (int i = 0; i < views.Length; i++) // 조회된 뷰 순회
            {
                if (views[i] != null && views[i].RuntimeState == state) return views[i]; // 같은 런타임 상태를 표시하면 반환
            }

            return null; // 연결된 뷰를 찾지 못하면 null 반환
        }

        private static void EnsureHorizontalBossShell(Transform model, Vector2Int size, float tileSize, Material material) // 수직으로 높은 탑형이 아니라 2x2 타일을 가로로 점유하는 차체형 보스 외곽을 만드는 메서드
        {
            if (model == null) return; // 모델 루트가 없으면 처리하지 않음

            Transform shell = model.Find("LargeBossShell"); // 이미 만들어 둔 대형 보스 외곽 루트 탐색
            if (shell != null) // 기존 외곽 루트가 있으면
            {
                if (Application.isPlaying) Object.Destroy(shell.gameObject); // 플레이 중이면 다음 프레임 파괴 예약
                else Object.DestroyImmediate(shell.gameObject); // 에디터 테스트에서는 즉시 제거
            }

            shell = new GameObject("LargeBossShell").transform; // 넓은 보스 실루엣 전용 루트 생성
            shell.SetParent(model, false); // 기존 모델의 자식으로 연결
            shell.localPosition = Vector3.zero; // 모델 기준 정중앙 배치
            shell.localRotation = Quaternion.identity; // 추가 회전 없이 배치
            shell.localScale = Vector3.one; // 크기 보정은 파츠 자체 크기로 처리

            float widthSpan = Mathf.Max(1.6f, size.x * tileSize * 0.95f) * BossVisualScaleFactor; // 기존 가로 총 폭에 0.75배 배율 적용
            float depthSpan = Mathf.Max(1.6f, size.y * tileSize * 0.95f) * BossVisualScaleFactor; // 기존 앞뒤 총 깊이에 0.75배 배율 적용
            float halfWidth = widthSpan * 0.5f; // 파츠 배치용 절반 폭
            float halfDepth = depthSpan * 0.5f; // 파츠 배치용 절반 깊이

            CreateShellPart(shell, "BaseDeck", PrimitiveType.Cube, new Vector3(0f, 0.135f, 0f), new Vector3(widthSpan, 0.135f, depthSpan), material); // 확대된 하부 갑판
            CreateShellPart(shell, "MainHull", PrimitiveType.Cube, new Vector3(0f, 0.315f, 0f), new Vector3(widthSpan * 0.86f, 0.195f, depthSpan * 0.86f), material); // 확대된 중심 차체
            CreateShellPart(shell, "FrontRam", PrimitiveType.Cube, new Vector3(0f, 0.285f, halfDepth * 0.72f), new Vector3(widthSpan * 0.46f, 0.135f, depthSpan * 0.22f), material); // 확대된 전면 충각
            CreateShellPart(shell, "RearBackplate", PrimitiveType.Cube, new Vector3(0f, 0.27f, -halfDepth * 0.72f), new Vector3(widthSpan * 0.56f, 0.12f, depthSpan * 0.22f), material); // 확대된 후면 장갑판
            CreateShellPart(shell, "LeftWing", PrimitiveType.Cube, new Vector3(-halfWidth * 0.78f, 0.255f, 0f), new Vector3(widthSpan * 0.18f, 0.105f, depthSpan * 0.82f), material); // 확대된 좌측 수평 익형 장갑
            CreateShellPart(shell, "RightWing", PrimitiveType.Cube, new Vector3(halfWidth * 0.78f, 0.255f, 0f), new Vector3(widthSpan * 0.18f, 0.105f, depthSpan * 0.82f), material); // 확대된 우측 수평 익형 장갑
            CreateShellPart(shell, "CenterBack", PrimitiveType.Cylinder, new Vector3(0f, 0.435f, -halfDepth * 0.15f), new Vector3(widthSpan * 0.18f, 0.075f, depthSpan * 0.24f), material); // 확대된 중앙 코어
            CreateShellPart(shell, "LeftFrontPod", PrimitiveType.Cylinder, new Vector3(-halfWidth * 0.52f, 0.36f, halfDepth * 0.52f), new Vector3(0.18f, 0.105f, 0.18f), material); // 확대된 좌전면 포드
            CreateShellPart(shell, "RightFrontPod", PrimitiveType.Cylinder, new Vector3(halfWidth * 0.52f, 0.36f, halfDepth * 0.52f), new Vector3(0.18f, 0.105f, 0.18f), material); // 확대된 우전면 포드
            CreateShellPart(shell, "LeftRearPod", PrimitiveType.Cylinder, new Vector3(-halfWidth * 0.52f, 0.36f, -halfDepth * 0.52f), new Vector3(0.18f, 0.105f, 0.18f), material); // 확대된 좌후면 포드
            CreateShellPart(shell, "RightRearPod", PrimitiveType.Cylinder, new Vector3(halfWidth * 0.52f, 0.36f, -halfDepth * 0.52f), new Vector3(0.18f, 0.105f, 0.18f), material); // 확대된 우후면 포드
            CreateShellPart(shell, "LeftSideBlade", PrimitiveType.Cube, new Vector3(-halfWidth * 0.96f, 0.15f, 0f), new Vector3(widthSpan * 0.08f, 0.06f, depthSpan * 0.74f), material); // 확대된 좌측 바깥 테두리 판금
            CreateShellPart(shell, "RightSideBlade", PrimitiveType.Cube, new Vector3(halfWidth * 0.96f, 0.15f, 0f), new Vector3(widthSpan * 0.08f, 0.06f, depthSpan * 0.74f), material); // 확대된 우측 바깥 테두리 판금
        }

        private static Material GetPrimaryMaterial(Transform model) // 기존 PieceView의 팀 색상 머티리얼을 읽어오는 메서드
        {
            if (model == null) return null; // 모델이 없으면 머티리얼도 없음

            var renderers = model.GetComponentsInChildren<Renderer>(); // 모델 아래 렌더러들을 모두 조회
            for (int i = 0; i < renderers.Length; i++) // 렌더러 순회
            {
                var renderer = renderers[i]; // 현재 렌더러 참조
                if (renderer == null) continue; // 비어 있으면 건너뜀
                if (renderer.sharedMaterial == null) continue; // 머티리얼이 없으면 건너뜀
                if (renderer.transform.name == "LargeBossShell") continue; // 외곽 루트 자체는 렌더러가 아니지만 안전하게 제외
                return renderer.sharedMaterial; // 첫 번째 유효 머티리얼을 대표 머티리얼로 사용
            }

            return null; // 찾지 못하면 null 반환
        }

        private static void CreateShellPart(Transform parent, string partName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material material) // 외곽 차체용 프리미티브 파츠를 만드는 공통 메서드
        {
            var part = GameObject.CreatePrimitive(primitiveType); // 지정한 기본 프리미티브 생성
            part.name = partName; // 디버그하기 쉬운 파츠 이름 지정
            part.transform.SetParent(parent, false); // 외곽 루트 자식으로 연결
            part.transform.localPosition = localPosition; // 로컬 위치 배치
            part.transform.localRotation = Quaternion.identity; // 기본 회전 유지
            part.transform.localScale = localScale; // 파츠별 크기 적용

            var renderer = part.GetComponent<Renderer>(); // 렌더러 조회
            if (renderer != null && material != null) renderer.sharedMaterial = material; // 기존 보스 팀 색상 머티리얼 적용

            var collider = part.GetComponent<Collider>(); // 기본 프리미티브 콜라이더 조회
            if (collider != null) // 충돌체가 있으면
            {
                if (Application.isPlaying) Object.Destroy(collider); // 플레이 중에는 예약 파괴
                else Object.DestroyImmediate(collider); // 에디터 테스트에서는 즉시 파괴
            }
        }
    }
}
