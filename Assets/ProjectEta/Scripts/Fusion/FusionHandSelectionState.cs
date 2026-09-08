using System.Collections.Generic; // 선택 슬롯 목록 사용

namespace ProjectEta.Fusion // 합성 관련 타입 네임스페이스
{
    public sealed class FusionHandSelectionState // 손패의 실제 슬롯 인덱스로 합성 재료 두 장을 구분하는 상태
    {
        private readonly List<int> _indices = new List<int>(2); // 선택된 손패 슬롯 인덱스 목록

        public IReadOnlyList<int> Indices => _indices; // 외부 읽기 전용 선택 목록
        public int Count => _indices.Count; // 현재 선택된 재료 수

        public bool Contains(int handIndex) // 특정 손패 슬롯 선택 여부 확인
        {
            return _indices.Contains(handIndex); // 슬롯 인덱스 기준 선택 여부 반환
        }

        public bool Toggle(int handIndex) // 손패 슬롯 선택 또는 선택 해제
        {
            if (handIndex < 0) // 잘못된 슬롯 인덱스 확인
            {
                return false; // 음수 슬롯 거부
            }

            int selectedIndex = _indices.IndexOf(handIndex); // 기존 선택 위치 탐색

            if (selectedIndex >= 0) // 이미 선택된 슬롯 확인
            {
                _indices.RemoveAt(selectedIndex); // 선택된 슬롯 제거
                return true; // 선택 해제 성공 반환
            }

            if (_indices.Count >= 2) // 재료 두 장 선택 완료 여부 확인
            {
                return false; // 세 번째 재료 선택 거부
            }

            _indices.Add(handIndex); // 새 손패 슬롯 선택
            return true; // 선택 성공 반환
        }

        public bool RemoveSelectionSlot(int selectionSlot) // 합성 패널의 A 또는 B 슬롯 선택 해제
        {
            if (selectionSlot < 0 || selectionSlot >= _indices.Count) // 선택 슬롯 범위 확인
            {
                return false; // 빈 선택 슬롯 거부
            }

            _indices.RemoveAt(selectionSlot); // 지정 재료 선택 제거
            return true; // 선택 해제 성공 반환
        }

        public void Prune(int handCount) // 손패 변경 뒤 유효하지 않은 슬롯 제거
        {
            for (int i = _indices.Count - 1; i >= 0; i--) // 뒤에서부터 선택 슬롯 검사
            {
                int handIndex = _indices[i]; // 현재 선택 손패 인덱스 조회

                if (handIndex < 0 || handIndex >= handCount) // 현재 손패 범위를 벗어난 선택 확인
                {
                    _indices.RemoveAt(i); // 무효 선택 제거
                }
            }
        }

        public void Clear() // 모든 합성 재료 선택 초기화
        {
            _indices.Clear(); // 선택 슬롯 목록 비우기
        }
    }
}
