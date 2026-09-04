# 24일차 개발 일지 — 페어리 기물 일괄 확장 및 Rider 이동 규칙

**날짜**: 2026-09-04  
**기준 커밋**: `236561cd020dfcf946f2cfa059ba4bc00a6e7abe`  
**목표**: 23일차에 구축한 데이터 기반 이동 규칙 구조를 실제 다수의 페어리 체스 기물에 적용하고, 반복 도약 계열인 Rider 규칙을 추가해 신규 기물을 전용 enum/switch 없이 데이터만으로 확장할 수 있는지 검증한다.

## 오늘 한 일

### 1. Rider 이동 규칙 추가
- `MovementRuleKind`에 `Rider` 규칙 추가.
- 기존 직렬화 값을 깨지 않도록 `Conditional = 3`, `Rider = 4`로 유지.
- `RiderMovementRule` 신규 구현.
- 동일한 기본 벡터를 같은 방향으로 반복해 여러 착지점을 생성.
- 보드 밖, 장애물, 점유 기물을 만나면 해당 반복 방향 탐색 종료.
- 적 기물이 반복 착지점을 점유하면 공격 후보로 추가하고 이후 반복을 차단.

### 2. MovementRuleFactory 확장
- `MovementRuleKind.Rider` 데이터를 `RiderMovementRule`로 변환하도록 연결.
- 기존 Step / Slide / Leap / Conditional 처리와 동일한 데이터 기반 생성 흐름 유지.
- 여러 `MovementRuleData`가 있는 기물은 기존처럼 `CompoundMovementRule`로 자동 결합.

### 3. 단거리·도약형 페어리 기물 추가
다음 신규 기물을 `PieceMovementType.Custom`과 `MovementRules` 데이터로 추가했다.

- **Ferz**: 대각선 4방향 1칸 Step.
- **Mann**: King과 동일한 인접 8방향 1칸 Step.
- **Dabbaba**: 상하좌우 정확히 2칸 Leap.
- **Alfil**: 대각선 정확히 2칸 Leap.
- **Camel**: `(1,3)` 계열 장거리 Leap.
- **Zebra**: `(2,3)` 계열 장거리 Leap.

### 4. 복합 기물 추가
- **Centaur**: Mann의 인접 8방향 Step + Knight Leap.
- **Waffle / Phoenix**: Wazir의 직교 1칸 Step + Alfil의 대각선 2칸 Leap.
- Waffle은 현재 기획 기준인 Wazir + Alfil 조합으로 반영.

### 5. Rider 계열 기물 추가
- **Nightrider**
  - Knight `(1,2)` 계열 벡터를 같은 방향으로 반복.
  - 반복 착지점이 점유되면 그 뒤 동일 방향 진행 차단.
- **Camelrider**
  - Camel `(1,3)` 계열 벡터를 같은 방향으로 반복.
  - Nightrider와 동일한 `RiderMovementRule`을 공유.

### 6. 기존 합성 3종 데이터 기반 이전
다음 기존 합성 기물에도 실제 `MovementRules` 데이터를 추가했다.

- **Archbishop**: Bishop 대각 Slide + Knight Leap.
- **Chancellor**: Rook 직교 Slide + Knight Leap.
- **Amazon**: Queen 8방향 Slide + Knight Leap.

기존 `PieceMovementType` 값은 하위 호환을 위해 유지하지만 실제 `PieceDefinition` 에셋은 데이터 기반 이동 규칙을 보유하도록 변경했다.

### 7. PieceDatabase 확장
- 23일차까지 등록된 기존 10종에 24일차 신규 10종을 추가.
- 현재 PieceDatabase에 총 20종 기물 정의가 등록되도록 갱신.
- 신규 id:
  - `ferz`
  - `mann`
  - `dabbaba`
  - `alfil`
  - `camel`
  - `zebra`
  - `centaur`
  - `waffle`
  - `nightrider`
  - `camelrider`

### 8. 24일차 회귀 테스트 추가
`Day24FairyMovementTests`에서 다음 항목을 검증하도록 구성했다.

- Ferz 대각선 1칸.
- Dabbaba 직교 2칸 도약 및 중간 기물 무시.
- Camel `(1,3)` / Zebra `(2,3)` 구분.
- Centaur = Mann + Knight.
- Waffle = Wazir + Alfil.
- Nightrider 반복 Knight 벡터.
- Rider 반복 착지점 차단.
- Camelrider 반복 Camel 벡터.
- Archbishop / Chancellor / Amazon의 실제 데이터 규칙 존재.
- PieceDatabase 신규 기물 조회.

## 최종 이동 구조

```text
PieceDefinition
↓
MovementRules[]
↓
MovementRuleFactory
↓
IMovementRule
├─ StepMovementRule
├─ SlideMovementRule
├─ LeapMovementRule
├─ ConditionalMovementRule
├─ RiderMovementRule
└─ CompoundMovementRule
↓
MovementResult
```

## 완료 기준 체크

- [x] Rider 이동 규칙 구조 추가.
- [x] MovementRuleFactory에 Rider 연결.
- [x] Ferz / Mann / Dabbaba / Alfil / Camel / Zebra 추가.
- [x] Centaur / Waffle 복합 이동 추가.
- [x] Nightrider / Camelrider Rider 이동 추가.
- [x] Archbishop / Chancellor / Amazon 데이터 이동 규칙 이전.
- [x] PieceDatabase 신규 10종 등록.
- [x] Day24 이동 규칙 회귀 테스트 작성.
- [ ] Unity Editor 전체 컴파일 오류 0 재확인.
- [ ] Unity Test Runner 전체 EditMode 테스트 통과 재확인.
- [ ] 신규 기물 Battle 씬 수동 이동·공격 검증.

24일차에서는 23일차의 이동 규칙 모듈화를 실제 페어리 기물 대량 추가에 적용했다. Step, Leap, Compound뿐 아니라 Knight/Camel 벡터를 반복하는 Rider 규칙까지 확장했으며, 신규 10종을 전용 enum이나 `MovementResolver` 분기 추가 없이 `PieceDefinition` 데이터 중심으로 등록했다.
