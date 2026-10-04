using System; // 문자열 비교 사용
using System.Collections.Generic; // IReadOnlyList와 Dictionary 사용

namespace ProjectEta.Pieces
{
    public sealed class PieceRosterEntry
    {
        public string PieceId { get; } // 고정 기물 ID
        public string DisplayName { get; } // 기획 기준 표시 이름
        public PieceGrade Grade { get; } // 목표 등급
        public PieceCategory Category { get; } // 목표 분류
        public PieceRoleTag RoleTags { get; } // 목표 역할 태그

        internal PieceRosterEntry(string pieceId, string displayName, PieceGrade grade, PieceCategory category, PieceRoleTag roleTags)
        {
            PieceId = pieceId; // ID 저장
            DisplayName = displayName; // 이름 저장
            Grade = grade; // 등급 저장
            Category = category; // 분류 저장
            RoleTags = roleTags; // 역할 저장
        }
    }

    public static class PieceRosterCatalog
    {
        public const int TargetPieceCount = 81; // V1.0 목표 플레이어 기물 수

        private static readonly PieceRosterEntry[] EntriesInternal =
        {
            new PieceRosterEntry("king", "킹", PieceGrade.OneStar, PieceCategory.Special, PieceRoleTag.None),
            new PieceRosterEntry("pawn", "폰", PieceGrade.OneStar, PieceCategory.Basic, PieceRoleTag.Melee),
            new PieceRosterEntry("wazir", "와지르", PieceGrade.OneStar, PieceCategory.Basic, PieceRoleTag.Melee),
            new PieceRosterEntry("ferz", "페르즈", PieceGrade.OneStar, PieceCategory.Basic, PieceRoleTag.Melee),
            new PieceRosterEntry("dabbaba", "다바바", PieceGrade.OneStar, PieceCategory.Basic, PieceRoleTag.Jumper),
            new PieceRosterEntry("alfil", "알필", PieceGrade.OneStar, PieceCategory.Basic, PieceRoleTag.Jumper),
            new PieceRosterEntry("knight", "나이트", PieceGrade.OneStar, PieceCategory.Basic, PieceRoleTag.Jumper),
            new PieceRosterEntry("bishop", "비숍", PieceGrade.OneStar, PieceCategory.Basic, PieceRoleTag.Slider),
            new PieceRosterEntry("rook", "룩", PieceGrade.OneStar, PieceCategory.Basic, PieceRoleTag.Slider),
            new PieceRosterEntry("camel", "카멜", PieceGrade.OneStar, PieceCategory.Basic, PieceRoleTag.Jumper),
            new PieceRosterEntry("zebra", "제브라", PieceGrade.OneStar, PieceCategory.Basic, PieceRoleTag.Jumper),
            new PieceRosterEntry("queen", "퀸", PieceGrade.OneStar, PieceCategory.Basic, PieceRoleTag.Slider),
            new PieceRosterEntry("spearman", "창병", PieceGrade.OneStar, PieceCategory.Special, PieceRoleTag.Ranged | PieceRoleTag.Attacker),
            new PieceRosterEntry("shooter", "사수", PieceGrade.OneStar, PieceCategory.Special, PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("shield_guard", "방패병", PieceGrade.OneStar, PieceCategory.Special, PieceRoleTag.Melee | PieceRoleTag.Tanker),
            new PieceRosterEntry("flag_bearer", "깃발병", PieceGrade.OneStar, PieceCategory.Special, PieceRoleTag.Melee | PieceRoleTag.Support),
            new PieceRosterEntry("pursuer", "추격병", PieceGrade.OneStar, PieceCategory.Special, PieceRoleTag.Melee | PieceRoleTag.Attacker),
            new PieceRosterEntry("scout", "척후병", PieceGrade.OneStar, PieceCategory.Special, PieceRoleTag.Melee | PieceRoleTag.Attacker),
            new PieceRosterEntry("mann", "맨", PieceGrade.TwoStar, PieceCategory.Fusion, PieceRoleTag.Melee),
            new PieceRosterEntry("waffle", "와플", PieceGrade.TwoStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Jumper),
            new PieceRosterEntry("centaur", "센타우르", PieceGrade.TwoStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Jumper),
            new PieceRosterEntry("cannon", "캐논", PieceGrade.TwoStar, PieceCategory.Special, PieceRoleTag.Ranged | PieceRoleTag.Attacker),
            new PieceRosterEntry("grasshopper", "그래스호퍼", PieceGrade.TwoStar, PieceCategory.Special, PieceRoleTag.Jumper | PieceRoleTag.Attacker),
            new PieceRosterEntry("nightrider", "나이트라이더", PieceGrade.TwoStar, PieceCategory.Special, PieceRoleTag.Jumper | PieceRoleTag.Rider),
            new PieceRosterEntry("archbishop", "아크비숍", PieceGrade.TwoStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Slider),
            new PieceRosterEntry("chancellor", "챈슬러", PieceGrade.TwoStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Slider),
            new PieceRosterEntry("camelrider", "카멜라이더", PieceGrade.TwoStar, PieceCategory.Special, PieceRoleTag.Jumper | PieceRoleTag.Rider),
            new PieceRosterEntry("canvasser", "캔버서", PieceGrade.TwoStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Slider),
            new PieceRosterEntry("caliph", "칼리프", PieceGrade.TwoStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Slider),
            new PieceRosterEntry("squirrel", "스쿼럴", PieceGrade.TwoStar, PieceCategory.Fusion, PieceRoleTag.Jumper),
            new PieceRosterEntry("chameleon", "카멜레온", PieceGrade.TwoStar, PieceCategory.Special, PieceRoleTag.Attacker),
            new PieceRosterEntry("amazon", "아마존", PieceGrade.TwoStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Slider),
            new PieceRosterEntry("assault_trooper", "강습병", PieceGrade.TwoStar, PieceCategory.Special, PieceRoleTag.Jumper | PieceRoleTag.Attacker),
            new PieceRosterEntry("sentry", "파수병", PieceGrade.TwoStar, PieceCategory.Special, PieceRoleTag.Slider | PieceRoleTag.Tanker),
            new PieceRosterEntry("breaker", "돌파병", PieceGrade.TwoStar, PieceCategory.Special, PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("courier", "전령", PieceGrade.TwoStar, PieceCategory.Special, PieceRoleTag.Jumper),
            new PieceRosterEntry("ambusher", "매복병", PieceGrade.TwoStar, PieceCategory.Special, PieceRoleTag.Jumper | PieceRoleTag.Attacker),
            new PieceRosterEntry("paladin", "성기사", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Slider | PieceRoleTag.Tanker),
            new PieceRosterEntry("war_chariot", "전차", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("grenadier", "척탄병", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Ranged | PieceRoleTag.Attacker),
            new PieceRosterEntry("pikeman", "장창병", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Ranged | PieceRoleTag.Attacker),
            new PieceRosterEntry("crossbowman", "석궁병", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Ranged | PieceRoleTag.Attacker),
            new PieceRosterEntry("guardian", "수호기사", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Support | PieceRoleTag.Tanker),
            new PieceRosterEntry("hunter", "사냥꾼", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Attacker),
            new PieceRosterEntry("falcon", "매", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Attacker),
            new PieceRosterEntry("unicorn", "유니콘", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Slider | PieceRoleTag.Rider),
            new PieceRosterEntry("gryphon", "그리폰", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("dragon_horse", "용마", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Slider),
            new PieceRosterEntry("dragon_king", "용왕", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Slider),
            new PieceRosterEntry("artillery", "포병대", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Ranged | PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("vanguard", "돌격대장", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Attacker),
            new PieceRosterEntry("tactician", "전술가", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Support),
            new PieceRosterEntry("medic", "의무병", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Support),
            new PieceRosterEntry("summoner", "소환사", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Support | PieceRoleTag.Summoner),
            new PieceRosterEntry("sniper", "저격수", PieceGrade.ThreeStar, PieceCategory.Fusion, PieceRoleTag.Ranged | PieceRoleTag.Attacker),
            new PieceRosterEntry("marshal", "대장군", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Slider | PieceRoleTag.Support | PieceRoleTag.Attacker),
            new PieceRosterEntry("grand_cannon", "대포병", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Ranged | PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("imperial_knight", "황실기사", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Attacker),
            new PieceRosterEntry("high_priest", "대성직자", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Slider | PieceRoleTag.Support),
            new PieceRosterEntry("war_rider", "전쟁기수", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Rider | PieceRoleTag.Attacker),
            new PieceRosterEntry("siege_chariot", "공성전차", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("grand_guardian", "수호대장", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Tanker),
            new PieceRosterEntry("grand_unicorn", "그랜드 유니콘", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Slider | PieceRoleTag.Rider),
            new PieceRosterEntry("grand_gryphon", "그랜드 그리폰", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("archmage", "대마도사", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("war_cleric", "전투치유사", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Slider | PieceRoleTag.Support),
            new PieceRosterEntry("executioner", "처형자", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("storm_knight", "폭풍기사", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Attacker),
            new PieceRosterEntry("bastion", "철벽기사", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Tanker),
            new PieceRosterEntry("field_commander", "전장지휘관", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Support),
            new PieceRosterEntry("illusionist", "환술사", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("deadeye", "대저격수", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Ranged | PieceRoleTag.Attacker),
            new PieceRosterEntry("gatekeeper", "문지기", PieceGrade.FourStar, PieceCategory.Fusion, PieceRoleTag.Melee | PieceRoleTag.Tanker),
            new PieceRosterEntry("siege_commander", "공성대장", PieceGrade.FiveStar, PieceCategory.Fusion, PieceRoleTag.Ranged | PieceRoleTag.Jumper | PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("grand_paladin", "대성기사", PieceGrade.FiveStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Slider | PieceRoleTag.Support | PieceRoleTag.Tanker),
            new PieceRosterEntry("grand_rider", "기마장군", PieceGrade.FiveStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Rider | PieceRoleTag.Attacker),
            new PieceRosterEntry("phantom_general", "환영장군", PieceGrade.FiveStar, PieceCategory.Special, PieceRoleTag.Ranged | PieceRoleTag.Jumper | PieceRoleTag.Slider | PieceRoleTag.Attacker),
            new PieceRosterEntry("emperor", "황제", PieceGrade.FiveStar, PieceCategory.Fusion, PieceRoleTag.Slider | PieceRoleTag.Support | PieceRoleTag.Attacker),
            new PieceRosterEntry("sky_marshal", "천공대장", PieceGrade.FiveStar, PieceCategory.Fusion, PieceRoleTag.Jumper | PieceRoleTag.Rider | PieceRoleTag.Attacker),
            new PieceRosterEntry("grand_sage", "대현자", PieceGrade.FiveStar, PieceCategory.Fusion, PieceRoleTag.Slider | PieceRoleTag.Support),
            new PieceRosterEntry("iron_regent", "철벽군주", PieceGrade.FiveStar, PieceCategory.Fusion, PieceRoleTag.Slider | PieceRoleTag.Support | PieceRoleTag.Tanker),
        };

        private static readonly Dictionary<string, PieceRosterEntry> EntryById = BuildLookup(); // ID 조회표 생성

        public static IReadOnlyList<PieceRosterEntry> Entries => EntriesInternal; // 전체 목표 로스터 공개

        public static PieceRosterEntry FindById(string pieceId)
        {
            if (string.IsNullOrWhiteSpace(pieceId)) return null; // 빈 ID 차단
            EntryById.TryGetValue(pieceId, out PieceRosterEntry entry); // 목표 항목 조회
            return entry; // 조회 결과 반환
        }

        public static int GetTargetCount(PieceGrade grade)
        {
            switch (grade)
            {
                case PieceGrade.OneStar: return 18; // 1성 목표 수
                case PieceGrade.TwoStar: return 19; // 2성 목표 수
                case PieceGrade.ThreeStar: return 18; // 3성 목표 수
                case PieceGrade.FourStar: return 18; // 4성 목표 수
                case PieceGrade.FiveStar: return 8; // 5성 목표 수
                default: return 0; // 유효하지 않은 등급
            }
        }

        private static Dictionary<string, PieceRosterEntry> BuildLookup()
        {
            var lookup = new Dictionary<string, PieceRosterEntry>(StringComparer.Ordinal); // 대소문자 구분 ID 표

            for (int index = 0; index < EntriesInternal.Length; index++)
            {
                PieceRosterEntry entry = EntriesInternal[index]; // 현재 목표 항목 조회

                if (entry == null || string.IsNullOrWhiteSpace(entry.PieceId)) continue; // 잘못된 항목 제외
                lookup[entry.PieceId] = entry; // ID 기준 등록
            }

            return lookup; // 완성 조회표 반환
        }
    }
}
