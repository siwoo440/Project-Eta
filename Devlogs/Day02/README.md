# 2일차 개발 일지 — 게임 구조 및 데이터 설계

**날짜**: 2026-09-03
**목표**: 실제 로직(이동·전투) 구현 전에 정적 데이터(ScriptableObject)와 런타임 상태 클래스의 책임·필드·참조 관계를 확정.

## 오늘 한 일

### 1. 정적 데이터 (ScriptableObject) 정의
- `PieceDefinition` (`Assets/ProjectEta/Scripts/Pieces/PieceDefinition.cs`): 기물 고정 정보 — 이름, 분류, 등급(1~5성), 이동 타입, 역할 태그, 기본 HP/ATK, 점유 크기.
- `FusionRecipe` (`Assets/ProjectEta/Scripts/Fusion/FusionRecipe.cs`): 재료 2종 → 결과 1종, 숨김 레시피 여부.
- 보조 enum 4개: `PieceGrade`, `PieceCategory`, `PieceMovementType`, `PieceRoleTag` (`Assets/ProjectEta/Scripts/Pieces/`).

### 2. 런타임 상태 클래스 정의 (순수 C#, MonoBehaviour 아님)
- `TileState`, `BoardState` (`Scripts/Board/`) — 10×10 격자, 좌표·점유·배치 영역 관리.
- `PieceRuntimeState` (`Scripts/Pieces/`) — 보드 위 기물 1개의 가변 상태.
- `DeckState`, `HandState` (`Scripts/Cards/`) — 카드 풀 순환과 손패(최대 10장) 관리.
- `RunState` (`Scripts/Run/`) — 런 전체 상태의 최상위 컨테이너.

### 3. 문서화
- [Docs/DataArchitecture.md](../../Docs/DataArchitecture.md): 위 클래스들의 책임·참조 관계 다이어그램.
- [Docs/NamingConventions.md](../../Docs/NamingConventions.md)에 네임스페이스 규칙(`Scripts/` 폴더 ↔ `ProjectEta.*` 네임스페이스 1:1 대응) 추가.

### 4. 에디터 검증 (사용자 확인)
- Unity 에디터에서 새 스크립트 전체가 컴파일 에러 없이 인식됨을 확인.
- `Assets/ProjectEta/Data`에 `PieceDefinition.asset` 테스트 에셋을 생성해 인스펙터에 필드(분류/등급/이동 타입/역할 태그/HP·ATK/점유 크기)가 정상 노출됨을 확인.
- 새 스크립트·에셋의 `.meta` 파일 전부 생성 확인, 이번 커밋에 포함.

## 오늘 하지 않은 것

- 이동 규칙 계산, HP·ATK 전투 처리, 저장/불러오기 구현, 마우스 입력·씬 연결 — 모두 3일차 이후.

## 완료 기준 체크

- [x] 주요 시스템(`BoardState`/`PieceRuntimeState`/`DeckState`/`HandState`/`RunState`) 책임 정의
- [x] `PieceDefinition`/`FusionRecipe` ScriptableObject 데이터 구조 확정
- [x] Unity 에디터에서 `PieceDefinition` 에셋을 실제로 1개 생성해 인스펙터에 필드가 정상 노출되는지 확인
- [x] Console에 Error 0 상태로 컴파일 확인

2일차 완료 기준을 모두 만족해 2일차를 종료한다.
