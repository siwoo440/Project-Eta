# 30일차 개발 일지 — 공격 연출 상태 머신 및 이동·사망 연출 구현

**날짜**: 2026-09-04  
**기준 커밋**: `1922e26742b4d00d2702ce626f9521315c3f3c50`  
**목표**: 기획서 6단계 원안 83~86일차(공격 연출 상태 머신·비치명 복귀·치명 착지·사망 전도)를 하나로 압축해, 기존에 순간이동만 하던 `PieceView`에 실제 연출을 넣는다. 판정 로직(HP·턴·보드 점유)은 12~29일차 그대로 완전히 동기 처리로 유지하고, 그 위에 "이미 확정된 결과를 재생하는" 연출 레이어만 얹는다.

## 오늘 한 일

### 1. 공격 연출 상태 머신을 순수 로직으로 분리
- `AttackAnimationPhase` : Idle→Rising→Approaching→Striking→Recovering→Complete 6단계 enum.
- `AttackAnimationTimings` : 단계별 지속 시간(모두 임시값).
- `AttackAnimationStateMachine` : Unity API를 전혀 쓰지 않는 순수 C# 상태 머신. `Advance(deltaTime)`가 프레임 드랍으로 한 번에 여러 단계 분량의 시간이 들어와도 `while` 루프로 초과분을 이월해 정확한 최종 단계까지 도달하도록 구현.
- Idle/Complete 이후 `Advance` 호출은 안전하게 무시.

### 2. 훅을 통한 연결 — ExecuteAttack은 한 줄도 안 바뀜
- `BoardInputController.Bind`/`OnDestroy`에 `BattleHooks.AfterAttack` 구독/해제를 추가(29일차 `TurnEnd` 구독과 동일한 패턴).
- `HandleBattleHooksAfterAttackVisual`이 **생존 케이스에서만** 근접 공격자의 접근·타격 연출과 방어자의 피격 반응을 재생. 치명타는 기존 `RemovePieceFromBoard`/`MovePieceTo` 경로가 이미 처리하므로 중복 연출 없이 자연스럽게 갈라짐.
- 원거리(Ranged) 공격자는 오늘 범위에서 제외(투사체 연출은 이후 일차).

### 3. PieceView 연출 메서드 추가
- `PlayNonLethalStrikeAndReturn` : 상태 머신을 코루틴에서 매 프레임 `Advance`시키며 위치를 계산.
- `PlayHitReaction` : 생존 시 짧게 기울었다 돌아오는 피격 흔들림.
- `PlayDeathTogglingThenDestroy` : 사망 시 4방향 중 무작위로 쓰러진 뒤 콜백으로 실제 `Destroy`를 위임(기획서 8.6). `RemovePieceFromBoard`가 이 메서드를 호출하도록 교체해, 전투 사망과 28일차 상태이상 틱 사망 모두 자동으로 쓰러짐 연출을 얻음. 보드 점유 해제·죽은 카드 더미 이동은 지금처럼 즉시 처리하고 시각적 파괴만 지연.

### 4. 사용자 피드백을 반영한 3차례 연출 조정
구현 직후 실제 플레이 피드백을 받아 같은 날 안에 세 번 더 다듬었다.

**① 컴파일 오류 수정**: `PieceView.cs`에 `using System;`(Action 콜백용)을 추가하면서 기존의 비한정 `Object.Destroy`/`Object.DestroyImmediate` 호출이 `UnityEngine.Object`와 `System.Object` 사이에서 모호해짐(CS0104). 두 호출을 `UnityEngine.Object.Destroy`/`UnityEngine.Object.DestroyImmediate`로 명시적으로 한정해 해결.

**② 일반 이동 연출 요청 반영**: 처음에는 이동을 완전히 순간이동으로 되돌렸으나, "살짝 떠서 공중을 붕 뜬 채 이동하여 착지" 요청에 따라 `MoveTo`를 3단계 코루틴(`AnimateHoveringMove`)으로 재구성 — ①제자리 상승 → ②뜬 높이를 유지한 채 수평 이동 → ③목표 칸 위에서 착지. 치명 처치 후 전진 이동도 `MoveTo`를 그대로 재사용하므로 동일한 연출을 자동으로 받음.

**③ 근접 공격 연출을 포물선으로 강화**: "공중에 높게 떠서 포물선으로 치고 돌아오는" 요청에 따라 `AnimateNonLethalStrike`의 높이 계산을 재설계.
- Rising: 제자리에서 `_strikeHopHeight`(0.12→0.4로 상향)까지 상승.
- Approaching: `diveHeight = h·(1-p²)`로 최고 높이에서 목표 지점까지 가속 하강 — 접근과 동시에 내려찍는 포물선.
- Striking: 완전히 내려찍은 상태(높이 0)에서 정지해 타격 순간을 표현.
- Recovering: `returnArc = h·4·p·(1-p)`로 0→최고점→0을 그리는 완전한 포물선으로 원위치까지 도약 복귀.

**④ 카드 드래그 고스트의 순간 추적 복구**: 카드를 뽑아 보드에 놓을 때 마우스를 따라다니는 3D 고스트(`_cardDropGhostView`)도 `MoveTo`를 함께 쓰고 있어서, 매 프레임 호출될 때마다 애니메이션이 다시 시작되며 마우스를 뒤늦게 쫓아가는 문제가 생겼다. `PieceView`에 연출 없는 `SnapTo` 메서드를 새로 추가하고, 드래그 고스트 프리뷰(`ShowCardDropGhost`)만 `SnapTo`를 쓰도록 교체해 마우스 추적은 다시 즉시 반응하도록 복구. 실제 배치(`Initialize`)와 실전 이동(`MoveTo`)은 영향 없음.

### 5. Day30 회귀 테스트 추가
`Day30AttackAnimationTests`를 추가했다. Unity 좌표·코루틴에 의존하지 않는 순수 상태 머신만 검증해 완전 자동화했다.

주요 검증 항목:

- `Start()` 직후 Rising 단계로 진입.
- Rising→Approaching→Striking→Recovering→Complete 순서로 정확히 전이.
- 지속 시간에 못 미치는 경과 시간에는 같은 단계에 머묾(진행률도 정확).
- 프레임 드랍으로 여러 단계 분량의 시간이 한 번에 들어와도 한 번의 `Advance` 호출 안에서 정확한 최종 단계까지 이월.
- `Start()` 호출 전(Idle)에는 `Advance`가 아무 효과 없음.
- `Complete` 이후 추가 `Advance` 호출도 상태를 유지.
- 타이밍을 지정하지 않으면 기본 임시값(모두 0보다 큼)으로 생성됨.

## 30일차 최종 구조

```text
AttackAnimationPhase / AttackAnimationTimings
└─ AttackAnimationStateMachine (순수 로직, 프레임 드랍 시 다단계 이월)

BattleHooks.AfterAttack (29일차)
└─ BoardInputController.HandleBattleHooksAfterAttackVisual
    ├─ 생존 + 근접: PlayNonLethalStrikeAndReturn (상승→포물선 접근→타격→포물선 복귀)
    ├─ 생존: PlayHitReaction (피격 흔들림)
    └─ 치명: (연출 없음, RemovePieceFromBoard/MovePieceTo가 별도 처리)

RemovePieceFromBoard
└─ PlayDeathTogglingThenDestroy (무작위 방향 전도 → 콜백으로 실제 Destroy)

PieceView
├─ MoveTo   : 상승→부양 이동→착지 (일반 이동 + 치명 처치 전진 공용)
├─ SnapTo   : 연출 없는 즉시 이동 (카드 드래그 고스트 전용)
└─ Initialize : 최초 배치(연출 없음, 기존과 동일)
```

## 완료 기준 체크

- [x] `AttackAnimationPhase`/`AttackAnimationTimings`/`AttackAnimationStateMachine` 추가(순수 로직, 프레임 드랍 대응).
- [x] `BattleHooks.AfterAttack` 구독으로 `ExecuteAttack` 무수정 연출 연결.
- [x] 비치명 근접 공격 시 접근·타격·복귀 포물선 연출.
- [x] 생존 시 피격 흔들림 연출.
- [x] 사망 시 무작위 방향 전도 후 지연 제거(전투·상태이상 사망 공통 적용).
- [x] 일반 이동을 상승→부양 이동→착지 3단계 연출로 구현(치명 처치 전진 공용).
- [x] 카드 드래그 고스트는 연출 없는 `SnapTo`로 분리해 마우스 즉시 추적 유지.
- [x] `using System;` 도입으로 발생한 `Object` 모호성 컴파일 오류 수정.
- [x] 공격 연출 상태 머신 순수 로직 회귀 테스트 7종 추가.
- [ ] Unity Editor Test Runner에서 Day30 테스트와 기존 전체 회귀 테스트 Run All 최종 확인.
- [ ] Battle 씬에서 이동·근접 공격·사망 연출 및 카드 드래그 배치를 직접 플레이해 체감 검증(수치 임시값 포함).

30일차는 판정과 연출을 철저히 분리하는 데 집중했다. 상태 머신은 Unity와 무관한 순수 C#으로 만들어 완전히 자동 테스트할 수 있게 했고, 실제 좌표·코루틴을 다루는 `PieceView`는 자동 검증이 불가능한 영역임을 인정하고 사용자의 실제 플레이 피드백을 그 자리에서 반영하는 방식으로 진행했다. 그 결과 이동은 상승·부양·착지의 3단계 연출로, 근접 공격은 높이 떠올랐다 포물선으로 내려찍고 다시 포물선으로 복귀하는 연출로 다듬어졌고, 카드 드래그 고스트만은 연출에서 제외해 원래의 즉각적인 마우스 추적을 유지했다. 다음 31일차는 이 연출 위에 HP·피해 팝업, 히트 스톱, 전투 로그를 얹어 6단계를 마무리한다.
