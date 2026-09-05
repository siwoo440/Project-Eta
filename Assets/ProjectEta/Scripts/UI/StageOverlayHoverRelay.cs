using UnityEngine; // MonoBehaviour 사용
using UnityEngine.EventSystems; // 포인터 진입·이탈 인터페이스 사용

namespace ProjectEta.UI
{
    public sealed class StageOverlayHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private System.Action _enterCallback; // 마우스 진입 콜백
        private System.Action _exitCallback; // 마우스 이탈 콜백

        public void Configure(System.Action enterCallback, System.Action exitCallback)
        {
            _enterCallback = enterCallback; // 진입 콜백 저장
            _exitCallback = exitCallback; // 이탈 콜백 저장
        }

        public void Clear()
        {
            _enterCallback = null; // 진입 콜백 제거
            _exitCallback = null; // 이탈 콜백 제거
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _enterCallback?.Invoke(); // 마우스 진입 콜백 실행
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _exitCallback?.Invoke(); // 마우스 이탈 콜백 실행
        }

        private void OnDisable()
        {
            _exitCallback?.Invoke(); // 카드 숨김 시 하단 설명 제거
        }
    }
}
