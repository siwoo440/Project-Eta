using System.Collections.Generic; // 생성 재질 소유 목록
using UnityEngine; // 실제 조사 모델과 충돌

namespace ProjectEta.Environment // 방 탐색 배치
{ // 영역 시작
    public static class RoomInvestigationLayout // 기존 방 안의 기초 조사 네 곳
    { // 타입 시작
        public static readonly IReadOnlyList<RoomInvestigationDefinition> Definitions = System.Array.AsReadOnly(new[] // 임시 문구와 고정 공간 데이터
        { // 목록 시작
            new RoomInvestigationDefinition("room_records", "기록대", "낮은 기록대 위에 종이와 책이 놓여 있다.\n기록대와 중앙 테이블 사이로 돌아갈 수 있는 통로가 보인다.", new Vector3(-12f, Day41BattleRoomLayout.FloorY, -5f), new Vector3(3.2f, 3.8f, 1.6f), Vector3.right), // 서쪽 기록대
            new RoomInvestigationDefinition("room_shelf", "기물 진열대", "선반에 작은 기물 모양의 장식이 놓여 있다.\n장식은 선반에 고정되어 있어 가져갈 수 없다.", new Vector3(12.3f, Day41BattleRoomLayout.FloorY, 0f), new Vector3(3.5f, 6f, 1.5f), Vector3.left), // 동쪽 진열대
            new RoomInvestigationDefinition("room_wall", "벽의 금속 장식", "각진 금속 장식이 벽 앞에 세워져 있다.\n장식 주변의 바닥을 따라 방 안으로 돌아갈 수 있다.", new Vector3(9.5f, Day41BattleRoomLayout.FloorY, 16.5f), new Vector3(3.4f, 6f, 0.6f), Vector3.back), // 북쪽 벽 장식
            new RoomInvestigationDefinition("room_door", "잠긴 출입문", "문을 밀어 보았지만 열리지 않는다.\n지금은 방 안을 살펴보고 테이블로 돌아가야 한다.", new Vector3(9.5f, Day41BattleRoomLayout.FloorY, -17.6f), new Vector3(4f, 7f, 0.6f), Vector3.forward) // 남쪽 닫힌 문
        }); // 목록 종료
        public static RoomInvestigationTarget[] Build(Transform parent, List<Material> ownedMaterials) // 원본 씬을 수정하지 않는 자동 배치
        { // 메서드 시작
            Material wood = Material("Room104Wood", new Color(0.18f, 0.12f, 0.07f), ownedMaterials); // 목재 재질
            Material metal = Material("Room104Metal", new Color(0.07f, 0.09f, 0.1f), ownedMaterials); // 금속 재질
            Material gold = Material("Room104Gold", new Color(0.95f, 0.67f, 0.15f), ownedMaterials); // 기존 UI 계열 강조색
            Material paper = Material("Room104Paper", new Color(0.76f, 0.71f, 0.54f), ownedMaterials); // 종이 재질
            Material focus = Material("Room104Focus", new Color(1f, 0.78f, 0.18f), ownedMaterials, true); // 어두운 방에서도 밝은 조사 테두리
            var targets = new List<RoomInvestigationTarget>(); // 실제 설치 목록
            foreach (RoomInvestigationDefinition definition in Definitions) // 고정 네 곳 순회
            { // 반복 시작
                var root = new GameObject(definition.Id); // 대상별 루트
                root.layer = 2; // 보드 선택 레이 제외
                root.transform.SetParent(parent, false); // 별도 방 루트 수명
                root.transform.position = definition.Position; // 바닥 위치
                root.transform.rotation = Quaternion.LookRotation(definition.Facing); // 방 안쪽 전면
                BoxCollider collision = root.AddComponent<BoxCollider>(); // 실제 걷기 장애물
                collision.center = Vector3.up * (definition.Size.y * 0.5f); // 바닥 기준 몸체
                collision.size = definition.Size; // 시각 모델과 일치한 통과 차단
                if (definition.Id == "room_shelf") // 선반의 열린 시각 모델
                { // 조건 시작
                    Box(root.transform, "LeftPost", new Vector3(-1.6f, 3f, 0f), new Vector3(0.3f, 6f, 1.5f), wood); // 왼쪽 기둥
                    Box(root.transform, "RightPost", new Vector3(1.6f, 3f, 0f), new Vector3(0.3f, 6f, 1.5f), wood); // 오른쪽 기둥
                    for (int level = 0; level < 3; level++) Box(root.transform, "Shelf", new Vector3(0f, 0.3f + level * 2.6f, 0f), new Vector3(3.5f, 0.25f, 1.5f), wood); // 가로 선반
                    Box(root.transform, "Back", new Vector3(0f, 3f, -0.65f), new Vector3(3.5f, 6f, 0.2f), metal); // 뒷판
                    for (int index = 0; index < 2; index++) // 기물 모양 장식
                    { // 반복 시작
                        float x = index == 0 ? -0.7f : 0.7f; // 장식 간격
                        Box(root.transform, "DisplayBase", new Vector3(x, 3.2f, 0.1f), new Vector3(0.8f, 0.3f, 0.8f), gold); // 장식 받침
                        Box(root.transform, "DisplayStem", new Vector3(x, 3.75f, 0.1f), new Vector3(0.35f, 0.8f, 0.35f), gold); // 장식 몸통
                        Box(root.transform, "DisplayTop", new Vector3(x, 4.3f, 0.1f), new Vector3(0.6f, 0.35f, 0.6f), paper); // 장식 머리
                    } // 반복 종료
                } // 조건 종료
                else // 기록대·문·벽 장식
                { // 조건 시작
                    Box(root.transform, "Body", Vector3.up * (definition.Size.y * 0.5f), definition.Size, definition.Id == "room_records" ? wood : metal); // 주 모델
                    if (definition.Id == "room_records") Box(root.transform, "Papers", new Vector3(0f, 3.95f, 0f), new Vector3(2.2f, 0.25f, 1.1f), paper); // 기록대 위 종이
                    else // 벽과 문 전면 장식
                    { // 조건 시작
                        Box(root.transform, "FrontBar", new Vector3(0f, 4.1f, 0.34f), new Vector3(2.5f, 0.15f, 0.12f), gold); // 가로 금속 장식
                        Box(root.transform, "FrontStem", new Vector3(0f, 4.1f, 0.34f), new Vector3(0.15f, 2.5f, 0.12f), gold); // 세로 금속 장식
                    } // 조건 종료
                } // 조건 종료
                var marker = new GameObject("FocusBorder"); // 대상별 강조 묶음
                marker.transform.SetParent(root.transform, false); // 대상 전면에 연결
                float y = Mathf.Min(definition.Size.y - 0.25f, 4.4f); // 조사 시선과 같은 강조 높이
                float z = definition.Size.z * 0.5f + 0.06f; // 앞면과 겹치지 않는 깊이
                Box(marker.transform, "Upper", new Vector3(0f, y + 0.7f, z), new Vector3(definition.Size.x + 0.2f, 0.07f, 0.07f), focus); // 상단 테두리
                Box(marker.transform, "Lower", new Vector3(0f, y - 0.7f, z), new Vector3(definition.Size.x + 0.2f, 0.07f, 0.07f), focus); // 하단 테두리
                Box(marker.transform, "Left", new Vector3(-definition.Size.x * 0.5f - 0.06f, y, z), new Vector3(0.07f, 1.4f, 0.07f), focus); // 왼쪽 테두리
                Box(marker.transform, "Right", new Vector3(definition.Size.x * 0.5f + 0.06f, y, z), new Vector3(0.07f, 1.4f, 0.07f), focus); // 오른쪽 테두리
                RoomInvestigationTarget target = root.AddComponent<RoomInvestigationTarget>(); // 실제 조사 입력 대상
                target.Configure(definition, marker); // 데이터와 시각 연결
                targets.Add(target); // 설치 결과 보관
            } // 반복 종료
            return targets.ToArray(); // 외부 변경과 분리한 목록
        } // 메서드 종료
        public static string AreaName(Vector3 feet) // 현재 통로 안내
        { // 메서드 시작
            if (RoomWalkingController.IsWithinChairReach(feet)) return "플레이어 의자"; // 좌석 접근 우선
            if (feet.z < -14f) return "출입문 앞 통로"; // 남쪽 가장자리
            if (feet.x < -7f) return "서쪽 기록대 통로"; // 서쪽 이동 공간
            if (feet.x > 7f) return "동쪽 진열대 통로"; // 동쪽 이동 공간
            return feet.z > 7f ? "상대 좌석 뒤 통로" : "중앙 테이블 주변"; // 북쪽과 테이블 주변
        } // 메서드 종료
        private static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material) // 시각 전용 기본 도형
        { // 메서드 시작
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube); // 각진 기초 모델
            part.name = name; // 역할 이름
            part.layer = 2; // 보드 레이 제외
            part.transform.SetParent(parent, false); // 대상 상대 배치
            part.transform.localPosition = position; // 실제 구성 위치
            part.transform.localScale = size; // 실제 구성 크기
            part.GetComponent<Renderer>().sharedMaterial = material; // 소유 재질 공유
            Collider collider = part.GetComponent<Collider>(); // 자동 생성 충돌
            collider.enabled = false; // 루트 충돌체만 사용
            if (Application.isPlaying) Object.Destroy(collider); // 런타임 시각 충돌 제거
            else Object.DestroyImmediate(collider); // 에디터 렌더 시 정리
        } // 메서드 종료
        private static Material Material(string name, Color color, List<Material> owned, bool unlit = false) // 생성 재질의 명시적 소유
        { // 메서드 시작
            Shader shader = unlit ? (Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default")) : (Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default")); // 강조는 조명 독립·모델은 기존 조명
            var material = new Material(shader); // 전용 런타임 재질
            material.name = name; // 정리용 식별 이름
            material.color = color; // 실제 재질색
            owned.Add(material); // 담당자 파괴 시 함께 해제
            return material; // 모델 공유 재질
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
