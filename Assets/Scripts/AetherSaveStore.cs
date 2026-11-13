using System.Collections.Generic;
using UnityEngine;

/// <summary>Journey persistence. Validation uses an isolated store so a test cannot overwrite a player's journey.</summary>
public static class AetherSaveStore
{
    static readonly Dictionary<string,string> validation = new Dictionary<string,string>();
    static bool IsValidation => AetherGame.I != null && AetherGame.I.IsSmokeMode;
    public static bool HasKey(string key) => IsValidation ? validation.ContainsKey(key) : PlayerPrefs.HasKey(key);
    public static string GetString(string key) => IsValidation ? (validation.TryGetValue(key,out string value)?value:"") : PlayerPrefs.GetString(key);
    public static void SetString(string key,string value)
    {
        if(IsValidation)validation[key]=value;
        else {PlayerPrefs.SetString(key,value);PlayerPrefs.Save();}
    }
}
