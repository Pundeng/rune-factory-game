namespace FantasyShapez.Buildings
{
    public interface IBuildingRemovalRule
    {
        bool CanRemove { get; }
    }

    public interface IBuildingMoveState
    {
        bool CanMove { get; }

        void DetachForMove();
        void ReattachAfterFailedMove();
    }
}
