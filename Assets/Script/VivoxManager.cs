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

public class VivoxManager : MonoBehaviour
{
    [SerializeField] private string UserDisplayName = "User";
    [SerializeField] private Text _statusText;
    [SerializeField] private TextMeshProUGUI _logText;
    
    // Speaker toggle buttons
    [SerializeField] private Toggle _speakToAllChannelToggle;
    [SerializeField] private Toggle _speakToTeamChannelToggle;
    [SerializeField] private Toggle _muteSpeakerToggle;
    
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
        
        await SetupAllChannels();
        
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
    }

    #region Join/Leave Channel related functions

    private async Task SetupAllChannels()
    {
        // Should we await these?
        SetTransmissionMode(TransmissionMode.None, null);
        
        Debug.Log($"Joining all channel");
        JoinGroupChannelAsync(AllChannelName);
        
        Debug.Log($"Joining Team channel");
        JoinGroupChannelAsync(TeamChannelName);
    }
    
    private async Task JoinGroupChannelAsync(string channelName)
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;
        
        float timeToJoinChannel = Time.time;
        _statusText.text = $"Joining {channelName} channel...";
        //ChannelOptions options = new ChannelOptions();
        //options.MakeActiveChannelUponJoining = false;
        
        #if UNITY_EDITOR
        await VivoxService.Instance.JoinEchoChannelAsync(channelName, ChatCapability.AudioOnly, null);
        #else
        await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly, null);
        #endif
        timeToJoinChannel = Time.time - timeToJoinChannel;
        _statusText.text = $"Joined {channelName} in {timeToJoinChannel:0.00}s";
        
        // Keep the channel volume to almost none till player taps on speaker button
        SetChannelVolume(channelName, MinVolume);
    }

    #endregion
    
    #region Vivox Speaker related functions
    
    public void ListenToAllButtonTap()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;
        
        SetChannelVolume(AllChannelName,  MaxVolume);
        SetChannelVolume(TeamChannelName,  MaxVolume);
    }
    
    public void ListenToTeamButtonTap()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;
        
        SetChannelVolume(AllChannelName,  MinVolume);
        SetChannelVolume(TeamChannelName,  MaxVolume);
    }

    public void MuteSpeakerButtonTap()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;

        if (_muteSpeakerToggle.isOn)
        {
            Debug.Log($"Mute speaker");
            SetChannelVolume(AllChannelName, MinVolume);
            SetChannelVolume(TeamChannelName, MinVolume);
        }
    }
    
    private void SetChannelVolume(string channelName, int volume)
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;
        
        Debug.Log($"Setting channel {channelName} volume to {volume}");
        VivoxService.Instance.SetChannelVolumeAsync(channelName, volume);
        _statusText.text = $"Channel {channelName} volume set to {volume}";
    }
    
    #endregion    

    #region Vivox Microphone related functions

    public void TalkToAllButtonTap()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;

        if (_talkToAllChannelToggle.isOn)
        {
            SetTransmissionMode(TransmissionMode.All, null);
        }
    }
    
    public async void TalkToTeamButtonTap()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
            return;

        if (_talkToTeamChannelToggle.isOn)
        {
            //await SetTransmissionMode(TransmissionMode.None, null);
            await SetTransmissionMode(TransmissionMode.Single, TeamChannelName);
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
        _speakToAllChannelToggle.interactable = enable;
        _speakToTeamChannelToggle.interactable = enable;
        _muteSpeakerToggle.interactable = enable;
        
        _talkToAllChannelToggle.interactable = enable;
        _talkToTeamChannelToggle.interactable = enable;
        _muteMicrophoneToggle.interactable = enable;
    }
    
    public void LogActiveChannels()
    {
        if(VivoxService.Instance == null || VivoxService.Instance.IsLoggedIn == false)
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
    
    private void RequestMicrophonePermission(Action<string> callback)
    {
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
}
