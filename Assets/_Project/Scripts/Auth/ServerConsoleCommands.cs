using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

public class ServerConsoleCommands : MonoBehaviour
{
    private readonly ConcurrentQueue<string> commandQueue = new();

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Thread consoleThread = new(ConsoleInputLoop)
        {
            IsBackground = true
        };
        consoleThread.Start();
    }
    
    void ConsoleInputLoop()
    {
        while (true)
        {
            string command = System.Console.ReadLine();
            if (!string.IsNullOrEmpty(command))
            {
                commandQueue.Enqueue(command);
            }
        }
    }
    
    void Update()
    {
        while (commandQueue.TryDequeue(out string command))
        {
            if (command.Equals("maintenance on", System.StringComparison.OrdinalIgnoreCase))
            {
                CustomNetworkManager manager = FindObjectOfType<CustomNetworkManager>();
                if (manager != null)
                {
                    manager.ToggleMaintenanceMode(true);
                    Debug.Log("������� maintenance on �������.");
                }
            }
            else if (command.Equals("maintenance off", System.StringComparison.OrdinalIgnoreCase))
            {
                CustomNetworkManager manager = FindObjectOfType<CustomNetworkManager>();
                if (manager != null)
                {
                    manager.ToggleMaintenanceMode(false);
                    Debug.Log("������� maintenance off �������.");
                }
            }
            else
            {
                Debug.Log($"����������� �������: {command}");
            }
        }
    }
}