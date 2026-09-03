# 9일차 개발 일지 — 전투 상태 통합

**날짜**: 2026-09-03  
**목표**: 2단계의 첫 작업으로 Battle 씬의 보드·손패·저장 상태가 서로 다른 객체를 사용하던 구조를 정리하고, `RunState` 하나를 전투 상태의 단일 기준으로 사용하도록 통합한다.

## 오늘 한 일

### 1. BattleController 추가 — 전투 상태의 단일 진입점
- `Assets/ProjectEta/Scripts/Battle/BattleController.cs`를 추가.
- Battle 씬에서 `RunState` 하나를 생성·소유하고 `BoardView`와 `BoardInputController`에 전달하도록 구성.
- 씬에 `BattleController`가 없을 경우 Battle 씬 로드 후 자동 생성되도록 부트스트랩 처리.
- 이후 저장 데이터 로드 등에서 기존 `RunState`를 전달할 수 있도록 `Initialize(RunState)` 진입점 추가.

### 2. BoardView의 독립 BoardState 제거
- 기존 `BoardView.Awake()`에서 자체적으로 `new BoardState()`를 생성하던 구조를 제거.
- `Bind(BoardState)`를 통해 외부에서 실제 보드 상태를 주입받도록 변경.
- `State`, `IsBound` 프로퍼티를 추가해 현재 연결된 상태를 확인할 수 있도록 함.
- 기존 단일 메시 보드 렌더링, 좌표 변환, 칸 강조 기능은 그대로 유지.

### 3. BoardInputController의 테스트용 HandState 제거
- 기존의 별도 `new HandState()`를 제거.
- `Bind(RunState, BoardView)`를 통해 `RunState.Hand`를 그대로 참조하도록 변경.
- 카드 사용 시 테스트용 손패가 아니라 실제 런의 손패에서 카드가 제거되도록 연결.
- 상태가 아직 주입되지 않은 경우 입력 처리를 하지 않도록 보호 로직 추가.

### 4. 프로토타입 시작 손패를 RunState와 연결
- 기존 숫자키 `1`/`2` 기반 King/Pawn 테스트 방식을 유지.
- 새 런의 손패가 비어 있을 때만 King/Pawn을 `RunState.Hand`에 넣도록 변경.
- 이미 손패가 존재하는 저장/복원 상태에서는 테스트 카드가 중복 추가되지 않도록 처리.

### 5. 단일 상태 참조 EditMode 테스트 추가
- `BattleStateBindingTests.cs` 추가.
- `BoardView.State`가 `RunState.Board`와 정확히 같은 인스턴스인지 검증하는 테스트 작성.
- `BoardInputController.HandState`가 `RunState.Hand`와 정확히 같은 인스턴스인지 검증하는 테스트 작성.
- 화면 보드와 입력 시스템이 같은 `RunState`를 바라보는 구조를 코드 수준에서 확인할 수 있도록 함.

## 구조 변경

기존 구조:

```text
BoardView
└─ 별도 BoardState

RunState
└─ 저장용 BoardState / HandState

BoardInputController
└─ 테스트용 HandState
```

변경 구조:

```text
BattleController
└─ RunState
   ├─ BoardState ──► BoardView
   ├─ HandState  ──► BoardInputController
   └─ DeckState
```

앞으로 이동·공격·저장 시스템은 모두 동일한 `RunState`를 기준으로 구현한다.

## 오늘 하지 않은 것

- `MovementResolver` 결과를 실제 클릭 이동으로 연결하는 작업은 진행하지 않음 — 다음 일차 작업.
- 플레이어/적 턴 전환 및 한 턴 한 기물 행동 제한은 아직 구현하지 않음 — 10일차 예정.
- HP·ATK 기반 실제 전투 판정은 아직 구현하지 않음.
- Unity Editor에서의 컴파일 결과와 EditMode 테스트 실행 결과는 이 개발 일지 작성 시점의 GitHub 정보만으로는 확인하지 않음.

## 완료 기준 체크

- [x] `BattleController`가 전투의 단일 `RunState`를 소유하는 구조 추가.
- [x] `BoardView`의 독립 `BoardState` 생성 제거.
- [x] `BoardInputController`의 독립 테스트용 `HandState` 생성 제거.
- [x] `BoardView`가 `RunState.Board`를 직접 참조하도록 연결.
- [x] `BoardInputController`가 `RunState.Hand`를 직접 참조하도록 연결.
- [x] 단일 Board/Hand 인스턴스 참조를 확인하는 EditMode 테스트 코드 추가.
- [ ] Unity Editor 컴파일 및 Console Error 0 최종 확인.
- [ ] Unity Test Runner에서 전체 EditMode 테스트 통과 최종 확인.

9일차에서는 2단계 전투 구현을 시작하기 전에 화면·입력·저장이 서로 다른 상태를 바라보는 구조적 부채를 제거했다. 다음 작업부터 실제 이동·턴·전투 로직을 동일한 `RunState` 위에 연결한다.
