using System.Collections.Generic; // 건물 파츠 렌더러·머티리얼 목록 사용
using UnityEngine; // MonoBehaviour·PrimitiveType·Color 사용
using ProjectEta.Run; // StageType 사용

namespace ProjectEta.Board
{
    public sealed class Day66RouteNodeBuildingModel : MonoBehaviour
    {
        private const float BaseYOffset = 0.06f; // 노드 기단 위 건물 시작 높이
        private const float SignalTintWeight = 0.22f; // Hover·선택 색상 혼합 비율

        private readonly List<Renderer> _partRenderers = new List<Renderer>(); // 건물 전체 파츠 렌더러 목록
        private readonly List<Color> _baseColors = new List<Color>(); // 파츠 원본 색상 목록
        private readonly List<Material> _materials = new List<Material>(); // 런타임 생성 머티리얼 목록
        private Renderer _signalRenderer; // 노드 Hover·선택 상태 신호 렌더러
        private GameObject _visualRoot; // Host에 직접 연결되는 건물 루트
        private RouteNodeBuildingStyle _style; // 현재 StageType 건물 스타일
        private float _tileSize; // 현재 보드 타일 크기
        private bool _initialized; // 중복 초기화 차단 상태
        private Color _lastSignalColor = new Color(-1f, -1f, -1f, -1f); // 마지막 신호 색상

        public GameObject VisualRoot => _visualRoot; // 테스트·전환 연출용 건물 루트 공개
        public int PartCount => _partRenderers.Count; // 실제 생성된 모델 파츠 수 공개

        public void Initialize(Renderer signalRenderer, StageType stageType, float tileSize)
        {
            if (_initialized) // 기존 초기화 여부 확인
            {
                return; // 중복 건물 생성 차단
            }

            _initialized = true; // 초기화 완료 기록
            _signalRenderer = signalRenderer; // 노드 상태 렌더러 저장
            _style = RouteNodeBuildingStyle.Resolve(stageType); // StageType별 건물 스타일 선택
            _tileSize = Mathf.Max(0.1f, tileSize); // 안전한 타일 크기 적용
            BuildVisual(); // 건물 파츠 즉시 생성
            RefreshTint(true); // 최초 색상 상태 적용
        }

        private void LateUpdate()
        {
            if (!_initialized || _visualRoot == null) // 건물 준비 여부 확인
            {
                return; // 미생성 상태 처리 차단
            }

            RefreshTint(false); // Hover·선택 신호만 후처리
        }

        private void BuildVisual()
        {
            _visualRoot = new GameObject($"Day66Building_{_style.Kind}"); // 건물 전용 루트 생성
            _visualRoot.transform.SetParent(transform, false); // 건물 Host 자식으로 직접 연결
            _visualRoot.transform.localPosition = new Vector3(0f, BaseYOffset * _tileSize, 0f); // 기단 바로 위에 건물 배치
            _visualRoot.transform.localRotation = Quaternion.identity; // 보드 정방향 유지
            _visualRoot.transform.localScale = Vector3.one; // Host 비균일 스케일 영향 제거

            switch (_style.Kind) // StageType별 상세 건물 선택
            {
                case RouteNodeBuildingKind.EliteKeep:
                    BuildEliteKeep(); // 엘리트 요새 생성
                    break;
                case RouteNodeBuildingKind.Treasury:
                    BuildTreasury(); // 보상 보물고 생성
                    break;
                case RouteNodeBuildingKind.MerchantHouse:
                    BuildMerchantHouse(); // 상점 건물 생성
                    break;
                case RouteNodeBuildingKind.ArcaneTower:
                    BuildArcaneTower(); // 이벤트 마법탑 생성
                    break;
                case RouteNodeBuildingKind.BossFortress:
                    BuildBossFortress(); // 중간 보스 요새 생성
                    break;
                case RouteNodeBuildingKind.FinalCitadel:
                    BuildFinalCitadel(); // 최종 보스 성채 생성
                    break;
                default:
                    BuildKeep(); // 일반 전투 성채 생성
                    break;
            }
        }

        private void BuildKeep()
        {
            float u = Unit; // 현재 성채 모델 단위 계산
            AddFoundation(u, _style.MainColor); // 넓은 석재 기단 생성
            AddPart("KeepBody", PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f) * u, new Vector3(0.56f, 0.58f, 0.52f) * u, Quaternion.identity, _style.MainColor); // 중앙 성채 본체 생성
            AddCornerTowers(u, 0.30f, 0.56f, 0.15f, _style.MainColor); // 네 모서리 망루 생성
            AddBattlements(u, 0.66f, _style.AccentColor); // 성채 상단 흉벽 생성
            AddPart("Gate", PrimitiveType.Cube, new Vector3(0f, 0.24f, -0.285f) * u, new Vector3(0.18f, 0.28f, 0.035f) * u, Quaternion.identity, _style.RoofColor); // 전면 성문 생성
            AddPart("Banner", PrimitiveType.Cube, new Vector3(0f, 0.58f, -0.31f) * u, new Vector3(0.12f, 0.18f, 0.018f) * u, Quaternion.identity, _style.AccentColor); // 전투 깃발 생성
        }

        private void BuildEliteKeep()
        {
            float u = Unit; // 엘리트 요새 모델 단위 계산
            AddFoundation(u, _style.MainColor); // 강화 요새 기단 생성
            AddPart("EliteBody", PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f) * u, new Vector3(0.62f, 0.66f, 0.58f) * u, Quaternion.identity, _style.MainColor); // 강화 중앙 성채 생성
            AddCornerTowers(u, 0.33f, 0.66f, 0.17f, _style.MainColor); // 강화 망루 생성
            AddBattlements(u, 0.76f, _style.AccentColor); // 엘리트 흉벽 생성
            AddPart("EliteGate", PrimitiveType.Cube, new Vector3(0f, 0.26f, -0.315f) * u, new Vector3(0.20f, 0.32f, 0.04f) * u, Quaternion.identity, _style.RoofColor); // 강화 성문 생성
            AddPart("EliteCrest", PrimitiveType.Sphere, new Vector3(0f, 0.78f, -0.04f) * u, new Vector3(0.14f, 0.14f, 0.14f) * u, Quaternion.identity, _style.AccentColor); // 엘리트 문장 생성
            AddPart("EliteSpireLeft", PrimitiveType.Cube, new Vector3(-0.20f, 0.88f, 0f) * u, new Vector3(0.055f, 0.30f, 0.055f) * u, Quaternion.Euler(0f, 0f, -18f), _style.AccentColor); // 좌측 첨탑 생성
            AddPart("EliteSpireRight", PrimitiveType.Cube, new Vector3(0.20f, 0.88f, 0f) * u, new Vector3(0.055f, 0.30f, 0.055f) * u, Quaternion.Euler(0f, 0f, 18f), _style.AccentColor); // 우측 첨탑 생성
        }

        private void BuildTreasury()
        {
            float u = Unit; // 보물고 모델 단위 계산
            AddFoundation(u, _style.MainColor); // 보물고 석재 기단 생성
            AddPart("TreasuryBody", PrimitiveType.Cube, new Vector3(0f, 0.30f, 0f) * u, new Vector3(0.58f, 0.48f, 0.50f) * u, Quaternion.identity, _style.MainColor); // 보물고 본체 생성
            AddPart("TreasuryRoof", PrimitiveType.Cylinder, new Vector3(0f, 0.62f, 0f) * u, new Vector3(0.40f, 0.10f, 0.40f) * u, Quaternion.identity, _style.RoofColor); // 금고 지붕 생성
            AddPart("GoldDoor", PrimitiveType.Cube, new Vector3(0f, 0.26f, -0.265f) * u, new Vector3(0.20f, 0.30f, 0.035f) * u, Quaternion.identity, _style.AccentColor); // 금빛 출입문 생성
            AddPillar(-0.24f, -0.18f, u, _style.AccentColor); // 좌전 기둥 생성
            AddPillar(0.24f, -0.18f, u, _style.AccentColor); // 우전 기둥 생성
            AddPillar(-0.24f, 0.18f, u, _style.AccentColor); // 좌후 기둥 생성
            AddPillar(0.24f, 0.18f, u, _style.AccentColor); // 우후 기둥 생성
            AddPart("TreasureOrb", PrimitiveType.Sphere, new Vector3(0f, 0.84f, 0f) * u, new Vector3(0.16f, 0.16f, 0.16f) * u, Quaternion.identity, _style.AccentColor); // 보상 보주 생성
        }

        private void BuildMerchantHouse()
        {
            float u = Unit; // 상점 모델 단위 계산
            AddFoundation(u, _style.MainColor); // 상점 기단 생성
            AddPart("ShopBody", PrimitiveType.Cube, new Vector3(0f, 0.31f, 0f) * u, new Vector3(0.62f, 0.50f, 0.54f) * u, Quaternion.identity, _style.MainColor); // 상점 본체 생성
            AddPart("RoofLeft", PrimitiveType.Cube, new Vector3(-0.17f, 0.62f, 0f) * u, new Vector3(0.39f, 0.10f, 0.62f) * u, Quaternion.Euler(0f, 0f, 27f), _style.RoofColor); // 좌측 경사 지붕 생성
            AddPart("RoofRight", PrimitiveType.Cube, new Vector3(0.17f, 0.62f, 0f) * u, new Vector3(0.39f, 0.10f, 0.62f) * u, Quaternion.Euler(0f, 0f, -27f), _style.RoofColor); // 우측 경사 지붕 생성
            AddPart("ShopDoor", PrimitiveType.Cube, new Vector3(-0.14f, 0.25f, -0.285f) * u, new Vector3(0.15f, 0.30f, 0.035f) * u, Quaternion.identity, new Color(0.12f, 0.08f, 0.05f)); // 상점 문 생성
            AddPart("Awning", PrimitiveType.Cube, new Vector3(0.12f, 0.38f, -0.34f) * u, new Vector3(0.36f, 0.07f, 0.15f) * u, Quaternion.Euler(12f, 0f, 0f), _style.AccentColor); // 상점 차양 생성
            AddPart("SignPost", PrimitiveType.Cube, new Vector3(0.34f, 0.54f, -0.22f) * u, new Vector3(0.045f, 0.38f, 0.045f) * u, Quaternion.identity, _style.RoofColor); // 상점 간판 기둥 생성
            AddPart("ShopSign", PrimitiveType.Cube, new Vector3(0.34f, 0.76f, -0.22f) * u, new Vector3(0.18f, 0.15f, 0.035f) * u, Quaternion.Euler(0f, 0f, 7f), _style.AccentColor); // 상점 간판 생성
            AddPart("Coin", PrimitiveType.Sphere, new Vector3(0.34f, 0.76f, -0.26f) * u, new Vector3(0.085f, 0.085f, 0.035f) * u, Quaternion.identity, new Color(0.96f, 0.76f, 0.20f)); // 동전 문양 생성
        }

        private void BuildArcaneTower()
        {
            float u = Unit; // 이벤트 마법탑 모델 단위 계산
            AddFoundation(u, _style.MainColor); // 마법탑 기단 생성
            AddPart("TowerBody", PrimitiveType.Cylinder, new Vector3(0f, 0.43f, 0f) * u, new Vector3(0.34f, 0.56f, 0.34f) * u, Quaternion.identity, _style.MainColor); // 중앙 마법탑 생성
            AddPart("TowerRingLow", PrimitiveType.Cylinder, new Vector3(0f, 0.24f, 0f) * u, new Vector3(0.46f, 0.055f, 0.46f) * u, Quaternion.identity, _style.AccentColor); // 하단 마력 링 생성
            AddPart("TowerRingHigh", PrimitiveType.Cylinder, new Vector3(0f, 0.68f, 0f) * u, new Vector3(0.42f, 0.055f, 0.42f) * u, Quaternion.identity, _style.AccentColor); // 상단 마력 링 생성
            AddPart("ArcaneOrb", PrimitiveType.Sphere, new Vector3(0f, 1.00f, 0f) * u, new Vector3(0.19f, 0.19f, 0.19f) * u, Quaternion.identity, _style.AccentColor); // 마력 보주 생성
            AddPart("CrystalLeft", PrimitiveType.Cube, new Vector3(-0.29f, 0.82f, 0f) * u, new Vector3(0.075f, 0.32f, 0.075f) * u, Quaternion.Euler(0f, 0f, 32f), _style.AccentColor); // 좌측 결정 생성
            AddPart("CrystalRight", PrimitiveType.Cube, new Vector3(0.29f, 0.82f, 0f) * u, new Vector3(0.075f, 0.32f, 0.075f) * u, Quaternion.Euler(0f, 0f, -32f), _style.AccentColor); // 우측 결정 생성
            AddPart("ArcaneSpire", PrimitiveType.Cube, new Vector3(0f, 1.18f, 0f) * u, new Vector3(0.055f, 0.28f, 0.055f) * u, Quaternion.Euler(0f, 0f, 45f), _style.RoofColor); // 상단 마력 첨탑 생성
        }

        private void BuildBossFortress()
        {
            float u = Unit; // 중간 보스 요새 단위 계산
            AddFoundation(u, _style.MainColor); // 대형 요새 기단 생성
            AddPart("BossKeep", PrimitiveType.Cube, new Vector3(0f, 0.40f, 0f) * u, new Vector3(0.70f, 0.70f, 0.66f) * u, Quaternion.identity, _style.MainColor); // 중앙 보스 성채 생성
            AddCornerTowers(u, 0.38f, 0.76f, 0.20f, _style.MainColor); // 대형 망루 생성
            AddBattlements(u, 0.88f, _style.AccentColor); // 보스 흉벽 생성
            AddPart("BossGate", PrimitiveType.Cube, new Vector3(0f, 0.30f, -0.355f) * u, new Vector3(0.23f, 0.38f, 0.045f) * u, Quaternion.identity, _style.RoofColor); // 보스 성문 생성
            AddPart("BossHornLeft", PrimitiveType.Cube, new Vector3(-0.26f, 1.02f, 0f) * u, new Vector3(0.06f, 0.38f, 0.06f) * u, Quaternion.Euler(0f, 0f, -27f), _style.AccentColor); // 좌측 뿔 첨탑 생성
            AddPart("BossHornRight", PrimitiveType.Cube, new Vector3(0.26f, 1.02f, 0f) * u, new Vector3(0.06f, 0.38f, 0.06f) * u, Quaternion.Euler(0f, 0f, 27f), _style.AccentColor); // 우측 뿔 첨탑 생성
            AddPart("BossCore", PrimitiveType.Sphere, new Vector3(0f, 1.02f, -0.05f) * u, new Vector3(0.19f, 0.19f, 0.19f) * u, Quaternion.identity, _style.AccentColor); // 보스 핵 장식 생성
        }

        private void BuildFinalCitadel()
        {
            float u = Unit; // 최종 성채 모델 단위 계산
            AddFoundation(u, _style.MainColor); // 최종 성채 대형 기단 생성
            AddPart("CitadelBody", PrimitiveType.Cube, new Vector3(0f, 0.48f, 0f) * u, new Vector3(0.78f, 0.82f, 0.72f) * u, Quaternion.identity, _style.MainColor); // 최종 성채 본체 생성
            AddCornerTowers(u, 0.43f, 0.92f, 0.22f, _style.MainColor); // 최종 성채 대형 망루 생성
            AddBattlements(u, 1.04f, _style.AccentColor); // 최종 성채 흉벽 생성
            AddPart("CitadelGate", PrimitiveType.Cube, new Vector3(0f, 0.34f, -0.395f) * u, new Vector3(0.26f, 0.44f, 0.05f) * u, Quaternion.identity, _style.RoofColor); // 최종 성문 생성
            AddPart("CentralTower", PrimitiveType.Cylinder, new Vector3(0f, 1.10f, 0f) * u, new Vector3(0.27f, 0.52f, 0.27f) * u, Quaternion.identity, _style.RoofColor); // 중앙 왕탑 생성
            AddPart("CrownDisk", PrimitiveType.Cylinder, new Vector3(0f, 1.62f, 0f) * u, new Vector3(0.38f, 0.08f, 0.38f) * u, Quaternion.identity, _style.AccentColor); // 왕관 받침 생성
            AddPart("CrownSpire", PrimitiveType.Cube, new Vector3(0f, 1.92f, 0f) * u, new Vector3(0.08f, 0.50f, 0.08f) * u, Quaternion.Euler(0f, 0f, 45f), _style.AccentColor); // 중앙 왕관 첨탑 생성
            AddPart("CrownLeft", PrimitiveType.Cube, new Vector3(-0.24f, 1.80f, 0f) * u, new Vector3(0.065f, 0.34f, 0.065f) * u, Quaternion.Euler(0f, 0f, -18f), _style.AccentColor); // 좌측 왕관 첨탑 생성
            AddPart("CrownRight", PrimitiveType.Cube, new Vector3(0.24f, 1.80f, 0f) * u, new Vector3(0.065f, 0.34f, 0.065f) * u, Quaternion.Euler(0f, 0f, 18f), _style.AccentColor); // 우측 왕관 첨탑 생성
        }

        private void AddFoundation(float u, Color color)
        {
            AddPart("Foundation", PrimitiveType.Cylinder, new Vector3(0f, 0.055f, 0f) * u, new Vector3(0.46f, 0.055f, 0.46f) * u, Quaternion.identity, color); // 건물 공통 원형 기단 생성
            AddPart("FoundationTrim", PrimitiveType.Cylinder, new Vector3(0f, 0.125f, 0f) * u, new Vector3(0.39f, 0.035f, 0.39f) * u, Quaternion.identity, _style.AccentColor); // 건물 공통 강조 테두리 생성
        }

        private void AddCornerTowers(float u, float offset, float height, float radius, Color color)
        {
            AddTower(-offset, -offset, u, height, radius, color); // 좌전 망루 생성
            AddTower(offset, -offset, u, height, radius, color); // 우전 망루 생성
            AddTower(-offset, offset, u, height, radius, color); // 좌후 망루 생성
            AddTower(offset, offset, u, height, radius, color); // 우후 망루 생성
        }

        private void AddTower(float x, float z, float u, float height, float radius, Color color)
        {
            AddPart("Tower", PrimitiveType.Cylinder, new Vector3(x, height * 0.5f, z) * u, new Vector3(radius, height * 0.5f, radius) * u, Quaternion.identity, color); // 망루 몸통 생성
            AddPart("TowerCap", PrimitiveType.Cylinder, new Vector3(x, height + 0.055f, z) * u, new Vector3(radius * 1.25f, 0.055f, radius * 1.25f) * u, Quaternion.identity, _style.RoofColor); // 망루 상단 지붕 생성
        }

        private void AddBattlements(float u, float y, Color color)
        {
            float offset = 0.25f; // 흉벽 모서리 위치 계산
            AddPart("BattlementA", PrimitiveType.Cube, new Vector3(-offset, y, -offset) * u, new Vector3(0.11f, 0.13f, 0.11f) * u, Quaternion.identity, color); // 좌전 흉벽 생성
            AddPart("BattlementB", PrimitiveType.Cube, new Vector3(offset, y, -offset) * u, new Vector3(0.11f, 0.13f, 0.11f) * u, Quaternion.identity, color); // 우전 흉벽 생성
            AddPart("BattlementC", PrimitiveType.Cube, new Vector3(-offset, y, offset) * u, new Vector3(0.11f, 0.13f, 0.11f) * u, Quaternion.identity, color); // 좌후 흉벽 생성
            AddPart("BattlementD", PrimitiveType.Cube, new Vector3(offset, y, offset) * u, new Vector3(0.11f, 0.13f, 0.11f) * u, Quaternion.identity, color); // 우후 흉벽 생성
        }

        private void AddPillar(float x, float z, float u, Color color)
        {
            AddPart("Pillar", PrimitiveType.Cylinder, new Vector3(x, 0.32f, z) * u, new Vector3(0.065f, 0.30f, 0.065f) * u, Quaternion.identity, color); // 보물고 장식 기둥 생성
        }

        private void AddPart(string partName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType); // 실제 3D Primitive 파츠 생성
            part.name = partName; // Hierarchy 파츠 이름 적용
            part.transform.SetParent(_visualRoot.transform, false); // 건물 루트 자식 연결
            part.transform.localPosition = localPosition; // 파츠 위치 적용
            part.transform.localRotation = localRotation; // 파츠 회전 적용
            part.transform.localScale = localScale; // 파츠 크기 적용

            Collider collider = part.GetComponent<Collider>(); // 자동 콜라이더 조회

            if (collider != null) // 콜라이더 존재 여부 확인
            {
                DestroyRuntimeObject(collider); // 지도 클릭 방해 콜라이더 제거
            }

            Renderer renderer = part.GetComponent<Renderer>(); // 파츠 렌더러 조회

            if (renderer == null) // 렌더러 생성 실패 확인
            {
                return; // 색상 등록 생략
            }

            Material material = CreateMaterial(renderer, color); // 안전한 파츠 머티리얼 생성

            if (material != null) // 머티리얼 준비 여부 확인
            {
                renderer.sharedMaterial = material; // 파츠 색상 머티리얼 적용
                _materials.Add(material); // 파괴 대상 머티리얼 기록
            }

            _partRenderers.Add(renderer); // 상태 색상 동기화 대상 등록
            _baseColors.Add(color); // 파츠 원본 색상 저장
        }

        private static Material CreateMaterial(Renderer renderer, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit"); // URP Lit 셰이더 우선 탐색

            if (shader == null) // URP 셰이더 누락 확인
            {
                shader = Shader.Find("Standard"); // 기본 Standard 셰이더 탐색
            }

            if (shader != null) // 사용 가능한 셰이더 확인
            {
                Material material = new Material(shader); // 새 런타임 머티리얼 생성
                material.color = color; // 건물 파츠 기본 색상 적용
                return material; // 생성 머티리얼 반환
            }

            Material fallback = renderer.material; // Primitive 기본 머티리얼 fallback 확보

            if (fallback != null) // 기본 머티리얼 존재 여부 확인
            {
                fallback.color = color; // fallback 색상 적용
            }

            return fallback; // 최종 머티리얼 반환
        }

        private void RefreshTint(bool force)
        {
            Color signalColor = ResolveSignalColor(); // 현재 노드 상태 색상 조회

            if (!force && ColorsApproximatelyEqual(signalColor, _lastSignalColor)) // 실제 상태 색상 변경 여부 확인
            {
                return; // 동일 상태 재적용 생략
            }

            _lastSignalColor = signalColor; // 현재 상태 색상 저장

            for (int i = 0; i < _partRenderers.Count && i < _baseColors.Count; i++) // 전체 건물 파츠 순회
            {
                Renderer renderer = _partRenderers[i]; // 현재 파츠 렌더러 조회

                if (renderer == null || renderer.sharedMaterial == null) // 파괴·누락 파츠 확인
                {
                    continue; // 색상 변경 제외
                }

                renderer.sharedMaterial.color = Color.Lerp(_baseColors[i], signalColor, SignalTintWeight); // 노드 상태 색상을 건물에 약하게 혼합
            }
        }

        private Color ResolveSignalColor()
        {
            if (_signalRenderer != null && _signalRenderer.sharedMaterial != null) // 신호 렌더러 존재 여부 확인
            {
                return _signalRenderer.sharedMaterial.color; // Hover·선택 상태 색상 반환
            }

            return _style != null ? _style.AccentColor : Color.white; // 신호 누락 시 건물 강조색 사용
        }

        private static bool ColorsApproximatelyEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.005f && Mathf.Abs(a.g - b.g) < 0.005f && Mathf.Abs(a.b - b.b) < 0.005f && Mathf.Abs(a.a - b.a) < 0.005f; // 미세 색상 차이 무시
        }

        private float Unit => _tileSize * _style.Scale; // 타일 크기·스테이지 스타일 기반 실제 모델 단위

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null) // 제거 대상 확인
            {
                return; // 빈 대상 제거 생략
            }

            if (Application.isPlaying) // Play Mode 여부 확인
            {
                Object.Destroy(target); // 프레임 종료 시 안전 제거
            }
            else
            {
                Object.DestroyImmediate(target); // EditMode 테스트 즉시 제거
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _materials.Count; i++) // 생성 머티리얼 순회
            {
                if (_materials[i] != null) // 남은 머티리얼 확인
                {
                    DestroyRuntimeObject(_materials[i]); // 런타임 머티리얼 정리
                }
            }

            _materials.Clear(); // 머티리얼 참조 목록 초기화
        }
    }
}
