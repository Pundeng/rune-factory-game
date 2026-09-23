using UnityEngine;

namespace FantasyShapez.Logistics
{
    public interface IItemInputReceiver
    {
        Vector2Int InputCell { get; }

        bool AllowsConcurrentInput { get; }

        bool CanAcceptItem(ITransportItem item, GridDirection incomingDirection);

        bool TryAcceptItem(ITransportItem item, GridDirection incomingDirection);
    }
}
