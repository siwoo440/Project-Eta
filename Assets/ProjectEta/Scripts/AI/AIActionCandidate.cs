using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class AIActionCandidate // 적 AI가 평가하는 행동 후보 하나를 표현하는 순수 데이터 클래스
    {
        public PieceRuntimeState Actor { get; } // 이 행동을 실행할 적 기물
        public Vector2Int Origin { get; } // 행동 시작 좌표
        public Vector2Int Target { get; } // 이동 또는 공격 목표 좌표
        public AIActionType ActionType { get; } // 이동인지 공격인지 나타내는 행동 종류
        public PieceRuntimeState TargetPiece { get; } // 공격 행동일 때의 대상 기물이며 이동이면 null
        public int Score { get; } // 공통 평가기가 계산한 최종 행동 점수

        public AIActionCandidate(PieceRuntimeState actor, Vector2Int origin, Vector2Int target, AIActionType actionType, PieceRuntimeState targetPiece, int score) // 후보 데이터를 한 번에 구성하는 생성자
        {
            Actor = actor; // 행동 주체 저장
            Origin = origin; // 시작 좌표 저장
            Target = target; // 목표 좌표 저장
            ActionType = actionType; // 행동 종류 저장
            TargetPiece = targetPiece; // 공격 대상 저장
            Score = score; // 평가 점수 저장
        }

        public override string ToString() // 개발 로그에서 후보를 쉽게 확인하기 위한 문자열 표현
        {
            string actorName = Actor?.Definition != null ? Actor.Definition.DisplayName : "Unknown"; // 안전한 행동 주체 이름 계산
            string targetName = TargetPiece?.Definition != null ? TargetPiece.Definition.DisplayName : "-"; // 공격 대상이 있으면 이름 표시
            return $"{ActionType} {actorName} {Origin}->{Target} Target={targetName} Score={Score}"; // 핵심 후보 정보를 한 줄로 반환
        }
    }
}
