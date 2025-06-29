using Mono.Data.Sqlite;
using System;
using System.Data;

public static class Database
{
    private static string GetPath()
    {
        return "C:/Users/ISAAC/Desktop/db_EL/EL_Accounts.db";
    }
    private static string GetString(string commandText)
    {
        string path = GetPath();
        string text = null;
        SqliteConnection connection = new SqliteConnection("Data Source=" + path);
        connection.Open();

        if (connection.State == ConnectionState.Open)
        {
            SqliteCommand sqliteCommand = new SqliteCommand();
            sqliteCommand.Connection = connection;
            sqliteCommand.CommandText = commandText;
            SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
            text = Convert.ToString(sqliteDataReader[0]);
        }
        connection.Close();
        return text;
    }

    private static int GetInt(string commandText)
    {
        string path = GetPath();
        int value = 0;
        SqliteConnection connection = new SqliteConnection("Data Source=" + path);
        connection.Open();

        if (connection.State == ConnectionState.Open)
        {
            SqliteCommand sqliteCommand = new SqliteCommand();
            sqliteCommand.Connection = connection;
            sqliteCommand.CommandText = commandText;
            SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
            value = Convert.ToInt32(sqliteDataReader[0]);
        }
        connection.Close();
        return value;
    }
}