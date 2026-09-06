# 50일차 : 방어형·전략형 킹 패시브 및 3종 선택 구조 완성

## 개발 목표

49일차에서 구축한 공통 킹 런타임 구조와 공격형 킹 `처형의 연쇄`를 기반으로 방어형 킹과 전략형 킹을 구현한다.

48일차에서 준비한 `king_defense`, `king_strategy` 영구 해금 ID를 실제 킹 선택 구조에 연결하고, 공격형·방어형·전략형이 같은 전투·경로 지도 흐름 위에서 서로 다른 운영 경험을 제공하도록 한다.

50일차의 핵심은 다음 두 패시브다.

- 방어형 킹 : `왕의 요새`
- 전략형 킹 : `전술적 준비`

## 주요 개발 내용

### KingRunState 확장

49일차의 `KingRunState`를 3종 킹 공통 상태로 확장했다.

기존 공격형 상태인 `RageStacks`에 다음 상태를 추가했다.

- `BarrierActive`
- `KingMovedThisTurn`
- `StrategyPreparationPending`
- `StrategyPreparationTurnNumber`

공격형·방어형·전략형의 전투 한정 상태는 같은 런의 선택 킹 정보와 분리해 관리한다.

전투가 종료되면 격노·방벽·이동 기록·전술적 준비 진행 상태를 초기화하지만 `Archetype` 선택 자체는 유지한다.

### 방어형 킹 — 왕의 요새

방어형 킹의 고정 패시브 `왕의 요새`를 구현했다.

기본 규칙은 다음과 같다.

1. 플레이어 턴 시작 시 킹 이동 여부를 초기화한다.
2. 플레이어 킹이 실제로 이동하면 `KingMovedThisTurn`을 기록한다.
3. 적 턴으로 전환될 때 이번 플레이어 턴에 킹이 이동하지 않았다면 방벽을 획득한다.
4. 방벽은 최대 1개만 유지한다.
5. 다음에 플레이어 킹이 피해를 받을 때 피해를 1 감소시킨다.
6. 방벽은 피해 처리 후 소모된다.
7. 최종 피해는 최소 1을 유지한다.

일반 아군 기물이 피해를 받아도 킹의 방벽은 소비되지 않는다.

### 처치 후 이동도 방어형 이동으로 처리

프로젝트 η의 근접 기물은 적을 처치하면 대상 칸으로 이동한다.

기존 `BoardInputController.MovePieceTo()`가 이동 완료 후 `BattleHooks.AfterMove`를 발행하므로, 방어형 킹이 직접 적을 처치해 대상 칸을 점유한 경우도 킹 이동으로 기록된다.

따라서 직접 처치 후 이동한 턴에는 `왕의 요새` 신규 방벽을 획득하지 않는다.

### DefenseKingAbility

방어형 킹 규칙을 `DefenseKingAbility`로 분리했다.

주요 책임은 다음과 같다.

- 플레이어 턴 시작 이동 기록 초기화
- 플레이어 킹 이동 감지
- EnemyTurn 진입 전 방벽 획득 판정
- 플레이어 킹 피해 전 방벽 적용
- 피해 최소 1 유지
- 방벽 1회 소비

기존 피해 계산 코드를 직접 변경하지 않고 `BattleHooks.BeforeDamage`를 통해 피해량을 조정한다.

### DeckState 전략형 지원 API

전략형 킹이 드로우 덱 위 카드를 확인하고 재배치할 수 있도록 `DeckState`를 확장했다.

추가된 주요 기능은 다음과 같다.

- `PeekTopCards(int count)`
- `AddDrawCardToBottom(PieceDefinition card)`
- `TryMoveSpecificToHand(...)`

기존 드로우 규칙은 리스트 마지막 요소를 덱 맨 위로 사용한다.

따라서 `PeekTopCards()`도 실제 드로우 순서와 동일하게 마지막 요소부터 후보를 반환한다.

`AddDrawCardToBottom()`은 리스트 첫 위치에 카드를 넣어 현재 드로우 규칙에서 덱 맨 아래로 이동시키는 역할을 한다.

### 기존 카드 정리 기능 유지

기존 손패 카드 정리 기능인 `DiscardToBottom()`도 새 `AddDrawCardToBottom()`을 사용하도록 정리했다.

이를 통해 일반 배치 턴 카드 정리와 전략형 킹의 미선택 카드 재배치가 동일한 덱 아래 규칙을 사용한다.

### 전략형 킹 — 전술적 준비

전략형 킹의 고정 패시브 `전술적 준비`를 구현했다.

기본 규칙은 다음과 같다.

1. 배치 턴 중 전략형 킹 여부를 확인한다.
2. 손패가 가득 차지 않았고 드로우 덱에 카드가 있으면 덱 위 최대 3장을 후보로 만든다.
3. 플레이어가 후보 중 1장을 선택한다.
4. 선택 카드는 손패에 추가한다.
5. 선택하지 않은 후보는 드로우 덱 맨 아래로 이동한다.
6. 같은 배치 턴에는 한 번만 발동한다.

드로우 덱에 카드가 1~2장만 남아 있으면 존재하는 카드 수만큼만 후보를 생성한다.

손패가 최대 10장인 경우에는 패시브를 발동하지 않고 해당 배치 턴을 처리 완료 상태로 기록한다.

### StrategyKingAbility

`StrategyKingAbility`에서 카드 후보 생성과 실제 선택 처리를 담당한다.

후보를 표시한 이후 다른 시스템이 드로우 덱 순서를 변경했을 가능성을 고려해 선택 시점에 현재 드로우 덱 상단과 기존 후보가 같은지 다시 확인한다.

카드 이동 과정에서 실패하면 임시로 뽑은 후보를 원래 순서로 복구한다.

정상 선택 시 선택 카드만 손패에 남기고 미선택 후보를 덱 아래로 보낸다.

### StrategyKingSelectionUI

전략형 킹의 카드 3장 선택용 `StrategyKingSelectionUI`를 추가했다.

배치 턴에 `전술적 준비`가 발동하면 화면 중앙에 다음 구조의 선택 화면을 표시한다.

- `전술적 준비` 제목
- 최대 3개의 카드 후보
- 카드 이름
- 선택 버튼
- 선택 카드와 미선택 카드 처리 규칙 안내

선택 UI는 전체 화면 입력 차단 이미지를 사용해 플레이어가 후보 선택 중 보드를 실수로 클릭하지 않도록 한다.

카드 선택이 성공하면 화면을 닫고 손패·드로우 덱 UI를 다시 갱신한다.

### 전략형 선택 중 배치 턴 종료 차단

전략형 패시브 선택 중 Space 입력 등으로 배치 턴이 종료되지 않도록 `TurnManager`에 `IsDeploymentChoicePending` 상태를 추가했다.

전략형 카드 선택이 진행 중이면 다음 행동을 차단한다.

- 일반 배치 입력
- 합성 입력
- 배치 턴 종료

카드 선택 완료 후 대기 상태를 해제하고 정상 배치 턴으로 복귀한다.

새 전투 시작, 전투 종료, 다음 PlayerTurn 진입 시에도 선택 대기 상태를 정리한다.

### KingAbilityController 3종 통합

49일차의 `KingAbilityController`를 3종 킹 공통 패시브 컨트롤러로 확장했다.

현재 연결 구조는 다음과 같다.

#### 공격형 킹

- `BeforeDamage`
- `AfterAttack`

#### 방어형 킹

- `AfterMove`
- `TurnManager.TurnChanged`
- `BeforeDamage`

#### 전략형 킹

- `DeploymentTurn`
- `RunState.Deck`
- `RunState.Hand`
- `StrategyKingSelectionUI`

각 킹의 고유 로직은 전용 Ability 클래스에 두고 `KingAbilityController`는 현재 런 상태와 BattleHooks·TurnManager·UI를 연결하는 역할을 담당한다.

### KingSelectionUI 3종 킹 확장

49일차의 킹 선택 UI를 다음 선택지로 확장했다.

- 기본 킹
- 공격형 킹
- 방어형 킹
- 전략형 킹

특수 킹은 48일차 영구 해금 상태에 따라 선택 가능 여부를 결정한다.

- 공격형 : `king_attack`
- 방어형 : `king_defense`
- 전략형 : `king_strategy`

잠긴 킹은 `잠금`으로 표시한다.

해금된 킹은 패시브 이름을 함께 표시한다.

- 공격형 : `처형의 연쇄`
- 방어형 : `왕의 요새`
- 전략형 : `전술적 준비`

### 런 중 킹 상태 표시

최초 선택 이후 킹 선택 패널은 축소되고 현재 킹 상태를 표시한다.

공격형은 현재 격노 스택을 표시한다.

`공격형 킹 | 격노 1/2`

방어형은 방벽 활성 여부를 표시한다.

`방어형 킹 | 방벽 ON`

전략형은 현재 전술적 준비 선택 여부 또는 배치 턴 패시브 설명을 표시한다.

### EditMode 테스트

`Day50KingAbilityTests`를 추가했다.

주요 테스트 항목은 다음과 같다.

#### 방어형

- 킹이 이동하지 않은 플레이어 턴 뒤 방벽 획득
- 킹 이동 시 방벽 미획득
- 방벽 피해 1 감소
- 최종 피해 최소 1
- 방벽 1회 소비
- 일반 아군 기물 피해 시 킹 방벽 유지

#### 전략형

- 덱 위 3장 후보 순서
- 선택 카드 손패 이동
- 미선택 카드 덱 아래 이동
- 손패 최대 상태에서 후보 생성 차단
- 전략형 선택 중 배치 턴 종료 차단

#### 영구 해금

- `king_defense` 해금 전 선택 차단
- `king_strategy` 해금 전 선택 차단
- 영구 해금 후 선택 허용

## 주요 파일

### 생성

- `Assets/ProjectEta/Scripts/King/DefenseKingAbility.cs`
- `Assets/ProjectEta/Scripts/King/StrategyKingAbility.cs`
- `Assets/ProjectEta/Scripts/King/StrategyKingSelectionUI.cs`
- `Assets/ProjectEta/Tests/EditMode/Day50KingAbilityTests.cs`

### 수정

- `Assets/ProjectEta/Scripts/Battle/TurnManager.cs`
- `Assets/ProjectEta/Scripts/Cards/DeckState.cs`
- `Assets/ProjectEta/Scripts/King/KingRunState.cs`
- `Assets/ProjectEta/Scripts/King/KingAbilityController.cs`
- `Assets/ProjectEta/Scripts/King/KingSelectionUI.cs`

### 삭제

없음.

## 검증 메모

최신 커밋 기준으로 50일차 변경 파일과 49일차 기준 diff를 확인했다.

GitHub에 등록된 CI 상태 검사는 현재 없다.

따라서 저장소 구조와 코드 연결에서 명확한 차단 문제는 확인하지 못했지만 Unity Editor 실제 컴파일 및 EditMode 실행 결과는 별도 로컬 검증이 필요하다.

전략형 선택 중에는 `TurnManager.IsDeploymentChoicePending`으로 배치·합성·배치 턴 종료를 차단하며, 전체 화면 선택 UI로 보드 포인터 입력을 가린다.

## 결과

공격형·방어형·전략형 3종 킹의 프로토타입 패시브가 모두 구현됐다.

공격형은 직접 처치와 격노 누적을 중심으로 공격적으로 운용한다.

방어형은 킹의 위치를 유지하면서 방벽을 얻는 생존 중심 운용을 제공한다.

전략형은 배치 턴마다 드로우 덱 위 카드 중 필요한 카드를 선택해 카드·합성 계획을 보조한다.

50일차까지 세 킹이 같은 전투·경로 지도·영구 해금 구조를 공유하면서도 서로 다른 발동 조건과 운영 방식을 가지는 기반이 완성됐다.

다음 51일차에서는 선택한 킹 종류를 포함해 RouteMapState, 현재 지도 좌표, 현재 노드, 방문 노드, 선택 경로, 맵 시드, 현재 StageDefinition 등 런 진행 정보를 저장·복원하는 세이브 확장으로 이어간다.
