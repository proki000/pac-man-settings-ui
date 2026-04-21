using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelEndAnimator : MonoBehaviour
{
    // this script takes care of the animation that plays when a level is completed

    Animator animator;
    SpriteRenderer sprite;

    const int endAnimationNumFrames = 8;
    const float animationSampleRate = 4;  // frames per seconds
    const float endAnimationDuration = endAnimationNumFrames / animationSampleRate;

    // we use animation names to tell the Animator what animation it should play
    const string endLevelAnimation = "end-level";  // the blue maze flashes white a few times
    const string doNothingAnimation = "Empty";     // this maze turns blue again

    void Start()
    {
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void PlayEndAnimation()
    {
        if (GameSettings.ReduceFlashing)
        {
            StopEndAnimation();
            return;
        }

        animator.Play(endLevelAnimation);  // play the animation
        Invoke(nameof(StopEndAnimation), endAnimationDuration);  // turn the maze blue again after it ends
    }

    void StopEndAnimation()
    {
        animator.Play(doNothingAnimation);  // turn the maze blue
    }

    public float EndLevel(float delay)
    { 
        // plays the end animation and returns its duration in seconds
        Invoke(nameof(PlayEndAnimation), delay);

        return endAnimationDuration;
    }
}
