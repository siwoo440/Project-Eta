using System; // StringComparison 사용
using System.Collections.Generic; // HashSet<T>·IReadOnlyList<T> 사용
using UnityEngine; // Vector2Int·Resources 사용
using ProjectEta.Battle; // BattleOutcome 사용
using ProjectEta.Board; // BoardState 사용
using ProjectEta.Cards; // DeckState·HandState 사용
using ProjectEta.Fusion; // FusionDiscoveryLog 사용
using ProjectEta.King; // 51일차 선택 킹 저장·복원 사용
using ProjectEta.Pieces; // PieceDefinition·PieceRuntimeState·PieceDatabase 사용

namespace ProjectEta.Run
{
    public class RunState
    {
        private int _kingHp; // 킹 체력 내부 값

        public string RunId { get; private set; } // 메타 보상 중복 방지용 런 고유 ID
        public BattleState Battle { get; private set; } // 현재 전투 임시 상태
        public RoundState Round { get; } // 현재 1~10라운드 진행 상태
        public RunFlowState Flow { get; } // 전투·지도·종료 상위 흐름
        public RouteMapState RouteMap { get; } // 체스판 경로 지도 상태
        public BoardState Board => Battle.Board; // 기존 호출부 호환 보드 접근
        public DeckState Deck { get; } // 런에서 유지되는 덱 상태
        public HandState Hand => Battle.Hand; // 기존 호출부 호환 손패 접근
        public FusionDiscoveryLog FusionDiscovery { get; } // 숨김 합성식 발견 기록
        public int MetaCurrency { get; set; } // 기존 런 세이브 호환 재화
        public bool IsDefeated => _kingHp <= 0; // 킹 체력 기반 패배 여부
        public RoundProgressStatus CurrentRoundStatus => Round.Status; // 현재 라운드 진행 상태
        public bool IsBossRound => Round.IsBossRound; // 현재 보스 라운드 여부
        public BattleOutcome LastBattleOutcome => Round.BattleOutcome; // 현재 라운드 전투 결과
        public RunFlowPhase CurrentFlowPhase => Flow.Phase; // 현재 로그라이트 상위 진행 단계
        public BoardMode CurrentBoardMode => Flow.BoardMode; // 현재 체스판 역할
        public IReadOnlyList<StageNode> SelectableStageNodes => RouteMap.GetSelectableNodes(); // 현재 선택 가능 스테이지 노드

        public int CurrentRound
        {
            get => Round.RoundNumber; // RoundState 라운드 번호 반환
            set => Round.SetRoundNumber(value); // RoundState 라운드 번호 변경
        }

        public int KingHp
        {
            get => _kingHp; // 현재 킹 체력 반환
            set => _kingHp = value < 0 ? 0 : value; // 음수 체력 방지
        }

        public RunState(int startingKingHp)
        {
            _kingHp = startingKingHp; // 시작 킹 체력 저장
            RunId = Guid.NewGuid().ToString("N"); // 새 런 고유 ID 생성
            Battle = new BattleState(); // 첫 전투 임시 상태 생성
            Round = new RoundState(RoundState.FirstRound); // 1라운드 상태 생성
            Flow = new RunFlowState(); // 전투 모드 런 흐름 생성
            RouteMap = new RouteMapState(); // 빈 경로 지도 상태 생성
            Deck = new DeckState(); // 새 덱 상태 생성
            FusionDiscovery = new FusionDiscoveryLog(); // 합성 발견 기록 생성
        }

        public void StartCurrentRound()
        {
            Flow.EnterBattle(); // 동일 체스판을 전투 모드로 지정
            Round.Begin(); // 라운드 상태를 진행 중으로 변경
        }

        public void RecordBattleOutcome(BattleOutcome outcome)
        {
            Round.Complete(outcome); // 승패 결과를 라운드 상태에 반영
        }

        public void HandleBattleOutcome(BattleOutcome outcome)
        {
            if (Flow.Phase != RunFlowPhase.Battle) return; // 이미 지도·종료 상태면 중복 결과 무시

            RecordBattleOutcome(outcome); // 기존 라운드 승패 상태 기록

            if (outcome == BattleOutcome.Defeat)
            {
                Flow.FailRun(); // 런 실패 상태 전환
                return; // 패배 처리 종료
            }

            if (outcome != BattleOutcome.Victory) return; // 승리 외 결과는 전투 상태 유지

            if (CurrentRound >= RoundState.FinalRound)
            {
                Flow.CompleteRun(); // 런 완료 상태 전환
                return; // 최종 승리 처리 종료
            }

            RouteMap.PreparePrototypeAfterBattle(CurrentRound); // 다음 깊이 선택 후보 상태 준비
            Flow.EnterMap(); // 동일 체스판을 경로 지도 모드로 전환
        }

        public void ResetBattleState()
        {
            Battle = new BattleState(); // 새 보드·손패 상태로 교체
        }

        public RunSaveData ToSaveData()
        {
            RunEconomyState economy = RunEconomyService.GetOrCreate(this); // 현재 런 Gold 상태 조회
            KingRunState kingState = KingRunStateService.Get(this); // 현재 선택 킹 상태 조회

            var data = new RunSaveData
            {
                saveVersion = RunSaveData.CurrentVersion, // 51일차 저장 포맷 버전 기록
                runId = RunId, // 런 고유 ID 기록
                kingHp = _kingHp, // 킹 체력 기록
                currentRound = CurrentRound, // 라운드 번호 기록
                metaCurrency = MetaCurrency, // 기존 런 세이브 호환 재화 기록
                roundStatus = (int)CurrentRoundStatus, // 라운드 진행 상태 기록
                battleOutcome = (int)LastBattleOutcome, // 전투 결과 기록
                isBossRound = IsBossRound, // 보스 라운드 플래그 기록
                flowPhase = (int)CurrentFlowPhase, // 현재 상위 런 흐름 기록
                runCurrency = economy != null ? economy.Currency : RunEconomyRules.StartingCurrency, // 런 Gold 기록
                selectedKingArchetype = kingState != null ? (int)kingState.Archetype : (int)KingArchetype.Default, // 선택 킹 기록
                currentStageDefinitionId = ResolveCurrentStageDefinitionId(), // 현재 스테이지 정의 ID 기록
                routeMap = RouteMap.ToSaveData() // 현재 지도·위치·선택 경로 기록
            };

            foreach (var recipeId in FusionDiscovery.DiscoveredRecipeIds)
            {
                data.discoveredRecipeIds.Add(recipeId); // 발견 합성식 기록
            }

            foreach (var card in Hand.Hand)
            {
                if (card != null) data.handCardIds.Add(card.PieceId); // 손패 기록
            }

            foreach (var card in Deck.OwnedCardPool)
            {
                if (card == null) continue; // 빈 카드 제외

                data.ownedCardPoolIds.Add(card.PieceId); // 구버전 호환 보유 풀 ID 기록
                data.ownedCards.Add(CreateCardSaveData(card)); // 강화 스탯 포함 보유 카드 기록
            }

            foreach (var card in Deck.DrawPile)
            {
                if (card != null) data.drawPileIds.Add(card.PieceId); // 드로우 순서 기록
            }

            foreach (var card in Deck.DeadCardPile)
            {
                if (card != null) data.deadCardPileIds.Add(card.PieceId); // 죽은 카드 기록
            }

            var savedPieces = new HashSet<PieceRuntimeState>(); // 대형 기물 중복 저장 방지

            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    var boardPosition = new Vector2Int(x, y); // 현재 검사 좌표 생성
                    var occupyingPiece = Board.GetTile(boardPosition).OccupyingPiece; // 현재 점유 기물 조회
                    if (occupyingPiece == null || !savedPieces.Add(occupyingPiece)) continue; // 빈 칸·중복 대형 기물 제외

                    var pieceSaveData = new PieceSaveData
                    {
                        x = occupyingPiece.BoardPosition.x, // 기물 기준 X 기록
                        y = occupyingPiece.BoardPosition.y, // 기물 기준 Y 기록
                        pieceId = occupyingPiece.Definition.PieceId, // 기물 ID 기록
                        currentHp = occupyingPiece.CurrentHp, // 현재 체력 기록
                        isPlayerPiece = occupyingPiece.IsPlayerPiece, // 진영 기록
                        movementCycleIndex = occupyingPiece.MovementCycleIndex // Chameleon 순환 단계 기록
                    };

                    foreach (var statusEffect in occupyingPiece.StatusEffects)
                    {
                        pieceSaveData.statusEffects.Add(new StatusEffectSaveData
                        {
                            statusType = (int)statusEffect.Definition.StatusType, // 상태 종류 기록
                            remainingTurns = statusEffect.RemainingTurns, // 남은 턴 기록
                            stackCount = statusEffect.StackCount // 중첩 수 기록
                        });
                    }

                    data.boardPieces.Add(pieceSaveData); // 기물 저장 항목 추가
                }
            }

            return data; // 완성 저장 데이터 반환
        }

        public static RunState FromSaveData(RunSaveData data, PieceDatabase database, StatusEffectDatabase statusEffectDatabase = null)
        {
            if (data == null) return null; // 잘못된 저장 데이터 방어

            int restoredKingHp = data.kingHp < 0 ? 0 : data.kingHp; // 저장 킹 HP 음수만 보정
            var runState = new RunState(restoredKingHp)
            {
                MetaCurrency = data.metaCurrency // 기존 런 세이브 호환 재화 복원
            };

            int restoredRound = data.currentRound <= 0 ? RoundState.FirstRound : data.currentRound; // 구버전 라운드 기본값 보정
            RoundProgressStatus restoredStatus = ParseRoundStatus(data.roundStatus); // 저장 라운드 상태 검증
            BattleOutcome restoredOutcome = ParseBattleOutcome(data.battleOutcome); // 저장 전투 결과 검증
            runState.Round.Restore(restoredRound, restoredStatus, restoredOutcome); // 라운드 진행 상태 복원

            if (data.saveVersion >= RunSaveData.CurrentVersion)
            {
                if (!string.IsNullOrWhiteSpace(data.runId)) runState.RunId = data.runId; // 저장 런 고유 ID 복원
                runState.Flow.Restore(data.flowPhase); // 51일차 상위 런 흐름 복원
                runState.RouteMap.Restore(data.routeMap); // 51일차 경로 지도·현재 위치 복원
                RunEconomyService.Restore(runState, data.runCurrency); // 51일차 런 Gold 복원
                KingRunStateService.Restore(runState, ParseKingArchetype(data.selectedKingArchetype)); // 51일차 선택 킹 복원
            }

            if (data.discoveredRecipeIds != null)
            {
                runState.FusionDiscovery.Restore(data.discoveredRecipeIds); // 숨김 합성식 기록 복원
            }

            if (data.handCardIds != null)
            {
                foreach (var pieceId in data.handCardIds)
                {
                    var definition = FindDefinition(database, pieceId); // 기물 정의 조회
                    if (definition != null) runState.Hand.TryAddCard(definition); // 손패 카드 추가
                }
            }

            RestoreOwnedCards(runState, data, database); // 강화 카드 포함 보유 풀 복원

            if (data.drawPileIds != null)
            {
                foreach (var pieceId in data.drawPileIds)
                {
                    var definition = FindDefinition(database, pieceId); // 기물 정의 조회
                    if (definition != null) runState.Deck.AddToDrawPile(definition); // 드로우 더미 추가
                }
            }

            if (data.deadCardPileIds != null)
            {
                foreach (var pieceId in data.deadCardPileIds)
                {
                    var definition = FindDefinition(database, pieceId); // 기물 정의 조회
                    if (definition != null) runState.Deck.MoveToDeadPile(definition); // 죽은 카드 더미 추가
                }
            }

            if (data.boardPieces != null)
            {
                foreach (var pieceData in data.boardPieces)
                {
                    if (pieceData == null) continue; // 잘못된 항목 제외

                    var definition = FindDefinition(database, pieceData.pieceId); // PieceDatabase·Resources 정의 조회
                    if (definition == null) continue; // 정의 누락 기물 제외

                    var boardPosition = new Vector2Int(pieceData.x, pieceData.y); // 저장 기준 좌표 생성
                    var anchorTile = runState.Board.GetTile(boardPosition); // 기준 좌표 타일 조회
                    if (anchorTile == null || anchorTile.OccupyingPiece != null) continue; // 범위 밖·중복 대형 기물 제외

                    var runtimePiece = new PieceRuntimeState(definition, boardPosition, pieceData.isPlayerPiece)
                    {
                        CurrentHp = pieceData.currentHp // 현재 체력 복원
                    };

                    runtimePiece.RestoreMovementCycleIndex(pieceData.movementCycleIndex); // Chameleon 순환 단계 복원

                    if (statusEffectDatabase != null && pieceData.statusEffects != null)
                    {
                        foreach (var statusData in pieceData.statusEffects)
                        {
                            var statusDefinition = statusEffectDatabase.FindByType((StatusEffectType)statusData.statusType); // 상태 종류 정의 조회
                            if (statusDefinition != null) runtimePiece.RestoreStatusEffect(statusDefinition, statusData.remainingTurns, statusData.stackCount); // 상태 이상 복원
                        }
                    }

                    Vector2Int footprint = GetSafeFootprint(definition); // 안전한 점유 크기 계산

                    if (!runState.Board.TryOccupyArea(boardPosition, footprint, runtimePiece))
                    {
                        anchorTile.OccupyingPiece = runtimePiece; // 충돌 세이브 기준 칸 복원
                    }
                }
            }

            return runState; // 복원 런 반환
        }

        private string ResolveCurrentStageDefinitionId()
        {
            StageNode selected = RouteMap.SelectedNode; // 현재 선택 스테이지 노드 조회
            if (selected != null) return selected.StageDefinitionId; // 선택 노드 정의 ID 우선 반환

            StageNode current = RouteMap.CurrentNode; // 현재 지도 노드 조회
            return current != null ? current.StageDefinitionId : string.Empty; // 현재 노드 정의 ID 또는 빈 값 반환
        }

        private static CardSaveData CreateCardSaveData(PieceDefinition card)
        {
            return new CardSaveData
            {
                pieceId = card.PieceId, // 원본 PieceId 기록
                displayName = card.DisplayName, // 런타임 강화 이름 기록
                baseHp = card.BaseHp, // 런타임 강화 HP 기록
                baseAtk = card.BaseAtk // 런타임 강화 ATK 기록
            };
        }

        private static void RestoreOwnedCards(RunState runState, RunSaveData data, PieceDatabase database)
        {
            if (data.saveVersion >= RunSaveData.CurrentVersion && data.ownedCards != null && data.ownedCards.Count > 0)
            {
                for (int i = 0; i < data.ownedCards.Count; i++)
                {
                    CardSaveData cardData = data.ownedCards[i]; // 저장 카드 스냅샷 조회
                    if (cardData == null) continue; // 잘못된 카드 항목 제외

                    PieceDefinition baseDefinition = FindDefinition(database, cardData.pieceId); // 원본 카드 정의 조회
                    if (baseDefinition == null) continue; // 정의 누락 카드 제외

                    PieceDefinition restored = RuntimeCardUpgradeService.CreateRestoredCard(
                        baseDefinition,
                        cardData.baseHp,
                        cardData.baseAtk,
                        cardData.displayName); // 저장 강화 스탯 기반 카드 복원

                    if (restored != null) runState.Deck.AddToOwnedPool(restored); // 복원 카드를 보유 풀에 추가
                }

                return; // 최신 카드 스냅샷 복원 완료
            }

            if (data.ownedCardPoolIds == null) return; // 구버전 보유 풀 데이터 없음

            foreach (var pieceId in data.ownedCardPoolIds)
            {
                var definition = FindDefinition(database, pieceId); // 구버전 기물 정의 조회
                if (definition != null) runState.Deck.AddToOwnedPool(definition); // 구버전 보유 풀 복원
            }
        }

        private static RoundProgressStatus ParseRoundStatus(int rawStatus)
        {
            if (rawStatus < (int)RoundProgressStatus.NotStarted || rawStatus > (int)RoundProgressStatus.Failed) return RoundProgressStatus.NotStarted; // 범위 밖 상태 기본값
            return (RoundProgressStatus)rawStatus; // 정상 상태 변환
        }

        private static BattleOutcome ParseBattleOutcome(int rawOutcome)
        {
            if (rawOutcome < (int)BattleOutcome.None || rawOutcome > (int)BattleOutcome.Defeat) return BattleOutcome.None; // 범위 밖 결과 기본값
            return (BattleOutcome)rawOutcome; // 정상 결과 변환
        }

        private static KingArchetype ParseKingArchetype(int rawArchetype)
        {
            if (rawArchetype < (int)KingArchetype.Default || rawArchetype > (int)KingArchetype.Strategy) return KingArchetype.Default; // 범위 밖 킹 기본값
            return (KingArchetype)rawArchetype; // 정상 킹 타입 변환
        }

        private static PieceDefinition FindDefinition(PieceDatabase database, string pieceId)
        {
            if (string.IsNullOrWhiteSpace(pieceId)) return null; // 빈 PieceId 제외

            var fromDatabase = database != null ? database.FindById(pieceId) : null; // PieceDatabase 우선 조회
            if (fromDatabase != null) return fromDatabase; // 등록 기물 반환

            var resourceDefinitions = Resources.LoadAll<PieceDefinition>(string.Empty); // Resources 독립 기물 전체 조회

            for (int i = 0; i < resourceDefinitions.Length; i++)
            {
                var definition = resourceDefinitions[i]; // 현재 정의 조회
                if (definition == null) continue; // 빈 정의 제외
                if (string.Equals(definition.PieceId, pieceId, StringComparison.OrdinalIgnoreCase)) return definition; // 일치 기물 반환
            }

            return null; // 정의 없음 반환
        }

        private static Vector2Int GetSafeFootprint(PieceDefinition definition)
        {
            if (definition == null) return Vector2Int.one; // 정의 누락 기본 1x1
            var size = definition.OccupancySize; // 저장 점유 크기 조회
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y)); // 최소 1x1 보정
        }

        public int CountOwnedCopies(PieceDefinition definition)
        {
            if (definition == null) return 0; // 기준 정의 누락 처리

            int count = 0; // 누적 수 초기화
            foreach (var card in Deck.OwnedCardPool) if (card == definition) count++; // 정상 보유 풀 포함
            foreach (var card in Deck.DeadCardPile) if (card == definition) count++; // 사망 카드 소유권 포함
            return count; // 총 보유 수 반환
        }

        public int CountDeployedCopies(PieceDefinition definition)
        {
            if (definition == null) return 0; // 기준 정의 누락 처리

            var uniquePieces = new HashSet<PieceRuntimeState>(); // 대형 기물 중복 카운트 방지

            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    var occupyingPiece = Board.GetTile(new Vector2Int(x, y)).OccupyingPiece; // 현재 점유 기물 조회
                    if (occupyingPiece == null || !occupyingPiece.IsPlayerPiece) continue; // 빈 칸·적 제외
                    if (occupyingPiece.Definition == definition) uniquePieces.Add(occupyingPiece); // 동일 정의 런타임 한 번만 등록
                }
            }

            return uniquePieces.Count; // 실제 배치 기물 수 반환
        }
    }
}
