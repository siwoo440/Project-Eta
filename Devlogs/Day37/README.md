# 37일차 개발 일지 — 2×2 보스 점유 및 수평형 대형 모델 구현

**날짜**: 2026-09-05  
**기준 커밋**: `76cd2777cf2b7e0ebf25338f070d3c8e25a6a2f7`  
**목표**: 하나의 보스가 보드 4칸을 실제로 점유하도록 보드·스폰·사망·카운트·AI 중복 방지 기반을 연결하고, 2×2 점유 크기에 맞는 낮고 수평적인 프로토타입 보스 모델을 구성한다.

## 오늘 한 일

### 1. BoardState 대형 기물 점유 보강
`BoardState`의 기존 영역 점유 기반을 2×2 보스가 실제 전투에서 사용할 수 있도록 보강했다.

추가·정리된 핵심 기능:

```text
CanOccupyArea()
TryOccupyArea()
ClearArea()
ClearPiece()
CountPieces()
```

`TryOccupyArea()`는 영역 전체를 먼저 검사한 뒤 점유한다.

따라서 2×2 영역 중 한 칸이라도:

```text
보드 밖
장애물
다른 기물 점유
```

상태라면 전체 배치가 실패하며 일부 칸만 보스가 남는 상황을 방지한다.

### 2. 하나의 PieceRuntimeState로 4칸 점유
2×2 보스는 네 개의 별도 기물이 아니다.

예:

```text
Anchor = (4, 7)

(4,8) (5,8)
  ■     ■

(4,7) (5,7)
  ■     ■
```

네 칸 모두 같은 `PieceRuntimeState` 하나를 참조한다.

```text
(4,7) ┐
(5,7) ├→ Boss RuntimeState
(4,8) ┤
(5,8) ┘
```

따라서 어느 칸을 대상으로 삼더라도 같은 HP·ATK·상태를 가진 동일 보스로 처리된다.

### 3. LargePieceBoardUtility 추가
`LargePieceBoardUtility`를 추가했다.

역할:

```text
PieceDefinition.OccupancySize 읽기
대형 기물 여부 판별
전체 영역 배치 가능 여부 확인
2×2 점유 적용
기존 1×1 스폰을 2×2로 확장
기존 이동 뒤 대형 점유 복구
사망 시 전체 점유 해제
```

일반 1×1 기물과 대형 기물을 가능한 한 같은 보드 흐름에서 사용할 수 있도록 보조 계층으로 구성했다.

### 4. 2×2 보스 사망 점유 해제
기존에는 사망 기물의 기준 칸만 비우는 흐름이 있었다.

37일차에서는 `BoardState.ClearPiece()`를 통해 보드 전체에서 같은 런타임 기물을 참조하는 칸을 찾아 모두 해제할 수 있게 했다.

```text
■ ■
■ ■

HP 0
↓

□ □
□ □
```

상태 이상 피해나 일반 공격으로 죽더라도 보이지 않는 잔여 점유가 남지 않도록 하는 기반이다.

### 5. CountPieces 중복 카운트 수정
2×2 보스를 타일 수로 계산하면 보스 1기가 4기로 계산되는 문제가 있다.

`CountPieces()`를 `HashSet<PieceRuntimeState>` 기반으로 변경했다.

예:

```text
Pawn       1
Rook       1
2×2 Boss   1

총 적      3
```

사망 상태의 기물은 사망 연출 때문에 점유가 잠시 남아 있더라도 남은 기물 수에서 제외한다.

### 6. LargePieceLifecycleController 추가
기존 전투 코드 전체를 대형 기물 전용으로 다시 작성하지 않고 `BattleHooks`에 호환 계층을 추가했다.

사용 훅:

```text
BeforeMove
AfterMove
AfterDamage
```

흐름:

```text
기존 이동/피해 처리
↓
LargePieceLifecycleController
↓
2×2 점유 복구 또는 전체 점유 해제
```

대형 기물 이동 전 원점을 기억하고, 기존 1×1 이동 처리가 끝난 뒤 새 기준점의 전체 점유 영역을 복구한다.

피해 후 HP가 0이 된 대형 기물은 같은 런타임 상태가 차지하던 모든 칸을 해제한다.

### 7. 외부 스폰 대형 기물 자동 보정
기존 `BoardInputController.SpawnTestEnemy()`는 그대로 재사용했다.

기존 경로로 기준 칸에 생성된 대형 기물을 `LargePieceLifecycleController`가 감지해 `OccupancySize` 전체로 확장할 수 있게 했다.

따라서 기존 스폰 시스템 내부를 크게 다시 작성하지 않고 2×2 보스 기반을 연결했다.

### 8. PrototypeBoss37 추가
`Resources/PrototypeBoss37.asset`을 추가했다.

현재 프로토타입 값:

```text
PieceId        prototype_boss_37
Category       Boss
Grade          FiveStar
Movement       Custom
Base HP        30
Base ATK       4
OccupancySize  2×2
```

37일차는 점유 기반 검증이 목적이므로 이동 규칙은 비워 두었다.

보스 이동·공격·행동 패턴은 38일차 범위로 남겨 둔다.

### 9. PrototypeBoss37Spawner 추가
Battle 씬에서 36일차 라운드 초기화가 끝난 뒤 프로토타입 2×2 보스를 자동 생성하는 개발용 스포너를 추가했다.

기준 위치:

```text
Anchor = (0, 8)
Size   = 2×2
```

스폰 전에 네 칸 전체를 확인하며 기존 기물과 충돌하면 생성하지 않는다.

생성 흐름:

```text
PrototypeBoss37 로드
↓
2×2 배치 가능 여부 검사
↓
BoardInputController.SpawnTestEnemy()
↓
기준 칸 생성
↓
LargePieceBoardUtility로 2×2 확장
↓
LargePieceVisualUtility 적용
```

### 10. 2×2 중앙 시각 위치 보정
대형 기물 모델이 기준 칸 한쪽에 치우쳐 보이지 않도록 네 칸 전체의 중앙을 계산한다.

2×2의 경우 기준 칸 중심에서:

```text
X + 0.5 Tile
Z + 0.5 Tile
```

만큼 이동한 지점이 모델 중심이 된다.

### 11. 수평형 대형 보스 모델 추가
초기 대형 모델이 수직으로 길어지는 형태가 되지 않도록 `LargePieceVisualUtility`에서 낮고 수평적인 차체형 외곽을 추가했다.

주요 파츠:

```text
BaseDeck
MainHull
FrontRam
RearBackplate
LeftWing
RightWing
CenterBack
4개 Pod
LeftSideBlade
RightSideBlade
```

즉 탑처럼 위로 솟는 형태보다 2×2 보드 영역을 좌우·앞뒤로 넓게 사용하는 실루엣을 목표로 했다.

### 12. 보스 모델 크기 조정
수평형 외곽 첫 버전은 Game View에서 지나치게 크게 보였다.

조정 과정:

```text
초기 수평형 크기
↓
0.5배로 축소
↓
축소 버전 기준 1.5배 확대
↓
최종 배율 0.75
```

현재 코드:

```text
BossVisualScaleFactor   = 0.75
BossColliderScaleFactor = 0.75
```

최종적으로 처음 큰 버전보다는 작고, 절반 버전보다는 큰 중간 크기로 맞췄다.

수직 높이는 낮게 유지하면서 가로와 앞뒤 폭이 높이보다 크게 보이도록 비균일 스케일을 사용한다.

### 13. 대형 선택 콜라이더 보정
2×2 보스는 모델 중앙만 클릭 가능한 것이 아니라 넓은 차체 영역에서 선택할 수 있도록 `BoxCollider`도 점유 크기에 맞춰 조정했다.

모델 크기 조정과 함께 콜라이더도 최종 0.75 배율 기준으로 조정했다.

## 테스트

### 14. Day37LargeBossOccupancyTests 추가
다음 항목을 검증하는 테스트를 추가했다.

- 2×2 보스가 정확히 4칸 점유.
- 네 칸 모두 같은 `PieceRuntimeState` 참조.
- 한 칸이 막혀 있으면 부분 점유 없이 전체 실패.
- 보드 밖 2×2 배치 실패.
- `ClearPiece()`가 네 칸 전체 해제.
- `CountPieces()`가 2×2 보스를 1기로 계산.
- 죽은 대형 기물은 남은 기물 수에서 제외.
- 2×2 모델 중심 좌표 계산.
- AI가 같은 대형 기물의 행동 후보를 타일 수만큼 중복 생성하지 않는지 확인.
- `PrototypeBoss37`가 Boss / 2×2 데이터로 로드되는지 확인.

### 15. Day37LargeBossVisualTests 추가
최종 수평형 모델 상태를 검증하는 테스트를 추가했다.

검증 방향:

```text
LargeBossShell 생성
여러 수평 파츠 존재
X/Z 크기가 Y보다 큼
최종 0.75 배율 적용
최종 크기에 맞는 BoxCollider 적용
```

## 37일차 최종 구조

```text
PieceDefinition
└─ OccupancySize = 2×2
        ↓
LargePieceBoardUtility
        ↓
BoardState
├─ CanOccupyArea
├─ TryOccupyArea
├─ ClearPiece
└─ CountPieces
        ↓
LargePieceLifecycleController
├─ 이동 점유 복구
├─ 사망 전체 해제
└─ 외부 스폰 점유 확장
        ↓
LargePieceVisualUtility
├─ 2×2 중앙 배치
├─ 낮고 넓은 수평형 모델
└─ 대형 클릭 콜라이더
```

프로토타입 확인 흐름:

```text
Battle
↓
36일차 라운드 초기화
↓
PrototypeBoss37Spawner
↓
2×2 보스 생성
↓
4칸 = 하나의 RuntimeState
```

## 완료 기준 체크

- [x] 2×2 전체 영역 배치 가능 여부 검사.
- [x] 부분 점유 없는 원자적 2×2 배치.
- [x] 네 칸에 동일 PieceRuntimeState 등록.
- [x] 보드 밖 대형 점유 차단.
- [x] 다른 기물과 겹치는 대형 점유 차단.
- [x] 대형 기물 전체 점유 해제.
- [x] CountPieces 대형 기물 중복 카운트 제거.
- [x] 죽은 대형 기물 카운트 제외.
- [x] 기존 BattleHooks 기반 대형 점유 생명주기 연결.
- [x] 기존 SpawnTestEnemy 경로 재사용.
- [x] PrototypeBoss37 데이터 추가.
- [x] Battle 씬 프로토타입 2×2 보스 자동 생성.
- [x] 2×2 영역 중앙에 모델 배치.
- [x] 낮고 수평적인 대형 보스 외곽 모델 추가.
- [x] 수평형 모델 최종 크기 0.75 배율 조정.
- [x] 대형 선택 BoxCollider 조정.
- [x] Day37LargeBossOccupancyTests 추가.
- [x] Day37LargeBossVisualTests 추가.
- [ ] GitHub CI 기반 Unity 컴파일 확인.
- [ ] Unity Test Runner 전체 EditMode 테스트 최종 통과 확인.
- [ ] Battle 씬에서 네 칸 중 어느 위치를 대상으로 해도 같은 보스 HP가 변하는지 수동 확인.
- [ ] 보스 사망 후 네 칸이 실제 Game View/BoardState에서 모두 비워지는지 수동 확인.

37일차에서는 보스 공격 패턴을 만들기 전에 대형 기물의 물리적 기반을 먼저 고정했다. 38일차에서는 이 2×2 단일 런타임 기물 구조 위에 보스 이동·공격·기본 전투 AI를 추가한다.
