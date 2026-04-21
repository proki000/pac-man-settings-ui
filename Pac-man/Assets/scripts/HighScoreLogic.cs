using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class HighScoreLogic : MonoBehaviour
{
    // this script takes care of loading the saved highscore when the game is launched and saving new highscores


    int highscore;
    // the highscore is stored in a file called 'highscore.txt'
    // the file is in the StreamingAssets folder - this folder is preserved when the game is build
    readonly string highscoreFilePath = Path.Combine(Application.streamingAssetsPath, "highscore.txt");


    [SerializeField] TextMeshProUGUI highscoreLabel;

    void UpdateHighscoreLabel()
    {
        // update the text in the highscore label
        // the label should show '00' when highscore is 0
        highscoreLabel.text = (highscore == 0) ? "00" : highscore.ToString();
    }

    void Start()
    {
        // try loading the saved highscore when the game starts

        try
        {
            using (StreamReader reader = new StreamReader(highscoreFilePath))
            {
                highscore = int.Parse(reader.ReadLine());  // load the saved highscore
            }
        }
        catch (System.Exception)   // the file can be missing or the contents can be corrupted
        {
            highscore = 0;
        }

        UpdateHighscoreLabel();
    }

    void SaveHighScore()
    {
        // save the highscore to the 'highscore.txt' file

        using (StreamWriter writer = new StreamWriter(highscoreFilePath))
        {
            writer.WriteLine(highscore.ToString());
        }
    }

    public void UpdateHighScore(int score)
    {
        // this function is called when the score changes
        // and updates the highscore if it has been exceeded

        if (score > highscore)
        {
            highscore = score;
            SaveHighScore();         // save the new highscore
            UpdateHighscoreLabel();  // update the label
        }
    }
}
