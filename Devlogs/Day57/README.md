# 57일차 : 설정 카테고리 분리 및 Master·BGM·SFX 오디오 설정 구현

## 개발 목표

56일차에서 구현한 재사용 설정 패널을 확장해 설정 영역을 카테고리 버튼으로 분리하고, 디스플레이와 사운드 설정을 독립적으로 보여주는 구조를 만든다.

57일차의 핵심 목표는 다음과 같다.

- 설정 패널 카테고리 내비게이션 추가
- 디스플레이 / 사운드 설정 분리
- 향후 조작 / 게임플레이 / 접근성 설정 확장 자리 확보
- Master / BGM / SFX 볼륨 설정
- 오디오 설정 실시간 미리보기
- 기존 settings.json에 오디오 설정 통합
- 56일차 이전 설정 파일 마이그레이션
- 현재 카테고리만 초기화
- MainMenu와 인게임 설정 패널에서 동일 구조 재사용

## 설정 카테고리 구조

`SettingsCategory`를 추가해 설정 영역을 다음 카테고리로 분리했다.

- Display
- Sound
- Controls
- Gameplay
- Accessibility

57일차에서는 Display와 Sound만 실제로 활성화한다.

Controls, Gameplay, Accessibility는 향후 기능을 추가할 수 있도록 카테고리 값과 UI 버튼만 준비하고 현재는 `준비 중` 상태로 비활성화한다.

`SettingsCategoryCatalog`는 각 카테고리의 표시 이름과 현재 구현 여부를 제공한다.

## 설정 패널 레이아웃 변경

`SettingsPanelController`를 좌측 카테고리 내비게이션과 우측 설정 콘텐츠 영역으로 재구성했다.

좌측 영역:

- 디스플레이
- 사운드
- 조작 · 준비 중
- 게임플레이 · 준비 중
- 접근성 · 준비 중

우측 영역은 선택한 구현 카테고리의 설정만 표시한다.

카테고리를 전환해도 현재 편집 중인 설정값은 유지된다.

마지막으로 열어본 구현 카테고리는 다음 설정 패널 진입 시 다시 표시된다.

## 디스플레이 카테고리

기존 56일차 화면 설정 기능을 Display 영역으로 이동했다.

현재 항목은 다음과 같다.

- 해상도
- 전체화면 창 / 창모드
- UI Scale

해상도와 화면 모드는 적용 시 실제 화면에 반영된다.

UI Scale은 Slider 변경 시 즉시 미리보기된다.

## 사운드 카테고리

Sound 영역에 다음 볼륨 Slider를 추가했다.

- Master
- BGM
- SFX

각 값은 0~100% 범위로 표시하고 내부 데이터는 0.0~1.0으로 관리한다.

Slider를 움직이면 저장 전에도 현재 오디오 설정을 즉시 미리보기한다.

## 오디오 설정 데이터

`GameSettingsData`에 다음 필드를 추가했다.

- SettingsVersion
- MasterVolume
- BgmVolume
- SfxVolume

기본값은 모두 1.0이다.

현재 설정 데이터 버전은 `2`다.

`Normalized()`는 최신 설정에서는 각 볼륨을 0~1 범위로 제한한다.

## 기존 settings.json 마이그레이션

56일차 이전의 `settings.json`에는 오디오 필드와 SettingsVersion이 없다.

기존 파일을 읽었을 때 SettingsVersion이 현재 버전보다 낮으면 새 오디오 필드가 0으로 해석돼 게임이 음소거되는 상황을 막기 위해 다음 기본값으로 보정한다.

- Master 100%
- BGM 100%
- SFX 100%

보정 후 설정 버전은 최신 버전으로 변환된다.

## 설정 편집 상태 확장

`GameSettingsEditState`에 다음 오디오 편집 기능을 추가했다.

- SetMasterVolume
- SetBgmVolume
- SetSfxVolume

오디오 변경도 기존 Display 설정과 동일하게 `IsDirty` 판정에 포함된다.

따라서 Display에서 값을 바꾸고 Sound로 이동하거나 반대로 이동해도 모든 미적용 변경 상태가 유지된다.

## 카테고리별 초기화

기존 전체 초기화 외에 `ResetCategory()`를 추가했다.

Display에서 초기화를 누르면 다음 값만 기본값으로 돌아간다.

- 해상도
- 화면 모드
- UI Scale

Sound에서 초기화를 누르면 다음 값만 기본값으로 돌아간다.

- Master
- BGM
- SFX

다른 카테고리의 편집값은 유지한다.

설정 패널 하단 버튼도 현재 선택 카테고리에 따라 `디스플레이 초기화` 또는 `사운드 초기화`로 표시된다.

## 적용과 취소

`적용`은 현재 카테고리만 저장하는 방식이 아니라 패널에서 편집한 모든 설정을 한 번에 적용하고 저장한다.

`취소`는 모든 미적용 변경을 마지막 저장 상태로 복원한다.

UI Scale과 오디오 모두 실시간 미리보기 값이 기존 적용 상태로 돌아간다.

## GameAudioService

`GameAudioService`를 추가해 설정 시스템과 실제 Unity 오디오 출력 사이의 공통 계층을 만들었다.

현재 관리 값은 다음과 같다.

- MasterVolume
- BgmVolume
- SfxVolume

Master는 `AudioListener.volume`에 적용한다.

BGM과 SFX는 각각 별도 채널 비율로 관리하며 변경 시 `VolumesChanged` 이벤트를 발생시킨다.

최종 오디오 출력 비율의 기본 관계는 다음과 같다.

`Master × Channel Volume`

## AudioChannelVolumeBinding

향후 실제 BGM과 효과음 AudioSource를 설정 시스템에 쉽게 연결할 수 있도록 `AudioChannelVolumeBinding`을 추가했다.

AudioSource가 있는 GameObject에 이 컴포넌트를 추가하고 채널을 Bgm 또는 Sfx로 지정하면 해당 채널 볼륨 변경을 반영한다.

원래 AudioSource의 볼륨을 기준값으로 보존한 뒤 채널 설정을 곱해 적용한다.

Master는 AudioListener 단계에서 전체 오디오에 별도로 적용된다.

## 설정 저장 및 런타임 적용

`GameSettingsService`는 기존 `settings.json` 하나에 Display와 Sound 설정을 함께 저장한다.

다음 기능을 추가했다.

- PreviewAudio
- ReapplyAudio
- ReapplyRuntimeSettings

설정 적용 시 화면·UI·오디오를 함께 갱신한다.

Scene 전환 후에도 런타임 부트스트랩이 새 Canvas와 오디오 환경에 저장 설정을 다시 적용한다.

## GameSettingsRuntimeBootstrap 확장

기존 Scene 로드 후 UI Scale 재적용 흐름을 화면 설정 전체 재적용 흐름으로 확장했다.

새 Scene의 런타임 UI와 오디오 객체가 생성된 뒤 저장된 설정을 다시 반영한다.

## 테스트 소스

`Day57AudioSettingsTests`를 추가했다.

현재 포함된 검증 항목은 다음과 같다.

- 56일차 이전 설정 파일에 기본 오디오 볼륨 마이그레이션
- 최신 오디오 볼륨의 0~1 범위 보정
- Master / BGM / SFX 편집값의 Dirty 상태와 취소
- Sound 카테고리 초기화 시 Display 설정 유지
- Display / Sound 활성 카테고리 판정
- Controls / Gameplay / Accessibility 향후 카테고리 판정
- Master와 채널 볼륨의 최종 출력 비율 계산

## 주요 변경 파일

### 생성

- `Assets/ProjectEta/Scripts/Settings/SettingsCategory.cs`
- `Assets/ProjectEta/Scripts/Settings/GameAudioService.cs`
- `Assets/ProjectEta/Scripts/Settings/AudioChannelVolumeBinding.cs`
- `Assets/ProjectEta/Tests/EditMode/Day57AudioSettingsTests.cs`

### 수정

- `Assets/ProjectEta/Scripts/Settings/GameSettingsData.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsEditState.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsService.cs`
- `Assets/ProjectEta/Scripts/Settings/GameSettingsRuntimeBootstrap.cs`
- `Assets/ProjectEta/Scripts/Settings/SettingsPanelController.cs`

### 삭제

없음.

## 결과

57일차 작업으로 설정 화면이 하나의 긴 설정 목록이 아니라 확장 가능한 카테고리 기반 구조로 변경됐다.

현재 플레이어는 Display와 Sound를 버튼으로 전환할 수 있으며, 향후 Controls / Gameplay / Accessibility 설정을 같은 구조에 추가할 수 있다.

사운드 설정은 Master / BGM / SFX로 분리됐고 기존 settings.json 저장 구조에 통합됐다.

구버전 설정 파일은 새 오디오 필드가 없어도 기본 100% 볼륨으로 마이그레이션된다.

MainMenu와 Battle의 재사용 설정 패널 구조는 그대로 유지된다.

## 검증 상태

최신 57일차 GitHub 커밋에 설정 카테고리, 오디오 서비스, AudioSource 채널 바인딩, 설정 데이터 확장, 설정 패널 변경, Day57AudioSettingsTests가 포함된 것을 확인했다.

현재 최신 커밋에는 연결된 GitHub commit status 검사가 등록되어 있지 않다.

따라서 GitHub에 올라온 변경 내역과 소스 구조는 확인했지만 Unity Editor 컴파일 및 EditMode TestRunner 전체 통과 여부는 GitHub 상태만으로 확인할 수 없다.
