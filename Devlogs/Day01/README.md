# 1일차 개발 일지 — 프로젝트 기반 구축

**날짜**: 2026-09-03
**목표**: 게임 기능 구현이 아니라, 이후 개발이 꼬이지 않도록 Unity 프로젝트의 뼈대를 확정.

## 오늘 한 일

### 1. 현황 점검
- GitHub 저장소 [siwoo440/Project-Eta](https://github.com/siwoo440/Project-Eta)는 생성만 되어 있고 커밋 0개(완전히 빈 상태)였음을 확인.
- 로컬 `F:\Project-Eta`에는 이미 Unity 6000.3.21f1 기반 **Universal 3D(URP) 템플릿**으로 프로젝트가 만들어져 있었음.
- `Packages/manifest.json`, `ProjectSettings/QualitySettings.asset`을 확인해 URP 패키지(17.3.0)와 Mobile/PC 두 Quality 레벨에 각각 URP Pipeline Asset이 정상 연결되어 있는 것을 확인 → **URP 설정은 별도 작업 없이 완료 상태**.
- 기획서(구글 문서) "기획" 탭과 "개발 방향" 탭 전체를 확인해 확정/테스트/미정 규칙과 180일 로드맵을 파악.

### 2. 프로젝트 폴더 구조 생성
`Assets/ProjectEta/` 아래에 앞으로 유지할 최상위 구조를 생성.

```
Assets/ProjectEta/
├─ Art / Audio / Data / Materials / Prefabs / Scenes / Settings / Tests / UI
└─ Scripts/
   ├─ Core / Board / Pieces / Battle / Cards / Fusion / AI / Run / UI / Utilities
```

템플릿 기본 리소스(`Assets/Settings`, `Assets/Scenes`, `Assets/TutorialInfo`)는 URP 템플릿이 만든 것이라 그대로 유지.

### 3. Git 연결
- `git init` → 기본 브랜치를 GitHub 기본 브랜치(`main`)에 맞춤.
- Unity 표준 `.gitignore` 작성 (`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.slnx` 등 제외 / `Assets/`, `Packages/`, `ProjectSettings/`는 포함).
- `origin`을 `https://github.com/siwoo440/Project-Eta.git`으로 연결.
- 사용자 승인 후 첫 커밋을 `main`에 push, GitHub에서 커밋 확인 완료.

### 4. 개발 규칙 문서화
- [Docs/CoreRules_Checklist.md](../../Docs/CoreRules_Checklist.md): 기획서에서 뽑은 **[확정] / [테스트 값] / [미정]** 핵심 규칙 체크리스트.
- [Docs/NamingConventions.md](../../Docs/NamingConventions.md): 폴더 구조, 씬 이름 규칙(`Boot`/`MainMenu`/`Battle`/`Test`), C# 네이밍 규칙(PascalCase/camelCase/`_camelCase`/`is·can·has` 접두사).

### 5. 사용자 확인 사항
- Unity 에디터로 프로젝트를 열어 새 `ProjectEta` 폴더들의 `.meta` 파일이 자동 생성된 것을 확인 → 프로젝트가 정상적으로 열리고 인식됨.

### 6. 테스트 씬 4개 생성
`Assets/ProjectEta/Scenes/`에 이름 규칙에 맞춰 `Boot`, `MainMenu`, `Battle`, `Test` 씬 파일을 생성.
- 각 씬은 URP 템플릿 기본 씬(`SampleScene`)을 그대로 복제한 상태로, Main Camera·Directional Light·Global Volume만 있는 빈 씬.
- `ProjectSettings/EditorBuildSettings.asset`에도 4개 씬을 모두 등록해 Build Settings에서 바로 보이고 빌드 대상에 포함되도록 함(기존 `SampleScene` 뒤에 추가, 순서 변경 없음).
- 실제 씬 구성(보드, 카메라 세팅, UI 등)은 2~3일차부터 진행 예정이며, 오늘은 이름과 껍데기만 확정.

## 1일차 완료 기준 체크

- [x] `ProjectEta` Unity 프로젝트가 정상 실행된다.
- [x] URP가 정상 설정되어 있다.
- [ ] Windows PC 빌드가 가능하다. *(에디터에서 직접 빌드 테스트 필요)*
- [x] 기본 Assets 폴더 구조가 만들어져 있다.
- [x] 파일·코드 네이밍 규칙을 정했다.
- [x] Git 저장소가 연결되어 있다.
- [x] GitHub Push가 정상적으로 된다.
- [x] Unity용 `.gitignore`가 적용되어 있다.
- [x] 핵심 게임 규칙 체크리스트가 존재한다.
- [ ] 테스트 Build가 실행된다. *(에디터에서 직접 확인 필요)*
- [ ] Console에 해결되지 않은 Error가 없다. *(에디터에서 직접 확인 필요)*
- [x] 1일차 커밋을 완료했다.

## 남은 일 (2일차 이전에 사용자가 직접)

1. Windows/x86_64로 테스트 빌드 1회 실행 → 빌드된 exe 실행 확인.
2. Console에 Error 0 상태 확인.
3. Unity 에디터에서 새로 추가된 `Boot`/`MainMenu`/`Battle`/`Test` 씬을 한 번씩 열어 정상적으로 로드되는지 확인(파일을 직접 복제해 만들었으므로 에디터에서 한 번 열어 검증 권장).
