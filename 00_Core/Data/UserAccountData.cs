using System;
using System.Collections.Generic;


[Serializable]
public class UserAccountData
{
    public string UserId;
    public string Username;
    public string PasswordHash;
    public string Salt;
    public DateTime CreatedDate;

    // 💾 Сейвы пользователя
    public List<string> SaveFiles = new();   // список saveId
    public string LastSaveFile;               // активный сейв
}