using UnityEngine; // Color 사용
using ProjectEta.Run; // StageType 사용

namespace ProjectEta.Board
{
    public enum RouteNodeBuildingKind
    {
        Keep = 0, // 일반 전투 성채
        EliteKeep = 1, // 엘리트 요새
        Treasury = 2, // 보상 보물고
        MerchantHouse = 3, // 상점 건물
        ArcaneTower = 4, // 이벤트 마법 탑
        BossFortress = 5, // 중간 보스 요새
        FinalCitadel = 6 // 최종 보스 성채
    }

    public sealed class RouteNodeBuildingStyle
    {
        public RouteNodeBuildingKind Kind { get; } // 건물 종류
        public float Scale { get; } // 노드 건물 크기
        public bool BossEmphasis { get; } // 보스 강조 여부
        public Color MainColor { get; } // 건물 본체 색상
        public Color AccentColor { get; } // 장식 색상
        public Color RoofColor { get; } // 지붕·상부 색상

        private RouteNodeBuildingStyle(RouteNodeBuildingKind kind, float scale, bool bossEmphasis, Color mainColor, Color accentColor, Color roofColor)
        {
            Kind = kind; // 건물 종류 저장
            Scale = scale; // 건물 크기 저장
            BossEmphasis = bossEmphasis; // 보스 강조 저장
            MainColor = mainColor; // 본체 색상 저장
            AccentColor = accentColor; // 장식 색상 저장
            RoofColor = roofColor; // 지붕 색상 저장
        }

        public static RouteNodeBuildingStyle Resolve(StageType stageType)
        {
            switch (stageType) // 스테이지별 건물 프로필 선택
            {
                case StageType.Elite:
                    return new RouteNodeBuildingStyle(RouteNodeBuildingKind.EliteKeep, 1.18f, false, new Color(0.30f, 0.24f, 0.22f), new Color(0.82f, 0.26f, 0.16f), new Color(0.16f, 0.12f, 0.12f)); // 엘리트 요새 확대 프로필
                case StageType.Reward:
                    return new RouteNodeBuildingStyle(RouteNodeBuildingKind.Treasury, 1.08f, false, new Color(0.46f, 0.36f, 0.18f), new Color(0.95f, 0.76f, 0.18f), new Color(0.24f, 0.18f, 0.10f)); // 보상 보물고 확대 프로필
                case StageType.Shop:
                    return new RouteNodeBuildingStyle(RouteNodeBuildingKind.MerchantHouse, 1.12f, false, new Color(0.34f, 0.24f, 0.16f), new Color(0.22f, 0.58f, 0.92f), new Color(0.16f, 0.26f, 0.38f)); // 상점 건물 확대 프로필
                case StageType.Event:
                    return new RouteNodeBuildingStyle(RouteNodeBuildingKind.ArcaneTower, 1.10f, false, new Color(0.24f, 0.16f, 0.34f), new Color(0.72f, 0.34f, 0.94f), new Color(0.16f, 0.08f, 0.22f)); // 이벤트 탑 확대 프로필
                case StageType.MidBoss:
                    return new RouteNodeBuildingStyle(RouteNodeBuildingKind.BossFortress, 1.38f, true, new Color(0.30f, 0.14f, 0.12f), new Color(0.96f, 0.30f, 0.12f), new Color(0.16f, 0.06f, 0.06f)); // 중간 보스 대형 요새 프로필
                case StageType.FinalBoss:
                    return new RouteNodeBuildingStyle(RouteNodeBuildingKind.FinalCitadel, 1.58f, true, new Color(0.25f, 0.08f, 0.10f), new Color(1.00f, 0.18f, 0.12f), new Color(0.10f, 0.03f, 0.04f)); // 최종 보스 대형 성채 프로필
                default:
                    return new RouteNodeBuildingStyle(RouteNodeBuildingKind.Keep, 1.05f, false, new Color(0.26f, 0.30f, 0.34f), new Color(0.20f, 0.78f, 0.76f), new Color(0.12f, 0.16f, 0.20f)); // 일반 전투 성채 확대 프로필
            }
        }
    }
}
