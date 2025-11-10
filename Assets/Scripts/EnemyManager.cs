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
    public LayerMask layerMask;
    public List<EnemyScript> attackingEnemies;
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
        else Destroy(gameObject);

        SpawnEnemies(spawnPositions);
    }

    private void Update()
    {
        if (!enemiesActive) return;

        DetectTarget();

        if (!onCooldown)
            TryAttack();
    }

    #region Attack Logic
    public void TryAttack()
    {
        onCooldown = true;
        EnemyScript attacker = ChooseAttacker();

        if (attacker != null)
            attacker.AttackPlayer();

        LeanTween.delayedCall(Random.Range(1.7f, maxCooldownTime), () => onCooldown = false);
    }

    private EnemyScript ChooseAttacker()
    {
        for (int i = 0; i < allEnemies.Count; i++)
        {
            EnemyScript attacker = GetRandomEnemy(20);
            if (attacker == null) continue;

            if (attacker._enemyState == EnemyState.Idle && attacker != _playerCombat.lockedTarget)
                return attacker;
        }

        return null;
    }
    #endregion

    #region Player Target Detection
    private void DetectTarget()
    {
        _inputDirection = _playerMovement.moveDir.normalized;

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
    #endregion

    #region Utility
    public EnemyScript GetCurrentTarget() => currentTarget;

    public EnemyScript GetRandomEnemy(float maxDistance = 10f)
    {
        for (int i = 0; i < 10; i++)
        {
            EnemyScript randomEnemy = allEnemies[Random.Range(0, allEnemies.Count)];
            if (randomEnemy._enemyState == EnemyState.Idle)
            {
                if (Vector3.Distance(randomEnemy.transform.position, _playerMovement.transform.position) <= maxDistance)
                    return randomEnemy;
            }
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

    public void AddEnemy(EnemyScript enemy) => allEnemies.Add(enemy);

    public void RemoveEnemy(EnemyScript enemy) => allEnemies.Remove(enemy);
    
    public void RemoveAttacker(EnemyScript attacker)
    {
        attacker.attackingParticle.Stop();
        attackingEnemies.Remove(attacker);
    }

    public void AddAttacker(EnemyScript attacker)
    {
        if(!attackingEnemies.Contains(attacker)) attackingEnemies.Add(attacker);
    }

    public int AliveEnemies() => allEnemies.Count;

    public EnemyScript[] GetAllEnemies() => allEnemies.ToArray();
    #endregion

    #region Spawning
    private void SpawnEnemies(Vector3[] spawnPositions)
    {
        foreach (Vector3 pos in spawnPositions)
        {
            EnemyScript enemy = Instantiate(_playerCombat.lockedTarget.GetComponent<EnemyScript>(), pos, Quaternion.identity);
            enemy.playerTarget = _playerMovement.transform;
            enemy.enabled = false; // Wait for activation
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, _inputDirection);
        Gizmos.DrawWireSphere(transform.position, 1);

        if (currentTarget != null)
            Gizmos.DrawSphere(currentTarget.transform.position, 0.5f);
    }
}
