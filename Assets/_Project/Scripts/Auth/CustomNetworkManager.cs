using System.Linq;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    [Header("Auto Start Options")]
    [Tooltip("���� ��������, �� ��� ������� ������� ����������� ����� ������. ���� ���������, ����������� ������.")]
    [SerializeField] private bool autoStartServer = false;

    [Header("Maintenance Options")]
    [Tooltip("���� ��������, ����� ����������� ����������� (����� �� active).")]
    [SerializeField] private bool maintenanceMode = false;

    private bool hasDisconnectedClients = false;

    public override void Start()
    {
        base.Start();
        if (autoStartServer)
        {
            Debug.Log("Auto Start �������: ��������� ������.");
            StartServer();
        }
        else
        {
            Debug.Log("Auto Start ��������: ��������� �������.");
            StartClient();
        }
    }

    public void ToggleMaintenanceMode(bool active)
    {
        maintenanceMode = active;
        hasDisconnectedClients = false;
        Debug.Log("����� �� " + (maintenanceMode ? "�������" : "��������"));

        if (maintenanceMode)
            DisconnectAllClients();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (maintenanceMode)
        {
            conn.Send(new MaintenanceMessage { active = true });
            Debug.Log("����� ����� �����������, �� ������ � ������ ��. ���������.");
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
        Debug.Log("����� �� ������� � ��������� ���� ������������ ��������.");
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