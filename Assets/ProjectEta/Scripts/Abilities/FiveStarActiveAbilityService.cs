using UnityEngine; // 좌표 참조
using ProjectEta.Battle; // 턴 상태 참조
using ProjectEta.Board; // 보드 상태 참조
using ProjectEta.Pieces; // 기물 상태 참조

namespace ProjectEta.Abilities // 5성 선택 능력 영역
{ // 범위 시작
    public static class FiveStarActiveAbilityService // 대현자 대상 검증과 환영 형태 선택
    { // 범위 시작
        public static bool IsSageChoice(PieceAbilityDefinition ability) // 현자의 선택 능력 확인
        { // 범위 시작
            return ability != null && (ability.AbilityId == FiveStarAbilityIds.SageHeal || ability.AbilityId == FiveStarAbilityIds.SagePoison || ability.AbilityId == FiveStarAbilityIds.SageGuard); // 세 가지 선택 판정
        } // 범위 종료

        public static string ValidateSageChoice(PieceAbilityDefinition ability, AbilityExecutionContext context) // 실제 대상과 행동 상태 검증
        { // 범위 시작
            var owner = context.Owner; // 능력 사용자
            var target = context.TargetPiece; // 선택한 기물
            var board = context.Board ?? AbilityBoardRegistry.FindBoardContaining(owner); // 사용자 보드 조회
            if (owner == null || owner.IsDead || !owner.CanAttack || target == null || target.IsDead || board == null) // 생존과 행동 가능 여부
            { // 범위 시작
                return "살아 있는 사용자와 대상이 필요합니다."; // 대상 오류 안내
            } // 범위 종료
            if (FiveStarCombatAbilityResolver.FindAbility(owner, ability.AbilityId) != ability) // 보유 능력 확인
            { // 범위 시작
                return "이 기물이 보유한 능력이 아닙니다."; // 타 기물 능력 사용 차단
            } // 범위 종료
            if (board.GetTile(owner.BoardPosition)?.OccupyingPiece != owner || board.GetTile(target.BoardPosition)?.OccupyingPiece != target) // 같은 보드 점유 확인
            { // 범위 시작
                return "같은 보드의 대상을 선택하세요."; // 다른 전투 대상 차단
            } // 범위 종료
            if (FiveStarCombatAbilityResolver.Distance(owner.BoardPosition, target.BoardPosition) > ability.Effects[0].Radius) // 테스트용 거리 2 적용
            { // 범위 시작
                return "거리 2 이내 대상을 선택하세요."; // 범위 오류 안내
            } // 범위 종료
            bool poison = ability.AbilityId == FiveStarAbilityIds.SagePoison; // 적 대상 선택 여부
            if (poison == (owner.IsPlayerPiece == target.IsPlayerPiece)) // 능력별 진영 조건
            { // 범위 시작
                return poison ? "독은 적에게 사용하세요." : "회복과 보호는 아군에게 사용하세요."; // 진영 오류 안내
            } // 범위 종료
            if (ability.AbilityId == FiveStarAbilityIds.SageGuard && target.SageGuardAmount > 0) // 보호 중복 확인
            { // 범위 시작
                return "이미 다음 피격 보호가 적용되어 있습니다."; // 중복 보호 차단
            } // 범위 종료
            return string.Empty; // 검증 성공
        } // 범위 종료

        public static AbilityExecutionResult EvaluateGuard(PieceAbilityDefinition ability, AbilityExecutionContext context, bool execute) // 다음 피격 보호 전용 적용
        { // 범위 시작
            int amount = Mathf.Abs(ability.Effects[0].Amount); // 보호 감소량 조회
            if (execute) // 실행 여부 확인
            { // 범위 시작
                context.TargetPiece.SetSageGuard(amount); // 공격력과 독립된 보호 저장
            } // 범위 종료
            bool consumed = execute && context.Owner.IsPlayerPiece && context.TurnManager != null && context.TurnManager.TryCompletePlayerAction(); // 일반 행동 1회 소비
            return AbilityExecutionResult.Succeeded(amount, context.TargetPiece, consumed, new[] { context.TargetPiece }); // 적용 결과 반환
        } // 범위 종료

        public static bool TrySelectStartForm(PieceRuntimeState piece, int index, BoardState board, TurnManager turns) // 배치 턴 환영 형태 선택
        { // 범위 시작
            if (piece == null || piece.IsDead || !piece.IsPlayerPiece || board == null || turns == null || !turns.CanDeploy || index < 0 || index >= 5) // 배치 권한과 형태 범위 확인
            { // 범위 시작
                return false; // 잘못된 선택 거부
            } // 범위 종료
            if (FiveStarCombatAbilityResolver.FindAbility(piece, FiveStarAbilityIds.Phantom) == null || board.GetTile(piece.BoardPosition)?.OccupyingPiece != piece) // 환영장군과 실제 점유 확인
            { // 범위 시작
                return false; // 다른 기물의 변환 차단
            } // 범위 종료
            piece.RestoreMovementCycleIndex(index); // 선택한 형태 적용
            return true; // 행동권 소비 없는 배치 선택 완료
        } // 범위 종료

        public static string GetFormName(int index) // 현재 이동 형태 표시
        { // 범위 시작
            string[] names = { "기병", "사제", "성채", "장군", "척탄병" }; // 순환 형태 이름
            return names[Mathf.Clamp(index, 0, 4)]; // 안전한 형태 이름 반환
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
