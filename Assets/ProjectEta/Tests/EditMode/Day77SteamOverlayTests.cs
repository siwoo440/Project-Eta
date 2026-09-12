using NUnit.Framework; // NUnit 테스트 사용
using ProjectEta.Steam; // Steam 플랫폼 서비스 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day77SteamOverlayTests
    {
        private const uint TestAppId = 480; // 테스트용 Steam AppID

        [TearDown]
        public void TearDown() // 테스트 Steam 상태 정리
        {
            SteamPlatformService.ResetBackend(); // 테스트 Backend 초기화
        }

        [Test]
        public void TryInitializeFailure_KeepsRuntimeUninitialized() // 초기화 실패 상태 검증
        {
            FakeSteamBackend backend = ConfigureBackend(initializeResult: false); // 초기화 실패 Backend 구성

            bool initialized = SteamPlatformService.TryInitialize(); // Steam 초기화 시도

            Assert.IsFalse(initialized); // 초기화 실패 반환 검증
            Assert.IsFalse(SteamPlatformService.IsInitialized); // Runtime 비초기화 상태 검증
            Assert.IsFalse(SteamPlatformService.IsOverlayEnabled); // Overlay 비활성 상태 검증
            Assert.AreEqual(1, backend.InitializeCalls); // Backend 초기화 호출 수 검증
        }

        [Test]
        public void TryInitialize_ForwardsConfiguredAppId() // AppID 전달 검증
        {
            FakeSteamBackend backend = ConfigureBackend(); // 정상 Backend 구성

            bool initialized = SteamPlatformService.TryInitialize(); // Steam 초기화 시도

            Assert.IsTrue(initialized); // 초기화 성공 검증
            Assert.IsTrue(SteamPlatformService.IsInitialized); // Runtime 초기화 상태 검증
            Assert.AreEqual("Fake", SteamPlatformService.BackendName); // Backend 이름 검증
            Assert.AreEqual(TestAppId, backend.LastAppId); // 설정 AppID 전달 검증
        }

        [Test]
        public void IsOverlayEnabled_ReflectsBackendCapability() // Overlay 사용 가능 상태 검증
        {
            FakeSteamBackend backend = ConfigureBackend(); // Overlay Backend 구성
            SteamPlatformService.TryInitialize(); // Steam 초기화 수행

            Assert.IsTrue(SteamPlatformService.IsOverlayEnabled); // Overlay 사용 가능 상태 검증

            backend.OverlayEnabled = false; // Overlay 사용 불가 상태 전환

            Assert.IsFalse(SteamPlatformService.IsOverlayEnabled); // Overlay 사용 불가 상태 검증
        }

        [Test]
        public void OpenOverlay_RequiresInitializationAndForwardsDialog() // Overlay 호출 조건 검증
        {
            FakeSteamBackend backend = ConfigureBackend(); // Overlay Backend 구성

            Assert.IsFalse(SteamPlatformService.OpenOverlay("Friends")); // 초기화 전 Overlay 차단 검증

            SteamPlatformService.TryInitialize(); // Steam 초기화 수행
            Assert.IsTrue(SteamPlatformService.OpenOverlay("Friends")); // 초기화 후 Overlay 호출 검증
            Assert.AreEqual("Friends", backend.LastOpenedDialog); // Overlay 문자열 전달 검증

            backend.OverlayEnabled = false; // Overlay 사용 불가 상태 전환
            Assert.IsFalse(SteamPlatformService.OpenOverlay("Community")); // 사용 불가 Overlay 차단 검증
        }

        [Test]
        public void PumpCallbacks_ForwardsOnlyAfterInitialization() // Callback 전달 조건 검증
        {
            FakeSteamBackend backend = ConfigureBackend(); // Callback Backend 구성

            SteamPlatformService.PumpCallbacks(); // 초기화 전 Callback 처리
            Assert.AreEqual(0, backend.PumpCallbackCalls); // 초기화 전 Callback 미호출 검증

            SteamPlatformService.TryInitialize(); // Steam 초기화 수행
            SteamPlatformService.PumpCallbacks(); // 초기화 후 Callback 처리
            Assert.AreEqual(1, backend.PumpCallbackCalls); // Callback 전달 검증
        }

        [Test]
        public void Shutdown_ClearsInitializedState() // Steam 종료 상태 검증
        {
            FakeSteamBackend backend = ConfigureBackend(); // 종료 Backend 구성
            SteamPlatformService.TryInitialize(); // Steam 초기화 수행

            SteamPlatformService.Shutdown(); // Steam 종료 수행

            Assert.IsFalse(SteamPlatformService.IsInitialized); // 종료 후 초기화 상태 해제 검증
            Assert.IsFalse(SteamPlatformService.IsOverlayEnabled); // 종료 후 Overlay 상태 해제 검증
            Assert.AreEqual(1, backend.ShutdownCalls); // Backend 종료 호출 수 검증
        }

        [Test]
        public void SteamPlatformDefaultOverlay_UsesFriendsDialog() // 기본 Overlay 문자열 검증
        {
            FakeSteamBackend backend = ConfigureBackend(); // 기본 Overlay Backend 구성
            SteamPlatformService.TryInitialize(); // Steam 초기화 수행

            bool opened = SteamPlatform.OpenOverlay(); // 기본 Overlay 호출

            Assert.IsTrue(opened); // 기본 Overlay 호출 성공 검증
            Assert.AreEqual("Friends", backend.LastOpenedDialog); // 기본 Friends 문자열 검증
        }

        private static FakeSteamBackend ConfigureBackend(bool initializeResult = true) // 테스트 Backend 구성
        {
            FakeSteamBackend backend = new FakeSteamBackend { InitializeResult = initializeResult }; // 가짜 Backend 생성
            SteamPlatformService.SetBackendForTests(backend); // 테스트 Backend 등록
            SteamPlatformService.Configure(TestAppId); // 테스트 AppID 설정
            return backend; // 구성 Backend 반환
        }

        private sealed class FakeSteamBackend : ISteamRuntimeBackend, ISteamOverlayBackend
        {
            private bool initialized; // Backend 초기화 상태

            public string Name => "Fake"; // 테스트 Backend 이름
            public bool IsAvailable => true; // 테스트 Backend 사용 가능 여부
            public bool IsInitialized => initialized; // 테스트 Backend 초기화 상태
            public bool IsOverlayEnabled => initialized && OverlayEnabled; // Overlay 사용 가능 상태
            public bool InitializeResult { get; set; } = true; // 초기화 결과 설정
            public bool OverlayEnabled { get; set; } = true; // Overlay 사용 가능 설정
            public uint LastAppId { get; private set; } // 마지막 초기화 AppID
            public int InitializeCalls { get; private set; } // 초기화 호출 수
            public int PumpCallbackCalls { get; private set; } // Callback 호출 수
            public int ShutdownCalls { get; private set; } // 종료 호출 수
            public string LastOpenedDialog { get; private set; } = string.Empty; // 마지막 Overlay 문자열

            public bool Initialize(uint appId) // AppID 기반 Backend 초기화
            {
                InitializeCalls++; // 초기화 호출 기록
                LastAppId = appId; // 초기화 AppID 기록
                initialized = InitializeResult; // 초기화 상태 반영
                return initialized; // 초기화 결과 반환
            }

            public void PumpCallbacks() // Steam Callback 처리
            {
                PumpCallbackCalls++; // Callback 호출 기록
            }

            public void Shutdown() // Backend 종료
            {
                if (!initialized) // 중복 종료 상태 검사
                {
                    return; // 미초기화 종료 생략
                }

                initialized = false; // 초기화 상태 해제
                ShutdownCalls++; // 종료 호출 기록
            }

            public bool OpenOverlay(string dialog) // Overlay 문자열 호출
            {
                if (!IsOverlayEnabled) // Overlay 사용 가능 상태 검사
                {
                    return false; // Overlay 호출 차단
                }

                LastOpenedDialog = dialog; // 마지막 요청 문자열 저장
                return true; // Overlay 호출 성공 반환
            }
        }
    }
}
