using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    // this script loads the map when the game is started and tells pacman and the ghosts where are the walls etc.

    // the map is stored in a file called 'mapa.txt'
    // the file is in the StreamingAssets folder - this folder is preserved when the game is build
    readonly string mapFilePath = Path.Combine(Application.streamingAssetsPath, "mapa.txt");

    const int mapWidth = 28;
    const int mapHeight = 31;
    public int Width => mapWidth;
    public int Height => mapHeight;

    readonly char[,] map = new char[mapHeight, mapWidth];  // the map will be stored in a 2D char array


    void Start()
    {
        // load the map at the start

        using (StreamReader reader = new StreamReader(mapFilePath))
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    map[y, x] = (char)reader.Read();
                }
                reader.ReadLine();
            }
        }
    }


    // we need to translate from the in-game coordinates to array indexes
    // clamp the X coordinate for tunnels - pacman will be out of bounds when going through the tunnel
    int IndexX(float x) => Mathf.Clamp((int)Mathf.Floor(x), 0, mapWidth - 1);   
    int IndexY(float y) => mapHeight - (int)Mathf.Ceil(y);

    bool IsDot(Vector3 pos) => map[IndexY(pos.y), IndexX(pos.x)] == '.' || map[IndexY(pos.y), IndexX(pos.x)] == 'B';
    bool IsPowerDot(Vector3 pos) => map[IndexY(pos.y), IndexX(pos.x)] == 'o';
    public bool IsWall(Vector3 pos) => map[IndexY(pos.y), IndexX(pos.x)] == 'X';
    public bool IsRedZone(Vector3 pos) => map[IndexY(pos.y), IndexX(pos.x)] == 'R' || map[IndexY(pos.y), IndexX(pos.x)] == 'B';
    public bool IsTunnel(Vector3 pos) => map[IndexY(pos.y), IndexX(pos.x)] == 'T';


    [SerializeField] GameObject dotPrefab;
    [SerializeField] GameObject powerDotPrefab;

    public void SpawnDots()
    {
        // spawns dots into the scene

        float mapEdgeOffset = 1.5f;

        Vector2 dotPos = Vector2.zero;  // the dot will be spawned at this position
        for (float y = mapEdgeOffset; y <= mapHeight - mapEdgeOffset; y++)
        {
            dotPos.y = y;
            for (float x = mapEdgeOffset; x <= mapWidth - mapEdgeOffset; x++)
            {
                dotPos.x = x;
                if (IsDot(dotPos)) Instantiate(dotPrefab, dotPos, Quaternion.identity);           // spawn dot
                if (IsPowerDot(dotPos)) Instantiate(powerDotPrefab, dotPos, Quaternion.identity);  // spawn power dot
            }
        }
    }
}
