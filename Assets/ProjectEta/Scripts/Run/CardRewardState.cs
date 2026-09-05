using System.Collections.Generic; // List<T>·IReadOnlyList<T> 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run // 런 카드 보상 상태 네임스페이스
{
    public enum CardRewardSource // 카드 보상 발생 경로
    {
        BattleVictory = 0, // 전투 스테이지 승리 직후 보상
        RewardNode = 1 // 경로 지도의 Reward 노드 보상
    }

    public sealed class CardRewardState // 현재 3개 후보와 1개 선택 결과를 보관하는 런타임 임시 상태
    {
        private readonly List<PieceDefinition> _candidates = new List<PieceDefinition>(); // 현재 보상 후보 목록

        public IReadOnlyList<PieceDefinition> Candidates => _candidates; // 후보 읽기 전용 공개
        public PieceDefinition SelectedCard { get; private set; } // 선택된 카드 정의
        public CardRewardSource Source { get; private set; } // 현재 보상 발생 경로
        public bool IsActive { get; private set; } // 보상 선택 진행 여부
        public bool HasSelection => SelectedCard != null; // 선택 완료 여부

        public void Begin(IReadOnlyList<PieceDefinition> candidates, CardRewardSource source) // 새 카드 보상 선택 시작
        {
            Clear(); // 이전 보상 상태 제거
            Source = source; // 보상 발생 경로 기록

            if (candidates != null) // 후보 목록 존재 확인
            {
                for (int i = 0; i < candidates.Count; i++) // 후보 순회
                {
                    PieceDefinition definition = candidates[i]; // 현재 후보 조회
                    if (definition != null) _candidates.Add(definition); // 유효 후보 등록
                }
            }

            IsActive = _candidates.Count > 0; // 후보가 있을 때만 선택 활성화
        }

        public bool Contains(PieceDefinition definition) // 현재 제시된 후보인지 확인
        {
            if (definition == null) return false; // 빈 카드 차단

            for (int i = 0; i < _candidates.Count; i++) // 후보 목록 순회
            {
                if (_candidates[i] == definition) return true; // 동일 정의 참조 확인
                if (_candidates[i] != null && _candidates[i].PieceId == definition.PieceId) return true; // 동일 PieceId 후보 확인
            }

            return false; // 후보 외 카드 반환
        }

        public bool TrySelect(PieceDefinition definition) // 후보 중 카드 1장 선택
        {
            if (!IsActive || HasSelection || !Contains(definition)) return false; // 비활성·중복·후보 외 선택 차단
            SelectedCard = definition; // 최초 선택 카드 기록
            return true; // 선택 성공 반환
        }

        public void Clear() // 현재 카드 보상 상태 초기화
        {
            _candidates.Clear(); // 후보 목록 제거
            SelectedCard = null; // 선택 결과 제거
            Source = CardRewardSource.BattleVictory; // 기본 보상 경로 복구
            IsActive = false; // 선택 비활성화
        }
    }
}
