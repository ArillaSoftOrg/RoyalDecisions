using System;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Infrastructure
{
    /// <summary>
    /// The run inside a save file, behind a marker that proves it was really written.
    /// </summary>
    /// <remarks>
    /// JsonUtility materialises nested serializable objects even when the JSON omits them
    /// entirely, so the presence of a payload object proves nothing. What does prove something is
    /// <see cref="Kind"/>: it is set only by the writing constructor, so a payload conjured out of
    /// an absent field arrives with it null and is rejected.
    ///
    /// This is why the marker must never gain a field initializer or be set in the parameterless
    /// constructor — that would hand the forgery the very credential it is missing.
    /// </remarks>
    [Serializable]
    public sealed class RunSavePayload
    {
        /// <summary>The exact value <see cref="Kind"/> must carry for a payload to be trusted.</summary>
        public const string KindMarker = "run";

        [SerializeField] private string kind;
        [SerializeField] private RunState run;

        /// <summary>
        /// Deserialization constructor. Deliberately leaves <see cref="Kind"/> null so an omitted
        /// payload cannot pass validation.
        /// </summary>
        public RunSavePayload()
        {
        }

        public RunSavePayload(RunState run)
        {
            kind = KindMarker;
            this.run = run;
        }

        public string Kind => kind;

        public RunState Run => run;

        public bool HasValidMarker => string.Equals(kind, KindMarker, StringComparison.Ordinal);
    }
}
