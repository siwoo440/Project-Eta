# 31일차 개발 일지 — 기물 정보 패널 UI 및 상태이상 하이라이트 수정

**날짜**: 2026-09-04  
**기준 커밋**: `7ccf71bd8b269a2d00e759edad3a7bc703a35730`  
**목표**: 기획서 11.6(기물 상세 정보창)에 해당하는 기능을 새로 추가한다. 기물을 선택하면 화면 우측 상단에 이름·등급·ATK·현재 체력·역할·상태이상을 보여주는 패널을 띄우고, 그 과정에서 발견한 상태이상 하이라이트 버그를 함께 고친다. (기획서 6단계 원안의 "피해·HP 피드백·전투 로그" 항목은 32일차로 미룬다.)

## 오늘 한 일

### 1. 기존 이동/공격 하이라이트 점검, 버그 발견
11일차부터 있던 `TrySelectPieceAt`의 후보 하이라이트 계산이 `MovementResolver.GetReachableTiles(definition, position, isPlayerPiece, board)` 구형 4-인자 오버로드를 쓰고 있어서, 28일차에 추가한 `PieceRuntimeState` 기반의 기절·속박 게이팅과 25일차 카멜레온 순환 단계를 전혀 반영하지 못하고 있었다. 즉 기절·속박에 걸린 기물을 선택해도 화면엔 평소와 같은 이동·공격 후보가 그대로 표시되는 불일치가 있었다.

**수정**: `TrySelectPieceAt`이 `MovementResolver.GetReachableTiles(_selectedPiece, _boardView.State)`(런타임 상태 기반 오버로드)를 사용하도록 교체. 이제 기절 중에는 후보가 아예 없고, 속박 중에는 이동 후보만 사라지고 공격 후보는 유지된다.

### 2. BoardInputController에 선택 변경 이벤트 추가
- `event Action<PieceRuntimeState> SelectionChanged` 추가.
- `TrySelectPieceAt` 성공 시 선택된 기물과 함께 발행, `DeselectPiece`에서 `null`과 함께 발행(같은 기물 재클릭으로 인한 해제도 공통 경로라 자동 처리됨).
- `RemovePieceFromBoard`에 안전장치 추가: 지금 선택 중인 기물이 죽으면(전투 사망이든 28일차 상태이상 틱 사망이든) 자동으로 `DeselectPiece()`를 호출해 정보 패널이 죽은 기물을 계속 보여주지 않게 함.
- `BattleHooks` 공개 접근자도 추가해, UI 쪽에서 `AfterDamage`/`TurnEnd` 훅을 직접 구독할 수 있게 함.

### 3. PieceInfoPanelUI 추가
`HandUI`/`DeckPanelUI`/`FusionPanelUI`와 동일한 런타임 Canvas 생성 패턴을 그대로 따랐다.

- 우측 상단 고정 패널(선택 없으면 숨김).
- 표시 항목: 카드 아트(또는 `CardView`와 동일한 규칙의 PieceId 앞 3글자 약칭) · 이름 · 등급(N성) · ATK · **현재 HP / 최대 HP**(기본값이 아닌 실제 전투 중 체력) · 역할 태그 한글 요약(근접·원거리·도약·슬라이더·라이더·지원·탱커·공격·소환) · 상태이상 요약(예: "독 2중첩(3턴), 기절(1턴)") · 설명.
- 이동 범위는 별도로 그리지 않음 — 보드 위 하이라이트가 이미 그 역할을 하므로 패널은 텍스트 정보에 집중.

### 4. 이벤트 기반 실시간 갱신(Update 폴링 없음)
프로젝트 전체가 `TurnChanged`/`DeckChanged`/`AttackResolved` 같은 이벤트 구독 방식으로 UI를 갱신하는 컨벤션을 그대로 따랐다.

- `SelectionChanged` : 선택이 바뀔 때마다 즉시 갱신.
- `BattleHooks.AfterDamage`(29일차 훅) : 지금 표시 중인 바로 그 기물이 피해를 입으면 즉시 HP 갱신. 전투 피해와 28일차 독·화상 틱 피해 모두 `DamageResolver`를 거치므로 둘 다 자동으로 반영됨.
- `BattleHooks.TurnEnd`(29일차 훅) : 매 턴 종료마다 표시 중인 기물의 상태이상 지속 턴·중첩 변화를 반영.

### 5. Day31 회귀 테스트 추가
`Day31PieceInfoPanelTests`를 추가했다. `CardView`의 기존 리플렉션 테스트 패턴(Day26)을 그대로 따라 private static 표시 로직을 검증했다.

주요 검증 항목:

- 역할 태그가 없으면 "-", 여러 개면 " · "로 연결된 문구를 만드는지.
- 상태이상이 없으면 "없음", 중첩 2 이상이면 중첩 수까지, 여러 상태는 쉼표로 연결되는지.
- Artwork 약칭이 `CardView`와 동일하게 PieceId 앞 3글자·소문자로 만들어지는지.
- 기물 선택/해제 시 `SelectionChanged`가 올바른 인자(선택된 기물 / `null`)로 발행되는지.
- **회귀**: 기절한 기물을 선택하면 이동 후보가 비어 있어 실제 이동 시도가 거부되는지(수정 전이었다면 통과하지 못했을 케이스).

## 31일차 최종 구조

```text
BoardInputController
├─ TrySelectPieceAt → GetReachableTiles(PieceRuntimeState, board) (수정: 기절·속박·카멜레온 반영)
├─ SelectionChanged(피스 또는 null) 발행
└─ RemovePieceFromBoard → 선택 중이던 기물이면 자동 DeselectPiece

PieceInfoPanelUI (우측 상단 Canvas)
├─ SelectionChanged 구독 → 선택 변경 시 즉시 갱신
├─ BattleHooks.AfterDamage 구독 → 표시 중인 기물이 맞으면 즉시 갱신
└─ BattleHooks.TurnEnd 구독 → 상태이상 지속 턴·중첩 변화 반영

표시 내용: 이름 · 등급 · ATK · 현재HP/최대HP · 역할 태그 · 상태이상 요약 · 설명
```

## 완료 기준 체크

- [x] 기절·속박 하이라이트가 실제 행동 가능 여부를 정확히 반영하도록 수정.
- [x] `BoardInputController.SelectionChanged` 이벤트 추가.
- [x] `PieceInfoPanelUI` 우측 상단 패널 추가(이름·등급·ATK·현재 HP·역할·상태이상·설명).
- [x] `BattleHooks.AfterDamage`/`TurnEnd` 구독을 통한 실시간 갱신(Update 폴링 없음).
- [x] 선택 중이던 기물이 죽으면 자동으로 선택 해제.
- [x] 표시 문구·선택 이벤트·하이라이트 버그 회귀 테스트 5종 추가.
- [ ] Unity Editor Test Runner에서 Day31 테스트와 전체 회귀 테스트 Run All 최종 확인.
- [ ] Battle 씬에서 패널 위치·크기·글자 크기를 직접 보고 조정.

31일차는 기획에 없던 요청(기물 정보 패널)을 처리하면서, 그 과정에서 27~29일차에 만든 상태이상·훅 인프라를 그대로 재사용했다 — 새로운 상태 갱신 로직을 따로 만들지 않고 이미 있는 `SelectionChanged`류 이벤트 패턴과 `BattleHooks`만으로 실시간 패널을 완성했다. 동시에 11일차부터 있었지만 아무도 알아채지 못했던 하이라이트-실제상태 불일치 버그를 함께 고쳤다. 6단계에서 원래 31일차 몫이었던 "피해·HP 피드백, 히트 스톱, 전투 로그, 통합 검증"은 32일차로 넘어간다.
