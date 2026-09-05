using UnityEngine; // Vector2·Vector3·Mathf 기능

namespace ProjectEta.Environment // 전투 공간 프레젠테이션 네임스페이스
{
    public static class Day41BattleRoomLayout // 41일차 거대 방·테이블 공통 치수
    {
        public const string RootName = "Day41BattleRoom"; // 런타임 환경 루트 이름
        public const string BattleSceneName = "Battle"; // 적용 대상 씬 이름
        public const string TopViewOccluderRootName = "TopViewOccluders"; // 맨 위 시점에서 숨길 천장 구조 그룹 이름
        public const float RoomWidth = 34f; // 방 전체 가로 크기
        public const float RoomDepth = 38f; // 방 전체 깊이 크기
        public const float FloorY = -2.4f; // 방 바닥 높이
        public const float CeilingY = 11.8f; // 방 천장 높이
        public const float WallThickness = 0.45f; // 방 벽 두께
        public const float FloorPanelSize = 3f; // 바닥 패널 기준 크기
        public const float WallPanelSize = 3f; // 벽 패널 기준 크기
        public const float PanelGap = 0.08f; // 패널 사이 틈 크기
        public const float BoardPlayableSize = 10f; // 기존 10×10 보드 외곽 크기
        public const float BoardFrameInnerSize = 10.35f; // 보드 프레임 내부 크기
        public const float BoardFrameOuterSize = 11.45f; // 보드 프레임 외곽 크기
        public const float TableTopY = -0.28f; // 테이블 상판 중심 높이
        public const float TableTopThickness = 0.44f; // 테이블 상판 두께
        public const float LegacyCameraDistance = 13f; // 기존 두 카메라 보드 중심 거리
        public const float TopCameraPitch = 60f; // 기존 맨 위 시점 각도
        public const float BasicCameraPitch = 45f; // 기존 기본 시점 각도
        public const float SeatedCameraFieldOfView = 60f; // 상대 시점 기본 시야각
        public const float SeatedCameraMinFieldOfView = 52f; // 상대 시점 최소 줌
        public const float SeatedCameraMaxFieldOfView = 66f; // 상대 시점 최대 줌
        public const float OpponentYawHalfRange = 90f; // 좌우 총 180도 회전 반경
        public const float OpponentPitchHalfRange = 89f; // 상하 뒤집힘 방지 회전 반경
        public const float OpponentLookSensitivity = 0.12f; // 상대 시점 마우스 회전 감도
        public static readonly Vector2 TableTopSize = new Vector2(12.7f, 12.7f); // 테이블 상판 크기
        public static readonly Vector3 PlayerChairRoot = new Vector3(0f, FloorY, -10.7f); // 플레이어 의자 기준점
        public static readonly Vector3 OpponentChairRoot = new Vector3(0f, FloorY, 10.7f); // 상대 의자 기준점
        public static readonly Vector3 OpponentBodyRoot = new Vector3(0f, FloorY, 8.7f); // 상대 실루엣 기준점
        public static readonly Vector3 BoardCenter = Vector3.zero; // 기존 카메라 보드 중심 목표
        public static readonly Vector3 TopCameraPosition = CalculateLegacyCameraPosition(TopCameraPitch); // 기존 맨 위 시점 위치
        public static readonly Vector3 BasicCameraPosition = CalculateLegacyCameraPosition(BasicCameraPitch); // 기존 기본 시점 위치
        public static readonly Vector3 SeatedCameraPosition = new Vector3(0f, 2.35f, -10.25f); // 플레이어 눈높이 카메라 위치
        public static readonly Vector3 OpponentLookTarget = new Vector3(0f, 2.75f, 8.7f); // 상대 상체 중심 시선 목표

        public static bool IsBattleScene(string sceneName) // Battle 씬 적용 여부 판정
        {
            return string.Equals(sceneName, BattleSceneName, System.StringComparison.Ordinal); // 정확한 씬 이름 비교
        }

        public static bool IsInsideRoom(Vector3 position) // 방 내부 좌표 판정
        {
            float halfWidth = RoomWidth * 0.5f; // 방 가로 반경 계산
            float halfDepth = RoomDepth * 0.5f; // 방 깊이 반경 계산
            bool insideHorizontal = Mathf.Abs(position.x) < halfWidth && Mathf.Abs(position.z) < halfDepth; // 수평 범위 판정
            bool insideVertical = position.y >= FloorY && position.y <= CeilingY; // 수직 범위 판정
            return insideHorizontal && insideVertical; // 전체 범위 결과 반환
        }

        public static Vector3 CalculateLegacyCameraPosition(float pitchDegrees) // 기존 TableCameraRig 위치 계산
        {
            float pitchRadians = pitchDegrees * Mathf.Deg2Rad; // 각도를 라디안으로 변환
            float height = Mathf.Sin(pitchRadians) * LegacyCameraDistance; // 높이 성분 계산
            float depth = Mathf.Cos(pitchRadians) * LegacyCameraDistance; // 깊이 성분 계산
            return new Vector3(0f, height, -depth); // 보드 남쪽 카메라 위치 반환
        }

        public static float GetWallPanelDepthOffset(int column, int row, int wallIndex) // 벽 패널 미세 돌출값 계산
        {
            int hash = Mathf.Abs(column * 17 + row * 31 + wallIndex * 13); // 위치 기반 결정적 해시 계산
            if (hash % 11 == 0) return 0.22f; // 강한 돌출 패널 선택
            if (hash % 7 == 0) return 0.12f; // 중간 돌출 패널 선택
            if (hash % 5 == 0) return -0.06f; // 미세 함몰 패널 선택
            return 0f; // 기본 평면 패널 반환
        }
    }
}
