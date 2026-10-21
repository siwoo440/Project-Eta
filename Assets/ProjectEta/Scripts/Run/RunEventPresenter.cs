using System; // 저장과 완료 콜백
using System.Collections.Generic; // 카드와 선택 목록
using System.Text; // 실제 결과 표시
using UnityEngine; // 개발 결과 로그
using ProjectEta.Pieces; // 실제 카드 정의
using ProjectEta.Fusion; // 합성 재료 후보
using ProjectEta.Meta; // 영구 정보 열람
using ProjectEta.UI; // 기존 보드 오버레이

namespace ProjectEta.Run // 이벤트 화면 연결
{ // 영역 시작
    public sealed class RunEventPresenter // 선택·대상·결과 화면 흐름
    { // 타입 시작
        private const int TargetsPerPage = 2; // 탐색 버튼과 분리한 한 줄 대상 수
        private readonly RunState _run; // 현재 런
        private readonly RunEconomyState _economy; // 현재 재화
        private readonly IReadOnlyList<PieceDefinition> _catalog; // 실제 카드 원본
        private readonly IReadOnlyList<FusionRecipe> _recipes; // 현재 합성 경로
        private readonly RunContentUnlockSnapshot _unlocks; // 런 시작 해금
        private readonly MetaProgressState _meta; // 영구 열람 기록
        private readonly StageBoardOverlayUI _overlay; // 기존 판 위 UI
        private readonly Func<bool> _save; // 안전 저장 시도
        private readonly Action _complete; // 지도 복귀
        private RunEventSession _session; // 노드의 고정 선택
        private bool _closed; // 오래된 콜백 차단
        public IReadOnlyList<StageOverlayOption> CurrentOptions // 현재 화면 입력 목록
        { // 속성 시작
            get; // 표시 값 조회
            private set; // 화면 내부 갱신
        } // 속성 종료
        public string CurrentTitle // 현재 페이지 제목
        { // 속성 시작
            get; // 표시 값 조회
            private set; // 화면 내부 갱신
        } // 속성 종료
        public string CurrentSubtitle // 현재 페이지 상태
        { // 속성 시작
            get; // 표시 값 조회
            private set; // 화면 내부 갱신
        } // 속성 종료
        private bool IsCurrent => !_closed && _session != null && _run.CurrentFlowPhase == RunFlowPhase.Event && _session.key == RunEventService.CurrentKey(_run) && ReferenceEquals(_session, _run.BalanceData.activeEvent102); // 현재 노드 화면 소유 확인
        public RunEventPresenter(RunState run, RunEconomyState economy, IReadOnlyList<PieceDefinition> catalog, IReadOnlyList<FusionRecipe> recipes, RunContentUnlockSnapshot unlocks, MetaProgressState meta, StageBoardOverlayUI overlay, Func<bool> save, Action complete) // 실제 의존성 연결
        { // 생성자 시작
            _run = run; // 런 상태
            _economy = economy; // 재화 상태
            _catalog = catalog; // 카드 풀
            _recipes = recipes; // 레시피 풀
            _unlocks = unlocks; // 고정 해금
            _meta = meta; // 정보 열람 상태
            _overlay = overlay; // 표시 대상
            _save = save; // 저장 경로
            _complete = complete; // 진행 경로
        } // 생성자 종료
        public void Open() // 이벤트 진입·이어하기
        { // 메서드 시작
            _session = RunEventService.GetOrCreate(_run); // 이전 선택 유지
            if (_session == null) return; // 잘못된 진입 차단
            if (_session.resolved && !string.IsNullOrWhiteSpace(_session.result?.informationId)) _meta?.TryReadEventInformation(_session.result.informationId); // 런 저장 후 영구 저장 실패 복구
            SaveAndShow(); // 최초 선택 또는 결과 안전 저장
        } // 메서드 종료
        public void Close() // 화면 소유 해제
        { // 메서드 시작
            _closed = true; // 남은 입력 콜백 차단
        } // 메서드 종료
        private void SaveAndShow() // 저장 완료 후 입력 허용
        { // 메서드 시작
            if (!IsCurrent) return; // 다른 노드로 넘어간 입력 제외
            if (_save == null || !_save()) // 저장 실패 확인
            { // 조건 시작
                ShowSaveFailure(); // 재시도 화면
                return; // 입력 진행 중단
            } // 조건 종료
            if (_session.resolved) ShowResult(); // 완료 결과 다시 표시
            else ShowChoices(); // 미완료 선택지 표시
        } // 메서드 종료
        private void ShowChoices() // 효과·비활성 사유 안내
        { // 메서드 시작
            if (!IsCurrent || _session.resolved) return; // 완료·이전 화면 차단
            var options = new List<StageOverlayOption>(); // 실제 선택지
            foreach (RunEventChoice choice in _session.definition.choices) // 기획 A·B·C
            { // 반복 시작
                string reason = Reason(choice, null); // 현재 선택 조건
                string description = choice.description; // 확정된 비용·보상 안내
                if (choice.information && _meta != null && _meta.HasEventInformation(_session.definition.id)) description += "\n이미 확인한 정보 · 기본 효과 유지"; // 중복 정보와 보상 구분
                if (!string.IsNullOrEmpty(reason)) description += "\n선택 불가: " + reason; // 비활성 이유
                RunEventChoice captured = choice; // 선택 콜백 고정
                options.Add(new StageOverlayOption(choice.title, description, string.IsNullOrEmpty(reason), () => Choose(captured), showDescriptionOnCard: true)); // 비용·불가 이유를 상시 표시
            } // 반복 종료
            Present(_session.definition.title, "Gold " + _economy.Currency + "  |  King HP " + _run.KingHp + "/" + RunEconomyRules.PrototypeKingMaxHp, options); // 짧은 상태 줄
        } // 메서드 종료
        private void Choose(RunEventChoice choice) // 첫 선택
        { // 메서드 시작
            if (!IsCurrent || !string.IsNullOrEmpty(Reason(choice, null))) return; // 오래된·비활성 입력 차단
            if (choice.NeedsTarget) ShowTargets(choice, 0); // 비용 없이 대상 선택
            else Commit(choice, null); // 즉시 효과 확정
        } // 메서드 종료
        private void ShowTargets(RunEventChoice choice, int page) // 강화·제거·교환 대상
        { // 메서드 시작
            if (!IsCurrent || _session.resolved) return; // 완료·이전 입력 차단
            List<PieceDefinition> targets = RunEventCardRules.Targets(_run, choice); // 실제 보유 대상
            int pages = Math.Max(1, (targets.Count + TargetsPerPage - 1) / TargetsPerPage); // 페이지당 두 장
            page = Math.Max(0, Math.Min(pages - 1, page)); // 페이지 범위
            int currentPage = page; // 이동 콜백 값
            var options = new List<StageOverlayOption>(); // 대상과 탐색 버튼
            for (int index = page * TargetsPerPage; index < Math.Min(targets.Count, page * TargetsPerPage + TargetsPerPage); index++) // 현재 페이지 카드
            { // 반복 시작
                PieceDefinition card = targets[index]; // 실제 카드 참조
                string reason = Reason(choice, card); // 대상별 교환 후보 포함 검사
                string description = choice.description + "\nHP " + card.BaseHp + " → " + (card.BaseHp + choice.hpUpgrade) + " / ATK " + card.BaseAtk + " → " + (card.BaseAtk + choice.atkUpgrade); // 확정 전 수치
                if (!string.IsNullOrEmpty(reason)) description += "\n" + reason; // 대상별 불가 사유
                options.Add(new StageOverlayOption(card.DisplayName, description, string.IsNullOrEmpty(reason), () => Commit(choice, card), showDescriptionOnCard: true)); // 최종 확정 전 비용·능력치 상시 표시
            } // 반복 종료
            if (page > 0) options.Add(new StageOverlayOption("◀ 이전", "이전 카드", true, () => ShowTargets(choice, currentPage - 1), isNavigation: true)); // 이전 페이지
            if (page + 1 < pages) options.Add(new StageOverlayOption("다음 ▶", "다음 카드", true, () => ShowTargets(choice, currentPage + 1), isNavigation: true)); // 다음 페이지
            options.Add(new StageOverlayOption("취소", "비용 없이 이벤트 선택으로 돌아갑니다.", true, ShowChoices, isNavigation: true)); // 비용 없는 취소
            Present(choice.title, "카드 확정 시 적용 · " + (page + 1) + "/" + pages, options); // 지불 시점 안내
        } // 메서드 종료
        private string Reason(RunEventChoice choice, PieceDefinition target) // 서비스와 같은 조건 사용
        { // 메서드 시작
            return RunEventService.UnavailableReason(_run, _economy, _session, choice, RunEconomyRules.PrototypeKingMaxHp, _catalog, _unlocks, _recipes, target); // 화면과 실행 조건 일치
        } // 메서드 종료
        private void Commit(RunEventChoice choice, PieceDefinition card) // 최종 선택 적용
        { // 메서드 시작
            if (!IsCurrent) return; // 오래된 화면 콜백 차단
            if (!RunEventService.TryCommit(_run, _economy, _session, choice, card, RunEconomyRules.PrototypeKingMaxHp, _catalog, _unlocks, _recipes, _meta, out RunEventRecord result)) // 실패 조건 재확인
            { // 조건 시작
                ShowChoices(); // 유효한 선택 화면 복원
                return; // 적용 중단
            } // 조건 종료
            Debug.Log("102일차 이벤트 결과: " + result.eventId + "/" + result.choiceId + " GoldΔ=" + (result.goldAfter - result.goldBefore) + " HPΔ=" + (result.kingAfter - result.kingBefore)); // 실제 손익 진단
            SaveAndShow(); // 결과 저장 후 확인 화면
        } // 메서드 종료
        private void ShowResult() // 저장된 실제 결과 표시
        { // 메서드 시작
            RunEventRecord result = _session.result; // 복원 가능한 결과
            if (result == null) // 손상된 결과 확인
            { // 조건 시작
                ShowSaveFailure(); // 오류 진행 차단
                return; // 결과 표시 중단
            } // 조건 종료
            var body = new StringBuilder(); // 손익 안내
            body.AppendLine("Gold " + result.goldBefore + " → " + result.goldAfter + " / King HP " + result.kingBefore + " → " + result.kingAfter); // 실제 전후 수치
            if (!string.IsNullOrEmpty(result.addedCardId)) body.AppendLine("획득: " + result.addedCardName); // 카드 획득
            if (!string.IsNullOrEmpty(result.removedCardId)) body.AppendLine("제거: " + result.removedCardName); // 카드 제거·교환
            if (!string.IsNullOrEmpty(result.targetName)) body.AppendLine(result.targetName + " · HP +" + result.hpUpgrade + " / ATK +" + result.atkUpgrade); // 실제 강화량
            if (!string.IsNullOrEmpty(result.informationId)) body.AppendLine(result.newInformation ? "선택 단서 후보 확인: " + result.eventTitle : "이미 확인한 정보 · 기본 효과 적용"); // 핵심 단서와 분리된 후보 열람
            var options = new List<StageOverlayOption> // 결과와 명시적 진행
            { // 목록 시작
                new StageOverlayOption(result.choiceTitle, body.ToString().TrimEnd(), false, null, showDescriptionOnCard: true), // 실제 손익 상시 표시
                new StageOverlayOption("다음 경로", "선택 결과 저장 완료", true, Continue) // 진행 확정
            }; // 목록 종료
            Present(_session.definition.title, "선택 결과", options); // 자동 종료 전 결과 확인
        } // 메서드 종료
        private void ShowSaveFailure() // 효과 재적용 없는 저장 재시도
        { // 메서드 시작
            var options = new List<StageOverlayOption> // 재적용 없는 저장 재시도
            { // 목록 시작
                new StageOverlayOption("저장 재시도", "저장 성공 후 진행할 수 있습니다.", true, SaveAndShow, showDescriptionOnCard: true) // 같은 결과 저장
            }; // 목록 종료
            Present("저장 확인 필요", "결과를 다시 적용하지 않고 저장을 재시도합니다.", options); // 실패 상태 유지
        } // 메서드 종료
        private void Continue() // 결과 확인 후 지도 복귀
        { // 메서드 시작
            if (!IsCurrent || !_session.resolved) return; // 중복·미완료 진행 차단
            if (_save == null || !_save()) // 마지막 저장 확인
            { // 조건 시작
                ShowSaveFailure(); // 재시도 화면
                return; // 진행 중단
            } // 조건 종료
            _closed = true; // 같은 프레임 진행 중복 차단
            _complete?.Invoke(); // 기존 비전투 완료 경로
        } // 메서드 종료
        private void Present(string title, string subtitle, List<StageOverlayOption> options) // 화면과 검사에 같은 옵션 제공
        { // 메서드 시작
            CurrentTitle = title; // 현재 제목
            CurrentSubtitle = subtitle; // 현재 상태
            CurrentOptions = options; // 현재 콜백과 활성 조건
            if (_overlay != null) _overlay.ShowPage(StageOverlayMode.Event, title, subtitle, options); // 실제 보드 UI 연결
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
