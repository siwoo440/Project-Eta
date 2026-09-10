using System.Collections.Generic; // Renderer·Material 목록 사용
using UnityEngine; // MonoBehaviour·PrimitiveType 사용
using ProjectEta.Run; // StageType 사용

namespace ProjectEta.Board
{
    public sealed class Day66RouteNodeBuildingModel : MonoBehaviour
    {
        private readonly List<Renderer> _partRenderers = new List<Renderer>(); // 발판 파트 렌더러 목록
        private readonly List<Color> _baseColors = new List<Color>(); // 발판 파트 기본 색상
        private readonly List<Material> _materials = new List<Material>(); // 런타임 머티리얼 목록
        private Renderer _signalRenderer; // 노드 상태 신호 렌더러
        private GameObject _visualRoot; // 발판 시각 루트
        private RouteNodeBuildingStyle _style; // 스테이지 타입별 발판 프로필
        private float _tileSize; // 보드 타일 크기
        private bool _initialized; // 초기화 완료 여부
        private Color _lastSignalColor = new Color(-1f, -1f, -1f, -1f); // 마지막 동기화 색상

        public GameObject VisualRoot => _visualRoot; // 기존 테스트·디버그에서 발판 시각 루트 조회
        public int PartCount => _partRenderers.Count; // 기존 테스트·디버그에서 생성된 발판 파트 수 조회

        public void Initialize(Renderer signalRenderer, StageType stageType, float tileSize)
        {
            if (_initialized) return; // 중복 초기화 차단

            _initialized = true; // 초기화 상태 기록
            _signalRenderer = signalRenderer; // 노드 상태 신호 저장
            _style = RouteNodeBuildingStyle.Resolve(stageType); // 스테이지 타입별 색상 프로필 조회
            _tileSize = Mathf.Max(0.1f, tileSize); // 타일 크기 보정
            BuildVisual(); // 바닥형 발판 시각 생성
            RefreshTint(true); // 최초 상태 색상 동기화
        }

        private void LateUpdate()
        {
            if (!_initialized || _visualRoot == null) return; // 시각 준비 전 처리 차단

            SyncVisualTransform(); // 외부 위치 변화 동기화
            RefreshTint(false); // Hover·선택 색상 반영
        }

        private void BuildVisual()
        {
            _visualRoot = new GameObject($"Day66RoutePlatform_{_style.Kind}"); // 발판 전용 루트 생성
            _visualRoot.transform.SetParent(transform, false); // 현재 노드 호스트 자식 연결
            SyncVisualTransform(); // 호스트 위치 기준 정렬

            BuildBasePlatform(); // 공통 바닥판 생성

            switch (_style.Kind)
            {
                case RouteNodeBuildingKind.EliteKeep:
                    BuildEliteSigil(); // 엘리트 발판 문양 생성
                    break;
                case RouteNodeBuildingKind.Treasury:
                    BuildTreasurySigil(); // 보상 발판 문양 생성
                    break;
                case RouteNodeBuildingKind.MerchantHouse:
                    BuildMerchantSigil(); // 상점 발판 문양 생성
                    break;
                case RouteNodeBuildingKind.ArcaneTower:
                    BuildArcaneSigil(); // 이벤트 발판 문양 생성
                    break;
                case RouteNodeBuildingKind.BossFortress:
                    BuildBossSigil(); // 중간 보스 발판 문양 생성
                    break;
                case RouteNodeBuildingKind.FinalCitadel:
                    BuildFinalBossSigil(); // 최종 보스 발판 문양 생성
                    break;
                default:
                    BuildBattleSigil(); // 일반 전투 발판 문양 생성
                    break;
            }
        }

        private void BuildBasePlatform()
        {
            float u = Unit; // 발판 기준 단위 계산
            Color rimColor = Color.Lerp(_style.MainColor, Color.black, 0.18f); // 테두리 색상 계산
            Color plateColor = Color.Lerp(_style.MainColor, _style.AccentColor, 0.12f); // 내부 판 색상 계산

            AddPart("BasePlate", PrimitiveType.Cylinder, new Vector3(0f, 0.030f, 0f) * u, new Vector3(0.78f, 0.030f, 0.78f) * u, Quaternion.identity, rimColor); // 외곽 원형 발판 생성
            AddPart("InnerPlate", PrimitiveType.Cylinder, new Vector3(0f, 0.050f, 0f) * u, new Vector3(0.60f, 0.015f, 0.60f) * u, Quaternion.identity, plateColor); // 내부 발판 생성
            AddPart("CenterPlate", PrimitiveType.Cylinder, new Vector3(0f, 0.062f, 0f) * u, new Vector3(0.30f, 0.010f, 0.30f) * u, Quaternion.identity, Color.Lerp(_style.AccentColor, Color.white, 0.08f)); // 중앙 중심판 생성
        }

        private void BuildBattleSigil()
        {
            float u = Unit; // 발판 기준 단위 계산
            AddPart("BattleLineA", PrimitiveType.Cube, new Vector3(0f, 0.075f, 0f) * u, new Vector3(0.72f, 0.018f, 0.10f) * u, Quaternion.Euler(0f, 45f, 0f), _style.AccentColor); // 전투 대각선 문양 생성
            AddPart("BattleLineB", PrimitiveType.Cube, new Vector3(0f, 0.075f, 0f) * u, new Vector3(0.72f, 0.018f, 0.10f) * u, Quaternion.Euler(0f, -45f, 0f), _style.AccentColor); // 전투 교차선 문양 생성
        }

        private void BuildEliteSigil()
        {
            float u = Unit; // 발판 기준 단위 계산
            AddPart("EliteDiamond", PrimitiveType.Cube, new Vector3(0f, 0.078f, 0f) * u, new Vector3(0.42f, 0.022f, 0.42f) * u, Quaternion.Euler(0f, 45f, 0f), _style.AccentColor); // 엘리트 다이아 문양 생성
            AddPart("EliteSlashA", PrimitiveType.Cube, new Vector3(0f, 0.095f, 0.18f) * u, new Vector3(0.34f, 0.015f, 0.08f) * u, Quaternion.Euler(0f, 0f, 0f), _style.RoofColor); // 상단 보조 문양 생성
            AddPart("EliteSlashB", PrimitiveType.Cube, new Vector3(0f, 0.095f, -0.18f) * u, new Vector3(0.34f, 0.015f, 0.08f) * u, Quaternion.Euler(0f, 0f, 0f), _style.RoofColor); // 하단 보조 문양 생성
        }

        private void BuildTreasurySigil()
        {
            float u = Unit; // 발판 기준 단위 계산
            AddPart("TreasureFrame", PrimitiveType.Cube, new Vector3(0f, 0.078f, 0f) * u, new Vector3(0.42f, 0.020f, 0.30f) * u, Quaternion.identity, _style.RoofColor); // 보물 상자 바닥 문양 생성
            AddPart("TreasureLid", PrimitiveType.Cylinder, new Vector3(0f, 0.096f, 0f) * u, new Vector3(0.24f, 0.012f, 0.24f) * u, Quaternion.Euler(90f, 0f, 0f), _style.AccentColor); // 금화 원반 문양 생성
            AddPart("TreasureGem", PrimitiveType.Sphere, new Vector3(0f, 0.110f, 0f) * u, new Vector3(0.11f, 0.05f, 0.11f) * u, Quaternion.identity, Color.Lerp(_style.AccentColor, Color.white, 0.18f)); // 보물 중심 보석 문양 생성
        }

        private void BuildMerchantSigil()
        {
            float u = Unit; // 발판 기준 단위 계산
            AddPart("MerchantBase", PrimitiveType.Cube, new Vector3(0f, 0.076f, 0f) * u, new Vector3(0.46f, 0.020f, 0.32f) * u, Quaternion.identity, _style.RoofColor); // 상점 바닥 간판 판 생성
            AddPart("MerchantAwningA", PrimitiveType.Cube, new Vector3(-0.12f, 0.094f, 0f) * u, new Vector3(0.18f, 0.016f, 0.34f) * u, Quaternion.identity, _style.AccentColor); // 좌측 차양 문양 생성
            AddPart("MerchantAwningB", PrimitiveType.Cube, new Vector3(0.12f, 0.094f, 0f) * u, new Vector3(0.18f, 0.016f, 0.34f) * u, Quaternion.identity, Color.Lerp(_style.AccentColor, Color.white, 0.18f)); // 우측 차양 문양 생성
            AddPart("MerchantCoin", PrimitiveType.Cylinder, new Vector3(0f, 0.110f, 0f) * u, new Vector3(0.13f, 0.010f, 0.13f) * u, Quaternion.identity, new Color(0.95f, 0.78f, 0.22f)); // 상점 동전 문양 생성
        }

        private void BuildArcaneSigil()
        {
            float u = Unit; // 발판 기준 단위 계산
            AddPart("ArcaneRingOuter", PrimitiveType.Cylinder, new Vector3(0f, 0.074f, 0f) * u, new Vector3(0.38f, 0.012f, 0.38f) * u, Quaternion.identity, _style.AccentColor); // 외곽 마법진 생성
            AddPart("ArcaneRingInner", PrimitiveType.Cylinder, new Vector3(0f, 0.088f, 0f) * u, new Vector3(0.22f, 0.010f, 0.22f) * u, Quaternion.identity, _style.RoofColor); // 내부 마법진 생성
            AddPart("ArcaneNorth", PrimitiveType.Cube, new Vector3(0f, 0.102f, 0.24f) * u, new Vector3(0.08f, 0.014f, 0.12f) * u, Quaternion.Euler(0f, 45f, 0f), _style.AccentColor); // 북쪽 룬 문양 생성
            AddPart("ArcaneSouth", PrimitiveType.Cube, new Vector3(0f, 0.102f, -0.24f) * u, new Vector3(0.08f, 0.014f, 0.12f) * u, Quaternion.Euler(0f, 45f, 0f), _style.AccentColor); // 남쪽 룬 문양 생성
            AddPart("ArcaneWest", PrimitiveType.Cube, new Vector3(-0.24f, 0.102f, 0f) * u, new Vector3(0.08f, 0.014f, 0.12f) * u, Quaternion.Euler(0f, 45f, 0f), _style.AccentColor); // 서쪽 룬 문양 생성
            AddPart("ArcaneEast", PrimitiveType.Cube, new Vector3(0.24f, 0.102f, 0f) * u, new Vector3(0.08f, 0.014f, 0.12f) * u, Quaternion.Euler(0f, 45f, 0f), _style.AccentColor); // 동쪽 룬 문양 생성
        }

        private void BuildBossSigil()
        {
            float u = Unit; // 발판 기준 단위 계산
            AddPart("BossPlate", PrimitiveType.Cube, new Vector3(0f, 0.078f, 0f) * u, new Vector3(0.56f, 0.022f, 0.56f) * u, Quaternion.Euler(0f, 45f, 0f), _style.RoofColor); // 중간 보스 중앙 문양 생성
            AddPart("BossHornLeft", PrimitiveType.Cube, new Vector3(-0.26f, 0.098f, 0.06f) * u, new Vector3(0.16f, 0.015f, 0.08f) * u, Quaternion.Euler(0f, 0f, 28f), _style.AccentColor); // 좌측 뿔 문양 생성
            AddPart("BossHornRight", PrimitiveType.Cube, new Vector3(0.26f, 0.098f, 0.06f) * u, new Vector3(0.16f, 0.015f, 0.08f) * u, Quaternion.Euler(0f, 0f, -28f), _style.AccentColor); // 우측 뿔 문양 생성
            AddPart("BossGate", PrimitiveType.Cube, new Vector3(0f, 0.098f, -0.20f) * u, new Vector3(0.18f, 0.015f, 0.12f) * u, Quaternion.identity, _style.AccentColor); // 전면 관문 문양 생성
        }

        private void BuildFinalBossSigil()
        {
            float u = Unit; // 발판 기준 단위 계산
            AddPart("FinalPlate", PrimitiveType.Cylinder, new Vector3(0f, 0.074f, 0f) * u, new Vector3(0.48f, 0.015f, 0.48f) * u, Quaternion.identity, _style.RoofColor); // 최종 보스 내부 원형판 생성
            AddPart("CrownCore", PrimitiveType.Cube, new Vector3(0f, 0.092f, 0f) * u, new Vector3(0.44f, 0.020f, 0.32f) * u, Quaternion.Euler(0f, 45f, 0f), _style.AccentColor); // 왕관 중심 문양 생성
            AddPart("CrownLeft", PrimitiveType.Cube, new Vector3(-0.18f, 0.112f, 0.18f) * u, new Vector3(0.08f, 0.016f, 0.18f) * u, Quaternion.Euler(0f, 45f, 0f), Color.Lerp(_style.AccentColor, Color.white, 0.10f)); // 좌측 왕관 갈래 문양 생성
            AddPart("CrownCenter", PrimitiveType.Cube, new Vector3(0f, 0.112f, 0.26f) * u, new Vector3(0.08f, 0.016f, 0.22f) * u, Quaternion.identity, Color.Lerp(_style.AccentColor, Color.white, 0.18f)); // 중앙 왕관 갈래 문양 생성
            AddPart("CrownRight", PrimitiveType.Cube, new Vector3(0.18f, 0.112f, 0.18f) * u, new Vector3(0.08f, 0.016f, 0.18f) * u, Quaternion.Euler(0f, -45f, 0f), Color.Lerp(_style.AccentColor, Color.white, 0.10f)); // 우측 왕관 갈래 문양 생성
        }

        private Renderer AddPart(string partName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType); // 프리미티브 파트 생성
            part.name = partName; // 계층창 파트 이름 지정
            part.transform.SetParent(_visualRoot.transform, false); // 발판 루트 자식 연결
            part.transform.localPosition = localPosition; // 파트 위치 적용
            part.transform.localScale = localScale; // 파트 크기 적용
            part.transform.localRotation = localRotation; // 파트 회전 적용

            Collider collider = part.GetComponent<Collider>(); // 기본 충돌체 조회
            if (collider != null) DestroyRuntimeObject(collider); // PlayMode·EditMode 모두 안전하게 지도 입력 간섭 제거

            Renderer renderer = part.GetComponent<Renderer>(); // 파트 렌더러 조회
            Material material = CreateMaterial(color); // 파트 머티리얼 생성
            renderer.sharedMaterial = material; // 파트 머티리얼 적용
            _partRenderers.Add(renderer); // 색상 동기화 대상 등록
            _baseColors.Add(color); // 기본 색상 저장
            return renderer; // 생성 렌더러 반환
        }

        private Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit"); // URP Lit 셰이더 조회
            if (shader == null) shader = Shader.Find("Standard"); // 기본 셰이더 대체
            Material material = new Material(shader); // 런타임 머티리얼 생성
            material.color = color; // 기본 색상 적용
            _materials.Add(material); // 정리 대상 등록
            return material; // 생성 머티리얼 반환
        }

        private void RefreshTint(bool force)
        {
            Color signalColor = ResolveSignalColor(); // 현재 노드 색상 조회
            if (!force && ColorsApproximatelyEqual(signalColor, _lastSignalColor)) return; // 동일 색상 중복 갱신 차단

            _lastSignalColor = signalColor; // 마지막 색상 저장
            float tintWeight = _style.BossEmphasis ? 0.34f : 0.24f; // 보스·일반 발판 틴트 비중 선택

            for (int i = 0; i < _partRenderers.Count && i < _baseColors.Count; i++)
            {
                Renderer renderer = _partRenderers[i]; // 현재 파트 렌더러 조회
                if (renderer == null) continue; // 제거된 파트 제외
                renderer.sharedMaterial.color = Color.Lerp(_baseColors[i], signalColor, tintWeight); // Hover·선택 색상 반영
            }
        }

        private Color ResolveSignalColor()
        {
            if (_signalRenderer == null) return _style.AccentColor; // 상태 렌더러 누락 시 강조 색상 사용
            if (_signalRenderer.sharedMaterial != null) return _signalRenderer.sharedMaterial.color; // 공유 머티리얼 색상 우선 사용
            return _style.AccentColor; // 머티리얼 누락 시 기본 강조 색상 사용
        }

        private void SyncVisualTransform()
        {
            if (_visualRoot == null) return; // 발판 루트 누락 방어
            _visualRoot.transform.localPosition = new Vector3(0f, 0.004f * _tileSize, 0f); // 바닥형 발판 위치 보정
            _visualRoot.transform.localRotation = Quaternion.identity; // 정방향 유지
            _visualRoot.transform.localScale = Vector3.one; // 부모 스케일 영향 차단
        }

        private static bool ColorsApproximatelyEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.005f && Mathf.Abs(a.g - b.g) < 0.005f && Mathf.Abs(a.b - b.b) < 0.005f && Mathf.Abs(a.a - b.a) < 0.005f; // 미세 색상 차이 무시
        }

        private float Unit => _tileSize * Mathf.Max(0.62f, _style.Scale); // 발판 단위 크기 계산

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null) return; // 빈 정리 대상 제외
            if (Application.isPlaying) Object.Destroy(target); // PlayMode 지연 제거 사용
            else Object.DestroyImmediate(target); // EditMode 즉시 제거로 테스트 로그 오류 방지
        }

        private void OnDestroy()
        {
            if (_visualRoot != null) DestroyRuntimeObject(_visualRoot); // 현재 실행 모드에 맞춰 발판 시각 루트 제거

            for (int i = 0; i < _materials.Count; i++)
            {
                if (_materials[i] != null) DestroyRuntimeObject(_materials[i]); // 현재 실행 모드에 맞춰 런타임 머티리얼 제거
            }

            _materials.Clear(); // 머티리얼 목록 초기화
            _partRenderers.Clear(); // 렌더러 목록 초기화
            _baseColors.Clear(); // 기본 색상 목록 초기화
        }
    }
}
