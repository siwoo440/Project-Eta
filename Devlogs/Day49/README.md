# 49일차 : 킹 공통 구조 및 공격형 킹 패시브 구축

## 개발 목표

48일차에서 준비한 `king_attack`, `king_defense`, `king_strategy` 영구 해금 ID를 실제 킹 시스템과 연결한다.

49일차에서는 세 킹이 함께 사용할 공통 런타임 구조를 먼저 만들고, 첫 번째 실제 특수 킹인 공격형 킹을 구현한다.

공격형 킹은 플레이어가 킹을 직접 전진시켜 적을 처치할수록 다음 공격이 강해지는 위험·보상형 패시브를 사용한다.

선택한 킹은 같은 RunState가 유지되는 동안 전투→지도→보상→다음 스테이지에서도 유지하고, 전투 한정 상태인 격노만 전투가 끝날 때 초기화한다.

## 주요 개발 내용

### KingArchetype

세 킹을 공통 타입으로 관리하기 위한 `KingArchetype`을 추가했다.

현재 타입은 다음과 같다.

- Default
- Attack
- Defense
- Strategy

49일차에서는 Default와 Attack을 실제 선택 가능한 상태로 사용한다.

Defense와 Strategy는 50일차 구현을 위해 타입과 영구 해금 ID만 미리 준비한다.

### KingUnlockIds

48일차 메타 성장 시스템에서 사용하는 킹 영구 해금 ID와 동일한 값을 공통 상수로 연결했다.

현재 ID는 다음과 같다.

- `king_attack`
- `king_defense`
- `king_strategy`

공격형 킹은 `king_attack`이 영구 해금된 경우에만 선택할 수 있다.

기본 킹은 영구 해금 상태와 관계없이 항상 선택할 수 있다.

### KingRunState

현재 런에서 선택한 킹과 킹 전용 임시 상태를 관리하는 `KingRunState`를 추가했다.

현재 관리 항목은 다음과 같다.

- 선택한 KingArchetype
- 공격형 킹 RageStacks

기본 킹으로 시작하며 다른 킹을 선택하면 기존 전투 한정 상태를 초기화한다.

### KingRunStateService

기존 `RunState` 코드를 직접 수정하지 않고 런 단위 킹 상태를 유지하기 위해 `KingRunStateService`를 추가했다.

각 `RunState` 객체를 키로 하나의 `KingRunState`를 연결한다.

따라서 같은 런의 BattleState가 새로 만들어져도 선택한 킹 정보는 동일한 RunState에 연결되어 유지된다.

51일차 런 세이브 확장 전까지 사용하는 런타임 유지 구조이며, 디스크 저장은 아직 포함하지 않는다.

### 공격형 킹 패시브 : 처형의 연쇄

공격형 킹의 첫 패시브로 `처형의 연쇄`를 구현했다.

규칙은 다음과 같다.

1. 공격형 킹이 적 기물을 직접 처치한다.
2. 격노를 1스택 획득한다.
3. 격노는 최대 2스택까지 저장한다.
4. 다음 플레이어 킹 공격 시 현재 격노 스택만큼 피해를 추가한다.
5. 공격 시 저장한 격노를 전부 소비한다.

예시는 다음과 같다.

- 격노 0 → 기본 피해
- 격노 1 → 다음 킹 공격 피해 +1
- 격노 2 → 다음 킹 공격 피해 +2

일반 아군 기물의 공격에는 격노가 적용되지 않는다.

플레이어 킹의 직접 공격만 격노를 소비한다.

### AttackKingAbility

`AttackKingAbility`를 추가해 공격형 킹의 전투 규칙을 전투 시스템과 분리했다.

`BeforeDamage` 시점에는 다음 킹 공격의 격노 피해를 적용한다.

`AfterAttack` 시점에는 공격자가 플레이어 킹이고 대상이 직접 사망했는지 확인해 격노를 지급한다.

기존 `CombatResolver`나 공격 실행 코드는 수정하지 않고 기존 `BattleHooks`를 통해 연결한다.

### 기존 BattleHooks 재사용

29일차부터 구축한 전투 훅 중 다음 두 개를 사용한다.

- `BeforeDamage`
- `AfterAttack`

흐름은 다음과 같다.

`킹 공격 → BeforeDamage → 격노 피해 적용 → 피해 처리 → 사망 처리 → AfterAttack → 직접 처치라면 격노 획득`

이를 통해 공격형 킹을 위해 별도의 전투 판정 시스템을 새로 만들지 않았다.

### KingAbilityController

`KingAbilityController`를 추가해 현재 `BattleController`, `RunState`, `BattleHooks`를 런타임에서 자동 탐색하고 킹 능력을 연결한다.

Battle 씬에 진입하면 `KingAbilityController_Day49` 오브젝트가 자동 생성된다.

현재 BattleHooks가 변경되면 기존 구독을 제거한 뒤 새로운 훅에 다시 연결한다.

오브젝트가 제거될 때도 훅 구독을 정리한다.

### 전투 종료 격노 초기화

격노는 런 전체 영구 버프가 아니라 현재 전투에서만 유지되는 상태로 정의했다.

따라서 런 흐름이 `Battle`에서 Map·Reward·Shop·Event·Completed·Failed 등 다른 단계로 넘어가면 `RageStacks`를 0으로 초기화한다.

선택한 킹 타입은 그대로 유지한다.

구조는 다음과 같다.

`공격형 킹 선택 → 전투 중 격노 획득 → 전투 종료 → 격노 0 → 공격형 킹 선택은 유지 → 다음 스테이지 진입`

### KingSelectionUI

49일차 개발용 킹 선택 UI를 추가했다.

첫 스테이지의 최초 배치 턴에서 아직 킹이 보드에 배치되지 않은 경우에만 선택 버튼을 표시한다.

현재 선택지는 다음과 같다.

- 기본 킹
- 공격형 킹

공격형 킹이 아직 영구 해금되지 않았다면 버튼을 잠금 상태로 표시한다.

킹을 보드에 배치한 이후에는 킹 종류를 변경할 수 없다.

### 공격형 킹 영구 해금 연결

`KingUnlockRules`를 통해 48일차 `MetaProgressState`와 연결했다.

기본 킹은 항상 선택 가능하다.

공격형 킹은 다음 조건이 필요하다.

`MetaProgressState → MetaUnlockType.King → king_attack 해금`

48일차 메타 성장 화면에서 공격형 킹을 영구 해금하면 이후 새 런의 첫 배치에서 공격형 킹을 선택할 수 있다.

### 런 중 킹 상태 표시

최초 킹 선택이 끝난 뒤에는 선택 패널을 축소하고 현재 킹 상태를 표시한다.

공격형 킹은 다음처럼 격노 상태를 함께 보여준다.

`공격형 킹 | 격노 0/2`

직접 처치로 격노가 쌓이거나 공격으로 소비되면 표시 값이 갱신된다.

### 자동 초기화 구조

Battle 씬이나 Inspector를 직접 수정하지 않아도 동작하도록 런타임 자동 생성 구조를 사용한다.

Battle 씬 로드 후 다음 컴포넌트가 자동으로 준비된다.

- KingAbilityController
- KingSelectionUI

기존 BattleController와 BattleHooks를 찾아 자동으로 연결한다.

### EditMode 테스트

`Day49KingAbilityTests`를 추가했다.

현재 테스트 항목은 다음과 같다.

- 새 KingRunState가 기본 킹으로 시작하는지
- 동일 RunState에서 동일 KingRunState가 유지되는지
- 공격형 킹 직접 처치 시 격노 +1
- 격노 스택만큼 다음 피해 증가
- 공격 후 격노 전부 소비
- 일반 기물 공격에서는 격노 미소비
- 격노 최대 2스택 제한
- 전투 상태 초기화 시 격노 제거
- 전투 상태 초기화 이후 킹 선택 유지
- 공격형 킹 영구 해금 전 선택 차단
- `king_attack` 해금 후 공격형 킹 선택 허용
- 기본 킹 상시 선택 가능

## 주요 파일

- `Assets/ProjectEta/Scripts/King/KingArchetype.cs`
- `Assets/ProjectEta/Scripts/King/KingRunState.cs`
- `Assets/ProjectEta/Scripts/King/KingRunStateService.cs`
- `Assets/ProjectEta/Scripts/King/KingUnlockRules.cs`
- `Assets/ProjectEta/Scripts/King/AttackKingAbility.cs`
- `Assets/ProjectEta/Scripts/King/KingAbilityController.cs`
- `Assets/ProjectEta/Scripts/King/KingSelectionUI.cs`
- `Assets/ProjectEta/Tests/EditMode/Day49KingAbilityTests.cs`

## 결과

48일차에서 구축한 메타 영구 해금 시스템과 실제 런의 킹 선택을 연결했다.

세 킹이 공통으로 사용할 타입·런 상태·해금 규칙·BattleHooks 연결 구조가 준비됐다.

첫 실제 특수 킹인 공격형 킹은 `처형의 연쇄`를 통해 직접 처치 시 최대 2스택의 격노를 얻고 다음 킹 공격에 추가 피해를 적용할 수 있다.

선택한 킹은 같은 런 동안 유지되며 격노만 현재 전투가 끝날 때 초기화된다.

50일차에서는 동일한 공통 구조를 이용해 방어형 킹의 `왕의 요새`와 전략형 킹의 `전술적 준비`를 구현한다.

51일차에서는 선택한 킹 종류와 필요한 킹 런타임 상태를 런 세이브 데이터에 포함해 게임을 종료한 뒤에도 현재 런을 복원할 수 있도록 확장한다.
