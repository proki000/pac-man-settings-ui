using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageLogic : MonoBehaviour
{
    // this script takes care of the 'ready!' and 'game over' messages that appear under the ghost house 


    SpriteRenderer messageBox;

    [SerializeField] Sprite readySprite;     // 'ready!' image
    [SerializeField] Sprite gameOverSprite;  // 'game over' image


    void Start()
    {
        messageBox = GetComponent<SpriteRenderer>();
        messageBox.enabled = false;  // hide the message box at the start
    }


    void HideMessageBox() => messageBox.enabled = false;
    void ShowMessageBox() => messageBox.enabled = true;


    public void ShowReady(float duration)
    {
        // show the 'ready!' message for a given duration

        ShowMessageBox();
        messageBox.sprite = readySprite;  // change the sprite to 'ready!'
        Invoke(nameof(HideMessageBox), duration);
    }

    public void ShowGameOver(float duration)
    {
        // show the 'game over' message for a given duration

        ShowMessageBox();
        messageBox.sprite = gameOverSprite;  // change the sprite to 'game over'
        Invoke(nameof(HideMessageBox), duration);
    }
}
