using UnityEngine;
using UnityEngine.AI; // AI 네임스페이스 추가!

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshTargetMovement : MonoBehaviour
{
    [Tooltip("타겟의 이동 속도")]
    public float speed = 3.5f;

    [Tooltip("활동 반경 (시작 위치로부터 얼마나 멀리까지 갈지)")]
    public float patrolRadius = 25f;

    private NavMeshAgent agent;
    private Vector3 startPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = speed;
        }
        startPosition = transform.position;

        SetNewRandomDestination();
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            SetNewRandomDestination();
        }
    }

    void SetNewRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition; 

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randomDirection, out navHit, 10.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }
}