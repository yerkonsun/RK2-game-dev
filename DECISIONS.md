{\rtf1\ansi\ansicpg1251\cocoartf2870
\cocoatextscaling0\cocoaplatform0{\fonttbl\f0\fswiss\fcharset0 Helvetica;}
{\colortbl;\red255\green255\blue255;}
{\*\expandedcolortbl;;}
\paperw11900\paperh16840\margl1440\margr1440\vieww11520\viewh8400\viewkind0
\pard\tx720\tx1440\tx2160\tx2880\tx3600\tx4320\tx5040\tx5760\tx6480\tx7200\tx7920\tx8640\pardirnatural\partightenfactor0

\f0\fs24 \cf0 # Decision Log (Assignment 10 - Three Enemy Types)\
\
- **2026-11-11**: Upgraded the Scorpion (`PatrolEnemy.cs`) from simple back-and-forth movement to a full 3-state FSM (Patrol -> Chase -> Attack). Added `Cooldown` state to make attacks fair.\
- **2026-11-11**: Added a `Physics2D.Raycast` for vision so the Scorpion cannot see the player through walls. Assigned a `Wall` layer to all static obstacles.\
- **2026-11-11**: Fixed a bug where the Scorpion would blindly run off ledges while chasing. Added ledge and wall checks inside the `ChaseLogic` so it stops at edges.\
- **2026-11-11**: Added a 0.5s `windUpTime` (red flash) before the Scorpion attack so the player has time to react.\
\
\
- 2026-11-12: Upgraded Boss AI. Added a 3-state machine (Patrol, WindUp, Attack). Added Raycast vision so he doesn't shoot through pillars. Added a 0.7s WindUp (red flash) before he attacks to make it fair.\
\
- 2026-11-12: Playtested with 2 friends. They beat Level 1 in 2 tries. Level 2 was too hard (5 tries). I decreased Scorpion chase speed from 2.5 to 2.0.}