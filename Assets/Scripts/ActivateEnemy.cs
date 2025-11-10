using UnityEngine;

public class ActivateEnemy : MonoBehaviour
{ 
    public AudioSource audioSource;
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        audioSource.volume = 0.05f;
        audioSource.Play();
        foreach (var enemy in EnemyManager.instance.allEnemies)
        {
            enemy.enabled = true;                   
            enemy.playerTarget = other.transform;
        }

        EnemyManager.instance.enemiesActive = true;
        gameObject.SetActive(false);
    }

}
