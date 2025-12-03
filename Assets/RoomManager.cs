using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    // Define a consistent app version for your game
    private const string GameAppVersion = "1.0";

    void Start()
    {
        Debug.Log("[RoomManager] Initializing Photon Network...");

        if (PhotonNetwork.PhotonServerSettings == null)
        {
            Debug.LogError("[RoomManager] CRITICAL: PhotonServerSettings is null, aborting connection.");
            return;
        }

        string appId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
        if (string.IsNullOrEmpty(appId))
        {
            Debug.LogError("[RoomManager] CRITICAL: AppIdRealtime is not configured, aborting connection.");
            return;
        }

        Debug.Log($"[RoomManager] Settings found:");
        Debug.Log($"  - AppId (partial): {appId.Substring(0, Math.Min(8, appId.Length))}***");
        Debug.Log($"  - PUN Version: {PhotonNetwork.PunVersion}");
        Debug.Log($"  - UseNameServer: {PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer}");
        Debug.Log($"  - FixedRegion: {PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion}");

        PhotonNetwork.AutomaticallySyncScene = true;

        // Set GameVersion BEFORE connecting
        PhotonNetwork.GameVersion = GameAppVersion;
        Debug.Log($"[RoomManager] GameVersion set to: {GameAppVersion}");

        Debug.Log("[RoomManager] Calling ConnectUsingSettings()...");
        bool connectResult = PhotonNetwork.ConnectUsingSettings();
        Debug.Log($"[RoomManager] ConnectUsingSettings() returned: {connectResult}");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[RoomManager] OnConnectedToMaster, joining or creating room 'main' directly.");

        var options = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.JoinOrCreateRoom("main", options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[RoomManager] OnJoinedRoom, joined room: {PhotonNetwork.CurrentRoom.Name}, players: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[RoomManager] OnCreateRoomFailed, Code: {returnCode}, Message: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("[RoomManager] OnJoinRoomFailed:");
        Debug.LogError($"  - Code: {returnCode}");
        Debug.LogError($"  - Message: {message}");
        Debug.LogError($"  - IsConnected: {PhotonNetwork.IsConnected}");
        Debug.LogError($"  - InLobby: {PhotonNetwork.InLobby}");
        Debug.LogError($"  - State: {PhotonNetwork.NetworkClientState}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("[RoomManager] OnDisconnected:");
        Debug.LogError($"  - Cause: {cause}");
        Debug.LogError($"  - State: {PhotonNetwork.NetworkClientState}");
    }
}
