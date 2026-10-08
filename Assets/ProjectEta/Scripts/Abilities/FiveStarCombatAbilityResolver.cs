using System; // 식별자 비교
using UnityEngine; // 좌표와 수치 계산
using ProjectEta.Battle; // 피해 처리
using ProjectEta.Board; // 보드 조회
using ProjectEta.Pieces; // 기물 상태

namespace ProjectEta.Abilities // 5성 전투 능력 영역
{ // 범위 시작
    public static class FiveStarCombatAbilityResolver // 조건부 공격과 아군 보호 처리
    { // 범위 시작
        public static PieceAbilityDefinition FindAbility(PieceRuntimeState piece, string id) // 기물의 능력 조회
        { // 범위 시작
            if (piece?.Definition == null) // 정의 누락 확인
            { // 범위 시작
                return null; // 조회 중단
            } // 범위 종료
            return Array.Find(piece.Definition.Abilities, a => a != null && a.AbilityId == id); // 일치 능력 반환
        } // 범위 종료

        public static int PreviewAttackBonus(PieceRuntimeState actor, PieceRuntimeState target, BoardState board = null) // 직접 공격의 돌파 피해 예측
        { // 범위 시작
            var ability = FindAbility(actor, FiveStarAbilityIds.Rider) ?? FindAbility(actor, FiveStarAbilityIds.Sky); // 돌파 능력 조회
            if (ability == null || actor.IsDead || target == null || target.IsDead || actor.IsPlayerPiece == target.IsPlayerPiece) // 공격 대상 검증
            { // 범위 시작
                return 0; // 보너스 제외
            } // 범위 종료
            board = board ?? AbilityBoardRegistry.FindBoardContaining(actor); // 실제 보드 조회
            if (board == null || !MovementResolver.GetReachableTiles(actor, board).AttackTiles.Contains(target.BoardPosition)) // 경로와 행동 가능 여부 검증
            { // 범위 시작
                return 0; // 막힌 공격 제외
            } // 범위 종료
            Vector2Int delta = target.BoardPosition - actor.BoardPosition; // 공격 상대 좌표
            foreach (var rule in actor.Definition.MovementRules) // 기물 이동 규칙 순회
            { // 범위 시작
                if (rule == null || rule.Kind != MovementRuleKind.Rider) // 라이더 규칙 확인
                { // 범위 시작
                    continue; // 단일 도약 제외
                } // 범위 종료
                foreach (var vector in rule.Vectors) // 라이더 방향 순회
                { // 범위 시작
                    for (int step = 2; step <= rule.MaxSteps; step++) // 두 번째 착지점부터 확인
                    { // 범위 시작
                        if (vector * step == delta) // 반복 착지점 일치 확인
                        { // 범위 시작
                            return Mathf.Max(0, ability.Effects[0].Amount); // 데이터의 추가 피해 반환
                        } // 범위 종료
                    } // 범위 종료
                } // 범위 종료
            } // 범위 종료
            return 0; // 반복 경로가 아닌 공격 제외
        } // 범위 종료

        public static void ProcessBeforeDamage(DamageContext context, BattleHooks hooks = null) // 최종 피해 대상 보호
        { // 범위 시작
            if (context?.Target == null || context.Target.IsDead || context.Amount <= 0) // 유효 피해 확인
            { // 범위 시작
                return; // 잘못된 피해 중단
            } // 범위 종료
            PieceRuntimeState target = context.Target; // 리다이렉트 이후 대상
            var board = AbilityBoardRegistry.FindBoardContaining(target); // 대상 보드 조회
            if (target.SageGuardAmount > 0) // 현자의 다음 피격 보호 확인
            { // 범위 시작
                context.Amount = Mathf.Max(1, context.Amount - target.SageGuardAmount); // 보호 수치 적용
                target.SetSageGuard(0); // 피격 1회 사용 완료
            } // 범위 종료
            if (board == null) // 주변 기물 조회 가능 여부
            { // 범위 시작
                return; // 자기 보호만 처리
            } // 범위 종료
            var paladin = FindProtector(board, target, FiveStarAbilityIds.Paladin, 1, requireReady: true); // 인접 수호자 선택
            if (paladin != null) // 수호자 존재 확인
            { // 범위 시작
                var effect = FindAbility(paladin, FiveStarAbilityIds.Paladin).Effects[0]; // 보호 수치 조회
                context.Amount = Mathf.Max(1, context.Amount + effect.Amount); // 최소 피해 1의 보호 적용
                paladin.PaladinGuardSpent = true; // 이번 턴 발동 완료
            } // 범위 종료
            var regent = FindProtector(board, target, FiveStarAbilityIds.Regent, 2, requireReady: false); // 거리 2의 분담자 선택
            if (regent != null && context.Amount > 1) // 분담 가능 피해 확인
            { // 범위 시작
                int maximum = Mathf.Abs(FindAbility(regent, FiveStarAbilityIds.Regent).Effects[0].Amount); // 데이터의 분담 상한
                int shared = Mathf.Min(maximum, Mathf.Min(context.Amount - 1, regent.CurrentHp - 1)); // 대상 최소 피해와 분담자 생존 보장
                context.Amount -= shared; // 원래 대상 피해 감소
                regent.CurrentHp -= shared; // 분담자 체력 감소
                hooks?.RaiseAfterDamage(regent, context.Source, shared); // 분담자 피격 알림
            } // 범위 종료
        } // 범위 종료

        private static PieceRuntimeState FindProtector(BoardState board, PieceRuntimeState target, string id, int radius, bool requireReady) // 동일 효과 중 한 명 선택
        { // 범위 시작
            PieceRuntimeState best = null; // 선택한 보호자
            foreach (var piece in AbilityBoardRegistry.GetUniquePieces(board)) // 보드 기물 순회
            { // 범위 시작
                if (piece == target || piece.IsDead || piece.IsPlayerPiece != target.IsPlayerPiece || FindAbility(piece, id) == null) // 생존 아군 보호자 검증
                { // 범위 시작
                    continue; // 보호 불가 기물 제외
                } // 범위 종료
                if (requireReady ? piece.PaladinGuardSpent : piece.CurrentHp <= 1) // 재발동 또는 생존 조건 확인
                { // 범위 시작
                    continue; // 사용 불가 보호자 제외
                } // 범위 종료
                if (Distance(piece.BoardPosition, target.BoardPosition) > radius) // 보호 거리 확인
                { // 범위 시작
                    continue; // 범위 밖 제외
                } // 범위 종료
                if (best == null || piece.BoardPosition.y < best.BoardPosition.y || (piece.BoardPosition.y == best.BoardPosition.y && piece.BoardPosition.x < best.BoardPosition.x)) // 좌표 순서로 동률 결정
                { // 범위 시작
                    best = piece; // 우선 보호자 기록
                } // 범위 종료
            } // 범위 종료
            return best; // 보호자 반환
        } // 범위 종료

        public static int Distance(Vector2Int first, Vector2Int second) // 대각선 포함 보드 거리 계산
        { // 범위 시작
            return Mathf.Max(Mathf.Abs(first.x - second.x), Mathf.Abs(first.y - second.y)); // 가장 긴 축 거리 반환
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
