# 4일차 개발 일지 — 보드 상태와 입력 구현

**날짜**: 2026-09-03
**목표**: 2일차의 `TileState`(좌표·점유 상태)를 마우스 클릭과 연결해, 클릭한 타일의 좌표·점유 상태를 확인할 수 있게 한다. 선택한 칸이 아군 배치 가능 영역인지 적 영역인지 색으로 구분한다.

## 오늘 한 일

### 1. 타일 기본색을 흰색으로 통일 (`BoardView.cs`)
- 3일차에 넣었던 "영역별 상시 파랑/빨강 색칠"을 제거하고, 선택되지 않은 모든 타일은 흰색(`_idleColor`)으로 통일.
- 하이라이트용 색을 두 종류로 분리: 아군 배치 가능 영역 선택 시 연한 파랑(`_installableHighlightColor`), 적 영역(배치 불가) 선택 시 연한 빨강(`_blockedHighlightColor`).

### 2. `TileView` 작성 (`Scripts/Board/TileView.cs`)
- 타일 GameObject마다 붙는 컴포넌트. 자신의 `TileState`와 흰색(대기)·하이라이트 머티리얼을 들고 있다가 `Select()`/`Deselect()`로 전환.
- `BoardView`가 타일을 생성할 때 `TileState.IsPlayerPlacementArea` 여부에 따라 어떤 하이라이트색을 쓸지 미리 정해서 넘겨준다.

### 3. `BoardInputController` 작성 (`Scripts/Board/BoardInputController.cs`)
- 프로젝트의 `ProjectSettings`가 `activeInputHandler: 1`(새 Input System 전용)이라 레거시 `Input` 클래스는 예외를 던지는 것을 확인 → `UnityEngine.InputSystem.Mouse.current`로 구현.
- 매 프레임 좌클릭을 감지해 `Camera.main` 기준으로 레이캐스트, 맞은 타일의 `TileView`를 선택. 같은 타일을 다시 클릭하면 해제, 빈 곳을 클릭하면 선택 해제.
- 선택 시 `Debug.Log`로 좌표(`BoardPosition`)와 점유 상태(`IsOccupied`), 배치 가능 여부(`IsPlayerPlacementArea`)를 출력해 완료 기준을 충족.

### 4. Battle 씬 연결 (`Battle.unity`)
- 기존 `BoardView` 오브젝트에 `BoardInputController` 컴포넌트를 추가로 부착(카메라 참조는 비워둬 런타임에 `Camera.main`으로 자동 연결).

## 오늘 하지 않은 것

- 실제 기물 배치(카드 사용) 로직 — 카드·합성 시스템 단계 이후.
- 화면 UI 텍스트로 좌표 표시(현재는 Console 로그로만 확인) — UI/UX 단계에서 정식 진행.

### 4. 사용자 확인 사항
- Unity 에디터에서 `Battle` 씬을 Play해 보드 전체가 흰색으로 표시되고, 아군 영역 클릭 시 연한 파랑, 적 영역 클릭 시 연한 빨강으로 바뀌는 것을 확인.
- 같은 타일 재클릭 시 흰색으로 복귀(해제)되고, Console에 좌표·점유 상태 로그가 정확히 출력되는 것을 확인.

## 완료 기준 체크

- [x] 마우스로 타일을 선택/해제할 수 있다.
- [x] 선택한 칸이 아군 배치 가능 영역이면 연한 파랑, 적 영역이면 연한 빨강으로 표시된다.
- [x] 선택하지 않은 칸은 모두 흰색으로 표시된다.
- [x] Unity 에디터에서 Battle 씬을 Play해 클릭 시 좌표·점유 상태가 Console에 정확히 출력되는지 확인
- [x] Console에 Error 0 상태 확인

4일차 완료 기준을 모두 만족해 4일차를 종료한다.
