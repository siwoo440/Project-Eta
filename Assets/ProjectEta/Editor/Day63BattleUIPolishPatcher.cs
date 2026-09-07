using System; // StringComparison을 사용하기 위한 네임스페이스
using System.IO; // 런타임 소스 파일 읽기·쓰기를 사용하기 위한 네임스페이스
using System.Text; // BOM 없는 UTF-8 저장을 사용하기 위한 네임스페이스
using UnityEditor; // Editor 로드 시 자동 패치와 Asset 재임포트를 사용하기 위한 네임스페이스
using UnityEngine; // 패치 결과 개발 로그를 사용하기 위한 네임스페이스

namespace ProjectEta.EditorTools // 프로젝트 η Editor 전용 도구 네임스페이스
{
    [InitializeOnLoad] // Unity Editor 스크립트 로드 직후 자동 실행을 등록하는 특성
    public static class Day63BattleUIPolishPatcher // 손패 10장 표시와 카드 Hover 상태를 최소 수정하는 일회성 패처
    {
        private const string HandUiPath = "Assets/ProjectEta/Scripts/UI/HandUI.cs"; // 손패 레이아웃 원본 소스 경로
        private const string CardViewPath = "Assets/ProjectEta/Scripts/UI/CardView.cs"; // 카드 Hover 원본 소스 경로
        private const string HandOldLine = "            _layoutGroup.spacing = -24f; // 카드가 살짝 겹쳐 10장도 화면에 들어오게 설정"; // 기존 손패 간격 정확 일치 문자열
        private const string HandNewLine = "            _layoutGroup.spacing = -30f; // 63일차: 최대 10장 손패가 1540px 영역 안에 안정적으로 들어오도록 겹침 간격 확대"; // 63일차 손패 간격 교체 문자열
        private const string HoverOldLine = "            if (_isInteractable && !_isDragging) transform.localScale = Vector3.one * 1.05f; // 사용 가능한 카드를 살짝 확대"; // 기존 카드 Hover 확대 문자열
        private const string HoverNewLine = "            if (_isInteractable && !_isDragging) transform.localScale = Vector3.one * 1.08f; // 63일차: 사용 가능한 카드 Hover 상태를 조금 더 분명하게 확대"; // 63일차 Hover 확대 문자열

        static Day63BattleUIPolishPatcher() // Editor 로드 시 호출되는 정적 초기화 메서드
        {
            EditorApplication.delayCall += ApplyPatch; // 기존 스크립트 컴파일 직후 소스 패치 예약
        }

        private static void ApplyPatch() // HandUI와 CardView의 기능적 배치를 최소 변경하는 메서드
        {
            bool handChanged = ReplaceExactLine(HandUiPath, HandOldLine, HandNewLine); // 손패 최대 10장 겹침 간격 패치 실행
            bool cardChanged = ReplaceExactLine(CardViewPath, HoverOldLine, HoverNewLine); // 카드 Hover 확대 상태 패치 실행

            if (!handChanged && !cardChanged) // 변경할 대상이 하나도 없으면
            {
                return; // 반복 재임포트를 발생시키지 않고 종료
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); // 변경된 런타임 스크립트를 Unity에 즉시 다시 불러옴
            Debug.Log("Day63 전투 UI 정식화: 손패 10장 간격과 카드 Hover 강조를 적용했습니다."); // 자동 패치 적용 결과 개발 로그 출력
        }

        private static bool ReplaceExactLine(string path, string oldLine, string newLine) // 지정 소스의 정확한 한 줄을 안전하게 교체하는 메서드
        {
            if (!File.Exists(path)) // 대상 소스 파일이 없으면
            {
                return false; // 변경 없이 종료
            }

            string source = File.ReadAllText(path); // 현재 소스 전체 문자열 읽기

            if (source.IndexOf(newLine, StringComparison.Ordinal) >= 0) // 이미 63일차 교체 문자열이 존재하면
            {
                return false; // 중복 패치 방지 후 종료
            }

            int index = source.IndexOf(oldLine, StringComparison.Ordinal); // 기존 정확 문자열 위치 탐색

            if (index < 0) // 예상 기존 코드가 존재하지 않으면
            {
                return false; // 다른 버전 소스를 임의 변경하지 않고 안전하게 종료
            }

            string patched = source.Remove(index, oldLine.Length).Insert(index, newLine); // 기존 한 줄을 63일차 한 줄로 정확 교체
            File.WriteAllText(path, patched, new UTF8Encoding(false)); // BOM 없는 UTF-8로 변경 소스 저장
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // 변경 파일 단일 재임포트 실행
            return true; // 실제 소스 변경 성공 반환
        }
    }
}
