# 94일차 : 4성 기물 Ability 연결 및 복합 전투 확장

## 개발 목표

93일차에서 구축한 3성 기물 Ability·AI 통합 구조를 4성 18종으로 확장하고, 기획서에 고유 능력이 명시된 13종에 실제 Ability 데이터를 연결했다.

4성부터 등장하는 오라, 고급 회복, 조건부 피해, 피해 분담, 상태 이상 선택, 공격 후 후속 효과, 장애물 파괴, 타일 봉쇄 등의 복합 전투 규칙을 기존 공통 Ability 프레임워크와 조건부 Resolver를 조합해 처리하도록 확장했다.

## 작업 내용

- 4성 18종 HP·ATK·MovementRules 회귀 검증 추가
- 기획서 기준 4성 고유 Ability 13종·14개 Ability 데이터 연결 추가
- 대장군 인접 아군 공격력 지휘 Aura 추가
- 대포병 공격 후 십자 인접 적 보조 피해 추가
- 대성직자 거리 2 Heal 2 축복 추가
- 공성전차 파괴 가능 장애물 통과·제거 이동 규칙 추가
- 수호대장 인접 아군 피해 분담 처리 추가
- 대마도사 독·화상·속박 공격 후 상태 이상 적용 추가
- 전투치유사 공격 후 인접 최저 HP 아군 회복 추가
- 처형자 HP 1/3 이하 대상 추가 피해 처리 추가
- 폭풍기사 비치명 공격 후 안전 위치 후속 이동 추가
- 철벽기사 미이동 조건 첫 피해 감소 처리 추가
- 전장지휘관 공격 명령·이동 명령 Active Ability 추가
- 대저격수 장거리 공격 추가 피해 처리 추가
- 문지기 이동 후 인접 빈 칸 봉쇄 처리 추가
- 다음 공격 1회용 TemporaryDamageModifierService 추가
- AI ModifyDamage Active 후보 생성·평가 추가
- AI 조건부 추가 피해 예상값 반영 수정
- Tanker 역할의 아군 보호 위치 평가 추가
- Advanced AI에서 동일 대상 복수 Ability 참조 보존 수정
- 선택 기물 Ability 설명 Overlay UI 추가
- 대마도사 공통 상태 이상 에셋 자동 생성 구조 추가
- AbilityEffectData 임시 소환 상한 직렬화 필드 누락 수정
- Day94 Patcher의 지역 변수 path 충돌 수정
- Day94 4성 통합 회귀 테스트와 Editor 검증 메뉴 추가

## 4성 기물 데이터

| PieceId | 이름 | HP | ATK |
| --- | --- | ---: | ---: |
| marshal | 대장군 | 6 | 6 |
| grand_cannon | 대포병 | 6 | 6 |
| imperial_knight | 황실기사 | 6 | 6 |
| high_priest | 대성직자 | 6 | 5 |
| war_rider | 전쟁기수 | 6 | 6 |
| siege_chariot | 공성전차 | 7 | 6 |
| grand_guardian | 수호대장 | 8 | 4 |
| grand_unicorn | 그랜드 유니콘 | 6 | 6 |
| grand_gryphon | 그랜드 그리폰 | 6 | 6 |
| archmage | 대마도사 | 5 | 6 |
| war_cleric | 전투치유사 | 7 | 5 |
| executioner | 처형자 | 6 | 7 |
| storm_knight | 폭풍기사 | 6 | 6 |
| bastion | 철벽기사 | 9 | 3 |
| field_commander | 전장지휘관 | 6 | 5 |
| illusionist | 환술사 | 5 | 5 |
| deadeye | 대저격수 | 5 | 7 |
| gatekeeper | 문지기 | 8 | 4 |

기존 PieceDatabase와 4성 이동 데이터는 유지하고 기획서의 고유 능력 및 복합 전투 규칙을 추가했다.

## 연결한 4성 Ability

### 대장군 — 지휘

인접 아군의 공격력을 1 증가시키는 Passive Aura를 연결했다.

동일 `AuraGroupId`의 지휘 효과는 기존 Aura 규칙에 따라 가장 높은 값 하나만 적용한다.

### 대포병 — 연쇄 포격

직접 공격 후 대상 기준 십자 인접 칸의 적을 탐색해 최대 2명에게 각각 보조 피해 1을 적용한다.

### 대성직자 — 축복

자신의 행동을 사용해 거리 2 이내 아군 1개의 HP를 2 회복한다.

기존 Heal Executor를 재사용하며 최대 HP를 넘지 않는다.

### 공성전차 — 공성

직선 이동·공격 경로에서 파괴 가능한 장애물 1개를 통과 후보로 허용한다.

실제 행동 시 해당 장애물을 제거한 뒤 이동 또는 공격을 계속하도록 처리한다.

파괴 불가 장애물 또는 두 번째 장애물은 통과하지 못한다.

### 수호대장 — 피해 분담

인접 아군이 피해를 받을 때 피해 1을 감소시키고 수호대장 자신의 HP를 1 감소시킨다.

수호대장의 HP가 1이면 발동하지 않는다.

### 대마도사 — 주문 선택

공격 적중 후 독, 화상, 속박 중 하나를 적용한다.

현재 자동 전투 처리에서는 속박 → 화상 → 독 순으로 실행 가능한 첫 상태를 선택한다.

공통 상태 에셋이 프로젝트에 없던 문제를 보완하기 위해 다음 상태 정의를 자동 생성하도록 Patcher를 확장했다.

```text
Status_Poison.asset
Status_Burn.asset
Status_Root.asset
```

### 전투치유사 — 전투 치유

직접 공격으로 피해를 준 뒤 거리 1 이내의 부상 아군 중 현재 HP가 가장 낮은 기물 하나를 1 회복한다.

### 처형자 — 처형

대상의 현재 HP가 최대 HP의 1/3 이하이면 공격 피해를 2 증가시킨다.

### 폭풍기사 — 폭풍 이탈

공격 후 대상이 생존하면 인접한 빈 칸 중 적과 가장 거리가 먼 안전 위치를 선택해 후속 이동한다.

이 후속 이동으로 추가 공격은 발생하지 않는다.

### 철벽기사 — 철벽

자신의 턴에 이동하지 않은 경우 다음 상대 턴에 받는 첫 피해를 2 감소시킨다.

피해는 최소 1 이상 유지한다.

### 전장지휘관 — 명령

두 개의 Active Ability를 연결했다.

- 명령: 공격 — 인접 아군의 다음 공격 피해 +1
- 명령: 이동 — 인접 아군의 다음 행동 이동 후보 +1

다음 공격 1회 피해 보정은 `TemporaryDamageModifierService`에서 저장·소비한다.

### 대저격수 — 초장거리 저격

5칸 이상 떨어진 대상에게 공격하면 피해를 1 증가시킨다.

AI의 예상 피해와 처치 가능성 계산에도 동일 조건을 반영한다.

### 문지기 — 봉쇄

이동 후 인접 빈 칸 하나를 봉쇄한다.

현재 자동 선택은 적에게 가까운 유효 빈 칸을 우선하고, 같은 거리에서는 좌표 순서로 결정한다.

이전에 생성한 봉쇄는 문지기의 다음 행동 시작 시 제거한다.

## 별도 Ability가 없는 4성

기획서에서 별도 고유 능력보다 복합 이동 자체가 핵심인 다음 5종은 기존 MovementRules를 유지한다.

- 황실기사
- 전쟁기수
- 그랜드 유니콘
- 그랜드 그리폰
- 환술사

## 상태 이상 데이터 보완

대마도사 주문 선택 구현 과정에서 프로젝트에 실제 `StatusEffectDefinition` 에셋이 존재하지 않는 문제를 확인했다.

Day94 Patcher가 기존 상태 에셋을 먼저 검색하고, 없을 경우 다음 기본값으로 생성하도록 수정했다.

### 독

- 최대 3중첩
- 기본 3턴
- 중첩당 틱 피해 1

### 화상

- 지속시간 갱신형
- 기본 1턴
- 틱 피해 1

### 속박

- 지속시간 갱신형
- 기본 1턴
- 틱 피해 0

대마도사의 `Archmage_SpellChoice.asset`은 세 상태 정의를 실제 ScriptableObject 참조로 보유한다.

## AI 확장

93일차 AI Ability 후보 구조를 4성에 맞춰 확장했다.

### ModifyDamage Active

전장지휘관의 공격 명령을 AI가 아군 대상 Ability 후보로 생성하고 평가할 수 있도록 `ModifyDamage` Active 효과를 후보 생성 대상에 추가했다.

### 조건부 피해 Preview

AI 공격 점수 계산 시 다음을 실제 예상 피해에 포함한다.

- 처형자 대상 HP 1/3 이하 추가 피해
- 대저격수 5칸 이상 추가 피해
- 다음 공격 1회 공격 명령 보정
- 기존 Aura 공격력 보정

### Tanker 위치 평가

Tanker 역할 기물은 아군과 가까운 위치를 추가로 선호하도록 AI 이동 점수를 보정했다.

### Ability 참조 보존

Advanced Planner가 후보를 재구성할 때 동일 대상에 여러 Ability가 존재해도 원래 `PieceAbilityDefinition` 참조를 유지하도록 수정했다.

## Ability UI

선택한 기물에 Ability가 존재하면 별도의 Overlay에서 다음을 표시한다.

```text
Ability 이름
Active / Passive
Ability 설명
```

UI는 런타임에서 자동 설치되고 기존 기물 선택 이벤트를 이용해 내용을 갱신한다.

## Editor 자동 생성

메뉴 경로:

```text
Project Eta
└─ Day 94
   ├─ Patch Four Star Abilities
   └─ Validate Four Star Integration
```

Patcher는 다음을 자동 처리한다.

- FourStar Ability 폴더 생성
- 4성 Ability 에셋 생성·갱신
- 13종 PieceDefinition Ability 연결
- 대마도사 상태 이상 Definition 검색·자동 생성
- 기존 Ability 연결이 있는 기물 보호

## 회귀 테스트

`Day94FourStarIntegrationTests`에서 다음을 검증한다.

- 4성 기물 정확히 18종
- 18종 HP·ATK 기획값 유지
- 모든 4성 MovementRules 존재
- 고유 Ability 명시 13종 연결
- 전장지휘관 2개 Ability 연결
- 대장군 Aura ATK +1
- 대성직자 Heal 2
- 수호대장 피해 분담
- 수호대장 HP 1 발동 제한
- 처형자 조건부 피해 +2
- 대저격수 장거리 피해 +1
- 철벽기사 첫 피해 감소
- 전장지휘관 다음 공격 1회 버프
- 대포병 최대 2명 보조 피해
- 전투치유사 공격 후 Heal
- 대마도사 공격 후 상태 이상
- 공성전차 파괴 가능 장애물 통과
- 문지기 이동 후 봉쇄
- 대성직자 AI Heal 후보
- 전장지휘관 AI 공격·이동 명령 후보

## 오류 수정

94일차 구현 후 확인된 컴파일·회귀 문제를 함께 수정했다.

### AbilityEffectData

`MaxActiveSummons` 프로퍼티와 생성자 대입은 존재하지만 실제 직렬화 필드 선언이 빠져 있던 문제를 수정했다.

```text
[SerializeField] private int _maxActiveSummons;
```

### Archmage 상태 이상

실제 Poison/Burn/Root ScriptableObject 에셋이 없어 `ApplyStatus`가 실행되지 않던 문제를 수정했다.

현재 `Archmage_SpellChoice.asset`에는 세 상태 에셋 참조가 모두 연결되어 있다.

### Day94FourStarAbilityPatcher

`EnsureStatus()`의 기존 상태 검색용 지역 변수와 새 에셋 생성용 지역 변수 이름이 모두 `path`여서 발생한 CS0136 오류를 수정했다.

검색 경로 변수는 `existingPath`로 변경했다.

## 검증 상태

- GitHub main 최신 커밋 `95ccf1c` 기준 Day94 변경 내용 확인
- `_maxActiveSummons` 직렬화 필드·프로퍼티·생성자 대입 확인
- Day94 Patcher `existingPath` 변수 충돌 수정 확인
- Poison/Burn/Root 실제 상태 에셋 존재 확인
- `Archmage_SpellChoice.asset`의 상태 에셋 참조 3개 연결 확인
- Day94 통합 회귀 테스트 파일 확인
- GitHub 커밋 상태에는 별도 CI 체크가 등록되어 있지 않음
- 실제 Unity 전체 TestRunner 최종 통과 여부는 로컬 Unity 실행 결과를 기준으로 확인 필요

## 다음 개발 방향

95일차에는 5성 8종의 HP·ATK·복합 이동·최상위 Ability를 실제 전투 데이터에 연결한다.

5성은 4성보다 강력한 복합 효과와 전투 규칙 변경이 포함되므로 동일 기물 보유·배치 제한, AI 가치 평가, 합성 결과 연결까지 함께 검증한다.
