using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyManager : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private CombatScript _playerCombat;
    [SerializeField] private PlayerMovement _playerMovement;

    [Header("Enemy Settings")]
    [SerializeField] private EnemyScript enemyPrefab;
    public LayerMask layerMask;
    public List<EnemyScript> attackingEnemies = new List<EnemyScript>();
    public List<EnemyScript> allEnemies = new List<EnemyScript>();
    public Vector3[] spawnPositions;
    public float maxCooldownTime = 3f;

    [Header("Activation")]
    public bool enemiesActive = false; 
    public float detectionRange = 10f; 

    [CanBeNull] private EnemyScript currentTarget;
    private Vector3 _inputDirection;
    private bool onCooldown = false;

    public static EnemyManager instance;

    private void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        SpawnEnemies(spawnPositions);
    }

    private void Update()
    {
        if (!enemiesActive) return;

        DetectTarget();

        if (!onCooldown)
            TryAttack();
    }

    public void TryAttack()
    {
        onCooldown = true;
        EnemyScript attacker = ChooseAttacker();

        if (attacker != null)
            attacker.AttackPlayer();

        float delay = Random.Range(1.7f, Mathf.Max(1.7f, maxCooldownTime));
        LeanTween.delayedCall(gameObject, delay, () => onCooldown = false);
    }

    private EnemyScript ChooseAttacker()
    {
        if (_playerCombat == null) return null;

        List<EnemyScript> candidates = new List<EnemyScript>();
        for (int i = 0; i < allEnemies.Count; i++)
        {
            EnemyScript enemy = allEnemies[i];
            if (enemy != null && enemy.enabled && enemy._enemyState == EnemyState.Idle && enemy != _playerCombat.lockedTarget && !attackingEnemies.Contains(enemy))
            {
                candidates.Add(enemy);
            }
        }

        if (candidates.Count > 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        return null;
    }

    private void DetectTarget()
    {
        if (_playerMovement == null) return;

        _inputDirection = _playerMovement.moveDir.normalized;

        if (_inputDirection.sqrMagnitude < 0.01f)
        {
            currentTarget = null;
            return;
        }

        RaycastHit ray;
        if (Physics.SphereCast(_playerMovement.transform.position, 3f, _inputDirection, out ray, detectionRange, layerMask))
        {
            EnemyScript hitEnemy = ray.collider.GetComponent<EnemyScript>();
            if (hitEnemy != null)
                currentTarget = hitEnemy;

            Debug.DrawLine(_playerMovement.transform.position, ray.collider.transform.position, Color.green);
        }
        else
        {
            currentTarget = null;
            Debug.DrawRay(_playerMovement.transform.position, _inputDirection * detectionRange, Color.red);
        }
    }

    public EnemyScript GetCurrentTarget() => currentTarget;

    public EnemyScript GetRandomEnemy(float maxDistance = 10f)
    {
        if (allEnemies.Count == 0 || _playerMovement == null) return null;

        List<EnemyScript> candidates = new List<EnemyScript>();
        Vector3 playerPos = _playerMovement.transform.position;

        for (int i = 0; i < allEnemies.Count; i++)
        {
            EnemyScript enemy = allEnemies[i];
            if (enemy != null && enemy.enabled && enemy._enemyState == EnemyState.Idle)
            {
                if (Vector3.Distance(enemy.transform.position, playerPos) <= maxDistance)
                {
                    candidates.Add(enemy);
                }
            }
        }

        if (candidates.Count > 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        return null;
    }

    public Vector3 GetRandomPositionNearTarget(Vector3 target, float radius, LayerMask obstacleMask)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 candidate = new Vector3(target.x + offset.x, target.y, target.z + offset.y);

            if (!Physics.CheckSphere(candidate, 0.5f, obstacleMask))
                return candidate;
        }

        return target;
    }

    public void AddEnemy(EnemyScript enemy)
    {
        if (enemy != null && !allEnemies.Contains(enemy))
        {
            allEnemies.Add(enemy);
        }
    }

    public void RemoveEnemy(EnemyScript enemy)
    {
        if (enemy == null) return;
        allEnemies.Remove(enemy);
        RemoveAttacker(enemy);
    }
    
    public void RemoveAttacker(EnemyScript attacker)
    {
        if (attacker == null) return;
        if (attacker.attackingParticle != null)
        {
            attacker.attackingParticle.Stop();
        }
        attackingEnemies.Remove(attacker);
    }

    public void AddAttacker(EnemyScript attacker)
    {
        if (attacker != null && !attackingEnemies.Contains(attacker))
        {
            attackingEnemies.Add(attacker);
        }
    }

    public int AliveEnemies() => allEnemies.Count;

    public EnemyScript[] GetAllEnemies() => allEnemies.ToArray();

    private void SpawnEnemies(Vector3[] positions)
    {
        if (positions == null || positions.Length == 0) return;

        EnemyScript prefab = enemyPrefab;
        if (prefab == null && allEnemies.Count > 0)
        {
            prefab = allEnemies[0];
        }

        if (prefab == null) return;

        foreach (Vector3 pos in positions)
        {
            EnemyScript enemy = Instantiate(prefab, pos, Quaternion.identity);
            if (_playerMovement != null)
            {
                enemy.playerTarget = _playerMovement.transform;
            }
            enemy.enabled = enemiesActive;
            AddEnemy(enemy);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, _inputDirection);
        Gizmos.DrawWireSphere(transform.position, 1);

        if (currentTarget != null)
            Gizmos.DrawSphere(currentTarget.transform.position, 0.5f);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
