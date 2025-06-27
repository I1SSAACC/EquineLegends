using System;
using System.IO;
using UnityEngine;
using Mirror;

public class AuthManager : NetworkBehaviour
{
    public static AuthManager Instance;

    private string _accountsFilePath;
    private string _playerDataDirectory;

    [HideInInspector]
    public PlayerData CurrentPlayerData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        string basePath = Directory.GetCurrentDirectory();
        _accountsFilePath = Path.Combine(basePath, Constants.AccountsFileName);
        _playerDataDirectory = Path.Combine(basePath, Constants.PlayerDataFileName);

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

        AccountInfo account = db.accounts
            .Find(acc => acc.login.Equals(msg.login, StringComparison.OrdinalIgnoreCase));

        var response = new LoginResponseMessage();

        if (account == null)
        {
            response.success = false;
            response.message = "Аккаунт не найден.";
            conn.Send(response);
            return;
        }

        if (account.passwordHash != hashedPassword)
        {
            response.success = false;
            response.message = "Неверный пароль.";
            conn.Send(response);
            return;
        }

        // Загружаем PlayerData с диска
        PlayerData pd = LoadPlayerData(account.id);
        if (pd == null)
        {
            response.success = false;
            response.message = "Ошибка загрузки данных игрока.";
            conn.Send(response);
            return;
        }

        // Формируем успешный ответ
        response.success = true;
        response.message = "Авторизация успешна.";
        response.accountId = account.id;
        response.nickname = pd.nickname;
        response.gameCurrency = pd.gameCurrency;
        response.donationCurrency = pd.donationCurrency;
        response.level = pd.level;

        conn.Send(response);
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
        string fileName = $"{ accountId}{Constants.JsonExtension}";
        string path = Path.Combine(_playerDataDirectory, fileName);
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(path, json);
    }

    private PlayerData LoadPlayerData(string accountId)
    {
        string fileName = $"{accountId}{Constants.JsonExtension}";
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

    private void OnLoginResponse(LoginResponseMessage msg)
    {
        if (!msg.success) return;

        CurrentPlayerData = new PlayerData
        {
            id = msg.accountId,
            nickname = msg.nickname,
            gameCurrency = msg.gameCurrency,
            donationCurrency = msg.donationCurrency,
            level = msg.level
        };

        // Запускаем переход на загрузку сцены
        SceneTransitionManager.Instance.StartGameLoad();
    }
}