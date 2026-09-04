using System; // Action 이벤트를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 관련 타입을 모아두는 네임스페이스
{
    public class BattleHooks // 29일차: 이동·공격·피해·턴 시점마다 구독자에게 통지하는 전투 훅 버스(전투 1회당 하나씩 생성해 사용)
    {
        public event Action<PieceRuntimeState, Vector2Int, Vector2Int> BeforeMove; // 기물이 실제로 이동하기 직전(원래 좌표, 목표 좌표)
        public event Action<PieceRuntimeState, Vector2Int, Vector2Int> AfterMove; // 기물 이동이 보드·화면에 모두 반영된 직후
        public event Action<PieceRuntimeState, PieceRuntimeState> BeforeAttack; // 공격 판정을 계산하기 직전(공격자, 방어자)
        public event Action<CombatResult> AfterAttack; // 공격 판정과 사망 처리까지 모두 끝난 직후
        public event Action<DamageContext> BeforeDamage; // 실제 HP를 깎기 직전(구독자가 Amount를 줄여 피해 경감 가능)
        public event Action<PieceRuntimeState, PieceRuntimeState, int> AfterDamage; // 32일차: 실제로 적용된 최종 피해량이 HP에 반영된 직후(대상, 발생원 — 상태 이상 등 발생원이 없으면 null, 적용량)
        public event Action<TurnState, int> TurnStart; // 새 일반 턴(PlayerTurn)이 시작된 직후
        public event Action<TurnState, int> TurnEnd; // 플레이어+적 행동이 모두 끝나 1턴이 종료된 직후

        public void RaiseBeforeMove(PieceRuntimeState piece, Vector2Int origin, Vector2Int destination) => BeforeMove?.Invoke(piece, origin, destination); // BeforeMove 발행
        public void RaiseAfterMove(PieceRuntimeState piece, Vector2Int origin, Vector2Int destination) => AfterMove?.Invoke(piece, origin, destination); // AfterMove 발행
        public void RaiseBeforeAttack(PieceRuntimeState attacker, PieceRuntimeState defender) => BeforeAttack?.Invoke(attacker, defender); // BeforeAttack 발행
        public void RaiseAfterAttack(CombatResult result) => AfterAttack?.Invoke(result); // AfterAttack 발행
        public void RaiseBeforeDamage(DamageContext context) => BeforeDamage?.Invoke(context); // BeforeDamage 발행
        public void RaiseAfterDamage(PieceRuntimeState target, PieceRuntimeState source, int appliedAmount) => AfterDamage?.Invoke(target, source, appliedAmount); // AfterDamage 발행(32일차: 발생원 포함)
        public void RaiseTurnStart(TurnState state, int turnNumber) => TurnStart?.Invoke(state, turnNumber); // TurnStart 발행
        public void RaiseTurnEnd(TurnState state, int turnNumber) => TurnEnd?.Invoke(state, turnNumber); // TurnEnd 발행
    }
}
