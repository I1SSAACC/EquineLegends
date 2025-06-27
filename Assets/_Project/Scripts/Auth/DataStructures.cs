using System;
using System.Collections.Generic;
using Mirror;

[Serializable]
public class AccountInfo
{
    public string id;
    public string email;
    public string login;
    public string passwordHash;
}

[Serializable]
public class AccountsDatabase
{
    public List<AccountInfo> accounts = new();
}

[Serializable]
public class PlayerData
{
    public string id;
    public string nickname;
    public int gameCurrency;
    public int donationCurrency;
    public int level;
}

public struct RegisterRequestMessage : NetworkMessage
{
    public string email;
    public string login;
    public string password;
}

public struct RegisterResponseMessage : NetworkMessage
{
    public bool success;
    public string message;
}

public struct LoginRequestMessage : NetworkMessage
{
    public string login;
    public string password;
}

public struct LoginResponseMessage : NetworkMessage
{
    public bool success;
    public string message;
    public string accountId;
    public string nickname;
    public int gameCurrency;
    public int donationCurrency;
    public int level;
}