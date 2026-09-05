# 39일차 개발 일지 — AI 성능 최적화 및 보스 전투 안정화

**날짜**: 2026-09-05  
**비교 기준 커밋**: `eb3e07af1d2b97d6764f7b1cc8bf485954c29197` — `38일차 : 2×2 보스 전투·2페이즈·텔레그래프 통합`  
**39일차 기준 커밋**: `78842db12956c2521ed838ad6812e1e371afd6d0` — `39`  
**목표**: 38일차까지 구현한 일반 적 AI·2×2 보스·2페이즈·텔레그래프 전투를 유지하면서 AI 평가 비용을 줄이고, 플레이 테스트에서 확인된 보스 체력·공격력·피격 문제를 함께 보정해 40일차 통합 테스트로 넘어갈 수 있는 안정적인 상태를 만든다.

## 38일차 대비 핵심 변화

38일차의 주요 상태는 다음과 같았다.

```text
일반 적 AI
+
2×2 보스 이동·공격
+
HP 50% 이하 Phase 2
+
Knight·Pawn 증원
+
위험 칸 텔레그래프
+
다음 EnemyTurn 범위 공격
```

39일차에서는 이 구조를 유지하면서 다음 항목을 보강했다.

```text
AI 후보 조기 제거
↓
EnemyTurn 단위 평가 캐시
↓
Lazy ThreatMap
↓
정밀 평가 예산
↓
Base AI fallback
↓
F1 디버그 중복 계산 제거
↓
AI 성능 통계 표시

보스 HP / ATK 절반 조정
+
보스 HP UI
+
2×2 보스 클릭 피격 보정
```

---

## 1. AI 평가 구조 최적화

기존 AI의 행동 결과와 점수 체계는 유지하고 계산 과정만 최적화했다.

기존 최종 점수 구조:

```text
Base Score
+
Role Score
+
Threat Score
+
Special Score
=
Final Score
```

39일차에서도 이 점수 합산 구조는 그대로 사용한다.

따라서 성능 최적화 때문에 기존 기물의 기본 전략 방향이 크게 달라지지 않도록 했다.

## 2. EnemyAICandidatePruner 추가

비싼 Role·Threat·Special 평가 전에 값싼 후보 검사를 먼저 수행한다.

주요 제거 대상:

```text
사망한 적 기물의 행동
플레이어 기물의 행동
보드 밖 좌표
점유 상태가 달라진 오래된 후보
이미 점유된 이동 칸
실제 대상과 달라진 공격 후보
동일 Actor / Origin / Target / Action 중복 후보
```

정상 후보만 정밀 평가 단계로 전달한다.

## 3. 정밀 평가 예산 추가

후보 수가 비정상적으로 많아지는 상황을 막기 위해 `EnemyAIEvaluationBudget`을 추가했다.

기본 정밀 평가 후보 상한:

```text
128개
```

후보가 예산 이하이면 기존 순서를 유지한다.

후보가 예산을 초과하면:

```text
공격 후보 우선 보존
↓
Base Score 높은 후보 우선
↓
남은 예산을 이동 후보에 사용
```

방식으로 정밀 평가 후보 수를 제한한다.

## 4. EnemyAIEvaluationContext 추가

한 번의 AI 평가 동안 반복 계산되는 정보를 공유하는 컨텍스트를 추가했다.

주요 캐시 대상:

```text
Player King 탐색 결과
ThreatMap
후보 위치 기준 미래 MovementResolver 결과
강제 이동 타입을 사용하는 미래 이동 결과
```

King 탐색과 미래 이동 계산을 후보마다 반복하지 않고 같은 평가 안에서 재사용한다.

## 5. ThreatMap Lazy 계산

기존에는 AI 평가 시 10×10 보드 전체의 위협도를 미리 계산할 수 있었다.

39일차에서는 필요한 좌표가 요청될 때만 계산한다.

```text
ThreatMap 생성
↓
아직 위협 계산 없음
↓
특정 좌표 요청
↓
해당 좌표만 실제 계산
↓
결과 캐시
↓
같은 좌표 재요청 시 캐시 사용
```

이를 통해 실제 평가에 사용되지 않는 보드 칸 계산을 줄였다.

## 6. 미래 이동 계산 캐시

Role AI와 Special AI가 같은 후보의 미래 이동 결과를 반복 계산하지 않도록 했다.

```text
후보 위치의 다음 이동 계산 요청
↓
캐시 존재
├─ Yes → 기존 결과 반환
└─ No  → MovementResolver 실행 후 캐시 저장
```

실제 `MovementResolver` 호출 횟수는 성능 통계에 기록한다.

## 7. 고급 AI 평가 예외 fallback

한 후보의 Role·Threat·Special 평가 중 예외가 발생해도 전체 EnemyTurn을 중단하지 않도록 보호했다.

```text
고급 평가 성공
→ 기존 Final Score 사용

고급 평가 실패
→ 해당 후보의 Base Score 유지
→ 경고 로그 출력
→ EnemyTurn 계속 진행
```

정밀 평가 후보 자체가 비정상적으로 사라진 경우에는 기존 `EnemyAIPlanner`의 합법 행동으로 한 번 더 fallback한다.

AI 평가 오류 하나 때문에 적 턴 전체가 교착되는 상황을 방지하는 것이 목적이다.

## 8. AI 성능 통계 추가

최근 AI 평가의 계산량을 저장하는 `EnemyAIPerformanceStats`를 추가했다.

추적 항목:

```text
전체 Base 후보 수
실제 정밀 평가 후보 수
제외 후보 수
Threat 계산 수
미래 이동 실제 계산 수
소요 시간 ms
Budget 적용 여부
Fallback 사용 여부
```

성능 문제를 체감이 아니라 실제 호출 수와 시간으로 확인할 수 있게 했다.

## 9. F1 AI 디버그 계산 최적화

기존 F1 디버그 UI가 AI 점수를 표시하기 위해 실제 AI와 유사한 계산을 다시 수행하던 부분을 줄였다.

39일차에서는 실제 플래너가 한 번 계산한 `EnemyAIScoredCandidate` 결과를 디버그 화면에서 재사용하도록 정리했다.

갱신 주기도 다음과 같이 완화했다.

```text
기존 0.2초
↓
39일차 0.5초
```

디버그 창 자체가 플레이 중 AI 성능을 과도하게 소비하지 않도록 했다.

## 10. F1 성능 정보 표시

F1 AI 디버그 화면에서 다음 정보를 확인할 수 있게 했다.

```text
전체 후보
실제 평가 후보
Threat 계산 수
미래 이동 계산 수
소요 ms
Budget
Fallback
```

후보 수가 많아질 때 어떤 단계에서 비용이 발생하는지 빠르게 추적할 수 있다.

---

# 보스 플레이 테스트 보정

## 11. 보스 HP 절반 조정

프로토타입 2×2 보스의 HP를 절반으로 낮췄다.

```text
기존 HP 30
↓
39일차 HP 15
```

현재 단계의 목적은 최종 밸런스 확정이 아니라 플레이 가능한 보스 전투 흐름 검증이므로 과도한 전투 시간을 줄이는 방향으로 조정했다.

## 12. 보스 ATK 절반 조정

보스 기본 공격력도 절반으로 낮췄다.

```text
기존 ATK 4
↓
39일차 ATK 2
```

기존 수치에서는 낮은 HP 기물이 보스 공격 한 번에 쉽게 제거됐고, Phase 2 범위 공격과 겹치면 아군이 급격히 전멸할 수 있었다.

다만 Pawn처럼 HP가 1인 기물은 ATK 2에서도 한 번에 사망할 수 있다.

따라서 이후 실제 밸런싱 단계에서는 일반 공격력과 Phase 2 범위 피해를 별도 수치로 분리하는 방안도 검토 대상이다.

## 13. 보스 HP UI 추가

살아 있는 보스가 존재하면 화면 상단에서 보스 HP를 확인할 수 있도록 전용 UI를 추가했다.

표시 예:

```text
BOSS  2x2 프로토타입 보스  HP 11 / 15
██████████████░░░░░░
```

표시 정보:

```text
보스 이름
현재 HP
최대 HP
HP 비율 바
```

피해를 받으면 즉시 갱신하고 사망하면 자동으로 숨긴다.

보스가 Battle 시작 후 늦게 생성되는 경우에도 다시 탐색할 수 있도록 구성했다.

## 14. Phase 2 UI 위치 조정

보스 HP UI가 추가되면서 기존 Phase 2 상태 UI와 겹치지 않도록 위치를 아래쪽으로 조정했다.

상단 UI의 기본 순서:

```text
Turn 상태
Round / Turn 정보
Boss HP
Boss Phase 상태
```

개발 단계에서 필요한 정보를 동시에 확인할 수 있게 했다.

---

# 2×2 보스 플레이어 피격 보정

## 15. 피격 문제 원인

2×2 보스는 일반 1×1 기물과 달리 네 칸을 하나의 `PieceRuntimeState`로 점유한다.

하지만 플레이어 입력은 마우스 Ray가 맞은 위치를 하나의 보드 좌표로 변환해 `AttackTiles`와 비교한다.

이 때문에 모델을 클릭한 위치가 실제 공격 가능한 점유 칸과 다르면 공격이 실패할 수 있었다.

예:

```text
■ ■
■ ■
```

플레이어가 왼쪽 아래 칸만 공격 가능한 상황에서 보스 모델의 오른쪽 위를 클릭하면 기존 입력은 다른 칸을 대상으로 판단할 수 있었다.

## 16. LargePiecePlayerAttackBridge 추가

2×2 적 모델 클릭을 기존 공격 시스템으로 연결하는 보정 브리지를 추가했다.

Battle 씬에서 자동 생성되며 플레이어가 공격 가능한 상태에서 대형 적을 클릭하면 해당 기물의 실제 런타임 상태를 확인한다.

## 17. 실제 공격 가능 점유 칸 탐색

`LargePiecePlayerAttackTargetResolver`가 클릭한 보스가 점유한 네 칸을 검사한다.

```text
2×2 보스 클릭
↓
같은 PieceRuntimeState의 점유 칸 확인
↓
선택된 아군의 AttackTiles와 비교
↓
공격 가능한 점유 칸 발견
↓
기존 공격 API에 해당 좌표 전달
```

보스가 실제 공격 범위 밖이면 강제로 공격을 만들지 않는다.

다른 적의 공격 가능 칸을 같은 보스로 오인하지 않도록 런타임 기물 동일성도 확인한다.

## 18. 기존 CombatResolver 재사용

피격 보정은 새로운 피해 시스템을 만들지 않는다.

최종 공격 흐름:

```text
LargePiecePlayerAttackBridge
↓
TryAttackSelectedPieceTarget()
↓
CombatResolver
↓
BattleHooks
↓
HP 감소
↓
사망 처리
↓
턴 종료
```

따라서 기존 공격 판정·상태 효과·사망·전투 로그 구조와 호환된다.

## 19. 중복 클릭 방지

대형 보스 클릭을 브리지가 성공적으로 공격으로 처리하면 해당 클릭 처리를 즉시 종료한다.

같은 마우스 입력이 기존 `BoardInputController.Update()`에서도 다시 공격으로 실행되는 것을 막기 위해 브리지의 실행 순서를 기존 입력보다 앞에 배치했다.

---

# 테스트 추가

## 20. Day39AIOptimizationTests

AI 최적화 구조의 핵심 동작을 검증하는 EditMode 테스트를 추가했다.

주요 검증 대상:

```text
Lazy ThreatMap
평가 컨텍스트 캐시
미래 이동 중복 계산 방지
후보 중복 제거
정밀 평가 예산
fallback
성능 통계
```

## 21. Day39BossHealthBalanceTests

보스 밸런스와 HP UI 관련 기준을 확인하는 테스트를 추가했다.

프로토타입 보스의 기준 수치:

```text
HP 15
ATK 2
```

## 22. Day39BossHitTargetingTests

2×2 보스 공격 타기팅 보정을 검증한다.

주요 케이스:

```text
같은 2×2 보스의 실제 공격 가능 칸 탐색
다른 적의 공격 가능 칸 오인 방지
공격 범위 밖 보스 강제 공격 방지
```

---

# 변경 파일 요약

39일차 커밋은 38일차 기준으로 AI 최적화·보스 밸런스·보스 UI·보스 피격 수정 관련 파일을 추가·수정했다.

주요 신규 AI 파일:

```text
Assets/ProjectEta/Scripts/AI/
├─ EnemyAICandidatePruner.cs
├─ EnemyAIEvaluationBudget.cs
├─ EnemyAIEvaluationContext.cs
├─ EnemyAIPerformanceStats.cs
└─ EnemyAIScoredCandidate.cs
```

주요 신규 보스 파일:

```text
Assets/ProjectEta/Scripts/Boss/
├─ BossHealthUI.cs
├─ LargePiecePlayerAttackBridge.cs
└─ LargePiecePlayerAttackTargetResolver.cs
```

주요 테스트:

```text
Assets/ProjectEta/Tests/EditMode/
├─ Day39AIOptimizationTests.cs
├─ Day39BossHealthBalanceTests.cs
└─ Day39BossHitTargetingTests.cs
```

주요 수정 대상:

```text
PrototypeBoss37.asset
EnemyAIAdvancedPlanner
EnemyAIRoleScoreEvaluator
EnemyAISpecialScoreEvaluator
EnemyAIThreatMap
EnemyAITurnDriver
BossPhase2Controller
BossPhaseStatusUI
AIDebugScoreSnapshot
AIDebugScoreSnapshotBuilder
ProjectEtaDebugWindow
```

삭제된 파일은 없다.

---

# 검증 상태

## 23. GitHub 최신 상태

39일차 기준 최신 `main` 커밋:

```text
78842db12956c2521ed838ad6812e1e371afd6d0
```

커밋 메시지는 현재 임시로:

```text
39
```

로 등록되어 있다.

`Devlogs/Day39/README.md`는 이 커밋에는 아직 포함되어 있지 않다.

## 24. 자동 검증 상태

작업 과정에서 수행한 정적 소스·구성 검사와 ZIP 무결성 검사는 통과한 상태다.

다만 현재 GitHub 커밋에는 별도의 CI 상태 또는 Workflow 실행 결과가 등록되어 있지 않다.

따라서 다음 항목은 Unity Editor에서 최종 확인이 필요하다.

```text
Unity 실제 컴파일
EditMode Test Runner 전체 통과
Play Mode 보스 실제 피격
보스 HP UI 갱신
Phase 2 텔레그래프와 HP UI 동시 표시
다수 적 상황 AI 처리 시간
Fallback 발생 여부
```

---

# 39일차 완료 상태

39일차 목표였던 AI 성능 안정화 구조는 구현됐다.

추가 플레이 테스트에서 확인된 보스 문제도 같은 일차에서 보정했다.

현재 확인 가능한 코드 기준 완료 항목:

```text
AI 후보 조기 제거
AI 평가 캐시
Lazy ThreatMap
정밀 평가 예산
AI fallback
F1 디버그 계산 최적화
AI 성능 통계
보스 HP 15
보스 ATK 2
보스 HP UI
2×2 보스 클릭 피격 보정
관련 EditMode 테스트
```

다음 40일차에서는 7단계 마지막 작업으로 일반 역할 AI·특수 AI·증원·RoundDefinition·2×2 보스·2페이즈·텔레그래프를 한 전투 흐름에서 통합 검증하고, 상태 효과·전투 훅·연출·로그·저장 회귀까지 확인하는 것이 목표다.
