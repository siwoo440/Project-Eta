# 40일차 개발 일지 — AI·보스 시스템 통합 및 7단계 마무리

**날짜**: 2026-09-05  
**비교 기준 커밋**: `bf38fe97055122669bc60b301db856dd99a4ccbd` — `39일차 : AI 성능 최적화 및 보스 전투 안정화`  
**40일차 기준 커밋**: `95588748873128462b42970683c08a84b71001a3` — `40`  
**목표**: 33~39일차에 구현한 일반 역할 AI·특수 AI·증원·RoundDefinition·2×2 보스 점유·보스 전투·Phase 2·텔레그래프를 실제 라운드 흐름에 연결하고, 대형 기물의 저장·상태 효과 처리까지 보정하여 7단계 적 AI·증원·보스 시스템을 통합한다.

## 39일차 대비 핵심 변화

39일차까지의 전투 구조는 다음과 같았다.

```text
일반 적 AI
+
역할별 / 특수 AI 평가
+
AI 후보 최적화
+
2×2 보스 기본 전투
+
HP 50% 이하 Phase 2
+
텔레그래프
+
보스 HP UI
```

40일차에서는 이 기능들을 실제 라운드 데이터와 저장·상태 효과 흐름에 연결했다.

```text
RoundDefinition
↓
5·10라운드 보스 데이터 선택
↓
일반 적 초기 배치
↓
2×2 보스 생성
↓
턴 기반 증원
↓
일반 AI / 보스 AI EnemyTurn
↓
Phase 2 / 텔레그래프
↓
상태 효과 / BattleHooks
↓
저장·복원
```

---

## 1. 보스 라운드 전용 RoundDefinition 추가

`PrototypeBossRound40.asset`을 추가했다.

현재 보스 라운드 데이터는 다음 정보를 가진다.

```text
DisplayName = Day40 Boss Integration
TurnLimit = 30
IsBossRound = true

Boss
- Resource = PrototypeBoss37
- Anchor = (0, 8)

Initial Enemies
- Pawn   (4, 8)
- Rook   (6, 8)
- Knight (3, 9)
- Bishop (7, 9)

Reinforcements
- Turn 3 : Queen  (2, 9)
- Turn 5 : Cannon (8, 9)
```

일반 적·증원·보스를 하나의 `RoundDefinition` 흐름에서 함께 검증할 수 있는 테스트 라운드 구성이다.

---

## 2. RoundDefinition 보스 데이터 확장

`RoundDefinition`에 보스 생성용 데이터를 추가했다.

추가된 주요 데이터:

```text
IsBossRound
BossResourceName
BossAnchor
HasBossConfiguration
```

기존 초기 적·증원·턴 제한 데이터와 동일한 ScriptableObject에서 보스 여부와 생성 위치도 관리한다.

보스 종류와 시작 위치를 코드에 직접 고정하지 않고 라운드 데이터가 결정할 수 있는 기반을 마련했다.

---

## 3. 5·10라운드 보스 데이터 선택

`RoundRuntimeController.ResolveRoundResourceName()`에서 현재 라운드 번호에 따라 사용할 라운드 데이터를 결정한다.

현재 프로토타입 규칙:

```text
1~4라운드  → PrototypeRound36
5라운드    → PrototypeBossRound40
6~9라운드  → PrototypeRound36
10라운드   → PrototypeBossRound40
```

기획 기준인 5라운드 중간 보스와 10라운드 최종 보스 흐름을 현재 프로토타입 보스 데이터로 우선 검증한다.

중간 보스와 최종 보스의 실제 개별 데이터·패턴 분리는 이후 콘텐츠 제작 단계에서 진행할 수 있다.

---

## 4. RoundDefinition 기반 보스 생성

기존에는 `PrototypeBoss37Spawner`가 Battle 씬에서 별도로 보스를 생성했다.

40일차에서는 보스 라운드일 경우 `RoundRuntimeController`가 `RoundDefinition`을 읽고 보스를 생성한다.

```text
현재 Round 확인
↓
보스 RoundDefinition 로드
↓
BossResourceName으로 PieceDefinition 로드
↓
BossAnchor 전체 점유 가능 여부 확인
↓
기존 BoardInputController 스폰 경로 사용
↓
2×2 점유 확장
↓
LargePieceVisualUtility 시각 보정
```

따라서 라운드 데이터와 실제 보스 생성 흐름이 연결됐다.

---

## 5. 기존 보스 자동 스포너와 중복 생성 방지

`PrototypeBoss37Spawner`는 기존 37~39일차 개발 테스트를 위해 유지한다.

다만 현재 라운드가 `RoundDefinition` 기반 보스 라운드라면 보스 생성을 `RoundRuntimeController`에 양보한다.

```text
일반 테스트 Battle
→ PrototypeBoss37Spawner 사용 가능

5·10라운드 보스 Battle
→ RoundRuntimeController가 보스 생성
→ PrototypeBoss37Spawner는 중복 생성하지 않음
```

기존 개발 편의 기능을 제거하지 않으면서 새 라운드 시스템과 충돌하지 않도록 했다.

---

## 6. 저장 복원 보스 재사용

보스 라운드 세이브를 불러온 경우 이미 보드에 같은 보스가 존재할 수 있다.

40일차에서는 같은 `PieceId`의 살아 있는 적 보스를 먼저 탐색한다.

기존 보스가 있으면:

```text
새 보스 생성 안 함
↓
기존 PieceRuntimeState 재사용
↓
2×2 점유 상태 검사
↓
필요하면 점유 영역 복구
↓
시각·콜라이더 보정
```

저장된 보스 HP와 상태를 유지한 채 전투를 계속할 수 있도록 구성했다.

---

## 7. 2×2 기물 저장 중복 방지

2×2 보스는 보드 네 칸이 같은 `PieceRuntimeState`를 참조한다.

기존 저장 방식에서 타일을 그대로 순회하면 같은 보스가 여러 번 저장될 수 있었다.

40일차에서는 `HashSet<PieceRuntimeState>`를 사용해 같은 런타임 기물을 한 번만 기록한다.

```text
2×2 보스 점유

[B][B]
[B][B]

기존 가능 상태
→ 저장 항목 4건

40일차
→ 저장 항목 1건
```

저장 좌표 역시 현재 순회 중인 타일 좌표가 아니라 `PieceRuntimeState.BoardPosition` 기준 좌표를 기록한다.

---

## 8. 대형 기물 저장 복원

저장 데이터에서 기물을 복원할 때 `PieceDefinition.OccupancySize`를 확인한다.

```text
저장 기물 로드
↓
PieceDefinition 조회
↓
OccupancySize 확인
↓
PieceRuntimeState 생성
↓
TryOccupyArea()
↓
전체 점유 영역 복원
```

따라서 2×2 보스는 로드 직후 네 칸이 동일한 런타임 상태를 다시 참조한다.

일반 1×1 기물은 기존과 동일하게 동작한다.

---

## 9. 구버전 대형 기물 세이브 호환

39일차 이전 방식으로 2×2 보스가 네 개의 저장 항목으로 기록된 세이브도 고려했다.

복원 중 이미 해당 좌표가 먼저 복원한 대형 기물에 의해 점유돼 있으면 중복 항목을 건너뛴다.

```text
구버전 저장

Boss (0,8)
Boss (0,9)
Boss (1,8)
Boss (1,9)

↓ 복원

실제 PieceRuntimeState 1개
↓
2×2 네 칸 점유
```

기존 개발 세이브가 대형 기물 중복 때문에 여러 보스로 증식하지 않도록 했다.

---

## 10. Resources 기반 기물 정의 fallback

기존 `RunState.FromSaveData()`는 기본적으로 `PieceDatabase`에서 `PieceId`를 찾는다.

프로토타입 보스처럼 별도의 Resources 에셋으로 관리되는 기물은 데이터베이스에 없을 수 있다.

40일차에서는 다음 순서로 조회한다.

```text
PieceDatabase.FindById()
↓ 실패
Resources 내부 PieceDefinition 탐색
↓
PieceId 일치 정의 사용
```

이를 통해 `PrototypeBoss37`이 기본 26종 PieceDatabase에 포함되지 않아도 저장 복원이 가능하도록 했다.

---

## 11. 대형 기물 상태 효과 중복 정산 문제 보정

2×2 보스는 네 타일이 같은 런타임 기물을 가리킨다.

기존 턴 종료 상태 효과 처리가 타일 단위로 순회하면 같은 보스에 독·화상·지속 턴 감소가 여러 번 적용될 가능성이 있었다.

40일차에 `LargePieceTurnEndStatusBridge`를 추가했다.

핵심 처리:

```text
Board 전체 순회
↓
HashSet<PieceRuntimeState>
↓
처음 만난 기물만 상태 효과 처리
↓
같은 런타임 상태의 추가 점유 칸은 Skip
```

따라서 1×1 기물과 2×2 보스 모두 실제 기물 하나당 한 번만 TurnEnd 정산을 받는다.

---

## 12. 기존 BattleHooks 유지

새 상태 효과 경로에서도 기존 전투 훅을 그대로 사용한다.

```text
TurnEnd
↓
StatusEffectTickResolver
↓
BeforeDamage
↓
HP 적용
↓
AfterDamage
↓
상태 지속 턴 감소
```

새로운 별도 피해 시스템을 만들지 않고 기존 상태 효과·피해·로그 파이프라인과 연결했다.

---

## 13. 상태 효과 사망 처리 재사용

독·화상 등의 턴 종료 피해로 기물이 사망하면 기존 `BoardInputController`의 사망 정리 흐름을 재사용한다.

주요 정리 대상:

```text
보드 점유 해제
PieceView 사망 연출
화면 오브젝트 정리
DeadCardPile 처리
대형 기물 전체 점유 해제
```

호환 호출에 실패하더라도 최소한 보드의 동일 `PieceRuntimeState` 점유 전체를 해제하는 fallback을 둔다.

---

## 14. 일반 AI와 보스 AI 통합 유지

39일차의 `EnemyAITurnDriver` 통합 구조를 그대로 사용한다.

EnemyTurn에서:

```text
Phase 2 텔레그래프 처리
↓
일반 AI 후보 계산
+
보스 AI 후보 계산
↓
점수 비교
↓
행동 하나 선택
↓
실행
↓
EnemyTurn 종료
```

40일차에서는 이 AI 자체를 다시 작성하지 않고, 앞단의 라운드 생성과 뒷단의 저장·상태 효과 구조를 연결하는 데 집중했다.

---

## 15. 증원 시스템과 보스 라운드 연결

36일차에 만든 턴 기반 증원 시스템도 동일한 `RoundRuntimeController`에서 유지한다.

보스 라운드에서도 일반 적과 보스가 배치된 뒤 지정 턴에 증원이 발생한다.

현재 테스트 데이터:

```text
Turn 3
→ Queen 증원

Turn 5
→ Cannon 증원
```

이를 통해 다음 흐름을 하나의 라운드에서 확인할 수 있다.

```text
일반 적
→ 보스 기본 전투
→ 증원
→ Phase 2
→ 텔레그래프
→ 추가 증원
→ 보스 전투 계속
```

---

## 16. Day40AIAndBossIntegrationTests 추가

40일차 통합 회귀 테스트를 추가했다.

주요 검증 항목:

```text
5라운드 보스 데이터 선택
10라운드 보스 데이터 선택
일반 라운드 데이터 유지
PrototypeBossRound40 로드
보스 Resource / Anchor 검증
초기 적·증원 데이터 검증
2×2 보스 저장 1건 보장
2×2 전체 점유 복원
구버전 중복 보스 저장 호환
PieceDatabase 미등록 보스 Resources 복원
대형 기물 상태 효과 1회 정산
```

AI·보스 통합에서 특히 위험한 데이터 경계와 대형 점유 회귀를 자동 테스트 대상으로 묶었다.

---

## 17. 7단계 통합 상태

33~40일차의 구현 흐름은 다음과 같다.

```text
33일차
AI 평가 코어

34일차
기본 역할별 AI

35일차
특수 AI·위협 평가

36일차
적 증원·RoundDefinition

37일차
2×2 보스 점유

38일차
보스 전투·Phase 2·텔레그래프

39일차
AI 성능 최적화·보스 전투 안정화

40일차
라운드·AI·증원·보스·상태 효과·저장 통합
```

40일차를 기준으로 7단계의 주요 시스템이 하나의 전투 상태 안에서 연결되는 구조를 갖는다.

---

## 18. 현재 확인 포인트

GitHub 최신 커밋 기준으로 확인해야 할 핵심 플레이 흐름:

```text
5 또는 10라운드 진입
↓
PrototypeBossRound40 선택
↓
일반 적 4기 + 2×2 보스 생성
↓
일반 AI / 보스 AI 행동
↓
Turn 3 Queen 증원
↓
보스 HP 50% 이하
↓
Phase 2 진입
↓
Knight·Pawn Phase 2 증원
↓
텔레그래프
↓
PlayerTurn 회피
↓
다음 EnemyTurn 범위 공격
↓
Turn 5 Cannon 증원
↓
보스 사망
↓
2×2 전체 점유 해제
```

추가 확인 대상:

```text
보스 저장 후 불러오기
보스 HP 유지
보스 2×2 점유 유지
대형 보스 상태 효과 1회 정산
일반 적 상태 효과 기존 동작 유지
증원 칸 충돌 시 안전한 실패
EnemyTurn 교착 없음
```

---

## 19. 현재 검증 상태

최신 GitHub 커밋에는 `Day40AIAndBossIntegrationTests`가 포함돼 있다.

다만 현재 저장소 커밋에는 GitHub Actions 또는 별도의 Commit Status 결과가 등록되어 있지 않다.

따라서 GitHub 소스 구조와 테스트 코드 포함 여부는 확인할 수 있지만, 실제 Unity Editor에서의 컴파일·EditMode 전체 테스트 통과 여부는 로컬 Unity Test Runner 결과를 기준으로 최종 확인해야 한다.

---

## 20. 다음 단계

40일차까지 7단계의 적 AI·증원·보스 시스템을 통합했다.

다음 개발 단계는 8단계 로그라이트 런 및 영구 성장이다.

41일차 예정 핵심:

```text
RunState
RoundState
BattleState

책임 재정리
↓
전투 단위 상태와 전체 런 상태 분리
↓
향후 10라운드 진행·보상·상점·이벤트·메타 성장 기반 준비
```

40일차 통합 구조를 유지한 상태에서 라운드 한 판이 아니라 전체 10라운드 런을 관리하는 구조로 확장한다.
