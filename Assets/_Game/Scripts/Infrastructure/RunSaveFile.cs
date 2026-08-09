using System;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// The on-disk shape of a run save: a version, a format marker, and the payload.
    /// </summary>
    /// <remarks>
    /// The envelope's version is the authoritative one. <see cref="RunState"/> carries its own
    /// <c>saveVersion</c> too, but that is a nested detail of the payload; versioning acts on the
    /// envelope so the file format can evolve without the domain model having to.
    ///
    /// <see cref="Format"/> and <see cref="RunSavePayload.Kind"/> are required markers with
    /// deliberately invalid defaults. Together they make a truncated file — <c>{"saveVersion":1}</c>
    /// and anything like it — distinguishable from a genuine save, which no amount of inspecting
    /// the run itself can achieve: every field in a run has a legitimate default value.
    /// </remarks>
    [Serializable]
    public sealed class RunSaveFile
    {
        /// <summary>The exact value <see cref="Format"/> must carry for a file to be trusted.</summary>
        public const string FormatMarker = "royaldecisions.save";

        [SerializeField] private int saveVersion;
        [SerializeField] private string format;
        [SerializeField] private RunSavePayload payload;

        /// <summary>
        /// Deserialization constructor. Deliberately leaves <see cref="Format"/> null and the
        /// payload absent — see <see cref="RunSavePayload"/> for why these must never be defaulted
        /// to valid values.
        /// </summary>
        public RunSaveFile()
        {
        }

        public RunSaveFile(int saveVersion, RunState run)
        {
            this.saveVersion = saveVersion;
            format = FormatMarker;
            payload = new RunSavePayload(run);
        }

        public int SaveVersion => saveVersion;

        public string Format => format;

        public RunSavePayload Payload => payload;

        public bool HasValidMarker => string.Equals(format, FormatMarker, StringComparison.Ordinal);
    }
}
