using System;
using UnityEngine;

/// <summary>
/// ApiConfig dynamically resolves API keys and service endpoints from external environment
/// variables and runtime memory overrides. Strictly prohibits hardcoding keys into project files.
/// </summary>
public static class ApiConfig
{
    public static string GetSecretKey(string envVarName)
    {
        // 1. Check process environment
        string envVal = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrEmpty(envVal)) return envVal.Trim();

        // 2. Check external user profile
        try
        {
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(profile))
            {
                string keyFile = System.IO.Path.Combine(profile, $".{envVarName.ToLowerInvariant()}");
                if (System.IO.File.Exists(keyFile))
                {
                    return System.IO.File.ReadAllText(keyFile).Trim();
                }
            }
        }
        catch { }

        return "";
    }
}
