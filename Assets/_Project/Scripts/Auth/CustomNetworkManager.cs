using System.Linq;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    [Header("Auto Start Options")]
    [Tooltip("Если включено, то при запуске проекта запускается сразу сервер. Если отключено, запускается клиент.")]
    [SerializeField] private bool autoStartServer = false;

    [Header("Maintenance Options")]
    [Tooltip("Если включено, новые подключения отключаются (режим ТО active).")]
    [SerializeField] private bool maintenanceMode = false;

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

    void Update()
    {
        if (maintenanceMode && !hasDisconnectedClients && NetworkServer.active && NetworkServer.connections.Count > 0)
            DisconnectAllClients();
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
}
public struct MaintenanceMessage : NetworkMessage
{
    public bool active;
}