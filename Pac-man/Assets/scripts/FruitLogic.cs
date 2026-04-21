using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitLogic : MonoBehaviour
{
    // this script takes care of fruit that spawns after the first 70 and 170 dots are eaten
    // pacman can eat the fruit to increase the score


    [SerializeField] Sprite[] fruitSprites = new Sprite[13];   // fruit images indexed by level
    [SerializeField] Sprite[] pointsSprites = new Sprite[13];  // a number is displayed when the fruit is eaten
    [SerializeField] GameObject fruitPrefab;

    // fruits in later levels are worth more points
    readonly int[] fruitScore = { 100, 300, 500, 500, 700, 700, 1000, 1000, 2000, 2000, 3000, 3000, 5000 };

    GameObject fruit;         // the current fruit will be stored here
    AudioLogic audioPlayer;
    LevelLogic levelLogic;

    void Start()
    {
        levelLogic = GetComponent<LevelLogic>();
        audioPlayer = GameObject.FindGameObjectWithTag("audio").GetComponent<AudioLogic>();
    }


    float fruitTimer = 0;
    float fruitTimerLimit;   // the fruit will despawn after 9 to 10 seconds
    bool spawned = false;

    readonly Vector2 spawnPos = new Vector2(14, 13.5f);  // all fruits will spawn here


    public void SpawnFruit()
    {
        // spawns a fruit for the current level

        if (spawned) return;  // this should never happen

        spawned = true;
        fruitTimer = 0;
        fruitTimerLimit = Random.Range(9, 10);  // the despawn time limit is random

        fruit = Instantiate(fruitPrefab, spawnPos, Quaternion.identity);    // spawn the fruit 
        int level = Mathf.Clamp(levelLogic.Level, 0, fruitSprites.Length - 1);         // fruits don't change after a certain level
        fruit.GetComponent<SpriteRenderer>().sprite = fruitSprites[level];  // change the sprite
    }

    public void DespawnFruit(float delay = 0)
    {
        // destroys the current fruit game object after a given delay

        spawned = false;
        if (fruit != null) Destroy(fruit, delay);
    }


    const float showPointsTime = 3f;   // after a fruit is eaten, points will be shown in its place for this long
    public int EatFruit()
    {
        // shows points gained from eating the fruit and then destroys it
        // returns the points gained

        if (!spawned) return 0;  // if the fruit isn't there, no points are gained

        spawned = false;
        int level = Mathf.Clamp(levelLogic.Level, 0, fruitSprites.Length - 1);   // fruit doesn't change after a certain level 
        fruit.GetComponent<SpriteRenderer>().sprite = pointsSprites[level];  // set the sprite to the correct points image
        
        DespawnFruit(showPointsTime);   // destroy the fruit game object after a delay
        audioPlayer.EatFruit();         // play the sound effect

        return fruitScore[level];    // return the points gained
    }



    void Update()
    {
        // if the game is frozen, time should not be counted
        if (levelLogic.GameFrozen) return;


        if (spawned)
        {
            // despawn the fruit after 'fruitTimerLimit' seconds have passed
            fruitTimer += Time.deltaTime;
            if (fruitTimer >= fruitTimerLimit) DespawnFruit(); 
        }
    }
}
