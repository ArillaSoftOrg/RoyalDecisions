using System;
using UnityEngine;

namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// Just enough of a save file to learn its version.
    /// </summary>
    /// <remarks>
    /// JsonUtility ignores fields a type does not declare, so parsing a whole save file into this
    /// reads the version without touching — or trusting — anything else. That ordering matters: a
    /// file written by a newer build must be recognised as such before its payload is interpreted
    /// against the current schema.
    /// </remarks>
    [Serializable]
    public sealed class SaveFileHeader
    {
        [SerializeField] private int saveVersion;

        public int SaveVersion => saveVersion;
    }
}
