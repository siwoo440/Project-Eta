# 36일차 개발 일지 — 라운드 데이터·적 증원 및 상단 전투 정보 UI

**날짜**: 2026-09-05  
**기준 커밋**: `8463ae7181fc704f064ba28dc13afe0b3209fd54`  
**목표**: 기존 Battle 씬의 테스트 적 배치를 라운드 데이터 기반 구조로 확장하고, 지정 턴 증원과 상단 중앙 라운드 정보 UI를 추가한다.

## 오늘 한 일

### 1. RoundDefinition 추가
`RoundDefinition`을 추가했다.

한 라운드에서 관리할 값을 ScriptableObject 데이터로 분리했다.

```text
DisplayName
TurnLimit
IsBossRound
InitialEnemies
Reinforcements
```

각 적 스폰 데이터는 `EnemySpawnDefinition`으로 관리한다.

```text
PieceId
Position
SpawnTurn
```

`SpawnTurn = 0`은 시작 적, 1 이상은 해당 일반 턴부터 등장 가능한 증원으로 사용한다.

### 2. 36일차 프로토타입 라운드 데이터 추가
`Resources/PrototypeRound36.asset`을 추가했다.

현재 테스트 값:

```text
Turn Limit : 30

시작 적
Pawn    (4, 8)
Rook    (6, 8)
Knight  (3, 9)
Bishop  (7, 9)

증원
Turn 3 → Queen   (2, 9)
Turn 5 → Cannon  (8, 9)
```

현재는 일반 라운드이므로 `IsBossRound = false`다.

### 3. RoundRuntimeController 추가
`RoundRuntimeController`를 추가했다.

Battle 씬에서 자동 생성되며 별도 Inspector 연결 없이 다음 흐름을 관리한다.

```text
BattleController / BoardInputController 탐색
↓
RoundDefinition 로드
↓
PlayerStartingDeck26 카탈로그 로드
↓
시작 적 구성 확인
↓
TurnManager 이벤트 연결
↓
지정 턴 증원 처리
↓
상단 라운드 정보 UI 연결
```

### 4. 기존 적 스폰 기능 재사용
새 스폰 시스템을 중복 구현하지 않고 기존 `BoardInputController.SpawnTestEnemy()`를 재사용한다.

따라서 증원도 기존과 동일하게:

```text
PieceRuntimeState 생성
↓
BoardState 점유 등록
↓
PieceView 생성
↓
기존 AI가 다음 EnemyTurn부터 자동 인식
```

흐름을 사용한다.

### 5. 기존 폰+룩 테스트 부대와 호환
BattleController가 먼저 생성하는 기존 테스트 적:

```text
Pawn (4, 8)
Rook (6, 8)
```

은 동일 위치·동일 PieceId이면 중복 생성하지 않는다.

그 뒤 RoundDefinition에만 있는:

```text
Knight (3, 9)
Bishop (7, 9)
```

을 추가해 초기 적 구성을 완성한다.

세이브 등으로 이미 다른 적 구성이 존재하면 36일차 시작 적 자동 배치를 건너뛰어 기존 전투 상태를 보호한다.

### 6. 적 증원 처리
새 일반 플레이어 턴이 시작될 때 `Reinforcements`를 확인한다.

예:

```text
Turn 3
→ Queen (2, 9)

Turn 5
→ Cannon (8, 9)
```

목표 칸이 비어 있으면 정상 등장한다.

목표 칸이 이미 점유되어 있거나 PieceId를 찾지 못하면:

```text
증원 실패
↓
Warning 로그
↓
해당 증원 이벤트 완료 처리
```

로 처리한다.

같은 증원을 매 턴 반복 시도하지 않는다.

### 7. 라운드 턴 제한 데이터 연결
기존 BattleController의 테스트 턴 제한 값과 `RoundDefinition.TurnLimit`을 런타임에서 동기화한다.

현재:

```text
RoundDefinition.TurnLimit = 30
```

이므로 일반 라운드는 30턴 제한을 사용한다.

이후 라운드 에셋의 값만 바꾸면 기존 턴 제한 판정도 동일한 값을 사용하도록 호환 연결했다.

### 8. 현재 적 수 계산
`RoundRuntimeController.CountCurrentEnemies()`를 추가했다.

현재 BoardState를 직접 순회해 살아 있는 적 기물 수를 계산한다.

향후 2×2 보스처럼 하나의 기물이 여러 타일을 점유해도 한 기물로 계산할 수 있도록 `HashSet<PieceRuntimeState>` 기반으로 중복을 제거한다.

## 상단 라운드 정보 UI

### 9. RoundSummaryUI 추가
사용자 요청에 따라 플레이어 화면 중앙 상단의 기존 턴 상태 UI 바로 아래에 새로운 보조 UI를 추가했다.

표시 형식:

```text
Round : 3    Turn : 5 / 30    현재 적 : 6
```

각 값은 런타임 상태에서 직접 읽는다.

```text
Round
= RunState.CurrentRound

Turn
= TurnManager.TurnNumber

30
= RoundDefinition.TurnLimit

현재 적
= 현재 BoardState의 실제 생존 적 수
```

### 10. 기존 TurnStatusUI 바로 아래 배치
기존 TurnStatusUI는:

```text
상단 중앙
Y = -24
크기 = 620 × 64
```

구조다.

새 RoundSummaryUI는:

```text
상단 중앙
Y = -96
크기 = 620 × 42
```

로 배치했다.

따라서 메인 턴 UI 바로 아래 약 8픽셀 간격으로 나타난다.

화면 구조:

```text
┌──────────────────────────────────────────┐
│ 5턴 · 플레이어 턴 · 이동/공격/소환 1회 │
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│ Round : 3   Turn : 5 / 30   현재 적 : 6 │
└──────────────────────────────────────────┘
```

### 11. UI 자동 갱신
다음 상황에서 자동으로 표시값을 다시 읽는다.

- 턴 변경.
- 시작 적 배치.
- 증원 등장.
- 적 처치.
- 라운드 상태 변경.

적 처치처럼 별도 라운드 이벤트가 직접 발생하지 않는 경우도 빠르게 반영하도록 짧은 주기의 가벼운 갱신을 사용한다.

### 12. UI 입력 차단 방지
새 UI의 Image와 Text는 `raycastTarget = false`로 설정했다.

따라서 상단 정보 UI가 보드 클릭 입력을 막지 않는다.

## 테스트

### 13. Day36RoundAndReinforcementTests 추가
다음 항목을 검증하는 EditMode 테스트를 추가했다.

- 요청한 라운드 UI 문자열 형식.
- `PrototypeRound36` Resources 에셋 로드.
- 기본 30턴 제한.
- 시작 적 4기 데이터.
- 증원 2건 데이터.
- 증원 지정 턴 판정.
- 실제 BoardState 기반 현재 적 수 계산.
- RunState.CurrentRound / TurnManager.TurnNumber / TurnLimit / 현재 적 수를 조합한 UI 표시.

## 36일차 최종 구조

```text
PrototypeRound36.asset
↓
RoundDefinition
├─ TurnLimit
├─ InitialEnemies
└─ Reinforcements
        ↓
RoundRuntimeController
├─ 기존 테스트 적과 중복 방지
├─ 시작 적 추가
├─ TurnManager 감시
├─ 지정 턴 증원
└─ 현재 적 수 계산
        ↓
BoardInputController.SpawnTestEnemy()
        ↓
기존 BoardState / PieceView / AI 시스템
```

상단 UI:

```text
TurnStatusUI
↓
RoundSummaryUI
├─ Round
├─ Turn / TurnLimit
└─ 현재 적
```

## 완료 기준 체크

- [x] RoundDefinition 데이터 구조 추가.
- [x] EnemySpawnDefinition 추가.
- [x] PrototypeRound36 Resources 에셋 추가.
- [x] 시작 적 4기 데이터 구성.
- [x] Turn 3 Queen 증원 데이터.
- [x] Turn 5 Cannon 증원 데이터.
- [x] RoundRuntimeController 자동 생성.
- [x] 기존 BoardInputController 적 스폰 기능 재사용.
- [x] 기존 Pawn + Rook 테스트 부대 중복 방지.
- [x] 기존 세이브/커스텀 적 구성 보호.
- [x] 지정 턴 증원 처리.
- [x] 점유된 증원 칸 안전 처리.
- [x] RoundDefinition 턴 제한 연결.
- [x] 현재 생존 적 수 계산.
- [x] 상단 중앙 RoundSummaryUI 추가.
- [x] 기존 TurnStatusUI 바로 아래 배치.
- [x] `Round : N    Turn : N / Limit    현재 적 : N` 형식 구현.
- [x] 턴·증원·적 수 변화 자동 갱신.
- [x] UI raycast 차단 방지.
- [x] Day36RoundAndReinforcementTests 추가.
- [ ] GitHub CI 기반 Unity 컴파일 확인.
- [ ] Unity Test Runner 전체 EditMode 테스트 최종 통과 확인.
- [ ] Battle 씬에서 Turn 3 / Turn 5 증원 실제 수동 확인.
- [ ] 상단 UI 위치·해상도별 겹침 여부 실제 Game View 확인.

36일차에서는 기존 전투와 AI를 다시 만들지 않고 라운드 데이터 계층을 위에 추가했다. 이후 라운드별 적 구성, 증원 패턴, 보스 라운드 턴 제한을 에셋 데이터만으로 확장할 수 있는 기반을 마련했다.
