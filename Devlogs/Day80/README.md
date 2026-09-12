# 80일차 : Steam Achievement 게임 이벤트·MainMenu UI 통합

## 개발 목표

79일차에서 Steam Achievement Capability와 공통 Achievement ID를 구성한 뒤, 80일차에서는 해당 기능을 실제 게임 진행 이벤트와 MainMenu UI에 연결했다.

이번 작업의 핵심은 다음과 같다.

- 게임 이벤트를 Achievement ID로 변환하는 `SteamGameEventBridge` 추가
- 실제 전투 승리와 `FIRST_VICTORY` 연결
- Stage 5 승리와 `MID_BOSS_CLEAR` 연결
- Run 완료와 `FIRST_RUN_CLEAR` 연결
- 보유 카드 Pool 변화 기반 첫 합성 감지
- 5성 카드 보유 감지를 통한 `FIRST_FIVE_STAR` 연결
- Steam 준비 전 Achievement 요청 대기 Queue 추가
- MainMenu Steam 상태 패널 및 Overlay 버튼 추가
- Day80 Steam 통합 EditMode 테스트 추가
- 기존 Battle·Board·MainMenu 핵심 파일을 직접 수정하지 않는 자동 통합 구조 적용

80일차에서는 77~79일차에서 만든 Steam Runtime·Overlay·Cloud·Achievement 기능을 실제 게임 플레이와 UI에 연결하는 데 집중했다.

---

## SteamGameEventBridge 추가

게임 시스템에서 Steam Achievement API Name을 직접 다루지 않도록 게임 이벤트와 Achievement ID 사이에 별도 변환 계층을 추가했다.

현재 제공하는 주요 변환은 다음과 같다.

```text
일반 전투 승리
→ FIRST_VICTORY

Stage 5 승리
→ FIRST_VICTORY
→ MID_BOSS_CLEAR

합성 성공
→ FIRST_FUSION

5성 합성
→ FIRST_FUSION
→ FIRST_FIVE_STAR

5성 카드 획득
→ FIRST_FIVE_STAR

Run 완료
→ FIRST_RUN_CLEAR
```

게임 이벤트 처리부는 Steamworks.NET이나 Achievement API Name 문자열을 직접 알 필요 없이 `SteamGameEventBridge`를 통해 Achievement 요청을 등록한다.

---

## 중간 보스 Stage 기준

현재 중간 보스 Achievement 기준은 다음 상수로 관리한다.

```text
MidBossRound = 5
```

따라서 완료한 Stage가 5일 경우 다음 두 Achievement를 함께 Queue한다.

```text
FIRST_VICTORY
MID_BOSS_CLEAR
```

일반 Stage 승리에서는 `FIRST_VICTORY`만 Queue한다.

---

## SteamGameIntegrationController 추가

전체 게임 이벤트와 Steam Achievement를 연결하는 `SteamGameIntegrationController`를 추가했다.

이 Controller는 다음 방식으로 자동 생성된다.

```text
RuntimeInitializeOnLoadMethod
→ SteamGameIntegrationController 자동 생성
→ DontDestroyOnLoad
→ 씬 전환 동안 유지
```

기존 `BattleController`, `BoardInputController`, `MainMenuController`에 Steam 코드를 직접 삽입하지 않고 별도 통합 Controller가 기존 공개 상태와 이벤트를 감시한다.

이 방식으로 기존 게임 핵심 로직과 Steam 플랫폼 코드를 분리했다.

---

## BattleController 자동 연결

`SteamGameIntegrationController`는 현재 씬의 `BattleController`를 탐색한 뒤 다음 상태를 연결한다.

```text
BattleController
TurnManager
RunState
```

현재 연결 상태가 변경되면 이전 `TurnManager` 이벤트를 해제하고 새로운 전투 상태에 다시 연결한다.

따라서 새로운 Battle 씬 진입이나 전투 상태 재구성 이후에도 현재 `TurnManager`를 기준으로 Achievement 이벤트를 감시한다.

---

## 첫 전투 승리 Achievement 연결

`TurnManager.TurnChanged`에서 다음 조건을 확인한다.

```text
TurnState == BattleEnded
BattleOutcome == Victory
```

실제 승리가 확정된 이후 현재 완료 Stage를 읽고 `SteamGameEventBridge.QueueBattleVictory()`에 전달한다.

일반 전투 승리에서는 다음 Achievement가 등록된다.

```text
FIRST_VICTORY
```

Stage 5 승리에서는 추가로 다음 Achievement가 등록된다.

```text
MID_BOSS_CLEAR
```

입력 시점이나 전투 시작 시점이 아니라 실제 BattleOutcome이 Victory로 확정된 뒤 처리하도록 구성했다.

---

## Run 완료 Achievement 연결

전투 종료 이벤트가 발생하는 시점에는 `RunStageFlowService`가 아직 최종 Flow 상태를 갱신하기 전일 수 있다.

따라서 승리 직후 한 프레임을 기다린 뒤 다음 상태를 확인한다.

```text
RunFlowPhase.Completed
```

최종 Run 완료 상태가 확인되면 다음 Achievement를 Queue한다.

```text
FIRST_RUN_CLEAR
```

이를 통해 일반 전투 승리와 최종 Run 클리어를 같은 시점에 잘못 판정하지 않고 실제 Flow 완료 이후에 구분한다.

---

## Achievement 대기 Queue 추가

Steam Achievement 기능은 게임 이벤트가 발생하는 시점에 아직 준비되지 않았을 수 있다.

이를 위해 두 종류의 ID 집합을 관리한다.

```text
pendingAchievementIds
completedAchievementIds
```

게임 이벤트가 발생하면 먼저 `pendingAchievementIds`에 등록한다.

이후 Update에서 다음 조건을 만족할 때 대기 Achievement를 실제 Steam Backend에 전달한다.

```text
SteamPlatform.IsAchievementEnabled == true
pendingAchievementIds.Count > 0
```

해금이 성공한 ID는 대기 Queue에서 제거하고 `completedAchievementIds`에 기록한다.

같은 실행 세션에서 동일 Achievement 요청이 매 프레임 반복되는 것도 방지한다.

---

## 5성 카드 획득 자동 감지

현재 `RunState.Deck.OwnedCardPool`을 매 프레임 Snapshot으로 확인한다.

보유 카드 중 다음 등급이 존재하면 5성 Achievement를 Queue한다.

```text
PieceGrade.FiveStar
```

따라서 특정 Reward UI 하나에만 연결하지 않고 실제 보유 Pool에 최종적으로 카드가 들어오는 경로를 기준으로 판정한다.

현재 구조에서는 카드 보상, 상점 구매, 이벤트 카드 획득 등 `OwnedCardPool`에 반영되는 여러 카드 획득 경로에서 5성 보유 상태를 감지할 수 있다.

이미 Queue 또는 처리 완료 상태라면 중복 요청은 다시 추가하지 않는다.

---

## 합성 성공 Snapshot 판정

현재 기존 합성 시스템에 Steam 전용 완료 이벤트를 직접 추가하지 않고 보유 카드 Pool의 이전·현재 Snapshot 차이를 사용한다.

합성 판정 조건은 다음과 같다.

```text
이전 보유 카드 수 >= 2
현재 보유 카드 수 == 이전 보유 카드 수 - 1
이전 Snapshot에 없던 증가 카드가 존재
```

즉 다음 형태를 합성 성공 패턴으로 판단한다.

```text
재료 2장 제거
+
결과 1장 추가
=
전체 카드 수 1 감소
```

합성으로 판단되면 다음 Achievement를 Queue한다.

```text
FIRST_FUSION
```

추가된 결과 카드의 최고 등급이 5성이면 다음 Achievement도 함께 Queue한다.

```text
FIRST_FIVE_STAR
```

현재 구현은 기존 합성 핵심 파일을 직접 수정하지 않기 위한 통합 방식이며, 이후 합성 시스템에 명시적인 완료 이벤트가 추가되면 Snapshot 판정보다 해당 이벤트를 직접 연결하는 방식이 더 명확하다.

---

## Scene 전환 통합

`SteamGameIntegrationController`는 `SceneManager.sceneLoaded`를 구독한다.

새 씬이 로드되면 다음 작업을 수행한다.

```text
이전 Battle 이벤트 연결 해제
카드 Snapshot 초기화
MainMenu이면 Steam 상태 UI 확인
```

이를 통해 이전 씬의 `TurnManager` 이벤트가 남아 중복 호출되는 것을 방지한다.

---

## MainMenu Steam 상태 UI 추가

MainMenu 씬에 진입하면 `SteamMainMenuStatusUI`를 자동 생성한다.

별도 Prefab이나 Scene 수동 배치 없이 다음 구조가 Runtime에서 생성된다.

```text
SteamStatusCanvas_Day80
└─ SteamStatusPanel
   ├─ SteamStatusText
   └─ SteamOverlayButton
```

패널은 MainMenu 우측 상단에 표시되도록 구성했다.

---

## Steam 연결 상태 표시

Steam Runtime이 초기화되지 않은 경우 다음 상태를 표시한다.

```text
STEAM · OFFLINE
로컬 모드로 정상 실행 중
```

이 경우 Steam Overlay 버튼은 비활성화한다.

Steam Runtime이 초기화된 경우 현재 Backend와 기능 상태를 표시한다.

예시는 다음과 같다.

```text
STEAM · Steamworks.NET
Overlay ON  ·  Cloud ON  ·  Achievement ON
```

현재 표시하는 상태는 다음과 같다.

```text
BackendName
IsOverlayEnabled
IsCloudEnabled
IsAchievementEnabled
```

상태 UI는 약 0.5초 간격으로 갱신한다.

---

## MainMenu Steam Overlay 버튼

Steam 상태 패널에 다음 버튼을 추가했다.

```text
Steam Overlay 열기
```

Overlay가 사용 가능한 경우에만 버튼을 활성화한다.

버튼을 누르면 기존 Steam 전역 Facade를 사용한다.

```csharp
SteamPlatform.OpenOverlay();
```

따라서 MainMenu UI도 Steamworks.NET Backend를 직접 참조하지 않는다.

---

## 기존 핵심 파일 비수정 구조

이번 80일차에서는 기존 대형 게임 파일을 직접 수정하지 않았다.

직접 수정하지 않은 대표 파일은 다음과 같다.

```text
BattleController.cs
BoardInputController.cs
MainMenuController.cs
CardRewardController.cs
ShopService.cs
StageEventService.cs
```

대신 신규 Steam 통합 Component가 기존 공개 상태를 감시하는 구조를 사용했다.

이 방식은 Steam 플랫폼 통합 코드를 기존 게임 로직에서 분리하고, 80일차 변경 범위를 신규 파일 중심으로 제한하기 위한 것이다.

---

## Day80 EditMode 테스트 추가

`Day80SteamIntegrationTests`를 추가했다.

현재 테스트 항목은 다음과 같다.

- 일반 Stage 승리 시 `FIRST_VICTORY` Queue
- Stage 5 승리 시 `MID_BOSS_CLEAR` Queue
- 5성 합성 시 `FIRST_FUSION`과 `FIRST_FIVE_STAR` Queue
- 5성 카드 획득 시 `FIRST_FIVE_STAR` Queue
- Run 완료 시 `FIRST_RUN_CLEAR` Queue
- 재료 2장 → 결과 1장 형태를 합성 변화로 판정
- 카드 제거만 발생한 경우 합성으로 오판하지 않음

총 7개의 EditMode 테스트가 게임 이벤트 → Achievement ID 변환과 합성 Snapshot 판정을 검증한다.

---

## 현재 Achievement 연결 구조

80일차 기준 전체 흐름은 다음과 같다.

```text
게임 이벤트 발생
  ↓
SteamGameEventBridge
  ↓
Achievement API Name Queue
  ↓
Steam 준비 여부 확인
  ↓
미준비 → pendingAchievementIds 유지
  ↓
준비 완료
  ↓
SteamPlatform.UnlockAchievement
  ↓
SteamPlatformService
  ↓
ISteamAchievementBackend
  ↓
SteamworksNetBackend
  ↓
GetAchievement / SetAchievement / StoreStats
```

77~79일차 Steam 계층을 변경하지 않고 위에 게임 통합 계층을 추가한 형태다.

---

## MainMenu Steam 통합 구조

MainMenu에서는 다음 구조로 동작한다.

```text
MainMenu 씬 진입
  ↓
SteamGameIntegrationController
  ↓
SteamMainMenuStatusUI 자동 생성
  ↓
SteamPlatform 상태 조회
  ↓
Runtime / Overlay / Cloud / Achievement 상태 표시
  ↓
Overlay 사용 가능
  ↓
Steam Overlay 버튼 활성화
```

Steam이 없는 환경에서는 OFFLINE 상태를 표시하고 게임 자체 실행을 막지 않는다.

---

## 주요 변경 파일

### Steam 게임 이벤트 통합

- `Assets/ProjectEta/Scripts/Steam/SteamGameEventBridge.cs`
- `Assets/ProjectEta/Scripts/Steam/SteamGameIntegrationController.cs`

### MainMenu Steam UI

- `Assets/ProjectEta/Scripts/Steam/SteamMainMenuStatusUI.cs`

### Test

- `Assets/ProjectEta/Tests/EditMode/Day80SteamIntegrationTests.cs`

신규 Unity Script에는 각각 대응하는 `.meta` 파일을 함께 추가했다.

80일차 커밋은 총 8개 신규 파일로 구성되며 기존 파일 수정과 삭제는 없다.

---

## 현재 원격 저장소 상태

확인 시점의 원격 `main` 최신 커밋은 다음과 같다.

```text
SHA
965c30b4d92f26800ef635d8b8e90c3b1c4acae1

현재 커밋 제목
80
```

79일차 커밋 `129d67ff3807faf93c1e2c596c4c371bf8efc119` 대비 80일차 커밋은 1개 커밋 앞서 있으며, 다음 8개 파일이 신규 추가되었다.

```text
Assets/ProjectEta/Scripts/Steam/SteamGameEventBridge.cs
Assets/ProjectEta/Scripts/Steam/SteamGameEventBridge.cs.meta
Assets/ProjectEta/Scripts/Steam/SteamGameIntegrationController.cs
Assets/ProjectEta/Scripts/Steam/SteamGameIntegrationController.cs.meta
Assets/ProjectEta/Scripts/Steam/SteamMainMenuStatusUI.cs
Assets/ProjectEta/Scripts/Steam/SteamMainMenuStatusUI.cs.meta
Assets/ProjectEta/Tests/EditMode/Day80SteamIntegrationTests.cs
Assets/ProjectEta/Tests/EditMode/Day80SteamIntegrationTests.cs.meta
```

`Devlogs/Day80/README.md`는 확인 시점의 원격 `main`에는 아직 존재하지 않는다.

따라서 이번 개발 일지는 현재 80일차 커밋에 추가한 뒤 해당 커밋을 `--amend`하는 흐름을 기준으로 한다.

---

## 현재 확인 상태

GitHub 원격의 80일차 커밋과 79일차 커밋 사이 변경 내용을 확인했다.

정적으로 확인한 항목은 다음과 같다.

```text
80일차 커밋이 79일차보다 정확히 1개 커밋 앞선 상태
신규 파일 8개
기존 파일 수정 없음
파일 삭제 없음
SteamGameEventBridge 존재
SteamGameIntegrationController 존재
SteamMainMenuStatusUI 존재
Day80SteamIntegrationTests 존재
Day80 EditMode 테스트 7개 정의
FIRST_VICTORY 연결
FIRST_FUSION 연결
FIRST_FIVE_STAR 연결
MID_BOSS_CLEAR 연결
FIRST_RUN_CLEAR 연결
Steam 미준비 Achievement 대기 Queue 존재
MainMenu Steam 상태 표시 존재
MainMenu Overlay 버튼 존재
```

현재 최신 커밋에는 GitHub Commit Status가 등록되어 있지 않다.

따라서 원격 코드의 구조와 변경 파일은 확인했지만 다음 항목은 GitHub 상태만으로 성공 여부를 확인할 수 없다.

- Unity Editor 전체 Script Compile
- Day77~Day80 EditMode Test Runner 전체 실행
- MainMenu 실제 UI 위치와 해상도별 표시 확인
- 실제 Steam Client Overlay 호출 확인
- 실제 Steam Cloud 상태 표시 확인
- 실제 Steam Achievement 해금 확인
- 프로젝트 실제 AppID에서 Achievement Publish 상태 확인

현재 정적 검토에서는 개발 일지 생성을 막을 명확한 코드 문제는 확인되지 않았다.

---

## 현재 구조의 주의점

합성 Achievement는 현재 명시적인 합성 완료 이벤트가 아니라 `OwnedCardPool` Snapshot 차이로 판정한다.

따라서 향후 다른 시스템에서 다음과 같은 변화가 한 프레임에 동시에 발생하면 합성으로 오판할 가능성이 있다.

```text
전체 카드 수 1 감소
+
새 카드 종류 증가
```

현재 게임 구조에서는 합성 패턴을 직접 수정하지 않고 연결하기 위한 방법이지만, 합성 시스템에 안정적인 `FusionCompleted` 이벤트를 추가할 수 있는 시점에는 해당 이벤트를 직접 연결하는 방식이 더 정확하다.

또한 MainMenu 상태 UI는 Runtime 생성 Canvas이므로 최종 UI 디자인 단계에서는 기존 MainMenu 디자인 시스템에 직접 포함시키는 방식으로 조정할 수 있다.

---

## Steamworks Partner에서 확인할 항목

실제 Steam 계정에 Achievement가 기록되려면 프로젝트 App Admin에 다음 API Name이 등록되어 있어야 한다.

| API Name | 용도 |
| --- | --- |
| `FIRST_VICTORY` | 첫 전투 승리 |
| `FIRST_FUSION` | 첫 합성 성공 |
| `FIRST_FIVE_STAR` | 첫 5성 기물 획득 |
| `MID_BOSS_CLEAR` | 중간 보스 격파 |
| `FIRST_RUN_CLEAR` | 첫 Run 클리어 |

API Name은 코드와 대소문자까지 동일해야 한다.

현재 개발 기본 AppID `480`은 Spacewar 개발용 App이므로 프로젝트 η 전용 Achievement 실연동 검증에는 프로젝트의 실제 Steam AppID와 Steamworks Partner 설정이 필요하다.

---

## 다음 개발 방향

80일차에서 Steam Achievement를 실제 게임 이벤트와 MainMenu UI까지 연결했으므로 다음 단계에서는 Steam 기능 자체를 추가하기보다 실제 플레이 환경에서 전체 플랫폼 통합을 검증하고 UI·Navigation을 정리하는 방향이 적합하다.

다음 작업의 우선 방향은 다음과 같다.

- Steam Runtime / Overlay / Cloud / Achievement 통합 QA
- MainMenu Steam 상태 UI를 기존 디자인 시스템과 정식 통합
- Steam UI Keyboard·Gamepad Navigation 확인
- Steam 미실행 / Offline 상태 회귀 테스트
- 실제 프로젝트 AppID 적용 테스트
- 실제 Steam Client Achievement 팝업 확인
- Cloud 저장 업로드·복원 실연동 검증
- 합성 완료 명시 이벤트 도입 여부 검토
- 전체 UI Navigation과 Steam 기능 충돌 확인

80일차에서는 Steam Achievement 기반 기능을 실제 게임 이벤트 및 MainMenu UI까지 연결했고, 이후 단계에서는 실제 Steam Client 기반 통합 QA와 플랫폼 마무리 작업을 진행한다.
