using AscendedSaint.Attunement;
using RWCustom;
using UnityEngine;
using static AscendedSaint.AscendedSaintMain;

namespace AscendedSaint.Endings;

/// <summary>
/// Utility functions for managing the mod's custom ascension endings.
/// </summary>
public static class VoidUtils
{
    /// <summary>
    /// Whether or not the player has ascended Looks to the Moon in the given game session.
    /// </summary>
    /// <param name="gameSession">The game session to be tested.</param>
    /// <returns><c>true</c> if this Iterator was ascended by the player, <c>false</c> otherwise.</returns>
    public static bool DidAscendMoon(GameSession gameSession) => gameSession is StoryGameSession storySession && storySession.saveState.deathPersistentSaveData.ripMoon;

    /// <summary>
    /// Whether or not the player has ascended Five Pebbles in the given game session.
    /// </summary>
    /// <param name="gameSession">The game session to be tested.</param>
    /// <returns><c>true</c> if this Iterator was ascended by the player, <c>false</c> otherwise.</returns>
    public static bool DidAscendPebbles(GameSession gameSession) => gameSession is StoryGameSession storySession && storySession.saveState.deathPersistentSaveData.ripPebbles;

    /// <summary>
    /// Determines if exactly one Iterator was ascended in the given game session.
    /// </summary>
    /// <param name="gameSession">The game session to be tested.</param>
    /// <returns><c>true</c> if only one Iterator is recorded to be ascended, <c>false</c> otherwise.</returns>
    /// <remarks>No further records of Iterator ascension are kept beyond what is in the vanilla game. Reviving an Iterator will count as not having ascended it at all.</remarks>
    public static bool AscendedOneIterator(GameSession gameSession) => DidAscendMoon(gameSession) != DidAscendPebbles(gameSession);

    /// <summary>
    /// Determines if both Iterator were ascended in the given game session.
    /// </summary>
    /// <param name="gameSession">The game session to be tested.</param>
    /// <returns><c>true</c> if both Iterators are recorded to be ascended, <c>false</c> otherwise.</returns>
    /// <remarks>No further records of Iterator ascension are kept beyond what is in the vanilla game. Reviving an Iterator will count as not having ascended it at all.</remarks>
    public static bool AscendedAllIterators(GameSession gameSession) => DidAscendMoon(gameSession) && DidAscendPebbles(gameSession);

    /// <summary>
    /// Determines if Saint's ascension ending should be overriden by this mod.
    /// </summary>
    /// <param name="player">The player to be tested.</param>
    /// <returns><c>true</c> if the mod's alternate endings should play, <c>false</c> otherwise.</returns>
    public static bool ShouldPlayAlternateEnding(Player player) => !AscendedOneIterator(player?.room.game.session as StoryGameSession) && ClientOptions.dynamicEndings;

    /// <summary>
    /// Plays the animation of activating a Karma Shrine, optionally also setting the player's karma to its max value as well.
    /// </summary>
    /// <param name="room">The room to spawn the effect into.</param>
    /// <param name="storySession">The game session of the player.</param>
    public static void RestorePlayerKarma(Player player)
    {
        if (player.room.game.session is not StoryGameSession storySession) return;

        storySession.saveState.deathPersistentSaveData.karmaCap = 9;
        storySession.saveState.deathPersistentSaveData.karma = storySession.saveState.deathPersistentSaveData.karmaCap;

        if (player.room.game.cameras[0].hud != null)
        {
            player.room.game.cameras[0].hud.karmaMeter.reinforceAnimation = 1;
        }

        player.room.PlaySound(SoundID.SB_A14, 0f, 1f, 1f);

        for (int i = 0; i < 20; i++)
        {
            player.room.AddObject(new MeltLights.MeltLight(1f, player.room.RandomPos(), player.room, RainWorld.GoldRGB));
        }
    }

    public class PlayerRevivalSprite : CosmeticSprite
    {
        private static int KillSprite => 0;

        private readonly Player player;

        private float killFac;
        private float lastKillFac;

        private float effectAdd;
        private readonly float effectInitLevel;
        private readonly RoomSettings.RoomEffect meltEffect;

        public PlayerRevivalSprite(Player player)
        {
            this.player = player;
            room = player.room;

            for (int i = 0; i < room.roomSettings.effects.Count; i++)
            {
                if (room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.VoidMelt)
                {
                    meltEffect = room.roomSettings.effects[i];
                    effectInitLevel = meltEffect.amount;
                    return;
                }
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            lastKillFac = killFac;

            if (player.dead)
            {
                killFac += 0.025f;
                if (killFac >= 1f)
                {
                    player.mainBodyChunk.vel += Custom.RNV() * 12f;
                    for (int k = 0; k < 20; k++)
                    {
                        player.room.AddObject(new Spark(player.mainBodyChunk.pos, Custom.RNV() * Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                    }

                    ASUtils.AscendCreature(player, null);

                    RestorePlayerKarma(player);

                    effectAdd = 3f;

                    (player.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark = true;
                }
            }
            else if (effectAdd > 0f)
            {
                effectAdd = Mathf.Max(0f, effectAdd - 0.03666667f);

                if (meltEffect != null)
                {
                    meltEffect.amount = Mathf.Lerp(effectInitLevel, 1f, Custom.SCurve(effectAdd, 0.6f));
                }
            }
            else
            {
                Destroy();
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];

            sLeaser.sprites[KillSprite] = new FSprite("Futile_White", true)
            {
                shader = rCam.game.rainWorld.Shaders["FlatLight"]
            };

            AddToContainer(sLeaser, rCam, null);
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) =>
            rCam.ReturnFContainer("Shortcuts").AddChild(sLeaser.sprites[KillSprite]);

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (killFac > 0f)
            {
                sLeaser.sprites[KillSprite].isVisible = true;

                sLeaser.sprites[KillSprite].x = Mathf.Lerp(player.mainBodyChunk.lastPos.x, player.mainBodyChunk.pos.x, timeStacker) - camPos.x;
                sLeaser.sprites[KillSprite].y = Mathf.Lerp(player.mainBodyChunk.lastPos.y, player.mainBodyChunk.pos.y, timeStacker) - camPos.y;

                float num = Mathf.Lerp(lastKillFac, killFac, timeStacker);
                sLeaser.sprites[KillSprite].scale = Mathf.Lerp(200f, 2f, Mathf.Pow(num, 0.5f));
                sLeaser.sprites[KillSprite].alpha = Mathf.Pow(num, 3f);
            }

            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }
    }
}