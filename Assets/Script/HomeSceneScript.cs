using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Black.VoiceChat
{
    public class HomeSceneScript : MonoBehaviour
    {
        public static string GetRandomChannelName()
        {
            return "Channel_" + Random.Range(1, 5);
        }

        public static string CurrentTeamChannelName;
        public static bool AskPermissionOnLoad = false;

        [SerializeField] private TMP_InputField _channelNameInputField;
        [SerializeField] private Toggle _askPermissionToggle;

        // Start is called before the first frame update
        void Start()
        {
            _channelNameInputField.text = GetRandomChannelName();
        }

        public void SingleChannelButtonTap()
        {
            if(string.IsNullOrEmpty(_channelNameInputField.text))
                return;
            
            CurrentTeamChannelName = _channelNameInputField.text;
            AskPermissionOnLoad = _askPermissionToggle.isOn;

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
}