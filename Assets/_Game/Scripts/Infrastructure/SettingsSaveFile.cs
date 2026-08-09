using System;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// The on-disk shape of the settings file: a version and the settings it describes.
    /// </summary>
    [Serializable]
    public sealed class SettingsSaveFile
    {
        [SerializeField] private int saveVersion;
        [SerializeField] private GameSettings settings;

        public SettingsSaveFile()
        {
        }

        public SettingsSaveFile(int saveVersion, GameSettings settings)
        {
            this.saveVersion = saveVersion;
            this.settings = settings;
        }

        public int SaveVersion => saveVersion;

        public GameSettings Settings => settings;
    }
}
