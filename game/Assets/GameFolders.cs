using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameFolders : MonoBehaviour
{
    public const string CONFIGS = "configs";
    public const string POPULATIONS = "populations";

    /// <summary>
    /// Ensures that a game folder exists and returns the full path to it
    /// </summary>
    public static string EnsureGameFolder(string folderName)
    {
        var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var configFolder = Path.Combine(documentsFolder, "CarBrains", folderName);
        Directory.CreateDirectory(configFolder);

        return configFolder;
    }
}
