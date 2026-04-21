using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GhostMove : MonoBehaviour
{
    GhostLogic ghostLogic;
    LevelLogic levelLogic;
    GhostChaseManager ghostChaseManager;
    MapManager map;
    MovementManager movementManager;
    AudioLogic audioPlayer;
    SpriteRenderer spriteRenderer;

    void Start()
    {
        map = GameObject.FindGameObjectWithTag("logic").GetComponent<MapManager>();
        movementManager = GameObject.FindGameObjectWithTag("logic").GetComponent<MovementManager>();
        ghostLogic = GameObject.FindGameObjectWithTag("logic").GetComponent<GhostLogic>();
        levelLogic = GameObject.FindGameObjectWithTag("logic").GetComponent<LevelLogic>();
        audioPlayer = GameObject.FindGameObjectWithTag("audio").GetComponent<AudioLogic>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        ghostChaseManager = GetComponent<GhostChaseManager>();
    }

    // Blinky=red, Pinky=pink, Inky=blue, Clyde=orange
    public enum GhostName { Blinky, Pinky, Inky, Clyde }
    public GhostName ghostName;  // this is set in unity 

    // ghosts start in scatter mode and then start chasing pacman
    public enum GhostMode { Chase, Scatter, Fright, Dead }
    public GhostMode ghostMode = GhostMode.Scatter;

    // store ghost's direction using an enum
    public enum Direction { Up, Left, Down, Right }
    Direction OppositeDir(Direction dir) => (Direction)(((int)dir + 2) % 4);  // returns the opposite direction
    bool OppositeDirs(Direction d1, Direction d2) => d2 == OppositeDir(d1);

    // in game directions can be indexed by Direction
    readonly Vector3[] inGameDirections = { Vector3.up, Vector3.left, Vector3.down, Vector3.right };
    Direction direction;
    public Direction GhostDir => direction;  // direction getter
    // variables used for setting the direction for the next tile


    // ghosts are sometimes told by the game system to reverse their direction
    bool ghostReverseDirection;
    public void ReverseDirection() => ghostReverseDirection = true;



    public void HideGhost()
    {
        // makes the ghost invisible
        spriteRenderer.enabled = false;
    }


    // FRIGHT MODE LOGIC

    GhostMode preFrightMode;  // remember the original mode
    public void FrightenGhost()
    {
        // reverse ghost's direction
        ReverseDirection();

        if (ghostMode == GhostMode.Dead) return;    // dead ghosts can't be frightened
        if (ghostMode == GhostMode.Fright) return;  // frightened ghosts already are frightened

        preFrightMode = ghostMode;     // save the original mode
        ghostMode = GhostMode.Fright;  // set fright mode 
    }

    public void UnFrightenGhost()
    {
        // return the ghost back from the fright mode 
        if (ghostMode == GhostMode.Fright)
        {
            ghostMode = preFrightMode;
        }
    }

    // points are displayed after a ghost is eaten
    [SerializeField] Sprite[] ghostPointSprites = new Sprite[4];

    // Blinky is at the top and Clyde is at the bottom of the ghosts sorting layer
    readonly int[] sortingOrder = { 3, 2, 1, 0 };
    const int highSortingOrder = 10;   // the number has to be displayer over the ghosts
    public void KillGhost(int ghostsEaten, float setDeadDelay)
    {
        // change the sprite to the points image
        spriteRenderer.sprite = ghostPointSprites[ghostsEaten - 1];  
        spriteRenderer.sortingOrder = highSortingOrder;  // make it appear on top

        Invoke(nameof(SetDeadMode), setDeadDelay);  // make the ghost dead after a delay
    }

    void SetDeadMode()
    {
        // revert the sorting layer and set the Dead mode
        // there is no need to return the original sprite, because everything is animated
        spriteRenderer.sortingOrder = sortingOrder[(int)ghostName];
        ghostMode = GhostMode.Dead;
        audioPlayer.GhostDied();
    }



    // LEAVING THE HOUSE

    void IdleInsideHouse()
    {
        // this function makes the ghost go up and down in the ghost house
        float houseTop = 17f;
        float houseBot = 16f;

        if (transform.position.y > houseTop) direction = Direction.Down;
        if (transform.position.y < houseBot) direction = Direction.Up;
    }


    float leaveHouseMaxStep;
    bool leavingHouse = false;
    public void LeaveHouse() => leavingHouse = true;

    void EscapeHouse()
    {
        // this function makes the ghost leave the house

        float houseCenterX = 14f;
        float houseCenterY = 16.5f;
        float houseDoorY = 19.5f;
        float epsilon = 0.05f;

        float x = transform.position.x;
        float y = transform.position.y;

        // check if the ghost already escaped
        if (Mathf.Abs(houseDoorY - y) < epsilon)
        {
            transform.position = new Vector2(houseCenterX, houseDoorY);
            leavingHouse = false;
            direction = Direction.Left;
            return;
        }

        // check if he isn't still walking up and down at one of the sides of the house
        if (!CenteredY() && Mathf.Abs(houseCenterX - x) > epsilon)
        {
            IdleInsideHouse();   // wait for the ghost to come into the house Y center
            leaveHouseMaxStep = Mathf.Abs(houseCenterY - y);  // make sure that he doesn't overshoot the center y
            return;
        }

        // y is now centered

        if (Mathf.Abs(houseCenterX - x) > epsilon)  // if x is not centered
        {
            // walk to the house center x
            direction = (x < houseCenterX) ? Direction.Right : Direction.Left;
            leaveHouseMaxStep = Mathf.Abs(houseCenterX - x);
        }
        else // walk out of the house
        {
            direction = Direction.Up;
            leaveHouseMaxStep = houseDoorY - y;
        }
    }



    // ENTERING THE HOUSE

    bool IsAboveTheDoorCenter()
    {
        // this function checks if the ghost above the center of the door to the ghost house

        float houseDoorX = 14f;
        float houseDoorY = 19.5f;
        float epsilon = 0.05f;

        float x = transform.position.x;
        float y = transform.position.y;

        return Mathf.Abs(y - houseDoorY) < epsilon && Mathf.Abs(x - houseDoorX) < epsilon;
    }

    bool IsAboveTheDoor()
    {
        // this function checks if the ghost above the door to the ghost house

        float houseDoorX = 14;
        float houseDoorY = 19.5f;
        float epsilon = 1f;

        float x = transform.position.x;
        float y = transform.position.y;

        return Mathf.Abs(y - houseDoorY) < epsilon && Mathf.Abs(x - houseDoorX) < epsilon;
    }

    float DistanceToDoor()
    {
        // returns the horizontal distance to the center of the door
        float houseDoorX = 14;
        return Mathf.Abs(transform.position.x - houseDoorX);
    }


    float enterHouseMaxStep;
    bool enteringHouse = false;
    void EnterHouse()
    {
        // this function makes the ghost enter the house

        float houseCenterY = 16.5f;
        float epsilon = 0.05f;

        float x = transform.position.x;
        float y = transform.position.y;

        // if the ghost is still outside of the house
        if (y > houseCenterY)
        {
            direction = Direction.Down;
            enterHouseMaxStep = y - houseCenterY;  // make sure that he doesn't overshoot the house center
            return;
        }

        float targetX = ghostStartX[(int)ghostName];  // the X coordinate of the target location
        if (Mathf.Abs(targetX - x) > epsilon)   // walk to the target location
        {
            direction = (targetX < x) ? Direction.Left : Direction.Right;
            enterHouseMaxStep = Mathf.Abs(targetX - x);  // make sure that he doesn't overshoot the target
            return;
        }

        transform.position = new Vector2(targetX, houseCenterY);
        enteringHouse = false;
        direction = Direction.Down;
        ghostMode = preFrightMode;   // restore the original mode 

        ghostLogic.GhostArrivedHome(ghostName);
        audioPlayer.GhostRespawned();
    }



    // SETTING THE TARGET TILE AND MOVING


    // the ghost is trying to reach the target tile - except for when he is frightened, then he moves randomly
    public Vector2 targetTile;

    float DistanceToTarget(Vector2 tile)
    {
        // returns the euclidean distance to target tile measured in tiles
        int dx = (int)Mathf.Abs(Mathf.Floor(tile.x) - Mathf.Floor(targetTile.x));
        int dy = (int)Mathf.Abs(Mathf.Floor(tile.y) - Mathf.Floor(targetTile.y));
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    void SetTargetTile()
    {
        // sets the target tile based on the mode 

        switch (ghostMode)
        {
            case GhostMode.Chase:   // chasing pacman
                targetTile = ghostChaseManager.ChaseTargetTile();
                break;

            case GhostMode.Scatter: // going to a corner
                int[] targetTileX = { map.Width - 3, 2, map.Width - 1, 0 };
                int[] targetTileY = { map.Height + 2, map.Height + 2, -1, -1 };
                targetTile = new Vector2(targetTileX[(int)ghostName], targetTileY[(int)ghostName]);
                break;

            case GhostMode.Dead:   // returning to the house
                int houseDoorX = 13;
                int houseDoorY = 19;
                targetTile = new Vector2(houseDoorX, houseDoorY);
                break;
        }
    }

    float DistanceToNextTileCenter(Direction dir) 
    {
        // returns the distance to the next tile center in a given direction
        // if x == 6.7 and dir == Right, then the next tile center is 7.5, so return 0.8
        // this function is used to make sure that pacman does not overshoot tile centers before walls

        float x = transform.position.x;
        float y = transform.position.y;
        return dir switch
        {
            Direction.Right => (1.5f - x % 1) % 1,
            Direction.Up => (1.5f - y % 1) % 1,
            Direction.Left => 1 - (1.5f - x % 1) % 1,
            Direction.Down => 1 - (1.5f - y % 1) % 1,
            _ => 0   // this should never happen
        };
    }

    void TakeAStep()
    {
        // moves the ghost based on his speed and time passed in the direction he is going

        // get the ghost's speed
        bool atHome = ghostLogic.IsGhostAtHome(ghostName) || leavingHouse;
        float speed = movementManager.GhostSpeed(this, atHome);

        // make sure that he doesn't overshoot anything important
        float maxStep = 0.5f;

        // if he is entering the house
        if (enteringHouse) 
            maxStep = enterHouseMaxStep;

        // if he is leaving the house
        else if (leavingHouse) 
            maxStep = leaveHouseMaxStep; 

        // if he is about to enter the house
        else if (!enteringHouse && ghostMode == GhostMode.Dead && IsAboveTheDoor())
            maxStep = DistanceToDoor();

        // if he is walking in the maze and about to turn
        else if (!enteringHouse && !atHome && dirToBeSet != direction)
            maxStep = DistanceToNextTileCenter(direction);

        // adjusts his position
        float deltaDist = Mathf.Clamp(speed * Time.deltaTime, 0, maxStep);
        transform.position += inGameDirections[(int)direction] * deltaDist;

        // handle tunnels
        if (transform.position.x < 0)
            transform.position = new Vector2(map.Width, transform.position.y);
        if (transform.position.x > map.Width)
            transform.position = new Vector2(0, transform.position.y);
    }


    // CHOOSING A NEW DIRECTION AND DECIDING THE COURSE OF ACTION

    // ghosts plan their movement 1 tile ahead - this function picks the next direction
    // if the ghost just entered tile A, then he knows the next tile he will enter - lets call it B
    // after entering tile A, the ghost chooses a direction he will set once he reaches the center of tile B
    // after reaching the center of tile A, he will set the direction he calculated on the previous tile

    // we need 2 variables, because he calculates the next direction immediately after entering the tile
    Direction nextDir;     // the direction he will set once he reaches the center of tile B
    Direction dirToBeSet;  // the direction he will set once he reaches the center of tile A

    // we will need to be able to tell whether the ghost entered a new tile
    Vector2 lastTile;

    Direction NewDirection()
    {
        // calculates the next direction based on the mode
        Vector3 newTile = transform.position + inGameDirections[(int)dirToBeSet];

        // if the ghost is frightened
        if (ghostMode == GhostMode.Fright)
        {
            // generate a random direction
            int randomDir = ghostLogic.rng.Next(4);
            for (int newDir = randomDir; newDir < randomDir + 4; newDir++)
            {
                newDir = newDir % 4;

                if (OppositeDirs((Direction)newDir, dirToBeSet)) continue;  // ghosts can not reverse direction
                if (map.IsWall(newTile + inGameDirections[newDir])) continue;   // skip walls

                // this is a legal direction
                return (Direction)newDir;  
            }
        }


        // if the ghost is not frightened
        // the ghost will try to get as close to the target tile as possible
        Direction newDirection = Direction.Up;
        float bestDistance = 1000;   

        for (int newDir = 0; newDir < 4; newDir++)  // try all of the directions
        {
            // ghosts can not reverse direction
            if (OppositeDirs((Direction)newDir, dirToBeSet)) continue;

            // ghost can move only left and right while in the red zone
            if (map.IsRedZone(newTile) && ghostMode != GhostMode.Dead)  
                if (newDir == (int)Direction.Up || newDir == (int)Direction.Down) continue;

            // skip walls
            if (map.IsWall(newTile + inGameDirections[newDir])) continue;

            // this is a legal direction
            float distToTarget = DistanceToTarget(newTile + inGameDirections[newDir]);
            if (distToTarget < bestDistance)
            {
                newDirection = (Direction)newDir;
                bestDistance = distToTarget;
            }
        }
        return newDirection;
    }


    bool CenteredX()
    {
        // returns: x is in the middle of a tile

        float x = transform.position.x;
        float tileCenter = 0.5f;
        float epsilon = 0.05f;

        return Mathf.Abs(x % 1 - tileCenter) < epsilon;  // 1.84 % 1 = 0.84
    }
    bool CenteredY()
    {
        // returns: y is in the middle of a tile

        float y = transform.position.y;
        float tileCenter = 0.5f;
        float epsilon = 0.05f;

        return Mathf.Abs(y % 1 - tileCenter) < epsilon;  // 6.45 % 1 = 0.45
    }

    void MoveGhost()
    {
        // this function decides the course of action of the ghost

        TakeAStep();  // actually move

        // if the ghost is about to enter the house 
        if (ghostMode == GhostMode.Dead && IsAboveTheDoorCenter()) {
            enteringHouse = true;
        }

        // if the ghost is entering the house
        if (enteringHouse)
        {
            EnterHouse();
            return;
        }

        // if the ghost is leaving the house
        if (leavingHouse)
        {
            EscapeHouse();
            return;
        }

        // if the ghost is inside of the house
        if (ghostLogic.IsGhostAtHome(ghostName))
        {
            IdleInsideHouse();
            return;
        }

        // if the ghost is moving through the maze

        // if the ghost entered a new tile
        Vector2 currentTile = new Vector2(Mathf.Floor(transform.position.x), Mathf.Floor(transform.position.y));
        if (currentTile.x != lastTile.x || currentTile.y != lastTile.y)    
        {
            // if he was told to reverse his direction
            if (ghostReverseDirection)
            {
                nextDir = OppositeDir(direction);  // overwrite the previously set direction
                ghostReverseDirection = false;
            }
            
            dirToBeSet = nextDir;      // this direction will be set once the ghost reaches the center of the tile
            nextDir = NewDirection();  // calculate the direction for the next tile
            lastTile = currentTile;
        }

        // if the ghost wants to turn
        if (dirToBeSet != direction && CenteredX() && CenteredY())
        {
            direction = dirToBeSet;  // update his direction
        }
    }


    // the ghost can be stopped if told so
    bool stopped = false;
    public void StopGhost() => stopped = true;


    readonly float[] ghostStartX = { 14, 14, 12, 16 };
    readonly float[] ghostStartY = { 19.5f, 16.5f, 16.5f, 16.5f };

    // starting directions are: Blinky left, Pinky down, Inky & Clyde Up
    readonly Direction[] startDirs = { Direction.Left, Direction.Down, Direction.Up, Direction.Up };

    public void SpawnGhost()
    {
        // this function is called after pacman dies and at the start of every level

        // show the ghost
        spriteRenderer.enabled = true;

        // reset the direction and mode to default
        direction = startDirs[(int)ghostName];
        dirToBeSet = direction;
        ghostMode = GhostMode.Scatter;

        // reset movement modifiers to default
        ghostReverseDirection = false;
        leavingHouse = false;
        enteringHouse = false;
        stopped = false;

        // calculate the direction for the next tile
        transform.position = new Vector2(ghostStartX[(int)ghostName], ghostStartY[(int)ghostName]);
        lastTile = new Vector2(Mathf.Floor(transform.position.x), Mathf.Floor(transform.position.y));
        nextDir = NewDirection();
    }


    void Update()
    {
        // don't move the ghost if he is stopped or the game is frozen
        if (stopped) return;  
        if (levelLogic.GameFrozen && ghostMode != GhostMode.Dead) return;
        
        // set the target tile and move the ghost
        SetTargetTile();
        MoveGhost();
    }
}
