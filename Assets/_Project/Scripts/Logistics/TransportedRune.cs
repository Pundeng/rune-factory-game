using System;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Logistics
{
    public sealed class TransportedRune
    {
        public TransportedRune(RuneData rune, GridDirection entryDirection)
        {
            Rune = rune ?? throw new ArgumentNullException(nameof(rune));
            EnterFrom(entryDirection);
        }

        public RuneData Rune { get; }

        public GridDirection EntryDirection { get; private set; }

        public float Progress { get; private set; }

        internal void Advance(float distance)
        {
            Progress = Mathf.Clamp01(Progress + distance);
        }

        internal void EnterFrom(GridDirection entryDirection)
        {
            EntryDirection = entryDirection;
            Progress = 0f;
        }
    }
}
