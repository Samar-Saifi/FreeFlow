using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    private Image image;
    
    public void FadeIn(float duration=2f)
    {
        Color color = image.color;
        color.a = 1;
        
        LeanTween.value(1,0, duration).setOnUpdate((float val)=>
        {
            color.a = val;
            image.color = color;
        });
    }
    
    public void FadeOut(float duration=2f)
    {
        Color color = image.color;
        color.a = 0;
        
        LeanTween.value(0,1, duration).setOnUpdate((float val)=>
        {
            color.a = val;
            image.color = color;
        });

        LeanTween.delayedCall(duration+0.1f, () =>
        {
            Destroy(gameObject);
            SceneManager.LoadScene("MainScene");
        });
    }

    void Start()
    {
        image = GetComponent<Image>();
        FadeIn(2);
    }
}
