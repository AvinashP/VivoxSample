using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine.Android;
using UnityEngine.UI;

public class ChannelSwitchSampleScript : MonoBehaviour
{
    [SerializeField] private string UserDisplayName = "User";
    [SerializeField] private Text _statusText;
    [SerializeField] private TextMeshProUGUI _logText;
    
    // Speaker toggle buttons
    [SerializeField] private Toggle _listenToAllChannelToggle;
    [SerializeField] private Toggle _listenToTeamChannelToggle;
    [SerializeField] private Toggle _muteListeningToggle;
    
    // Microphone toggle buttons
    [SerializeField] private Toggle _talkToAllChannelToggle;
    [SerializeField] private Toggle _talkToTeamChannelToggle;
    [SerializeField] private Toggle _muteMicrophoneToggle;
    
    private const string AllChannelName = "all";
    private const string TeamChannelName = "team";
    
    private const int MinVolume = -50;
    private const int MaxVolume = 40;
    
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
    
    private async void SetupVoiceChatAsync()
    {
        await InitializeAsync();
        
        SetupVivoxEvents();
        
        await LoginToVivoxAsync();
        
        await SetTransmissionMode(TransmissionMode.None, null);
        
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

    #region Join/Leave Channel related functions

    private async Task JoinGroupChannelAsync(string channelName)
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;

        if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
        {
            Debug.Log($"Already joined {channelName} channel");
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
    }

    private async Task LeaveChannel(string channelName)
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;
        
        if(VivoxService.Instance.ActiveChannels.ContainsKey(channelName) == false)
            return;
        
        float timeTaken = Time.time;
        _statusText.text = $"Leaving {channelName} channel...";
        
        await VivoxService.Instance.LeaveChannelAsync(channelName);
        timeTaken = Time.time - timeTaken;
        _statusText.text = $"Left {channelName} channel in {timeTaken:0.00}s";
    }
    
    private async Task LeaveAllChannels()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;
        
        float timeTaken = Time.time;
        _statusText.text = $"Leaving all channels...";
        await VivoxService.Instance.LeaveAllChannelsAsync();
        
        timeTaken = Time.time - timeTaken;
        _statusText.text = $"Left all channels in {timeTaken:0.00}s";
    }

    #endregion
    
    #region Vivox Speaker related functions
    
    public void ListenToAllButtonTap()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;

        Debug.Log($"ListenToAllButtonTap {_listenToAllChannelToggle.isOn}");
        // When listening to all, you are listening to team as well
        if (_listenToAllChannelToggle.isOn)
        {
            JoinGroupChannelAsync(AllChannelName);

            JoinGroupChannelAsync(TeamChannelName);
        }
    }
    
    public void ListenToTeamButtonTap()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;

        // When listening to team, you are only listening to team
        if (_listenToTeamChannelToggle.isOn)
        {
            JoinGroupChannelAsync(TeamChannelName);

            // If you are subscribed to All channel, then leave it
            if (VivoxService.Instance.ActiveChannels.ContainsKey(AllChannelName))
            {
                LeaveChannel(AllChannelName);
            }
        }
    }

    public void MuteSpeakerButtonTap()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;

        if (_muteListeningToggle.isOn)
        {
            Debug.Log($"Mute speaker");
            LeaveAllChannels();
        }
    }
    
    private void SetChannelVolume(string channelName, int volume)
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;
        
        Debug.Log($"Setting {channelName.ToUpper()} volume to {volume}");
        VivoxService.Instance.SetChannelVolumeAsync(channelName, volume);
        _statusText.text = $"{channelName.ToUpper()} volume set to {volume}";
    }
    
    #endregion    

    #region Vivox Microphone related functions

    public async void TalkToAllButtonTap()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;

        // When talking to all, you will have to listen to all as well
        if (_talkToAllChannelToggle.isOn)
        {
            // First join the channels if not joined already
            await JoinGroupChannelAsync(AllChannelName);
            await JoinGroupChannelAsync(TeamChannelName);
            
            // Set transmission mode to all
            SetTransmissionMode(TransmissionMode.All, null);
            
            // You gotta listen to all as well. COD design
            _listenToAllChannelToggle.isOn = true;
        }
    }
    
    public async void TalkToTeamButtonTap()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;

        if (_talkToTeamChannelToggle.isOn)
        {
            // First join the channels if not joined already
            await JoinGroupChannelAsync(TeamChannelName);
            
            // Set transmission mode to single
            await SetTransmissionMode(TransmissionMode.Single, TeamChannelName);
            
            // If you are not listening, then you need to listen to team channel
            if (_muteListeningToggle.isOn)
            {
                // Setting toggle calls the function. So we dont need to call it again
                _listenToTeamChannelToggle.isOn = true;
            }
        }
    }
    
    public void MuteMicrophoneButtonTap()
    {
        if (_muteMicrophoneToggle.isOn)
        {
            // If we want to mute microphone, set transmission mode to none
            SetTransmissionMode(TransmissionMode.None, null);
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
    
    private void ToggleButtons(bool enable)
    {
        _listenToAllChannelToggle.interactable = enable;
        _listenToTeamChannelToggle.interactable = enable;
        _muteListeningToggle.interactable = enable;
        
        _talkToAllChannelToggle.interactable = enable;
        _talkToTeamChannelToggle.interactable = enable;
        _muteMicrophoneToggle.interactable = enable;
    }
    
    public void LogActiveChannels()
    {
        if(VivoxService.Instance == null)
            return;
        
        StringBuilder logString = new StringBuilder();
        
        logString.AppendLine($"Active channels: {VivoxService.Instance.ActiveChannels.Count}");
        foreach (var channel in VivoxService.Instance.ActiveChannels)
        {
            logString.AppendLine($"Channel: {channel.Key} - Volume {channel.Value.Count}");
        }
        
        logString.AppendLine($"Transmitting channels: {VivoxService.Instance.TransmittingChannels.Count}");
        foreach(var channel in VivoxService.Instance.TransmittingChannels)
        {
            logString.AppendLine($"Channel: {channel}");
        }
        
        Debug.Log(logString.ToString());
        _logText.SetText(logString.ToString());
    }

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
    
}
