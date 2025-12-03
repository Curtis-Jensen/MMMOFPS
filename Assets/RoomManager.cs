using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    // Define a consistent app version for your game
    private const string GameAppVersion = "1.0";

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log($"[RoomManager] Initializing Photon Network...");

        // Verify settings are configured
        if (PhotonNetwork.PhotonServerSettings == null)
        {
            Debug.LogError("[RoomManager] CRITICAL: PhotonServerSettings is null! Setup is required in Assets > Resources > PhotonServerSettings. Aborting connection.");
            return;
        }

        string appId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
        if (string.IsNullOrEmpty(appId))
        {
            Debug.LogError("[RoomManager] CRITICAL: PhotonServerSettings AppId (Realtime) is not configured! Aborting connection.");
            return;
        }

        Debug.Log($"[RoomManager] Settings found:");
        Debug.Log($"  - AppId: {appId.Substring(0, Math.Min(8, appId.Length))}***");
        Debug.Log($"  - PUN Version: {PhotonNetwork.PunVersion}");
        Debug.Log($"  - UseNameServer: {PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer}");
        Debug.Log($"  - FixedRegion: {PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion}");

        // Enable automatic scene sync for multiplayer
        PhotonNetwork.AutomaticallySyncScene = true;

        // Connect using settings
        Debug.Log($"[RoomManager] Calling ConnectUsingSettings()...");
        bool connectResult = PhotonNetwork.ConnectUsingSettings();
        
        // Set GameVersion AFTER ConnectUsingSettings (following PUN 2 best practices)
        PhotonNetwork.GameVersion = GameAppVersion;
        
        Debug.Log($"[RoomManager] ConnectUsingSettings() returned: {connectResult}");
        Debug.Log($"[RoomManager] GameVersion set to: {GameAppVersion}");
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        Debug.Log($"[RoomManager] OnConnectedToMaster() - Connected to Photon Master Server");
        Debug.Log($"[RoomManager] Attempting to join lobby...");
        bool joinLobbyResult = PhotonNetwork.JoinLobby();
        Debug.Log($"[RoomManager] JoinLobby() returned: {joinLobbyResult}");
        
        // IMPORTANT: Some Photon Cloud configurations don't require explicit lobby join
        // If lobby join seems to fail, try direct room operations
        if (!joinLobbyResult)
        {
            Debug.LogWarning("[RoomManager] JoinLobby() returned false. Attempting direct room join...");
            PhotonNetwork.JoinOrCreateRoom("main", null, null);
        }
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();

        Debug.Log($"[RoomManager] OnJoinedLobby() - Successfully in lobby");
        Debug.Log($"[RoomManager] OnJoinedLobby() - Attempting to join or create room 'main'");
        PhotonNetwork.JoinOrCreateRoom("main", null, null);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        Debug.Log($"[RoomManager] OnJoinedRoom() - Successfully joined room: {PhotonNetwork.CurrentRoom.Name} with {PhotonNetwork.CurrentRoom.PlayerCount} player(s)");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        Debug.LogError($"[RoomManager] OnCreateRoomFailed() - Failed to create room. Code: {returnCode}, Message: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        Debug.LogError($"[RoomManager] OnJoinRoomFailed():");
        Debug.LogError($"  - Code: {returnCode}");
        Debug.LogError($"  - Message: {message}");
        Debug.LogError($"  - IsConnected: {PhotonNetwork.IsConnected}");
        Debug.LogError($"  - InLobby: {PhotonNetwork.InLobby}");
        
        // Error 32752 = "Unsupported Plugin" - usually means AppId validation failure
        // This can happen if:
        // 1. AppId doesn't exist in Photon Cloud
        // 2. AppId was created for a different protocol/region
        // 3. AppId has reached CCU limits
        if (returnCode == 32752)
        {
            Debug.LogError("[RoomManager] ERROR 32752 is 'Unsupported Plugin' - Check your AppId on Photon Dashboard!");
            Debug.LogError("[RoomManager] Verify: Assets > Resources > PhotonServerSettings > AppIdRealtime");
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        Debug.LogError($"[RoomManager] OnDisconnected():");
        Debug.LogError($"  - Cause: {cause}");
        
        // Common causes and solutions
        switch (cause)
        {
            case DisconnectCause.None:
                Debug.Log("[RoomManager] Disconnected normally.");
                break;
            case DisconnectCause.ExceptionOnConnect:
                Debug.LogError("[RoomManager] Exception during connection - check network and AppId!");
                break;
            case DisconnectCause.DisconnectByServerLogic:
                Debug.LogError("[RoomManager] Server disconnected us - may be AppId issue!");
                break;
            case DisconnectCause.InvalidAuthentication:
                Debug.LogError("[RoomManager] Invalid authentication - AppId may be wrong!");
                break;
            default:
                Debug.LogError($"[RoomManager] Disconnected for reason: {cause}");
                break;
        }
    }
}
