# 6일차 개발 일지 — 저장·테스트 기반 구축

**날짜**: 2026-09-03
**목표**: `RunState`에 저장/복원 구조를 만들고, Test 씬에서 수동으로 확인할 수 있게 하며, 좌표·보드·기물·저장 데이터에 대한 자동 단위 테스트를 작성한다.

## 오늘 한 일

### 1. 저장용 데이터 구조 (`Assets/ProjectEta/Scripts/Run/RunSaveData.cs`)
- `RunSaveData`/`PieceSaveData` — `JsonUtility`로 직렬화 가능한 평면 스냅샷 클래스. 기물은 애셋을 직접 저장하지 않고 `PieceId` 문자열만 저장.

### 2. 기물 데이터 조회용 `PieceDatabase` (`Scripts/Pieces/PieceDatabase.cs`, `Data/PieceDatabase.asset`)
- `PieceId → PieceDefinition` 역참조용 ScriptableObject. King/Pawn을 등록해 `PieceDatabase.asset`으로 생성.

### 3. `RunState` 저장/복원 메서드 (`RunState.cs`)
- `ToSaveData()`: 킹 HP·라운드·메타 재화·손패/보유/죽은 카드 풀·보드 위 기물(좌표·id·HP·아군 여부)을 스냅샷으로 변환.
- `FromSaveData(data, database)`: 스냅샷 + `PieceDatabase`로 새 `RunState`를 복원.

### 4. 저장 시스템 (`RunSaveSystem.cs`)
- `Application.persistentDataPath`에 `run_save.json`으로 저장/로드하는 정적 클래스.

### 5. Test 씬 수동 확인용 하네스 (`RunSaveTestHarness.cs`, `Test.unity`)
- 키 입력: `R` 테스트 런 생성(킹 HP2·라운드3·손패 폰1장·보드에 킹1기) / `S` 저장 / `L` 불러오기.
- `OnGUI()`로 현재 상태(킹 HP·라운드·손패 수·보드 기물 수)를 화면에 표시.

### 6. 자동 단위 테스트 (`Assets/ProjectEta/Tests/EditMode/`)
- `ProjectEta.Tests.EditMode.asmdef` 신규 추가(Editor 전용, `Assembly-CSharp` 참조).
- `BoardStateTests`: 보드가 10×10인지, 좌표 범위 판정, 아군·적군 10×5 분할이 올바른지.
- `PieceRuntimeStateTests`: 체력이 음수로 안 내려가는지, `IsDead` 판정.
- `RunStateSaveTests`: 저장→복원 왕복 시 킹 HP·라운드·메타 재화·보드 기물이 원본과 동일한지.

### 7. 코드 주석 규칙 추가 반영
- 사용자 요청에 따라 이번 턴부터 모든 `.cs` 스크립트에 중괄호 단독 줄을 제외한 대부분의 줄에 한글 설명 주석을 추가. 기존 스크립트 20개 전체와 이번에 새로 만든 스크립트에 모두 적용. 앞으로 작성하는 코드에도 계속 적용 예정.

### 8. 사용자 확인 사항
- Unity 상단 `Window > General > Test Runner`의 EditMode 탭에서 `BoardStateTests`/`PieceRuntimeStateTests`/`RunStateSaveTests`가 모두 통과하는 것을 확인.
- `Test` 씬을 Play해 `R`/`S`/`L` 키로 테스트 런 생성 → 저장 → 불러오기가 정상 동작하고, 화면 표시 값이 저장 전후로 동일한 것을 확인.

## 오늘 하지 않은 것

- 정식 세이브 슬롯 UI, 자동 저장/오토세이브 — 필요해지면 이후 UI 단계에서.
- PlayMode 테스트(씬 로드가 필요한 테스트) — 이번엔 EditMode 테스트만 작성.

## 완료 기준 체크

- [x] 기본 데이터(킹 HP·라운드·손패·보드 기물)가 저장·복원된다.
- [x] Test 씬에서 수동으로 저장/불러오기를 실행할 수 있다.
- [x] EditMode 자동 단위 테스트가 작성되고 Test Runner에서 통과한다.
- [x] Console에 Error 0 상태 확인.

6일차 완료 기준을 모두 만족해 6일차를 종료한다.
