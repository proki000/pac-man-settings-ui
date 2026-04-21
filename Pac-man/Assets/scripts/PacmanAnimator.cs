using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacmanAnimator : MonoBehaviour
{
    // this script manages all pacman animations

    PacmanMove pacman;
    Animator animator;
    LevelLogic levelLogic;
    SpriteRenderer sprite;

    void Start()
    {
        pacman = GetComponent<PacmanMove>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        levelLogic = GameObject.FindGameObjectWithTag("logic").GetComponent<LevelLogic>();
    }


    // we use animation names to tell the Animator what animation it should play

    // movement animations array is indexed by pacmans direction
    // PacmanMove.Direction: 0=right, 1=up, 2=left, 3=down
    readonly string[] movementAnimations = { "right", "up", "left", "down" };
    const string readyAnimation = "ready";   // just a sprite of pacman as a full circle
    const string deathAnimation = "death";

    // calculate the duration of pacmans death animation
    const int deathAnimationNumFrames = 16;
    const float animationSampleRate = 10;    // frames per second
    const float deathAnimationDuration = deathAnimationNumFrames / animationSampleRate;

    string currentState;  // remember the current animation
    Vector2 lastPos;      // remember pacmans last position --> the animation should stop when he stops moving

    void EnableAnimator() => animator.enabled = true;

    void ChangeAnimationState(string newState)
    {
        // stop the same animation from interrupting itself
        if (currentState == newState) return;

        // play the animation
        EnableAnimator();
        animator.Play(newState);
        currentState = newState;
    }


    // hide pacman for a short moment if he eats a ghost - points will be shown instead
    public void HidePacman() => sprite.enabled = false;
    public void ShowPacman() => sprite.enabled = true;

    public void SpawnPacman()
    {
        // pacman should turn into a full circle when he spawns
        ChangeAnimationState(readyAnimation);
        lastPos = transform.position;
        ShowPacman();
    }

    public float PlayPacmanDeathAnimation(float delay)
    {
        // stops pacman and after a given delay plays the death animation
        // returns the death animation duration in seconds

        ChangeAnimationState(deathAnimation);  // set the death animation

        animator.enabled = false;    // stop the animation before it even starts
        Invoke(nameof(EnableAnimator), delay);  // let it play after a given delay

        return deathAnimationDuration;  
    }

    void Update()
    {
        // if pacman dies, the PlayPacmanDeathAnimation() is called and takes care of the rest
        if (pacman.PacmanDead) return;

        Vector2 newPos = transform.position;
        animator.enabled = !(lastPos == newPos);  // stop the movement animation when pacman hits a wall
        lastPos = newPos;

        ChangeAnimationState(movementAnimations[(int)pacman.PacmanDir]);  // walking animation
    }
}
