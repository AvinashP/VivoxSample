using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeSceneScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SingleChannelButtonTap()
    {
        SceneManager.LoadSceneAsync("SingleChannelScene");
    }
    
    public void ChannelSwitchButtonTap()
    {
        SceneManager.LoadSceneAsync("ChannelSwitchScene");
    }
    
    public void VolumeAdjustButtonTap()
    {
        SceneManager.LoadSceneAsync("VolumeAdjustScene");
    }
    
    public void VivoxSampleButtonTap()
    {
        SceneManager.LoadSceneAsync("VivoxSampleMainScene");
    }
}
