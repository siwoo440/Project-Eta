# 프로젝트 η — 데이터 구조 설계 (2일차 확정)

> 목적: 4~5일차부터 실제 로직(이동·전투)을 구현할 때 클래스 책임을 다시 고민하지 않도록,
> 정적 데이터(ScriptableObject)와 런타임 상태 클래스의 필드·관계만 먼저 확정한다.
> 오늘은 로직(이동 규칙, 전투 계산, 저장/불러오기)을 구현하지 않는다 — 필드와 참조 관계만.

## 정적 데이터 (ScriptableObject) — `Assets/ProjectEta/Data`에 에셋으로 저장

| 타입 | 파일 | 책임 |
|---|---|---|
| `PieceDefinition` | `Scripts/Pieces/PieceDefinition.cs` | 기물 종류별 고정 정보: 이름, 분류(`PieceCategory`), 등급(`PieceGrade` 1~5성), 이동 타입(`PieceMovementType`), 역할 태그(`PieceRoleTag`), 기본 HP/ATK, 점유 크기 |
| `FusionRecipe` | `Scripts/Fusion/FusionRecipe.cs` | 합성 레시피: 재료 카드 2종(`PieceDefinition` 참조) → 결과 `PieceDefinition`, 숨김 레시피 여부 |

보조 enum: `PieceGrade`, `PieceCategory`, `PieceMovementType`, `PieceRoleTag`(`Scripts/Pieces/`) — 모두 [기획서] 6장 기물 분류를 그대로 반영.

## 런타임 상태 클래스 (순수 C#, MonoBehaviour 아님)

| 타입 | 파일 | 책임 |
|---|---|---|
| `TileState` | `Scripts/Board/TileState.cs` | 칸 1개의 좌표, 점유 기물, 배치 가능 영역 여부, 장애물 여부 |
| `BoardState` | `Scripts/Board/BoardState.cs` | 10×10 `TileState` 격자 생성·보관, 좌표 유효성 검사, 타일 조회 |
| `PieceRuntimeState` | `Scripts/Pieces/PieceRuntimeState.cs` | 보드 위 기물 1개의 가변 상태: `PieceDefinition` 참조 + 현재 HP, 좌표, 아군 여부, 선택/행동 가능 여부 |
| `DeckState` | `Scripts/Cards/DeckState.cs` | 보유 카드 풀 / 드로우 덱 / 죽은 카드 덱 순환 관리 |
| `HandState` | `Scripts/Cards/HandState.cs` | 손패(최대 10장, [확정] 규칙) 보관과 추가/제거 |
| `RunState` | `Scripts/Run/RunState.cs` | 런 전체 상태: 킹 HP, 현재 라운드, 메타 재화, `BoardState`/`DeckState`/`HandState` 소유 — 저장/불러오기의 최상위 단위 |

## 참조 관계

```
RunState
 ├─ BoardState  → TileState[10,10] → PieceRuntimeState (있으면)
 ├─ DeckState   → PieceDefinition 참조 목록 (보유/드로우/죽은 카드)
 └─ HandState   → PieceDefinition 참조 목록 (손패, 최대 10장)

PieceRuntimeState → PieceDefinition (고정값 읽기 전용 참조)
FusionRecipe      → PieceDefinition × 2 (재료) → PieceDefinition (결과)
```

## 네임스페이스

폴더 구조와 1:1로 대응한다 — `ProjectEta.Board`, `ProjectEta.Pieces`, `ProjectEta.Fusion`, `ProjectEta.Cards`, `ProjectEta.Run`. 새 폴더를 추가할 때 같은 규칙으로 네임스페이스를 만든다.

## 오늘 하지 않은 것 (의도적으로 비워둠)

- 실제 이동 규칙 계산(4~5일차), HP·ATK 전투 처리(2단계, 16일차~)
- 저장/불러오기 직렬화 구현(6일차)
- MonoBehaviour/씬 연결, 마우스 입력(3~5일차)
- 합성 로직 실행, 드로우 로직 실행 — 지금은 데이터를 담는 그릇만 존재

## 완료 기준 체크

- [x] `PieceDefinition`, `FusionRecipe` ScriptableObject 정의
- [x] `BoardState`, `TileState`, `PieceRuntimeState`, `DeckState`, `HandState`, `RunState` 책임과 필드 정의
- [x] 클래스 간 참조 관계 문서화
- [ ] Unity 에디터에서 `PieceDefinition` 에셋 1개 실제 생성해 필드가 인스펙터에 정상 노출되는지 확인 *(에디터 작업 필요)*
