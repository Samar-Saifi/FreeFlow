using System;
using System.Collections;
using Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

public class CombatScript : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode attackKey = KeyCode.Mouse0;
    public KeyCode counterKey = KeyCode.Mouse1;

    [Header("Combat Settings")]
    public AttackType[] attacksList;
    public AudioClip punchSound;
    public AudioClip swooshSound;
    [SerializeField] AudioClip[] footStepSounds;

    [Header("Particles")] public TrailRenderer leftTrail;
    public TrailRenderer rightTrail;
    

    [HideInInspector] public EnemyScript lockedTarget;

    private Animator animator;
    private AudioSource audioSource;
    private PlayerMovement playerMovement;
    private CharacterController characterController;
    private Health health;
    
    private CinemachineImpulseSource impulseSource;

    private Coroutine attackCoroutine;
    private Coroutine moveCoroutine;

    private int lastAttackIndex = 0;

    private bool attacking;
    private bool countering;
    private bool blocking;

    private void Start()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        characterController = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        impulseSource  =  GetComponent<CinemachineImpulseSource>();
        audioSource = GetComponent<AudioSource>();

        health.onDamageApplied.AddListener(OnGettingHit);
        health.onDeath.AddListener(OnDeath);
        
        DisableTrail();
    }

    private void Update()
    {
        animator.applyRootMotion = !(attacking || countering);

        if (Input.GetKeyDown(attackKey))
            TryAttack();

        if (Input.GetKeyDown(counterKey))
            TryCounter();
    }

    #region Events
    private void OnGettingHit()
    {
        StopAllCoroutines();
        attacking = countering = false;
        playerMovement.enabled = true;
        playerMovement.EnableMovement();
        animator.SetTrigger("Hit");
    }

    private void OnDeath()
    {
        Destroy(playerMovement);
        Destroy(EnemyManager.instance);
        Destroy(characterController);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Destroy(this);
    }
    #endregion

    #region Attack
    private void TryAttack()
    {
        if (attacking || countering || blocking)
            return;

        EnemyScript target = FindTarget();
        lockedTarget = target;

        if (target == null)
        {
            PlayAttack(null, "cross", 0);
            return;
        }
        
        PlayAttack(target, attacksList[lastAttackIndex].attackName, attacksList[lastAttackIndex].duration);
    }

    private EnemyScript FindTarget()
    {
        if (playerMovement.moveDir.magnitude < 0.2f)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 4f, EnemyManager.instance.layerMask);
            foreach (Collider hit in hits)
            {
                EnemyScript enemy = hit.GetComponent<EnemyScript>();
                if (enemy != null) return enemy;
            }
        }

        EnemyScript current = EnemyManager.instance.GetCurrentTarget();
        if (current != null) return current;

        return EnemyManager.instance.GetRandomEnemy();
    }

    private void PlayAttack(EnemyScript target, string attackName, float duration, bool retract = false)
    {
        if (target == null) return;

        StopCoroutineIfRunning(ref attackCoroutine);
        StopCoroutineIfRunning(ref moveCoroutine);

        if (target._enemyState == EnemyState.Retracting)
        {
            target.targetPosition = Vector3.zero;
            target._enemyState = EnemyState.Idle;
        }
        
        animator.SetTrigger(attackName);
        attackCoroutine = StartCoroutine(AttackRoutine(target, duration, retract));
        MoveTowardsTarget(target, duration);
    }

    private IEnumerator AttackRoutine(EnemyScript target, float duration, bool retract)
    {
        playerMovement.DisableMovement();
        playerMovement.enabled = false;
        attacking = true;
        float originalLength = 0.193f;

        audioSource.volume = 1;
        audioSource.clip = swooshSound;
        audioSource.pitch = originalLength / (duration);
        
        yield return new WaitForSeconds(0.2f);
        
        audioSource.Play();
        
        yield return new WaitForSeconds(duration - 0.2f);

        if (target != null)
        {
            
            audioSource.volume = 1;
            audioSource.clip = punchSound;
            audioSource.pitch = 1;
            audioSource.Play();
            
            target.enemyHealth.RegisterHit();

            if (target._enemyState == EnemyState.Attacking || target._enemyState == EnemyState.Running)
                target.StopAttacking();
            else if (target._enemyState == EnemyState.Retracting)
            {
                target.targetPosition = Vector3.zero;
                target._enemyState = EnemyState.Idle;
            }
        }
        
        impulseSource.m_ImpulseDefinition.m_AmplitudeGain = 0.4f;
        impulseSource.GenerateImpulse();
        yield return new WaitForSeconds(0.4f);
        impulseSource.m_ImpulseDefinition.m_AmplitudeGain = 0;
        
        attacking = countering = false;

        playerMovement.enabled = true;
        playerMovement.EnableMovement();

        if (retract && target != null)
            target.Retract();
    }
    #endregion

    #region Counter
    private void TryCounter()
    {
        if (attacking || blocking)
            return;

        EnemyScript attacker = (EnemyManager.instance.attackingEnemies.Count > 0)? EnemyManager.instance.attackingEnemies[0]:null;

        if (attacker == null)
        {
            StartBlock();
            return;
        }

        attacker.attackingParticle.Stop();
        countering = true;
        EnemyManager.instance.RemoveAttacker( attacker);

        attacker.StopAttacking();

        PlayAttack(attacker, attacksList[lastAttackIndex].attackName, attacksList[lastAttackIndex].duration, true);

        lastAttackIndex = (int)Mathf.Repeat(lastAttackIndex + 1, attacksList.Length);
    }

    private void StartBlock()
    {
        blocking = true;
        animator.SetTrigger("Block");
        StartCoroutine(BlockDuration(0.13f));
    }

    private IEnumerator BlockDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        blocking = false;
    }
    #endregion

    #region Movement
    private void MoveTowardsTarget(EnemyScript target, float duration)
    {
        if (target == null) return;

        StopCoroutineIfRunning(ref moveCoroutine);

        Vector3 direction = target.transform.position - transform.position;
        Vector3 rotation = Quaternion.LookRotation(direction).eulerAngles;
        rotation.x = rotation.z = 0;

        moveCoroutine = StartCoroutine(MoveRoutine(TargetOffset(target.transform), rotation, duration));
        lastAttackIndex = (int)Mathf.Repeat(lastAttackIndex + 1, attacksList.Length);
    }

    private IEnumerator MoveRoutine(Vector3 target, Vector3 targetRot, float duration)
    {
        EnableTrail();
        Vector3 startPos = transform.position;
        Vector3 startRot = transform.rotation.eulerAngles;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            transform.rotation = Quaternion.Euler(Vector3.Lerp(startRot, targetRot, t));

            characterController.Move(Vector3.Lerp(startPos, target, t) - transform.position);

            elapsed += Time.deltaTime;
            yield return null;
        }
        DisableTrail();
    }

    public Vector3 TargetOffset(Transform target)
    {
        return Vector3.MoveTowards(target.position, transform.position, attacksList[lastAttackIndex].distance);
    }
    #endregion

    #region Helpers
    
    public void playFootSteps()
    {
        audioSource.volume = 0.3f;
        audioSource.clip = footStepSounds[Random.Range(0, footStepSounds.Length)];
        audioSource.Play();
    }
    
    private void StopCoroutineIfRunning(ref Coroutine routine)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }
    
    public void EnableTrail()
    {
        StartCoroutine(FadeTrail(0f, 0.5f));
    }
    
    public void DisableTrail()
    {
        StartCoroutine(FadeTrail(leftTrail.time, 0f));
    }
    
    private IEnumerator FadeTrail(float start, float end)
    {
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            leftTrail.time = Mathf.Lerp(start, end, elapsed / 0.5f);
            rightTrail.time = Mathf.Lerp(start, end, elapsed / 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        leftTrail.time = end;
        rightTrail.time = end;
    }
    
    #endregion
}


[Serializable]
public struct AttackType
{
    public string attackName;
    public float duration;
    public float distance;
}