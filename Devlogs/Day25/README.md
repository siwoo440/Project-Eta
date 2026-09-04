# 25일차 개발 일지 — 특수 이동 규칙 및 원거리 공격 확장

**날짜**: 2026-09-04  
**기준 커밋**: `cab66edb337ccc35c56cf2cf08af08e3a9287067`  
**목표**: 24일차까지 완성한 Step·Slide·Leap·Rider 구조를 기반으로 Grasshopper, Cannon, Chameleon 같은 특수 규칙을 확장하고, 26종 기물 구성을 완성한다.

## 오늘 한 일

### 1. Grasshopper 전용 Hopper 규칙 추가
- `MovementRuleKind.Hopper` 추가.
- `HopperMovementRule` 신규 구현.
- Queen과 같은 8방향으로 첫 번째 기물을 탐색.
- 첫 기물을 발판으로 사용하고 바로 뒤 1칸만 착지 가능.
- 발판 앞의 빈 칸에는 이동할 수 없음.
- 발판 바로 뒤가 빈 칸이면 이동 후보.
- 발판 바로 뒤에 적이 있으면 공격 후보.
- 착지점이 아군·장애물·보드 밖이면 해당 방향 이동 불가.

### 2. Cannon 이동·공격 규칙 변경 및 구현
기존 중국 장기식 Screen 공격 규칙은 사용하지 않고 프로젝트 η 전용 규칙으로 변경했다.

#### 이동
- 상 / 하 / 좌 / 우 십자 방향으로 정확히 1칸만 이동.
- 대각선 이동 불가.
- 직교 2칸 이상 이동 불가.

#### 공격
- 상 / 하 / 좌 / 우 직선 방향으로 장거리 공격 가능.
- Rook처럼 같은 행·열을 탐색.
- 같은 방향에서 처음 만나는 기물이 적이면 공격 후보.
- 처음 만나는 기물이 아군이면 그 뒤의 적은 공격할 수 없음.
- 기물을 관통하지 않음.

### 3. Cannon 원거리 처치 정책 추가
- `CombatMovementPolicy` 신규 추가.
- `PieceRoleTag.Ranged`를 가진 기물은 적을 처치해도 공격 대상 칸으로 이동하지 않도록 처리.
- Cannon에 `Ranged` 역할 태그 적용.
- Cannon이 적을 처치하면:
  - 적 기물만 제거.
  - Cannon은 원래 위치 유지.
  - 일반 근접 기물은 기존처럼 처치한 칸을 점유.

### 4. BoardInputController 공격 처리 연결
기존 치명 공격 처리:

```text
적 사망
↓
적 제거
↓
공격자가 대상 칸 점유
```

를 다음 정책 기반으로 변경했다.

```text
적 사망
↓
적 제거
↓
CombatMovementPolicy 확인
├─ 근접 → 대상 칸 점유
└─ 원거리 → 원위치 유지
```

이를 통해 Cannon의 원거리 공격과 기존 근접 전투를 같은 공격 흐름 안에서 처리할 수 있게 했다.

### 5. Canvasser 추가
- `PieceMovementType.Custom` 사용.
- 기존 규칙 데이터 조합만으로 구성.
- 이동 규칙:

```text
Rook Slide
+
Camel Leap
```

- 직교 장거리 이동과 `(1,3)` 계열 도약을 함께 사용.

### 6. Caliph 추가
- `PieceMovementType.Custom` 사용.
- 이동 규칙:

```text
Bishop Slide
+
Camel Leap
```

- 대각선 장거리 이동과 `(1,3)` 계열 도약을 함께 사용.

### 7. Squirrel 추가
- 별도 전용 이동 코드를 만들지 않고 기존 Leap 규칙 3개를 조합.
- 이동 규칙:

```text
Dabbaba
+
Knight
+
Alfil
```

- 직교 2칸 도약.
- Knight 도약.
- 대각선 2칸 도약.
- 모든 도약은 중간 기물을 무시.

### 8. Chameleon 순환 이동 상태 추가
Chameleon은 이동할 때마다 다음 순서로 이동 능력이 변경된다.

```text
Knight
↓
Bishop
↓
Rook
↓
Queen
↓
Knight
...
```

- `PieceRuntimeState`에 `MovementCycleIndex` 추가.
- Chameleon 생성 시 Knight 단계인 0으로 시작.
- 실제 보드 위치가 변경될 때 다음 단계로 진행.
- 일반 기물은 해당 상태를 사용하지 않음.
- `MovementResolver`에 `PieceRuntimeState` 기반 계산 경로 추가.

### 9. Chameleon 저장·복원 지원
- `PieceSaveData`에 `movementCycleIndex` 추가.
- `RunState.ToSaveData()`에서 현재 순환 단계 저장.
- `RunState.FromSaveData()`에서 순환 단계 복원.
- 저장/불러오기 이후에도 Knight/Bishop/Rook/Queen 현재 단계가 유지되도록 구성.

### 10. 특수 이동 규칙 타입 확장
25일차 종료 기준 이동 규칙 종류:

```text
Step
Slide
Leap
Conditional
Rider
Hopper
Cannon
```

Conditional에는 다음 상태가 포함된다.

```text
Pawn
ChameleonCycle
```

### 11. 신규 기물 6종 추가
25일차 신규 기물:

- Grasshopper
- Cannon
- Canvasser
- Caliph
- Squirrel
- Chameleon

모두 `PieceDefinition.asset` 형태로 등록했다.

### 12. PieceDatabase 26종 완성
24일차의 20종에 신규 6종을 추가해 `PieceDatabase`를 총 26종으로 확장했다.

추가 id:

```text
grasshopper
cannon
canvasser
caliph
squirrel
chameleon
```

### 13. Day25 특수 이동 회귀 테스트 추가
`Day25SpecialMovementTests`를 추가해 다음 항목을 검증하도록 구성했다.

- Cannon은 직교 1칸만 이동.
- Cannon은 같은 행·열의 먼 적을 공격 가능.
- Cannon은 중간 아군을 관통하지 않음.
- Cannon은 원거리 처치 후 대상 칸으로 이동하지 않음.
- Grasshopper는 첫 발판 바로 뒤에만 착지.
- Canvasser = Rook + Camel.
- Caliph = Bishop + Camel.
- Squirrel = Dabbaba + Knight + Alfil.
- Chameleon = Knight → Bishop → Rook → Queen 순환.
- Chameleon 순환 단계 저장/복원.
- 신규 6종이 PieceDatabase에서 조회 가능.

## 25일차 최종 구조

```text
MovementResolver
↓
MovementRuleFactory
↓
IMovementRule
├─ StepMovementRule
├─ SlideMovementRule
├─ LeapMovementRule
├─ ConditionalMovementRule
├─ RiderMovementRule
├─ HopperMovementRule
├─ CannonMovementRule
└─ CompoundMovementRule
```

전투 처치 후 이동 정책:

```text
CombatResolver
↓
적 사망
↓
CombatMovementPolicy
├─ Ranged 없음 → 대상 칸 점유
└─ Ranged 있음 → 원위치 유지
```

## 완료 기준 체크

- [x] Grasshopper Hopper 규칙 추가.
- [x] Cannon 십자 1칸 이동 규칙 추가.
- [x] Cannon 직교 장거리 원거리 공격 추가.
- [x] Cannon 공격 시 기물 관통 차단.
- [x] Cannon 원거리 처치 후 원위치 유지.
- [x] CombatMovementPolicy 추가.
- [x] Canvasser 추가.
- [x] Caliph 추가.
- [x] Squirrel 추가.
- [x] Chameleon N → B → R → Q 순환 상태 추가.
- [x] Chameleon 순환 단계 저장·복원 추가.
- [x] PieceDatabase 총 26종으로 확장.
- [x] Day25 특수 이동 회귀 테스트 작성.
- [ ] Unity Editor 전체 컴파일 오류 0 재확인.
- [ ] Unity Test Runner 전체 EditMode 테스트 통과 재확인.
- [ ] Battle 씬에서 신규 6종 수동 플레이 검증.

25일차에서는 단순 벡터 조합으로 표현하기 어려운 특수 이동 기물까지 데이터 기반 이동 시스템에 편입했다. 특히 Cannon은 프로젝트 η 전용 규칙인 '십자 1칸 이동 + 룩 방향 장거리 원거리 공격 + 처치 후 원위치 유지'로 확정했고, Chameleon은 런타임 상태와 저장 데이터까지 연결해 이동 능력 순환을 유지하도록 확장했다. 이로써 PieceDatabase 기준 26종 기물 구성이 완성되었다.
