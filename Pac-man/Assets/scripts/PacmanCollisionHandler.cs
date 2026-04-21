using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PacmanCollisionHandler : MonoBehaviour
{
    // this script handles all pacman collisions - with dots, fruit and ghosts


    LevelLogic levelLogic;
    PacmanMove pacman;

    void Start()
    {
        levelLogic = GameObject.FindGameObjectWithTag("logic").GetComponent<LevelLogic>();
        pacman = GetComponent<PacmanMove>();
    }

    // use the built-in collision system
    private void OnTriggerEnter2D(Collider2D collision)
    {
        switch (collision.gameObject.tag)
        {
            case "dot":   // dot collision
                Destroy(collision.gameObject);    // delete the dot
                levelLogic.EatDot();              // tell level logic about it
                pacman.StopPacmanForNumFrames(1); // pacman should stop moving for 1 frame
                return;

            case "powerDot":  // power dot collision
                Destroy(collision.gameObject);     // delete the power dot
                levelLogic.EatDot(powerDot: true); // tell level logic that it was a power dot
                pacman.StopPacmanForNumFrames(3);  // pacman should stop moving for 3 frames
                return;

            case "fruit":  // fruit collision
                levelLogic.EatFruit();
                return;

            case "ghost":  // ghost collision
                GhostMove ghost = collision.gameObject.GetComponent<GhostMove>();
                switch (ghost.ghostMode)
                {
                    case GhostMove.GhostMode.Chase:  // if the ghost isn't frightened or dead he kill pacman
                    case GhostMove.GhostMode.Scatter:
                        if (GameSettings.Invincible) return;
                        levelLogic.PacmanDied();     
                        return;
                    case GhostMove.GhostMode.Fright: // if the ghost is frightened, pacman eats him
                        levelLogic.GhostDied(ghost.ghostName);
                        return;
                }
                return;
        }
    }
}
