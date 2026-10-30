using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float deathAnimationLength = 1f;
    [SerializeField] private int currentHealth = 0;

    [Header("Events")]
    public UnityEvent onDamageApplied;
    public UnityEvent onDeath;

    private Animator animator;
    private bool isDead = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
    }
    
    public void RegisterHit(int damage = 1)
    {
        if (isDead) return;
        ApplyDamage(damage);
    }

    private void ApplyDamage(int damage = 1)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth > 0)
        {
            onDamageApplied?.Invoke();
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        onDeath?.Invoke();

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        Destroy(gameObject, deathAnimationLength);
    }

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
}
