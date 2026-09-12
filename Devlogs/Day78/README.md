# 78일차 : Steam Cloud 동기화 계층 구현 및 Runtime 자동 연동 보정

## 개발 목표

77일차에서 Steamworks.NET Runtime과 Overlay 기반을 구성한 뒤, 78일차에서는 해당 Runtime 위에 Steam Cloud 저장 동기화 계층을 추가하고 Steam 공통 API 구조를 후속 기능 확장에 맞게 정리했다.

이번 작업의 핵심은 다음과 같다.

- Steam Cloud Capability 인터페이스 추가
- Steamworks.NET Remote Storage 읽기·쓰기 연결
- 로컬 `run_save.json`과 Steam Cloud 기본 동기화 구현
- 로컬 저장 변경 감지 후 주기적 Cloud 업로드
- 게임 종료 전 마지막 Cloud 업로드
- Steam Runtime·Overlay 계약을 AppID 기반 정적 서비스 구조로 보정
- Steam 미사용 환경에서도 안전하게 동작하는 Null Backend 확장
- Day78 Steam Cloud EditMode 테스트 추가
- Day77 Overlay 테스트를 변경된 Runtime 계약에 맞게 마이그레이션

78일차에서는 복잡한 충돌 해결 시스템보다 먼저 “로컬 저장이 있으면 Cloud에 올리고, 로컬 저장이 없을 때 Cloud 저장을 복원한다”는 단순하고 예측 가능한 기본 정책을 구현했다.

---

## Steam Cloud Capability 추가

Steam Runtime Backend에 Cloud 기능을 직접 강제하지 않고 `ISteamCloudBackend`를 별도 Capability로 추가했다.

현재 제공하는 기능은 다음과 같다.

```text
IsCloudEnabled
CloudFileExists
TryReadCloudFile
TryWriteCloudFile
```

Runtime 초기화와 Cloud 기능을 분리했기 때문에 Steam Runtime을 사용할 수 있더라도 계정 또는 게임 설정에서 Cloud가 비활성화된 경우 Cloud 기능만 안전하게 사용할 수 없는 상태로 처리할 수 있다.

`SteamPlatformService`에서는 현재 Backend가 `ISteamCloudBackend`를 지원하는지 확인한 뒤 Cloud 요청을 전달한다.

---

## Steamworks.NET Remote Storage 연결

`SteamworksNetBackend`가 기존 Runtime·Overlay 기능과 함께 `ISteamCloudBackend`를 구현하도록 확장했다.

Cloud 사용 가능 상태는 다음 두 조건을 함께 확인한다.

```text
SteamRemoteStorage.IsCloudEnabledForAccount()
SteamRemoteStorage.IsCloudEnabledForApp()
```

파일 존재 확인, 읽기, 쓰기는 각각 Steam Remote Storage API에 연결했다.

```text
SteamRemoteStorage.FileExists
SteamRemoteStorage.GetFileSize
SteamRemoteStorage.FileRead
SteamRemoteStorage.FileWrite
```

읽기 과정에서는 원격 파일 크기를 먼저 확인한 뒤 동일 크기의 byte 배열을 만들고 실제 읽은 크기가 예상 크기와 동일할 때만 성공으로 처리한다.

쓰기 과정에서는 Cloud 사용 가능 여부, 파일 이름, 데이터 null 여부를 먼저 검사한 뒤 Remote Storage에 저장한다.

---

## NullSteamBackend Cloud fallback

Steamworks.NET을 사용할 수 없는 환경에서도 기존 게임 실행이 유지되도록 `NullSteamBackend`가 Runtime·Overlay·Cloud Capability를 모두 구현하도록 확장했다.

Cloud 관련 상태와 동작은 다음과 같다.

```text
IsCloudEnabled → false
CloudFileExists → false
TryReadCloudFile → false
TryWriteCloudFile → false
```

따라서 Steam Client가 없거나 Steamworks.NET이 비활성화된 환경에서는 Cloud 동기화만 비활성화되고 로컬 게임 흐름에는 Cloud 예외가 전파되지 않는다.

---

## SteamCloudSaveSync 추가

로컬 Save 파일과 Steam Cloud 파일의 기본 동기화를 담당하는 `SteamCloudSaveSync`를 추가했다.

동기화 결과는 `SteamCloudSyncResult`로 구분한다.

```text
Unavailable
NoSave
Unchanged
UploadedLocal
RestoredRemote
Failed
```

이 결과값을 통해 Runtime 계층에서 Cloud가 사용할 수 없는 상황, 저장 파일이 없는 상황, 변경이 없는 상황, 업로드 또는 복원 성공, 실패를 구분할 수 있다.

---

## 시작 시 저장 동기화 정책

게임 시작 시 `SyncAtStartup()`을 실행한다.

현재 정책은 로컬 저장을 우선하는 단순한 구조다.

```text
Steam Cloud 사용 불가
→ Unavailable

로컬 저장 존재
→ 로컬 저장을 Steam Cloud에 업로드
→ UploadedLocal

로컬 저장 없음 + Cloud 저장 존재
→ Cloud 저장을 로컬에 복원
→ RestoredRemote

로컬 저장 없음 + Cloud 저장 없음
→ NoSave
```

현재 단계에서는 로컬과 원격 저장이 동시에 존재할 때 timestamp나 플레이 진행도를 비교하지 않는다.

로컬 저장이 존재하면 로컬 데이터를 기준으로 Cloud를 갱신한다.

복잡한 Save Conflict UI를 먼저 추가하지 않고 기본 동작을 안정적으로 구성하는 데 집중했다.

---

## Cloud 저장 복원 안전 처리

원격 Cloud 데이터를 로컬 저장으로 복원할 때 바로 최종 파일에 덮어쓰지 않고 임시 파일을 사용한다.

현재 복원 흐름은 다음과 같다.

```text
Cloud 데이터 읽기
→ run_save.json.cloud.tmp 작성
→ 작성 성공 시 run_save.json 위치로 이동
→ 실패 시 임시 파일 정리
```

파일 I/O 과정에서 `IOException` 또는 `UnauthorizedAccessException`이 발생하면 `Failed`를 반환한다.

이를 통해 Cloud 복원 중 파일 접근 문제가 발생하더라도 동기화 계층에서 실패를 처리하고 예외가 게임 전체 흐름으로 직접 확장되는 것을 줄였다.

---

## 로컬 저장 변경 감지

`SteamCloudSaveSync`는 마지막으로 Cloud에 반영한 로컬 byte 데이터를 Snapshot으로 보관한다.

`UploadLocalIfChanged()` 호출 시 현재 로컬 저장과 마지막 Snapshot을 byte 단위로 비교한다.

```text
저장 파일 없음
→ NoSave

이전 Snapshot과 동일
→ Unchanged

내용 변경
→ Steam Cloud 업로드
→ UploadedLocal
```

저장 내용이 바뀌지 않았는데 매 주기마다 같은 파일을 반복 업로드하지 않도록 구성했다.

---

## SteamPlatformService 정적 구조 보정

77일차의 인스턴스 기반 `SteamPlatformService`를 78일차에서는 전역 정적 서비스 구조로 정리했다.

현재 Runtime 공통 상태는 다음과 같다.

```text
BackendName
IsAvailable
IsInitialized
IsOverlayEnabled
IsCloudEnabled
```

Runtime 초기화는 AppID를 먼저 설정한 뒤 수행한다.

```text
Configure(appId)
→ TryInitialize()
```

Callback 처리도 기존 `RunCallbacks` 계열에서 `PumpCallbacks()`로 이름과 계약을 통일했다.

Cloud 기능은 서비스에 다음 Facade로 노출한다.

```text
CloudFileExists
TryReadCloudFile
TryWriteCloudFile
```

테스트에서는 실제 Steam Backend를 사용하지 않고 `SetBackendForTests()`와 `ResetBackend()`를 통해 Fake Backend를 연결할 수 있도록 했다.

---

## Runtime Backend 계약 보정

후속 Steam 기능을 추가하기 쉽도록 `ISteamRuntimeBackend` 계약을 다시 정리했다.

현재 Runtime Backend는 다음 정보를 제공한다.

```text
Name
IsAvailable
IsInitialized
Initialize(uint appId)
PumpCallbacks()
Shutdown()
```

77일차의 `BackendName`, 매개변수 없는 `Initialize()`, `RunCallbacks()` 계약을 새 구조에 맞게 변경했다.

AppID를 Backend 초기화 단계에 명시적으로 전달하여 RuntimeController와 Backend 사이의 초기화 책임을 분명하게 했다.

---

## Overlay Capability 계약 보정

Cloud Capability 추가와 함께 Overlay API도 동일한 Backend Capability 방식에 맞게 단순화했다.

현재 `ISteamOverlayBackend`는 다음 항목을 제공한다.

```text
IsOverlayEnabled
OpenOverlay(string dialog)
```

77일차에 사용하던 Overlay 활성 이벤트와 `SteamOverlayPage` 기반 호출 대신 Steam Overlay dialog 문자열을 직접 전달하도록 변경했다.

`SteamPlatform.OpenOverlay()`의 기본 dialog는 `Friends`다.

개발용 F8 Overlay Shortcut도 변경된 API에 맞게 다음 기준을 사용한다.

```text
SteamPlatform.OpenOverlay()
SteamPlatform.IsOverlayEnabled
```

---

## SteamPlatform 전역 접근점 보정

`SteamPlatform`은 인스턴스 Service를 Bind하는 구조 대신 `SteamPlatformService`의 정적 상태를 직접 노출하도록 변경했다.

현재 주요 접근 항목은 다음과 같다.

```text
BackendName
IsAvailable
IsInitialized
IsOverlayEnabled
IsCloudEnabled
OpenOverlay
```

게임 코드에서 Steamworks.NET 구현 세부사항을 직접 참조하지 않고 공통 진입점을 사용할 수 있도록 유지했다.

---

## SteamRuntimeController 자동 Cloud 연동

`SteamRuntimeController`가 Runtime 초기화뿐 아니라 Cloud 저장 동기화 생명주기도 함께 관리하도록 확장했다.

현재 주요 흐름은 다음과 같다.

```text
AfterSceneLoad
→ SteamRuntimeController 자동 생성

Awake
→ AppID Configure
→ Steam Runtime TryInitialize
→ 성공 시 SteamCloudSaveSync 생성
→ 시작 저장 동기화

Update
→ Steam Callback Pump
→ 설정된 주기마다 로컬 저장 변경 확인
→ 변경 시 Cloud 업로드

Application Quit / Object Destroy
→ 마지막 로컬 변경 업로드
→ Steam Runtime Shutdown
```

Cloud 저장 파일 이름은 현재 다음 파일을 사용한다.

```text
run_save.json
```

로컬 경로는 `Application.persistentDataPath`를 기준으로 생성한다.

---

## Cloud 주기 동기화

`SteamRuntimeController`에는 Cloud 동기화를 제어하는 설정을 추가했다.

현재 기본 구성은 다음과 같다.

```text
enableCloudSync = true
cloudSyncIntervalSeconds = 2
```

최소 주기는 0.5초로 제한한다.

매 프레임 파일을 바로 업로드하는 대신 지정된 시간 간격마다 `UploadLocalIfChanged()`를 호출하고 실제 데이터가 변경되었을 때만 Remote Storage 쓰기를 수행한다.

마지막 동기화 결과는 `LastCloudSyncResult`에 저장하여 Runtime 상태를 확인할 수 있게 했다.

---

## Day78 EditMode 테스트 추가

`Day78SteamCloudTests`를 추가하여 실제 Steam Client 없이 Fake Cloud Backend로 기본 동기화 규칙을 검증한다.

현재 테스트 항목은 다음과 같다.

- 로컬 Save가 존재하면 시작 시 Cloud에 업로드
- 로컬 Save가 없고 Remote Save가 존재하면 로컬로 복원
- 로컬·Remote Save가 모두 없으면 `NoSave`
- Cloud 사용 불가 상태에서는 Remote 쓰기를 수행하지 않음
- 시작 동기화 이후 내용이 같으면 중복 업로드하지 않음
- 로컬 Save 내용이 변경되면 새 byte 데이터를 업로드
- `SteamPlatformService` Cloud Facade가 Backend 읽기·쓰기를 정상 전달

테스트는 임시 디렉터리에 `run_save.json`을 생성하고 Fake Backend 내부 저장소를 사용하므로 실제 Steam Remote Storage 없이 동기화 규칙을 확인할 수 있도록 구성했다.

---

## Day77 테스트 및 API 마이그레이션

78일차 Runtime 계약이 변경되면서 기존 Day77 Overlay 테스트와 일부 호출부도 새 API에 맞게 보정할 필요가 생겼다.

주요 변경 방향은 다음과 같다.

```text
BackendName
→ Name

Initialize()
→ Initialize(uint appId)

RunCallbacks()
→ PumpCallbacks()

IsOverlayEnabled()
→ IsOverlayEnabled property

OpenOverlay(SteamOverlayPage)
→ OpenOverlay(string)

인스턴스 SteamPlatformService
→ 정적 SteamPlatformService
```

이를 통해 Day77 테스트가 이전 Runtime 인터페이스를 계속 구현하여 발생하던 계약 불일치를 78일차 구조와 동일한 형태로 맞췄다.

---

## 주요 변경 파일

### Steam Runtime / Capability

- `Assets/ProjectEta/Scripts/Steam/ISteamRuntimeBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/ISteamOverlayBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/ISteamCloudBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/NullSteamBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamworksNetBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamPlatformService.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamPlatform.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamRuntimeController.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamOverlayDebugShortcut.cs`

### Steam Cloud

- `Assets/ProjectEta/Scripts/Steam/SteamCloudSaveSync.cs`

### Test

- `Assets/ProjectEta/Tests/EditMode/Day78SteamCloudTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day77SteamOverlayTests.cs`

신규 Unity Script에는 대응하는 `.meta` 파일이 함께 추가되어 있다.

---

## 현재 확인 상태

78일차 기준 최신 커밋은 다음 작업을 반영한다.

```text
78일차 : Steam Cloud 동기화 계층 구현 및 Runtime 자동 연동 보정
```

현재 저장소 코드에서 확인되는 상태는 다음과 같다.

```text
Steam Runtime
→ AppID 기반 초기화
→ Callback Pump
→ 안전 종료

Steam Overlay
→ IsOverlayEnabled
→ string dialog 기반 OpenOverlay

Steam Cloud
→ 계정·App Cloud 활성 상태 확인
→ Remote 파일 존재 확인
→ byte 데이터 읽기·쓰기
→ 시작 시 로컬 우선 기본 동기화
→ 변경된 로컬 저장만 주기 업로드
→ 종료 전 마지막 업로드
```

`Day78SteamCloudTests`에는 Cloud 기본 동기화와 Service Facade를 검증하는 7개의 EditMode 테스트가 작성되어 있다.

다만 저장소 코드 확인만으로 Unity Editor 전체 Script Compile과 실제 Steam Client Remote Storage 동작 성공까지 확정할 수는 없다.

최종 확인 항목은 다음과 같다.

- Unity Editor 전체 Script Compile
- Day77 / Day78 EditMode Test Runner 실행
- Steam Client 실행 상태에서 Runtime 초기화
- Steamworks App 설정에서 Steam Cloud 활성화
- `run_save.json` 실제 Remote Storage 업로드
- 로컬 Save 제거 후 Remote Save 복원
- 게임 종료 직전 마지막 변경 업로드

---

## 현재 Cloud 동기화 정책의 한계

78일차의 Cloud 동기화는 기본 구조를 먼저 완성하기 위한 단순 정책이다.

현재 로컬과 Remote Save가 모두 존재하면 로컬 저장을 우선하여 Remote 데이터를 덮어쓴다.

따라서 다음 기능은 아직 포함하지 않는다.

- Local / Remote timestamp 비교
- 플레이 진행도 기반 최신 Save 판별
- 사용자 선택형 Save Conflict UI
- Save 버전 호환성 검사
- 손상된 Save 복구
- 여러 Save Slot 동기화

이 기능들은 실제 Save 데이터 구조가 안정된 뒤 확장하는 편이 안전하다.

---

## 다음 개발 방향

78일차에서 Steam Runtime 위에 Cloud Capability를 추가했으므로 다음 단계에서는 동일한 구조로 Steam Achievement 기능을 추가하는 것이 자연스럽다.

다음 작업의 우선 방향은 다음과 같다.

- `ISteamAchievementBackend` Capability 정의
- Steam User Stats 초기화 및 준비 상태 관리
- Achievement 해금 API 연결
- 이미 해금된 Achievement 중복 처리
- 게임 이벤트와 Achievement ID 연결
- Steam 미사용 환경 fallback 유지
- Achievement EditMode 단위 테스트
- 실제 Steam Client에서 Achievement 팝업 및 저장 확인

Runtime → Overlay → Cloud까지 공통 Steam 계층이 정리되었기 때문에 Achievement 역시 게임 코드가 Steamworks.NET API를 직접 호출하지 않고 `SteamPlatform`과 Backend Capability를 통해 확장하는 방향으로 이어갈 수 있다.
