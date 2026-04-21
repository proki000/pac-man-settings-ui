using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioLogic : MonoBehaviour
{
    // this script takes care of all audio management in the game

    // all game sounds
    [SerializeField] AudioSource eatDotSoundEffect;
    [SerializeField] AudioSource eatFruitSoundEffect;
    [SerializeField] AudioSource eatGhostSoundEffect;
    [SerializeField] AudioSource gainExtraLifeSoundEffect;

    [SerializeField] AudioSource sirenBackgroundMusic;
    [SerializeField] AudioSource ghostFrightBackgroundMusic;
    [SerializeField] AudioSource ghostDeadBackgroundMusic;

    [SerializeField] AudioSource gameStart;
    [SerializeField] AudioSource pacmanDeath;

    AudioSource backgroundMusic = null;  // current background music


    public void EatFruit() => eatFruitSoundEffect.Play();
    public void GainExtraLife() => gainExtraLifeSoundEffect.Play();
    void PlayPacmanDeathSound() => pacmanDeath.Play();

    public void EatDot()
    {
        // plays the munch sound effect

        // don't let the sound effect interrupt itself
        if (!eatDotSoundEffect.isPlaying) eatDotSoundEffect.Play();  
    }

    public void EatGhost()
    {
        // plays the ghost death sound effect

        if (backgroundMusic != null) backgroundMusic.Stop();  // stop the background music

        eatGhostSoundEffect.Play();
    }


    bool startSiren = false;    // the background music starts playing after the initial melody
    float startSirenTimer = 0;
    float startSirenTimeLimit;  // how long should we wait before starting the siren
    public void GameStart(float sirenDelay)
    {
        // plays the initial melody and setups the siren BG music

        startSiren = true;
        startSirenTimer = 0;
        startSirenTimeLimit = sirenDelay;

        if (backgroundMusic != null) backgroundMusic.Stop();  // stop the background music
        gameStart.Play();   // play the initial melody
    }

    public void EndLevel()
    {
        // the background music stops when a level ends

        backgroundMusic.Stop();
    }

    public void PacmanDeath(float delay)
    {
        // when pacman dies, the BG music stops
        // and the pacman death sound effect starts playing after the ghosts disappear

        numDeadGhosts = 0;  // all of the ghosts will respawn

        if (backgroundMusic != null) backgroundMusic.Stop();
        Invoke(nameof(PlayPacmanDeathSound), delay);
    }

    void PlayBGMusic(AudioSource newBGMusic)
    {
        // plays the given BG music 

        // don't let the BG music interrupt itself
        if (backgroundMusic == newBGMusic && backgroundMusic.isPlaying) return;

        if (backgroundMusic != null) backgroundMusic.Stop();  // stop current BG music
        newBGMusic.Play();  // play the new one
        backgroundMusic = newBGMusic;
    }


    int numDeadGhosts = 0;  // keep track of dead ghosts

    public void FrightModeStarted()
    {
        // the BG music changes when ghosts enter fright mode 

        PlayBGMusic(ghostFrightBackgroundMusic);
    }

    public void GhostDied()
    {
        // when a ghosts dies, he returns to the ghost house
        // a different BG music plays while he is returning to the ghost house

        ++numDeadGhosts;
        PlayBGMusic(ghostDeadBackgroundMusic);
    }

    public void GhostRespawned()
    {
        // once all of the eaten ghosts have respawned, the siren starts playing again
        // if a power dot was eaten while the last ghost was returning, then the fright BG music is playing 
        // if this is the case, then the BG music should not change

        --numDeadGhosts;
        if (!ghostFrightBackgroundMusic.isPlaying && numDeadGhosts == 0)
        {
            PlayBGMusic(sirenBackgroundMusic);
        }
    }

    public void FrightModeEnded()
    {
        // after the fright mode ends and all of the eaten ghosts respawn, the siren should start playing
        // this should not interrupt the BG music of a ghost returning to the ghost house

        if (ghostFrightBackgroundMusic.isPlaying)  
        {
            PlayBGMusic(sirenBackgroundMusic);
        }
    }


    void Update()
    {
        // starts the siren - this is set up using the GameStart function

        if (startSiren)
        {
            startSirenTimer += Time.deltaTime;
            if (startSirenTimer >= startSirenTimeLimit)
            {
                startSiren = false;
                PlayBGMusic(sirenBackgroundMusic);
            }
        }
    }
}
