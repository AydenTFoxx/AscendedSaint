namespace AscendedSaint.Attunement;

public interface IAscensionImpl
{
    void AscendCreature(Creature target, Player caller);
    void AscendOracle(Oracle target, Player caller);
    void SpawnAscensionEffects(PhysicalObject target, bool isRevival);
}