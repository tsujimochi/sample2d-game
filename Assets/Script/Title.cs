using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Title : MonoBehaviour
{
    [Header("フェード")] public FadeImage fade;

    private bool firstPush = false; 
    private bool goNextScene = false;


    /**
     * スタートボタンが押されたら呼び出される処理
     */
    public void PressStart()
    {
        if (!firstPush) {
            fade.StartFadeOut();
            firstPush = true;
        }
    }

    private void Update() 
    {
        if (!goNextScene && fade.IsFadeOutComplete()) {
            SceneManager.LoadScene("stage1");
            goNextScene = true;
        }
    }
}
