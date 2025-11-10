using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    [Header("References")] [CanBeNull] public Transform playerTarget;
    [SerializeField] public Health enemyHealth;
    [SerializeField] public ParticleSystem attackingParticle;
    [SerializeField] private AudioClip punchSound;
    [SerializeField] private AudioClip[] footStepSounds;

    [Header("Movement")] [SerializeField] private float speed = 3f;
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private float dampTime = 0.3f;

    [Header("State")] public EnemyState _enemyState = EnemyState.Idle;
    public Vector3 targetPosition;

    private AudioSource audioSource;
    private Animator animator;
    private CharacterController controller;
    private Coroutine attackCoroutine;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<Health>();
        audioSource = GetComponent<AudioSource>();

        EnemyManager.instance.AddEnemy(this);

        enemyHealth.onDamageApplied.AddListener(OnDamaged);
        enemyHealth.onDeath.AddListener(OnDeath);

        this.enabled = false;
    }

    private void Update()
    {
        HandleMovement();
        RotateTowardsPlayer();
    }

    private void FixedUpdate()
    {
        if (!controller.isGrounded)
            controller.Move(Vector3.down * gravity * Time.fixedDeltaTime);
    }

    #region Player Interaction / Attack

    public void AttackPlayer(float stopDistance = 1f)
    {
        if (_enemyState == EnemyState.Attacking || attackCoroutine != null || playerTarget == null)
            return;

        _enemyState = EnemyState.Running;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance <= stopDistance)
        {
            StartCoroutine(PerformAttack(stopDistance));
            return;
        }

        animator.SetTrigger("Run");
        attackCoroutine = StartCoroutine(PerformAttack(stopDistance));
    }

    private IEnumerator PerformAttack(float stopDistance)
    {
        while (playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) > stopDistance)
        {
            yield return null;
        }

        EnemyManager.instance.AddAttacker(this);
        _enemyState = EnemyState.Attacking;
        if (attackingParticle != null) attackingParticle.Play();
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(1f);
        TryHittingPlayer();

        yield return new WaitForSeconds(0.5f);
        EnemyManager.instance.RemoveAttacker(this);
        Retract();

        _enemyState = EnemyState.Idle;
        attackCoroutine = null;
    }

    public void StopAttacking()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        EnemyManager.instance.RemoveAttacker(this);
        attackingParticle.Stop();
        _enemyState = EnemyState.Idle;
        animator.SetTrigger("Idle");
    }

    private void TryHittingPlayer()
    {
        Vector3 attackPos = Vector3.MoveTowards(transform.position, playerTarget.position, 1f);
        Collider[] hits = Physics.OverlapSphere(attackPos, 0.7f, LayerMask.GetMask("Player"));

        foreach (Collider col in hits)
        {
            Health playerHealth = col.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.RegisterHit();
                audioSource.volume = 1;
                audioSource.clip = punchSound;
                audioSource.Play();
            }
        }
    }


    public void playFootSteps()
    {
        audioSource.volume = 0.3f;
        audioSource.clip = footStepSounds[Random.Range(0, footStepSounds.Length)];
        audioSource.Play();
    }

#endregion

    #region Movement & Rotation
    private void HandleMovement()
    {
        if (targetPosition == Vector3.zero) 
        {
            animator.SetFloat("X", 0f, dampTime, Time.deltaTime);
            animator.SetFloat("Y", 0f, dampTime, Time.deltaTime);
            return;
        }

        Vector3 moveDir = targetPosition - transform.position;
        moveDir.y = 0;
        float distance = moveDir.magnitude;

        if (distance > 0.4f)
        {
            controller.Move(moveDir.normalized * speed * Time.deltaTime);

            Vector3 localDir = transform.InverseTransformDirection(moveDir.normalized);
            animator.SetFloat("X", localDir.x, dampTime, Time.deltaTime);
            animator.SetFloat("Y", localDir.z, dampTime, Time.deltaTime);
        }
        else
        {
            targetPosition = Vector3.zero;
            _enemyState = EnemyState.Idle;
            animator.SetFloat("X", 0f, dampTime, Time.deltaTime);
            animator.SetFloat("Y", 0f, dampTime, Time.deltaTime);
        }
    }

    private void RotateTowardsPlayer()
    {
        if (playerTarget == null ) return;

        Vector3 dir = playerTarget.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.05f);
        }
    }
    #endregion

    #region Retract
    public void Retract()
    {
        attackingParticle.Stop();
        _enemyState = EnemyState.Retracting;
        targetPosition = EnemyManager.instance.GetRandomPositionNearTarget(
            playerTarget.position, 5f, EnemyManager.instance.layerMask);
    }
    #endregion

    #region Events
    private void OnDamaged()
    {
        StopAllCoroutines();
        StopAttacking();
        animator.SetTrigger("Hit");
        _enemyState = EnemyState.Idle;
    }

    private void OnDeath()
    {
        animator.SetTrigger("Death");
        EnemyManager.instance.RemoveEnemy(this);
        
        if (EnemyManager.instance.AliveEnemies() <= 0)
        {
            UI_Manager.instance.ShowWinScreen();
        }
        
        Destroy(controller);
        Destroy(this);
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (_enemyState == EnemyState.Attacking && playerTarget != null)
        {
            Gizmos.color = Color.red;
            Vector3 attackPos = Vector3.MoveTowards(transform.position, playerTarget.position, 1f);
            Gizmos.DrawSphere(attackPos, 0.7f);
        }
    }
}

public enum EnemyState
{
    Idle,
    Running,
    Attacking,
    Retracting
}
