# 35일차 개발 일지 — 특수 AI·위협 평가 및 디버그 점수 확장

**날짜**: 2026-09-04  
**기준 커밋**: `d31c285451bbac98cb299035403e1c3af8789a8a`  
**목표**: 33~34일차의 공통·역할별 AI 점수 구조 위에 플레이어 위협도와 특수 기물 활용 점수를 추가하고, F1 디버그 창에서 모든 점수 계층을 직접 확인할 수 있게 한다.

## 오늘 한 일

### 1. 플레이어 위협 맵 추가
`EnemyAIThreatMap`을 추가했다.

현재 보드의 플레이어 기물들이 공격할 수 있는 칸을 10×10 전체에서 계산하고, 각 칸을 몇 개의 플레이어 기물이 위협하는지 누적한다.

```text
ThreatCount 0 = 현재 공격 가능한 플레이어 기물 없음
ThreatCount 1 = 플레이어 기물 1개가 위협
ThreatCount 2 = 플레이어 기물 2개가 위협
...
```

위협 맵은 AI 이동을 별도의 체스 규칙으로 재구현하지 않고 기존 `MovementResolver`를 사용한다.

빈 칸이나 플레이어 기물이 점유한 칸도 가상의 적 기물을 잠시 배치해 실제 공격 후보인지 검사한 뒤 보드 점유 상태를 원상 복구한다.

기절 등으로 `CanAttack == false`인 플레이어 기물은 위협을 생성하지 않는다.

### 2. 후보 위치 시뮬레이션 공통화
`EnemyAICandidateSimulation`을 추가했다.

AI가 실제 보드 상태를 영구 변경하지 않고:

```text
이 후보 칸으로 이동했다고 가정
↓
그 위치에서 다음 이동·공격 가능 범위 계산
↓
보드 상태 즉시 원상 복구
```

흐름을 사용할 수 있게 했다.

이 구조는 특수 기물의 다음 행동 평가와 Chameleon의 다음 형태 미리보기에 사용한다.

### 3. Threat Score 추가
`EnemyAIThreatScoreEvaluator`를 추가했다.

현재 프로토타입 값:

```text
최종 위치를 위협하는 플레이어 기물 1개당  -120
현재보다 위협 수를 1 줄이면             +40
```

위험 칸은 이동 금지로 처리하지 않고 점수 감점으로만 처리한다.

따라서 King 직접 공격처럼 기존 Base Score가 매우 높은 행동은 위험을 감수하고 선택할 수 있다.

공격 행동의 최종 위치도 기존 전투 정책과 맞춘다.

```text
비치명 근접 공격
→ 원위치

근접 처치
→ 대상 칸 점유

원거리 처치
→ 원위치
```

이를 위해 기존 `CombatMovementPolicy`를 재사용한다.

### 4. Cannon 특수 AI
`EnemyAISpecialScoreEvaluator`에 Cannon 전용 평가를 추가했다.

현재 프로토타입 보너스:

```text
즉시 Cannon 공격             +320
이동 후 공격 가능 대상 1개당    +70
이동 후 King 공격선 확보       +650
```

Cannon은 일반 슬라이더처럼 King에게 단순 접근하기보다 직선 공격선을 확보하는 위치를 더 높게 평가한다.

### 5. Grasshopper 특수 AI
Grasshopper의 허들 이동·공격을 활용할 수 있는 위치에 별도 보너스를 부여한다.

현재 프로토타입 보너스:

```text
즉시 특수 공격               +260
이동 후 공격 가능 대상 1개당    +90
이동 후 King 위협             +600
```

실제 이동·공격 가능 여부는 기존 Grasshopper 이동 규칙을 그대로 사용한다.

### 6. Nightrider 특수 AI
Nightrider는 반복 Knight 벡터를 이용한 장거리 기동과 다음 공격 위치를 평가한다.

현재 프로토타입 보너스:

```text
즉시 Nightrider 공격          +220
이동 후 공격 가능 대상 1개당    +60
이동 거리 1당                 +25
이동 후 King 위협             +520
```

장거리 Rider 특성을 실제로 활용하는 이동이 조금 더 높은 점수를 받는다.

### 7. Chameleon 다음 형태 평가
Chameleon은 현재 형태뿐 아니라 이동 후 바뀔 다음 형태까지 미리 평가한다.

순환:

```text
Knight
→ Bishop
→ Rook
→ Queen
→ Knight
```

현재 프로토타입 보너스:

```text
현재 형태 즉시 공격            +120
다음 형태 합법 선택지 1개당      +20
다음 형태로 King 위협           +480
```

실제 `MovementCycleIndex`를 직접 변경하지 않고 다음 형태의 이동 타입만 시뮬레이션에 전달한다.

### 8. 기타 Special 기물 공통 평가
아직 전용 AI 분기가 없는 `PieceCategory.Special` 기물도 최소한의 공통 점수를 받도록 했다.

```text
즉시 특수 공격               +120
이동 후 공격 가능 대상 1개당    +30
이동 후 King 위협             +350
```

35일차 이후 개별 특수 AI가 추가되면 이 공통 분기를 전용 평가로 교체할 수 있다.

### 9. EnemyAIAdvancedPlanner 추가
`EnemyAIAdvancedPlanner`를 추가했다.

최종 점수 구조:

```text
Final Score
=
Base Score
+ Role Bonus
+ Threat Score
+ Special Bonus
```

각 계층:

```text
Base
= 33일차 공통 AI

Role
= 34일차 근접·슬라이더·도약형 역할 성격

Threat
= 35일차 플레이어 공격 위험도

Special
= 35일차 특수 기물 활용도
```

최종 행동 선택의 결정론적 동점 규칙은 33~34일차와 동일하게 유지한다.

### 10. 실제 EnemyTurn에 Advanced Planner 연결
`EnemyAITurnDriver`가 이제 `EnemyAIAdvancedPlanner`를 사용하도록 변경했다.

행동 실행부인 `EnemyAIActionExecutor`는 변경하지 않았다.

따라서:

```text
AI 평가
↓
최종 행동 선택
↓
기존 이동/공격/피해/턴 종료 파이프라인
```

구조를 유지한다.

## F1 디버그 창 확장

### 11. Threat / Special 점수 표시
`AIDebugScoreEntry`에 다음 값을 추가했다.

```text
ThreatScore
SpecialBonus
```

F1 첫 페이지의 최종 표시값은 다음과 같다.

```text
B = Base
R = Role
T = Threat
S = Special
F = Final
```

예:

```text
▶[Slider] Rook (4,8)>(4,5) Move
| B35 R+80 T-120 S+0 F-5
```

실제로 선택될 행동은 기존과 동일하게 `SELECT`와 `▶`로 표시한다.

### 12. 디버그 점수 계산을 실제 AI와 동기화
`AIDebugScoreSnapshotBuilder`도 `EnemyAIAdvancedPlanner`를 기준으로 변경했다.

따라서 디버그 창의 Final Score는 실제 AI와 같은 계산식을 사용한다.

```text
Final
=
Base
+ Role
+ Threat
+ Special
```

### 13. 디버그 창 크기 축소
기존 창 크기:

```text
980 × 680
```

35일차 변경:

```text
588 × 408
```

기존 크기의 정확히 약 0.6배다.

작은 창에서도 버튼이 겹치지 않도록 상단 메뉴를 두 줄로 재구성했다.

유지 기능:

- F1 열기/닫기.
- 수동 갱신.
- Console 출력.
- 현재 Scene.
- 현재 TurnState.
- 후보 수.
- 실제 SELECT 행동.
- 스크롤 로그.
- 창 드래그 이동.

## 테스트

### 14. Day35ThreatAndSpecialAITests 추가
다음 회귀 테스트를 추가했다.

- 플레이어 Rook의 빈 공격선이 위협 맵에 기록되는지 확인.
- 위험한 칸 이동이 안전한 칸보다 낮은 Threat Score를 받는지 확인.
- Cannon 특수 공격 보너스 확인.
- Grasshopper 특수 공격 보너스 확인.
- Nightrider 특수 공격 보너스 확인.
- Chameleon이 다음 형태의 King 위협을 평가하는지 확인.
- `Base + Role + Threat + Special = Final` 확인.
- F1 디버그 스냅샷에 Threat / Special 값이 분리 기록되는지 확인.

## 35일차 최종 구조

```text
EnemyTurn
↓
EnemyAIAdvancedPlanner
├─ EnemyAIPlanner
│  └─ Base
├─ EnemyAIRoleScoreEvaluator
│  └─ Role
├─ EnemyAIThreatMap
├─ EnemyAIThreatScoreEvaluator
│  └─ Threat
└─ EnemyAISpecialScoreEvaluator
   ├─ Cannon
   ├─ Grasshopper
   ├─ Nightrider
   ├─ Chameleon
   └─ Generic Special
↓
Final Score
↓
EnemyAIActionExecutor
↓
기존 전투 파이프라인
```

F1 디버그 흐름:

```text
현재 보드
↓
AIDebugScoreSnapshotBuilder
├─ Base
├─ Role
├─ Threat
├─ Special
└─ Final
↓
Page 1 : AI 점수 로그
```

## 완료 기준 체크

- [x] 플레이어 위협 맵 구조 추가.
- [x] 빈 칸 포함 공격 가능 위치 위협 계산.
- [x] Threat Score 추가.
- [x] 위험 위치 감점.
- [x] 안전한 위치 이동 보너스.
- [x] Cannon 전용 AI 점수.
- [x] Grasshopper 전용 AI 점수.
- [x] Nightrider 전용 AI 점수.
- [x] Chameleon 다음 이동 형태 평가.
- [x] 기타 Special 기물 공통 평가.
- [x] EnemyAIAdvancedPlanner 추가.
- [x] 실제 EnemyTurn에 Advanced Planner 연결.
- [x] F1 창 Threat / Special 점수 표시.
- [x] F1 창 Final 계산을 실제 AI와 동기화.
- [x] 디버그 창 크기 980×680 → 588×408로 축소.
- [x] Day35ThreatAndSpecialAITests 추가.
- [ ] GitHub CI 기반 Unity 컴파일 확인.
- [ ] Unity Test Runner 전체 EditMode 테스트 최종 통과 확인.
- [ ] Battle 씬에서 실제 AI 행동과 F1 SELECT 결과 수동 대조 확인.

35일차에서는 기존 AI 구조를 다시 만들지 않고, 점수 계층을 추가하는 방식으로 위험 판단과 특수 기물 성격을 확장했다. 이후 36일차의 적 증원·라운드 데이터 및 보스 AI에서도 동일한 위협 맵과 점수 평가 구조를 재사용할 수 있도록 구성했다.
