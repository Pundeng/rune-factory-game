using UnityEngine;

namespace FantasyShapez.Logistics
{
    public interface IItemOutputSource
    {
        Vector2Int OutputCell { get; }

        GridDirection OutputDirection { get; }

        bool HasOutput { get; }

        ITransportItem PeekOutput();

        bool TryTakeOutput(out ITransportItem item);
    }
}
