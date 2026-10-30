using UnityEngine;

public class ActivateEnemy : MonoBehaviour
{ 
    public AudioSource audioSource;
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        if (audioSource != null)
        {
            audioSource.volume = 0.05f;
            audioSource.Play();
        }

        if (EnemyManager.instance != null)
        {
            foreach (var enemy in EnemyManager.instance.allEnemies)
            {
                if (enemy != null)
                {
                    enemy.enabled = true;                   
                    enemy.playerTarget = other.transform;
                }
            }

            EnemyManager.instance.enemiesActive = true;
        }

        gameObject.SetActive(false);
    }
}
