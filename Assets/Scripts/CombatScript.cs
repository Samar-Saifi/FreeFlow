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
        impulseSource = GetComponent<CinemachineImpulseSource>();
        audioSource = GetComponent<AudioSource>();

        if (health != null)
        {
            health.onDamageApplied.AddListener(OnGettingHit);
            health.onDeath.AddListener(OnDeath);
        }
        
        DisableTrail();
    }

    private void Update()
    {
        if (animator != null)
        {
            animator.applyRootMotion = !(attacking || countering);
        }

        if (Input.GetKeyDown(attackKey))
            TryAttack();

        if (Input.GetKeyDown(counterKey))
            TryCounter();
    }

    private void OnGettingHit()
    {
        StopCoroutineIfRunning(ref attackCoroutine);
        StopCoroutineIfRunning(ref moveCoroutine);
        attacking = countering = false;

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            playerMovement.EnableMovement();
        }

        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    private void OnDeath()
    {
        StopCoroutineIfRunning(ref attackCoroutine);
        StopCoroutineIfRunning(ref moveCoroutine);
        attacking = countering = false;

        if (playerMovement != null) playerMovement.enabled = false;
        if (characterController != null) characterController.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        this.enabled = false;
    }

    private void TryAttack()
    {
        if (attacking || countering || blocking)
            return;

        EnemyScript target = FindTarget();
        lockedTarget = target;

        if (target == null)
        {
            PlayAirAttack("cross", 0.4f);
            return;
        }

        if (attacksList == null || attacksList.Length == 0) return;
        PlayAttack(target, attacksList[lastAttackIndex].attackName, attacksList[lastAttackIndex].duration);
    }

    private EnemyScript FindTarget()
    {
        if (EnemyManager.instance == null) return null;

        if (playerMovement != null && playerMovement.moveDir.magnitude < 0.2f)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 4f, EnemyManager.instance.layerMask);
            foreach (Collider hit in hits)
            {
                EnemyScript enemy = hit.GetComponent<EnemyScript>();
                if (enemy != null && enemy.enabled) return enemy;
            }
        }

        EnemyScript current = EnemyManager.instance.GetCurrentTarget();
        if (current != null && current.enabled) return current;

        return EnemyManager.instance.GetRandomEnemy();
    }

    private void PlayAirAttack(string attackName, float duration)
    {
        StopCoroutineIfRunning(ref attackCoroutine);
        StopCoroutineIfRunning(ref moveCoroutine);

        if (animator != null) animator.SetTrigger(attackName);
        attackCoroutine = StartCoroutine(AirAttackRoutine(duration));
    }

    private IEnumerator AirAttackRoutine(float duration)
    {
        if (playerMovement != null)
        {
            playerMovement.DisableMovement();
            playerMovement.enabled = false;
        }

        attacking = true;

        if (audioSource != null && swooshSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(swooshSound, 1f);
        }

        yield return new WaitForSeconds(duration);

        attacking = false;

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            playerMovement.EnableMovement();
        }
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
        
        if (animator != null) animator.SetTrigger(attackName);
        attackCoroutine = StartCoroutine(AttackRoutine(target, duration, retract));
        MoveTowardsTarget(target, duration);
    }

    private IEnumerator AttackRoutine(EnemyScript target, float duration, bool retract)
    {
        if (playerMovement != null)
        {
            playerMovement.DisableMovement();
            playerMovement.enabled = false;
        }

        attacking = true;
        float safeDuration = Mathf.Max(0.25f, duration);
        float originalLength = 0.193f;

        if (audioSource != null && swooshSound != null)
        {
            audioSource.pitch = Mathf.Clamp(originalLength / safeDuration, 0.5f, 2f);
            audioSource.PlayOneShot(swooshSound, 1f);
        }
        
        yield return new WaitForSeconds(0.15f);
        
        yield return new WaitForSeconds(Mathf.Max(0.05f, safeDuration - 0.15f));

        if (target != null)
        {
            if (audioSource != null && punchSound != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(punchSound, 1f);
            }
            
            if (target.enemyHealth != null)
            {
                target.enemyHealth.RegisterHit();
            }

            if (target._enemyState == EnemyState.Attacking || target._enemyState == EnemyState.Running)
                target.StopAttacking();
            else if (target._enemyState == EnemyState.Retracting)
            {
                target.targetPosition = Vector3.zero;
                target._enemyState = EnemyState.Idle;
            }
        }
        
        if (impulseSource != null)
        {
            impulseSource.m_ImpulseDefinition.m_AmplitudeGain = 0.4f;
            impulseSource.GenerateImpulse();
            yield return new WaitForSeconds(0.4f);
            impulseSource.m_ImpulseDefinition.m_AmplitudeGain = 0;
        }
        
        attacking = countering = false;

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            playerMovement.EnableMovement();
        }

        if (retract && target != null)
            target.Retract();
    }

    private void TryCounter()
    {
        if (attacking || blocking || EnemyManager.instance == null)
            return;

        EnemyScript attacker = (EnemyManager.instance.attackingEnemies.Count > 0) ? EnemyManager.instance.attackingEnemies[0] : null;

        if (attacker == null)
        {
            StartBlock();
            return;
        }

        if (attacker.attackingParticle != null)
        {
            attacker.attackingParticle.Stop();
        }

        countering = true;
        EnemyManager.instance.RemoveAttacker(attacker);
        attacker.StopAttacking();

        if (attacksList != null && attacksList.Length > 0)
        {
            PlayAttack(attacker, attacksList[lastAttackIndex].attackName, attacksList[lastAttackIndex].duration, true);
        }
    }

    private void StartBlock()
    {
        blocking = true;
        if (animator != null) animator.SetTrigger("Block");
        StartCoroutine(BlockDuration(0.13f));
    }

    private IEnumerator BlockDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        blocking = false;
    }

    private void MoveTowardsTarget(EnemyScript target, float duration)
    {
        if (target == null || attacksList == null || attacksList.Length == 0) return;

        StopCoroutineIfRunning(ref moveCoroutine);

        Vector3 direction = target.transform.position - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            Vector3 rotation = Quaternion.LookRotation(direction).eulerAngles;
            rotation.x = rotation.z = 0;

            moveCoroutine = StartCoroutine(MoveRoutine(TargetOffset(target.transform), rotation, duration));
        }

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

            if (characterController != null)
            {
                characterController.Move(Vector3.Lerp(startPos, target, t) - transform.position);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        DisableTrail();
    }

    public Vector3 TargetOffset(Transform target)
    {
        if (target == null) return transform.position;
        float distance = (attacksList != null && attacksList.Length > 0) ? attacksList[lastAttackIndex].distance : 1.5f;
        return Vector3.MoveTowards(target.position, transform.position, distance);
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
        if (leftTrail != null && rightTrail != null)
        {
            StartCoroutine(FadeTrail(0f, 0.5f));
        }
    }
    
    public void DisableTrail()
    {
        if (leftTrail != null && rightTrail != null)
        {
            StartCoroutine(FadeTrail(leftTrail.time, 0f));
        }
    }
    
    private IEnumerator FadeTrail(float start, float end)
    {
        if (leftTrail == null || rightTrail == null) yield break;

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            float val = Mathf.Lerp(start, end, elapsed / 0.5f);
            leftTrail.time = val;
            rightTrail.time = val;
            elapsed += Time.deltaTime;
            yield return null;
        }
        leftTrail.time = end;
        rightTrail.time = end;
    }
}

[Serializable]
public struct AttackType
{
    public string attackName;
    public float duration;
    public float distance;
}