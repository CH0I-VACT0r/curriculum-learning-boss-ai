using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

public enum BossStage
{
    Movement,
    Attack,
    Dodge,
    Defense,
    RangedAttack,
    Full
}

[RequireComponent(typeof(Rigidbody))]
public class BossAgent : Agent
{
    private static int instanceCounter = 0; // 모든 인스턴스가 공유하는 정적 카운터
    private int agentInstanceId;
    private bool _isLoggingEpisodeEnd = false;

    // --- 평가 지표 추적 ---
    [Header("평가 설정")]
    [Tooltip("평가를 위해 실행할 총 에피소드 수")]
    public int totalEpisodesForEval = 100;
    private int currentEpisodeCount = 0; // 현재까지 진행된 에피소드 수

    // 여러 에피소드에 걸쳐 지표를 누적할 변수들
    private float totalTimeInKitingZone = 0f; // 전체 카이팅 존 유지 시간
    private float totalEpisodeDuration = 0f; // 전체 에피소드 지속 시간
    private int totalAttackAttempts = 0; // 전체 공격 시도 횟수
    private int totalAttackSuccesses = 0; // 전체 공격 성공 횟수

    // 에피소드 내에서 사용할 임시 추적 변수들
    private float episodeStartTime = 0f; // 에피소드 시작 시간
    private float timeInKitingZone = 0f; // 현재 에피소드의 카이팅 존 유지 시간
    private int episodeAttackAttempts = 0; // 현재 에피소드의 공격 시도 횟수
    private int episodeAttackSuccesses = 0; // 현재 에피소드의 공격 성공 횟수

    private int totalWallCollisions = 0; // 전체 벽 충돌 횟수 누적
    private int episodeWallCollisions = 0; // 현재 에피소드의 벽 충돌 횟수
    // -------------------------------------------------------------------------------------


    [Header("Stage Control")]
    public BossStage currentStage = BossStage.Movement;

    [Header("State Link")]
    public MoveState moveState;
    private AttackState attackState;

    [Header("Target")]
    public Transform target;
    private Rigidbody targetRb;

    // ======== ======== ======== Movement Reward Function Settings ======== ======== ========
    [Header("Success/Failure Conditions")]
    [Tooltip("에이전트가 목표에 도달했다고 판단하는 거리")]
    public float successDistance = 1.5f;
    [Tooltip("성공 시 최종적으로 부여할 보상")]
    public float successReward = 5.0f;
    [Tooltip("벽과 충돌 시 부여하는 패널티")]
    public float wallCollisionPenalty = -1.0f;

    [Header("Reward Weights")]
    [Tooltip("거리가 가까워지는 것에 대한 보상 가중치")]
    public float distanceRewardMultiplier = 1.0f; 
    [Tooltip("목표를 향해 바라보는 것에 대한 보상 가중치")]
    public float alignmentMultiplier = 0.5f;
    [Tooltip("목표 방향으로의 전진 속도에 대한 보상 가중치")]
    public float forwardVelocityMultiplier = 0.02f;
    [Tooltip("시간이 흐를 때마다 부여되는 기본 패널티 (생존 패널티)")]
    public float timePenalty = -0.01f;
    [Tooltip("속도 벡터가 목표를 향할수록 주는 보너스")]
    public float velocityAlignmentBonus = 0.01f;

    [Header("Proximity Reward")]
    [Tooltip("근접 보상이 최대로 적용되는 반경")]
    public float proximityRadius = 20.0f;
    [Tooltip("근접 보상의 가중치")]
    public float proximityRewardMultiplier = 0.01f;


    [Header("Penalties")]
    [Tooltip("목표로부터 멀어질 때 적용되는 패널티 가중치")]
    public float moveAwayPenaltyMultiplier = 2.0f;
    [Tooltip("선회 행동(circling)에 대한 패널티 가중치")]
    public float tangentialPenaltyMultiplier = -0.1f;
    [Tooltip("정체 상태(stagnation)일 때 부여되는 패널티")]
    public float stagnationPenalty = -0.05f;
    [Tooltip("좌우 움직임에 대한 패널티 가중치")]
    public float lateralPenaltyMultiplier = 0.02f;

    [Header("Condition Thresholds")]
    [Tooltip("올바른 방향으로 간주하는 최대 각도 (degrees)")]
    public float alignmentAngleThreshold = 5.0f;
    [Tooltip("선회 행동으로 판단하는 최소 접선 속도")]
    public float tangentialVelocityThreshold = 0.5f;
    [Tooltip("선회 행동 판단 시 사용하는 최대 중심 속도 (너무 빠르면 선회가 아님)")]
    public float radialVelocityThresholdForCircling = 0.05f;
    [Tooltip("성공 조건: 목표 도달 시 허용되는 최대 중심 속도")]
    public float radialVelocityThresholdForSuccess = 0.1f;

    [Header("Stagnation Settings")]
    [Tooltip("정체 상태를 판단하기 위해 사용할 스텝 수 (window)")]
    public int recentWindow = 20;
    [Tooltip("정체 상태로 판단하는 최소 거리 변화량 합계")]
    public float stagnationDeltaSumThreshold = 0.01f;

    [Header("Reward Clamping")]
    [Tooltip("한 스텝에서 받을 수 있는 최소 보상")]
    public float minStepReward = -0.2f;
    [Tooltip("한 스텝에서 받을 수 있는 최대 보상")]
    public float maxStepReward = 0.2f;


    [Header("Kiting Settings")]
    [Tooltip("카이팅을 시작할 최소 거리 (이보다 가까우면 위험)")]
    public float minKitingDistance = 0.5f;
    [Tooltip("최적 교전 거리의 최대값 (이보다 멀면 접근해야 함)")]
    public float maxKitingDistance = 3.0f;
    [Tooltip("스위트 스폿에 머물렀을 때 주는 보너스")]
    public float sweetSpotBonus = 0f;

    // --- 내부 사용 변수 ---
    private float prevDistance;
    private readonly Queue<float> recentDeltas = new Queue<float>();

    // ======== ======== ======== ======== ======== ======== ======== ======== ======== ========


    // ======== ======== ======== Attack Reward Function Settings ======== ======== ========
    [Header("Reward Weights")]
    [Tooltip("공격 성공 시 보상")]
    public float meleeHitReward = 3.0f;
    [Tooltip("공격 실패 패널티")]
    public float meleeMissPenalty = -1.5f;
    [Tooltip("공격 가능 거리에서 공격하지 않았을 때의 패널티")]
    public float hesitationPenalty = -0.2f;
    [Tooltip("공격 행동 자체에 대한 비용")]
    public float meleeActionCost = -0.01f;
    // ======== ======== ======== ======== ======== ======== ======== ======== ======== ========

    [Header("Movement Smoothing")]
    public float smoothingFactor = 5.0f;
    private float smoothedMoveX, smoothedMoveZ, smoothedTurn;
    private DecisionRequester decisionRequester;

    private Vector3 dummySkills = Vector3.zero;


    
    public override void Initialize()
    {
        agentInstanceId = instanceCounter++;

        if (target != null) { targetRb = target.GetComponent<Rigidbody>(); }
        if (moveState == null) { moveState = GetComponent<MoveState>(); }
        if (moveState != null) { moveState.target = target; }

        if (attackState == null) { attackState = GetComponent<AttackState>(); }

        // DecisionRequester
        decisionRequester = GetComponent<DecisionRequester>();
        if (decisionRequester == null)
            decisionRequester = gameObject.AddComponent<DecisionRequester>();

        decisionRequester.DecisionPeriod = 2;
        
        //----------------------------------------------
        //statsRecorder = Academy.Instance.StatsRecorder;

        // 평가 카운터 초기화 (훈련/추론 시작 시)
        currentEpisodeCount = 0;
        totalTimeInKitingZone = 0f;
        totalEpisodeDuration = 0f;
        totalAttackAttempts = 0;
        totalAttackSuccesses = 0;
        totalWallCollisions = 0;
    }

    public override void OnEpisodeBegin()
    {
        if (currentStage == BossStage.Movement || currentStage == BossStage.Attack || currentStage == BossStage.Full)
        {
            float range = 12.0f;
            float minSpawnDistance = 7.0f;

            Vector3 agentPos = new Vector3(Random.Range(-range, range), 0.5f, Random.Range(-range, range));
            transform.position = agentPos;

            Vector3 targetPos;
            do
            {
                targetPos = new Vector3(Random.Range(-range, range), 0.5f, Random.Range(-range, range));
            } while (Vector3.Distance(agentPos, targetPos) < minSpawnDistance);

            target.position = targetPos;
        }

        //-----------------------------------------------
        // 평가 카운터 초기화 (훈련/추론 시작 시)
        episodeStartTime = Time.time;
        timeInKitingZone = 0f;
        episodeAttackAttempts = 0;
        episodeAttackSuccesses = 0;
        episodeWallCollisions = 0;

        _isLoggingEpisodeEnd = false;
        //-----------------------------------------------

        prevDistance = Vector3.Distance(transform.position, target.position);
        smoothedMoveX = smoothedMoveZ = smoothedTurn = 0f;

        if (moveState != null)
            moveState.ResetVelocity();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target position
        sensor.AddObservation(transform.InverseTransformPoint(target.position).normalized); // 3
        sensor.AddObservation(Vector3.Distance(transform.position, target.position) / 35.5f); // 1

        // Movement
        sensor.AddObservation(smoothedMoveX);
        sensor.AddObservation(smoothedMoveZ);
        sensor.AddObservation(smoothedTurn);
        sensor.AddObservation(transform.forward.normalized); // 3

        // Target velocity
        if (targetRb != null)
            sensor.AddObservation(targetRb.velocity.normalized); // 3
        else
            sensor.AddObservation(Vector3.zero); // 3

        // Skills / action padding
        sensor.AddObservation(dummySkills); // 3
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Movement
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        float turn = actions.ContinuousActions[2];

        smoothedMoveX = Mathf.Lerp(smoothedMoveX, moveX, Time.fixedDeltaTime * smoothingFactor);
        smoothedMoveZ = Mathf.Lerp(smoothedMoveZ, moveZ, Time.fixedDeltaTime * smoothingFactor);
        smoothedTurn = Mathf.Lerp(smoothedTurn, turn, Time.fixedDeltaTime * smoothingFactor);

        if (moveState != null)
        {
            moveState.pendingMoveXInput = smoothedMoveX;
            moveState.pendingForwardInput = smoothedMoveZ;
            moveState.pendingTurnInput = smoothedTurn;
        }

        // --- Read Discrete Actions Safely ---
        int melee = 0;
        int ranged = 0;
        int dodge = 0;
        int block = 0;

        // 이산 행동 배열의 크기가 0보다 클 때만 값을 읽어옵니다.
        if (actions.DiscreteActions.Length > 0)
        {
            melee = actions.DiscreteActions[0];
            if (actions.DiscreteActions.Length > 1) ranged = actions.DiscreteActions[1];
            if (actions.DiscreteActions.Length > 2) dodge = actions.DiscreteActions[2];
            if (actions.DiscreteActions.Length > 3) block = actions.DiscreteActions[3];
        }

        // --- Execute Actions ---
        if (melee == 1)
        {
            attackState.PerformAttack();
            AddReward(meleeActionCost);
        }

        // Stage Reward
        if (currentStage == BossStage.Movement || currentStage == BossStage.Attack || currentStage == BossStage.Full)
            AddReward(MovementReward());

        if (currentStage == BossStage.Attack || currentStage == BossStage.Full)
            AddReward(AttackReward(melee));

        if (currentStage == BossStage.RangedAttack || currentStage == BossStage.Full)
            AddReward(RangedAttackReward(ranged));

        if (currentStage == BossStage.Dodge || currentStage == BossStage.Full)
            AddReward(DodgeReward(dodge));

        if (currentStage == BossStage.Defense || currentStage == BossStage.Full)
            AddReward(DefenseReward(block));

        // 공격 기회를 놓쳤을 때 패널티 계산
        if (currentStage == BossStage.Attack || currentStage == BossStage.Full)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            if (distanceToTarget <= successDistance && melee == 0)
            {
                AddReward(hesitationPenalty);
            }
        }

        if (StepCount >= MaxStep - 1)
        {
            LogEvaluationMetrics();
            //if (!_isLoggingEpisodeEnd)
            //{
            //    _isLoggingEpisodeEnd = true;
            //    LogEvaluationMetrics();
            //}
        }
    }


    void FixedUpdate()
    {
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= maxKitingDistance)
        {
            timeInKitingZone += Time.fixedDeltaTime;
        }
    }

    public void IncrementAttackAttempts()
    {
        episodeAttackAttempts++;
    }
    public void IncrementAttackSuccesses()
    {
        episodeAttackSuccesses++;
    }

    // --- Reward functions ---
    private float MovementReward()
    {
        float dist = Vector3.Distance(transform.position, target.position);
        float delta = prevDistance - dist;
        prevDistance = dist;

        // Recent Delta Window
        recentDeltas.Enqueue(Mathf.Abs(delta));
        if (recentDeltas.Count > recentWindow) recentDeltas.Dequeue();

        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 dirToTarget = (target.position - transform.position).normalized;

        float alignment = Vector3.Dot(transform.forward, dirToTarget);
        float orientationReward = (alignment >= Mathf.Cos(alignmentAngleThreshold * Mathf.Deg2Rad)) ? alignmentMultiplier : Mathf.Min(0f, alignment * alignmentMultiplier * 3f);

        // Forward Velocity Reward
        float forwardVel = Mathf.Max(0f, Vector3.Dot(rb.velocity, dirToTarget));
        float forwardVelReward = forwardVel * forwardVelocityMultiplier;

        // Tangential Velocity Penalty
        //float radialVel = Vector3.Dot(rb.velocity, dirToTarget);
        Vector3 relativeVelocity = rb.velocity - targetRb.velocity;
        float radialVel = Vector3.Dot(relativeVelocity, dirToTarget);
        float tangentialVel = (rb.velocity - radialVel * dirToTarget).magnitude;
        float currentTangentialPenalty = 0f;

        // Lateral Velocity Penalty
        Vector3 lateralVelocity = Vector3.ProjectOnPlane(rb.velocity, dirToTarget);
        float lateralPenalty = -lateralVelocity.magnitude * lateralPenaltyMultiplier;

        if (tangentialVel > tangentialVelocityThreshold && Mathf.Abs(radialVel) < radialVelocityThresholdForCircling)
        {
            currentTangentialPenalty = tangentialPenaltyMultiplier * Mathf.Clamp01((tangentialVel - tangentialVelocityThreshold) / 2f);
        }
        float currentStagnationPenalty = 0f;

        // Recent Stagnation Penalty
        float recentSum = 0f;
        foreach (var v in recentDeltas) recentSum += v;

        if (recentDeltas.Count == recentWindow && recentSum < stagnationDeltaSumThreshold && dist > successDistance * 1.1f)
        {
            currentStagnationPenalty = stagnationPenalty;
        }

        float velocityAlignmentReward = 0f;
        if (rb.velocity.magnitude > 0.1f)
        {
            Vector3 velocityDirection = rb.velocity.normalized;
            float velocityAlignment = Vector3.Dot(velocityDirection, dirToTarget);
            velocityAlignmentReward = velocityAlignment * velocityAlignmentBonus;
        }

        float distanceBasedReward = 0;
        float moveAwayPenalty = 0;

        if (dist < minKitingDistance) // 1. 위험 구간 (너무 가까움)
        {
            distanceBasedReward = -delta * distanceRewardMultiplier;
        }
        else if (dist <= maxKitingDistance) // 2. (최적 거리)
        {
            distanceBasedReward = sweetSpotBonus;
        }
        else // 3. 접근 구간 
        {
            distanceBasedReward = Mathf.Max(0f, delta) * distanceRewardMultiplier;
            if (delta < 0)
            {
                moveAwayPenalty = delta * moveAwayPenaltyMultiplier;
            }
        }

        // Reward Aggregation
        float stepReward = distanceBasedReward + moveAwayPenalty + orientationReward + forwardVelReward + currentTangentialPenalty 
            + timePenalty + currentStagnationPenalty + velocityAlignmentReward + lateralPenalty;

        // Reward Clamping
        stepReward = Mathf.Clamp(stepReward, minStepReward, maxStepReward);

        // Success Condition
        if (currentStage == BossStage.Movement && dist < successDistance && Mathf.Abs(radialVel) < radialVelocityThresholdForSuccess)
        {
            AddReward(successReward);
            // EndEpisode();
            return 0f;
        }

        return stepReward;
    }

    private float AttackReward(int melee)
    {
        if (melee == 0) return 0f;

        if (attackState.CheckAndConsumeHit())
        {

            Vector3 dirToTarget = (target.position - transform.position).normalized;
            float alignment = Vector3.Dot(transform.forward, dirToTarget); 

            // float dynamicHitReward = meleeHitReward * Mathf.Clamp01(alignment);
            float dynamicHitReward = meleeHitReward * (alignment - 0.5f) * 2f;


            if (alignment > 0.95f)
            {
                dynamicHitReward *= 1.5f;
            }

            AddReward(dynamicHitReward);
            //LogEvaluationMetrics();
            // EndEpisode();
            return 0f;
        }
        else
        {
            return meleeMissPenalty;
        }
    }

    private float RangedAttackReward(int ranged)
    {
        bool hit = false; // 추후 로직 필요
        if (ranged == 1 && hit) return 0.6f;
        return 0f;
    }

    private float DodgeReward(int dodge)
    {
        bool dodged = false; // 추후 로직 필요
        if (dodge == 1 && dodged) return 0.2f;
        return 0f;
    }

    private float DefenseReward(int block)
    {
        bool blocked = false; // 추후 로직 필요
        if (block == 1 && blocked) return 0.2f;
        return 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(wallCollisionPenalty);
            episodeWallCollisions++;
            // EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) 
    { 
        float horizontalInput = Input.GetAxis("Horizontal"); 
        float verticalInput = Input.GetAxis("Vertical"); 
        float turnInput = 0.0f; 

        if (Input.GetKey(KeyCode.Q)) turnInput = -1.0f; 
        else if (Input.GetKey(KeyCode.E)) turnInput = 1.0f; 
        var continuousActions = actionsOut.ContinuousActions; 
        continuousActions[0] = horizontalInput; 
        continuousActions[1] = verticalInput; 
        continuousActions[2] = turnInput;

        var discreteActions = actionsOut.DiscreteActions;
        if (discreteActions.Length > 0)
        {
            discreteActions[0] = 0;
            if (Input.GetMouseButtonDown(0))
            {
                discreteActions[0] = 1;
            }
        }
    }

    private void LogEvaluationMetrics()
    {
        if (_isLoggingEpisodeEnd) return;
        _isLoggingEpisodeEnd = true;
        // 목표 에피소드 수를 초과하지 않도록 방지
        if (currentEpisodeCount >= totalEpisodesForEval)
        {         
            return;
        }
        currentEpisodeCount++;
        
        // 에피소드 지속 시간 계산
        float duration = Time.time - episodeStartTime;
        totalEpisodeDuration += duration;

        // 통계 누적
        totalTimeInKitingZone += timeInKitingZone;
        totalAttackAttempts += episodeAttackAttempts;
        totalAttackSuccesses += episodeAttackSuccesses;
        totalWallCollisions += episodeWallCollisions;

        // 개별 에피소드 통계 로그 (선택 사항, 디버깅에 유용)
        float episodeSuccessRate = (episodeAttackAttempts > 0) ? (float)episodeAttackSuccesses / episodeAttackAttempts : 0f;
        float episodeKitingRatio = (duration > 0) ? timeInKitingZone / duration : 0f;
        Debug.Log($"에피소드 {currentEpisodeCount}/{totalEpisodesForEval} 종료. " +
                  $"지속시간: {duration:F2}초, " +
                  $"타겟 범위 내 비율: {episodeKitingRatio * 100f:F1}%, " +
                  $"공격 성공률: {episodeSuccessRate * 100f:F1}% ({episodeAttackSuccesses}/{episodeAttackAttempts})" +
                  $"벽 충돌: {episodeWallCollisions}");

        Debug.Log($"ID:{agentInstanceId} - 에피소드 {currentEpisodeCount}/{totalEpisodesForEval} 종료. ...");
        // 평가 완료 여부 확인
        if (currentEpisodeCount == totalEpisodesForEval)
        {
            Debug.Log($"[{Time.time:F3}] ID:{agentInstanceId} - 인스턴스 평가 완료"); // 노란색 경고 로그
            // 평균 계산
            float avgKitingRatio = (totalEpisodeDuration > 0) ? totalTimeInKitingZone / totalEpisodeDuration : 0f;
            float avgSuccessRate = (totalAttackAttempts > 0) ? (float)totalAttackSuccesses / totalAttackAttempts : 0f;
            float avgAttemptsPerEpisode = (float)totalAttackAttempts / totalEpisodesForEval;
            float avgWallCollisionsPerEpisode = (float)totalWallCollisions / totalEpisodesForEval;

            // 최종 결과 로그 (노란색 경고로 출력)
            Debug.Log("--- 평가 완료 ---");
            Debug.Log($"{totalEpisodesForEval} 에피소드 평균 카이팅 존 비율: {avgKitingRatio * 100f:F1}%");
            Debug.Log($"에피소드당 평균 공격 시도 횟수: {avgAttemptsPerEpisode:F2}");
            Debug.Log($"{totalEpisodesForEval} 에피소드 평균 공격 성공률: {avgSuccessRate * 100f:F1}% ( 총 {totalAttackSuccesses}/{totalAttackAttempts})");
            Debug.Log($"에피소드당 평균 벽 충돌 횟수: {avgWallCollisionsPerEpisode:F2} (총 {totalWallCollisions})");
            Debug.Log("-----------------");
        }
    }
}

