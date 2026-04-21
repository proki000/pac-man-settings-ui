using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GhostMove;

public class MovementManager : MonoBehaviour
{
    // this script tells pacman and the ghosts how fast they should be moving 


    const float maxSpeed = 75.75757625f / 8f;   // 75.75757625 pixels per second when moving at 100% speed
    const float framePeriod = 1 / 60f;          // 60 frames per second
    public float FramePeriod => framePeriod;
    
    MapManager map;
    LevelLogic level;
    GhostLogic ghostLogic;

    void Start()
    {
        map = GetComponent<MapManager>();
        level = GetComponent<LevelLogic>();
        ghostLogic = GetComponent<GhostLogic>();
    }

    readonly float[] pacmanNormalSpeed = { 0.80f, 0.90f, 0.90f, 0.90f, 1.00f };  
    readonly float[] pacmanFrightSpeed = { 0.90f, 0.95f, 0.95f, 0.95f, 1.00f };
    readonly float[] ghostNormalSpeed =  { 0.75f, 0.85f, 0.85f, 0.85f, 0.95f };
    readonly float[] ghostFrightSpeed =  { 0.50f, 0.55f, 0.55f, 0.55f, 0.60f };
    readonly float[] ghostTunnelSpeed =  { 0.40f, 0.45f, 0.45f, 0.45f, 0.50f };
    readonly float[] elroy1Speed =       { 0.80f, 0.90f, 0.90f, 0.90f, 1.00f };
    readonly float[] elroy2Speed =       { 0.85f, 0.95f, 0.95f, 0.95f, 1.05f };

    const float ghostDeadSpeed = 1.5f;
    const float ghostAtHomeSpeed = 0.4f;
    const float pacmanFinalSpeed = 0.9f;

    // Blinky turns intro Elroy after a certain number of dots have been eaten - Elroy is faster than other ghosts
    readonly int[] elroy1DotsLeft = { 20, 30, 40, 40, 40, 50, 50, 50, 60, 60, 60, 80, 80, 80, 100, 100, 100, 100, 120 };
    readonly int[] elroy2DotsLeft = { 10, 15, 20, 20, 20, 25, 25, 25, 30, 30, 30, 40, 40, 40, 50, 50, 50, 50, 60 };

    public float PacmanSpeed()
    {
        // returns the current speed of pacman

        float relativeSpeed;

        if (ghostLogic.FrightMode)  // if ghosts are frightened 
            relativeSpeed = pacmanFrightSpeed[Mathf.Clamp(level.Level, 0, pacmanFrightSpeed.Length-1)];
        else if (level.Level < 20)   // levels 0 - 19
            relativeSpeed = pacmanNormalSpeed[Mathf.Clamp(level.Level, 0, pacmanNormalSpeed.Length-1)];
        else  // levels 20+
            relativeSpeed = pacmanFinalSpeed;

        return maxSpeed * relativeSpeed * GameSettings.PacmanSpeedMultiplier;
    }

    public float GhostSpeed(GhostMove ghost, bool isAtHome)
    {
        // returns the current speed of the given ghost

        int Elroy1Limit = elroy1DotsLeft[Mathf.Clamp(level.Level, 0, elroy1DotsLeft.Length - 1)];  // when Blinky turns into Elroy
        int Elroy2Limit = elroy2DotsLeft[Mathf.Clamp(level.Level, 0, elroy2DotsLeft.Length - 1)];  

        float relativeSpeed;

        if (ghost.ghostMode == GhostMode.Dead)   // if the ghost is dead
            relativeSpeed = ghostDeadSpeed;
        else if (isAtHome)                       // if the ghost is in the ghost house
            relativeSpeed = ghostAtHomeSpeed;
        else if (map.IsTunnel(ghost.transform.position))  // if the ghost is inside of a tunnel
            relativeSpeed = ghostTunnelSpeed[Mathf.Clamp(level.Level, 0, ghostTunnelSpeed.Length - 1)];
        else if (ghost.ghostMode == GhostMode.Fright)     // if the ghost is frightened
            relativeSpeed = ghostFrightSpeed[Mathf.Clamp(level.Level, 0, ghostFrightSpeed.Length - 1)];
        else if (ghost.ghostName == GhostName.Blinky && level.DotsLeft <= Elroy2Limit) // Elroy 2
            relativeSpeed = elroy2Speed[Mathf.Clamp(level.Level, 0, elroy2Speed.Length - 1)];
        else if (ghost.ghostName == GhostName.Blinky && level.DotsLeft <= Elroy1Limit) // Elroy 1
            relativeSpeed = elroy1Speed[Mathf.Clamp(level.Level, 0, elroy1Speed.Length - 1)];
        else     // the normal speed  
            relativeSpeed = ghostNormalSpeed[Mathf.Clamp(level.Level, 0, ghostNormalSpeed.Length - 1)];

        return maxSpeed * relativeSpeed * GameSettings.GhostSpeedMultiplier;
    }
}
