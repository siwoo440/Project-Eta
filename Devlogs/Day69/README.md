# 69일차 : 전체 UI 통합 안정화·동적 조작키 연동 및 조작법 정리

## 개발 목표

69일차에서는 56~68일차에 개별적으로 구축한 UI들을 하나의 런 흐름 안에서 안정적으로 함께 동작하도록 정리했다.

이번 일차의 중심은 다음과 같다.

- 공통 UI Canvas 계층 규칙 정리
- Pause·Tutorial·Run Result·Reward·Battle Announcement·System Toast 간 표시 우선순위 통합
- Modal UI가 열려 있을 때 뒤쪽 시스템 알림 표시 제어
- Pause 진입 가능 상태 규칙 통합
- 개발용 승리·패배 UI의 Development Build 제한
- 조작키 설정 데이터를 실제 입력과 조작법에 함께 연결
- 조작법의 키 열과 설명 열 정렬
- 설정 저장 버전 v4 및 조작키 마이그레이션
- Day69 EditMode 회귀 테스트 추가
- EditMode 테스트 어셈블리의 Unity Input System 참조 정리

## 공통 UI 계층 정리

각 UI가 개별 숫자로 `Canvas.sortingOrder`를 관리하던 구조를 `UiLayerOrder`로 통합했다.

주요 계층은 다음 개념으로 정리했다.

- Battle HUD
- Reward·Activity UI
- Run Result
- Battle Announcement
- Pause
- Tutorial
- System Toast

이를 통해 새로운 UI를 추가할 때 기존 Canvas의 숫자를 다시 찾아 비교하지 않고 공통 계층 규칙을 참조할 수 있도록 했다.

## UI 표시 규칙 통합

`Day69UiPresentationRules`를 추가해 현재 UI 상태에 따라 다른 화면이 표시되거나 입력을 받을 수 있는지 판단하도록 구성했다.

주요 규칙은 다음과 같다.

- Tutorial이 열려 있을 때 Pause 신규 진입 차단
- Run Result가 열려 있을 때 Pause 신규 진입 차단
- Reward·Shop·Event·Pause·Tutorial·Run Result가 표시되는 동안 System Toast 표시 보류
- Battle Announcement가 표시되는 동안 System Toast 표시 보류
- 개발용 UI는 Editor 또는 Development Build에서만 생성

이 규칙은 개별 UI를 강제로 켜고 끄는 별도 Guard를 추가하는 방식이 아니라, 각 UI 소유자가 공통 판정 결과를 참조하도록 구성했다.

## System Toast 표시 안정화

저장·불러오기 알림인 `SystemToastUI`가 Modal UI와 겹치지 않도록 표시 조건을 정리했다.

Toast 표시 중 새로운 Modal이 열리는 경우 현재 알림을 무조건 폐기하지 않고 잠시 숨긴 뒤, Modal이 닫히면 남은 표시 시간을 이어서 사용할 수 있도록 구성했다.

따라서 다음 화면에서 시스템 알림이 내용 위를 가리지 않는다.

- Reward
- Shop
- Event
- Pause
- Tutorial
- Run Result

## Pause 표시와 입력 정리

기존 68일차 Pause 구조를 유지하면서 69일차 UI 상태 규칙을 연결했다.

Pause는 Tutorial·Run Result와 같은 상위 Modal이 열려 있지 않을 때만 새로 진입할 수 있다.

기존 ESC 하드코딩도 제거하고 현재 저장된 Pause 조작키를 통해 입력을 받도록 변경했다.

## 개발용 결과 버튼 제한

`DebugBattleResultButtons`는 런타임 Bootstrap에서 무조건 생성하지 않고 다음 조건에서만 생성하도록 변경했다.

- Unity Editor
- Development Build

일반 Release Build에서는 개발용 Victory/Defeat 버튼이 플레이 화면에 나타나지 않도록 정리했다.

## 조작키 설정 기반 추가

조작법 문구와 실제 입력이 서로 다른 값을 사용하지 않도록 `GameInputBindingService`를 추가했다.

현재 공통 관리 대상은 다음과 같다.

- 배치 턴 종료 / 플레이어 행동 완료
- Pause / UI 뒤로가기

기본값은 다음과 같다.

- Complete Action: `Space`
- Pause: `Escape`

실제 `BattleController`는 더 이상 Space 키를 직접 확인하지 않고 현재 설정에 저장된 Complete Action 키를 사용한다.

Pause 역시 ESC를 직접 확인하지 않고 저장된 Pause 키를 사용한다.

## 설정 저장 버전 v4

조작키 설정을 저장하기 위해 `GameSettingsData.CurrentVersion`을 4로 변경했다.

추가된 설정값:

- `CompleteActionKey`
- `PauseKey`

v3 이하의 기존 설정 데이터는 조작키 정보가 존재하지 않는 것으로 판단해 기본 키로 마이그레이션한다.

잘못된 키 문자열이 저장된 경우에도 기본값으로 복원할 수 있도록 정규화 규칙을 추가했다.

## 조작키 편집 상태

`GameSettingsEditState`에 조작키 변경 기능을 추가했다.

설정 화면에서 조작키를 변경하면 현재 편집 설정의 키 값이 갱신되고, 기존 설정 적용 흐름을 통해 런타임 조작키에도 반영할 수 있도록 구성했다.

Controls 카테고리를 초기화하면 행동 완료와 Pause 키도 기본값으로 돌아간다.

## 조작법 동적 표시

Pause의 조작법 화면에 고정 문자열로 작성되어 있던 `Space`, `ESC` 표기를 제거했다.

조작법 진입 시 현재 설정값을 읽어 다음 위치에 동적으로 반영한다.

- 행동 완료 / 배치 종료
- 일시정지
- UI 뒤로가기

예를 들어 Complete Action 키가 `Space`에서 `R`로 변경되면 조작법 화면도 `R`로 표시할 수 있는 구조다.

최초 튜토리얼 내부에서 행동 완료 키와 Pause 키를 안내하는 문장도 같은 설정 데이터를 사용하도록 연결했다.

## 조작법 2열 정렬

기존 조작법은 하나의 Text 안에서 공백 개수로 키와 설명 간격을 맞추고 있었다.

69일차에서는 각 행을 다음 세 요소로 분리했다.

- 키 전용 열
- 구분점 열
- 설명 전용 열

키 열은 우측 정렬하고 설명 열은 동일한 X 좌표에서 시작하도록 배치했다.

따라서 `R`, `Space`, `ESC`, `마우스 이동`처럼 키 이름의 길이가 달라도 오른쪽 설명 문장의 시작 위치가 동일하게 유지된다.

## Battle 안내 문구 동기화

전투 시작 배치와 주기 배치 로그 역시 `Space`를 직접 문자열로 사용하지 않도록 수정했다.

현재 설정된 Complete Action 키 표시명을 조회해 다음 안내에 반영한다.

- 초기 King 배치 안내
- 초기 자유 배치 안내
- 주기 배치 턴 종료 안내

## 테스트 구조 정리

Day69 UI 통합과 조작키 설정을 검증하는 EditMode 테스트를 추가했다.

주요 검증 대상:

- UI 계층 순서
- Modal 상태에 따른 System Toast 표시 가능 여부
- Pause 진입 가능 여부
- 개발용 UI 생성 조건
- v3 → v4 조작키 마이그레이션
- 유효한 변경 키 유지
- 잘못된 키의 기본값 복구
- 키 표시 문자열 변환
- 조작키 편집 상태
- 런타임 입력과 조작법 표시의 동일 설정 사용

조작키 테스트가 `UnityEngine.InputSystem.Key`를 직접 사용하므로 EditMode 테스트 asmdef에 `Unity.InputSystem` 참조를 명시적으로 추가했다.

## 삭제·재추가 복구

69일차 중간 정리 과정에서 다음 본체 파일들이 일시적으로 삭제된 상태가 확인됐다.

- `GameSettingsData.cs`
- `GameSettingsEditState.cs`
- `GameInputBindingService.cs`
- `Day69InputBindingTests.cs`
- `ProjectEta.Tests.EditMode.asmdef`

`.meta` 파일은 유지한 상태에서 최신 Day69 규격 파일을 동일 경로에 다시 추가했다.

최종 상태에서는 다음 구조가 다시 존재한다.

- `GameSettingsData.SettingsVersion`
- `GameSettingsData.Normalized()`
- `GameSettingsEditState.SetControlKey()`
- `GameSettingsEditState.Apply()`
- `GameInputBindingService`
- `Day69InputBindingTests`
- `ProjectEta.Tests.EditMode`의 `Unity.InputSystem` 참조

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Settings/GameInputBindingService.cs`
- `Assets/ProjectEta/Scripts/UI/Day69UiPresentationRules.cs`
- `Assets/ProjectEta/Scripts/UI/UiLayerOrder.cs`
- `Assets/ProjectEta/Tests/EditMode/Day69InputBindingTests.cs`
- `Assets/ProjectEta/Tests/EditMode/Day69UiIntegrationTests.cs`
- 신규 파일의 `.meta`
- `Devlogs/Day69/README.md`

### 수정

- `Assets/ProjectEta/Scripts/Battle/BattleController.cs`
- `Assets/ProjectEta/Scripts/Meta/MetaProgressUI.cs`
- `Assets/ProjectEta/Scripts/SceneFlow/SceneRuntimeBootstrap.cs`
- `Assets/ProjectEta/Scripts/Settings/BattleSettingsOverlayController.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsData.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsEditState.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsService.cs`
- `Assets/ProjectEta/Scripts/UI/BattleHUD.cs`
- `Assets/ProjectEta/Scripts/UI/CardRewardUI.cs`
- `Assets/ProjectEta/Scripts/UI/Day67BattleAnnouncementUI.cs`
- `Assets/ProjectEta/Scripts/UI/FirstRunTutorialController.cs`
- `Assets/ProjectEta/Scripts/UI/SystemToastUI.cs`
- `Assets/ProjectEta/Tests/EditMode/ProjectEta.Tests.EditMode.asmdef`

### 삭제

- 최종 커밋 기준 영구 삭제 파일 없음

## 결과

69일차에서는 개별 UI를 추가하는 단계에서 전체 UI를 한 런 안에서 함께 사용하는 단계로 전환했다.

공통 Canvas 계층과 Modal 표시 규칙을 도입해 Pause, Tutorial, Reward, Run Result, Battle Announcement, System Toast 사이의 우선순위를 명확하게 만들었다.

동시에 조작키 설정을 실제 전투 입력, Pause 입력, 조작법 화면, 최초 튜토리얼, 전투 안내 문구가 함께 참조하도록 연결했다.

조작법 화면은 키 문자열 길이에 의존하는 공백 정렬을 제거하고 키 열과 설명 열을 분리해 설명 시작 위치를 일정하게 유지하도록 변경했다.

## 검증 상태

GitHub `main` 최신 커밋은 `c55b6ec0f3c6511abed3f18ff151400d272b9756`이며 커밋 메시지는 `69`다.

68일차 커밋과 비교하면 69일차 변경은 1개 커밋으로 구성되어 있고, Day69 UI 통합·조작키 관련 생성 및 수정 파일이 모두 포함되어 있다.

이전에 삭제됐던 `GameSettingsData.cs`, `GameSettingsEditState.cs`, `GameInputBindingService.cs`, `Day69InputBindingTests.cs`, `ProjectEta.Tests.EditMode.asmdef`도 최신 커밋에서 다시 확인됐다.

EditMode asmdef에는 `Unity.InputSystem` 참조가 포함되어 있다.

GitHub에 등록된 Commit Status와 Workflow Run은 현재 없으므로 GitHub CI 기준 컴파일·테스트 결과는 확인할 수 없다.

따라서 소스 구성과 의존 관계 기준으로 69일차 작업 범위는 정리된 상태이며, Unity Editor에서의 실제 컴파일 및 EditMode Test Runner 최종 통과 여부는 별도 실행 결과로 확인해야 한다.
