using System;
using System.IO;
using UnityEngine;
using Mirror;

public class AuthManager : NetworkBehaviour
{
    public static AuthManager Instance;

    private string _accountsFilePath;
    private string _playerDataDirectory;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        string basePath = Directory.GetCurrentDirectory();
        _accountsFilePath = Path.Combine(basePath, "Accounts.json");
        _playerDataDirectory = Path.Combine(basePath, "PlayerData");

        if (Directory.Exists(_playerDataDirectory) == false)
            Directory.CreateDirectory(_playerDataDirectory);

        if (File.Exists(_accountsFilePath) == false)
        {
            AccountsDatabase newDB = new();
            SaveAccountsDatabase(newDB);
        }
    }

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<RegisterRequestMessage>(OnRegisterRequest, false);
        NetworkServer.RegisterHandler<LoginRequestMessage>(OnLoginRequest, false);
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<RegisterResponseMessage>(OnRegisterResponse, false);
        NetworkClient.RegisterHandler<LoginResponseMessage>(OnLoginResponse, false);
    }

    private void OnRegisterRequest(NetworkConnectionToClient conn, RegisterRequestMessage msg)
    {
        Debug.Log("Запрос регистрации для логина: " + msg.login);

        string hashedPassword = Utils.ComputeSHA512Hash(msg.password);
        AccountsDatabase db = LoadAccountsDatabase();

        if (db.accounts.Exists(acc => acc.login.Equals(msg.login, StringComparison.OrdinalIgnoreCase)) == true)
        {
            RegisterResponseMessage response = new()
            {
                success = false,
                message = "Логин уже существует."
            };
            conn.Send(response);
            return;
        }

        AccountInfo account = new()
        {
            id = Guid.NewGuid().ToString(),
            email = msg.email,
            login = msg.login,
            passwordHash = hashedPassword
        };

        db.accounts.Add(account);
        SaveAccountsDatabase(db);

        PlayerData playerData = new()
        {
            id = account.id,
            nickname = msg.login,
            gameCurrency = 100,
            donationCurrency = 0,
            level = 1
        };
        SavePlayerData(account.id, playerData);

        RegisterResponseMessage successResponse = new()
        {
            success = true,
            message = "Регистрация успешна."
        };
        conn.Send(successResponse);
        Debug.Log("Регистрация успешна для логина: " + msg.login);
    }

    private void OnLoginRequest(NetworkConnectionToClient conn, LoginRequestMessage msg)
    {
        Debug.Log("Запрос авторизации для логина: " + msg.login);

        string hashedPassword = Utils.ComputeSHA512Hash(msg.password);
        AccountsDatabase db = LoadAccountsDatabase();

        AccountInfo account = db.accounts.Find(acc => acc.login.Equals(msg.login, StringComparison.OrdinalIgnoreCase));
        if (account == null)
        {
            LoginResponseMessage response = new ()
            {
                success = false,
                message = "Аккаунт не найден."
            };
            conn.Send(response);
            return;
        }

        if (account.passwordHash != hashedPassword)
        {
            LoginResponseMessage response = new()
            {
                success = false,
                message = "Неверный пароль."
            };
            conn.Send(response);
            return;
        }

        PlayerData playerData = LoadPlayerData(account.id);
        string playerDataJson = JsonUtility.ToJson(playerData);
        LoginResponseMessage successResponse = new()
        {
            success = true,
            message = "Авторизация успешна.",
            accountId = account.id,
            playerDataJson = playerDataJson
        };
        conn.Send(successResponse);
        Debug.Log("Авторизация успешна для логина: " + msg.login);
    }

    private AccountsDatabase LoadAccountsDatabase()
    {
        if (File.Exists(_accountsFilePath) == false)
        {
            AccountsDatabase emptyDB = new();
            SaveAccountsDatabase(emptyDB);
            return emptyDB;
        }
        string json = File.ReadAllText(_accountsFilePath);
        AccountsDatabase db = JsonUtility.FromJson<AccountsDatabase>(json);
        db ??= new AccountsDatabase();
        return db;
    }

    private void SaveAccountsDatabase(AccountsDatabase db)
    {
        string json = JsonUtility.ToJson(db, true);
        File.WriteAllText(_accountsFilePath, json);
    }

    private void SavePlayerData(string accountId, PlayerData playerData)
    {
        string fileName = accountId + ".json";
        string path = Path.Combine(_playerDataDirectory, fileName);
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(path, json);
    }

    private PlayerData LoadPlayerData(string accountId)
    {
        string fileName = accountId + ".json";
        string path = Path.Combine(_playerDataDirectory, fileName);
        if (File.Exists(path) == true)
        {
            string json = File.ReadAllText(path);
            PlayerData data = JsonUtility.FromJson<PlayerData>(json);
            return data;
        }
        else
        {
            Debug.LogWarning("Отсутствует файл данных игрока по пути: " + path);
            return null;
        }
    }

    private void OnRegisterResponse(RegisterResponseMessage msg) =>
        Debug.Log("Ответ регистрации (клиент): " + msg.message);

    private void OnLoginResponse(LoginResponseMessage msg) =>
        Debug.Log("Ответ авторизации (клиент): " + msg.message);
}