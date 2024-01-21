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

    public void VolumeAdjustButtonTap()
    {
        SceneManager.LoadSceneAsync("VolumeAdjustScene");
    }
    
    public void ChannelSwitchButtonTap()
    {
        SceneManager.LoadSceneAsync("VolumeAdjustScene");
    }
    
    public void VivoxSampleButtonTap()
    {
        SceneManager.LoadSceneAsync("VivoxSampleMainScene");
    }
}
