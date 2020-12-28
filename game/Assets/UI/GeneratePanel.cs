using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GeneratePanel : MonoBehaviour
{
    // Events

    [Serializable]
    public class ConfigPopulationPathEvent : UnityEvent<string, string> { }

    public ConfigPopulationPathEvent OnConfigPopulationSelected;

    // UI Elements

    [SerializeField] private Dropdown _configDropdown = default;

    [SerializeField] private Dropdown _populationDropdown = default;

    [SerializeField] private Button _refreshButton = default;

    [SerializeField] private Button _generateButton = default;

    // Internals

    private FilePathName[] _configs;

    private FilePathName[] _populations;

    public void RefreshFolders()
    {
        _configs = GetConfigs();
        _populations = GetPopulations();

        _configDropdown.options = _configs.Select(path => new Dropdown.OptionData(path.FileName)).ToList();
        _populationDropdown.options = _populations.Select(path => new Dropdown.OptionData(path.FileName)).ToList();
    }

    public void Freeze()
    {
        GetComponent<CanvasGroup>().interactable = false;
    }

    public void Unfreeze()
    {
        GetComponent<CanvasGroup>().interactable = true;
    }

    private void Awake()
    {
        _refreshButton.onClick.AddListener(RefreshFolders);
        _generateButton.onClick.AddListener(() =>
        {
            Freeze();

            OnConfigPopulationSelected.Invoke(
                _configs[_configDropdown.value].Path,
                _populations[_populationDropdown.value].Path
            );
        });

        EnsureDefaultConfigCreated();

        RefreshFolders();
    }

    private void Update()
    {
        _generateButton.interactable = _configDropdown.value > 0 || _populationDropdown.value > 0;
    }

    private void EnsureDefaultConfigCreated()
    {
        // We always regenerate the default config since it might have changed after an update

        var configsFolder = GameFolders.EnsureGameFolder(GameFolders.CONFIGS);
        var defaultConfigPath = Path.Combine(configsFolder, "default-config.json");

        Population.ExportDefaultConfig(defaultConfigPath);

    }

    private FilePathName[] GetConfigs()
    {
        return new [] 
        {
            new FilePathName
            {
                Path = null,
                FileName = "No Configuration",
            }
        }
        .Concat(Directory.GetFiles(GameFolders.EnsureGameFolder(GameFolders.CONFIGS), "*.json").Select(path => new FilePathName
        {
            Path = path,
            FileName = Path.GetFileName(path),
        }))
        .ToArray();
    }

    private FilePathName[] GetPopulations()
    {
        return new []
        {
            new FilePathName
            {
                Path = null,
                FileName = "Generate from config",
            }
        }
        .Concat(Directory.GetFiles(GameFolders.EnsureGameFolder(GameFolders.POPULATIONS), "*.json").Select(path => new FilePathName
        {
            Path = path,
            FileName = Path.GetFileName(path),
        }))
        .ToArray();
    }

    private class FilePathName
    {
        public string Path;
        public string FileName;
    }
}
