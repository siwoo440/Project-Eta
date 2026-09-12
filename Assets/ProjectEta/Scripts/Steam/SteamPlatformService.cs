using System; // Action·Exception 사용

namespace ProjectEta.Steam
{
    public sealed class SteamPlatformService
    {
        private readonly ISteamRuntimeBackend _runtimeBackend; // 공통 Steam Runtime Backend
        private readonly ISteamOverlayBackend _overlayBackend; // 선택적 Overlay Capability
        private bool _overlaySubscribed; // Overlay 이벤트 구독 상태

        public string BackendName => _runtimeBackend != null ? _runtimeBackend.BackendName : "None"; // 현재 Backend 이름
        public bool IsInitialized { get; private set; } // Steam Runtime 초기화 성공 여부
        public bool IsOverlayActive { get; private set; } // Steam Overlay 현재 활성 여부
        public bool CanAcceptGameInput => !IsOverlayActive; // Overlay 중 게임 입력 허용 여부
        public string LastError { get; private set; } = string.Empty; // 마지막 Steam 오류 메시지
        public bool IsOverlayAvailable => IsInitialized && _overlayBackend != null && SafeIsOverlayEnabled(); // Overlay 호출 가능 여부

        public event Action<bool> OverlayActiveChanged; // 게임 계층에 전달하는 Overlay 상태 이벤트

        public SteamPlatformService(ISteamRuntimeBackend runtimeBackend)
        {
            _runtimeBackend = runtimeBackend ?? new NullSteamBackend(); // null Backend를 안전한 fallback으로 치환
            _overlayBackend = _runtimeBackend as ISteamOverlayBackend; // Runtime Backend의 Overlay Capability 탐색
        }

        public bool Initialize()
        {
            if (IsInitialized) return true; // 중복 초기화 방지

            LastError = string.Empty; // 이전 오류 초기화

            try
            {
                if (!_runtimeBackend.Initialize())
                {
                    LastError = $"{BackendName} 초기화 실패"; // 비예외 초기화 실패 기록
                    return false; // 게임 자체는 계속 실행
                }

                IsInitialized = true; // 초기화 성공 상태 저장
                SubscribeOverlay(); // Overlay Capability 이벤트 연결
                return true; // Steam 초기화 성공 반환
            }
            catch (Exception exception)
            {
                LastError = exception.Message; // 예상하지 못한 Backend 오류 기록
                IsInitialized = false; // Steam 비활성 상태 유지
                return false; // 게임 실행은 차단하지 않음
            }
        }

        public void Tick()
        {
            if (!IsInitialized) return; // Steam 미초기화 상태 Callback 생략

            try
            {
                _runtimeBackend.RunCallbacks(); // Steam Callback 처리
            }
            catch (Exception exception)
            {
                LastError = exception.Message; // Callback 오류 진단 정보 저장
                Shutdown(); // 불안정 Backend를 안전하게 종료
            }
        }

        public bool OpenOverlay(SteamOverlayPage page)
        {
            if (!IsOverlayAvailable) return false; // 초기화·Overlay 사용 가능 상태 검증

            try
            {
                return _overlayBackend.OpenOverlay(page); // 실제 Overlay 요청 전달
            }
            catch (Exception exception)
            {
                LastError = exception.Message; // Overlay 호출 오류 저장
                return false; // 게임 흐름 유지
            }
        }

        public bool TryGetCapability<TCapability>(out TCapability capability)
            where TCapability : class
        {
            capability = _runtimeBackend as TCapability; // 현재 Backend에서 선택 Capability 조회
            return capability != null; // Capability 지원 여부 반환
        }

        public void Shutdown()
        {
            if (!IsInitialized)
            {
                ResetOverlayState(); // 중복 종료에서도 입력 상태 안전 복구
                return; // Backend 중복 종료 차단
            }

            UnsubscribeOverlay(); // Callback 객체 종료 전 이벤트 연결 해제

            try
            {
                _runtimeBackend.Shutdown(); // 실제 Steam Runtime 종료
            }
            catch (Exception exception)
            {
                LastError = exception.Message; // 종료 오류 기록
            }

            IsInitialized = false; // Runtime 종료 상태 저장
            ResetOverlayState(); // Overlay·게임 입력 상태 복구
        }

        private bool SafeIsOverlayEnabled()
        {
            try
            {
                return _overlayBackend.IsOverlayEnabled(); // Backend Overlay 가능 여부 조회
            }
            catch (Exception exception)
            {
                LastError = exception.Message; // 조회 오류 기록
                return false; // 오류 시 Overlay 비활성 처리
            }
        }

        private void SubscribeOverlay()
        {
            if (_overlayBackend == null || _overlaySubscribed) return; // Capability 누락·중복 구독 차단
            _overlayBackend.OverlayActiveChanged += HandleOverlayActiveChanged; // Overlay 상태 이벤트 구독
            _overlaySubscribed = true; // 구독 상태 기록
        }

        private void UnsubscribeOverlay()
        {
            if (_overlayBackend == null || !_overlaySubscribed) return; // 구독 없음 차단
            _overlayBackend.OverlayActiveChanged -= HandleOverlayActiveChanged; // Overlay 상태 이벤트 해제
            _overlaySubscribed = false; // 구독 상태 해제
        }

        private void HandleOverlayActiveChanged(bool active)
        {
            if (IsOverlayActive == active) return; // 동일 상태 중복 알림 차단
            IsOverlayActive = active; // Overlay 현재 상태 갱신
            OverlayActiveChanged?.Invoke(active); // 게임 계층에 상태 변경 전달
        }

        private void ResetOverlayState()
        {
            if (!IsOverlayActive) return; // 이미 비활성 상태면 생략
            IsOverlayActive = false; // Overlay 비활성으로 복구
            OverlayActiveChanged?.Invoke(false); // 게임 입력 복구 알림
        }
    }
}
