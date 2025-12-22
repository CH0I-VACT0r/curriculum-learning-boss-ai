# Deep Reinforcement Learning for RPG Boss AI


모듈식 강화학습을 활용한 고성능 RPG 보스 AI 구현 프로젝트

## Project Summary
### 1. 배경 및 목표 (Background & Objective)
현대 RPG 게임의 보스 몬스터는 대부분 FSM(유한 상태 기계) 기반의 스크립트로 동작하여, 패턴이 단조롭고 플레이어에게 쉽게 공략당하는 문제가 있다. 본 프로젝트는 강화학습을 도입하여 타겟과의 거리 유지와 정밀 타격이 가능한 보스 AI를 구현하는 것을 목표로 한다. 특히, 이동과 공격이 결합된 복잡한 행동 공간에서의 학습 불안정성을 해결하기 위해 모듈식 커리큘럼 학습 프레임워크를 제안한다.

### 2. 핵심 성과 (Key Results)
압도적인 성능 향상: 기존 통합 학습 대비 공격 성공률 15% → 98.3% 달성.

정교한 제어: 타겟과의 거리 유지 비율 96.1% 및 벽 충돌 최소화 (Episode당 0.05회).

안정적 학습: PPO 알고리즘 기반의 단계별 학습을 통해 오실레이션을 최소화한 자연스러운 기동 구현.

## System Architecture & Method
### 1. 모듈식 학습 프레임워크
복잡한 태스크를 분해하여 단계적으로 학습시키고 지식을 전이하는 전략을 사용하였다.

Phase 1 (이동 학습): 공격 기능을 끄고, 타겟 추적 및 회피 기동만 집중 학습.

Phase 2 (공격 학습): 1단계 모델의 가중치를 전이 받아, 이동 능력 위에서 공격 타이밍 학습.

### 2. 보상 함수 설계
$$R_{total} = R_{goal} - C_{stability} - C_{efficiency}$$

Goal: 거리 좁힘, 방향 정렬, 유효타 적중 보상.

Stability: 수평 이동(Lateral Move), 제자리 떨림(Jittering), 맴돌기(Circling) 방지 패널티.

Efficiency: 시간 경과 및 공격 빗나감(Miss) 패널티.

## Code Instruction
### Prerequisites
Unity 2022.3.10f1 (LTS)

Python 3.9.13

ml-agents 0.30.0 / torch 1.13.1

### Installation
1. Clone the repository:
``` 
    git clone https://github.com/your-username/your-repo-name.git
```
2. Install Python dependencies:
```
    pip install mlagents==0.30.0 torch==1.13.1
```

3. Open the project in Unity Hub.
   
### Training (How to Run)
#### Phase 1: Movement Training
```
mlagents-learn config/ppo/BossAgent_Phase1.yaml --run-id=Phase1_Movement
```
#### Phase 2: Attack Integration (Transfer Learning)
```
mlagents-learn config/ppo/BossAgent_Phase2.yaml --run-id=Phase2_Final --initialize-from=Phase1_Movement
```

### Demo
#### Performance Comparison
/Assets/ONXX/Boss_AI_SG : PPO Modular
/Assets?ONXX2/


