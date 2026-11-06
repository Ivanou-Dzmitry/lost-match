# Lost Match: level difficulty analysis

Source: the 100 level assets in `Assets/ScriptableObjects` (not the spreadsheet). Spreadsheet `TOTAL` is shown for comparison.

## How the pressure is counted

`pressure` = estimated moves needed for the goals / moves available. 1.0 means the goals need about all the moves. Rough assumptions:

- element goal: `need x colors / 4.5` moves (goals of different colours are collected in parallel);
- obstacle goal (blocker, breakable, locker): `need x average hit points / 2` moves (about 2 hits per move);
- bomb goals: line bomb 2 moves, wrap bomb 3, colour bomb 4 per bomb.

It is a relative scale for comparing levels with each other. Real values need play tests.

Rating: Easy < 0.55, Normal 0.55-0.80, Hard 0.80-1.00, Very hard > 1.00.

## Worlds

| World | Levels | Avg pressure | Min | Max | Avg moves | Avg colours | Avg cells | Avg obstacle HP | Score per move (3 stars) |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 1-10 | 0.96 | 0.40 | 1.67 | 22.6 | 3.8 | 40 | 28 | 6.6 |
| 2 | 11-20 | 1.10 | 0.40 | 2.30 | 21.5 | 3.9 | 48 | 33 | 13.8 |
| 3 | 21-30 | 1.05 | 0.50 | 2.53 | 20.6 | 4.0 | 45 | 34 | 10.8 |
| 4 | 31-40 | 0.95 | 0.48 | 1.46 | 20.2 | 4.1 | 51 | 44 | 9.5 |
| 5 | 41-50 | 0.98 | 0.50 | 1.36 | 22.5 | 3.9 | 39 | 45 | 8.8 |
| 6 | 51-60 | 1.06 | 0.44 | 1.88 | 21.2 | 4.4 | 51 | 33 | 8.3 |
| 7 | 61-70 | 0.79 | 0.43 | 1.45 | 19.8 | 4.2 | 43 | 27 | 8.2 |
| 8 | 71-80 | 0.83 | 0.36 | 1.90 | 20.5 | 4.6 | 64 | 40 | 9.2 |
| 9 | 81-90 | 0.79 | 0.08 | 1.54 | 23.5 | 4.3 | 40 | 26 | 8.1 |
| 10 | 91-100 | 0.95 | 0.10 | 1.72 | 23.7 | 4.4 | 57 | 46 | 9.7 |

Ratings: Easy 25, Normal 18, Hard 17, Very hard 40.

Difficulty tag in the asset vs real pressure:

| Tag | Levels | Avg pressure |
|---|---|---|
| Simple | 57 | 0.87 |
| Medium | 25 | 1.07 |
| Hard | 18 | 1.01 |

## All levels

| # | Size | Cells | Colours | Moves | Goals | Obstacles (HP) | Preload | Stars | Tag | Pressure | Rating | Sheet TOTAL |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 5x5 | 25 | 3 | 14 | e03x15 | 0 (0) | 4 | 30/45/55 | Simple | 0.71 | Normal | 17 |
| 2 | 6x8 | 48 | 4 | 28 | blocker_01x26 | 26 (26) | 0 | 60/80/100 | Simple | 0.46 | Easy | 49 |
| 3 | 7x6 | 30 | 4 | 22 | LineBombx6 | 0 (0) | 0 | 53/63/83 | Simple | 0.55 | Normal | 36 |
| 4 | 8x7 | 42 | 3 | 30 | blocker_01x24 | 24 (24) | 0 | 40/70/90 | Simple | 0.40 | Easy | 58 |
| 5 | 7x8 | 28 | 3 | 15 | blocker_01x24, WrapBombx3 | 24 (24) | 6 | 90/125/150 | Medium | 1.40 | Very hard | 68 |
| 6 | 8x8 | 39 | 4 | 28 | blocker_01x34, WrapBombx6 | 34 (34) | 10 | 100/250/350 | Hard | 1.25 | Very hard | 73 |
| 7 | 7x7 | 49 | 5 | 24 | e01x36 | 28 (28) | 7 | 75/100/150 | Hard | 1.67 | Very hard | 62 |
| 8 | 7x7 | 29 | 4 | 13 | breakable_01x21 | 21 (21) | 0 | 50/80/100 | Simple | 0.81 | Hard | 56 |
| 9 | 8x10 | 72 | 4 | 30 | breakable_01x30, blocker_01x14 | 44 (70) | 0 | 150/220/300 | Medium | 1.17 | Very hard | 52 |
| 10 | 8x9 | 39 | 4 | 22 | blocker_01x38 | 38 (52) | 4 | 60/90/110 | Medium | 1.18 | Very hard | 62 |
| 11 | 6x7 | 38 | 4 | 15 | locker_01x12 | 12 (12) | 0 | 75/125/150 | Simple | 0.40 | Easy | 52 |
| 12 | 7x8 | 56 | 4 | 19 | blocker_01x24 | 24 (32) | 0 | 50/150/200 | Simple | 0.84 | Hard | 53 |
| 13 | 6x9 | 50 | 4 | 18 | locker_01x22 | 26 (26) | 0 | 100/150/300 | Simple | 0.61 | Normal | 55 |
| 14 | 7x8 | 44 | 5 | 18 | breakable_01x10, e02x30 | 23 (31) | 7 | 50/125/170 | Medium | 2.30 | Very hard | 61 |
| 15 | 8x8 | 48 | 3 | 25 | blocker_01x36, LineBombx4 | 36 (76) | 0 | 200/350/450 | Hard | 1.84 | Very hard | 72 |
| 16 | 7x9 | 39 | 3 | 30 | blocker_01x14, LineBombx7 | 14 (26) | 20 | 100/350/500 | Hard | 0.90 | Hard | 72 |
| 17 | 8x9 | 32 | 4 | 25 | breakable_01x20 | 20 (24) | 0 | 80/150/175 | Hard | 0.48 | Easy | 42 |
| 18 | 8x10 | 76 | 4 | 20 | breakable_01x12, blocker_01x26 | 38 (50) | 0 | 100/300/400 | Simple | 1.25 | Very hard | 56 |
| 19 | 8x10 | 36 | 3 | 25 | blocker_01x18, LineBombx14 | 18 (18) | 0 | 200/400/500 | Simple | 1.48 | Very hard | 57 |
| 20 | 7x9 | 63 | 5 | 20 | breakable_01x9, locker_01x16 | 25 (35) | 0 | 50/150/200 | Medium | 0.88 | Hard | 60 |
| 21 | 8x9 | 36 | 4 | 15 | e04x25 | 12 (12) | 0 | 75/125/150 | Simple | 1.48 | Very hard | 45 |
| 22 | 7x10 | 38 | 4 | 22 | blocker_01x17, locker_01x9 | 26 (35) | 5 | 100/150/200 | Simple | 0.80 | Hard | 53 |
| 23 | 8x10 | 48 | 4 | 18 | blocker_01x10, breakable_01x18 | 28 (36) | 5 | 125/180/210 | Simple | 1.00 | Very hard | 48 |
| 24 | 8x9 | 30 | 3 | 15 | blocker_01x18, e05x30 | 18 (36) | 0 | 100/170/200 | Simple | 2.53 | Very hard | 55 |
| 25 | 8x9 | 36 | 3 | 18 | blocker_01x26, LineBombx5 | 26 (30) | 0 | 125/175/250 | Hard | 1.39 | Very hard | 71 |
| 26 | 8x10 | 56 | 4 | 28 | blocker_01x16, locker_01x8 | 24 (28) | 0 | 75/150/220 | Hard | 0.50 | Easy | 48 |
| 27 | 6x8 | 48 | 5 | 15 | blocker_01x12 | 20 (26) | 4 | 30/40/80 | Simple | 0.60 | Normal | 55 |
| 28 | 8x8 | 46 | 4 | 22 | blocker_01x10, LineBombx5 | 19 (28) | 0 | 100/150/225 | Simple | 0.68 | Normal | 56 |
| 29 | 8x10 | 60 | 4 | 30 | breakable_01x18 | 32 (60) | 0 | 100/200/400 | Medium | 0.57 | Normal | 63 |
| 30 | 8x10 | 52 | 5 | 23 | blocker_01x6, breakable_01x22 | 28 (44) | 9 | 100/150/300 | Simple | 0.96 | Hard | 57 |
| 31 | 5x10 | 50 | 5 | 14 | e04x18, e05x15 | 29 (32) | 0 | 20/80/100 | Medium | 1.43 | Very hard | 69 |
| 32 | 6x6 | 36 | 3 | 18 | WrapBombx5 | 12 (12) | 0 | 40/80/100 | Simple | 0.83 | Hard | 47 |
| 33 | 7x7 | 49 | 3 | 16 | ColorBombx2 | 24 (24) | 0 | 100/200/250 | Simple | 0.50 | Easy | 42 |
| 34 | 8x10 | 80 | 5 | 30 | blocker_01x18, locker_01x40 | 58 (58) | 0 | 100/200/300 | Hard | 0.97 | Hard | 74 |
| 35 | 7x9 | 39 | 4 | 18 | e05x10, locker_01x15 | 41 (51) | 0 | 60/90/120 | Simple | 1.10 | Very hard | 56 |
| 36 | 8x10 | 52 | 4 | 23 | blocker_01x14, WrapBombx3 | 22 (25) | 0 | 100/150/250 | Simple | 0.76 | Normal | 56 |
| 37 | 8x8 | 64 | 4 | 21 | LineBombx5 | 40 (72) | 0 | 50/80/110 | Simple | 0.48 | Easy | 55 |
| 38 | 8x10 | 24 | 4 | 23 | breakable_01x26 | 34 (46) | 0 | 100/170/250 | Simple | 0.83 | Hard | 56 |
| 39 | 8x9 | 72 | 5 | 15 | e02x15, e01x15 | 32 (52) | 0 | 40/80/100 | Hard | 1.11 | Very hard | 72 |
| 40 | 8x10 | 40 | 4 | 24 | WrapBombx5, LineBombx10 | 24 (72) | 0 | 100/250/400 | Hard | 1.46 | Very hard | 81 |
| 41 | 8x10 | 64 | 4 | 23 | locker_01x36 | 46 (46) | 0 | 150/200/250 | Medium | 0.78 | Normal | 63 |
| 42 | 8x10 | 40 | 4 | 21 | blocker_01x24, locker_01x4 | 52 (76) | 0 | 140/170/200 | Hard | 1.24 | Very hard | 79 |
| 43 | 7x10 | 34 | 5 | 24 | LineBombx10 | 22 (56) | 0 | 110/160/220 | Simple | 0.83 | Hard | 56 |
| 44 | 8x9 | 30 | 4 | 20 | blocker_01x22 | 22 (42) | 0 | 60/90/120 | Simple | 1.05 | Very hard | 54 |
| 45 | 7x9 | 9 | 3 | 15 | LineBombx10 | 6 (6) | 0 | 150/200/265 | Simple | 1.33 | Very hard | 53 |
| 46 | 8x8 | 48 | 4 | 24 | blocker_01x36 | 36 (54) | 0 | 75/90/135 | Medium | 1.12 | Very hard | 67 |
| 47 | 7x10 | 46 | 4 | 25 | locker_01x22, breakable_01x21 | 43 (68) | 0 | 33/66/100 | Hard | 1.36 | Very hard | 76 |
| 48 | 7x7 | 31 | 3 | 20 | blocker_01x20 | 20 (26) | 0 | 60/90/120 | Simple | 0.65 | Normal | 53 |
| 49 | 6x10 | 36 | 4 | 28 | breakable_01x16, locker_01x6 | 24 (34) | 0 | 100/200/300 | Simple | 0.50 | Easy | 56 |
| 50 | 8x10 | 49 | 4 | 25 | blocker_01x20, locker_01x10 | 30 (45) | 0 | 100/150/200 | Medium | 0.90 | Hard | 62 |
| 51 | 5x10 | 50 | 4 | 23 | locker_01x32 | 32 (68) | 0 | 100/200/300 | Hard | 1.48 | Very hard | 93 |
| 52 | 8x10 | 32 | 4 | 17 | blocker_01x24 | 24 (29) | 0 | 40/100/150 | Simple | 0.85 | Hard | 45 |
| 53 | 7x9 | 43 | 5 | 25 | blocker_01x24 | 24 (39) | 0 | 80/100/160 | Simple | 0.78 | Normal | 48 |
| 54 | 8x7 | 56 | 5 | 18 | e05x18, e02x18 | 8 (8) | 0 | 50/75/100 | Medium | 1.11 | Very hard | 55 |
| 55 | 8x10 | 58 | 4 | 15 | e03x25, locker_01x12 | 12 (12) | 0 | 50/80/110 | Medium | 1.88 | Very hard | 57 |
| 56 | 7x9 | 53 | 4 | 14 | e04x28 | 22 (22) | 0 | 60/100/150 | Simple | 1.78 | Very hard | 56 |
| 57 | 8x10 | 48 | 4 | 21 | blocker_01x32 | 32 (32) | 0 | 60/90/160 | Medium | 0.76 | Normal | 58 |
| 58 | 7x8 | 32 | 4 | 25 | breakable_01x22 | 22 (22) | 0 | 70/100/130 | Simple | 0.44 | Easy | 42 |
| 59 | 7x10 | 70 | 5 | 26 | WrapBombx4 | 19 (42) | 0 | 80/160/250 | Simple | 0.46 | Easy | 58 |
| 60 | 8x8 | 64 | 5 | 28 | locker_01x15, blocker_01x24 | 39 (58) | 0 | 90/150/250 | Medium | 1.04 | Very hard | 48 |
| 61 | 7x10 | 42 | 4 | 14 | locker_01x12 | 22 (22) | 0 | 40/80/120 | Simple | 0.43 | Easy | 53 |
| 62 | 8x10 | 40 | 4 | 25 | LineBombx6, blocker_01x14 | 14 (14) | 0 | 100/150/220 | Simple | 0.76 | Normal | 58 |
| 63 | 8x10 | 48 | 5 | 18 | e05x21 | 18 (26) | 0 | 60/80/120 | Simple | 1.30 | Very hard | 51 |
| 64 | 7x9 | 43 | 5 | 17 | locker_01x9 | 9 (15) | 0 | 40/70/100 | Simple | 0.44 | Easy | 54 |
| 65 | 8x10 | 36 | 4 | 20 | blocker_01x30 | 30 (58) | 0 | 90/160/200 | Simple | 1.45 | Very hard | 67 |
| 66 | 8x10 | 52 | 4 | 19 | breakable_01x18 | 18 (26) | 0 | 100/150/250 | Simple | 0.68 | Normal | 50 |
| 67 | 8x10 | 48 | 5 | 23 | locker_01x16 | 16 (20) | 0 | 90/140/180 | Simple | 0.43 | Easy | 52 |
| 68 | 8x10 | 36 | 3 | 20 | blocker_01x10, LineBombx8 | 26 (28) | 0 | 100/140/170 | Simple | 1.10 | Very hard | 56 |
| 69 | 8x9 | 48 | 5 | 24 | breakable_01x26 | 26 (34) | 0 | 80/120/150 | Simple | 0.71 | Normal | 43 |
| 70 | 7x9 | 41 | 3 | 18 | locker_01x10, breakable_01x10 | 28 (28) | 0 | 60/80/120 | Medium | 0.56 | Normal | 52 |
| 71 | 7x10 | 70 | 5 | 15 | locker_01x15 | 35 (39) | 0 | 50/110/150 | Simple | 0.50 | Easy | 54 |
| 72 | 6x9 | 50 | 4 | 14 | e02x15, e01x30 | 30 (42) | 0 | 100/125/150 | Simple | 1.90 | Very hard | 66 |
| 73 | 7x10 | 70 | 5 | 17 | blocker_01x10 | 30 (40) | 0 | 30/50/100 | Medium | 0.47 | Easy | 49 |
| 74 | 8x10 | 68 | 5 | 28 | locker_01x8, e02x24 | 24 (40) | 0 | 60/120/200 | Simple | 1.24 | Very hard | 54 |
| 75 | 8x10 | 80 | 5 | 19 | e03x25, e05x25 | 24 (36) | 0 | 75/100/150 | Simple | 1.46 | Very hard | 65 |
| 76 | 7x9 | 59 | 4 | 25 | blocker_01x18 | 36 (54) | 0 | 100/150/200 | Hard | 0.36 | Easy | 58 |
| 77 | 8x10 | 80 | 5 | 20 | locker_01x24 | 24 (32) | 0 | 100/150/250 | Medium | 0.80 | Hard | 52 |
| 78 | 8x10 | 56 | 5 | 17 | locker_01x12 | 24 (28) | 0 | 90/140/200 | Simple | 0.47 | Easy | 55 |
| 79 | 7x9 | 63 | 4 | 25 | breakable_01x21 | 39 (51) | 0 | 100/150/200 | Simple | 0.42 | Easy | 50 |
| 80 | 8x10 | 48 | 4 | 25 | blocker_01x26, breakable_01x8 | 34 (34) | 0 | 150/220/260 | Medium | 0.68 | Normal | 56 |
| 81 | 8x10 | 38 | 4 | 21 | blocker_01x23, LineBombx7 | 23 (34) | 0 | 100/130/170 | Medium | 1.48 | Very hard | 69 |
| 82 | 8x10 | 36 | 4 | 18 | breakable_01x22 | 30 (36) | 0 | 100/150/250 | Simple | 0.78 | Normal | 60 |
| 83 | 8x9 | 40 | 4 | 18 | blocker_01x18, breakable_01x6 | 24 (32) | 0 | 100/150/200 | Medium | 0.89 | Hard | 68 |
| 84 | 8x10 | 38 | 4 | 24 | locker_01x13, breakable_01x12 | 25 (25) | 0 | 100/150/200 | Hard | 0.52 | Easy | 60 |
| 85 | 7x9 | 31 | 4 | 25 | blocker_01x4 | 10 (18) | 0 | 100/150/200 | Hard | 0.24 | Easy | 47 |
| 86 | 8x10 | 40 | 5 | 26 | locker_01x8, e05x16 | 22 (22) | 0 | 80/120/150 | Medium | 0.84 | Hard | 52 |
| 87 | 8x10 | 40 | 4 | 25 | blocker_01x14, e01x32 | 20 (26) | 0 | 70/100/150 | Medium | 1.54 | Very hard | 63 |
| 88 | 8x10 | 52 | 5 | 30 | blocker_01x23 | 25 (33) | 0 | 110/140/180 | Simple | 0.52 | Easy | 55 |
| 89 | 8x10 | 48 | 5 | 23 | e02x20, e01x20 | 16 (18) | 0 | 90/115/130 | Simple | 0.97 | Hard | 65 |
| 90 | 7x10 | 42 | 4 | 25 | locker_01x4 | 16 (20) | 0 | 100/150/200 | Hard | 0.08 | Easy | 37 |
| 91 | 8x10 | 72 | 5 | 22 | WrapBombx5, blocker_01x20 | 44 (48) | 0 | 90/130/180 | Simple | 1.23 | Very hard | 17 (no data) |
| 92 | 8x9 | 64 | 5 | 19 | locker_01x4, e02x24 | 16 (26) | 0 | 100/180/250 | Simple | 1.72 | Very hard | 19 (no data) |
| 93 | 7x10 | 42 | 4 | 28 | blocker_01x26 | 26 (46) | 0 | 100/200/250 | Simple | 0.82 | Hard | 8 (no data) |
| 94 | 8x10 | 48 | 5 | 17 | blocker_01x34 | 34 (43) | 3 | 80/120/150 | Hard | 1.26 | Very hard | 16 (no data) |
| 95 | 8x10 | 72 | 5 | 20 | breakable_01x4 | 40 (52) | 0 | 100/150/200 | Simple | 0.10 | Easy | 14 (no data) |
| 96 | 7x9 | 39 | 4 | 30 | breakable_01x35 | 35 (81) | 0 | 100/150/250 | Medium | 1.35 | Very hard | 8 (no data) |
| 97 | 7x9 | 47 | 4 | 25 | locker_01x8 | 14 (30) | 0 | 150/210/250 | Medium | 0.48 | Easy | 10 (no data) |
| 98 | 8x10 | 76 | 4 | 28 | blocker_01x12, locker_01x16 | 28 (70) | 0 | 200/250/300 | Medium | 1.25 | Very hard | 10 (no data) |
| 99 | 8x10 | 58 | 4 | 23 | blocker_01x24 | 27 (39) | 0 | 150/250/300 | Simple | 0.78 | Normal | 8 (no data) |
| 100 | 7x10 | 50 | 4 | 25 | locker_01x9, blocker_01x6 | 15 (24) | 0 | 70/110/150 | Simple | 0.48 | Easy | 12 (no data) |
