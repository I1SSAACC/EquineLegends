﻿using System.Linq;
using Mirror;
using UnityEngine;
public struct MovePlayerMessage : NetworkMessage { }

public class CustomNetworkManager : NetworkManager
{
    [SerializeField] private bool autoStartServer;
    [SerializeField] private bool maintenanceMode;
    private bool hasDisconnectedClients = false;

    public override void Start()
    {
        base.Start();
        if (autoStartServer)
        {
            Debug.Log("Auto Start включен: запускаем сервер.");
            StartServer();
        }
        else
        {
            Debug.Log("Auto Start выключен: запускаем клиента.");
            StartClient();
        }
    }

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<MovePlayerMessage>(OnMovePlayerMessage, false);
    }

    void OnMovePlayerMessage(NetworkConnectionToClient conn, MovePlayerMessage msg)
    {
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public void ToggleMaintenanceMode(bool active)
    {
        maintenanceMode = active;
        hasDisconnectedClients = false;
        Debug.Log("Режим ТО " + (maintenanceMode ? "включён" : "выключён"));
        if (maintenanceMode)
            DisconnectAllClients();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (maintenanceMode)
        {
            conn.Send(new MaintenanceMessage { active = true });
            Debug.Log("Новый игрок подключился, но сервер в режиме ТО. Отключаем.");
            conn.Disconnect();
            return;
        }
        base.OnServerConnect(conn);
    }

    public override void Update()
    {
        if (maintenanceMode
            && !hasDisconnectedClients
            && NetworkServer.active
            && NetworkServer.connections.Count > 0)
        {
            DisconnectAllClients();
        }
    }

    private void DisconnectAllClients()
    {
        Debug.Log("Режим ТО активен — отключаем всех подключённых клиентов.");
        var connections = NetworkServer.connections.Values.ToList();
        for (int i = 0; i < connections.Count; i++)
        {
            var conn = connections[i];
            conn.Send(new MaintenanceMessage { active = true });
            conn.Disconnect();
        }
        hasDisconnectedClients = true;
    }

    public void SpawnPlayer(NetworkConnectionToClient conn)
    {
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);
    }
}

public struct MaintenanceMessage : NetworkMessage
{
    public bool active;
}