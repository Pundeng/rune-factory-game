using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Logistics
{
    public interface IRuneOutputSource
    {
        Vector2Int OutputCell { get; }

        GridDirection OutputDirection { get; }

        bool HasOutput { get; }

        RuneData PeekOutput();

        bool TryTakeOutput(out RuneData rune);
    }
}
