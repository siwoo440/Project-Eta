# 5일차 개발 일지 — 기물 기본 구조 구현

**날짜**: 2026-09-03
**목표**: `PieceRuntimeState`를 실제 3D 화면(`PieceView`)과 연결하고, 킹·폰을 구분 가능한 형태로 만들어 카드를 냈을 때 선택한 칸에 소환되는 흐름까지 구현한다.

## 오늘 한 일

### 1. `PieceView` 작성 (`Assets/ProjectEta/Scripts/Pieces/PieceView.cs`)
- `TileView`와 같은 방식으로 `PieceRuntimeState`를 들고 있는 `MonoBehaviour`.
- 좌표 변환은 3일차에 만든 `BoardView.BoardToLocalPosition`을 정적 메서드로 뽑아내 타일·기물이 동일한 기준으로 정렬되도록 공유.
- `PieceDefinition.MovementType`이 `King`이면 받침·기둥·머리·십자가(세로/가로) 5개 프리미티브를, `Pawn`이면 받침·몸통·머리 3개 프리미티브를 쌓아 서로 다른 실루엣을 code로 구성(별도 3D 모델링 툴 없이 프리미티브 조합으로 표현할 수 있는 최대치이며, 정교한 스컬프트 모델은 아님).
- 자식 프리미티브들의 `Collider`는 제거하고, 전체 `Renderer.bounds`를 합쳐 계산한 `CapsuleCollider` 하나만 루트에 붙여 클릭 판정용으로 사용.
- `PieceRuntimeState.IsPlayerPiece` 값에 따라 파랑(아군)/빨강(적군) 머티리얼 적용.

### 2. 킹·폰 데이터 에셋 생성 (`Assets/ProjectEta/Data/King.asset`, `Pawn.asset`)
- `PieceDefinition` ScriptableObject 2개를 데이터로 채워 생성.
  - 킹: HP 3([확정] 규칙), ATK 2(임시값), `MovementType.King`.
  - 폰: HP 1, ATK 1(임시값), `MovementType.Pawn`, `RoleTag.Melee`.

### 3. 카드 → 소환 흐름 (`BoardInputController.cs`)
- `HandState`에 킹·폰 카드를 테스트용으로 채워 시작(정식 드로우 파이프라인은 3단계/31~45일차 예정이라 이번엔 손패 직접 구성으로 단순화).
- 숫자키 `1`(킹)/`2`(폰)로 손패 카드를 선택·해제.
- 카드가 선택된 상태에서 타일을 클릭하면: 아군 배치 영역이면서 빈 칸인지 검사 → `PieceRuntimeState` 생성 → `PieceView` 인스턴스화 → `TileState.OccupyingPiece` 갱신 → 손패에서 카드 제거. 조건에 맞지 않으면 소환하지 않고 콘솔에 사유 출력.
- `OnGUI()`로 남은 카드/선택 상태를 화면 좌상단에 표시(정식 UI는 이후 11장 단계에서 별도 진행).

### 4. Battle 씬 연결 (`Assets/ProjectEta/Scenes/Battle.unity`)
- `BoardInputController`에 `_boardView`, `_kingDefinition`(King.asset), `_pawnDefinition`(Pawn.asset) 참조 연결.

### 5. 사용자 확인 사항
- Unity 에디터에서 `Battle` 씬을 Play해 `1`/`2` 키로 카드를 선택하고, 아군 영역(파란 칸) 클릭 시 킹·폰이 서로 다른 형태·색으로 정확한 칸 위치에 소환되는 것을 확인.
- 점유된 칸이나 적 영역 클릭 시 소환이 거부되고 콘솔에 사유가 출력되는 것을 확인.

## 오늘 하지 않은 것

- 기물 이동 규칙, 전투(공격/피해) 처리 — 6일차 이후.
- 정식 카드 UI(손패 카드 시각화, 드래그 앤 드롭), 덱 드로우 파이프라인 — 3단계(31~45일차) 예정.
- 실제 스컬프트 3D 모델·텍스처 — 별도 아트 파이프라인 필요(현재는 프리미티브 조합).

## 완료 기준 체크

- [x] `PieceRuntimeState` 데이터와 3D 화면 위치가 동기화된다.
- [x] 킹·폰이 서로 다른 형태로 구분되어 표시된다.
- [x] 카드를 낸 위치(선택한 아군 빈 칸)에 해당 기물이 정확히 소환된다.
- [x] Unity 에디터에서 Play해 위 동작을 실제로 확인.
- [x] Console에 Error 0 상태 확인.

5일차 완료 기준을 모두 만족해 5일차를 종료한다.
