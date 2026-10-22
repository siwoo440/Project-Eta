using UnityEngine; // 조사 위치와 크기

namespace ProjectEta.Environment // 방 조사 데이터
{ // 영역 시작
    public sealed class RoomInvestigationDefinition // 현재 방의 고정 조사 대상
    { // 타입 시작
        public readonly string Id; // 중복 없는 조사 ID
        public readonly string Title; // 화면 이름
        public readonly string Description; // 현재 임시 조사 문구
        public readonly Vector3 Position; // 방 안 바닥 위치
        public readonly Vector3 Size; // 실제 충돌 크기
        public readonly Vector3 Facing; // 실내 방향 전면
        public readonly float Reach = 4.5f; // 눈 위치 기준 조사 거리
        public RoomInvestigationDefinition(string id, string title, string description, Vector3 position, Vector3 size, Vector3 facing) // 고정 데이터 연결
        { // 생성자 시작
            Id = id; // 고정 ID
            Title = title; // 표시 제목
            Description = description; // 조사 문구
            Position = position; // 바닥 기준 위치
            Size = size; // 물리 크기
            Facing = facing; // 전면 방향
        } // 생성자 종료
    } // 타입 종료
} // 영역 종료
