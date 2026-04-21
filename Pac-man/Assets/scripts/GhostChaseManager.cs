using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GhostChaseManager : MonoBehaviour
{
    // this script tells the ghost which tile they should target when chasing pacman

    GhostMove ghost;    // every ghost has this script
    GhostMove blinky;   // Inky uses Blinky's position to determine his target tile
    PacmanMove pacman;  

    void Start()
    {
        ghost = GetComponent<GhostMove>();
        pacman = GameObject.FindGameObjectWithTag("pacman").GetComponent<PacmanMove>();

        // find Blinky 
        foreach (GameObject ghost in GameObject.FindGameObjectsWithTag("ghost"))
        {
            GhostMove g = ghost.GetComponent<GhostMove>();
            if (g.ghostName == GhostMove.GhostName.Blinky) blinky = g;  // if its Blinky
        }
    }

    public Vector2 ChaseTargetTile()
    {
        // all of the ghosts have different chase behaviors --> they choose different target tiles
        // this function returns a position in game coordinates

        Vector3 pacmanPos = pacman.transform.position;

        switch (ghost.ghostName)
        {
            case GhostMove.GhostName.Blinky:
                return pacmanPos;  // always target pacman

            case GhostMove.GhostName.Pinky:
                // if pacman is not facing up --> target the tile 4 tiles ahead of pacman
                if (pacman.PacmanDir != PacmanMove.Direction.Up)
                {
                    return pacmanPos + 4 * pacman.InGameDirection;
                }
                // if pacman is facing up --> there was a overflow bug in the original game
                // pinky targets the tile that is 4 tiles up and 4 tiles to the left of pacman
                return pacmanPos + 4 * Vector3.up + 4 * Vector3.left;

            case GhostMove.GhostName.Inky:
                // if pacman is not facing up, then the middle tile is the tile 2 tiles ahead of pacman
                Vector3 middleTile = pacmanPos + 2 * pacman.InGameDirection;

                // if pacman is facing up, then there is a similar bug as with pinky
                if (pacman.PacmanDir == PacmanMove.Direction.Up) middleTile += 2 * Vector3.left;
                
                // draw a line from Blinky to the middle tile, then double it as a vector - this is Inky's target 
                return blinky.transform.position + 2 * (middleTile - blinky.transform.position);

            case GhostMove.GhostName.Clyde:
                float dx = Mathf.Abs(pacmanPos.x - transform.position.x);
                float dy = Mathf.Abs(pacmanPos.y - transform.position.y);
                float dist = Mathf.Sqrt(dx*dx + dy*dy);  // the distance between Clyde and pacman

                // if the distance is 8 tiles or more, then he targets pacman
                if (dist >= 8)  return pacman.transform.position;
                // if he gets too close to pacman, then he runs back to his corner = bottom left
                return new Vector2(0, -1);  
        }

        return Vector2.zero;  // this should never happen
    }
}
