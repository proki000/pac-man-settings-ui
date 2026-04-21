using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostAnimator : MonoBehaviour
{
    // this script manages all animations of a single ghost

    GhostMove ghost;
    Animator animator;
    LevelLogic levelLogic;
    GhostLogic ghostLogic;

    // we use animation names to tell the Animator what animation it should play

    // this array is first indexed by GhostName and then by Direction
    // GhostMove.Direction: 0=up, 1=left, 2=down, 3=right
    // GhostMove.GhostName: 0=Blinky, 1=Pinky, 2=Inky, 3=Clyde
    readonly string[,] normalAnimation = {
        { "blinky-up", "blinky-left", "blinky-down", "blinky-right" },
        { "pinky-up", "pinky-left", "pinky-down", "pinky-right" },
        { "inky-up", "inky-left", "inky-down", "inky-right" },
        { "clyde-up", "clyde-left", "clyde-down", "clyde-right" }
    };

    readonly string[] deadAnimation = { "dead-up", "dead-left", "dead-down", "dead-right" };  // dead ghosts = eyes
    const string frightNormal = "fright";  // frightened ghost are blue
    const string frightFlash = "flash";    // before exiting the fright mode, they flash white a few times

    string currentState;  // remember the current animation

    void Start()
    {
        ghost = GetComponent<GhostMove>();
        animator = GetComponent<Animator>();
        levelLogic = GameObject.FindGameObjectWithTag("logic").GetComponent<LevelLogic>();
        ghostLogic = GameObject.FindGameObjectWithTag("logic").GetComponent<GhostLogic>();
    }

    void ChangeAnimationState(string newState)
    {
        // stop the same animation from interrupting itself
        if (currentState == newState) return;

        // play the animation
        animator.enabled = true;
        animator.Play(newState);
        currentState = newState;
    }

    // ghosts flash white a certain number of times before exiting the fright mode
    readonly int[] frightNumOfFlashes = { 5, 5, 5, 5, 5, 5, 5, 5, 3, 5, 5, 3, 3, 5, 3, 3, 0, 3, 0 };
    const float flashDuration = 0.2f;  // how long will the ghost be white / blue

    float FlashAnimationDuration()
    {
        // if the number of flashes is 3, the animation goes like this: W B W B W

        // after a certain level, the ghosts don't become frightened at all, so the number of flashes is always 0 
        int level = Mathf.Clamp(levelLogic.Level, 0, frightNumOfFlashes.Length - 1);  
        int numFlashes = frightNumOfFlashes[level];   // how many times will the ghost turn white
        return (2 * numFlashes - 1) * flashDuration;
    }


    void Update()
    {
        animator.enabled = !levelLogic.GameFrozen;  // stop the animation if the game is frozen


        // if the ghost is frightened
        if (ghost.ghostMode == GhostMove.GhostMode.Fright)
        {
            if (!GameSettings.ReduceFlashing && ghostLogic.TimeUntilFrightModeEnd() <= FlashAnimationDuration())  // if the fright mode is about to end
                ChangeAnimationState(frightFlash);  // play the flash animation
            else
                ChangeAnimationState(frightNormal); // play the fright animation
        }
        // if the ghost is dead
        else if (ghost.ghostMode == GhostMove.GhostMode.Dead)
        {
            string state = deadAnimation[(int)ghost.GhostDir];  
            ChangeAnimationState(state);
        }
        // if the ghost is alive and chasing pacman
        else
        {
            string state = normalAnimation[(int)ghost.ghostName, (int)ghost.GhostDir];
            ChangeAnimationState(state);
        }
    }
}
