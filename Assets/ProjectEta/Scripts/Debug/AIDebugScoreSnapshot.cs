using System.Collections.Generic; // IReadOnlyList<T>와 List<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // AI 점수와 디버그 로그 데이터를 기존 AI 네임스페이스에 함께 배치
{
    public sealed class AIDebugScoreEntry // F1 디버그 창 한 줄에 표시할 AI 행동 후보 점수 정보
    {
        public PieceRuntimeState Actor { get; } // 행동을 수행하려는 적 기물
        public Vector2Int Origin { get; } // 행동 시작 좌표
        public Vector2Int Target { get; } // 이동 또는 공격 목표 좌표
        public AIActionType ActionType { get; } // 이동 또는 공격 행동 종류
        public EnemyAIBasicRole Role { get; } // 34일차 기본 역할 분류
        public int BaseScore { get; } // 33일차 공통 AI 점수
        public int RoleBonus { get; } // 34일차 역할별 추가 점수
        public int ThreatScore { get; } // 35일차 플레이어 위협 맵에 따른 안전/위험 점수
        public int SpecialBonus { get; } // 35일차 특수 기물 활용 보너스
        public int FinalScore { get; } // Base + Role + Threat + Special 최종 점수
        public bool IsSelected { get; } // 실제 AI가 현재 선택할 최우선 행동인지 여부

        public AIDebugScoreEntry( // 디버그 로그 한 줄의 모든 값을 한 번에 받는 생성자
            PieceRuntimeState actor, // 행동 주체
            Vector2Int origin, // 시작 좌표
            Vector2Int target, // 목표 좌표
            AIActionType actionType, // 행동 종류
            EnemyAIBasicRole role, // 기본 AI 역할
            int baseScore, // 공통 점수
            int roleBonus, // 역할 보너스
            int threatScore, // 위협 점수
            int specialBonus, // 특수 점수
            int finalScore, // 최종 점수
            bool isSelected) // 선택 여부
        {
            Actor = actor; // 행동 주체 저장
            Origin = origin; // 시작 좌표 저장
            Target = target; // 목표 좌표 저장
            ActionType = actionType; // 행동 종류 저장
            Role = role; // 역할 저장
            BaseScore = baseScore; // 공통 점수 저장
            RoleBonus = roleBonus; // 역할 점수 저장
            ThreatScore = threatScore; // 위협 점수 저장
            SpecialBonus = specialBonus; // 특수 점수 저장
            FinalScore = finalScore; // 최종 점수 저장
            IsSelected = isSelected; // 선택 상태 저장
        }
    }

    public sealed class AIDebugScoreSnapshot // 특정 시점 보드 전체의 AI 점수 로그 스냅샷
    {
        private readonly List<AIDebugScoreEntry> _entries; // 내부에서 보관하는 점수 로그 목록

        public IReadOnlyList<AIDebugScoreEntry> Entries => _entries; // 외부에는 읽기 전용 목록으로 제공
        public AIDebugScoreEntry SelectedEntry { get; } // 현재 AI가 실제로 선택할 행동 로그
        public int CandidateCount => _entries.Count; // 화면 상단에 표시할 총 후보 수

        public AIDebugScoreSnapshot(List<AIDebugScoreEntry> entries, AIDebugScoreEntry selectedEntry) // 완성된 로그 목록을 받는 생성자
        {
            _entries = entries ?? new List<AIDebugScoreEntry>(); // null이 들어와도 빈 목록으로 안전하게 보정
            SelectedEntry = selectedEntry; // 현재 선택 행동 저장
        }

        public static AIDebugScoreSnapshot Empty() // 보드가 없거나 Battle 씬이 아닐 때 사용할 빈 스냅샷 생성
        {
            return new AIDebugScoreSnapshot(new List<AIDebugScoreEntry>(), null); // 후보와 선택 행동이 없는 상태 반환
        }
    }
}
