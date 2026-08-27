using System;
using System.Text;
using RoyalDecisions.Data;
using RoyalDecisions.Domain;
using UnityEngine;

namespace RoyalDecisions.Presentation
{
    /// <summary>
    /// Renders the four statistics. Never writes to a run.
    /// </summary>
    /// <remarks>
    /// It holds a <see cref="StatSystem"/> reference for exactly one reason — to subscribe to its
    /// two change events. It calls no mutator on it, and nothing here can reach a run's state.
    ///
    /// Subscription is explicit rather than tied to <c>OnEnable</c>/<c>OnDisable</c>: CLAUDE.md §10
    /// demands symmetry, and explicit methods make that provable in a test.
    /// </remarks>
    public sealed class HUDView : MonoBehaviour
    {
        private static readonly StatType[] RequiredStats =
        {
            StatType.People,
            StatType.Security,
            StatType.Authority,
            StatType.Wealth
        };

        [Tooltip("Exactly one item per statistic. Order does not matter; the stat on each does.")]
        [SerializeField] private StatItemView[] statItems = Array.Empty<StatItemView>();

        [SerializeField] private InterfaceTextDefinition interfaceText;

        private StatSystem boundSystem;

        public bool IsBound => boundSystem != null;

        public int ItemCount => statItems != null ? statItems.Length : 0;

        /// <summary>Renders all four statistics immediately.</summary>
        public void Render(StatValues values)
        {
            Render(values, false);
        }

        public void Render(StatValues values, bool animated)
        {
            if (statItems == null)
            {
                return;
            }

            for (int i = 0; i < statItems.Length; i++)
            {
                StatItemView item = statItems[i];
                if (item == null)
                {
                    continue;
                }

                // The StatValues struct is passed by value and only ever read from.
                item.SetValue(values[item.Stat], animated);
            }
        }

        /// <summary>Updates the one bar whose statistic moved.</summary>
        public void Apply(StatChange change)
        {
            StatItemView item = Find(change.Stat);

            if (item != null)
            {
                item.SetValue(change.Current, true);

                if (change.Delta != 0)
                {
                    item.TriggerValueChangeGlow();
                }
            }
        }

        public void ShowChoiceImpact(StatDeltas deltas, float strength)
        {
            if (statItems == null)
            {
                return;
            }

            for (int i = 0; i < statItems.Length; i++)
            {
                StatItemView item = statItems[i];
                if (item != null)
                {
                    item.ShowImpact(deltas[item.Stat], strength);
                }
            }
        }

        public void ClearChoiceImpact()
        {
            if (statItems == null)
            {
                return;
            }

            for (int i = 0; i < statItems.Length; i++)
            {
                statItems[i]?.ClearImpact();
            }
        }

        public void ApplyTheme(GameUITheme theme)
        {
            if (statItems == null)
            {
                return;
            }

            for (int i = 0; i < statItems.Length; i++)
            {
                statItems[i]?.ApplyTheme(theme);
            }

            ApplyLabels();
        }

        /// <summary>
        /// Subscribes to a stat system and draws its current values.
        /// Binding twice without unbinding is safe: the previous subscription is released first.
        /// </summary>
        public void Bind(StatSystem statSystem)
        {
            if (statSystem == null)
            {
                return;
            }

            Unbind();

            boundSystem = statSystem;
            boundSystem.StatChanged += OnStatChanged;
            boundSystem.StatsChanged += OnStatsChanged;

            Render(boundSystem.Current);
            ApplyLabels();
        }

        public void Unbind()
        {
            if (boundSystem == null)
            {
                return;
            }

            boundSystem.StatChanged -= OnStatChanged;
            boundSystem.StatsChanged -= OnStatsChanged;
            boundSystem = null;
        }

        public StatItemView Find(StatType stat)
        {
            if (statItems == null)
            {
                return null;
            }

            for (int i = 0; i < statItems.Length; i++)
            {
                StatItemView item = statItems[i];
                if (item != null && item.Stat == stat)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Reports a null slot, a duplicated statistic, or a missing one. A HUD quietly rendering
        /// three of four bars is worse than one that says why.
        /// </summary>
        public bool TryValidate(out string message)
        {
            StringBuilder problems = new StringBuilder();

            if (statItems == null || statItems.Length == 0)
            {
                message = "HUDView has no stat items assigned.";
                return false;
            }

            for (int i = 0; i < statItems.Length; i++)
            {
                if (statItems[i] == null)
                {
                    problems.Append("Stat item slot ").Append(i).Append(" is empty. ");
                }
            }

            for (int i = 0; i < RequiredStats.Length; i++)
            {
                StatType required = RequiredStats[i];
                int count = CountFor(required);

                if (count == 0)
                {
                    problems.Append("No item renders ").Append(required).Append(". ");
                }
                else if (count > 1)
                {
                    problems.Append(count).Append(" items render ").Append(required).Append(". ");
                }
            }

            message = problems.ToString().TrimEnd();
            return message.Length == 0;
        }

        private int CountFor(StatType stat)
        {
            int count = 0;

            for (int i = 0; i < statItems.Length; i++)
            {
                if (statItems[i] != null && statItems[i].Stat == stat)
                {
                    count++;
                }
            }

            return count;
        }

        private void OnStatChanged(StatChange change)
        {
            Apply(change);
        }

        private void OnStatsChanged(StatValues values)
        {
            Render(values, true);
        }

        private void OnDestroy()
        {
            // A destroyed view must not keep a live subscription on a system that outlives it.
            Unbind();
        }

        private void Awake()
        {
            ApplyLabels();
        }

        public void ApplyLabels()
        {
            if (interfaceText == null || statItems == null)
            {
                return;
            }

            for (int i = 0; i < statItems.Length; i++)
            {
                StatItemView item = statItems[i];
                if (item != null)
                {
                    item.SetLabel(interfaceText.GetStatLabel(item.Stat));
                }
            }
        }

        private void OnValidate()
        {
            if (statItems == null || statItems.Length == 0)
            {
                return;
            }

            if (!TryValidate(out string message))
            {
                Debug.LogWarning("[HUDView] " + message, this);
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor-only wiring hook shared by prefab setup and tests.</summary>
        public void SetAuthoringReferences(
            StatItemView[] items,
            InterfaceTextDefinition text = null)
        {
            statItems = items ?? Array.Empty<StatItemView>();
            interfaceText = text;
        }
#endif
    }
}
