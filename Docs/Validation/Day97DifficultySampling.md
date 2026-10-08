---
# 97일차 난이도 곡선 자동 표본

각 페이즈와 전투 종류마다 Seed 500개를 연속 생성해 해금 원형, 직전 원형 반복, 적 수와 위협도를 검사했다.
자동 위협도 표본이며 실제 승률·종료 턴·왕 HP 손실은 F1 런 기록으로 별도 확인한다.

| 페이즈 | 종류 | 해금 원형 | 표본 | 평균 적 수 | 평균 위협도 | 연속 반복 | 원형 분포 |
|---:|---|---:|---:|---:|---:|---:|---|
| 1 | Battle | 2 | 500 | 4.00 | 36.73 | 0 | normal_frontline=250, normal_skirmish=250 |
| 1 | Elite | 1 | 500 | 5.50 | 58.50 | 0 | elite_vanguard=500 |
| 2 | Battle | 4 | 500 | 4.00 | 40.85 | 0 | normal_frontline=126, normal_guard=114, normal_ranged=134, normal_skirmish=126 |
| 2 | Elite | 2 | 500 | 5.50 | 68.55 | 0 | elite_hunters=250, elite_vanguard=250 |
| 3 | Battle | 5 | 500 | 4.21 | 46.98 | 0 | normal_frontline=99, normal_guard=107, normal_mixed=106, normal_ranged=92, normal_skirmish=96 |
| 3 | Elite | 3 | 500 | 6.00 | 86.65 | 0 | elite_fortress=168, elite_hunters=164, elite_vanguard=168 |
| 4 | Battle | 5 | 500 | 4.22 | 52.04 | 0 | normal_frontline=96, normal_guard=103, normal_mixed=108, normal_ranged=100, normal_skirmish=93 |
| 4 | Elite | 3 | 500 | 6.00 | 95.10 | 0 | elite_fortress=157, elite_hunters=165, elite_vanguard=178 |
| 5 | Battle | 5 | 500 | 4.19 | 54.31 | 0 | normal_frontline=105, normal_guard=88, normal_mixed=96, normal_ranged=109, normal_skirmish=102 |
| 5 | Elite | 3 | 500 | 6.00 | 95.93 | 0 | elite_fortress=171, elite_hunters=157, elite_vanguard=172 |

| 보스 | 시작 적 | 라운드 증원 | 증원 턴 | 제한 턴 |
|---|---:|---:|---|---:|
| 중간 보스 | 3 | 2 | 4, 6 | 30 |
| 최종 보스 | 4 | 3 | 3, 5, 7 | 30 |

해금 원형이 둘 이상인 구간은 같은 원형이 연속하지 않았다. 최종 보스는 중간 보스보다 시작 적과 증원 수가 많고 증원이 더 이르게 시작한다.
실제 플레이 3런 이상에서 원형별 승률·평균 턴·왕 HP 손실을 수집한 뒤 수치를 확정한다.
