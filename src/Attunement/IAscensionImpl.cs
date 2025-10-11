namespace AscendedSaint.Attunement;

public interface IAscensionImpl
{
    /// <summary>
    ///     Attempts to ascend or revive the given creature.
    /// </summary>
    /// <param name="target">The creature to be ascended or revived.</param>
    /// <param name="caller">The player who caused this action.</param>
    /// <returns><c>true</c> if the target was ascended or revived, <c>false</c> otherwise.</returns>
    bool TryAscendCreature(Creature target, Player caller);

    /// <summary>
    ///     Attempts to revive the given oracle.
    /// </summary>
    /// <param name="target">The oracle to be revived.</param>
    /// <param name="caller">The player who caused this action.</param>
    /// <returns><c>true</c> if the target was revived, <c>false</c> otherwise.</returns>
    bool TryAscendOracle(Oracle target, Player caller);

    /// <summary>
    ///     Displays the visual effects for ascending or reviving a given object.
    /// </summary>
    /// <param name="target">The object which was ascended or revived.</param>
    /// <param name="isRevival">If the object was revived by the player.</param>
    void SpawnAscensionEffects(PhysicalObject target, bool isRevival);

    /// <summary>
    ///     Removes the given creature from the list of respawns for the next cycle.
    /// </summary>
    /// <param name="creature">The creature to be removed.</param>
    void RemoveFromRespawnsList(Creature creature);
}