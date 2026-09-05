# 46일차 : 카드 보상 선택 및 런 덱 반영 구축

## 개발 목표

45일차에 구현한 `StageDefinition` 기반 스테이지 전환 구조에 카드 보상 흐름을 연결한다.

일반 전투 승리 또는 `Reward` 스테이지 진입 시 획득 가능한 카드 후보를 최대 3장 제시하고, 플레이어가 한 장을 선택하면 해당 카드를 런 전체 `OwnedCardPool`에 추가한 뒤 다시 경로 지도로 복귀하도록 구성한다.

선택한 카드는 이후 전투 시작 시 `OwnedCardPool` 기반으로 드로우 더미가 재구성되면서 실제 다음 전투에서 사용할 수 있어야 한다.

## 주요 개발 내용

### CardRewardState

현재 카드 보상 선택 상태를 관리하는 `CardRewardState`를 추가했다.

보상 상태는 다음 정보를 관리한다.

- 현재 후보 카드 목록
- 선택된 카드
- 보상 발생 경로
- 보상 진행 여부
- 선택 완료 여부

한 번 보상을 선택하면 같은 보상 화면에서 두 번째 카드를 선택하지 못하도록 제한한다.

보상 발생 경로는 다음 두 종류로 분리했다.

- `BattleVictory`
- `RewardNode`

### CardRewardGenerator

획득 가능한 카드 풀에서 최대 3개의 후보를 만드는 `CardRewardGenerator`를 추가했다.

후보 생성 시 동일한 `PieceId`가 같은 보상 화면에 중복해서 등장하지 않도록 처리한다.

현재 런의 `OwnedCardPool`을 함께 전달해 이미 보유 상한에 도달한 카드는 후보에서 제외한다.

프로토타입 단계에서는 현재 라운드와 보유 카드 수, 보상 발생 경로를 조합한 시드를 사용해 후보 순서를 결정한다.

### CardRewardRules

카드 보상 획득 가능 여부와 동일 카드 보유 상한을 담당하는 `CardRewardRules`를 추가했다.

현재 일반 카드 보상에서 제외되는 항목은 다음과 같다.

- 플레이어 King
- `Fusion` 분류
- `Monster` 분류
- `Boss` 분류
- 4성
- 5성

동일 `PieceId` 카드의 프로토타입 보유 상한은 3장으로 설정했다.

선택 카드 추가는 `OwnedCardPool`의 읽기 전용 인터페이스를 직접 수정하지 않고 기존 `DeckState.AddToOwnedPool()`을 통해 처리하도록 구성했다.

### CardRewardController

전투 결과와 스테이지 흐름을 실제 카드 보상 선택에 연결하는 `CardRewardController`를 추가했다.

Battle 씬에서 런타임에 자동 생성되며 별도의 Scene 또는 Inspector 설정 없이 동작하도록 구성했다.

일반 전투 승리 시 다음 흐름을 사용한다.

`Battle 승리 → Map 전환 확인 → Reward 흐름 → 카드 후보 생성 → 카드 선택 → OwnedCardPool 추가 → Map 복귀`

최종 보스 승리는 카드 보상으로 진입하지 않고 기존 런 완료 흐름을 유지한다.

### Reward 노드 실제 보상 연결

45일차까지 `Reward` 스테이지는 임시 Placeholder 화면으로만 진입했다.

46일차에서는 `StageTransitionController`를 수정해 `Reward` 노드에 도착하면 `RunFlowPhase.Reward`로 전환하고 `CardRewardController`가 실제 카드 보상 UI를 표시하도록 연결했다.

Reward 노드에서 카드를 선택하면 해당 스테이지를 완료 상태로 기록하고 현재 위치를 기준으로 다음 깊이의 경로를 생성한 뒤 Map으로 복귀한다.

`Shop`과 `Event`는 47일차 구현 전까지 기존 임시 Placeholder 흐름을 유지한다.

### CardRewardUI

후보 카드들을 화면 중앙에 표시하는 `CardRewardUI`를 추가했다.

현재 개발용 보상 카드는 다음 정보를 표시한다.

- 기물 이름
- 별 등급
- HP
- ATK
- Category
- MovementType
- 카드 설명

카드를 클릭하면 해당 `PieceDefinition`을 선택 결과로 전달하고, 정상적으로 `OwnedCardPool`에 추가된 뒤 보상 화면을 닫는다.

보상 화면 전체에 입력 차단 배경을 두어 뒤쪽 경로 지도를 동시에 클릭하지 못하도록 구성했다.

### 지도 입력 차단

`RouteMapBoardController`의 지도 입력 조건을 보강했다.

지도 시각이 화면에 남아 있더라도 실제 `RunFlowPhase.Map` 상태일 때만 경로 노드 클릭을 처리한다.

따라서 다음 상태에서는 뒤쪽 지도 노드 입력이 차단된다.

- Reward
- Shop
- Event

### 다음 전투 덱 반영

카드 보상은 현재 전투의 `Hand`나 `DrawPile`에 직접 넣지 않고 런 전체 `OwnedCardPool`에 추가한다.

다음 전투 진입 시 기존 45일차의 전투 재구성 과정에서 `OwnedCardPool`을 기준으로 DrawPile을 다시 만들기 때문에 새로 획득한 카드가 이후 전투의 실제 드로우 후보가 된다.

흐름은 다음과 같다.

`카드 선택 → OwnedCardPool 추가 → Map → 다음 Battle → DrawPile 재구성 → 보상 카드 드로우 가능`

### 컴파일 오류 수정

46일차 구현 과정에서 두 종류의 컴파일 오류를 수정했다.

첫 번째는 `CardRewardUI`에서 `CardRewardSource`가 정의된 `ProjectEta.Run` 네임스페이스를 참조하지 않아 발생한 오류다.

`using ProjectEta.Run;`을 추가해 해결했다.

두 번째는 `DeckState.OwnedCardPool`이 `IReadOnlyList<PieceDefinition>`으로 공개되어 있는데 보상 규칙에서 `IList<PieceDefinition>`을 요구해 발생한 타입 불일치다.

보상 규칙이 `DeckState` 자체를 전달받고 기존 `DeckState.AddToOwnedPool()`을 사용하도록 변경해 읽기 전용 외부 인터페이스를 유지하면서 카드를 정상적으로 추가하도록 수정했다.

### 회귀 테스트

`Day46CardRewardTests`를 추가해 다음 규칙을 검증하도록 구성했다.

- 획득 가능한 카드 풀에서 최대 3개의 중복 없는 후보 생성
- King·Fusion·Monster·Boss 제외
- 4·5성 일반 보상 제외
- 동일 카드 3장 보유 시 후보 제외
- `DeckState` 공개 API를 통한 보상 카드 추가
- 한 보상에서 한 장만 선택 가능

## 주요 파일

- `Assets/ProjectEta/Scripts/Run/CardRewardState.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardRules.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardGenerator.cs`
- `Assets/ProjectEta/Scripts/Run/CardRewardController.cs`
- `Assets/ProjectEta/Scripts/UI/CardRewardUI.cs`
- `Assets/ProjectEta/Scripts/Run/StageTransitionController.cs`
- `Assets/ProjectEta/Scripts/Board/RouteMapBoardController.cs`
- `Assets/ProjectEta/Tests/EditMode/Day46CardRewardTests.cs`

## 결과

일반 전투 승리와 Reward 노드가 실제 카드 보상 흐름으로 연결됐다.

플레이어는 최대 3개의 후보 중 한 장을 선택할 수 있고, 선택한 카드는 런 전체 `OwnedCardPool`에 추가된다.

보상 완료 후 동일한 10×10 체스판 경로 지도로 복귀하며, 획득한 카드는 이후 전투의 DrawPile 재구성에 포함되어 실제 플레이 카드로 사용할 수 있는 구조가 마련됐다.

다음 47일차에서는 45일차부터 Placeholder로 남겨 둔 `Shop`과 `Event` 노드를 실제 구매·카드 제거·회복 및 선택형 이벤트 흐름으로 연결한다.
