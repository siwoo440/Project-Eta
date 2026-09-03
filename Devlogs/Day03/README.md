# 3일차 개발 일지 — 씬 및 10×10 보드 구축

**날짜**: 2026-09-03
**목표**: 2일차에 정의한 `BoardState`/`TileState` 데이터를 실제로 화면에 그려, Battle 씬에서 10×10 보드와 아군·적군 영역이 눈으로 보이게 한다.

## 오늘 한 일

### 1. `BoardView` 작성 (`Assets/ProjectEta/Scripts/Board/BoardView.cs`)
- `ProjectEta.Board` 네임스페이스의 `MonoBehaviour`. `Awake()`에서 `BoardState`를 새로 생성하고, `BoardState.Width × Height`(10×10) 만큼 `Quad` Primitive를 격자로 배치해 100개 타일을 만든다.
- 타일은 바닥에 눕도록 `Quaternion.Euler(90, 0, 0)`으로 회전시키고, 보드 중앙이 원점에 오도록 좌표를 정렬한다(`_tileSize`, `_tileGap`으로 크기·간격 조절 가능).
- 각 타일의 `TileState.IsPlayerPlacementArea`/`IsEnemyPlacementArea` 값을 읽어 아군(파랑)·적군(빨강) 머티리얼을 구분해 적용(URP `Lit` 셰이더로 머티리얼 2개만 생성해 공유).

### 2. Battle 씬에 연결 (`Assets/ProjectEta/Scenes/Battle.unity`)
- `BoardView` 컴포넌트를 붙인 `BoardView` 게임오브젝트를 씬 루트에 추가.
- 기존 Main Camera가 눈높이·정면(`(0,1,-10)`, 무회전)으로만 세팅돼 있어 보드를 옆에서 납작하게 보게 되는 문제가 있어, 보드 전체가 내려다보이도록 카메라 위치/각도를 `(0, 9, -9)`, `X축 45도 회전`으로 조정.

## 오늘 하지 않은 것

- 타일 클릭/마우스 입력, 카드 배치, 기물 이동 규칙 — 4일차 이후.
- 타일 프리팹화·머티리얼 에셋 분리(현재는 코드에서 런타임 생성) — 필요해지면 이후에 정리.

### 3. 사용자 확인 사항
- Unity 에디터에서 `Battle` 씬을 열어 `BoardView` 오브젝트와 컴파일 에러 없음을 확인.
- ▶ Play로 Game 뷰에서 10×10 격자와 아래쪽(파랑)·위쪽(빨강) 영역 구분이 정상 표시됨을 확인.

## 완료 기준 체크

- [x] Battle 씬에 10×10 보드를 구성하는 `BoardView` 연결
- [x] 아군·적군 10×5 영역이 색상으로 구분되도록 구현
- [x] Unity 에디터에서 Battle 씬을 Play해 보드와 진영 영역이 정상 표시되는지 확인
- [x] Console에 Error 0 상태 확인

3일차 완료 기준을 모두 만족해 3일차를 종료한다.
