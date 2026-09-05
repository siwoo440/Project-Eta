using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 2x2 이상 보스 전투 관련 타입을 모아두는 네임스페이스
{
    public enum BossActionType // 38일차 보스 기본 전투에서 사용하는 행동 종류
    {
        Move = 0, // 2x2 점유 영역 전체를 상하좌우 한 칸 이동
        Attack = 1 // 현재 점유 영역 외곽에 인접한 플레이어 기물 1기를 공격
    }

    public sealed class BossActionCandidate // 보스가 현재 EnemyTurn에 선택할 수 있는 행동 후보 하나를 표현하는 데이터
    {
        public PieceRuntimeState Actor { get; } // 행동을 수행할 보스 런타임 기물
        public BossActionType ActionType { get; } // 이동 또는 공격 종류
        public Vector2Int Origin { get; } // 행동 시작 Anchor 좌표
        public Vector2Int Target { get; } // 이동 목표 Anchor 또는 공격 대상 좌표
        public PieceRuntimeState TargetPiece { get; } // 공격 행동일 때 실제 플레이어 대상
        public int Score { get; } // 일반 AI 행동과 비교할 수 있는 최종 보스 행동 점수

        public BossActionCandidate(PieceRuntimeState actor, BossActionType actionType, Vector2Int origin, Vector2Int target, PieceRuntimeState targetPiece, int score) // 모든 후보 값을 받는 생성자
        {
            Actor = actor; // 행동 주체 저장
            ActionType = actionType; // 행동 종류 저장
            Origin = origin; // 시작 좌표 저장
            Target = target; // 목표 좌표 저장
            TargetPiece = targetPiece; // 공격 대상 저장
            Score = score; // 평가 점수 저장
        }

        public override string ToString() // Unity Console에서 후보를 읽기 쉽게 표시하는 메서드
        {
            string actorName = Actor?.Definition?.DisplayName ?? "Unknown Boss"; // 보스 표시 이름을 안전하게 읽음
            string targetName = TargetPiece?.Definition?.DisplayName ?? "-"; // 공격 대상이 있으면 이름 표시
            return $"{actorName} {ActionType} {Origin}->{Target} Target={targetName} Score={Score}"; // 디버그용 한 줄 문자열 반환
        }
    }
}
