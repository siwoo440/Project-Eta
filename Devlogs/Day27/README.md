# 27일차 개발 일지 — 상태 효과 프레임워크 및 면역 태그

**날짜**: 2026-09-04  
**기준 커밋**: `5844b31ebf8a296b2a76d0144245c2d7e08ee3ee`  
**목표**: 기획서 6단계(전투 확장 및 피드백) 원안 76·81일차를 압축해, 28일차에 독·화상·기절·속박 4종을 얹을 수 있도록 상태 이상의 공통 데이터 구조·부여/해제/지속 턴 API·저장 복원·면역 배관을 먼저 완성한다.

## 오늘 한 일

### 1. 상태 이상 종류·중첩 방식 데이터 타입 추가
- `StatusEffectType` : `Poison`, `Burn`, `Stun`, `Root` 4종을 담는 `[Flags]` 비트 플래그 열거형. `PieceRoleTag`와 동일한 패턴으로 면역 태그 비교에 그대로 재사용.
- `StatusStackMode` : `StacksAdd`(중첩 누적, 독 계열) / `RefreshDuration`(중첩 없이 지속 턴만 갱신, 화상 계열) 2가지 재적용 방식.

### 2. StatusEffectDefinition (ScriptableObject) 추가
- `PieceDefinition`과 동일한 데이터 주도 설계를 따름.
- 필드: 상태 종류, 표시 이름, 중첩 방식, 최대 중첩 수, 기본 지속 턴, 설명.
- `MaxStacks`·`DefaultDurationTurns`는 읽기 시 최소 1로 보정해 잘못된 값 입력을 방지.

### 3. RuntimeStatusEffect (POCO) 추가
- 기물 1개에 실제로 걸린 상태 1건의 가변 데이터: 정의 참조, 남은 지속 턴, 현재 중첩 수.
- `Reapply()` : 재적용 시 지속 턴 갱신 + 중첩형이면 최대치까지 중첩 증가.
- `Tick()` : 턴 종료 시 지속 턴 1 감소, 0 이하가 되면 만료 여부를 반환.
- `RestoreState()` : 저장 데이터로부터 지속 턴·중첩 수를 그대로 복원.

### 4. StatusEffectDatabase 추가
- `PieceDatabase.FindById`와 동일한 패턴의 `FindByType(StatusEffectType)` 조회용 ScriptableObject.
- 저장 데이터 복원 시 `StatusEffectType` 값만으로 실제 정의를 찾아오는 용도.

### 5. PieceRuntimeState 상태 관리 API 확장
`ApplyStatus`, `HasStatus`, `FindStatus`, `RemoveStatus`, `TickStatusEffects`, `RestoreStatusEffect`를 추가했다.

- `ApplyStatus` 호출 시 기물의 `Definition.ImmuneStatusTags`를 먼저 검사해, 면역 대상이면 조용히 무시하고 `false`를 반환.
- 이미 걸려 있는 같은 종류의 상태는 새로 추가하지 않고 `Reapply()`로 갱신.
- `TickStatusEffects`는 제거 중 인덱스가 어긋나지 않도록 뒤에서부터 순회.

### 6. PieceDefinition 상태 면역 태그 추가
- `_immuneStatusTags` (`StatusEffectType`) 필드와 `ImmuneStatusTags` 프로퍼티 추가.
- 기존 26종 `.asset`은 필드가 없어도 Unity가 기본값 `None`으로 처리하므로 별도 수정 없이 호환.
- 실제 보스 기물에 면역을 부여하는 인스펙터 체크는 보스 에셋이 생기는 이후 일차의 몫으로 남김.

### 7. 저장·복원(RunSaveData / RunState / RunSaveSystem) 연결
- `RunSaveData.PieceSaveData`에 `List<StatusEffectSaveData> statusEffects` 추가, `StatusEffectSaveData`는 상태 종류·남은 지속 턴·중첩 수를 기록.
- `RunState.ToSaveData()`가 보드 위 각 기물의 `StatusEffects`를 순회해 스냅샷에 포함.
- `RunState.FromSaveData(data, database, statusEffectDatabase = null)` : 상태 이상 DB를 **선택적 매개변수**로 추가해 기존 6곳의 호출부(`RunSaveSystem`, `RunSaveTestHarness`, 기존 회귀 테스트 4개)가 전부 수정 없이 그대로 컴파일되도록 유지. DB를 생략하면 상태 이상 없이 복원(구버전 세이브 호환).
- `RunSaveSystem.TryLoad`도 동일하게 `statusEffectDatabase` 선택적 매개변수를 추가.

### 8. Day27 상태 효과 코어 회귀 테스트 추가
`Day27StatusEffectCoreTests`를 추가했다.

주요 검증 항목:

- 상태 부여 후 지속 턴이 0이 되면 자동 제거.
- 중첩형(`StacksAdd`) 상태는 최대 중첩까지 누적, 재적용 시 지속 턴 갱신.
- 갱신형(`RefreshDuration`) 상태는 중첩 없이 지속 턴만 갱신.
- 면역 태그가 있는 기물에는 해당 상태가 부여되지 않음(목록에도 남지 않음).
- 면역 대상이 아닌 다른 상태는 정상적으로 부여됨(면역이 특정 종류에만 한정되는지 확인).
- 상태가 걸린 기물을 `RunState`로 저장 후 `StatusEffectDatabase`와 함께 복원해도 종류·지속 턴·중첩 수가 그대로 유지.
- `StatusEffectDatabase` 없이 복원해도(구버전 호출 호환) 예외 없이 기물만 정상 복원되고 상태는 복원되지 않음.

테스트용 `PieceDefinition`·`StatusEffectDefinition`·각 Database는 실제 `.asset` 파일을 새로 만들지 않고 `ScriptableObject.CreateInstance` + `SerializedObject`로 즉석 구성해, 기존 26종 데이터 에셋을 건드리지 않고도 완전히 격리된 상태로 검증한다.

## 27일차 최종 구조

```text
StatusEffectType (Poison/Burn/Stun/Root)
↓
StatusEffectDefinition (중첩 방식·최대 중첩·기본 지속 턴)
↓
StatusEffectDatabase (종류로 정의 조회)

PieceRuntimeState
├─ ApplyStatus (면역 검사 → 신규 부여 또는 Reapply)
├─ TickStatusEffects (지속 턴 감소·만료 제거)
├─ HasStatus / FindStatus / RemoveStatus
└─ RestoreStatusEffect (저장 데이터 복원 전용)

PieceDefinition
└─ ImmuneStatusTags (보스 등 면역 대상 지정)

RunSaveData / RunState / RunSaveSystem
├─ ToSaveData : 상태 이상 스냅샷 기록
└─ FromSaveData(statusEffectDatabase 선택적) : 상태 이상 복원, 생략 시 구버전 호환
```

## 완료 기준 체크

- [x] `StatusEffectType`/`StatusStackMode` 데이터 타입 추가.
- [x] `StatusEffectDefinition`/`StatusEffectDatabase` ScriptableObject 추가.
- [x] `RuntimeStatusEffect` 부여·재적용·틱·복원 로직 추가.
- [x] `PieceRuntimeState`에 상태 부여/조회/해제/틱 API 연결.
- [x] `PieceDefinition`에 상태 면역 태그 추가(기존 에셋 호환 유지).
- [x] 저장·복원 파이프라인에 상태 이상 스냅샷 연결(기존 호출부 무수정 호환).
- [x] 상태 부여·중첩·만료·면역·저장 복원 회귀 테스트 8종 추가.
- [ ] Unity Editor Test Runner에서 `Day27StatusEffectCoreTests` 전체 Run All 최종 확인.
- [ ] 실제 보스 기물 에셋에 `ImmuneStatusTags` 인스펙터 설정(보스 에셋이 생기는 이후 일차에서 진행).

27일차는 새 상태 이상 자체(독·화상·기절·속박)를 구현하기보다, 그 4종이 공통으로 올라탈 배관 — 데이터 구조, 부여/해제/지속 턴/중첩 API, 면역 검사, 저장·복원 — 을 먼저 완성하는 데 집중했다. `PieceRuntimeState`와 `PieceDefinition`은 기존 API를 전혀 깨지 않고 확장했고, `RunState.FromSaveData`/`RunSaveSystem.TryLoad`는 선택적 매개변수로 확장해 기존 6곳의 호출부가 수정 없이 그대로 동작한다. 이 위에서 28일차는 독의 턴 종료 피해, 화상의 지속 피해, 기절의 행동 스킵, 속박의 이동 차단을 실제 전투 흐름에 연결하는 데만 집중할 수 있다.
