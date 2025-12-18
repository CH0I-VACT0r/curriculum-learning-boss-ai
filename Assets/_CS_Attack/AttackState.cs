using System.Collections;
using UnityEngine;

public class AttackState : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("애니메이션을 제어할 Animator")]
    public Animator animator;
    [Tooltip("공격 판정에 사용할 콜라이더")]
    public Collider attackHitbox;

    [Header("연결")]
    [Tooltip("메인 BossAgent 스크립트에 대한 참조")]
    public BossAgent agent; // BossAgent 참조 추가

    [Header("Attack Timings")]
    [Tooltip("공격 애니메이션 선딜레이")]
    public float attackWindUp = 0.8f;
    [Tooltip("공격 판정이 활성화되어 있는 시간")]
    public float attackActiveTime = 0.1f;
    [Tooltip("공격 애니메이션 후 딜레이")]
    public float attackCooldown = 1.1f;

    [Header("Global Cooldown")]
    [Tooltip("공격 후 다음 공격까지 필요한 최소 시간 (전체 쿨타임)")]
    public float globalCooldown = 2.5f;

    public bool IsAttacking { get; private set; } = false;
    private bool _isOnGlobalCooldown = false;
    private bool _didHit = false;
    private bool _hitRegisteredThisAttack = false;

    void Awake() // Awake를 사용하여 agent 참조를 일찍 설정
    {
        if (agent == null) agent = GetComponent<BossAgent>();
    }
    void Start()
    {
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }
    }

    // Start Attack
    public void PerformAttack()
    {
        if (_isOnGlobalCooldown || IsAttacking)
        {
            return;
        }
        StartCoroutine(AttackSequence());
        if (agent != null)
        {
            agent.IncrementAttackAttempts(); // BossAgent에 새로 만들 public 함수 호출
        }
    }

    private IEnumerator AttackSequence()
    {
        IsAttacking = true;
        _isOnGlobalCooldown = true; // 글로벌 쿨타임 시작
        _didHit = false;
        _hitRegisteredThisAttack = false; // 성공 카운트 플래그 초기화

        animator.SetTrigger("Attack");

        // 선딜레이
        yield return new WaitForSeconds(attackWindUp);

        // 콜라이더 활성화
        if (attackHitbox != null)
        {
            attackHitbox.enabled = true;
        }

        // 공격 판정 시간
        yield return new WaitForSeconds(attackActiveTime);

        // 콜라이더 비활성화
        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
        }

        // 이동/회전 제한 해제 (후딜레이 중 움직임 가능)
        IsAttacking = false;

        // 1. 이미 지나간 시간 (선딜 + 판정시간) 계산
        float timeElapsed = attackWindUp + attackActiveTime;

        // 2. 남은 애니메이션 후딜레이 시간 계산
        float remainingAnimCooldown = attackCooldown; 

        // 3. 남은 글로벌 쿨타임 시간 계산
        float remainingGlobalCooldown = globalCooldown - timeElapsed;

        // 4. 둘 중 더 긴 시간을 계산하여 한 번만 기다림
        float waitTime = Mathf.Max(remainingAnimCooldown, remainingGlobalCooldown);

        if (waitTime > 0)
        {
            yield return new WaitForSeconds(waitTime);
        }
        _isOnGlobalCooldown = false;
    }

    // Register Attack Success
    public void RegisterHit()
    {
        if (!_hitRegisteredThisAttack)
        {
            _hitRegisteredThisAttack = true; // 플래그를 true로 바꿔 중복 처리 방지
            _didHit = true;                

            if (agent != null)
            {
                agent.IncrementAttackSuccesses();
            }
            else
            {
                Debug.Log("RegisterHit: BossAgent reference not set!");
            }
        }
    }

    // Agent.cs Check Attack Success
    public bool CheckAndConsumeHit()
    {
        if (_didHit)
        {
            _didHit = false;
            return true;
        }
        return false;
    }
}
