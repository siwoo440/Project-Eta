using UnityEngine; // MonoBehaviour·LineRenderer·Material 사용

namespace ProjectEta.Board // 경로 지도 시각화 네임스페이스
{ // 네임스페이스 시작
    public sealed class RouteNodeSelectionHighlight : MonoBehaviour // 다음 이동 가능 노드 금빛 고리
    { // 클래스 시작
        private const string HighlightName = "RouteNodeSelectableHighlight"; // 고리 오브젝트 이름
        private const int SegmentCount = 64; // 매끄러운 원형 분할 수
        private const float RingRadiusRatio = 0.62f; // 원판 지름 기준 고리 반지름 비율
        private const float RingHeight = 0.045f; // 원판 위 고리 높이
        private const float RingWidth = 0.045f; // 금빛 선 굵기
        private const float PulseDuration = 1.2f; // 한 번 반짝이는 시간
        private const float MinimumPulseScale = 1f; // 최소 고리 크기
        private const float MaximumPulseScale = 1.10f; // 최대 고리 크기
        private static readonly Color DimGold = new Color(1f, 0.58f, 0.08f, 0.52f); // 어두운 금빛 색상
        private static readonly Color BrightGold = new Color(1f, 0.90f, 0.32f, 0.98f); // 밝은 금빛 색상

        private Transform _highlightTransform; // 금빛 고리 변환 참조
        private LineRenderer _ringRenderer; // 원형 선 렌더러
        private Material _ringMaterial; // 고리 전용 머티리얼
        private Vector3 _scaleCompensation = Vector3.one; // 원판 비균일 스케일 보정값
        private bool _initialized; // 중복 초기화 차단 상태

        public void Initialize() // 금빛 고리 생성과 반짝임 시작
        { // 초기화 시작
            if (_initialized) // 기존 초기화 확인
            { // 재활성화 시작
                SetVisible(true); // 기존 고리 다시 표시
                return; // 중복 생성 차단
            } // 재활성화 종료

            Vector3 ownerScale = transform.localScale; // 원판의 현재 비균일 스케일 조회
            float safeScaleX = Mathf.Max(0.001f, Mathf.Abs(ownerScale.x)); // 가로 스케일 안전값 계산
            float safeScaleY = Mathf.Max(0.001f, Mathf.Abs(ownerScale.y)); // 높이 스케일 안전값 계산
            float safeScaleZ = Mathf.Max(0.001f, Mathf.Abs(ownerScale.z)); // 세로 스케일 안전값 계산
            float markerDiameter = Mathf.Max(safeScaleX, safeScaleZ); // 원판 표시 지름 계산
            float ringRadius = markerDiameter * RingRadiusRatio; // 원판 바깥 고리 반지름 계산
            _scaleCompensation = new Vector3(1f / safeScaleX, 1f / safeScaleY, 1f / safeScaleZ); // 부모 비균일 스케일 상쇄값 저장

            GameObject highlightObject = new GameObject(HighlightName); // 고리 전용 자식 오브젝트 생성
            highlightObject.transform.SetParent(transform, false); // 선택 노드 수명에 고리 연결
            highlightObject.transform.localPosition = new Vector3(0f, RingHeight / safeScaleY, 0f); // 원판과 아이콘 위에 고리 배치
            highlightObject.transform.localRotation = Quaternion.identity; // 보드 수평 방향 유지
            highlightObject.transform.localScale = _scaleCompensation; // 원판 비균일 스케일 상쇄
            _highlightTransform = highlightObject.transform; // 애니메이션용 변환 참조 저장

            _ringRenderer = highlightObject.AddComponent<LineRenderer>(); // 원형 선 렌더러 추가
            _ringRenderer.useWorldSpace = false; // 노드 로컬 좌표계 사용
            _ringRenderer.loop = true; // 닫힌 원형 선 적용
            _ringRenderer.positionCount = SegmentCount; // 원형 분할 수 적용
            _ringRenderer.widthMultiplier = RingWidth; // 금빛 선 굵기 적용
            _ringRenderer.numCornerVertices = 4; // 곡선 모서리 부드럽게 처리
            _ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // 불필요한 고리 그림자 제거
            _ringRenderer.receiveShadows = false; // 주변 그림자 영향 제거
            _ringRenderer.textureMode = LineTextureMode.Stretch; // 단색 고리 텍스처 방식 적용
            ApplyCirclePositions(ringRadius); // 원형 꼭짓점 좌표 적용

            _ringMaterial = CreateGlowMaterial(); // 금빛 발광 머티리얼 생성
            _ringRenderer.sharedMaterial = _ringMaterial; // 고리 렌더러에 전용 머티리얼 연결
            ApplyPulse(0f); // 초기 반짝임 상태 적용
            _initialized = true; // 초기화 완료 기록
        } // 초기화 종료

        public void SetVisible(bool visible) // 선택 가능 상태에 따른 고리 표시 변경
        { // 표시 변경 시작
            if (_highlightTransform != null) _highlightTransform.gameObject.SetActive(visible); // 고리 오브젝트 활성 상태 적용
        } // 표시 변경 종료

        public static float EvaluatePulseScale(float elapsedTime) // 시간에 따른 고리 크기 계산
        { // 크기 계산 시작
            float safeTime = Mathf.Max(0f, elapsedTime); // 음수 시간 보정
            float radians = safeTime * Mathf.PI * 2f / PulseDuration; // 반복 주기 라디안 계산
            float pulse = (Mathf.Sin(radians) + 1f) * 0.5f; // 0~1 왕복 값 계산
            return Mathf.Lerp(MinimumPulseScale, MaximumPulseScale, pulse); // 현재 고리 크기 반환
        } // 크기 계산 종료

        private void Update() // 프레임별 반짝임 갱신
        { // 갱신 시작
            if (!_initialized || _highlightTransform == null || !_highlightTransform.gameObject.activeSelf) return; // 비활성·미초기화 상태 제외
            ApplyPulse(Time.unscaledTime); // 시간 배율과 무관한 반짝임 적용
        } // 갱신 종료

        private void ApplyCirclePositions(float radius) // 원형 선 꼭짓점 구성
        { // 원형 구성 시작
            for (int index = 0; index < SegmentCount; index++) // 전체 원형 분할 순회
            { // 분할 순회 시작
                float ratio = index / (float)SegmentCount; // 현재 원 둘레 비율 계산
                float angle = ratio * Mathf.PI * 2f; // 현재 각도 계산
                _ringRenderer.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius)); // 수평 원형 좌표 적용
            } // 분할 순회 종료
        } // 원형 구성 종료

        private void ApplyPulse(float elapsedTime) // 현재 시간의 밝기와 크기 적용
        { // 반짝임 적용 시작
            if (_highlightTransform == null || _ringRenderer == null) return; // 필수 고리 참조 누락 방어
            float scale = EvaluatePulseScale(elapsedTime); // 현재 고리 크기 계산
            float pulse = Mathf.InverseLerp(MinimumPulseScale, MaximumPulseScale, scale); // 현재 밝기 비율 계산
            _highlightTransform.localScale = new Vector3(_scaleCompensation.x * scale, _scaleCompensation.y, _scaleCompensation.z * scale); // 크기 변화와 비균일 보정 적용
            Color displayColor = Color.Lerp(DimGold, BrightGold, pulse); // 현재 금빛 색상 계산
            _ringRenderer.startColor = displayColor; // 고리 시작 색상 적용
            _ringRenderer.endColor = displayColor; // 고리 끝 색상 적용
            ApplyMaterialColor(displayColor, pulse); // 머티리얼 발광 색상 적용
        } // 반짝임 적용 종료

        private static Material CreateGlowMaterial() // 금빛 발광 머티리얼 생성
        { // 머티리얼 생성 시작
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit"); // URP 비조명 셰이더 우선 조회
            if (shader == null) shader = Shader.Find("Sprites/Default"); // URP 누락 시 투명 스프라이트 셰이더 사용
            if (shader == null) shader = Shader.Find("Standard"); // 최종 기본 셰이더 대체
            var material = new Material(shader); // 런타임 고리 머티리얼 생성
            material.name = "RouteNodeSelectableGoldRing"; // 머티리얼 식별 이름 적용
            ConfigureTransparency(material); // 알파 반짝임용 투명 설정
            return material; // 생성 머티리얼 반환
        } // 머티리얼 생성 종료

        private static void ConfigureTransparency(Material material) // 셰이더 투명 렌더링 설정
        { // 투명 설정 시작
            if (material == null) return; // 빈 머티리얼 처리
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f); // URP 투명 표면 적용
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); // 원본 알파 혼합 적용
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One); // 금빛 가산 혼합 적용
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f); // 투명 고리 깊이 기록 해제
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); // URP 투명 키워드 활성화
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; // 투명 렌더 순서 적용
        } // 투명 설정 종료

        private void ApplyMaterialColor(Color displayColor, float pulse) // 머티리얼 색상과 밝기 적용
        { // 머티리얼 색상 적용 시작
            if (_ringMaterial == null) return; // 빈 머티리얼 처리
            Color hdrColor = displayColor * Mathf.Lerp(1.4f, 2.8f, pulse); // 밝기 변화용 HDR 금빛 계산
            hdrColor.a = displayColor.a; // 투명도 원본 유지
            if (_ringMaterial.HasProperty("_BaseColor")) _ringMaterial.SetColor("_BaseColor", hdrColor); // URP 기본 금빛 적용
            if (_ringMaterial.HasProperty("_Color")) _ringMaterial.SetColor("_Color", hdrColor); // 기본 셰이더 금빛 적용
            if (_ringMaterial.HasProperty("_EmissionColor")) _ringMaterial.SetColor("_EmissionColor", hdrColor); // 발광 지원 셰이더 색상 적용
        } // 머티리얼 색상 적용 종료

        private void OnDestroy() // 런타임 머티리얼 정리
        { // 제거 시작
            if (_ringMaterial == null) return; // 이미 정리된 머티리얼 제외
            if (Application.isPlaying) Destroy(_ringMaterial); // 플레이 모드 지연 제거
            else DestroyImmediate(_ringMaterial); // EditMode 즉시 제거
            _ringMaterial = null; // 머티리얼 참조 초기화
        } // 제거 종료
    } // 클래스 종료
} // 네임스페이스 종료
