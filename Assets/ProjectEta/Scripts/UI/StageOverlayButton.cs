using UnityEngine.EventSystems; // 기본 Submit 이벤트
using UnityEngine.UI; // 포인터 버튼 동작

namespace ProjectEta.UI // 선택 화면 입력 영역
{ // 영역 시작
    public sealed class StageOverlayButton : Button // 키 확정은 화면 담당자가 한 번 처리
    { // 타입 시작
        public override void OnSubmit(BaseEventData eventData) // 기본 모듈의 중복 Enter 차단
        { // 메서드 시작
            // 키보드 포커스와 확정은 StageBoardOverlayUI 소유
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
