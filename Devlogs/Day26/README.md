# 26일차 개발 일지 — 26종 기물 통합 안정화·시작 덱·시각화

**날짜**: 2026-09-04  
**기준 커밋**: `925e8987dffa3adf6c0f117cceea0529550c4b10`  
**목표**: 25일차까지 완성한 26종 기물을 전투·저장·합성·덱·UI 흐름에 통합하고, 플레이어 시작 덱과 기물 시각화를 확장해 5단계 개발을 마감한다.

## 오늘 한 일

### 1. PieceDatabase 전체 로스터 조회 지원
- 기존 `FindById()` 조회 구조 유지.
- 26종 전체를 통합 테스트에서 검사할 수 있도록 읽기 전용 `Definitions` 목록 노출.
- 데이터베이스 등록 수, null, 중복 id, 기본 스탯, 점유 크기, 이동 규칙 존재 여부를 전체 로스터 기준으로 확인할 수 있게 구성.

### 2. FusionRecipeDatabase 전체 레시피 조회 지원
- 기존 `TryFindRecipe()` 유지.
- 통합 테스트가 등록된 모든 합성식을 순회할 수 있도록 읽기 전용 `Recipes` 목록 추가.
- 합성 재료 A/B와 결과 기물이 실제 `PieceDatabase`의 등록 기물을 참조하는지 검증할 수 있게 구성.

### 3. 26종 기물 통합 회귀 테스트 추가
`Day26PieceRosterIntegrationTests`를 추가했다.

주요 검증 항목:

- PieceDatabase 등록 수 26종.
- 예상 PieceId 26개 전체 조회.
- PieceId 중복 없음.
- null PieceDefinition 없음.
- 표시 이름 존재.
- HP 1 이상.
- ATK 0 이상.
- 점유 크기 유효.
- 설명 존재.
- Legacy 또는 데이터 기반 이동 규칙 존재.
- Jumper / Slider / Rider / Ranged 등 핵심 역할 태그 확인.
- Step / Slide / Leap / Compound / Rider / Conditional / Hopper / Cannon 대표 이동 규칙 회귀 확인.

### 4. 시스템 통합 테스트 추가
`Day26SystemIntegrationTests`를 추가했다.

주요 검증 항목:

- 26종 전부 보드 배치 후 저장·복원.
- PieceId 유지.
- 현재 체력 유지.
- 플레이어/적 진영 유지.
- Chameleon 이동 순환 단계 유지.
- DeadCardPile 저장·복원.
- 죽은 카드가 영구 보유 수에서 중복되지 않는지 확인.
- Cannon은 원거리 처치 후 원위치 유지.
- 일반 근접 기물은 처치 후 대상 칸 점유.
- FusionRecipe가 실제 등록 PieceDefinition을 참조하는지 확인.

### 5. 플레이어 시작 덱을 26종 한 장씩으로 확장
플레이어가 테스트 전투에서 전체 기물을 바로 사용할 수 있도록 26종 시작 덱 구성을 추가했다.

구성:

```text
King
Pawn
Knight
Bishop
Rook
Queen
Archbishop
Chancellor
Amazon
Wazir
Ferz
Mann
Dabbaba
Alfil
Camel
Zebra
Centaur
Waffle
Nightrider
Camelrider
Grasshopper
Cannon
Canvasser
Caliph
Squirrel
Chameleon
```

- `PlayerStartingDeckCatalog` 추가.
- `Resources/PlayerStartingDeck26.asset` 추가.
- 26종을 각각 정확히 한 장씩 참조.
- Battle 씬에서 기존 기본 6종 프로토타입 덱을 감지하면 나머지 20종 자동 추가.
- OwnedCardPool 기준 총 26장.
- 기존 시작 손패 5장 유지.
- DrawPile은 손패 5장을 제외한 21장 구성.
- 이미 26종이 구성된 상태에서는 다시 추가하지 않음.
- 기본 6종 구조가 아닌 커스텀 덱/세이브 데이터는 강제로 덮어쓰지 않음.

### 6. 플레이어 덱 회귀 테스트 추가
`Day26PlayerDeckRosterTests`를 추가했다.

- 시작 덱 카탈로그 26장 확인.
- PieceId 중복 없음.
- 기존 6종 덱이 26종으로 확장되는지 확인.
- Owned 26 / Hand 5 / Draw 21 확인.
- 확장 함수를 두 번 실행해도 중복되지 않는지 확인.
- 커스텀 덱을 자동으로 덮어쓰지 않는지 확인.

### 7. 20종 페어리 기물 전용 3D 실루엣 추가
기존에는 기본 6종 외 기물이 Pawn 임시 모델로 표시되던 구조를 확장했다.

전용 모델을 추가한 20종:

```text
Archbishop
Chancellor
Amazon
Wazir
Ferz
Mann
Dabbaba
Alfil
Camel
Zebra
Centaur
Waffle
Nightrider
Camelrider
Grasshopper
Cannon
Canvasser
Caliph
Squirrel
Chameleon
```

- `PieceView` 모델 선택 기준을 단순 MovementType 중심에서 PieceId 중심으로 확장.
- Unity Primitive를 조합한 임시 프로토타입 모델 사용.
- 각 기물의 이동 특성이나 이름에서 연상되는 실루엣으로 구분.
- 기본 6종 모델도 기존 형태 유지.

### 8. 카드 초상화 임시 약칭 개선
카드 Artwork가 없는 기물에 표시되던 `?`를 PieceId 앞 3글자로 변경했다.

예시:

```text
pawn      → paw
king      → kin
cannon    → can
chameleon → cha
amazon    → ama
```

- PieceId 우선 사용.
- PieceId가 없으면 에셋 이름 사용.
- 소문자 3글자로 통일.

### 9. 카드 합성 선택 기능 회귀 복구
시각화 작업 중 구버전 `CardView` 기반 덮어쓰기로 합성 선택 API가 빠지는 문제가 발생했다.

복구 항목:

- `SetFusionSelected(bool)`
- 합성 재료 선택 금색 테두리.
- `IPointerClickHandler`.
- 합성 모드 좌클릭 재료 선택.
- 우클릭 손패 정리.
- 합성 모드에서 드래그 소환 차단.
- 3글자 카드 약칭 유지.

### 10. CanvasGroup TestRunner 오류 수정
`CardView` 드래그 테스트에서 다음 문제가 확인됐다.

```text
CardView_Bind_AlwaysEnsuresCanvasGroup
CardView_BeginDrag_HidesCardVisual_AndInvalidReleaseRestoresIt
```

원인은 Unity 컴포넌트 확보에 C#의 `??` 연산자를 사용한 것이었다.

수정 전 개념:

```text
GetComponent<CanvasGroup>() ?? AddComponent<CanvasGroup>()
```

수정 후:

```text
Unity의 == null 비교
↓
기존 CanvasGroup 재확보
↓
그래도 없으면 AddComponent
```

같은 위험이 있는 `RectTransform`, `LayoutElement`, `Image` 확보도 명시적 Unity null 검사 방식으로 정리했다.

## 26일차 최종 구조

```text
26종 PieceDefinition
↓
PieceDatabase
├─ 전체 로스터 통합 테스트
├─ 이동 규칙 통합 테스트
└─ 역할 태그 검증

26종 시작 덱
↓
PlayerStartingDeckCatalog
↓
PrototypePlayerDeck26Bootstrap
↓
Owned 26 / Hand 5 / Draw 21

전투·저장·합성
├─ RunState 저장·복원
├─ Chameleon 순환 상태
├─ DeadCard 생명주기
├─ Cannon 원거리 처치 정책
└─ FusionRecipe 참조 무결성

시각화
├─ PieceView 26종 실루엣
└─ CardView PieceId 앞 3글자
```

## 완료 기준 체크

- [x] PieceDatabase 26종 전체 조회 API 추가.
- [x] FusionRecipeDatabase 전체 레시피 조회 API 추가.
- [x] 26종 로스터 무결성 테스트 추가.
- [x] 26종 저장·복원 통합 테스트 추가.
- [x] DeadCard 소유권 회귀 테스트 추가.
- [x] Cannon 근접/원거리 처치 정책 회귀 테스트 추가.
- [x] 합성 레시피 PieceDatabase 연결 검사 추가.
- [x] 플레이어 시작 덱 26종 한 장씩 구성.
- [x] 시작 덱 중복 추가 방지.
- [x] 커스텀 덱 보호.
- [x] 기본 6종 외 20종 전용 프로토타입 모델 추가.
- [x] 카드 `?` 표시를 PieceId 앞 3글자로 변경.
- [x] CardView 합성 선택 기능 복구.
- [x] CanvasGroup 확보 로직을 Unity null 검사 방식으로 수정.
- [ ] 최신 수정본 적용 후 Unity Test Runner 전체 EditMode Run All 최종 재확인.
- [ ] Battle 씬에서 26종 카드 드로우·소환·모델 표시 전체 수동 확인.

26일차에서는 새로운 기물을 더 추가하기보다 23~25일차에 확장한 26종 기물을 하나의 실제 플레이 흐름으로 묶는 데 집중했다. 데이터베이스·이동·전투·저장·합성 회귀 테스트를 추가하고, 플레이어 시작 덱에서 26종을 한 장씩 사용할 수 있도록 확장했다. 또한 기본 6종 외 20종에 프로토타입 3D 실루엣을 추가하고 카드 약칭을 PieceId 앞 3글자로 통일했다. 마지막으로 카드 UI 합성 선택 기능과 CanvasGroup 드래그 회귀 문제를 수정해 5단계 기물 확장 작업을 통합 안정화 단계까지 마무리했다.
