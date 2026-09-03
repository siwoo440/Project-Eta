# 10일차 개발 일지 — 턴 시스템 도입

**날짜**: 2026-09-03
**목표**: 9일차까지의 단일 `RunState` + `BattleController` → `BoardView`/`BoardInputController` 구조를 유지한 채, 플레이어 턴·적 턴을 오가는 턴 시스템을 추가.

## 오늘 한 일

### 1. 턴 상태 관리
- `Battle/TurnState.cs`: `PlayerTurn` / `EnemyTurn` / `BattleEnded` 3가지 상태 정의.
- `Battle/TurnManager.cs`: 턴 번호, 행동권(`HasPlayerActed`), 상태 전환(`TryCompletePlayerAction`, `CompleteEnemyTurn`, `EndBattle`)을 관리하는 순수 상태 클래스. 상태 변경 시 `TurnChanged` 이벤트로 통지.

### 2. 턴 표시 UI
- `Battle/TurnStatusUI.cs`: 화면 상단 중앙에 `"N턴 · 플레이어 턴"` 형태로 현재 턴을 표시하는 Screen Space Overlay Canvas. 씬 YAML을 직접 건드리지 않고 런타임에 Canvas/Panel/Text를 자동 생성.
- 프로젝트에 이미 포함된 uGUI(`UnityEngine.UI`) 패키지를 사용하므로 별도 패키지 설치 불필요. `Scripts/ProjectEta.Runtime.asmdef`에 `UnityEngine.UI` 참조만 추가.

### 3. 기존 시스템과 연동
- `Battle/BattleController.cs`: `TurnManager`/`TurnStatusUI` 생성·연결, Space 키(10일차 임시 테스트 입력)로 `TryCompletePlayerAction` 호출, 더미 적 턴을 코루틴으로 약 0.5초 뒤 자동 종료.
- `Board/BoardInputController.cs`: `TurnManager`를 전달받아 `CanReceivePlayerInput`으로 적 턴 동안 카드 선택·보드 클릭 입력을 전부 차단.

### 4. 테스트
- `Tests/EditMode/TurnManagerTests.cs`, `Tests/EditMode/TurnStatusUITests.cs` 추가 — 턴 흐름과 Canvas 위치·문구 갱신을 검증.

## 확인된 흐름

```
1턴 · 플레이어 턴 → Space → 1턴 · 적 턴 → 약 0.5초 → 2턴 · 플레이어 턴
```

적 턴 동안에는 카드 선택과 보드 클릭이 차단된다. 카드 배치는 아직 일반 행동으로 계산하지 않으므로 Space를 눌러야 턴이 종료된다(추후 조정 대상).

## 오늘 하지 않은 것 / 알려진 한계

- 실제 적 AI 행동 없음 — 지금은 일정 시간 후 자동으로 넘어가는 더미 적 턴.
- 카드 배치를 플레이어 행동으로 계산하는 로직은 아직 없음(Space만으로 턴 종료).
- **Unity 에디터를 직접 실행할 수 없는 환경에서 작업해, asmdef JSON 유효성·C# 구조·GUID 중복·ZIP 무결성까지만 검사했고 실제 Unity 컴파일과 Test Runner 통과 여부는 아직 검증하지 않음.**

## 완료 기준 체크

- [x] `TurnManager`/`TurnState`로 플레이어 턴 ↔ 적 턴 전환 구현
- [x] 상단 중앙 Canvas 턴 표시 UI 구현(씬 파일 직접 수정 없음)
- [x] 적 턴 동안 보드·카드 입력 차단
- [x] 턴 흐름 EditMode 테스트 작성
- [ ] Unity 에디터에서 실제 컴파일 확인(Console Error 0) *(에디터 작업 필요)*
- [ ] Battle 씬 Play로 Space 턴 전환이 설명대로 동작하는지 실제 확인 *(에디터 작업 필요)*
- [ ] Test Runner에서 `TurnManagerTests`/`TurnStatusUITests` 통과 확인 *(에디터 작업 필요)*

## 남은 일 (사용자가 직접)

1. Unity 에디터를 열어 컴파일 에러가 없는지 확인.
2. Battle 씬을 Play해 위 흐름대로 동작하는지 확인.
3. Test Runner(EditMode)에서 새 테스트 2종이 통과하는지 확인.
4. 문제가 있으면 Console 오류를 기준으로 다음 턴에서 수정.
