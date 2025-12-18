using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackHitbox : MonoBehaviour
{
    [Tooltip("이 히트박스를 제어하는 AttackState 스크립트")]
    public AttackState attackState;
    private BoxCollider _collider;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
    }

    void OnTriggerStay(Collider other)
    {
        if ((other.CompareTag("Player")) || (other.CompareTag("Dummy")) && attackState != null)
        {
            attackState.RegisterHit();
        }
    }
    private void OnDrawGizmos()
    {
        if (_collider == null)
        {
            _collider = GetComponent<BoxCollider>();
            if (_collider == null) return;
        }

        // 콜라이더의 활성화 상태에 따라 기즈모 색상을 결정합니다.
        Gizmos.color = _collider.enabled ? Color.green : Color.red;

        // 콜라이더의 위치, 회전, 스케일을 기즈모에 정확히 반영합니다.
        Gizmos.matrix = transform.localToWorldMatrix;

        // 콜라이더 타입에 맞춰 기즈모를 그립니다. (BoxCollider 예시)
        if (_collider is BoxCollider boxCollider)
        {
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}
