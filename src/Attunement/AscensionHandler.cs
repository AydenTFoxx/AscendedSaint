using System.Collections.Generic;
using AscendedSaint.Meadow;
using ModLib.Meadow;
using MoreSlugcats;
using RWCustom;
using static ModLib.CompatibilityManager;
using static ModLib.Options.OptionUtils;

namespace AscendedSaint.Attunement;

public static partial class AscensionHandler
{
    /// <summary>
    /// Ascends or returns a creature back from life, depending on whether it was dead beforehand.
    /// </summary>
    /// <param name="creature">The creature to be targeted.</param>
    /// <param name="callingPlayer">The player who caused this action.</param>
    /// <returns><c>true</c> if the creature was successfully ascended/revived, <c>false</c> otherwise.</returns>
    public static void AscendCreature(Creature creature, Player callingPlayer)
    {
        if (creature.dead)
        {
            ModLib.Logger.LogInfo("Return! " + creature.Template.name);

            if (IsRainMeadowEnabled() && MeadowUtils.IsOnline)
            {
                MeadowHelper.TryReviveCreature(creature, () => ReviveCreature(creature, GetOptionValue(Options.REVIVAL_HEALTH_FACTOR)));
                MeadowHelper.RequestAscensionEffectsSync(creature);

                if (callingPlayer is not null)
                {
                    MeadowUtils.LogSystemMessage($"{(creature is Player player ? OnlineQueries.GetOnlineName(player) : creature.Template.name)} was revived by {OnlineQueries.GetOnlineName(callingPlayer)}.");
                }
            }
            else
            {
                ReviveCreature(creature, GetOptionValue(Options.REVIVAL_HEALTH_FACTOR));

                SpawnAscensionEffects(creature);
            }

            creature.Stun(creature is Player ? 40 : 100);
        }
        else if (creature is Player player && player == callingPlayer)
        {
            ModLib.Logger.LogInfo($"Ascend! {creature.Template.name}");

            creature.Die();

            if (IsRainMeadowEnabled() && MeadowUtils.IsOnline)
            {
                MeadowHelper.RequestAscensionEffectsSync(creature);

                MeadowUtils.LogSystemMessage($"{OnlineQueries.GetOnlineName(player)} self-ascended.");
            }
            else
            {
                SpawnAscensionEffects(creature, isRevival: false);
            }
        }
        else
        {
            ModLib.Logger.LogWarning($"Could not ascend or revive invalid creature: {creature}");
        }
    }

    /// <summary>
    /// Attempts to revive a given oracle/Iterator.
    /// </summary>
    /// <param name="oracle">The oracle to be revived.</param>
    public static void AscendOracle(Oracle oracle, Player callingPlayer)
    {
        if (!CanReviveOracle(oracle)) return;

        ModLib.Logger.LogInfo($"Return, Iterator! {GetOracleName(oracle.ID)}");

        if (IsRainMeadowEnabled() && MeadowUtils.IsOnline)
        {
            MeadowHelper.TryReviveCreature(oracle, () => ReviveOracle(oracle));
            MeadowHelper.RequestAscensionEffectsSync(oracle);

            MeadowUtils.LogSystemMessage($"{GetOracleName(oracle.ID)} was revived by {OnlineQueries.GetOnlineName(callingPlayer)}.");
        }
        else
        {
            ReviveOracle(oracle);

            SpawnAscensionEffects(oracle);
        }
    }

    /// <summary>
    /// Restores a creature's health and sets its state as "alive" once again.
    /// </summary>
    /// <param name="creature">The creature to be revived.</param>
    /// <param name="health">
    ///     The health to be restored for the newly revived creature.
    ///     For slugcats/slugpups, this is always <c>1f</c> (100%).
    /// </param>
    public static void ReviveCreature(Creature creature, float health = 1f)
    {
        AbstractCreature abstractCreature = creature.abstractCreature;

        if (abstractCreature.state is not HealthState healthState)
            healthState = new HealthState(abstractCreature);

        healthState.alive = true;
        healthState.health = creature is Player ? 1f : health;

        creature.dead = false;
        creature.killTag = null;
        creature.killTagCounter = 0;

        abstractCreature.abstractAI?.SetDestination(abstractCreature.pos);

        if (creature is Player player)
        {
            player.playerState.alive = true;
            player.playerState.permaDead = false;

            player.airInLungs = 0.1f;
            player.exhausted = true;
            player.aerobicLevel = 1f;

            if (player == player.room.game.FirstRealizedPlayer
                && player.room.game.cameras?[0].hud?.textPrompt is not null)
            {
                player.room.game.cameras[0].hud.textPrompt.gameOverMode = false;
            }
        }
        else
        {
            RemoveFromRespawnsList(creature);
        }
    }

    /// <summary>
    /// Revives a given iterator, rebinding them to the shackles of the Great Cycle once more. Unfinished business I'd say.
    /// </summary>
    /// <param name="oracle">The iterator to de-ascend.</param>
    /// <remarks>But why would you?</remarks>
    public static void ReviveOracle(Oracle oracle)
    {
        if (oracle.room?.game.session is not StoryGameSession storyGame) return;

        if (oracle.ID == MoreSlugcatsEnums.OracleID.CL)
        {
            Custom.Log("De-Ascend saint pebbles");

            storyGame.saveState.deathPersistentSaveData.ripPebbles = false;
        }
        else if (oracle.ID == Oracle.OracleID.SL)
        {
            Custom.Log("De-Ascend saint moon");

            List<OracleSwarmer> myNewSwarmers = [];

            for (int i = 0; i < 7; i++)
            {
                SLOracleSwarmer? swarmer = CreateSLOracleSwarmer(oracle);

                if (swarmer is null) continue;

                myNewSwarmers.Add(swarmer);
            }

            oracle.mySwarmers.AddRange(myNewSwarmers);

            (oracle.oracleBehavior as SLOracleBehavior)?.State.neuronsLeft = 7;

            storyGame.saveState.deathPersistentSaveData.ripMoon = false;
        }
        else
        {
            ModLib.Logger.LogWarning("Unknown Oracle has been revived: " + oracle.ID);
        }

        oracle.health = 1f;
    }
}