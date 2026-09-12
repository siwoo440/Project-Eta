# 77일차 : Steamworks.NET Runtime·Overlay 연동 및 Assembly 참조·개발 AppID 정식화

## 개발 목표

76일차까지 카드·기물·Enemy·Fusion 콘텐츠 Pool 규칙과 데이터 무결성 검증 계층을 정리한 뒤, 77일차에서는 Steam 출시 환경 준비의 첫 단계로 Steamworks.NET Runtime과 Steam Overlay 연동 기반을 추가했다.

이번 작업의 핵심은 다음과 같다.

- Steamworks.NET 패키지 설치 및 ProjectEta Runtime Assembly 연결
- Steam 초기화·Callback·종료를 담당하는 공통 Runtime 계층 구성
- Steam Overlay 호출과 활성 상태 추적
- Overlay 활성 중 게임 입력 차단 상태 제공
- Steam이 없거나 초기화에 실패해도 게임은 계속 실행되는 fallback 구성
- Development Build용 `steam_appid.txt` 처리 자동화
- Steam Runtime·Overlay 기본 동작 EditMode 테스트 추가

Steam Cloud와 Achievement를 바로 구현하기보다 이후 Steam 기능들이 공통으로 사용할 Runtime 기반을 먼저 정리했다.

---

## Steamworks.NET 패키지 연결

Steamworks.NET은 OpenUPM Scoped Registry를 통해 프로젝트 Package에 추가했다.

현재 사용하는 패키지는 다음과 같다.

```text
com.rlabrecque.steamworks.net
2025.164.1
```

`Packages/manifest.json`에는 OpenUPM Registry와 Steamworks.NET dependency를 등록했고, `Packages/packages-lock.json`에도 동일 버전의 Registry package가 기록되어 있다.

---

## ProjectEta.Runtime Assembly 참조 추가

Steamworks.NET 패키지가 설치되면 `STEAMWORKS_NET` define이 활성화되고 `SteamworksNetBackend`가 실제 컴파일 대상에 포함된다.

이 과정에서 `ProjectEta.Runtime.asmdef`가 Steamworks.NET Runtime Assembly를 직접 참조하지 않아 다음 계열의 컴파일 오류가 발생했다.

```text
Steamworks namespace not found
GameOverlayActivated_t not found
Callback<> not found
```

이를 해결하기 위해 기존 Runtime Assembly 참조를 유지하면서 다음 참조를 추가했다.

```text
com.rlabrecque.steamworks.net
```

현재 `ProjectEta.Runtime`의 주요 참조는 다음과 같다.

```text
Unity.InputSystem
UnityEngine.UI
com.rlabrecque.steamworks.net
```

Steamworks 코드를 조건부 컴파일하는 것만으로 끝내지 않고 실제 Assembly Definition 의존성까지 명시하여 Runtime 코드가 패키지 Assembly를 정상적으로 참조할 수 있도록 정리했다.

---

## Steam Runtime Backend 추상화

Steam 기능을 게임 코드에 직접 결합하지 않도록 Backend 인터페이스를 추가했다.

주요 구성은 다음과 같다.

```text
ISteamRuntimeBackend
ISteamOverlayBackend
NullSteamBackend
SteamworksNetBackend
SteamBackendFactory
```

`ISteamRuntimeBackend`는 다음 공통 동작을 제공한다.

```text
Initialize
RunCallbacks
Shutdown
```

`ISteamOverlayBackend`는 Overlay 기능을 선택 Capability로 분리하여 다음 기능을 제공한다.

```text
OverlayActiveChanged
IsOverlayEnabled
OpenOverlay
```

이 구조를 통해 이후 Steam Cloud, Achievement 같은 기능을 추가할 때 Runtime 초기화 코드를 다시 만들지 않고 Capability를 확장할 수 있는 기반을 마련했다.

---

## SteamworksNetBackend 구현

`SteamworksNetBackend`는 `STEAMWORKS_NET` define이 활성화된 환경에서 실제 Steamworks.NET API를 사용한다.

현재 처리하는 기능은 다음과 같다.

- `SteamAPI.Init()` 초기화
- `SteamAPI.RunCallbacks()` Callback 처리
- `SteamAPI.Shutdown()` 종료
- `GameOverlayActivated_t` Callback 등록
- `SteamUtils.IsOverlayEnabled()` 사용 가능 상태 확인
- `SteamFriends.ActivateGameOverlay()` Overlay 호출

Steam Native DLL이 없거나 초기 타입 초기화에 실패하는 경우 게임 전체 실행 실패로 확장되지 않도록 초기화 실패를 안전하게 반환한다.

---

## NullSteamBackend fallback

Steamworks.NET이 비활성화된 환경에서는 `NullSteamBackend`를 사용한다.

```text
Initialize → false
RunCallbacks → no-op
Shutdown → no-op
```

따라서 Steam Client가 없거나 Steam 기능을 사용할 수 없는 환경에서도 Steam 기능만 비활성 상태가 되고 게임 본체는 계속 실행할 수 있다.

`SteamBackendFactory`가 현재 빌드 환경에 따라 실제 Steamworks.NET Backend와 Null Backend를 선택한다.

---

## SteamPlatformService 추가

`SteamPlatformService`를 추가하여 게임 계층에서 Steam Runtime 세부 구현을 직접 다루지 않도록 했다.

현재 관리하는 상태는 다음과 같다.

```text
BackendName
IsInitialized
IsOverlayAvailable
IsOverlayActive
CanAcceptGameInput
LastError
```

초기화 실패, Callback 처리 예외, Overlay 호출 예외, 종료 예외는 서비스 내부에서 처리하고 오류 메시지는 `LastError`에 기록한다.

Steam Runtime에 선택적 기능이 추가될 수 있도록 `TryGetCapability<TCapability>()`도 제공한다.

---

## Overlay 활성 상태와 게임 입력

Steam Overlay가 활성화되면 `GameOverlayActivated_t` Callback을 통해 서비스 상태를 갱신한다.

```text
Overlay 활성
→ IsOverlayActive = true
→ CanAcceptGameInput = false
```

Overlay가 닫히면 다음 상태로 복구된다.

```text
Overlay 비활성
→ IsOverlayActive = false
→ CanAcceptGameInput = true
```

현재 단계에서는 입력 시스템 전체를 직접 잠그는 대신 다른 게임 시스템이 `CanAcceptGameInput` 상태를 확인할 수 있는 공통 기준을 제공하는 방식으로 구성했다.

---

## SteamPlatform 전역 접근점

`SteamPlatform`을 추가하여 다른 게임 시스템에서 현재 Steam 상태와 Overlay 기능을 조회할 수 있도록 했다.

주요 접근 항목은 다음과 같다.

```text
Service
IsInitialized
IsOverlayAvailable
IsOverlayActive
CanAcceptGameInput
BackendName
LastError
OpenOverlay
```

Cloud와 Achievement 같은 후속 Steam 기능도 이 Runtime Service를 기준으로 연결할 수 있다.

---

## SteamRuntimeController 자동 생성

`SteamRuntimeController`는 `BeforeSceneLoad` 시점에 자동 생성된다.

현재 역할은 다음과 같다.

- 중복 RuntimeController 방지
- `DontDestroyOnLoad` 적용
- Steam Backend 및 Service 생성
- Steam 초기화
- 매 프레임 Callback 처리
- Application 종료 및 Object 파괴 시 Shutdown
- 전역 `SteamPlatform` Bind / Unbind

따라서 특정 Scene에 Steam 전용 GameObject를 수동으로 배치하지 않아도 Runtime이 자동으로 준비된다.

---

## 개발용 Overlay F8 테스트

Editor 또는 Development Build에서는 `SteamOverlayDebugShortcut`을 자동으로 붙인다.

F8 키를 누르면 Steam 친구 Overlay를 열도록 요청하고 다음 진단 정보를 Console에 기록한다.

```text
Opened
Backend
Available
```

이는 Steam Client에서 Overlay가 실제로 활성화되는지 빠르게 확인하기 위한 개발 전용 경로다.

Release Build에는 이 Debug Shortcut이 포함되지 않는다.

---

## Development Build용 steam_appid.txt 처리

로컬 Steam 개발 실행에 사용하는 `steam_appid.txt`가 저장소에 올라가지 않도록 `.gitignore`에 추가했다.

또한 `SteamDevelopmentAppIdPostprocessor`를 추가하여 Development Build에서 프로젝트 루트의 `steam_appid.txt`가 존재할 경우 실행 파일 위치로 복사한다.

현재 동작은 다음과 같다.

```text
Development Build
+ 프로젝트 루트 steam_appid.txt 존재
→ 빌드 실행 파일 위치로 복사
```

Release Build에서는 복사하지 않는다.

Windows와 Linux는 실행 파일이 위치한 폴더를 사용하고, macOS는 App Bundle의 `Contents/MacOS` 폴더를 대상으로 한다.

---

## Day77 EditMode 테스트 추가

`Day77SteamOverlayTests`를 추가했다.

현재 테스트 항목은 다음과 같다.

- Steam 초기화 실패 시 게임 입력 가능 상태 유지
- 중복 Initialize 호출 방지
- Overlay 활성 시 게임 입력 차단 상태 전환
- Overlay 종료 후 게임 입력 복구
- 초기화·사용 가능 상태에 따른 Overlay 호출 제한
- 초기화 이후에만 Callback 전달
- 중복 Shutdown 안전 처리
- Overlay Capability 조회

테스트에서는 실제 Steam Client 대신 Fake Backend를 사용하여 Steam Runtime Service의 상태 전환과 호출 계약을 검증한다.

---

## 주요 변경 파일

### Package / Assembly

- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Assets/ProjectEta/Scripts/ProjectEta.Runtime.asmdef`

### Steam Runtime

- `Assets/ProjectEta/Scripts/Steam/ISteamRuntimeBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/ISteamOverlayBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/NullSteamBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamworksNetBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamBackendFactory.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamPlatformService.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamPlatform.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamRuntimeController.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamOverlayPage.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamOverlayDebugShortcut.cs`

### Editor / Development

- `Assets/ProjectEta/Editor/SteamDevelopmentAppIdPostprocessor.cs`
- `.gitignore`

### Test

- `Assets/ProjectEta/Tests/EditMode/Day77SteamOverlayTests.cs`

각 신규 Unity Asset에는 대응하는 `.meta` 파일을 함께 추가했다.

---

## 현재 확인 상태

최신 `main`의 77일차 커밋에는 Steamworks.NET Runtime, Overlay 기반, Development AppID 처리, EditMode 테스트와 Steamworks.NET Assembly 참조 수정이 함께 반영되어 있다.

원격 저장소에서 확인한 현재 Package 상태는 다음과 같다.

```text
Steamworks.NET 2025.164.1
source: OpenUPM Registry
ProjectEta.Runtime reference: com.rlabrecque.steamworks.net
```

초기에 발생했던 `Steamworks`, `GameOverlayActivated_t`, `Callback<>` namespace/type 오류의 직접 원인이었던 Runtime Assembly 참조 누락은 최신 커밋에서 수정되어 있다.

GitHub 저장소에는 현재 커밋에 연결된 CI Status가 등록되어 있지 않다.

따라서 이번 개발 일지에서는 원격 저장소의 코드·Package·Assembly 설정 반영 상태까지만 확인한 것으로 기록한다. Unity Editor의 전체 Script Compile, EditMode Test Runner, 실제 Steam Client Overlay 호출 성공 여부는 로컬 Unity/Steam 실행 결과로 최종 확인해야 한다.

---

## 다음 개발 방향

77일차에서 Steam 공통 Runtime과 Overlay 진입 기반을 마련했으므로 다음 단계에서는 이 Runtime Service 위에 Steam Cloud 저장 기능을 연결하는 것이 자연스럽다.

다음 작업의 우선 방향은 다음과 같다.

- Steam Cloud Capability 인터페이스 정의
- 기존 Save / Load 데이터와 Remote Storage 연결
- 로컬 저장과 Steam Cloud 충돌 정책 정의
- Steam 미사용 환경의 기존 로컬 Save 유지
- 업로드·다운로드 실패 시 게임 진행 보호
- Steam Cloud EditMode 단위 테스트
- 실제 Steam Client 환경 저장 동기화 확인

Cloud 기반을 정리한 뒤 Achievement와 전체 Steam UI 통합으로 확장할 수 있다.
