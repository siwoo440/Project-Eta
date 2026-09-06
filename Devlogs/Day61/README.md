# 61일차 : King 전투 HUD 정식화 및 승리·패배 버튼 복구

## 개발 목표

60일차에서 정식화한 King 선택 캐러셀과 전투 중 King 상태 표시 책임을 분리하고, 선택한 King의 현재 전투 상태를 지속적으로 확인할 수 있는 전용 HUD를 구현한다.

추가로 개발·진행 확인에 사용하던 승리 / 패배 버튼을 다시 복구하고 기존 BattleController 전투 종료 흐름에 연결한다.

61일차의 핵심 목표는 다음과 같다.

- King 선택 UI와 전투 상태 HUD 역할 분리
- 현재 King 이름 및 HP 표시
- King 타입별 패시브 상태 표시
- 공격형 King 격노 상태 표시
- 방어형 King 방벽 및 이동 상태 표시
- 전략형 King 전술적 준비 상태 표시
- 기본 King 상태 표시
- Battle 중 King이 실제 배치된 상태에서만 HUD 표시
- 전투 종료 및 비전투 흐름에서 King HUD 숨김
- 승리 / 패배 버튼 복구
- 승패 버튼을 기존 BattleController.EndBattle 흐름에 연결
- SceneRuntimeBootstrap에서 61일차 런타임 UI 자동 주입

## KingCombatHudPresentation

전투 HUD에 표시할 King 정보를 UI 생성 코드와 분리하기 위해 `KingCombatHudPresentation`을 추가했다.

현재 표시 정보는 다음과 같다.

- King 이름
- 현재 HP
- 패시브 이름
- 핵심 패시브 상태
- 보조 패시브 상태

현재 KingRunState와 RunState의 King HP를 읽어 King 타입에 맞는 표시 데이터를 생성한다.

## 공격형 King HUD

공격형 King은 `처형의 연쇄` 상태를 표시한다.

격노는 최대 2칸을 점 형태로 표시한다.

예시:

`격노  ○ ○`

`격노  ● ○`

`격노  ● ●`

격노가 존재하면 다음 King 공격의 보너스 ATK도 표시한다.

예시:

`다음 King 공격 ATK +1`

`다음 King 공격 ATK +2`

격노가 없으면 다음 공격 보너스가 없음을 표시한다.

## 방어형 King HUD

방어형 King은 `왕의 요새` 상태를 표시한다.

방벽이 없을 때는 비활성 상태를 표시한다.

`방벽  ◇ 비활성`

방벽이 존재하면 활성 상태로 표시한다.

`방벽  ◆ 활성`

또한 현재 플레이어 턴에 King이 이동했는지를 함께 표시한다.

King이 이동하지 않았다면 턴 종료 시 방벽을 준비할 수 있다는 정보를 표시한다.

King이 이동했다면 이번 턴에는 방벽을 새로 획득할 수 없다는 정보를 표시한다.

## 전략형 King HUD

전략형 King은 `전술적 준비` 상태를 표시한다.

배치 턴이 아닐 때는 다음 배치 턴에 발동할 패시브임을 표시한다.

배치 턴에서는 전술적 준비 상태를 표시한다.

실제 카드 선택이 진행 중이면 다음과 같이 표시한다.

`전술적 준비 · 카드 선택 중...`

카드 선택 완료 전에는 배치 입력이 대기 중임을 함께 표시한다.

## 기본 King HUD

기본 King은 별도의 특수 패시브 상태가 없으므로 다음 정보를 간단히 표시한다.

- 기본 King 이름
- 현재 HP
- 기본형 패시브 이름
- 특수 패시브 없음
- 기본 King 규칙으로 전투

## KingCombatHUD

`KingCombatHUD`를 추가해 실제 전투 화면의 King 상태 표시를 담당하도록 했다.

HUD는 좌측 상단에 고정 표시한다.

현재 표시 조건은 다음과 같다.

- 현재 RunFlowPhase가 Battle
- TurnManager가 존재
- 최초 King이 실제 보드에 배치됨
- BattleEnded 상태가 아님

따라서 King 선택 중에는 전투 HUD를 표시하지 않는다.

Map / Reward / Shop / Event 상태에서도 표시하지 않는다.

전투가 종료되면 자동으로 숨긴다.

## KingSelectionUI 역할 분리

60일차 `KingSelectionUI`는 King 선택과 전투 중 상태 표시를 함께 담당하고 있었다.

61일차에서는 전투 상태 표시를 `KingCombatHUD`로 이동했다.

`KingSelectionUI`는 다음 역할만 담당한다.

- 최초 King 캐러셀 선택
- 잠금 상태 확인
- `이 King으로 시작` 확정
- 확정 후 최초 배치 안내

King을 확정한 뒤 실제로 보드에 배치하기 전에는 다음 안내를 유지한다.

`보드에 King을 배치하세요`

실제 King이 배치되고 전투가 시작되면 상태 표시는 `KingCombatHUD`가 담당한다.

## 승리 / 패배 버튼 복구

`BattleOutcomeDebugUI`를 추가해 전투 화면 우측 상단에 승리 / 패배 버튼을 다시 제공한다.

현재 버튼은 다음 기존 전투 종료 함수를 직접 사용한다.

승리:

`BattleController.EndBattle(BattleOutcome.Victory)`

패배:

`BattleController.EndBattle(BattleOutcome.Defeat)`

별도의 임시 승패 상태를 만들지 않는다.

따라서 기존 BattleController가 사용하는 TurnManager 종료 처리와 RunStageFlowService 전투 완료 흐름을 그대로 사용한다.

## 승패 버튼 표시 조건

승리 / 패배 버튼은 실제 Battle 진행 중에만 표시한다.

다음 조건에서는 숨긴다.

- RunState 없음
- TurnManager 없음
- 현재 Flow가 Battle이 아님
- 이미 BattleEnded 상태
- BattleOutcome이 이미 결정됨

버튼을 누르면 즉시 두 버튼의 입력을 비활성화해 같은 프레임의 중복 승패 처리를 막는다.

## SceneRuntimeBootstrap 확장

Battle Scene의 런타임 자동 초기화에 61일차 기능을 추가했다.

King 런타임 호스트에는 다음 컴포넌트를 보장한다.

- KingAbilityController
- KingSelectionUI
- StrategyKingSelectionUI
- KingCombatHUD

또한 Battle 런타임 구성에서 `BattleOutcomeDebugUI`를 자동 생성한다.

따라서 별도의 Scene 또는 Prefab 직렬화 수정 없이 Boot → MainMenu → Battle 경로에서도 동일한 UI 구성이 생성된다.

## 테스트 소스

`Day61KingCombatHudTests`를 추가했다.

현재 테스트 소스에는 다음 검증 항목이 포함된다.

- 공격형 King 격노 2스택 표시
- 공격형 다음 공격 ATK +2 표시
- 방어형 방벽 활성 표시
- 방어형 이동 안 함 상태 표시
- 전략형 전술적 준비 카드 선택 중 표시
- KingSelectionUI에서 기존 전투 상태 텍스트 제거
- King 배치 안내 유지
- 승리 버튼의 Victory 종료 흐름 연결
- 패배 버튼의 Defeat 종료 흐름 연결
- SceneRuntimeBootstrap의 KingCombatHUD 주입
- SceneRuntimeBootstrap의 BattleOutcomeDebugUI 주입

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/King/KingCombatHUD.cs`
- `Assets/ProjectEta/Scripts/King/KingCombatHudPresentation.cs`
- `Assets/ProjectEta/Scripts/UI/BattleOutcomeDebugUI.cs`
- `Assets/ProjectEta/Tests/EditMode/Day61KingCombatHudTests.cs`

각 신규 C# 파일에 대응하는 `.meta` 파일도 함께 추가했다.

### 수정

- `Assets/ProjectEta/Scripts/King/KingSelectionUI.cs`
- `Assets/ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs`

### 삭제

없음.

## 결과

61일차 작업으로 King 선택 화면과 전투 상태 표시가 서로 분리됐다.

King 선택은 60일차 캐러셀 UI가 계속 담당하고, 실제 King 배치 이후에는 별도의 `KingCombatHUD`가 현재 HP와 패시브 상태를 지속적으로 표시한다.

공격형은 격노와 다음 공격 보너스, 방어형은 방벽과 현재 턴 이동 여부, 전략형은 전술적 준비 진행 상태를 같은 HUD 레이아웃 안에서 확인할 수 있다.

또한 승리 / 패배 버튼이 복구되어 개발 중 전투 결과 흐름을 직접 진행할 수 있으며 두 버튼 모두 기존 `BattleController.EndBattle()` 경로를 사용한다.

## 검증 상태

최신 GitHub `main`의 61일차 커밋은 이전 60일차 커밋보다 1개 커밋 앞서 있으며 다음 10개 변경 파일을 포함하는 것을 확인했다.

- `KingCombatHUD.cs`
- `KingCombatHUD.cs.meta`
- `KingCombatHudPresentation.cs`
- `KingCombatHudPresentation.cs.meta`
- `KingSelectionUI.cs`
- `SceneRuntimeBootstrap.cs`
- `BattleOutcomeDebugUI.cs`
- `BattleOutcomeDebugUI.cs.meta`
- `Day61KingCombatHudTests.cs`
- `Day61KingCombatHudTests.cs.meta`

최신 커밋에서 Day61 테스트가 사용하는 `KingRunState`의 `TryAddRage`, `BeginPlayerTurn`, `TryGainBarrier`, `TryBeginStrategyPreparation` API가 실제 존재하는 것도 확인했다.

GitHub commit status에는 연결된 상태 검사가 등록되어 있지 않다.

따라서 최신 커밋의 변경 파일과 코드 연결은 확인했지만 Unity Editor 컴파일 및 EditMode TestRunner 전체 통과 여부는 GitHub 상태만으로 확인할 수 없다.
