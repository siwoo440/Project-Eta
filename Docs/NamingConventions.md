# 프로젝트 η — 네이밍·구조 규칙 (1일차 확정)

## 폴더 구조

```
Assets/ProjectEta/
├─ Art
├─ Audio
├─ Data
├─ Materials
├─ Prefabs
├─ Scenes
├─ Scripts
│  ├─ Core
│  ├─ Board
│  ├─ Pieces
│  ├─ Battle
│  ├─ Cards
│  ├─ Fusion
│  ├─ AI
│  ├─ Run
│  ├─ UI
│  └─ Utilities
├─ Settings
├─ Tests
└─ UI
```

기본 템플릿이 만든 `Assets/Settings`, `Assets/Scenes`, `Assets/TutorialInfo`는 URP 템플릿 기본 리소스이므로 1일차에는 그대로 둔다. 앞으로 만드는 프로젝트 고유 에셋은 전부 `Assets/ProjectEta/` 아래에 넣는다.

## 씬 이름 규칙

| 씬 | 역할 |
|---|---|
| `Boot` | 게임 시작, 전역 시스템 초기화 |
| `MainMenu` | 메인 메뉴 |
| `Battle` | 실제 전투 |
| `Test` | 보드·기물·이동 규칙 개발 테스트 |

씬 파일은 `Assets/ProjectEta/Scenes/`에 위 이름 그대로 저장한다. 실제 씬 구성은 2~3일차부터 진행.

## C# 네이밍 규칙

- 클래스/타입: `PascalCase` — 예: `BoardState`, `TileState`, `PieceRuntimeState`, `PieceDefinition`, `DeckState`, `HandState`, `RunState`, `FusionRecipe`
- 지역 변수 / public 프로퍼티: `camelCase` — 예: `currentHp`, `attackPower`, `boardPosition`, `currentTurn`
- private 필드: `_camelCase` (언더스코어 접두사, 예외 없이 유지) — 예: `private int _currentHp;`, `private PieceDefinition _definition;`
- Boolean은 `is/can/has` 접두사로 의미를 바로 드러낸다 — 예: `isDead`, `isSelected`, `isPlayerPiece`, `canMove`, `canAttack`
- 상수: `PascalCase` 또는 `UPPER_SNAKE_CASE` 중 하나로 통일(팀 결정 시 갱신) — 기본은 `PascalCase`(C# 관례)
- 파일명은 그 파일이 담는 public 타입명과 동일하게 유지(1파일 1주요 타입 원칙)

## 데이터 값 관리 원칙

[확정] 규칙만 코드 상수로 두고, 테스트 값·미정 값은 `Assets/ProjectEta/Data`에 `ScriptableObject`로 분리한다. 자세한 확정/테스트/미정 구분은 [CoreRules_Checklist.md](CoreRules_Checklist.md) 참고.

## 네임스페이스 규칙 (2일차 확정)

네임스페이스는 `Scripts/` 아래 폴더 구조와 1:1로 대응한다 — 예: `Scripts/Board` → `ProjectEta.Board`, `Scripts/Pieces` → `ProjectEta.Pieces`. 새 폴더를 만들 때는 같은 규칙으로 네임스페이스도 함께 정한다. 클래스 책임과 참조 관계의 자세한 내용은 [DataArchitecture.md](DataArchitecture.md) 참고.
