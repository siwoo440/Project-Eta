# 79일차 : Steam Achievement 기반 계층 구현 및 최신 Steamworks SDK 대응

## 개발 목표

78일차에서 Steam Runtime·Overlay·Cloud 기반을 정리한 뒤, 79일차에서는 동일한 Capability 구조를 사용해 Steam Achievement 기능을 추가했다.

이번 작업의 핵심은 다음과 같다.

- `ISteamAchievementBackend` Capability 추가
- Achievement API Name 공통 정의
- `SteamPlatform` / `SteamPlatformService` Achievement Facade 추가
- `NullSteamBackend` Achievement fallback 확장
- `SteamworksNetBackend`에 실제 Achievement 조회·해금·저장 연결
- 최신 Steamworks SDK에서 제거된 `RequestCurrentStats()` 의존성 제거
- Achievement EditMode 테스트 추가
- Steamworks Partner 등록 항목과 실제 게임 이벤트 연결 방법 정리

79일차에서는 게임 시스템이 Steamworks.NET API를 직접 호출하지 않고 `SteamPlatform`을 통해 Achievement를 사용할 수 있는 기반을 만드는 데 집중했다.

---

## Achievement Capability 추가

Steam Runtime Backend에 Achievement 기능을 직접 강제하지 않고 `ISteamAchievementBackend`를 별도 Capability로 추가했다.

현재 제공하는 기능은 다음과 같다.

```text
IsAchievementEnabled
TryUnlockAchievement
TryGetAchievementUnlocked
```

이 구조는 기존의 다음 Capability 분리 방식과 동일하다.

```text
ISteamRuntimeBackend
ISteamOverlayBackend
ISteamCloudBackend
ISteamAchievementBackend
```

게임 코드에서는 실제 Steamworks.NET 구현을 알 필요 없이 `SteamPlatform` 또는 `SteamPlatformService`를 통해 Achievement 기능을 호출한다.

---

## Steam Achievement ID 공통 관리

Steamworks Partner에 등록하는 Achievement API Name을 코드 여러 위치에 문자열로 직접 작성하지 않도록 `SteamAchievementIds`를 추가했다.

현재 정의한 Achievement ID는 다음과 같다.

```text
FIRST_VICTORY
FIRST_FUSION
FIRST_FIVE_STAR
MID_BOSS_CLEAR
FIRST_RUN_CLEAR
```

코드에서는 문자열을 직접 전달하기보다 다음과 같이 사용한다.

```csharp
SteamPlatform.UnlockAchievement(SteamAchievementIds.FirstVictory);
```

이를 통해 API Name 오타와 대소문자 불일치 가능성을 줄였다.

---

## SteamPlatform Achievement API 추가

기존 Steam 전역 접근점인 `SteamPlatform`에 Achievement 기능을 추가했다.

현재 추가된 접근 항목은 다음과 같다.

```text
IsAchievementEnabled
UnlockAchievement
TryGetAchievementUnlocked
```

따라서 일반 게임 시스템에서는 Steamworks.NET을 직접 참조하지 않고 다음 형태로 사용할 수 있다.

```csharp
SteamPlatform.UnlockAchievement(SteamAchievementIds.FirstVictory);
```

Achievement 상태 조회가 필요한 경우 다음 API를 사용할 수 있다.

```csharp
SteamPlatform.TryGetAchievementUnlocked(
    SteamAchievementIds.FirstVictory,
    out bool unlocked);
```

---

## SteamPlatformService Achievement Facade

`SteamPlatformService`는 현재 Backend가 `ISteamAchievementBackend`를 지원하는지 확인한 뒤 요청을 전달한다.

Achievement 해금 요청 전에는 다음 조건을 확인한다.

```text
Steam Runtime 초기화 여부
Achievement Capability 지원 여부
Achievement 사용 가능 상태
API Name null / 빈 문자열 여부
```

조건을 통과한 경우 실제 Backend의 `TryUnlockAchievement()`를 호출한다.

조회 API도 동일한 방식으로 `TryGetAchievementUnlocked()`에 전달한다.

---

## NullSteamBackend Achievement fallback

Steamworks.NET이 없는 환경에서도 게임 실행이 유지되도록 `NullSteamBackend`에 `ISteamAchievementBackend`를 구현했다.

현재 fallback 동작은 다음과 같다.

```text
IsAchievementEnabled → false
TryUnlockAchievement → false
TryGetAchievementUnlocked → false
```

따라서 Unity Editor, Steam Client 미실행 환경, Steamworks.NET 비활성 환경에서도 Achievement 기능만 비활성화되고 게임 전체 실행 흐름은 유지할 수 있다.

---

## SteamworksNetBackend Achievement 구현

`SteamworksNetBackend`가 기존 Runtime·Overlay·Cloud Capability와 함께 Achievement Capability를 구현하도록 확장했다.

현재 Achievement 해금 흐름은 다음과 같다.

```text
Steam Runtime 초기화
→ GetAchievement
→ 이미 해금되어 있으면 성공 반환
→ 미해금 상태라면 SetAchievement
→ StoreStats
→ Steam에 상태 저장
```

사용하는 주요 Steam API는 다음과 같다.

```text
SteamUserStats.GetAchievement
SteamUserStats.SetAchievement
SteamUserStats.StoreStats
```

이미 해금된 Achievement는 다시 `SetAchievement()`를 실행하지 않고 성공으로 처리한다.

`SetAchievement()`가 성공해도 실제 Steam 저장을 반영하기 위해 `StoreStats()` 결과까지 확인한다.

---

## RequestCurrentStats 제거 대응

초기 Achievement 구현 과정에서 다음 컴파일 오류가 확인되었다.

```text
CS0117:
'SteamUserStats' does not contain a definition for 'RequestCurrentStats'
```

최신 Steamworks SDK에서는 현재 사용자의 Stats·Achievement 데이터를 Steam Client가 게임 실행 전에 동기화하므로 `RequestCurrentStats()`가 더 이상 필요하지 않고 SDK에서 제거되었다.

따라서 79일차 최종 코드에서는 다음 의존성을 사용하지 않는다.

```text
RequestCurrentStats()
UserStatsReceived_t
achievementStatsReady
```

현재 구조는 `SteamAPI.Init()` 성공을 기준으로 Achievement Capability를 활성화하고 `GetAchievement`, `SetAchievement`, `StoreStats`를 사용한다.

`RequestUserStats()`는 다른 사용자의 Stats를 요청하는 API이므로 `RequestCurrentStats()`의 대체 호출로 사용하지 않는다.

참고:

- Steamworks ISteamUserStats: https://partner.steamgames.com/doc/api/isteamuserstats
- Steamworks SDK 변경 기록: https://github.com/rlabrecque/SteamworksSDK/blob/main/Readme.txt

---

## Day79 EditMode 테스트 추가

`Day79SteamAchievementTests`를 추가하여 실제 Steam Client 없이 Fake Achievement Backend로 Facade 동작을 검증할 수 있도록 했다.

현재 테스트 항목은 다음과 같다.

- Runtime 미초기화 상태에서는 Achievement 해금 요청 차단
- Achievement 사용 가능 상태에서는 API Name을 Backend에 전달
- 빈 Achievement API Name 차단
- Achievement 해금 상태 조회 결과 전달
- Achievement Capability가 준비되지 않은 상태에서는 비활성 상태 반환

Fake Backend는 실제 Steam 서버를 사용하지 않고 Runtime 초기화와 Achievement 상태를 테스트 내부에서 제어한다.

---

## 직접 해야 하는 부분 1 — Steamworks Partner 설정

Steamworks Partner의 해당 게임 App Admin에서 Achievement를 등록해야 실제 Steam 계정에 Achievement가 기록된다.

다음 API Name을 코드와 정확히 동일하게 등록한다.

| API Name | 용도 |
| --- | --- |
| `FIRST_VICTORY` | 첫 전투 승리 |
| `FIRST_FUSION` | 첫 합성 성공 |
| `FIRST_FIVE_STAR` | 첫 5성 기물 획득 |
| `MID_BOSS_CLEAR` | 중간 보스 격파 |
| `FIRST_RUN_CLEAR` | 첫 Run 클리어 |

표시 이름, 설명, 아이콘은 자유롭게 구성할 수 있지만 API Name은 코드와 대소문자까지 동일해야 한다.

Steamworks App Admin에서 Achievement를 만든 뒤 변경사항을 게시해야 실제 API에서 사용할 수 있다.

현재 개발 기본 AppID는 다음 값이다.

```text
480
```

AppID `480`은 Steam의 Spacewar 개발용 App이므로 프로젝트 η 전용 Achievement를 실제 검증하려면 최종적으로 프로젝트 η의 실제 Steam AppID와 해당 App에 등록된 Achievement 설정이 필요하다.

---

## 직접 해야 하는 부분 2 — 게임 이벤트 연결

Achievement 기반 계층은 추가되었지만 전투 승리, 합성 성공, 보스 처치 같은 실제 게임 이벤트 발생 지점에는 아직 자동 연결하지 않았다.

각 게임 이벤트의 성공이 확정된 직후 다음 호출을 연결한다.

```csharp
SteamPlatform.UnlockAchievement(SteamAchievementIds.FirstVictory);
SteamPlatform.UnlockAchievement(SteamAchievementIds.FirstFusion);
SteamPlatform.UnlockAchievement(SteamAchievementIds.FirstFiveStar);
SteamPlatform.UnlockAchievement(SteamAchievementIds.MidBossClear);
SteamPlatform.UnlockAchievement(SteamAchievementIds.FirstRunClear);
```

호출하는 파일이 `ProjectEta.Steam` namespace를 사용하지 않는 경우 파일 상단에 다음 using을 추가한다.

```csharp
using ProjectEta.Steam;
```

Achievement 호출 위치는 입력 또는 시도 시점이 아니라 결과가 실제로 확정된 지점이어야 한다.

예시는 다음과 같다.

```text
전투 시작
X FIRST_VICTORY

전투 승리 결과 확정
O FIRST_VICTORY
```

```text
합성 버튼 입력
X FIRST_FUSION

합성 결과 생성 성공
O FIRST_FUSION
```

같은 기준으로 5성 기물 생성, 중간 보스 격파, Run 클리어도 실제 성공 결과가 확정된 이후 호출한다.

---

## 현재 Achievement 동작 흐름

79일차 최종 구조는 다음과 같다.

```text
게임 실행
  ↓
Steam Runtime 초기화
  ↓
Achievement Capability 사용 가능
  ↓
게임 이벤트에서 UnlockAchievement(API Name)
  ↓
GetAchievement
  ↓
이미 해금됨 → 성공 반환
  ↓
미해금 → SetAchievement
  ↓
StoreStats
  ↓
Steam 상태 저장
```

최신 SDK 대응으로 다음 단계는 포함하지 않는다.

```text
RequestCurrentStats
UserStatsReceived_t 대기
```

Steam Client가 현재 사용자의 Stats와 Achievement 데이터를 게임 시작 전에 동기화하는 최신 SDK 동작을 기준으로 구성했다.

---

## 주요 변경 파일

### Steam Achievement

- `Assets/ProjectEta/Scripts/Steam/ISteamAchievementBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamAchievementIds.cs`

### Steam Runtime / Backend

- `Assets/ProjectEta/Scripts/Steam/NullSteamBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamworksNetBackend.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamPlatformService.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamPlatform.cs`

### Test

- `Assets/ProjectEta/Tests/EditMode/Day79SteamAchievementTests.cs`

신규 Unity Script에는 대응하는 `.meta` 파일을 함께 추가했다.

---

## 현재 확인 상태

원격 `main`의 최신 커밋은 아직 78일차다.

```text
78일차 : Steam Cloud 동기화 계층 구현 및 Runtime 자동 연동 보정
```

따라서 79일차 Achievement 작업은 아직 원격 저장소에 커밋되지 않은 상태를 기준으로 한다.

79일차 최종 덮어쓰기 파일에 대해 확인한 항목은 다음과 같다.

```text
ISteamAchievementBackend 구현 일치
NullSteamBackend Achievement fallback 존재
SteamPlatform Achievement Facade 존재
SteamPlatformService Achievement Capability 검사 존재
SteamworksNetBackend GetAchievement / SetAchievement / StoreStats 연결
RequestCurrentStats 제거
UserStatsReceived_t 의존성 제거
Achievement ID 5종 정의
Day79 EditMode 테스트 존재
```

현재 원격 최신 78일차 커밋에는 등록된 CI Status가 없다.

따라서 이번 개발 일지에서 확인한 범위는 GitHub 원격 코드와 79일차 덮어쓰기 파일의 정적 계약 확인까지다.

다음 항목은 로컬 Unity에서 최종 확인해야 한다.

- Unity Editor 전체 Script Compile
- Day77 / Day78 / Day79 EditMode Test Runner 실행
- 실제 Steam Client에서 Steam Runtime 초기화
- 실제 프로젝트 AppID 적용
- Steamworks Partner Achievement 등록 및 Publish
- 실제 게임 이벤트 연결 후 Achievement 팝업 확인
- 게임 재실행 후 Achievement 상태 유지 확인

---

## 다음 개발 방향

79일차에서 Steam Achievement Capability 기반을 구성했으므로 다음 단계에서는 Achievement를 실제 게임 이벤트에 연결하고 Steam 기능을 게임 UI와 통합하는 방향이 자연스럽다.

다음 작업의 우선 방향은 다음과 같다.

- 전투 승리 이벤트와 `FIRST_VICTORY` 연결
- 합성 성공 이벤트와 `FIRST_FUSION` 연결
- 5성 기물 생성과 `FIRST_FIVE_STAR` 연결
- 중간 보스 처치와 `MID_BOSS_CLEAR` 연결
- Run 클리어와 `FIRST_RUN_CLEAR` 연결
- MainMenu 또는 설정 화면에서 Steam 연결 상태 표시
- Steam Overlay / Cloud / Achievement 통합 동작 확인
- Steam 미사용 환경 fallback 회귀 테스트
- 실제 Steam AppID 기반 통합 QA

79일차에서는 Achievement 통신 계층을 완성하고, 이후 일차에서는 게임 콘텐츠 이벤트와 Steam 기능을 실제 플레이 흐름에 연결하는 단계로 확장한다.
