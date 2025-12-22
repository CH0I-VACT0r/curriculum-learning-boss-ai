# Deep Reinforcement Learning for RPG Boss AI


모듈식 강화학습을 활용한 고성능 RPG 보스 AI 구현 프로젝트

---

## Project Summary
### 1. 배경 및 목표 (Background & Objective)
현대 RPG 게임의 보스 몬스터는 대부분 FSM(유한 상태 기계) 기반의 스크립트로 동작하여, 패턴이 단조롭고 플레이어에게 쉽게 공략당하는 문제가 있다. 본 프로젝트는 강화학습을 도입하여 타겟과의 거리 유지와 정밀 타격이 가능한 보스 AI를 구현하는 것을 목표로 한다. 특히, 이동과 공격이 결합된 복잡한 행동 공간에서의 학습 불안정성을 해결하기 위해 모듈식 커리큘럼 학습 프레임워크를 제안한다.

### 2. 핵심 성과 (Key Results)
압도적인 성능 향상: 기존 통합 학습 대비 공격 성공률 15% → 98.3% 달성.

정교한 제어: 타겟과의 거리 유지 비율 96.1% 및 벽 충돌 최소화 (Episode당 0.05회).

게임성 확보 : 불필요한 움직임을 제어하는 보상 함수를 통해 오실레이션을 최소화한 자연스러운 움직임 구현 노력

---

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

---

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

---
### Training (How to Run)
#### Phase 1: Movement Training
```
mlagents-learn config/ppo/BossAgent_Phase1.yaml --run-id=Phase1_Movement
```
#### Phase 2: Attack Integration (Transfer Learning)
```
mlagents-learn config/ppo/BossAgent_Phase2.yaml --run-id=Phase2_Final --initialize-from=Phase1_Movement
```

---
### Usage Guide (Inference & Time Scale)
#### How to Run Inference (Pre-trained Models)
학습이 완료된 ONNX 모델을 적용하여 파이썬 연결 없이 유니티 에디터 상에서 AI를 테스트하는 방법

1. Navigate to the Assets/ONNX (or Assets/ONNX_2) folder to find the trained .onnx files.

2. Select the Agent GameObject (e.g., BossAgent) in the Hierarchy window.

3. Locate the Behavior Parameters component in the Inspector.

4. Drag and drop the desired .onnx file into the Model slot.

5. Set Behavior Type to Inference Only.

6. Press the Play button in Unity.

#### Adjusting Time Scale (Speed Up Simulation)
시뮬레이션 속도를 높여 AI의 행동의 빠른 관찰을 위한 Time Scale 조정

1. Using the Inspector (Recommended) If you have the TimeManager script attached to the scene:

2. Find the TimeManager object in the Hierarchy.

3. Change the Time Scale slider value (e.g., 1.0 to 15.0).
   
#### Verifying Results (Console Log)
유니티 에디터 하단의 Console 창을 통해 실시간 추론 결과를 정량적으로 확인할 수 있다.

Episode Result: 각 에피소드가 종료될 때마다 거리 유지 비율, 공격 횟수, 공격 성공률, 벽 충돌 횟수가 출력된다.

100-Episode Average: 100번의 에피소드가 진행될 때마다 핵심 평가지표의 평균값이 자동으로 계산되어 로그에 출력된다.

---

### Demo
#### Performance Comparison
Assets/ONNX/BOSSAI_SG.onnx : 모듈식 ppo

Assets/ONNX/MONO.onxx : 통합 학습 방식 ppo

Assets/ONNX_2/Modular 1 & 2.onnx : 모듈식 ppo

Assets/ONNX_2/MONO 1 & 2.onnx : 통합 학습 방식 ppo

Assets/ONNX_2/SAC 1 & 2.onnx : 통합 학습 방식 sac

Assets/ONNX_2/SAC_Tuned.onnx : sac 하이퍼파라미터 튜닝

---

## Conclusion and Future Work
본 프로젝트는 복잡한 3D 액션 환경에서 모듈식 학습이 통합 학습 방식보다 우수한 수렴 속도와 성능을 보임을 입증하였다. 특히 엔트로피 제어 실패로 불안정한 모습을 보인 SAC 알고리즘과 달리, 제안된 PPO 기반 모듈식 모델은 안정적인 내비게이션과 높은 공격 적중률을 동시에 달성했다.

### Future Work:
Skill Diversity: 단일 공격을 넘어 스킬을 선택하는 계층적 강화학습(Hierarchical RL)으로 확장.

Multi-Agent: 1:1 전투를 넘어 다수의 플레이어와 상호작용하는 레이드(Raid) 보스 AI 연구.

Complex Terrain: 장애물이 복잡한 환경에서의 내비게이션 지능 고도화.

### Author
Name: 최성우 / SUNG-WOO CHOI

Contact: vactor823@khu.ac.kr

Affiliation: 경희대학교


