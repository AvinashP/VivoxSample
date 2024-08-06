using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

namespace VivoxSamples
{
    public class SingleChannelScript : MonoBehaviour
    {
        [SerializeField] private string UserDisplayName = "User";
        [SerializeField] private Text _statusText;
        [SerializeField] private TextMeshProUGUI _logText;
    
        // Voice Chat toggle buttons
        [SerializeField] private Toggle _talkToggle;
        [SerializeField] private Toggle _muteToggle;
    
        private const string TeamChannelName = "team";
    
        private const int MinVolume = -50;
        private const int DefaultVolume = 0;
    
        private List<VivoxParticipant> _allChannelParticipants = new List<VivoxParticipant>();
    
        private Action<string> _permissionCallback;
    
        // Start is called before the first frame update
        void Start()
        {
            ToggleButtons(false);
        
            RequestMicrophonePermission(permission =>
            {
                SetupVoiceChatAsync();
            });
        }
    
        private void OnDestroy()
        {
            VivoxService.Instance.LoggedIn -= VivoxServiceOnLoggedIn;
            VivoxService.Instance.LoggedOut -= VivoxServiceOnLoggedOut;
        
            VivoxService.Instance.ParticipantAddedToChannel -= VivoxServiceOnParticipantAddedToChannel;
            VivoxService.Instance.ParticipantRemovedFromChannel -= VivoxServiceOnParticipantRemovedFromChannel;
        }
    
        #region Vivox Setup
        
        private async void SetupVoiceChatAsync()
        {
            await InitializeAsync();
        
            SetupVivoxEvents();
        
            await LoginToVivoxAsync();
        
            _muteToggle.isOn = true;
        
            ToggleButtons(true);
        }

        async Task InitializeAsync()
        {
            _statusText.text = "Initializing Unity Services...";
        
            await UnityServices.InitializeAsync();
        
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            await VivoxService.Instance.InitializeAsync();
        }

        private void SetupVivoxEvents()
        {
            VivoxService.Instance.LoggedIn += VivoxServiceOnLoggedIn;
            VivoxService.Instance.LoggedOut += VivoxServiceOnLoggedOut;
        
            VivoxService.Instance.ParticipantAddedToChannel += VivoxServiceOnParticipantAddedToChannel;
            VivoxService.Instance.ParticipantRemovedFromChannel += VivoxServiceOnParticipantRemovedFromChannel;
        }

        public async Task LoginToVivoxAsync()
        {
            _statusText.text = "Logging in to Vivox...";
            float timeToLogin = Time.time;
            LoginOptions options = new LoginOptions();
            options.DisplayName = UserDisplayName;
            options.EnableTTS = true;
            await VivoxService.Instance.LoginAsync(options);
        
            timeToLogin = Time.time - timeToLogin;
            _statusText.text = VivoxService.Instance.IsLoggedIn ? $"Logged in to Vivox in {timeToLogin:0.00}s" : "Failed to log in to Vivox";
        }
    
        public async void LogoutOfVivoxAsync ()
        {
            if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
                return;
        
            _statusText.text = "Logging out of Vivox...";
            float timeToLogout = Time.time;
            await VivoxService.Instance.LogoutAsync();
            timeToLogout = Time.time - timeToLogout;
            _statusText.text = VivoxService.Instance.IsLoggedIn ? "Failed to log out of Vivox" : $"Logged out of Vivox in {timeToLogout:0.00}s";
        
            ToggleButtons(false);
        }
        
        private async Task JoinGroupChannelAsync(string channelName)
        {
            if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
                return;

            if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            {
                Debug.Log($"Already joined {channelName} channel");
                SetChannelVolume(channelName, DefaultVolume);
                return;
            }

            float timeTaken = Time.time;
            _statusText.text = $"Joining {channelName} channel...";
            //ChannelOptions options = new ChannelOptions();
            //options.MakeActiveChannelUponJoining = false;
        
#if UNITY_EDITOR
            await VivoxService.Instance.JoinEchoChannelAsync(channelName, ChatCapability.AudioOnly, null);
#else
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly, null);
#endif
            timeTaken = Time.time - timeTaken;
            _statusText.text = $"Joined {channelName} in {timeTaken:0.00}s";
        
            SetChannelVolume(channelName, DefaultVolume);
        }

        private void SetChannelVolume(string channelName, int volume)
        {
            if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
                return;
        
            if(VivoxService.Instance.ActiveChannels.ContainsKey(channelName) == false)
                return;
        
            Debug.Log($"Setting {channelName.ToUpper()} volume to {volume}");
            VivoxService.Instance.SetChannelVolumeAsync(channelName, volume);
            _statusText.text = $"{channelName.ToUpper()} volume set to {volume}";
        }
        
        #endregion
    
        #region Vivox Microphone related functions
    
        public async void TalkButtonTap()
        {
            Debug.Log($"TalkButtonTap - {_talkToggle.isOn}");
            if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
                return;

            if (_talkToggle.isOn)
            {
                ToggleButtons(false);
                
                // First join the channels if not joined already
                await JoinGroupChannelAsync(TeamChannelName);
            
                // Set transmission mode to single
                await SetTransmissionMode(TransmissionMode.Single, TeamChannelName);
            
                _talkToggle.isOn = true;
                _muteToggle.isOn = false;
                
                ToggleButtons(true);
            }
        }
    
        public void MuteMicrophoneButtonTap()
        {
            Debug.Log($"MuteMicrophoneButtonTap - {_muteToggle.isOn}");
            
            if (_muteToggle.isOn)
            {
                // If we want to mute microphone, set transmission mode to none
                SetTransmissionMode(TransmissionMode.None, null);
                
                _talkToggle.isOn = false;
                _muteToggle.isOn = true;
            }
        }
    
        private async Task SetTransmissionMode(TransmissionMode mode, string channelName)
        {
            if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
                return;
        
            _statusText.text = $"Setting TransmissionMode to {mode}  in {channelName} channel...";
            await VivoxService.Instance.SetChannelTransmissionModeAsync(mode, channelName);
            //VivoxService.Instance.MuteInputDevice();
            _statusText.text = $"TransmissionMode set to {mode} in {channelName}";
        }

        #endregion
    
        #region VivoxService Events

        private void VivoxServiceOnParticipantAddedToChannel(VivoxParticipant obj)
        {
            _statusText.text = $"Participant {obj.DisplayName} joined channel {obj.ChannelName}";
            _allChannelParticipants.Add(obj);
        }
    
        private void VivoxServiceOnParticipantRemovedFromChannel(VivoxParticipant obj)
        {
            _statusText.text = $"Participant {obj.DisplayName} left channel {obj.ChannelName}";
            _allChannelParticipants.Remove(obj);
        }
    
        private void VivoxServiceOnLoggedIn()
        {
            Debug.Log("Logged in to Vivox");
        }
    
        private void VivoxServiceOnLoggedOut()
        {
            Debug.Log("Logged out of Vivox");
        }

        #endregion

        #region Permission related functions

        private void RequestMicrophonePermission(Action<string> callback)
        {
#if UNITY_ANDROID
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                _permissionCallback = null;
                callback?.Invoke(Permission.Microphone);
                return;
            }
        
            _permissionCallback = callback;

            var callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
            callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
            callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
#else
        _permissionCallback = null;
        callback?.Invoke(Permission.Microphone);
#endif
        }

        private void PermissionCallbacks_PermissionDeniedAndDontAskAgain(string obj)
        {
            _logText.SetText($"Permission denied and don't ask again: {obj}");
            _permissionCallback?.Invoke(obj);
        }

        private void PermissionCallbacks_PermissionGranted(string obj)
        {
            _logText.SetText($"Permission granted: {obj}");
            _permissionCallback?.Invoke(obj);
        }

        private void PermissionCallbacks_PermissionDenied(string obj)
        {
            _logText.SetText($"Permission denied: {obj}");
            _permissionCallback?.Invoke(obj);
        }

        #endregion
    
        #region Misc functions

        private void ToggleButtons(bool enable)
        {
            _talkToggle.interactable = enable;
            _muteToggle.interactable = enable;
        }
    
        public void LogActiveChannels()
        {
            if(VivoxService.Instance == null)
                return;
        
            StringBuilder logString = new StringBuilder();
        
            logString.AppendLine($"Active channels: {VivoxService.Instance.ActiveChannels.Count}");
            foreach (var channel in VivoxService.Instance.ActiveChannels)
            {
                logString.AppendLine($"Channel: {channel.Key}");
            }
        
            logString.AppendLine($"Transmitting channels: {VivoxService.Instance.TransmittingChannels.Count}");
            foreach(var channel in VivoxService.Instance.TransmittingChannels)
            {
                logString.AppendLine($"Channel: {channel}");
            }
        
            Debug.Log(logString.ToString());
            _logText.SetText(logString.ToString());
        }

        #endregion
    }
}