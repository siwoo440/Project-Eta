# 62일차 : Battle HUD 정식화·전투 UI 정리 및 시작 덱 구성 확정

## 개발 목표

62일차에서는 전투 중 항상 확인해야 하는 공통 정보를 상단 Battle HUD로 정식화하고, 이전 개발 단계에서 남아 있던 중복·디버그 UI를 정리한다.

추가로 새 런 시작 시 사용하는 기본 카드 구성을 실제 체스 기본 편성에 가까운 고정 수량으로 확정한다.

핵심 목표는 다음과 같다.

- Battle 전용 상단 공통 HUD 추가
- 현재 Stage 및 Boss Stage 표시
- 현재 Turn 상태 표시
- 현재 Turn Number / Limit 표시
- 현재 Gold 표시
- 최초 배치 및 주기 배치 안내 표시
- 다음 Deployment까지 남은 Turn 표시
- Battle 외 Run 흐름에서 Battle HUD 숨김
- 43일차 구형 승리 / 패배 버튼 중복 노출 차단
- 61일차 좌측 King 전투 HUD 제거
- BoardInputController 좌측 상단 개발용 IMGUI 제거
- SceneRuntimeBootstrap에서 BattleHUD 자동 생성
- 새 런 시작 기본 카드 풀 16장 구성
- 시작 덱 카드 수량 EditMode 테스트 보강

이번 일차에서는 손패 상세 디자인, 카드 상세 패널, 기물 상세 UI, 이동·공격·배치 타일 정식화는 확장하지 않는다.

## BattleHudPresentation

전투 HUD에 표시할 문구 계산을 실제 Unity UI 생성 코드와 분리하기 위해 `BattleHudPresentation`을 추가했다.

담당 정보는 다음과 같다.

- Stage 문구
- Turn 상태 문구
- Turn Number / Limit 문구
- Gold 문구
- Deployment 안내
- 다음 Deployment까지 남은 Turn 계산

### Stage 표시

일반 Stage:

`STAGE 1 / 10`

중간 보스 Stage:

`STAGE 5 / 10 · MID BOSS`

최종 보스 Stage:

`STAGE 10 / 10 · FINAL BOSS`

Stage 값은 `RoundState.FirstRound`와 `RoundState.FinalRound` 범위 안에서 보정한다.

### Turn 상태 표시

`TurnManager.CurrentState`와 `IsInitialDeployment`를 사용해 다음 상태를 표시한다.

- `INITIAL DEPLOYMENT`
- `DEPLOYMENT`
- `PLAYER TURN`
- `ENEMY TURN`
- `BATTLE ENDED`

Turn Limit을 확인할 수 있는 경우:

`TURN 1 / 30`

Turn Limit을 확인할 수 없는 경우:

`TURN 1`

### Gold 표시

현재 Gold는 `RunEconomyService.TryGet()`을 통해 현재 Run의 `RunEconomyState.Currency`를 조회한다.

예시:

`GOLD 100`

경제 상태가 아직 등록되지 않은 신규 Run에서는 `RunEconomyRules.StartingCurrency`를 기본 표시값으로 사용한다.

### Deployment 표시

최초 배치:

`King과 기물을 배치하세요`

주기 배치:

`배치한 카드 2`

일반 전투 턴:

`NEXT DEPLOYMENT 4`

현재 턴 종료 후 바로 배치로 진입하는 경우:

`NEXT DEPLOYMENT THIS TURN`

전투 종료 상태에서는 Deployment 안내를 숨긴다.

## BattleHUD

`BattleHUD`를 추가해 전투 화면 상단의 공통 정보 표시를 담당하도록 했다.

런타임에는 `BattleHUDCanvas_Day62`를 생성한다.

현재 표시 정보는 다음과 같다.

- Stage
- Battle 상태
- 현재 Turn 상태
- Turn Number / Limit
- Gold
- Deployment 상태
- 다음 Deployment까지 남은 Turn

`RunState.CurrentFlowPhase == RunFlowPhase.Battle`일 때만 HUD를 표시한다.

Map / Reward / Shop / Event 등 Battle이 아닌 Run 흐름에서는 자동으로 숨긴다.

`BattleHUD`는 게임 상태를 별도로 보관하거나 변경하지 않고 기존 `BattleController`, `RunState`, `TurnManager`, `RunEconomyState` 값을 읽어 표시하는 역할만 담당한다.

## Turn Limit 표시

현재 Turn Limit은 기존 `BattleController`의 비공개 직렬화 필드 `_turnLimitTestValue`를 Reflection으로 조회한다.

별도의 HUD 전용 턴 제한 상태는 추가하지 않는다.

필드를 찾을 수 없거나 값이 유효하지 않은 경우에는 현재 Turn Number만 표시한다.

## 기존 승리 / 패배 버튼 정리

61일차의 `BattleOutcomeDebugUI`를 현재 개발용 승리 / 패배 버튼으로 유지한다.

43일차의 `DebugBattleResultButtons`가 동시에 존재하는 경우 `BattleHUD`가 구형 버튼 호스트를 비활성화하고 제거한다.

따라서 Battle Scene에서 승리 / 패배 개발 버튼이 중복 표시되지 않도록 정리했다.

## 기존 King 전투 HUD 제거

61일차 좌측 `KingCombatHUD`는 62일차 화면 정리 과정에서 런타임 표시 대상에서 제외했다.

`SceneRuntimeBootstrap.EnsureKingRuntime()`에서 다음을 처리한다.

- 기존 `KingCombatHUD` 제거
- 기존 `KingCombatHUDCanvas_Day61` 제거
- 신규 King 런타임에 `KingCombatHUD`를 추가하지 않음

King 시스템 자체는 유지한다.

계속 유지되는 런타임은 다음과 같다.

- `KingAbilityController`
- `KingSelectionUI`
- `StrategyKingSelectionUI`

## 전투 디버그 IMGUI 제거

`BoardInputController.OnGUI()`에서 화면 좌측 상단에 표시하던 개발용 IMGUI를 비활성화했다.

기존 표시 대상은 다음과 같았다.

- Player Hand
- Draw / Dead pile
- Enemy Hand / Draw / Dead pile
- Turn 조작 안내
- 선택 기물 Debug 상태

현재 `OnGUI()`는 즉시 반환해 화면에 해당 디버그 정보를 그리지 않는다.

턴 입력, 카드 입력, 선택 기물 처리 등 실제 전투 로직은 유지한다.

## 새 런 시작 덱 구성 확정

새 런을 시작할 때 `BoardInputController.EnsurePrototypeStartingHand()`가 카드 상태가 비어 있는 경우에만 기본 카드 풀을 구성한다.

기본 카드 구성은 다음과 같다.

| 카드 | 수량 |
| --- | ---: |
| King | 1 |
| Pawn | 8 |
| Rook | 2 |
| Bishop | 2 |
| Queen | 1 |
| Knight | 2 |
| **합계** | **16** |

사용자가 지정한 일반 기물 카드는 총 15장이며, 초기 배치 진행에 필요한 King 1장을 기존 규칙대로 함께 유지한다.

카드 상태가 이미 존재하면 저장 복원 또는 진행 중인 Run으로 판단해 기본 카드가 다시 추가되지 않는다.

초기화 순서는 다음과 같다.

1. 기본 카드 16장을 `OwnedCardPool`에 등록
2. `OwnedCardPool`을 기준으로 DrawPile 재구성 및 셔플
3. King 1장을 반드시 시작 손패로 이동
4. 나머지 카드를 랜덤 드로우해 시작 손패를 총 5장까지 구성
5. 나머지 11장은 DrawPile에 유지

따라서 정상적인 새 런 직후 카드 상태는 다음과 같다.

- OwnedCardPool: 16장
- Hand: 5장
- DrawPile: 11장
- 시작 손패에 King 필수 포함

## Day62StartingDeckPatcher

기존 프로젝트에 6종 1장씩으로 구성된 프로토타입 시작 카드 배열이 남아 있는 경우를 대비해 Editor용 `Day62StartingDeckPatcher`를 추가했다.

패처는 다음 두 파일을 대상으로 한다.

- `BoardInputController.cs`
- `CardFlowTests.cs`

현재 최신 `main`에는 이미 시작 덱 변경 마커와 테스트 변경이 적용되어 있으므로 정상 상태에서는 패처가 다시 소스를 수정하지 않고 종료한다.

## CardFlowTests 보강

기존 `EnsurePrototypeStartingHand_AlwaysContainsKing` 테스트를 시작 덱 수량 검증까지 확장했다.

현재 검증 항목은 다음과 같다.

- 시작 손패에 King 존재
- 시작 손패 5장
- OwnedCardPool 16장
- DrawPile 11장
- Pawn 8장
- Rook 2장
- Bishop 2장
- Queen 1장
- Knight 2장

이를 통해 시작 덱 수량이 변경되거나 누락되는 회귀를 EditMode 테스트에서 확인할 수 있게 했다.

## SceneRuntimeBootstrap 확장

Battle Scene 런타임 자동 초기화에 `BattleHUD` 생성을 추가했다.

Battle 진입 시 다음 컴포넌트를 보장한다.

`BattleHUD_Day62`

King 런타임 구성 단계에서는 기존 좌측 King 전투 HUD를 제거하고 King 능력 및 선택 관련 시스템만 유지한다.

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Editor.meta`
- `Assets/ProjectEta/Editor/Day62HudCleanupPatcher.cs`
- `Assets/ProjectEta/Editor/Day62HudCleanupPatcher.cs.meta`
- `Assets/ProjectEta/Editor/Day62StartingDeckPatcher.cs`
- `Assets/ProjectEta/Editor/Day62StartingDeckPatcher.cs.meta`
- `Assets/ProjectEta/Scripts/UI/BattleHUD.cs`
- `Assets/ProjectEta/Scripts/UI/BattleHUD.cs.meta`
- `Assets/ProjectEta/Scripts/UI/BattleHudPresentation.cs`
- `Assets/ProjectEta/Scripts/UI/BattleHudPresentation.cs.meta`

### 수정

- `Assets/ProjectEta/Scripts/Board/BoardInputController.cs`
- `Assets/ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs`
- `Assets/ProjectEta/Tests/EditMode/CardFlowTests.cs`
- `Devlogs/Day62/README.md`

### 삭제

없음.

## 결과

62일차 작업으로 전투 화면의 공통 정보를 한곳에서 확인할 수 있는 상단 `BattleHUD`가 정식화됐다.

Stage, Turn 상태, Turn Number / Limit, Gold, Deployment 상태 및 다음 Deployment까지 남은 Turn을 표시하며 Battle 흐름 외에서는 자동으로 숨긴다.

과거 개발 단계에서 남아 있던 좌측 King HUD와 BoardInputController 디버그 IMGUI를 화면에서 제거하고, 구형 승리 / 패배 버튼의 중복 노출도 차단했다.

추가로 새 런의 기본 카드 구성을 King 1장과 기본 체스 기물 15장으로 확정했다.

최종 시작 카드 풀은 총 16장이며, 시작 손패는 King을 포함한 5장, 나머지 11장은 DrawPile에서 순환한다.

## 검증 상태

2026-09-07 기준 GitHub `main` 최신 커밋:

- SHA: `7746538aacd4faa42e22eb95ca3ded6bc8f2c9ce`
- 메시지: `62일차 : Battle HUD 기본 정식화 및 기존 전투 UI 정리`
- 부모 일차: 61일차

정적 코드 확인 결과 다음 사항이 반영되어 있다.

- `SceneRuntimeBootstrap`에서 `BattleHUD` 자동 생성
- `BattleHUD`에서 Stage / Turn / Gold / Deployment 정보 표시
- Battle 이외 Run Flow에서 HUD 숨김
- `DebugBattleResultButtons` 중복 차단
- 기존 `KingCombatHUD` 런타임 제거
- `BoardInputController.OnGUI()` 개발용 출력 비활성화
- 새 런 시작 카드 풀 16장 구성
- King 필수 시작 손패 유지
- 초기 Hand 5장 / DrawPile 11장 구조
- Pawn 8 / Rook 2 / Bishop 2 / Queen 1 / Knight 2 수량 검증
- `CardFlowTests`에 시작 덱 회귀 검증 추가
- Editor 패처의 중복 적용 방지 마커 존재

GitHub commit status에는 연결된 상태 검사가 등록되어 있지 않다.

따라서 저장소의 코드 및 테스트 소스 연결은 확인했지만 Unity Editor 실제 컴파일과 EditMode / PlayMode TestRunner 실행 결과는 GitHub 상태만으로 확인할 수 없다.
