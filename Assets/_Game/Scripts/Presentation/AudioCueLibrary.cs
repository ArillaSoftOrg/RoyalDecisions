using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Maps audio event IDs to clips.
    /// </summary>
    /// <remarks>
    /// An explicit serialized asset rather than a folder scan: CLAUDE.md §7 rules out
    /// <c>Resources.Load</c> and scene searches, and an asset reference is the only mapping that
    /// survives a build with no surprises.
    ///
    /// Lookup is ordinal, so <c>"sfx_seal"</c> and <c>"SFX_Seal"</c> are different cues on every
    /// device locale.
    /// </remarks>
    [CreateAssetMenu(
        menuName = "Royal Decisions/Audio Cue Library",
        fileName = "AudioCueLibrary")]
    public class AudioCueLibrary : ScriptableObject
    {
        [SerializeField] private AudioCue[] cues = Array.Empty<AudioCue>();

        private Dictionary<string, AudioCue> index;

        public int CueCount => cues != null ? cues.Length : 0;

        public bool TryGetCue(string id, out AudioCue cue)
        {
            cue = null;

            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            EnsureIndex();
            return index.TryGetValue(id, out cue);
        }

        /// <summary>Rebuilds the lookup. Call after changing cues at runtime.</summary>
        public void RebuildIndex()
        {
            index = new Dictionary<string, AudioCue>(StringComparer.Ordinal);

            if (cues == null)
            {
                return;
            }

            for (int i = 0; i < cues.Length; i++)
            {
                AudioCue cue = cues[i];

                if (cue == null || string.IsNullOrEmpty(cue.Id))
                {
                    continue;
                }

                // First entry wins; a duplicate ID is an authoring mistake, not a reason to throw.
                if (!index.ContainsKey(cue.Id))
                {
                    index.Add(cue.Id, cue);
                }
            }
        }

        private void EnsureIndex()
        {
            if (index == null)
            {
                RebuildIndex();
            }
        }

        private void OnDisable()
        {
            // Dropped so a domain reload cannot leave a stale lookup behind.
            index = null;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only authoring hook shared by asset setup and tests.</summary>
        public void SetAuthoringData(AudioCue[] authoredCues)
        {
            cues = authoredCues ?? Array.Empty<AudioCue>();
            RebuildIndex();
        }
#endif
    }
}
