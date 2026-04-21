using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;


public class GhostLogic : MonoBehaviour
{
    // this scripts takes care of ghost behavior - setting ghost modes and leaving the ghost house

    LevelLogic levelLogic;
    AudioLogic audioPlayer;

    // array of ghosts indexed by GhostMove.GhostName
    // GhostMove.GhostName: 0=Blinky, 1=Pinky, 2=Inky, 3=Clyde
    const int numGhosts = 4;
    readonly GhostMove[] ghosts = new GhostMove[numGhosts];  

    void Start()
    {
        levelLogic = gameObject.GetComponent<LevelLogic>();
        audioPlayer = GameObject.FindGameObjectWithTag("audio").GetComponent<AudioLogic>();

        // find ghosts 
        foreach (GameObject ghost in GameObject.FindGameObjectsWithTag("ghost"))
        {
            GhostMove g = ghost.GetComponent<GhostMove>();
            ghosts[(int)g.ghostName] = g;   // the array will be indexable by GhostMove.GhostName
        }
    }



    // LEAVING THE GHOST HOUSE LOGIC

    // the array is indexed by GhostMove.GhostName --> 0=Blinky, 1=Pinky, 2=Inky, 3=Clyde
    bool[] ghostsAtHome = new bool[numGhosts];  // is the ghost inside of the ghost house?
    public bool IsGhostAtHome(GhostMove.GhostName name) => ghostsAtHome[(int)name];
    public void GhostArrivedHome(GhostMove.GhostName name) => ghostsAtHome[(int)name] = true;


    // ghosts use local counters and a global counter as well as a timer to determine when to leave the house

    // indexed by GhostName --> 0=Blinky, 1=Pinky, 2=Inky, 3=Clyde
    int[] ghostCounters = { 0, 0, 0, 0 };   // dot counters for each ghost
    // indexed by level and then by GhostName
    // level 1: Blinky and Pinky will leave immediately, Inky will leave after pacman eats 30 dots and Clyde after 60 more
    readonly int[,] ghostCounterLimits = { { 0, 0, 30, 60 }, { 0, 0, 0, 50 }, { 0, 0, 0, 0 } }; 

    // global counter is used when pacman dies
    int globalCounter = 0;
    // Blinky leaves immediately, Pinky after 7 dots, Inky after 10 more and Clyde after 15 more
    readonly int[] globalCounterLimits = { 0, 7, 17, 32 }; 
    bool globalCounterActive = false;

    // ghosts also start leaving the house when pacman stops eating dots
    float timeSinceLastDotEaten = 0;
    const float ghostWaitForDotTimeLimit = 4;

    public void DotEaten(bool powerDot)
    {
        // this function is called whenever pacman eats a dot
        if (powerDot) SetFrightMode();  // set fright mode if it was a power dot

        timeSinceLastDotEaten = 0;   // refresh the timer

        // if the global counter is active
        if (globalCounterActive)  
        {
            ++globalCounter;
            return;
        }

        // if the local counters are active
        for (int i = 0; i < numGhosts; i++)
        {
            if (ghostsAtHome[i])
            {
                // increase the counter of the ghost with the highest priority and return
                ++ghostCounters[i];   
                return;
            }
        }
    }

    void GhostHouseLogic()
    {
        // this function tells the ghosts when they should leave the ghost house 
        // it is called in every Update()

        // blinky always leaves immediately after arriving home
        if (ghostsAtHome[(int)GhostMove.GhostName.Blinky]) 
        {
            ghosts[(int)GhostMove.GhostName.Blinky].LeaveHouse();
            ghostsAtHome[(int)GhostMove.GhostName.Blinky] = false;
        }

        // if pacman stopped eating dots
        if (timeSinceLastDotEaten >= ghostWaitForDotTimeLimit)
        {
            timeSinceLastDotEaten -= ghostWaitForDotTimeLimit;
            for (int i = 0; i < numGhosts; i++)
            {
                // tell the ghost with the highest priority to leave the house
                if (ghostsAtHome[i])
                {
                    ghosts[i].LeaveHouse();
                    ghostsAtHome[i] = false;
                    break;
                }
            }
        }

        // if pacman dies the global counter is activated
        if (globalCounterActive)
        {
            for (int i = 0; i < numGhosts; i++)
            {
                if (!ghostsAtHome[i]) continue;  // if the ghost already left --> skip

                // if the ghost should leave
                if (globalCounter == globalCounterLimits[i])
                {
                    ghosts[i].LeaveHouse();
                    ghostsAtHome[i] = false;
                    // deactivate global counter only if Clyde leaves while using it --> this can be exploited in game
                    if (i == (int)GhostMove.GhostName.Clyde) globalCounterActive = false;
                }
            }
        }
        // every ghost uses his own dot counter if the global counter is not active
        else
        {
            for (int i = 0; i < numGhosts; i++)
            {
                if (ghostsAtHome[i])  
                {
                    // tell the ghost with the highest priority to leave the house if pacman ate enough dots
                    int level = Mathf.Clamp(levelLogic.Level, 0, ghostCounterLimits.GetLength(0) - 1);
                    if (ghostCounters[i] >= ghostCounterLimits[level, i])
                    {
                        ghosts[i].LeaveHouse();
                        ghostsAtHome[i] = false;
                    }
                }
            }
        }
    }



    // SCATTER CHASE LOGIC

    // ghosts alter between scatter and chase modes - in chase they target pacman, in scatter they don't

    // the table is indexed by level
    // S,C,S,C,S,C,S --> even = S, odd = C
    readonly float[,] scatterChaseTimeTable = {
        { 7, 20, 7, 20, 5, 20, 5 },
        { 7, 20, 7, 20, 5, 1033, 1f/60f },
        { 7, 20, 7, 20, 5, 1033, 1f/60f },
        { 7, 20, 7, 20, 5, 1033, 1f/60f },   
        { 5, 20, 5, 20, 5, 1037, 1f/60f },
    };


    bool scatter = true;  // ghosts start in the scatter mode
    float scatterChaseTimer = 0;

    void SetScatterMode()
    {
        // tells all of the ghosts to change their mode to scatter
        if (scatter) return;

        scatter = true;
        foreach (GhostMove ghost in ghosts)
        {
            ghost.ghostMode = GhostMove.GhostMode.Scatter;  // change mode to scatter
            ghost.ReverseDirection();   // reverse the ghost's direction
        }
    }

    void SetChaseMode()
    {
        // tells all of the ghosts to change their mode to chase

        if (!scatter) return;  // if ghosts are already chasing

        scatter = false;
        foreach (GhostMove ghost in ghosts)
        {
            ghost.ghostMode = GhostMove.GhostMode.Chase;  // change mode to scatter
            ghost.ReverseDirection();    // reverse the ghost's direction
        }
    }

    void ScatterChaseLogic()
    {
        // this function tells the ghosts whether they should be chasing pacman or not

        // find out in what period of the time table the game is at
        float runningSum = 0;
        for (int i = 0; i < scatterChaseTimeTable.GetLength(1); i++)
        {
            int level = Mathf.Clamp(levelLogic.Level, 0, scatterChaseTimeTable.GetLength(0) - 1);
            runningSum += scatterChaseTimeTable[level, i];   // add period duration
            
            if (scatterChaseTimer < runningSum)  // if the game is in this period
            {
                if (i % 2 == 0) SetScatterMode();   // even periods are scatter
                else SetChaseMode();                // odd ones are chase
                return;
            }
        }

        // if the scatter-chase-repeat period has ended --> indefinite chase
        SetChaseMode();
    }


    // FRIGHT MODE LOGIC

    // ghosts get frightened when pacman eats a power dot
    // they get frightened for a shorter duration in later levels and eventually just reverse direction
    readonly int[] frightDuration = { 6, 5, 4, 3, 2, 5, 2, 2, 1, 5, 2, 1, 1, 3, 1, 1, 0, 1, 0 };
    float frightTimer = 0;

    bool frightened = false;
    public bool FrightMode => frightened;

    int ghostsEaten = 0;   // num ghosts eaten in this fright mode


    public float TimeUntilFrightModeEnd() 
    {
        // returns the time until the end of the fright mode
        int level = Mathf.Clamp(levelLogic.Level, 0, frightDuration.Length - 1);
        return frightDuration[level] * GameSettings.FrightDurationMultiplier - frightTimer;
    }

    void SetFrightMode()
    {
        // this function is called whenever a power dot is eaten
        frightened = true;
        frightTimer = 0;
        ghostsEaten = 0;

        // frighten the ghosts
        foreach (GhostMove ghost in ghosts) ghost.FrightenGhost();

        audioPlayer.FrightModeStarted();
    }

    void DisableFrightMode()
    {
        // this function is called when the fright mode ends
        frightened = false;

        // tell ghosts to exit fright mode
        foreach (GhostMove ghost in ghosts) ghost.UnFrightenGhost();

        audioPlayer.FrightModeEnded();
    }

    public int GhostDied(GhostMove.GhostName name, float gameFreezeDuration)
    {
        // ghosts can be eaten when they are frightened
        // the game freezes and displays points gained briefly - the ghost returns to the ghost house after that
        ++ghostsEaten;

        // kill the ghost
        ghosts[(int)name].KillGhost(ghostsEaten, gameFreezeDuration);

        // first ghost = 200, second = 400, third = 800, fourth = 1600
        int points = 100 * (int)Mathf.Pow(2, ghostsEaten);
        return points;
    }


    // PACMAN'S DEATH LOGIC
    // ghosts stop for a moment and then become invisible while the death animation plays

    void StopGhosts()
    {
        // stops ghost movement
        foreach (GhostMove ghost in ghosts) ghost.StopGhost();
    }

    void HideGhosts()
    {
        // makes all of the ghosts invisible
        foreach (GhostMove ghost in ghosts) ghost.HideGhost();
    }

    public void StopAndHideGhosts(float hideDelay)
    {
        // stops all of the ghosts and makes them invisible after a delay
        StopGhosts();
        Invoke(nameof(HideGhosts), hideDelay);
    }


    
    public Random rng;  // frightened ghosts move randomly
    public void SpawnGhosts(bool newLevel = false)
    {
        // this function is called after pacman dies and at the start of every level

        // use the same seed every time --> the same pattern will have the same effect every time
        rng = new Random(161803);

        for (int i = 0; i < numGhosts; i++)
        {
            // Blinky starts outside of the ghost house, others inside
            ghostsAtHome[i] = !((GhostMove.GhostName)i == GhostMove.GhostName.Blinky);
            ghosts[i].SpawnGhost();   // spawn the ghost

            if (newLevel) ghostCounters[i] = 0;  // reset dot counter at the start of a new level
        }

        DisableFrightMode();    // turn off the fright mode

        // reset ghost behavior variables to defaults
        scatter = true;      
        scatterChaseTimer = 0;
        timeSinceLastDotEaten = 0;

        // reset the global counter
        globalCounter = 0;
        globalCounterActive = !newLevel;  // activate the global counter if pacman died
    }



    void Update()
    {
        // don't count time if the game is frozen
        if (levelLogic.GameFrozen) return;

        // handle ghost house logic
        timeSinceLastDotEaten += Time.deltaTime;
        GhostHouseLogic();


        if (frightened)  // if the fright mode is active
        {
            int level = Mathf.Clamp(levelLogic.Level, 0, frightDuration.Length - 1);

            frightTimer += Time.deltaTime;
            if (frightTimer >= frightDuration[level] * GameSettings.FrightDurationMultiplier)
            {
                DisableFrightMode();  // disable the mode if it has ended
            }
        }
        else  // if ghosts are not frightened
        {
            scatterChaseTimer += Time.deltaTime;
            ScatterChaseLogic();
        }
    }
}
