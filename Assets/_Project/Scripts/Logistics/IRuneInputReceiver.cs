using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Logistics
{
    public interface IRuneInputReceiver
    {
        Vector2Int InputCell { get; }

        GridDirection RequiredIncomingDirection { get; }

        bool CanAcceptInput { get; }

        bool AllowsConcurrentInput { get; }

        bool CanAcceptInputFrom(GridDirection incomingDirection);

        bool TryAcceptInput(RuneData rune, GridDirection incomingDirection);
    }
}
