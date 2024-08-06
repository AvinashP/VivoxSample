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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Black.VoiceChat
{
    public class SingleChannelScript : MonoBehaviour
    {
        [SerializeField] private string UserDisplayName = "User";
        [SerializeField] private Text _channelNameText;
        [SerializeField] private Text _statusText;
        [SerializeField] private TextMeshProUGUI _logText;

        // Voice Chat toggle buttons
        [SerializeField] private Toggle _talkToggle;
        [SerializeField] private Toggle _muteToggle;

        private static readonly string TeamChannelName = HomeSceneScript.CurrentTeamChannelName;

        private const int DefaultVolume = 0;

        private List<VivoxParticipant> _allChannelParticipants = new List<VivoxParticipant>();

        private Action<string> _permissionCallback;

        // Start is called before the first frame update
        void Start()
        {
            _channelNameText.text = TeamChannelName;

            if (HomeSceneScript.AskPermissionOnLoad)
            {
                ToggleButtons(false);

                RequestMicrophonePermission(permission => { SetupVoiceChatAsync(); });
            }
        }

        private void OnDestroy()
        {
            if (VivoxService.Instance != null)
            {
                VivoxService.Instance.LoggedIn -= VivoxServiceOnLoggedIn;
                VivoxService.Instance.LoggedOut -= VivoxServiceOnLoggedOut;

                VivoxService.Instance.ParticipantAddedToChannel -= VivoxServiceOnParticipantAddedToChannel;
                VivoxService.Instance.ParticipantRemovedFromChannel -= VivoxServiceOnParticipantRemovedFromChannel;
            }
        }

        public void BackButtonTap()
        {
            LogoutOfVivoxAsync();
            SceneManager.LoadSceneAsync("HomeScene");
        }

        #region Vivox Setup

        private async void SetupVoiceChatAsync()
        {
            await InitializeAsync();

            SetupVivoxEvents();

            await LoginToVivoxAsync();

            await SetTransmissionMode(TransmissionMode.None, null);

            if (_talkToggle.isOn)
            {
                TalkButtonTap();
            }
            else if (_muteToggle.isOn)
            {
                MuteMicrophoneButtonTap();
            }
            else
                ToggleButtons(true);
        }

        async Task InitializeAsync()
        {
            SetStatus("Initializing Unity Services...");

            if (UnityServices.Instance.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            if (AuthenticationService.Instance.IsAuthorized == false)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            if (VivoxService.Instance.IsLoggedIn == false)
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
            if (VivoxService.Instance.IsLoggedIn)
                return;

            try
            {
                SetStatus("Logging in to Vivox...");
                float timeToLogin = Time.time;
                LoginOptions options = new LoginOptions();
                options.DisplayName = UserDisplayName;
                options.EnableTTS = true;
                await VivoxService.Instance.LoginAsync(options);

                timeToLogin = Time.time - timeToLogin;
                SetStatus(VivoxService.Instance.IsLoggedIn
                    ? $"Logged in to Vivox in {timeToLogin:0.00}s"
                    : "Failed to log in to Vivox");
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public async void LogoutOfVivoxAsync()
        {
            if (VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
                return;

            SetStatus("Logging out of Vivox...");
            float timeToLogout = Time.time;
            await VivoxService.Instance.LogoutAsync();
            timeToLogout = Time.time - timeToLogout;
            SetStatus(VivoxService.Instance.IsLoggedIn
                ? "Failed to log out of Vivox"
                : $"Logged out of Vivox in {timeToLogout:0.00}s");

            ToggleButtons(false);
        }

        private async Task JoinGroupChannelAsync(string channelName)
        {
            if (VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
                return;

            if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            {
                Debug.Log($"Already joined {channelName} channel");
                SetChannelVolume(channelName, DefaultVolume);
                return;
            }

            float timeTaken = Time.time;
            SetStatus($"Joining {channelName} channel...");
            //ChannelOptions options = new ChannelOptions();
            //options.MakeActiveChannelUponJoining = false;

#if UNITY_EDITOR
            await VivoxService.Instance.JoinEchoChannelAsync(channelName, ChatCapability.AudioOnly, null);
#else
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly, null);
#endif
            timeTaken = Time.time - timeTaken;
            SetStatus($"Joined {channelName} in {timeTaken:0.00}s");

            SetChannelVolume(channelName, DefaultVolume);
        }

        private void SetChannelVolume(string channelName, int volume)
        {
            if (VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
                return;

            if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName) == false)
                return;

            Debug.Log($"Setting {channelName.ToUpper()} volume to {volume}");
            VivoxService.Instance.SetChannelVolumeAsync(channelName, volume);
            SetStatus($"{channelName.ToUpper()} volume set to {volume}");
        }

        #endregion

        #region Vivox Microphone related functions

        public async void TalkButtonTap()
        {
            Debug.Log($"TalkButtonTap - {_talkToggle.isOn}");
            if (VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            {
                ToggleButtons(false);
                RequestMicrophonePermission(permission => { SetupVoiceChatAsync(); });
                return;
            }

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
            if (VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            {
                ToggleButtons(false);
                RequestMicrophonePermission(permission => { SetupVoiceChatAsync(); });
                return;
            }

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
            if (VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
                return;

            SetStatus($"Setting TransmissionMode to {mode}  in {channelName} channel...");
            await VivoxService.Instance.SetChannelTransmissionModeAsync(mode, channelName);
            //VivoxService.Instance.MuteInputDevice();
            SetStatus($"TransmissionMode set to {mode} in {channelName}");
        }

        private async Task LeaveAllChannels()
        {
            if (VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
                return;

            float timeTaken = Time.time;
            SetStatus($"Leaving all channels...");
            await VivoxService.Instance.LeaveAllChannelsAsync();

            timeTaken = Time.time - timeTaken;
            SetStatus($"Left all channels in {timeTaken:0.00}s");
        }

        #endregion

        #region VivoxService Events

        private void VivoxServiceOnParticipantAddedToChannel(VivoxParticipant obj)
        {
            SetStatus($"Participant {obj.DisplayName} joined channel {obj.ChannelName}");
            _allChannelParticipants.Add(obj);
        }

        private void VivoxServiceOnParticipantRemovedFromChannel(VivoxParticipant obj)
        {
            SetStatus($"Participant {obj.DisplayName} left channel {obj.ChannelName}");
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
            if(_talkToggle == null || _muteToggle == null)
                return;
            
            _talkToggle.interactable = enable;
            _muteToggle.interactable = enable;
        }

        public void LogActiveChannels()
        {
            if (VivoxService.Instance == null)
                return;

            StringBuilder logString = new StringBuilder();

            logString.AppendLine($"Active channels: {VivoxService.Instance.ActiveChannels.Count}");
            foreach (var channel in VivoxService.Instance.ActiveChannels)
            {
                logString.AppendLine($"Channel: {channel.Key}");
            }

            logString.AppendLine($"Transmitting channels: {VivoxService.Instance.TransmittingChannels.Count}");
            foreach (var channel in VivoxService.Instance.TransmittingChannels)
            {
                logString.AppendLine($"Channel: {channel}");
            }

            Debug.Log(logString.ToString());
            _logText.SetText(logString.ToString());
        }

        private void SetStatus(string statusText)
        {
            if (_statusText == null) return;
            _statusText.text = statusText;
        }

        #endregion
    }
}