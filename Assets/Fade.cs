using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    private Image image;
    
    public void FadeIn(float duration = 2f)
    {
        if (image == null) image = GetComponent<Image>();
        if (image == null) return;

        Color color = image.color;
        color.a = 1;
        image.color = color;
        
        LeanTween.value(gameObject, 1, 0, duration).setOnUpdate((float val) =>
        {
            if (image != null)
            {
                color.a = val;
                image.color = color;
            }
        });
    }
    
    public void FadeOut()
    {
        FadeOut("MainScene", 2f);
    }

    public void FadeOut(float duration)
    {
        FadeOut("MainScene", duration);
    }

    public void FadeOut(string sceneName, float duration = 2f)
    {
        if (image == null) image = GetComponent<Image>();
        if (image == null) return;

        Color color = image.color;
        color.a = 0;
        image.color = color;
        
        LeanTween.value(gameObject, 0, 1, duration).setOnUpdate((float val) =>
        {
            if (image != null)
            {
                color.a = val;
                image.color = color;
            }
        });

        LeanTween.delayedCall(gameObject, duration + 0.1f, () =>
        {
            SceneManager.LoadScene(string.IsNullOrEmpty(sceneName) ? "MainScene" : sceneName);
        });
    }

    private void Start()
    {
        image = GetComponent<Image>();
        FadeIn(2);
    }
}
