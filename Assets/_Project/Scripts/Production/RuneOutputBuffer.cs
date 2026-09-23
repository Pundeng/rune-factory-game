using System;
using System.Collections.Generic;
using FantasyShapez.Runes;

namespace FantasyShapez.Production
{
    public sealed class RuneOutputBuffer
    {
        private readonly Queue<RuneData> outputs = new();

        public RuneOutputBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
            }

            Capacity = capacity;
        }

        public int Capacity { get; }

        public int Count => outputs.Count;

        public bool HasOutput => outputs.Count > 0;

        public bool CanAcceptOutput => outputs.Count < Capacity;

        public RuneData PeekOutput()
        {
            return HasOutput ? outputs.Peek() : null;
        }

        public bool TryAdd(RuneData rune)
        {
            if (rune == null)
            {
                throw new ArgumentNullException(nameof(rune));
            }

            if (!CanAcceptOutput)
            {
                return false;
            }

            outputs.Enqueue(rune);
            return true;
        }

        public bool TryTakeOutput(out RuneData rune)
        {
            if (!HasOutput)
            {
                rune = null;
                return false;
            }

            rune = outputs.Dequeue();
            return true;
        }

        public void Clear()
        {
            outputs.Clear();
        }

        internal RuneOutputBuffer Copy()
        {
            var copy = new RuneOutputBuffer(Capacity);
            foreach (RuneData rune in outputs)
            {
                copy.TryAdd(rune.Copy());
            }

            return copy;
        }
    }
}
