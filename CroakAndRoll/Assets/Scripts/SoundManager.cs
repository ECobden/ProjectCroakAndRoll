using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    [Header("Background Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private List<AudioClip> musicTracks = new List<AudioClip>();
    
    private int currentTrackIndex = 0;
    
    void Start()
    {
        // Get or add AudioSource component
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Start playing music if we have tracks
        if (musicTracks.Count > 0)
        {
            PlayTrack(0);
        }
    }

    void Update()
    {
        // Check if current track has finished and play next
        if (musicTracks.Count > 0 && musicSource != null && !musicSource.isPlaying)
        {
            PlayNextTrack();
        }
    }
    
    private void PlayTrack(int index)
    {
        if (musicTracks.Count == 0 || musicSource == null)
            return;
        
        currentTrackIndex = index % musicTracks.Count;
        musicSource.clip = musicTracks[currentTrackIndex];
        musicSource.Play();
        
        Debug.Log($"Now playing: Track {currentTrackIndex + 1}/{musicTracks.Count}");
    }
    
    private void PlayNextTrack()
    {
        int nextIndex = (currentTrackIndex + 1) % musicTracks.Count;
        PlayTrack(nextIndex);
    }
}
