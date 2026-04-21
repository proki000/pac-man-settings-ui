using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

public class LevelLogic : MonoBehaviour
{
    // this script connects all of the other major scripts and tells them what to do
    // it handles the logic behind pacman's death, ghosts deaths, starting new levels and counting score


    MapManager mapManager;     // map
    GhostLogic ghosts;         // ghosts
    FruitLogic fruit;          // fruit
    PacmanMove pacman;         // pacman
    PacmanAnimator pacmanAnimator;

    UILogic userInterface;     // pacmans lives bar and fruit bar
    MessageLogic messageBox;   // 'ready!' and 'game over' messages
    HighScoreLogic highScoreLogic;   // highscore
    AudioLogic audioPlayer;    // sound effects
    LevelEndAnimator levelEndAnimator;   // the animation that plays when a level is completed
    [SerializeField] TextMeshProUGUI scoreLabel;   // label where the score is displayed

    private void Start()
    {
        GameSettings.Load();
        mapManager = GetComponent<MapManager>();
        ghosts = GetComponent<GhostLogic>();
        fruit = GetComponent<FruitLogic>();
        userInterface = GetComponent<UILogic>();

        pacman = GameObject.FindGameObjectWithTag("pacman").GetComponent<PacmanMove>();
        pacmanAnimator = GameObject.FindGameObjectWithTag("pacman").GetComponent<PacmanAnimator>();

        levelEndAnimator = GameObject.FindGameObjectWithTag("levelEnd").GetComponent<LevelEndAnimator>();
        highScoreLogic = GameObject.FindGameObjectWithTag("highscore").GetComponent<HighScoreLogic>();
        messageBox = GameObject.FindGameObjectWithTag("message").GetComponent<MessageLogic>();
        audioPlayer = GameObject.FindGameObjectWithTag("audio").GetComponent<AudioLogic>();
        pacmanLives = GameSettings.StartingLives;
    }


    // keep track of dots
    const int maxDots = 244;
    int dotsEaten = 0;
    public int DotsLeft => maxDots - dotsEaten;

    // remember current level - first level is 0
    int level = 0;
    public int Level => level;
    
    // remember pacman's lives
    const int maxPacmanLives = 6;
    int pacmanLives = 4;

    // count score
    int score = 0;


    // points gained from eating dots
    const int dotPoints = 10;
    const int powerDotPoints = 50;

    // fruit spawns after the first 70 and 170 dots are eaten
    readonly int[] fruitSpawnDotsEatenCounts = { 70, 170 };

    public void EatDot(bool powerDot = false)
    {
        // this function is called when pacman collides with a dot or a power dot 

        score += (powerDot) ? powerDotPoints : dotPoints;
        ghosts.DotEaten(powerDot);   // tell the ghosts about it  

        ++dotsEaten;
        if (dotsEaten == maxDots)  // if pacman ate all of the dots
        {
            ++level;
            EndLevel();    // end the level
        }

        // spawn fruit 
        if (fruitSpawnDotsEatenCounts.Contains(dotsEaten)) fruit.SpawnFruit();

        audioPlayer.EatDot();  // play the munch sound effect
    }

    public void EatFruit()
    {
        // this function is called when pacman collides with a fruit
        score += fruit.EatFruit();
    }


    // if the game is frozen, then nobody can move and no animations are being played
    // the exception are dead ghosts, who can move freely and return to the ghost house

    bool frozen = false;
    float freezeTimer = 0f;
    float freezeDuration;
    public bool GameFrozen => frozen;

    void FreezeGame(float time)
    {
        // freeze the game for a given duration
        frozen = true;
        freezeTimer = 0f;
        freezeDuration = time;
    }


    // freeze the game after a ghost is eaten - points will be showed instead
    const float ghostDeathFreezeDuration = 1f;
    const int maxGhostPoints = 1600;    // points for the fourth ghost

    public void GhostDied(GhostMove.GhostName name)
    {
        FreezeGame(ghostDeathFreezeDuration);  // freeze the game
        pacmanAnimator.HidePacman();           // hide pacman
        audioPlayer.EatGhost();  // play the ghost death sound effect

        // first ghost = 200, second = 400, third = 800, fourth = 1600
        int points = ghosts.GhostDied(name, ghostDeathFreezeDuration);
        score += points;

        // if pacman ate all 4 ghosts, then he gets an extra live 
        if (points == maxGhostPoints && pacmanLives < maxPacmanLives)
        {
            userInterface.RefreshPacmanLives(++pacmanLives);  // refresh lives bar
            audioPlayer.GainExtraLife();        // play the sound effect
        }
    }


    // ghosts will stop moving after a level is completed - they will disappear after a delay
    const float levelEndStopGhostsDuration = 2f;

    void EndLevel()
    {
        // stops everyone for a short moment, then hides ghost and starts playing the level-end animation

        ghosts.StopAndHideGhosts(levelEndStopGhostsDuration);  // stop and after a delay hide the ghosts
        fruit.DespawnFruit(levelEndStopGhostsDuration);        // despawn the fruit after a delay
        audioPlayer.EndLevel();  // stop background music

        // play the level-end animation after the ghosts hide
        float endLevelAnimationDuration = levelEndAnimator.EndLevel(levelEndStopGhostsDuration);
        // stop pacman and the ghosts from moving and playing animations
        FreezeGame(levelEndStopGhostsDuration + endLevelAnimationDuration);

        // start a new level after the level-end animation is done playing
        Invoke(nameof(StartLevel), levelEndStopGhostsDuration + endLevelAnimationDuration);
    }

    // freeze the game at the start when the 'ready!' message is displayed
    const float readyFreezeDuration = 4.3f;

    void StartLevel()
    {
        // spawn new dots
        dotsEaten = 0;  
        mapManager.SpawnDots();

        // freeze the game - the 'ready!' message will be displayed
        FreezeGame(readyFreezeDuration);

        // spawn ghosts and pacman 
        ghosts.SpawnGhosts(newLevel: true);
        pacman.SpawnPacman();

        // refresh lives and fruit bar at the bottom of the screen
        userInterface.RefreshFruitBar(level);
        userInterface.RefreshPacmanLives(pacmanLives);

        // show the ready message and play the starting melody
        messageBox.ShowReady(readyFreezeDuration);
        audioPlayer.GameStart(readyFreezeDuration);
    }




    const float pacmanDeathGhostStopTime = 1.5f;

    public void PacmanDied()
    {
        // stops everyones movement, but doesn't stop ghost's animations

        --pacmanLives;
        pacman.PacmanDied();  // stop pacmans movement
        ghosts.StopAndHideGhosts(pacmanDeathGhostStopTime);  // stop ghosts and hide them after a delay

        // do the following after the ghosts hide:

        fruit.DespawnFruit(pacmanDeathGhostStopTime);      // despawn fruit
        audioPlayer.PacmanDeath(pacmanDeathGhostStopTime); // play the death sound effect
        // play the death animation
        float pacmanDeathAnimationDuration = pacmanAnimator.PlayPacmanDeathAnimation(pacmanDeathGhostStopTime);


        if (pacmanLives == 0)  // if pacman lost his last life --> end the game
            Invoke(nameof(GameOver), pacmanDeathGhostStopTime + pacmanDeathAnimationDuration);
        else  // if pacman still has some lives left --> continue
            Invoke(nameof(ResetGhostsAndPacman), pacmanDeathGhostStopTime + pacmanDeathAnimationDuration);
    }

    void ResetGhostsAndPacman()
    {
        // freeze the game - the 'ready!' message will be displayed
        FreezeGame(readyFreezeDuration);

        // spawn ghosts and pacman 
        ghosts.SpawnGhosts();
        pacman.SpawnPacman();

        // refresh lives and fruit bar at the bottom of the screen
        userInterface.RefreshFruitBar(level);
        userInterface.RefreshPacmanLives(pacmanLives);

        // show the ready message and play the starting melody
        messageBox.ShowReady(readyFreezeDuration);
        audioPlayer.GameStart(readyFreezeDuration);


        // this function isn't called in StartLevel(), because it would require a newLevel bool parameter
        // and functions with parameters are not Invokable
    }

    const float showGameOverDuration = 3f;
    void GameOver()
    {
        // displays the 'game over' message
        // this function is called when pacman loses his last life
        messageBox.ShowGameOver(showGameOverDuration);   // display the message
        Invoke(nameof(GoBackToStartScreen), showGameOverDuration);  // ho back to start screen after it disappears
    }

    const string startSceneName = "StartScreen";
    void GoBackToStartScreen()
    {
        // load the starting scene
        SceneManager.LoadScene(startSceneName);  
    }




    // the first level starts once the game scene is loaded
    // this can not be called in Start(), because it requires other scripts to have completed their Start()
    bool startFirstLevel = true;

    void Update()
    {
        // start the first level
        if (startFirstLevel)
        {
            startFirstLevel = false;
            StartLevel();
        }

        // update the score and highscore labels
        scoreLabel.text = (score == 0) ? "00" : score.ToString();
        highScoreLogic.UpdateHighScore(score);

        // work on unfreezing the game if its frozen
        if (frozen)
        {
            freezeTimer += Time.deltaTime;
            if (freezeTimer >= freezeDuration)
            {
                frozen = false;
                // pacman is hidden when he eats a ghost and the game freezes --> show him
                pacmanAnimator.ShowPacman(); 
            }
        }
    }
}
