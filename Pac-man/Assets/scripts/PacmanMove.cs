using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PacmanMove : MonoBehaviour
{
    MapManager map;
    LevelLogic levelLogic;
    MovementManager movementManager;
    void Start()
    {
        map = GameObject.FindGameObjectWithTag("logic").GetComponent<MapManager>();
        movementManager = GameObject.FindGameObjectWithTag("logic").GetComponent<MovementManager>();
        levelLogic = GameObject.FindGameObjectWithTag("logic").GetComponent<LevelLogic>();
    }

    // store pacman's direction using an enum
    public enum Direction { Right, Up, Left, Down }
    bool IsVertical(Direction dir) => dir == Direction.Up || dir == Direction.Down;
    bool IsHorizontal(Direction dir) => dir == Direction.Right || dir == Direction.Left;

    Direction direction;  // pacman's current direction
    Direction turnCorrectionDir;  // used for correcting pacman's position after a pre-turn / post-turn
    public Direction PacmanDir => direction;  // direction getter

    // in game directions can be indexed by Direction
    readonly Vector3[] inGameDirections = { Vector3.right, Vector3.up, Vector3.left, Vector3.down };
    public Vector3 InGameDirection => inGameDirections[(int)direction];  // getter for the in game direction


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

    bool WallNextTile()
    {
        // checks if the next tile contains a wall
        Vector3 nextTile = transform.position + inGameDirections[(int)direction];
        return map.IsWall(nextTile);  // look into the map
    }

    bool ShouldStop()
    {
        // the player should stop moving when he hits a wall
        return WallNextTile() && CenteredX() && CenteredY();
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


    bool turning = false;

    void TakeAStep()
    {
        // moves pacman a bit in the direction he is going 

        if (ShouldStop()) return;  // don't move if a wall is ahead

        float maxStep = 0.5f;   // cant be more - it would break wall collisions
        if (WallNextTile()) maxStep = DistanceToNextTileCenter(direction);  // don't overshoot a wall

        // adjust pacmans position
        float deltaDist = Time.deltaTime * movementManager.PacmanSpeed();  
        transform.position += inGameDirections[(int)direction] * Mathf.Clamp(deltaDist, 0, maxStep);

        // pacman can pre-turn and post-turn = the turn can begin a few pixels before and after the center of a tile
        // his position has to be corrected to the tile center
        if (turning)
        {
            float maxTurnStep = DistanceToNextTileCenter(turnCorrectionDir);  // don't overshoot the tile center

            // move pacman closer to the tile center in the correction direction
            transform.position += inGameDirections[(int)turnCorrectionDir] * Mathf.Clamp(deltaDist, 0, maxTurnStep);

            // set turning to false upon reaching the tile center
            if ((IsHorizontal(direction) && CenteredY()) || (IsVertical(direction) && CenteredX())) turning = false;
        }

        // handle tunnels
        if (transform.position.x < 0) 
            transform.position = new Vector2(map.Width, transform.position.y);
        if (transform.position.x > map.Width) 
            transform.position = new Vector2(0, transform.position.y);
    }

    Direction NewDirection()
    {
        // looks at player's key presses and returns the direction he wants to go next

        Vector2 touchDirection = GameSettings.TouchDirection;
        if (touchDirection.x > 0.4f) return Direction.Right;
        if (touchDirection.x < -0.4f) return Direction.Left;
        if (touchDirection.y > 0.4f) return Direction.Up;
        if (touchDirection.y < -0.4f) return Direction.Down;

        float dirX = Input.GetAxisRaw("Horizontal");
        float dirY = Input.GetAxisRaw("Vertical");

        if (dirX > 0) return Direction.Right;
        if (dirX < 0) return Direction.Left;
        if (dirY > 0) return Direction.Up;
        if (dirY < 0) return Direction.Down;
        return direction;  // return the current direction if the player doesn't want to change it
    }

    bool IsNewDirectionLegal(Direction newDir)
    {
        // checks if pacman would be inside of a wall after moving 1 tile in a given direction
        Vector3 nextTile = transform.position + inGameDirections[(int)newDir];
        return !map.IsWall(nextTile);
    }

    void MovePacman()
    {
        // moves pacman and sets a new direction after player's input

        TakeAStep();  // move pacman
        Direction newDir = NewDirection();  // get the new direction

        // if the new direction is not legal or it is the same --> return
        if (turning || !IsNewDirectionLegal(newDir) || newDir == direction) return;   
     
        // if the player wants to go up or down, but pacman is moving left or right
        if (IsHorizontal(direction) && IsVertical(newDir))
        {
            // set up a correction if x is not centered
            if (!CenteredX())
            {
                turning = true;
                float tileCenter = 0.5f;
                turnCorrectionDir = (transform.position.x % 1 > tileCenter) ? Direction.Left : Direction.Right;
            }
        }
        // if the player wants to go left or right, but pacman is moving up or down
        if (IsVertical(direction) && IsHorizontal(newDir))
        {
            // set up a correction if y is not centered
            if (!CenteredY())
            {
                turning = true;
                float tileCenter = 0.5f;
                turnCorrectionDir = (transform.position.y % 1 > tileCenter) ? Direction.Down : Direction.Up;
            }
        }

        direction = newDir;
    }


    float freezeTimer = 0;  // used for briefly stopping pacman when he eats a dot
    public void StopPacmanForNumFrames(int numFrames)
    {
        // stop pacman from moving for a given number of frames
        freezeTimer = movementManager.FramePeriod * numFrames;
    }


    bool pacmanDead = false;
    public bool PacmanDead => pacmanDead;
    public void PacmanDied() => pacmanDead = true;

    readonly Vector2 pacmanSpawnPos = new Vector2(14, 7.5f);  // pacman starting position
    public void SpawnPacman()
    {
        // spawns pacman at his starting position facing left
        // this function is called when a new level starts and when pacman dies

        // reset variables to default values
        pacmanDead = false;
        turning = false;
        freezeTimer = 0;

        // teleport pacman back to his starting position
        direction = Direction.Left;
        transform.position = pacmanSpawnPos;

        // tell the animator to turn pacman into a full circle
        GetComponent<PacmanAnimator>().SpawnPacman();  
    }

    void Update()
    {
        // don't move pacman if he's dead or the game is frozen
        if (pacmanDead || levelLogic.GameFrozen) return;

        // don't move pacman if he was told to stop moving
        if (freezeTimer > 0) freezeTimer -= Time.deltaTime;
        else MovePacman();  
    }
}
