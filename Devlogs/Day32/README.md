# 32일차 개발 일지 — 전투 로그 패널·합성 버튼 재배치·배치 턴 배너

**날짜**: 2026-09-04  
**기준 커밋**: `c2c37f75aaea91e4b5966f32272899cdfdb93703`  
**목표**: 6단계 원안에 남아 있던 "전투 로그"를 화면에 보이는 UI로 구현하고, 사용자가 추가로 요청한 합성 버튼 재배치와 배치 턴 전환 알림 배너까지 함께 처리한다.

## 오늘 한 일

### 1. 전투 로그 UI(CombatLogUI) 추가
채팅창처럼 접혀 있다가 눌렀을 때 위로 펼쳐지는 패널로 구현했다.

- 좌하단 "뽑을 카드" 버튼(24,24 / 150×74) 바로 위에 얇은 한 줄 막대(220×36, "전투 로그 ▲") 배치.
- 클릭하면 그 위로 340×420 스크롤 패널이 펼쳐지고, 마우스 휠로 과거 로그를 탐색할 수 있음(`ScrollRect` + `VerticalLayoutGroup`).
- `BattleHooks`(29일차)의 `AfterMove`·`AfterAttack`·`AfterDamage`·`TurnStart`를 구독해 이동·공격 결과(생존/처치와 남은 HP)·상태이상 틱 피해·턴 시작을 자동 기록.
- 최대 200줄까지만 보관하고 오래된 줄부터 제거.

### 2. BattleHooks.AfterDamage에 발생원(source) 추가 — 버그 수정
전투 로그를 만들다가 발견한 설계 공백: `AfterDamage` 훅이 `(target, appliedAmount)`만 전달해서 "이 피해가 전투에서 온 건지 상태이상 틱에서 온 건지" 구분할 방법이 없었다. 그대로 두면 전투 피해가 `AfterAttack`과 `AfterDamage` 양쪽에서 중복 기록된다.

- `BattleHooks.AfterDamage`를 `(PieceRuntimeState target, PieceRuntimeState source, int appliedAmount)`로 확장.
- `DamageResolver.ApplyDamage`가 이미 갖고 있던 `source`를 그대로 함께 전달하도록 수정.
- `PieceInfoPanelUI`(31일차)의 기존 구독도 새 시그니처에 맞춰 갱신(동작은 그대로, source는 사용하지 않음).
- `CombatLogUI`는 `source == null`(공격자가 없는 상태이상 틱)일 때만 `AfterDamage`를 기록해, 전투 피해는 `AfterAttack` 한 곳에서만 기록되도록 분리.

### 3. 합성 버튼 재배치
`FusionPanelUI`의 토글 버튼을 하단 중앙(150×56)에서 우하단 "죽은 카드" 버튼 바로 위(225×56, 가로 1.5배)로 이동. 우하단 버튼과 동일한 앵커·중앙 정렬 좌표(-99, 106)를 사용해 폭이 늘어난 뒤에도 죽은 카드 버튼과 가로 중심이 정확히 맞도록 계산.

### 4. 배치 턴 알림 배너(DeploymentTurnBannerUI) 추가
화면 정중앙에 "배치 턴" 문구가 페이드 인(0.2초) → 유지(1.1초) → 페이드 아웃(0.35초)되는 배너를 추가하고 `TurnManager.TurnChanged`를 구독했다.

### 5. 배너 중복 표시 버그 수정
구현 직후 확인해보니 배치 턴 동안 카드를 배치할 때마다 배너가 계속 다시 떴다. 원인은 `TurnManager.RegisterDeployment()`와 `MarkInitialKingPlaced()`가 상태는 그대로 `DeploymentTurn`인 채로 `TurnChanged`를 재발행하기 때문 — 배너가 "지금 상태가 DeploymentTurn인가"만 보고 "방금 다른 턴에서 넘어왔는가"는 확인하지 않았다.

**수정**: `_previousState`를 기억해 `state == DeploymentTurn && _previousState != DeploymentTurn`(실제 전환 순간)일 때만 배너를 재생하도록 변경. `Bind()` 시점에도 현재 상태로 `_previousState`를 초기화해 연결 직후 오탐지를 방지했다.

### 6. Day32 회귀 테스트 추가
`Day32CombatFeedbackTests`를 추가했다.

주요 검증 항목:

- 이동 시 전투 로그에 "이동" 문구가 담긴 한 줄이 추가됨.
- 비치명 공격은 `AfterAttack`에서만 한 줄 기록되고 `AfterDamage`와 중복되지 않음("생존" 문구 포함).
- 치명 공격은 처치 결과와 처치 후 전진 이동이 각각 한 줄씩(총 2줄) 기록되고 피해 중복 기록은 없음.
- 공격자가 없는 상태이상 틱 피해(`source == null`)는 별도로 한 줄 기록됨("상태 이상 피해" 문구 포함).
- `TurnStart` 훅 발행 시 턴 구분선 로그가 추가됨.
- **배치 턴 배너는 실제 턴 전환(다른 턴 → 배치 턴) 시에만 표시되고, 일반 턴 전환이나 같은 배치 턴 안에서의 재발행에는 반응하지 않음**(오늘 발견한 버그의 회귀 테스트).

## 32일차 최종 구조

```text
BattleHooks.AfterDamage(target, source, amount) — 32일차: source 추가

CombatLogUI (뽑을 카드 버튼 위 채팅창형 패널)
├─ AfterMove   → "이동" 기록
├─ AfterAttack → "생존/처치" 기록
├─ AfterDamage(source == null) → "상태 이상 피해" 기록(전투 피해와 중복 방지)
└─ TurnStart   → "N턴 시작" 구분선

FusionPanelUI 토글 버튼
└─ 우하단 죽은 카드 버튼 위, 가로 1.5배(225×56)

DeploymentTurnBannerUI (화면 중앙)
└─ TurnChanged 구독
    └─ _previousState 비교로 "진입 순간"만 필터링 → 배치 턴 진입 시에만 페이드 인·유지·페이드 아웃
```

## 완료 기준 체크

- [x] 전투 로그를 화면 UI 패널(채팅창형, 뽑을 카드 버튼 위)로 구현.
- [x] 클릭 시 위로 펼쳐지고 휠 스크롤로 과거 로그 탐색 가능.
- [x] `BattleHooks.AfterDamage`에 발생원 추가로 전투/상태이상 피해 로그 중복 제거.
- [x] 합성 버튼 가로 1.5배 확장 및 죽은 카드 버튼 위로 재배치.
- [x] 배치 턴 진입 시 화면 중앙 "배치 턴" 배너 페이드 인/아웃.
- [x] 배치 턴 도중 재발행되는 `TurnChanged`에 배너가 중복 반응하는 버그 수정.
- [x] 로그 기록·중복 방지·배너 트리거 조건 회귀 테스트 6종 추가.
- [ ] Unity Editor Test Runner에서 Day32 테스트와 전체 회귀 테스트 Run All 최종 확인.
- [ ] Battle 씬에서 로그 패널 펼침/스크롤, 합성 버튼 위치, 배치 턴 배너 타이밍을 직접 보고 조정.

32일차는 6단계 원안의 마지막 조각(전투 로그)을 마무리하면서, 그 과정에서 `AfterDamage` 훅의 설계 공백(발생원 정보 없음)을 발견해 고쳤다. 이 훅 확장 덕분에 전투 피해와 상태이상 피해를 로그에서 정확히 구분할 수 있게 됐다. 사용자 요청으로 끼어든 합성 버튼 재배치와 배치 턴 배너 중 배너 쪽에서는 "상태 값 변화"와 "상태 진입 이벤트"를 혼동하는 흔한 함정(같은 상태로의 재발행을 전환으로 오인)을 발견해 함께 고쳤다. 이것으로 기획서 6단계(전투 확장 및 피드백)가 마무리되고, 다음은 7단계(적 AI·증원·보스 시스템)로 넘어간다.
