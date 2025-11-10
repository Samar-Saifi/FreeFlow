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

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        playerhealth = GetComponent<Health>();
        playerhealth.onDamageApplied.AddListener(CheckHealth);
        playerhealth.onDeath.AddListener(OnDeath);

        volume.profile.TryGetSettings(out vignette);
    }

    private void CheckHealth()
    {
        Image heart = hearts[hearts.Count - 1];
        hearts.Remove(heart);
        Destroy(heart.gameObject);
        float currentHealthPercent = playerhealth.CurrentHealth / playerhealth.MaxHealth;

        if (currentHealthPercent < 1f)
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
                    var color = healthVingette.color;
                    color.a = val;
                    healthVingette.color = color;
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

                var color = healthVingette.color;
                color.a = 0f;
                healthVingette.color = color;
            }
        }
    }

    public void ShowWinScreen()
    {
        Destroy(GetComponent<PlayerMovement>());
        Destroy(GetComponent<CombatScript>());
        healthVingette.enabled = false;
        winScreen.gameObject.SetActive(true);
        StartCoroutine(FadeVignette(0.5f, 1.5f));
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private IEnumerator FadeVignette(float target, float duration)
    {
        vignette.color.value = new Color32(0,21,171,255);
        float start = vignette.intensity.value;
        float smoothnessValue = vignette.smoothness.value;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            vignette.smoothness.value = Mathf.Lerp(smoothnessValue, 0.213f, elapsed/duration);
            vignette.intensity.value = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        vignette.intensity.value = target;
    }


    private void OnDeath()
    {
        Destroy(hearts[0].gameObject);
        hearts.Remove(hearts[0]);
        if (breathingTween != null)
        {
            LeanTween.cancel(breathingTween.uniqueId);
            breathingTween = null;
        }
        
        deathScreen.gameObject.SetActive(true);
        var color = healthVingette.color;
        color.a = 1f;
        healthVingette.color = color;
    }
}
