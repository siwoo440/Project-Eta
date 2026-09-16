using UnityEngine; // 런타임 아이콘 오브젝트 사용
using UnityEngine.Rendering; // 투명 혼합 모드 사용
using ProjectEta.Run; // StageType 사용

namespace ProjectEta.Board // 경로 지도 표시 네임스페이스
{ // 네임스페이스 시작
    [DisallowMultipleComponent] // 노드 아이콘 중복 방지
    public sealed class RouteNodeIconPresenter : MonoBehaviour // 기존 입체 모델을 대체하는 원판 아이콘 표시기
    { // 클래스 시작
        private const string ResourceRoot = "UI/RouteMap/"; // 아이콘 Resources 기준 경로
        private const float IconHeight = 0.034f; // 원판 위 아이콘 높이
        private const float RegularIconSize = 0.798f; // 일반 노드 현재 크기 대비 70% 아이콘 크기
        private const float BossIconSize = 0.966f; // 보스 노드 현재 크기 대비 70% 아이콘 크기

        private GameObject _visualRoot; // 아이콘 시각 루트
        private Material _iconMaterial; // 투명 아이콘 머티리얼
        private int _partCount; // 생성된 아이콘 표면 수

        public GameObject VisualRoot => _visualRoot; // 테스트·진단용 표시 루트
        public int PartCount => _partCount; // 테스트·진단용 표면 수

        public void Initialize(Renderer signalRenderer, StageType stageType, float tileSize) // 스테이지별 아이콘 생성
        { // 초기화 시작
            ClearVisual(); // 기존 표시 제거
            BuildIcon(stageType, Mathf.Max(0.01f, tileSize)); // 새 아이콘 표시 생성
        } // 초기화 종료

        private void BuildIcon(StageType stageType, float tileSize) // 단일 투명 아이콘 표면 구성
        { // 구성 시작
            _visualRoot = new GameObject("RouteNodeIconVisual"); // 아이콘 루트 생성
            _visualRoot.transform.SetParent(transform, false); // 노드 호스트 자식 연결
            _visualRoot.transform.localPosition = Vector3.zero; // 호스트 중심 정렬
            _visualRoot.transform.localRotation = Quaternion.identity; // 호스트 회전 유지
            _visualRoot.transform.localScale = Vector3.one; // 호스트 크기 유지

            GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Quad); // 단일 아이콘 표면 생성
            icon.name = "RouteNodeIconSurface"; // 아이콘 계층창 이름
            icon.transform.SetParent(_visualRoot.transform, false); // 아이콘 루트 자식 연결
            icon.transform.localPosition = new Vector3(0f, IconHeight, 0f); // 원판 위 배치
            icon.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // 보드 평면 방향 회전
            float iconSize = ResolveIconSize(stageType) * tileSize; // 타입별 아이콘 크기 계산
            icon.transform.localScale = new Vector3(iconSize, iconSize, 1f); // 정사각 아이콘 크기 적용

            Collider collider = icon.GetComponent<Collider>(); // 기본 Quad 콜라이더 조회
            DestroyUnityObject(collider); // 노드 클릭 방해 콜라이더 제거

            Texture2D texture = Resources.Load<Texture2D>(ResolveResourcePath(stageType)); // 스테이지 아이콘 텍스처 로드
            _iconMaterial = CreateIconMaterial(texture); // 투명 아이콘 머티리얼 생성
            icon.GetComponent<Renderer>().sharedMaterial = _iconMaterial; // 아이콘 표면에 머티리얼 연결
            _partCount = 1; // 단일 표면 수 기록
        } // 구성 종료

        public static string ResolveResourcePath(StageType stageType) // 스테이지별 Resources 경로 계산
        { // 경로 계산 시작
            switch (stageType) // 스테이지 타입 분기
            { // 분기 시작
                case StageType.Elite: // 엘리트 분기
                    return ResourceRoot + "RouteEliteIcon"; // 엘리트 아이콘 경로
                case StageType.Reward: // 보상 분기
                    return ResourceRoot + "RouteRewardIcon"; // 보상 아이콘 경로
                case StageType.Shop: // 상점 분기
                    return ResourceRoot + "RouteShopIcon"; // 상점 아이콘 경로
                case StageType.Event: // 이벤트 분기
                    return ResourceRoot + "RouteEventIcon"; // 이벤트 아이콘 경로
                case StageType.MidBoss: // 중간 보스 분기
                case StageType.FinalBoss: // 최종 보스 분기
                    return ResourceRoot + "RouteBossIcon"; // 공용 보스 아이콘 경로
                default: // 일반 전투·예외 분기
                    return ResourceRoot + "RouteBattleIcon"; // 일반 전투 아이콘 경로
            } // 분기 종료
        } // 경로 계산 종료

        private static float ResolveIconSize(StageType stageType) // 타입별 아이콘 크기 계산
        { // 크기 계산 시작
            bool boss = stageType == StageType.MidBoss || stageType == StageType.FinalBoss; // 보스 타입 판정
            return boss ? BossIconSize : RegularIconSize; // 보스 확대 크기 반환
        } // 크기 계산 종료

        private static Material CreateIconMaterial(Texture2D texture) // 투명 텍스처 머티리얼 생성
        { // 머티리얼 생성 시작
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit"); // URP Unlit 셰이더 조회
            if (shader == null) shader = Shader.Find("Sprites/Default"); // Sprite 셰이더 대체
            Material material = new Material(shader); // 런타임 머티리얼 생성
            material.name = "RouteNodeIconMaterial"; // 진단용 머티리얼 이름
            material.mainTexture = texture; // 아이콘 텍스처 연결
            material.color = Color.white; // 원본 색상 유지
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture); // URP 기본 텍스처 연결
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white); // URP 기본 색상 적용
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f); // 투명 Surface 적용
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); // 소스 알파 혼합 적용
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); // 대상 알파 혼합 적용
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f); // 투명 깊이 쓰기 해제
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off); // 양면 아이콘 표시
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); // URP 투명 키워드 활성화
            material.renderQueue = (int)RenderQueue.Transparent; // 투명 렌더 순서 적용
            return material; // 완성 머티리얼 반환
        } // 머티리얼 생성 종료

        private void ClearVisual() // 기존 아이콘 자원 정리
        { // 정리 시작
            if (_visualRoot != null) DestroyUnityObject(_visualRoot); // 기존 아이콘 루트 제거
            if (_iconMaterial != null) DestroyUnityObject(_iconMaterial); // 기존 머티리얼 제거
            _visualRoot = null; // 표시 루트 참조 초기화
            _iconMaterial = null; // 머티리얼 참조 초기화
            _partCount = 0; // 표면 수 초기화
        } // 정리 종료

        private static void DestroyUnityObject(Object target) // 실행 모드별 안전한 제거
        { // 제거 시작
            if (target == null) return; // 빈 대상 제외
            if (Application.isPlaying) Destroy(target); // PlayMode 지연 제거
            else DestroyImmediate(target); // EditMode 즉시 제거
        } // 제거 종료

        private void OnDestroy() // 컴포넌트 해제 처리
        { // 해제 시작
            ClearVisual(); // 런타임 생성 자원 정리
        } // 해제 종료
    } // 클래스 종료
} // 네임스페이스 종료
