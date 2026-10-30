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
    private float verticalVelocity = -2f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<Health>();
        audioSource = GetComponent<AudioSource>();

        if (EnemyManager.instance != null)
        {
            EnemyManager.instance.AddEnemy(this);
        }
    }

    private void Start()
    {
        if (enemyHealth != null)
        {
            enemyHealth.onDamageApplied.AddListener(OnDamaged);
            enemyHealth.onDeath.AddListener(OnDeath);
        }

        if (EnemyManager.instance != null && !EnemyManager.instance.enemiesActive)
        {
            this.enabled = false;
        }
    }

    private void Update()
    {
        HandleMovement();
        RotateTowardsPlayer();
    }

    public void AttackPlayer(float stopDistance = 1f)
    {
        if (_enemyState == EnemyState.Attacking || attackCoroutine != null || playerTarget == null)
            return;

        _enemyState = EnemyState.Running;
        attackCoroutine = StartCoroutine(PerformAttack(stopDistance));
    }

    private IEnumerator PerformAttack(float stopDistance)
    {
        if (animator != null) animator.SetTrigger("Run");

        while (playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) > stopDistance)
        {
            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), 10f * Time.deltaTime);
                controller.Move(toPlayer.normalized * speed * Time.deltaTime);
            }
            yield return null;
        }

        if (playerTarget == null)
        {
            _enemyState = EnemyState.Idle;
            if (animator != null) animator.SetTrigger("Idle");
            attackCoroutine = null;
            yield break;
        }

        if (EnemyManager.instance != null) EnemyManager.instance.AddAttacker(this);
        _enemyState = EnemyState.Attacking;

        if (attackingParticle != null) attackingParticle.Play();
        if (animator != null) animator.SetTrigger("Attack");

        yield return new WaitForSeconds(1f);
        TryHittingPlayer();

        yield return new WaitForSeconds(0.5f);
        if (EnemyManager.instance != null) EnemyManager.instance.RemoveAttacker(this);
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

        if (EnemyManager.instance != null) EnemyManager.instance.RemoveAttacker(this);
        if (attackingParticle != null) attackingParticle.Stop();
        _enemyState = EnemyState.Idle;
        if (animator != null) animator.SetTrigger("Idle");
    }

    private void TryHittingPlayer()
    {
        if (playerTarget == null) return;

        Vector3 attackPos = Vector3.MoveTowards(transform.position, playerTarget.position, 1f);
        Collider[] hits = Physics.OverlapSphere(attackPos, 0.7f, LayerMask.GetMask("Player"));

        foreach (Collider col in hits)
        {
            Health playerHealth = col.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.RegisterHit();
                if (audioSource != null && punchSound != null)
                {
                    audioSource.PlayOneShot(punchSound, 1f);
                }
            }
        }
    }

    public void playFootSteps()
    {
        if (audioSource != null && footStepSounds != null && footStepSounds.Length > 0)
        {
            AudioClip clip = footStepSounds[Random.Range(0, footStepSounds.Length)];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, 0.3f);
            }
        }
    }

    private void HandleMovement()
    {
        if (controller == null) return;

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        if (targetPosition == Vector3.zero) 
        {
            if (animator != null)
            {
                animator.SetFloat("X", 0f, dampTime, Time.deltaTime);
                animator.SetFloat("Y", 0f, dampTime, Time.deltaTime);
            }
            return;
        }

        Vector3 moveDir = targetPosition - transform.position;
        moveDir.y = 0;
        float distance = moveDir.magnitude;

        if (distance > 0.4f)
        {
            controller.Move(moveDir.normalized * speed * Time.deltaTime);

            Vector3 localDir = transform.InverseTransformDirection(moveDir.normalized);
            if (animator != null)
            {
                animator.SetFloat("X", localDir.x, dampTime, Time.deltaTime);
                animator.SetFloat("Y", localDir.z, dampTime, Time.deltaTime);
            }
        }
        else
        {
            targetPosition = Vector3.zero;
            _enemyState = EnemyState.Idle;
            if (animator != null)
            {
                animator.SetFloat("X", 0f, dampTime, Time.deltaTime);
                animator.SetFloat("Y", 0f, dampTime, Time.deltaTime);
            }
        }
    }

    private void RotateTowardsPlayer()
    {
        if (playerTarget == null) return;

        Vector3 dir = playerTarget.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        }
    }

    public void Retract()
    {
        if (attackingParticle != null) attackingParticle.Stop();
        _enemyState = EnemyState.Retracting;

        if (EnemyManager.instance != null && playerTarget != null)
        {
            targetPosition = EnemyManager.instance.GetRandomPositionNearTarget(
                playerTarget.position, 5f, EnemyManager.instance.layerMask);
        }
    }

    private void OnDamaged()
    {
        StopAllCoroutines();
        attackCoroutine = null;
        StopAttacking();
        if (animator != null) animator.SetTrigger("Hit");
        _enemyState = EnemyState.Idle;
    }

    private void OnDeath()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (attackingParticle != null) attackingParticle.Stop();

        if (EnemyManager.instance != null)
        {
            EnemyManager.instance.RemoveAttacker(this);
            EnemyManager.instance.RemoveEnemy(this);
            
            if (EnemyManager.instance.AliveEnemies() <= 0 && UI_Manager.instance != null)
            {
                UI_Manager.instance.ShowWinScreen();
            }
        }
        
        if (controller != null) controller.enabled = false;
        this.enabled = false;
    }

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
