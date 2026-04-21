using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILogic : MonoBehaviour
{
    // this script takes care of two indicators at the bottom of the screen
    // the lives indicator (bottom-left) and the fruit bar (bottom-bar)


    [SerializeField] Sprite[] pacmanLivesSprites = new Sprite[6];  // sprites for different # lives remaining
    [SerializeField] GameObject pacmanLives;

    [SerializeField] Sprite[] fruitBarSprites = new Sprite[19];   // sprites for different levels
    [SerializeField] GameObject fruitBar;


    public void RefreshPacmanLives(int lives) 
    {
        // shows the correct sprite for given number of lives

        // clamp the index - the same thing should be displayed for 1 life and 0 lives left 
        int index = Mathf.Clamp(lives - 1, 0, pacmanLivesSprites.Length - 1);
        pacmanLives.GetComponent<SpriteRenderer>().sprite = pacmanLivesSprites[index];  // change the sprite
    }

    public void RefreshFruitBar(int level)  // levels start at level 0
    {
        // shows the correct sprite for given number of lives

        // clamp the index - the fruit bar does not change after a certain level
        int index = Mathf.Clamp(level, 0, fruitBarSprites.Length - 1);
        fruitBar.GetComponent<SpriteRenderer>().sprite = fruitBarSprites[index];  // change the sprite
    }
}
