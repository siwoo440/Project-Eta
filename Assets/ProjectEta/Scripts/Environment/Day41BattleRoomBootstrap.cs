using System.Collections.Generic; // 생성 Material 수명 관리
using UnityEngine; // GameObject·Primitive·Light·Material 기능
using UnityEngine.Rendering; // 그림자·환경광 설정
using UnityEngine.SceneManagement; // Battle 씬 판정
using ProjectEta.Board; // 기존 TableCameraRig 연결

namespace ProjectEta.Environment // 전투 공간 프레젠테이션 네임스페이스
{
    [DefaultExecutionOrder(-900)] // 일반 런타임 초기화보다 이른 환경 생성
    public sealed class Day41BattleRoomBootstrap : MonoBehaviour // 41일차 거대 방·테이블 런타임 모델 생성기
    {
        private sealed class Palette // 런타임 공유 재질 묶음
        {
            public Material Floor; // 바닥 재질
            public Material FloorAlt; // 바닥 변형 재질
            public Material Wall; // 벽 재질
            public Material WallAlt; // 벽 변형 재질
            public Material Seam; // 패널 틈·구조 재질
            public Material TableWood; // 테이블 목재 재질
            public Material TableDark; // 테이블 어두운 목재 재질
            public Material Metal; // 금속 장식 재질
            public Material Upholstery; // 의자 쿠션 재질
            public Material Opponent; // 상대 실루엣 재질
            public Material Accent; // 조명·포인트 장식 재질
        }

        private readonly List<Material> _runtimeMaterials = new List<Material>(); // 런타임 생성 재질 목록
        private Palette _palette; // 현재 환경 재질 팔레트
        private int _createdPartCount; // 생성된 모델 파트 수

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 진입 직후 자동 환경 생성
        private static void AutoCreateForBattleScene() // 씬 수정 없이 41일차 환경 자동 주입
        {
            Scene activeScene = SceneManager.GetActiveScene(); // 현재 활성 씬 조회
            if (!Day41BattleRoomLayout.IsBattleScene(activeScene.name)) return; // Battle 씬 외 생성 방지
            if (GameObject.Find(Day41BattleRoomLayout.RootName) != null) return; // 중복 환경 루트 생성 방지

            GameObject root = new GameObject(Day41BattleRoomLayout.RootName); // 환경 루트 생성
            root.AddComponent<Day41BattleRoomBootstrap>(); // 모델 생성 부트스트랩 연결
        }

        private void Awake() // 환경 모델 생성 시작
        {
            if (!Day41BattleRoomLayout.IsBattleScene(SceneManager.GetActiveScene().name)) return; // Battle 씬 외 실행 방지

            _palette = CreatePalette(); // 공유 재질 생성
            ConfigureRenderSettings(); // 거대 방 조명 환경 설정
            BuildRoomShell(); // 바닥·벽·천장 패널 생성
            BuildTableAndBoardFrame(); // 중앙 테이블·보드 프레임 생성
            BuildPlayerChair(); // 플레이어 좌석 생성
            BuildOpponentChair(); // 상대 좌석 생성
            BuildOpponentFigure(); // 상대 실루엣 생성
            BuildCeilingFixture(); // 중앙 천장 조명 구조 생성
            BuildOpponentBackdrop(); // 상대 뒤 벽 포컬 구조 생성
            BuildLighting(); // 전투 테이블 중심 조명 생성
            ConfigureCamera(); // 앉은 플레이어 시점 적용

            Debug.Log($"41일차 전투 공간 생성 완료: {_createdPartCount}개 모델 파트"); // 생성 결과 로그
        }

        private Palette CreatePalette() // 참고 이미지 기반 저채도 재질 구성
        {
            Palette palette = new Palette // 재질 팔레트 인스턴스 생성
            {
                Floor = CreateMaterial("Day41_Floor", new Color(0.67f, 0.68f, 0.69f), 0.12f, 0f), // 밝은 회색 바닥
                FloorAlt = CreateMaterial("Day41_FloorAlt", new Color(0.58f, 0.60f, 0.62f), 0.08f, 0f), // 변형 회색 바닥
                Wall = CreateMaterial("Day41_Wall", new Color(0.72f, 0.73f, 0.74f), 0.10f, 0f), // 밝은 벽 패널
                WallAlt = CreateMaterial("Day41_WallAlt", new Color(0.62f, 0.64f, 0.66f), 0.08f, 0f), // 돌출 벽 패널
                Seam = CreateMaterial("Day41_Seam", new Color(0.14f, 0.15f, 0.17f), 0.05f, 0.05f), // 패널 틈 구조
                TableWood = CreateMaterial("Day41_TableWood", new Color(0.17f, 0.10f, 0.065f), 0.35f, 0.05f), // 어두운 목재
                TableDark = CreateMaterial("Day41_TableDark", new Color(0.075f, 0.045f, 0.035f), 0.30f, 0.05f), // 짙은 목재
                Metal = CreateMaterial("Day41_Metal", new Color(0.20f, 0.18f, 0.14f), 0.55f, 0.70f), // 어두운 금속
                Upholstery = CreateMaterial("Day41_Upholstery", new Color(0.075f, 0.08f, 0.09f), 0.18f, 0f), // 검은 의자 쿠션
                Opponent = CreateMaterial("Day41_Opponent", new Color(0.025f, 0.028f, 0.035f), 0.12f, 0f), // 익명 상대 실루엣
                Accent = CreateMaterial("Day41_Accent", new Color(0.62f, 0.48f, 0.24f), 0.62f, 0.65f) // 황동 포인트
            };

            return palette; // 완성 재질 팔레트 반환
        }

        private Material CreateMaterial(string materialName, Color color, float smoothness, float metallic) // URP 호환 런타임 재질 생성
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit"); // URP Lit 셰이더 우선 탐색
            if (shader == null) shader = Shader.Find("Standard"); // 기본 Standard 셰이더 대체 탐색

            Material material = new Material(shader); // 런타임 재질 생성
            material.name = materialName; // 디버그용 재질 이름 지정
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color); // URP 기본색 적용
            if (material.HasProperty("_Color")) material.SetColor("_Color", color); // Standard 기본색 적용
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness); // 표면 매끈함 적용
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic); // 금속성 적용
            _runtimeMaterials.Add(material); // 수명 관리 목록 등록
            return material; // 생성 재질 반환
        }

        private void ConfigureRenderSettings() // 방 전체 환경광·안개 설정
        {
            RenderSettings.ambientMode = AmbientMode.Trilight; // 상하 분리 환경광 사용
            RenderSettings.ambientSkyColor = new Color(0.20f, 0.21f, 0.23f); // 차가운 상부 환경광
            RenderSettings.ambientEquatorColor = new Color(0.13f, 0.14f, 0.16f); // 중간 환경광
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.075f, 0.085f); // 어두운 하부 환경광
            RenderSettings.fog = true; // 거대 방 깊이감 안개 활성화
            RenderSettings.fogMode = FogMode.ExponentialSquared; // 부드러운 거리 안개 사용
            RenderSettings.fogDensity = 0.0035f; // 보드 가독성 유지 안개 농도
            RenderSettings.fogColor = new Color(0.18f, 0.19f, 0.21f); // 차가운 회색 안개색
        }

        private void BuildRoomShell() // 거대한 폐쇄형 패널 방 모델링
        {
            Transform roomRoot = CreateGroup("RoomShell", transform); // 방 구조 그룹 생성
            BuildFloor(roomRoot); // 바닥 패널 생성
            BuildWall(roomRoot, 0); // 북쪽 벽 생성
            BuildWall(roomRoot, 1); // 남쪽 벽 생성
            BuildWall(roomRoot, 2); // 동쪽 벽 생성
            BuildWall(roomRoot, 3); // 서쪽 벽 생성
            BuildCeiling(roomRoot); // 천장·보 구조 생성
            BuildCornerPillars(roomRoot); // 네 모서리 기둥 생성
        }

        private void BuildFloor(Transform parent) // 참고 이미지식 대형 바닥 패널 생성
        {
            Vector3 basePosition = new Vector3(0f, Day41BattleRoomLayout.FloorY - 0.16f, 0f); // 바닥 베이스 위치 계산
            Vector3 baseScale = new Vector3(Day41BattleRoomLayout.RoomWidth, 0.32f, Day41BattleRoomLayout.RoomDepth); // 바닥 베이스 크기 계산
            CreateBox("FloorBase", basePosition, baseScale, _palette.Seam, parent); // 패널 틈용 어두운 바닥 베이스 생성

            float usableWidth = Day41BattleRoomLayout.RoomWidth - 1f; // 벽 안쪽 바닥 가로 계산
            float usableDepth = Day41BattleRoomLayout.RoomDepth - 1f; // 벽 안쪽 바닥 깊이 계산
            int columns = Mathf.FloorToInt(usableWidth / Day41BattleRoomLayout.FloorPanelSize); // 바닥 가로 패널 수 계산
            int rows = Mathf.FloorToInt(usableDepth / Day41BattleRoomLayout.FloorPanelSize); // 바닥 세로 패널 수 계산
            float panelSize = Day41BattleRoomLayout.FloorPanelSize - Day41BattleRoomLayout.PanelGap; // 실제 패널 크기 계산

            for (int x = 0; x < columns; x++) // 바닥 가로 패널 순회
            {
                for (int z = 0; z < rows; z++) // 바닥 세로 패널 순회
                {
                    float px = (x - (columns - 1) * 0.5f) * Day41BattleRoomLayout.FloorPanelSize; // 패널 월드 X 계산
                    float pz = (z - (rows - 1) * 0.5f) * Day41BattleRoomLayout.FloorPanelSize; // 패널 월드 Z 계산
                    float heightOffset = ((x * 5 + z * 3) % 13 == 0) ? 0.045f : 0f; // 일부 패널 미세 돌출 계산
                    Material material = ((x + z) % 7 == 0) ? _palette.FloorAlt : _palette.Floor; // 패널 색상 변화 선택
                    Vector3 position = new Vector3(px, Day41BattleRoomLayout.FloorY + 0.015f + heightOffset, pz); // 패널 위치 계산
                    Vector3 scale = new Vector3(panelSize, 0.07f, panelSize); // 패널 크기 계산
                    CreateBox($"FloorPanel_{x}_{z}", position, scale, material, parent); // 개별 바닥 패널 생성
                }
            }
        }

        private void BuildWall(Transform parent, int wallIndex) // 방향별 대형 벽 패널 생성
        {
            bool northSouth = wallIndex <= 1; // 앞뒤 벽 여부 판정
            float wallLength = northSouth ? Day41BattleRoomLayout.RoomWidth : Day41BattleRoomLayout.RoomDepth; // 벽 가로 길이 계산
            float roomHeight = Day41BattleRoomLayout.CeilingY - Day41BattleRoomLayout.FloorY; // 벽 전체 높이 계산
            float halfWidth = Day41BattleRoomLayout.RoomWidth * 0.5f; // 방 가로 반경 계산
            float halfDepth = Day41BattleRoomLayout.RoomDepth * 0.5f; // 방 깊이 반경 계산
            Vector3 basePosition; // 벽 베이스 위치 저장
            Vector3 baseScale; // 벽 베이스 크기 저장

            if (wallIndex == 0) // 북쪽 벽 배치
            {
                basePosition = new Vector3(0f, (Day41BattleRoomLayout.FloorY + Day41BattleRoomLayout.CeilingY) * 0.5f, halfDepth); // 북쪽 벽 중심 계산
                baseScale = new Vector3(Day41BattleRoomLayout.RoomWidth, roomHeight, Day41BattleRoomLayout.WallThickness); // 북쪽 벽 크기 계산
            }
            else if (wallIndex == 1) // 남쪽 벽 배치
            {
                basePosition = new Vector3(0f, (Day41BattleRoomLayout.FloorY + Day41BattleRoomLayout.CeilingY) * 0.5f, -halfDepth); // 남쪽 벽 중심 계산
                baseScale = new Vector3(Day41BattleRoomLayout.RoomWidth, roomHeight, Day41BattleRoomLayout.WallThickness); // 남쪽 벽 크기 계산
            }
            else if (wallIndex == 2) // 동쪽 벽 배치
            {
                basePosition = new Vector3(halfWidth, (Day41BattleRoomLayout.FloorY + Day41BattleRoomLayout.CeilingY) * 0.5f, 0f); // 동쪽 벽 중심 계산
                baseScale = new Vector3(Day41BattleRoomLayout.WallThickness, roomHeight, Day41BattleRoomLayout.RoomDepth); // 동쪽 벽 크기 계산
            }
            else // 서쪽 벽 배치
            {
                basePosition = new Vector3(-halfWidth, (Day41BattleRoomLayout.FloorY + Day41BattleRoomLayout.CeilingY) * 0.5f, 0f); // 서쪽 벽 중심 계산
                baseScale = new Vector3(Day41BattleRoomLayout.WallThickness, roomHeight, Day41BattleRoomLayout.RoomDepth); // 서쪽 벽 크기 계산
            }

            CreateBox($"WallBase_{wallIndex}", basePosition, baseScale, _palette.Seam, parent); // 패널 틈용 벽 베이스 생성

            int columns = Mathf.FloorToInt((wallLength - 1f) / Day41BattleRoomLayout.WallPanelSize); // 벽 가로 패널 수 계산
            int rows = Mathf.FloorToInt((roomHeight - 1f) / Day41BattleRoomLayout.WallPanelSize); // 벽 세로 패널 수 계산
            float panelSize = Day41BattleRoomLayout.WallPanelSize - Day41BattleRoomLayout.PanelGap; // 벽 패널 실제 크기 계산
            float verticalStart = Day41BattleRoomLayout.FloorY + Day41BattleRoomLayout.WallPanelSize * 0.6f; // 첫 패널 높이 계산

            for (int column = 0; column < columns; column++) // 벽 가로 패널 순회
            {
                for (int row = 0; row < rows; row++) // 벽 세로 패널 순회
                {
                    float horizontal = (column - (columns - 1) * 0.5f) * Day41BattleRoomLayout.WallPanelSize; // 벽 패널 수평 좌표 계산
                    float vertical = verticalStart + row * Day41BattleRoomLayout.WallPanelSize; // 벽 패널 수직 좌표 계산
                    float depthOffset = Day41BattleRoomLayout.GetWallPanelDepthOffset(column, row, wallIndex); // 패널 돌출값 계산
                    Material material = Mathf.Abs(depthOffset) > 0.001f ? _palette.WallAlt : _palette.Wall; // 돌출 패널 색상 선택
                    Vector3 position; // 패널 월드 위치 저장
                    Vector3 scale; // 패널 월드 크기 저장

                    if (wallIndex == 0) // 북쪽 패널 위치 계산
                    {
                        position = new Vector3(horizontal, vertical, halfDepth - Day41BattleRoomLayout.WallThickness * 0.5f - 0.08f - depthOffset); // 북쪽 패널 안쪽 면 배치
                        scale = new Vector3(panelSize, panelSize, 0.16f); // 북쪽 패널 크기 계산
                    }
                    else if (wallIndex == 1) // 남쪽 패널 위치 계산
                    {
                        position = new Vector3(horizontal, vertical, -halfDepth + Day41BattleRoomLayout.WallThickness * 0.5f + 0.08f + depthOffset); // 남쪽 패널 안쪽 면 배치
                        scale = new Vector3(panelSize, panelSize, 0.16f); // 남쪽 패널 크기 계산
                    }
                    else if (wallIndex == 2) // 동쪽 패널 위치 계산
                    {
                        position = new Vector3(halfWidth - Day41BattleRoomLayout.WallThickness * 0.5f - 0.08f - depthOffset, vertical, horizontal); // 동쪽 패널 안쪽 면 배치
                        scale = new Vector3(0.16f, panelSize, panelSize); // 동쪽 패널 크기 계산
                    }
                    else // 서쪽 패널 위치 계산
                    {
                        position = new Vector3(-halfWidth + Day41BattleRoomLayout.WallThickness * 0.5f + 0.08f + depthOffset, vertical, horizontal); // 서쪽 패널 안쪽 면 배치
                        scale = new Vector3(0.16f, panelSize, panelSize); // 서쪽 패널 크기 계산
                    }

                    CreateBox($"WallPanel_{wallIndex}_{column}_{row}", position, scale, material, parent); // 개별 벽 패널 생성

                    if ((column * 3 + row * 5 + wallIndex) % 17 == 0) // 일부 패널 접힘 느낌 선택
                    {
                        BuildPanelFacet(parent, wallIndex, position, panelSize); // 참고 이미지식 각진 돌출 장식 생성
                    }
                }
            }

            BuildWallTrim(parent, wallIndex, wallLength, roomHeight); // 벽 하단·상단 구조선 생성
        }

        private void BuildPanelFacet(Transform parent, int wallIndex, Vector3 panelPosition, float panelSize) // 벽 패널 각진 돌출 장식
        {
            Vector3 position = panelPosition; // 장식 기준 위치 복사
            Vector3 scale; // 장식 크기 저장
            Quaternion rotation; // 장식 회전 저장

            if (wallIndex <= 1) // 앞뒤 벽 장식 계산
            {
                float direction = wallIndex == 0 ? -1f : 1f; // 방 안쪽 방향 계산
                position += new Vector3(panelSize * 0.24f, panelSize * 0.25f, direction * 0.12f); // 패널 우상단 돌출 위치 계산
                scale = new Vector3(panelSize * 0.72f, panelSize * 0.09f, 0.26f); // 얇은 사선 면 크기 계산
                rotation = Quaternion.Euler(0f, 0f, 43f); // 패널 면 사선 회전 계산
            }
            else // 좌우 벽 장식 계산
            {
                float direction = wallIndex == 2 ? -1f : 1f; // 방 안쪽 방향 계산
                position += new Vector3(direction * 0.12f, panelSize * 0.25f, panelSize * 0.24f); // 패널 우상단 돌출 위치 계산
                scale = new Vector3(0.26f, panelSize * 0.09f, panelSize * 0.72f); // 얇은 사선 면 크기 계산
                rotation = Quaternion.Euler(43f, 0f, 0f); // 패널 면 사선 회전 계산
            }

            CreateBox("PanelFacet", position, scale, _palette.WallAlt, parent, rotation); // 각진 돌출 면 생성
        }

        private void BuildWallTrim(Transform parent, int wallIndex, float wallLength, float roomHeight) // 벽 상하부 몰딩 생성
        {
            float halfWidth = Day41BattleRoomLayout.RoomWidth * 0.5f; // 방 가로 반경 계산
            float halfDepth = Day41BattleRoomLayout.RoomDepth * 0.5f; // 방 깊이 반경 계산
            float lowerY = Day41BattleRoomLayout.FloorY + 0.42f; // 하단 몰딩 높이 계산
            float upperY = Day41BattleRoomLayout.CeilingY - 0.46f; // 상단 몰딩 높이 계산
            Vector3 lowerPosition; // 하단 몰딩 위치 저장
            Vector3 upperPosition; // 상단 몰딩 위치 저장
            Vector3 scale; // 몰딩 크기 저장

            if (wallIndex == 0) // 북쪽 몰딩 계산
            {
                lowerPosition = new Vector3(0f, lowerY, halfDepth - 0.42f); // 북쪽 하단 몰딩 위치
                upperPosition = new Vector3(0f, upperY, halfDepth - 0.42f); // 북쪽 상단 몰딩 위치
                scale = new Vector3(wallLength, 0.36f, 0.34f); // 북쪽 몰딩 크기
            }
            else if (wallIndex == 1) // 남쪽 몰딩 계산
            {
                lowerPosition = new Vector3(0f, lowerY, -halfDepth + 0.42f); // 남쪽 하단 몰딩 위치
                upperPosition = new Vector3(0f, upperY, -halfDepth + 0.42f); // 남쪽 상단 몰딩 위치
                scale = new Vector3(wallLength, 0.36f, 0.34f); // 남쪽 몰딩 크기
            }
            else if (wallIndex == 2) // 동쪽 몰딩 계산
            {
                lowerPosition = new Vector3(halfWidth - 0.42f, lowerY, 0f); // 동쪽 하단 몰딩 위치
                upperPosition = new Vector3(halfWidth - 0.42f, upperY, 0f); // 동쪽 상단 몰딩 위치
                scale = new Vector3(0.34f, 0.36f, wallLength); // 동쪽 몰딩 크기
            }
            else // 서쪽 몰딩 계산
            {
                lowerPosition = new Vector3(-halfWidth + 0.42f, lowerY, 0f); // 서쪽 하단 몰딩 위치
                upperPosition = new Vector3(-halfWidth + 0.42f, upperY, 0f); // 서쪽 상단 몰딩 위치
                scale = new Vector3(0.34f, 0.36f, wallLength); // 서쪽 몰딩 크기
            }

            CreateBox($"WallLowerTrim_{wallIndex}", lowerPosition, scale, _palette.Seam, parent); // 하단 몰딩 생성
            CreateBox($"WallUpperTrim_{wallIndex}", upperPosition, scale, _palette.Seam, parent); // 상단 몰딩 생성
        }

        private void BuildCeiling(Transform parent) // 높은 천장·보 구조 생성
        {
            Vector3 ceilingPosition = new Vector3(0f, Day41BattleRoomLayout.CeilingY + 0.16f, 0f); // 천장 베이스 위치 계산
            Vector3 ceilingScale = new Vector3(Day41BattleRoomLayout.RoomWidth, 0.32f, Day41BattleRoomLayout.RoomDepth); // 천장 베이스 크기 계산
            CreateBox("CeilingBase", ceilingPosition, ceilingScale, _palette.Seam, parent); // 어두운 천장 베이스 생성

            for (int x = -4; x <= 4; x++) // 천장 세로 보 반복
            {
                Vector3 position = new Vector3(x * 3.6f, Day41BattleRoomLayout.CeilingY - 0.10f, 0f); // 세로 보 위치 계산
                Vector3 scale = new Vector3(0.26f, 0.30f, Day41BattleRoomLayout.RoomDepth - 1f); // 세로 보 크기 계산
                CreateBox($"CeilingBeamX_{x}", position, scale, _palette.Metal, parent); // 천장 세로 보 생성
            }

            for (int z = -5; z <= 5; z++) // 천장 가로 보 반복
            {
                Vector3 position = new Vector3(0f, Day41BattleRoomLayout.CeilingY - 0.12f, z * 3.4f); // 가로 보 위치 계산
                Vector3 scale = new Vector3(Day41BattleRoomLayout.RoomWidth - 1f, 0.26f, 0.24f); // 가로 보 크기 계산
                CreateBox($"CeilingBeamZ_{z}", position, scale, _palette.Metal, parent); // 천장 가로 보 생성
            }
        }

        private void BuildCornerPillars(Transform parent) // 방 모서리 거대 기둥 생성
        {
            float x = Day41BattleRoomLayout.RoomWidth * 0.5f - 0.72f; // 기둥 X 오프셋 계산
            float z = Day41BattleRoomLayout.RoomDepth * 0.5f - 0.72f; // 기둥 Z 오프셋 계산
            float height = Day41BattleRoomLayout.CeilingY - Day41BattleRoomLayout.FloorY; // 기둥 전체 높이 계산
            float centerY = (Day41BattleRoomLayout.CeilingY + Day41BattleRoomLayout.FloorY) * 0.5f; // 기둥 중심 높이 계산
            Vector3[] positions = // 네 모서리 위치 목록
            {
                new Vector3(x, centerY, z), // 북동 모서리
                new Vector3(-x, centerY, z), // 북서 모서리
                new Vector3(x, centerY, -z), // 남동 모서리
                new Vector3(-x, centerY, -z) // 남서 모서리
            };

            for (int i = 0; i < positions.Length; i++) // 모서리 기둥 순회
            {
                CreateBox($"CornerPillar_{i}", positions[i], new Vector3(1.05f, height, 1.05f), _palette.Seam, parent); // 기둥 본체 생성
                CreateBox($"CornerPillarCapLow_{i}", new Vector3(positions[i].x, Day41BattleRoomLayout.FloorY + 0.55f, positions[i].z), new Vector3(1.45f, 0.35f, 1.45f), _palette.Metal, parent); // 기둥 하단 캡 생성
                CreateBox($"CornerPillarCapHigh_{i}", new Vector3(positions[i].x, Day41BattleRoomLayout.CeilingY - 0.55f, positions[i].z), new Vector3(1.45f, 0.35f, 1.45f), _palette.Metal, parent); // 기둥 상단 캡 생성
            }
        }

        private void BuildTableAndBoardFrame() // 기존 보드를 감싸는 중앙 테이블 모델링
        {
            Transform tableRoot = CreateGroup("CentralTable", transform); // 중앙 테이블 그룹 생성
            Vector3 topScale = new Vector3(Day41BattleRoomLayout.TableTopSize.x, Day41BattleRoomLayout.TableTopThickness, Day41BattleRoomLayout.TableTopSize.y); // 상판 크기 계산
            CreateBox("TableTop", new Vector3(0f, Day41BattleRoomLayout.TableTopY, 0f), topScale, _palette.TableWood, tableRoot); // 메인 상판 생성
            CreateBox("TableTopLowerLip", new Vector3(0f, Day41BattleRoomLayout.TableTopY - 0.31f, 0f), new Vector3(13.15f, 0.20f, 13.15f), _palette.TableDark, tableRoot); // 상판 하부 몰딩 생성
            CreateBox("TableTopUpperLip", new Vector3(0f, -0.055f, 0f), new Vector3(12.95f, 0.08f, 12.95f), _palette.Metal, tableRoot); // 상판 상부 얇은 금속 테두리 생성
            BuildBoardFrame(tableRoot); // 보드 외곽 프레임 생성
            BuildTableApron(tableRoot); // 테이블 측면 앞치마 생성
            BuildTableLegs(tableRoot); // 테이블 다리·보강대 생성
            BuildTableCornerDetails(tableRoot); // 테이블 모서리 장식 생성
        }

        private void BuildBoardFrame(Transform parent) // 보드 클릭을 가리지 않는 외곽 프레임 생성
        {
            float outer = Day41BattleRoomLayout.BoardFrameOuterSize; // 프레임 외곽 크기 조회
            float inner = Day41BattleRoomLayout.BoardFrameInnerSize; // 프레임 내부 크기 조회
            float rail = (outer - inner) * 0.5f; // 프레임 레일 두께 계산
            float offset = inner * 0.5f + rail * 0.5f; // 프레임 레일 중심 오프셋 계산
            float railY = -0.005f; // 보드 타일과 거의 같은 프레임 높이 설정
            Vector3 horizontalScale = new Vector3(outer, 0.10f, rail); // 가로 레일 크기 계산
            Vector3 verticalScale = new Vector3(rail, 0.10f, inner); // 세로 레일 크기 계산

            CreateBox("BoardFrame_North", new Vector3(0f, railY, offset), horizontalScale, _palette.Metal, parent); // 북쪽 보드 프레임 생성
            CreateBox("BoardFrame_South", new Vector3(0f, railY, -offset), horizontalScale, _palette.Metal, parent); // 남쪽 보드 프레임 생성
            CreateBox("BoardFrame_East", new Vector3(offset, railY, 0f), verticalScale, _palette.Metal, parent); // 동쪽 보드 프레임 생성
            CreateBox("BoardFrame_West", new Vector3(-offset, railY, 0f), verticalScale, _palette.Metal, parent); // 서쪽 보드 프레임 생성

            float corner = rail * 1.15f; // 코너 장식 크기 계산
            CreateCylinder("BoardCorner_NE", new Vector3(offset, 0.035f, offset), new Vector3(corner, 0.07f, corner), _palette.Accent, parent); // 북동 코너 장식 생성
            CreateCylinder("BoardCorner_NW", new Vector3(-offset, 0.035f, offset), new Vector3(corner, 0.07f, corner), _palette.Accent, parent); // 북서 코너 장식 생성
            CreateCylinder("BoardCorner_SE", new Vector3(offset, 0.035f, -offset), new Vector3(corner, 0.07f, corner), _palette.Accent, parent); // 남동 코너 장식 생성
            CreateCylinder("BoardCorner_SW", new Vector3(-offset, 0.035f, -offset), new Vector3(corner, 0.07f, corner), _palette.Accent, parent); // 남서 코너 장식 생성
        }

        private void BuildTableApron(Transform parent) // 테이블 측면 구조 생성
        {
            float halfX = Day41BattleRoomLayout.TableTopSize.x * 0.5f - 0.30f; // 측면 X 반경 계산
            float halfZ = Day41BattleRoomLayout.TableTopSize.y * 0.5f - 0.30f; // 측면 Z 반경 계산
            float y = -0.88f; // 앞치마 중심 높이 설정

            CreateBox("Apron_North", new Vector3(0f, y, halfZ), new Vector3(11.55f, 0.95f, 0.34f), _palette.TableDark, parent); // 북쪽 앞치마 생성
            CreateBox("Apron_South", new Vector3(0f, y, -halfZ), new Vector3(11.55f, 0.95f, 0.34f), _palette.TableDark, parent); // 남쪽 앞치마 생성
            CreateBox("Apron_East", new Vector3(halfX, y, 0f), new Vector3(0.34f, 0.95f, 11.55f), _palette.TableDark, parent); // 동쪽 앞치마 생성
            CreateBox("Apron_West", new Vector3(-halfX, y, 0f), new Vector3(0.34f, 0.95f, 11.55f), _palette.TableDark, parent); // 서쪽 앞치마 생성
        }

        private void BuildTableLegs(Transform parent) // 묵직한 테이블 다리·가로대 생성
        {
            float legX = 5.05f; // 다리 X 오프셋 설정
            float legZ = 5.05f; // 다리 Z 오프셋 설정
            float legBottom = Day41BattleRoomLayout.FloorY + 0.15f; // 다리 하단 높이 계산
            float legTop = -0.72f; // 다리 상단 높이 설정
            float legHeight = legTop - legBottom; // 다리 높이 계산
            float legY = legBottom + legHeight * 0.5f; // 다리 중심 높이 계산
            Vector3[] legPositions = // 네 다리 위치 목록
            {
                new Vector3(legX, legY, legZ), // 북동 다리
                new Vector3(-legX, legY, legZ), // 북서 다리
                new Vector3(legX, legY, -legZ), // 남동 다리
                new Vector3(-legX, legY, -legZ) // 남서 다리
            };

            for (int i = 0; i < legPositions.Length; i++) // 테이블 다리 순회
            {
                CreateBox($"TableLeg_{i}", legPositions[i], new Vector3(0.78f, legHeight, 0.78f), _palette.TableWood, parent); // 사각 다리 본체 생성
                CreateBox($"TableLegFoot_{i}", new Vector3(legPositions[i].x, legBottom + 0.12f, legPositions[i].z), new Vector3(1.10f, 0.28f, 1.10f), _palette.TableDark, parent); // 다리 발 장식 생성
                CreateBox($"TableLegCapital_{i}", new Vector3(legPositions[i].x, legTop - 0.12f, legPositions[i].z), new Vector3(1.10f, 0.30f, 1.10f), _palette.Accent, parent); // 다리 상단 장식 생성
            }

            CreateBox("TableStretcher_X", new Vector3(0f, Day41BattleRoomLayout.FloorY + 0.78f, 0f), new Vector3(10.5f, 0.34f, 0.42f), _palette.TableDark, parent); // 가로 중앙 보강대 생성
            CreateBox("TableStretcher_Z", new Vector3(0f, Day41BattleRoomLayout.FloorY + 0.78f, 0f), new Vector3(0.42f, 0.34f, 10.5f), _palette.TableDark, parent); // 세로 중앙 보강대 생성
            CreateCylinder("TableStretcherHub", new Vector3(0f, Day41BattleRoomLayout.FloorY + 0.78f, 0f), new Vector3(0.95f, 0.32f, 0.95f), _palette.Metal, parent); // 중앙 보강 허브 생성
        }

        private void BuildTableCornerDetails(Transform parent) // 상판 모서리 장식 생성
        {
            float x = Day41BattleRoomLayout.TableTopSize.x * 0.5f - 0.24f; // 장식 X 오프셋 계산
            float z = Day41BattleRoomLayout.TableTopSize.y * 0.5f - 0.24f; // 장식 Z 오프셋 계산
            Vector3[] positions = // 네 모서리 장식 위치 목록
            {
                new Vector3(x, -0.10f, z), // 북동 모서리
                new Vector3(-x, -0.10f, z), // 북서 모서리
                new Vector3(x, -0.10f, -z), // 남동 모서리
                new Vector3(-x, -0.10f, -z) // 남서 모서리
            };

            for (int i = 0; i < positions.Length; i++) // 모서리 장식 순회
            {
                CreateCylinder($"TableCornerStud_{i}", positions[i], new Vector3(0.42f, 0.12f, 0.42f), _palette.Accent, parent); // 황동 스터드 생성
            }
        }

        private void BuildPlayerChair() // 플레이어 의자 생성
        {
            BuildChair("PlayerChair", Day41BattleRoomLayout.PlayerChairRoot, 0f); // 보드 방향 플레이어 좌석 생성
        }

        private void BuildOpponentChair() // 상대 의자 생성
        {
            BuildChair("OpponentChair", Day41BattleRoomLayout.OpponentChairRoot, 180f); // 플레이어 방향 상대 좌석 생성
        }

        private void BuildChair(string chairName, Vector3 position, float yaw) // 고등받이 의자 모델링
        {
            Transform chairRoot = CreateGroup(chairName, transform); // 의자 그룹 생성
            chairRoot.position = position; // 의자 월드 위치 적용
            chairRoot.rotation = Quaternion.Euler(0f, yaw, 0f); // 테이블 방향 회전 적용

            CreateBox("SeatBase", new Vector3(0f, 1.15f, 0f), new Vector3(3.35f, 0.34f, 2.55f), _palette.TableWood, chairRoot, Quaternion.identity, true); // 의자 좌판 본체 생성
            CreateBox("SeatCushion", new Vector3(0f, 1.39f, 0.05f), new Vector3(2.95f, 0.25f, 2.18f), _palette.Upholstery, chairRoot, Quaternion.identity, true); // 의자 좌판 쿠션 생성
            CreateBox("BackPanel", new Vector3(0f, 3.28f, -1.14f), new Vector3(2.75f, 3.45f, 0.34f), _palette.TableDark, chairRoot, Quaternion.identity, true); // 높은 등받이 판 생성
            CreateBox("BackCushion", new Vector3(0f, 3.23f, -0.92f), new Vector3(2.24f, 2.55f, 0.20f), _palette.Upholstery, chairRoot, Quaternion.identity, true); // 등받이 쿠션 생성
            CreateBox("BackTopRail", new Vector3(0f, 5.10f, -1.14f), new Vector3(3.20f, 0.38f, 0.44f), _palette.TableWood, chairRoot, Quaternion.identity, true); // 등받이 상단 레일 생성

            float[] xs = { -1.28f, 1.28f }; // 좌우 구조 X 목록
            for (int side = 0; side < xs.Length; side++) // 의자 좌우 구조 순회
            {
                float x = xs[side]; // 현재 구조 X 저장
                CreateBox($"FrontLeg_{side}", new Vector3(x, 0.55f, 0.82f), new Vector3(0.34f, 1.75f, 0.34f), _palette.TableWood, chairRoot, Quaternion.identity, true); // 앞다리 생성
                CreateBox($"RearLeg_{side}", new Vector3(x, 0.55f, -0.82f), new Vector3(0.38f, 1.75f, 0.38f), _palette.TableDark, chairRoot, Quaternion.identity, true); // 뒷다리 생성
                CreateBox($"BackPost_{side}", new Vector3(x, 3.20f, -1.18f), new Vector3(0.34f, 4.05f, 0.38f), _palette.TableWood, chairRoot, Quaternion.identity, true); // 등받이 기둥 생성
                CreateBox($"Arm_{side}", new Vector3(side == 0 ? -1.72f : 1.72f, 2.15f, 0.08f), new Vector3(0.28f, 0.24f, 2.35f), _palette.TableWood, chairRoot, Quaternion.identity, true); // 팔걸이 생성
                CreateBox($"ArmPost_{side}", new Vector3(side == 0 ? -1.72f : 1.72f, 1.65f, 0.84f), new Vector3(0.28f, 1.10f, 0.28f), _palette.Metal, chairRoot, Quaternion.identity, true); // 팔걸이 지지대 생성
                CreateSphere($"Finial_{side}", new Vector3(x, 5.47f, -1.18f), new Vector3(0.50f, 0.50f, 0.50f), _palette.Accent, chairRoot, true); // 상단 장식 구 생성
            }

            CreateBox("FrontLowerBrace", new Vector3(0f, 0.38f, 0.82f), new Vector3(2.70f, 0.22f, 0.22f), _palette.Metal, chairRoot, Quaternion.identity, true); // 앞다리 하단 보강대 생성
            CreateBox("RearLowerBrace", new Vector3(0f, 0.38f, -0.82f), new Vector3(2.70f, 0.22f, 0.22f), _palette.Metal, chairRoot, Quaternion.identity, true); // 뒷다리 하단 보강대 생성
        }

        private void BuildOpponentFigure() // 의자에 앉은 익명 상대 실루엣 모델링
        {
            Transform figureRoot = CreateGroup("OpponentFigure", transform); // 상대 실루엣 그룹 생성
            figureRoot.position = Day41BattleRoomLayout.OpponentBodyRoot; // 상대 기준 위치 적용
            figureRoot.rotation = Quaternion.Euler(0f, 180f, 0f); // 플레이어 방향 회전 적용

            CreateCapsule("Torso", new Vector3(0f, 3.35f, 0f), new Vector3(1.45f, 1.45f, 1.00f), _palette.Opponent, figureRoot, Quaternion.identity, true); // 상체 실루엣 생성
            CreateBox("ChestPanel", new Vector3(0f, 3.35f, 0.66f), new Vector3(1.75f, 1.85f, 0.18f), _palette.TableDark, figureRoot, Quaternion.identity, true); // 상체 앞면 패널 생성
            CreateCylinder("Neck", new Vector3(0f, 4.68f, 0f), new Vector3(0.46f, 0.45f, 0.46f), _palette.Opponent, figureRoot, Quaternion.identity, true); // 목 실루엣 생성
            CreateSphere("Head", new Vector3(0f, 5.35f, 0f), new Vector3(1.15f, 1.28f, 1.08f), _palette.Opponent, figureRoot, true); // 얼굴 없는 머리 생성
            CreateBox("FacePlane", new Vector3(0f, 5.28f, 0.55f), new Vector3(0.72f, 0.58f, 0.08f), _palette.Seam, figureRoot, Quaternion.identity, true); // 얼굴 없는 전면 마스크 생성
            CreateBox("ShoulderBar", new Vector3(0f, 4.18f, 0f), new Vector3(3.35f, 0.30f, 0.72f), _palette.Opponent, figureRoot, Quaternion.identity, true); // 어깨 실루엣 생성

            BuildOpponentArm(figureRoot, -1f); // 상대 왼팔 생성
            BuildOpponentArm(figureRoot, 1f); // 상대 오른팔 생성

            CreateBox("Lap", new Vector3(0f, 1.55f, 0.35f), new Vector3(2.55f, 0.72f, 2.30f), _palette.Opponent, figureRoot, Quaternion.identity, true); // 앉은 하체 실루엣 생성
            CreateBox("Collar", new Vector3(0f, 4.33f, 0.57f), new Vector3(1.35f, 0.22f, 0.18f), _palette.Accent, figureRoot, Quaternion.identity, true); // 얇은 황동 목 장식 생성
        }

        private void BuildOpponentArm(Transform parent, float side) // 테이블 위로 뻗은 상대 팔 모델링
        {
            float x = side * 1.55f; // 팔 좌우 위치 계산
            Quaternion upperRotation = Quaternion.Euler(18f, 0f, side * -18f); // 상완 기울기 계산
            Quaternion foreRotation = Quaternion.Euler(72f, 0f, side * 7f); // 전완 기울기 계산

            CreateCapsule($"UpperArm_{side}", new Vector3(x, 3.52f, 0.42f), new Vector3(0.52f, 1.00f, 0.52f), _palette.Opponent, parent, upperRotation, true); // 상완 실루엣 생성
            CreateCapsule($"ForeArm_{side}", new Vector3(side * 1.72f, 2.35f, 1.85f), new Vector3(0.48f, 1.28f, 0.48f), _palette.Opponent, parent, foreRotation, true); // 전완 실루엣 생성
            CreateSphere($"Hand_{side}", new Vector3(side * 1.82f, 2.33f, 3.08f), new Vector3(0.62f, 0.40f, 0.82f), _palette.Opponent, parent, true); // 테이블 위 손 실루엣 생성
        }

        private void BuildCeilingFixture() // 중앙 원형 천장 조명 구조 모델링
        {
            Transform occluderRoot = transform.Find(Day41BattleRoomLayout.TopViewOccluderRootName); // 맨 위 시점 가림 구조 그룹 탐색
            if (occluderRoot == null) occluderRoot = CreateGroup(Day41BattleRoomLayout.TopViewOccluderRootName, transform); // 가림 구조 공통 그룹 생성
            Transform fixtureRoot = CreateGroup("CeilingFixture", occluderRoot); // 천장 조명 그룹 생성
            float ringY = 8.15f; // 조명 링 높이 설정
            float radius = 4.2f; // 조명 링 반경 설정
            int segments = 18; // 조명 링 세그먼트 수 설정

            for (int i = 0; i < segments; i++) // 조명 링 세그먼트 순회
            {
                float angle = i * Mathf.PI * 2f / segments; // 현재 세그먼트 각도 계산
                float degrees = angle * Mathf.Rad2Deg; // 회전용 각도 변환
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, ringY, Mathf.Sin(angle) * radius); // 링 세그먼트 위치 계산
                Quaternion rotation = Quaternion.Euler(0f, -degrees, 0f); // 링 접선 방향 회전 계산
                CreateBox($"FixtureRing_{i}", position, new Vector3(1.55f, 0.20f, 0.30f), _palette.Metal, fixtureRoot, rotation); // 원형 금속 링 생성
            }

            for (int i = 0; i < 4; i++) // 천장 행거 순회
            {
                float angle = i * 90f * Mathf.Deg2Rad; // 행거 각도 계산
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius * 0.72f, 10.0f, Mathf.Sin(angle) * radius * 0.72f); // 행거 위치 계산
                CreateCylinder($"FixtureHanger_{i}", position, new Vector3(0.13f, 1.85f, 0.13f), _palette.Metal, fixtureRoot); // 천장 행거 봉 생성
            }

            CreateCylinder("FixtureCore", new Vector3(0f, ringY, 0f), new Vector3(1.20f, 0.35f, 1.20f), _palette.Accent, fixtureRoot); // 중앙 황동 코어 생성
        }

        private void BuildOpponentBackdrop() // 상대 뒤쪽 거대 벽 포컬 구조 생성
        {
            Transform backdropRoot = CreateGroup("OpponentBackdrop", transform); // 상대 배경 구조 그룹 생성
            float z = Day41BattleRoomLayout.RoomDepth * 0.5f - 0.52f; // 북쪽 벽 안쪽 위치 계산
            float centerY = 3.3f; // 배경 구조 중심 높이 설정

            CreateBox("Backdrop_LeftPillar", new Vector3(-4.0f, centerY, z), new Vector3(0.62f, 9.8f, 0.42f), _palette.Metal, backdropRoot); // 왼쪽 거대 프레임 기둥 생성
            CreateBox("Backdrop_RightPillar", new Vector3(4.0f, centerY, z), new Vector3(0.62f, 9.8f, 0.42f), _palette.Metal, backdropRoot); // 오른쪽 거대 프레임 기둥 생성
            CreateBox("Backdrop_Top", new Vector3(0f, 8.05f, z), new Vector3(8.6f, 0.68f, 0.46f), _palette.Metal, backdropRoot); // 상단 거대 프레임 생성
            CreateBox("Backdrop_InnerTop", new Vector3(0f, 6.98f, z - 0.08f), new Vector3(6.9f, 0.28f, 0.30f), _palette.Accent, backdropRoot); // 내부 황동 라인 생성
            CreateCylinder("Backdrop_Medallion", new Vector3(0f, 7.72f, z - 0.25f), new Vector3(1.35f, 0.24f, 1.35f), _palette.Accent, backdropRoot, Quaternion.Euler(90f, 0f, 0f)); // 상대 뒤 중앙 문장 장식 생성
        }

        private void BuildLighting() // 보드·상대·방 깊이 조명 구성
        {
            Transform lightRoot = CreateGroup("Lighting", transform); // 조명 그룹 생성
            CreateSpotLight("TableKey", new Vector3(-4.2f, 7.8f, -4.0f), new Vector3(0f, 0f, 0f), new Color(1.0f, 0.83f, 0.62f), 8.0f, 62f, 28f, lightRoot); // 테이블 주광 생성
            CreateSpotLight("TableFill", new Vector3(4.5f, 7.2f, -1.5f), new Vector3(0f, 0f, 1.5f), new Color(0.66f, 0.76f, 1.0f), 6.0f, 68f, 30f, lightRoot); // 차가운 보조광 생성
            CreateSpotLight("OpponentRim", new Vector3(0f, 7.5f, 12.8f), new Vector3(0f, 2.0f, 8.5f), new Color(0.92f, 0.70f, 0.42f), 6.5f, 48f, 24f, lightRoot); // 상대 윤곽광 생성
            CreatePointLight("RoomAmbient_North", new Vector3(0f, 6.8f, 13.8f), new Color(0.48f, 0.55f, 0.68f), 3.2f, 16f, lightRoot); // 북쪽 공간광 생성
            CreatePointLight("RoomAmbient_South", new Vector3(0f, 6.5f, -13.8f), new Color(0.42f, 0.48f, 0.58f), 2.6f, 14f, lightRoot); // 남쪽 공간광 생성
        }

        private void ConfigureCamera() // 기존 자유 카메라를 앉은 시점으로 전환
        {
            Camera targetCamera = Camera.main; // 메인 카메라 우선 탐색
            if (targetCamera == null) targetCamera = Object.FindFirstObjectByType<Camera>(); // 씬 카메라 대체 탐색
            if (targetCamera == null) return; // 카메라 누락 시 종료

            TableCameraRig legacyRig = targetCamera.GetComponentInParent<TableCameraRig>(); // 기존 테이블 카메라 리그 탐색
            GameObject rigObject = legacyRig != null ? legacyRig.gameObject : targetCamera.gameObject; // 새 리그 부착 대상 선택
            Day41SeatedCameraRig seatedRig = rigObject.GetComponent<Day41SeatedCameraRig>(); // 기존 앉은 리그 탐색
            if (seatedRig == null) seatedRig = rigObject.AddComponent<Day41SeatedCameraRig>(); // 앉은 리그 신규 연결
            seatedRig.Configure(targetCamera); // 카메라 위치·렌즈 적용
        }

        private Transform CreateGroup(string groupName, Transform parent) // 빈 모델 그룹 생성
        {
            GameObject group = new GameObject(groupName); // 빈 그룹 오브젝트 생성
            group.transform.SetParent(parent, false); // 지정 부모 아래 배치
            return group.transform; // 그룹 Transform 반환
        }

        private GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material, Transform parent, Quaternion? rotation = null, bool local = false) // Cube 기반 모델 파트 생성
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube); // Cube 프리미티브 생성
            PreparePart(part, name, material, parent); // 공통 파트 설정 적용
            ApplyTransform(part.transform, position, scale, rotation ?? Quaternion.identity, local); // 위치·크기·회전 적용
            return part; // 생성 파트 반환
        }

        private GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent, Quaternion? rotation = null, bool local = false) // Cylinder 기반 모델 파트 생성
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // Cylinder 프리미티브 생성
            PreparePart(part, name, material, parent); // 공통 파트 설정 적용
            ApplyTransform(part.transform, position, scale, rotation ?? Quaternion.identity, local); // 위치·크기·회전 적용
            return part; // 생성 파트 반환
        }

        private GameObject CreateSphere(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool local = false) // Sphere 기반 모델 파트 생성
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere); // Sphere 프리미티브 생성
            PreparePart(part, name, material, parent); // 공통 파트 설정 적용
            ApplyTransform(part.transform, position, scale, Quaternion.identity, local); // 위치·크기 적용
            return part; // 생성 파트 반환
        }

        private GameObject CreateCapsule(string name, Vector3 position, Vector3 scale, Material material, Transform parent, Quaternion rotation, bool local = false) // Capsule 기반 모델 파트 생성
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Capsule); // Capsule 프리미티브 생성
            PreparePart(part, name, material, parent); // 공통 파트 설정 적용
            ApplyTransform(part.transform, position, scale, rotation, local); // 위치·크기·회전 적용
            return part; // 생성 파트 반환
        }

        private void PreparePart(GameObject part, string name, Material material, Transform parent) // 모델 파트 렌더·충돌 공통 설정
        {
            part.name = name; // 모델 파트 이름 지정
            part.transform.SetParent(parent, false); // 모델 그룹 아래 배치

            Collider collider = part.GetComponent<Collider>(); // 자동 생성 충돌체 조회
            if (collider != null) collider.enabled = false; // 보드 클릭 레이 가림 즉시 차단
            if (collider != null) Destroy(collider); // 장식 충돌체 제거 예약

            Renderer renderer = part.GetComponent<Renderer>(); // 모델 렌더러 조회
            if (renderer != null) renderer.sharedMaterial = material; // 공유 재질 적용
            if (renderer != null) renderer.shadowCastingMode = ShadowCastingMode.On; // 그림자 투사 활성화
            if (renderer != null) renderer.receiveShadows = true; // 그림자 수신 활성화
            _createdPartCount++; // 생성 모델 파트 수 증가
        }

        private static void ApplyTransform(Transform target, Vector3 position, Vector3 scale, Quaternion rotation, bool local) // 월드·로컬 Transform 공통 적용
        {
            if (local) // 로컬 좌표 적용 경로
            {
                target.localPosition = position; // 로컬 위치 적용
                target.localRotation = rotation; // 로컬 회전 적용
            }
            else // 월드 좌표 적용 경로
            {
                target.position = position; // 월드 위치 적용
                target.rotation = rotation; // 월드 회전 적용
            }

            target.localScale = scale; // 모델 파트 크기 적용
        }

        private void CreateSpotLight(string name, Vector3 position, Vector3 target, Color color, float intensity, float angle, float range, Transform parent) // 스포트라이트 생성
        {
            GameObject lightObject = new GameObject(name); // 조명 오브젝트 생성
            lightObject.transform.SetParent(parent, false); // 조명 그룹 아래 배치
            lightObject.transform.position = position; // 조명 위치 적용
            lightObject.transform.rotation = Quaternion.LookRotation((target - position).normalized, Vector3.up); // 조명 목표 방향 회전 적용
            Light light = lightObject.AddComponent<Light>(); // Light 컴포넌트 추가
            light.type = LightType.Spot; // 스포트라이트 유형 설정
            light.color = color; // 조명 색상 적용
            light.intensity = intensity; // 조명 강도 적용
            light.spotAngle = angle; // 스포트 각도 적용
            light.range = range; // 조명 범위 적용
            light.shadows = LightShadows.Soft; // 부드러운 그림자 활성화
        }

        private void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range, Transform parent) // 포인트라이트 생성
        {
            GameObject lightObject = new GameObject(name); // 조명 오브젝트 생성
            lightObject.transform.SetParent(parent, false); // 조명 그룹 아래 배치
            lightObject.transform.position = position; // 조명 위치 적용
            Light light = lightObject.AddComponent<Light>(); // Light 컴포넌트 추가
            light.type = LightType.Point; // 포인트라이트 유형 설정
            light.color = color; // 조명 색상 적용
            light.intensity = intensity; // 조명 강도 적용
            light.range = range; // 조명 범위 적용
            light.shadows = LightShadows.Soft; // 부드러운 그림자 활성화
        }

        private void OnDestroy() // 런타임 재질 수명 정리
        {
            for (int i = 0; i < _runtimeMaterials.Count; i++) // 생성 재질 순회
            {
                if (_runtimeMaterials[i] != null) Destroy(_runtimeMaterials[i]); // 런타임 재질 제거 예약
            }

            _runtimeMaterials.Clear(); // 재질 목록 초기화
        }
    }
}
