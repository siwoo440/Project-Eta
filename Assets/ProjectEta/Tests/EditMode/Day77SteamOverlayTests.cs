using System; // Action 사용
using NUnit.Framework; // NUnit 테스트 사용
using ProjectEta.Steam; // Steam 플랫폼 서비스 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day77SteamOverlayTests
    {
        [Test]
        public void InitializeFailure_KeepsGameplayAvailable()
        {
            var backend = new FakeSteamBackend { InitializeResult = false }; // 초기화 실패 Backend 구성
            var service = new SteamPlatformService(backend); // 테스트 서비스 생성

            bool initialized = service.Initialize(); // Steam 초기화 시도

            Assert.IsFalse(initialized); // 초기화 실패 반환 검증
            Assert.IsFalse(service.IsInitialized); // 서비스 초기화 상태 검증
            Assert.IsTrue(service.CanAcceptGameInput); // Steam 실패가 게임 입력을 막지 않는지 검증
            Assert.AreEqual(1, backend.InitializeCalls); // Backend 초기화 1회 호출 검증
        }

        [Test]
        public void Initialize_IsIdempotent()
        {
            var backend = new FakeSteamBackend(); // 정상 Backend 구성
            var service = new SteamPlatformService(backend); // 테스트 서비스 생성

            Assert.IsTrue(service.Initialize()); // 첫 초기화 검증
            Assert.IsTrue(service.Initialize()); // 중복 초기화 검증

            Assert.AreEqual(1, backend.InitializeCalls); // 실제 Backend 초기화는 한 번만 실행되는지 검증
        }

        [Test]
        public void OverlayActivation_BlocksAndRestoresGameplayInput()
        {
            var backend = new FakeSteamBackend(); // Overlay Backend 구성
            var service = new SteamPlatformService(backend); // 테스트 서비스 생성
            service.Initialize(); // 서비스 초기화

            backend.RaiseOverlayActive(true); // Overlay 활성화 Callback 발생

            Assert.IsTrue(service.IsOverlayActive); // Overlay 활성 상태 검증
            Assert.IsFalse(service.CanAcceptGameInput); // Overlay 중 게임 입력 차단 상태 검증

            backend.RaiseOverlayActive(false); // Overlay 비활성화 Callback 발생

            Assert.IsFalse(service.IsOverlayActive); // Overlay 비활성 상태 검증
            Assert.IsTrue(service.CanAcceptGameInput); // Overlay 종료 후 게임 입력 복구 검증
        }

        [Test]
        public void OpenOverlay_RequiresInitializationAndAvailability()
        {
            var backend = new FakeSteamBackend { OverlayEnabled = true }; // Overlay 사용 가능 Backend 구성
            var service = new SteamPlatformService(backend); // 테스트 서비스 생성

            Assert.IsFalse(service.OpenOverlay(SteamOverlayPage.Friends)); // 초기화 전 Overlay 호출 차단 검증

            service.Initialize(); // 서비스 초기화
            Assert.IsTrue(service.OpenOverlay(SteamOverlayPage.Friends)); // 초기화 후 Overlay 호출 검증
            Assert.AreEqual(SteamOverlayPage.Friends, backend.LastOpenedPage); // 요청 페이지 전달 검증

            backend.OverlayEnabled = false; // Overlay 사용 불가 상태 전환
            Assert.IsFalse(service.OpenOverlay(SteamOverlayPage.Community)); // 사용 불가 Overlay 호출 차단 검증
        }

        [Test]
        public void Tick_ForwardsCallbacksOnlyAfterInitialization()
        {
            var backend = new FakeSteamBackend(); // Callback Backend 구성
            var service = new SteamPlatformService(backend); // 테스트 서비스 생성

            service.Tick(); // 초기화 전 Tick
            Assert.AreEqual(0, backend.RunCallbackCalls); // 초기화 전 Callback 미호출 검증

            service.Initialize(); // 서비스 초기화
            service.Tick(); // 초기화 후 Tick
            Assert.AreEqual(1, backend.RunCallbackCalls); // Callback 전달 검증
        }

        [Test]
        public void Shutdown_ClearsOverlayStateAndIsSafeToRepeat()
        {
            var backend = new FakeSteamBackend(); // 종료 Backend 구성
            var service = new SteamPlatformService(backend); // 테스트 서비스 생성
            service.Initialize(); // 서비스 초기화
            backend.RaiseOverlayActive(true); // Overlay 활성화

            service.Shutdown(); // 첫 종료
            service.Shutdown(); // 중복 종료

            Assert.IsFalse(service.IsInitialized); // 종료 후 초기화 상태 해제 검증
            Assert.IsFalse(service.IsOverlayActive); // 종료 후 Overlay 상태 해제 검증
            Assert.IsTrue(service.CanAcceptGameInput); // 종료 후 입력 허용 검증
            Assert.AreEqual(1, backend.ShutdownCalls); // Backend 종료 중복 호출 방지 검증
        }

        [Test]
        public void CapabilityLookup_ReturnsOverlayCapabilityWithoutChangingRuntimeInterface()
        {
            var backend = new FakeSteamBackend(); // Runtime·Overlay Capability Backend 구성
            var service = new SteamPlatformService(backend); // 테스트 서비스 생성

            bool found = service.TryGetCapability<ISteamOverlayBackend>(out var overlay); // Overlay Capability 조회

            Assert.IsTrue(found); // Capability 발견 검증
            Assert.AreSame(backend, overlay); // 동일 Backend Capability 반환 검증
        }

        private sealed class FakeSteamBackend : ISteamRuntimeBackend, ISteamOverlayBackend
        {
            public string BackendName => "Fake"; // 테스트 Backend 이름
            public bool InitializeResult { get; set; } = true; // 초기화 결과 설정
            public bool OverlayEnabled { get; set; } = true; // Overlay 사용 가능 설정
            public int InitializeCalls { get; private set; } // 초기화 호출 수
            public int RunCallbackCalls { get; private set; } // Callback 호출 수
            public int ShutdownCalls { get; private set; } // 종료 호출 수
            public SteamOverlayPage? LastOpenedPage { get; private set; } // 마지막 Overlay 페이지
            public event Action<bool> OverlayActiveChanged; // Overlay 상태 변경 이벤트

            public bool Initialize()
            {
                InitializeCalls++; // 초기화 호출 기록
                return InitializeResult; // 설정된 초기화 결과 반환
            }

            public void RunCallbacks()
            {
                RunCallbackCalls++; // Callback 호출 기록
            }

            public void Shutdown()
            {
                ShutdownCalls++; // 종료 호출 기록
            }

            public bool IsOverlayEnabled()
            {
                return OverlayEnabled; // 설정된 Overlay 가능 상태 반환
            }

            public bool OpenOverlay(SteamOverlayPage page)
            {
                if (!OverlayEnabled) return false; // Overlay 불가 상태 차단
                LastOpenedPage = page; // 마지막 요청 페이지 저장
                return true; // Overlay 호출 성공 반환
            }

            public void RaiseOverlayActive(bool active)
            {
                OverlayActiveChanged?.Invoke(active); // 테스트 Overlay 상태 이벤트 발생
            }
        }
    }
}
