using ProjectEta.Battle; // 전투 턴·결과 타입 사용

namespace ProjectEta.UI
{
    public enum Day67AnnouncementKind
    {
        BattleStart,
        Deployment,
        PlayerTurn,
        EnemyTurn,
        Passive,
        BossPhase,
        Victory,
        Defeat,
        RunCompleted,
        RunFailed
    }

    public readonly struct Day67BattleAnnouncement
    {
        public Day67AnnouncementKind Kind { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public float HoldSeconds { get; }
        public string Key => $"{(int)Kind}|{Title}|{Subtitle}"; // 인접 중복 알림 식별 키

        public Day67BattleAnnouncement(Day67AnnouncementKind kind, string title, string subtitle, float holdSeconds)
        {
            Kind = kind; // 알림 종류 저장
            Title = title ?? string.Empty; // 안전한 제목 저장
            Subtitle = subtitle ?? string.Empty; // 안전한 보조 문구 저장
            HoldSeconds = holdSeconds < 0.15f ? 0.15f : holdSeconds; // 지나치게 짧은 표시 방지
        }

        public static Day67BattleAnnouncement CreateBattleStart(int round, bool bossBattle)
        {
            string title = bossBattle ? "BOSS BATTLE" : "BATTLE START"; // 보스전 시작 문구 구분
            return new Day67BattleAnnouncement(Day67AnnouncementKind.BattleStart, title, $"STAGE {round}", 0.72f); // 전투 시작 알림 생성
        }

        public static Day67BattleAnnouncement CreateTurn(TurnState state, int turnNumber, bool initialDeployment)
        {
            if (state == TurnState.DeploymentTurn)
            {
                string subtitle = initialDeployment ? "INITIAL DEPLOYMENT" : $"TURN {turnNumber} · DEPLOYMENT"; // 시작·주기 배치 구분
                return new Day67BattleAnnouncement(Day67AnnouncementKind.Deployment, "DEPLOYMENT", subtitle, 0.52f); // 배치 턴 알림 생성
            }

            if (state == TurnState.PlayerTurn)
            {
                return new Day67BattleAnnouncement(Day67AnnouncementKind.PlayerTurn, "PLAYER TURN", $"TURN {turnNumber}", 0.48f); // 플레이어 턴 알림 생성
            }

            if (state == TurnState.EnemyTurn)
            {
                return new Day67BattleAnnouncement(Day67AnnouncementKind.EnemyTurn, "ENEMY TURN", $"TURN {turnNumber}", 0.42f); // 적 턴 알림 생성
            }

            return new Day67BattleAnnouncement(Day67AnnouncementKind.Deployment, string.Empty, string.Empty, 0.15f); // 종료 상태 빈 알림 반환
        }

        public static Day67BattleAnnouncement CreateBattleResult(BattleOutcome outcome)
        {
            if (outcome == BattleOutcome.Victory)
            {
                return new Day67BattleAnnouncement(Day67AnnouncementKind.Victory, "VICTORY", "BATTLE CLEARED", 0.86f); // 전투 승리 알림 생성
            }

            return new Day67BattleAnnouncement(Day67AnnouncementKind.Defeat, "DEFEAT", "KING FALLEN", 0.92f); // 전투 패배 알림 생성
        }

        public static Day67BattleAnnouncement CreatePassive(string detail)
        {
            return new Day67BattleAnnouncement(Day67AnnouncementKind.Passive, "PASSIVE", detail, 0.56f); // 킹 패시브 발동 알림 생성
        }

        public static Day67BattleAnnouncement CreateBossPhase2()
        {
            return new Day67BattleAnnouncement(Day67AnnouncementKind.BossPhase, "WARNING", "BOSS PHASE II", 0.92f); // 보스 2페이즈 경고 생성
        }

        public static Day67BattleAnnouncement CreateRunResult(bool completed)
        {
            return completed
                ? new Day67BattleAnnouncement(Day67AnnouncementKind.RunCompleted, "RUN COMPLETED", "THE RUN IS CLEARED", 0.92f) // 런 완료 알림 생성
                : new Day67BattleAnnouncement(Day67AnnouncementKind.RunFailed, "RUN FAILED", "THE RUN HAS ENDED", 0.92f); // 런 실패 알림 생성
        }
    }
}
