using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class UI_Manager : MonoBehaviour
{
    public Image healthVingette;
    public Transform deathScreen;
    public Transform winScreen;
    public List<Image> hearts;
    
    private Health playerhealth;
    private LTDescr breathingTween;
    public static UI_Manager instance;
    public PostProcessVolume volume;
    private Vignette vignette;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        playerhealth = GetComponent<Health>();
        if (playerhealth != null)
        {
            playerhealth.onDamageApplied.AddListener(CheckHealth);
            playerhealth.onDeath.AddListener(OnDeath);
        }

        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGetSettings(out vignette);
        }
    }

    private void CheckHealth()
    {
        if (hearts != null && hearts.Count > 0)
        {
            Image heart = hearts[hearts.Count - 1];
            hearts.RemoveAt(hearts.Count - 1);
            if (heart != null)
            {
                Destroy(heart.gameObject);
            }
        }

        if (playerhealth == null || playerhealth.MaxHealth <= 0) return;
        float currentHealthPercent = playerhealth.CurrentHealth / playerhealth.MaxHealth;

        if (currentHealthPercent < 1f && healthVingette != null)
        {
            float minAlpha = Mathf.Clamp01((1 - currentHealthPercent) - 0.17f);
            float maxAlpha = Mathf.Clamp01((1 - currentHealthPercent) + 0.17f);

            if (breathingTween != null)
            {
                LeanTween.cancel(breathingTween.uniqueId);
            }

            breathingTween = LeanTween.value(gameObject, minAlpha, maxAlpha, 1f)
                .setOnUpdate((float val) =>
                {
                    if (healthVingette != null)
                    {
                        var color = healthVingette.color;
                        color.a = val;
                        healthVingette.color = color;
                    }
                })
                .setEaseInOutSine()
                .setLoopPingPong();
        }
        else
        {
            if (breathingTween != null)
            {
                LeanTween.cancel(breathingTween.uniqueId);
                breathingTween = null;
            }

            if (healthVingette != null)
            {
                var color = healthVingette.color;
                color.a = 0f;
                healthVingette.color = color;
            }
        }
    }

    public void ShowWinScreen()
    {
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        var combat = GetComponent<CombatScript>();
        if (combat != null) combat.enabled = false;

        if (healthVingette != null) healthVingette.enabled = false;
        if (winScreen != null) winScreen.gameObject.SetActive(true);

        if (vignette != null)
        {
            StartCoroutine(FadeVignette(0.5f, 1.5f));
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private IEnumerator FadeVignette(float target, float duration)
    {
        if (vignette == null) yield break;

        vignette.color.value = new Color32(0, 21, 171, 255);
        float start = vignette.intensity.value;
        float smoothnessValue = vignette.smoothness.value;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            vignette.smoothness.value = Mathf.Lerp(smoothnessValue, 0.213f, elapsed / duration);
            vignette.intensity.value = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        vignette.intensity.value = target;
    }

    private void OnDeath()
    {
        if (hearts != null && hearts.Count > 0)
        {
            Image heart = hearts[0];
            hearts.RemoveAt(0);
            if (heart != null)
            {
                Destroy(heart.gameObject);
            }
        }

        if (breathingTween != null)
        {
            LeanTween.cancel(breathingTween.uniqueId);
            breathingTween = null;
        }
        
        if (deathScreen != null)
        {
            deathScreen.gameObject.SetActive(true);
        }

        if (healthVingette != null)
        {
            var color = healthVingette.color;
            color.a = 1f;
            healthVingette.color = color;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (breathingTween != null)
        {
            LeanTween.cancel(breathingTween.uniqueId);
            breathingTween = null;
        }

        if (instance == this)
        {
            instance = null;
        }
    }
}
