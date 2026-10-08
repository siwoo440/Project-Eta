---
# 98일차 UI 4단계 적용 기록

투명 PNG 14개를 개별 생성하고 메인 메뉴·King 선택·튜토리얼·설정·영구 성장·런 결과 화면에 연결했다. 압축 1~4단계의 프로젝트용 UI 이미지는 총 45개다. 생성 도구는 Codex 내장 `image_gen.imagegen`이다.

---
## 적용 이미지

저장 경로: `Assets/ProjectEta/Resources/UI/Day98/`

| 파일 | 적용 위치 |
|---|---|
| `MainMenuBackdrop.png` | 메인 메뉴 배경 |
| `MainMenuEmblem.png` | 메인 메뉴 하단 체스 문양 |
| `KingCardFrame.png` | King 선택 카드·전략형 King 선택 항목 |
| `KingEmblemFrame.png` | King 초상화 영역 |
| `TutorialPageFrame.png` | 튜토리얼 페이지 |
| `UiModalFrame.png` | 메뉴·영구 성장 확인 창 |
| `UiCategoryTab.png` | 설정·영구 성장 카테고리 |
| `UiSliderTrack.png` | 음량·UI 배율 슬라이더 |
| `UiSliderHandle.png` | 슬라이더 손잡이 |
| `MetaUnlockTile.png` | 영구 성장 해금 목록 |
| `RunResultFrame.png` | 런 결과 패널 |
| `UiCloseIcon.png` | 런 결과 닫기 장식 |
| `UiArrowLeft.png` | King·설정 이전 버튼·장식 |
| `UiArrowRight.png` | King·설정 다음 버튼·장식 |

---
## 동작과 배치

- 기존 버튼 클릭, 비활성화, 슬라이더 값 변경, 해금 조건과 문구 유지
- 글자와 수치는 Unity Text 사용, 장식 이미지의 클릭 간섭 차단
- 확인 창은 불투명 바탕 위에 투명 프레임 배치
- 공통 패널·일반 버튼·위험 버튼을 일시정지와 조작법 화면에도 적용
- 설정 값·화살표를 행 안쪽으로 이동, 상태 문구와 하단 버튼의 겹침 수정
- 조작법 제목을 패널 안쪽으로 이동
- 영구 성장 목록에 세로 스크롤과 마스크 추가, 첫 진입 시 첫 항목 표시
- 성장 항목 선택 후 스크롤 위치 유지, 카테고리 변경 시 목록 맨 위 표시
- King 초상화 영역의 기존 자리표시자 유지, 실제 캐릭터 초상화 제작은 별도 작업

수정 범위는 UI 컨트롤러 9개와 공통 이미지 로더 `Day98UiSkin.cs`다. PNG와 Unity 메타 파일은 각각 별도 파일로 유지한다.

---
## 검증 결과

| 검증 | 결과 |
|---|---|
| 전체 EditMode | 859개 통과, 실패 0개 |
| 전체 PlayMode | 3개 통과, 실패 0개 |
| Windows 개발 빌드 | 성공, `ProjectEta.exe` 생성 확인 |
| 샘플 UI 렌더 | 13개 화면 × 2개 해상도, 총 26장 |
| 검사 해상도 | 1920×1080·2560×1440 |

새 EditMode 검사 11개는 자산 로딩·버튼 입력·슬라이더·화살표·배치·목록 접근을 검증한다. 새 PlayMode 검사 1개는 성장 목록의 휠 스크롤·마지막 항목 선택·상세 표시·위치 유지·카테고리 전환을 검증한다.

샘플 화면은 실제 UI 생성 코드를 샘플 데이터로 실행하여 렌더했다. 실제 게임의 전체 런과 모든 입력 흐름을 사람이 시각 검수한 결과는 아니다. 원본 프로젝트의 Unity Play 모드에서 메뉴 → King 선택 → 튜토리얼 → 설정 → 성장 → 런 결과 연결과 UI 배율 변경을 확인해야 한다.

4단계 구현 검증은 별도 복사본에서 수행했고, 반영한 71개 파일의 해시가 일치했다. 2026-10-09 일차 마무리에서는 실제 `F:\Project-Eta`에서 전체 테스트와 개발 빌드를 다시 실행했다. 전체 45종의 연결을 확인하고 기존 20종의 Import 설정을 보정했다.

---
## 기록 위치

- 제작 프롬프트: `Docs/Day98Stage4ImagePrompts.md`
- 적용 계획: `Docs/Day98UiImagePlan.md`
- 샘플 화면: `Docs/Visuals/Day98/Stage4/`
- 검증 XML·로그·개발 빌드: 작업 공간의 `Day98ValidationOutput/`

2026-10-09 일차 마무리 요청에 따라 98일차 개발일지와 전체 UI 변경을 커밋·푸시한다. 기존 99일차 콘텐츠 개발 계획은 유지한다.
