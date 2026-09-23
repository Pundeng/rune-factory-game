using System;
using UnityEngine;

namespace FantasyShapez.Logistics
{
    public sealed class BeltCell
    {
        public BeltCell(Vector2Int cell, GridDirection direction)
        {
            Cell = cell;
            Direction = direction;
        }

        public Vector2Int Cell { get; }

        public GridDirection Direction { get; }

        public Vector2Int OutputCell => Cell + Direction.ToOffset();

        public TransportedRune Item { get; private set; }

        public bool HasItem => Item != null;

        public bool CanAccept => !HasItem;

        public bool TryAccept(ITransportItem item, GridDirection entryDirection)
        {
            if (!CanAccept)
            {
                return false;
            }

            Item = new TransportedRune(item, entryDirection);
            return true;
        }

        internal bool TryAccept(TransportedRune item, GridDirection entryDirection)
        {
            if (!CanAccept)
            {
                return false;
            }

            Item = item ?? throw new ArgumentNullException(nameof(item));
            Item.EnterFrom(entryDirection);
            return true;
        }

        internal void Advance(float distance)
        {
            Item?.Advance(distance);
        }

        internal TransportedRune TakeItem()
        {
            TransportedRune item = Item;
            Item = null;
            return item;
        }
    }
}
