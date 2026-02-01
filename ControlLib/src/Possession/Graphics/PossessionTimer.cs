using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace ControlLib.Possession.Graphics;

public class PossessionTimer(PossessionManager manager)
    : PlayerAccessory(manager)
{
    private int PipSpritesLength =>
        Manager.IsAttunedSlugcat
            ? 16
            : Manager.IsHardmodeSlugcat
                ? 8
                : 12;

    private float rubberRadius;

    private bool IsPlayerVisible => player.room is not null && !player.dead;
    private bool ShouldShowPips => IsPlayerVisible && (Manager.IsPossessing || Manager.PossessionTime < Manager.MaxPossessionTime);

    private Color PipColor =>
        player.graphicsModule is PlayerGraphics playerGraphics
            ? GetPlayerColor(playerGraphics)
            : Color.white;

    private Color FlashingPipColor =>
        Manager.TargetSelector.State is TargetSelector.QueryingState
            ? PipColor == Color.white
                ? RainWorld.GoldRGB
                : Color.white
            : Color.red;

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        pos = GetMarkPos(player, camPos, timeStacker);

        alpha += ((ShouldShowPips ? 1f : 0f) - alpha) * 0.05f;

        if (alpha <= 0f) return;

        if (ShouldShowPips)
        {
            float pipScale = Manager.MaxPossessionTime / PipSpritesLength;

            for (int m = 0; m < sLeaser.sprites.Length; m++)
            {
                FSprite pip = sLeaser.sprites[m];

                float num22 = pipScale * m;
                float num23 = pipScale * (m + 1);
                pip.scale = Manager.PossessionTime <= num22
                    ? 0f
                    : Manager.PossessionTime >= num23
                        ? 1f
                        : (Manager.PossessionTime - num22) / pipScale;
            }

            UpdateColorLerp(Manager.LowPossessionTime || Manager.TargetSelector.State is TargetSelector.QueryingState);
        }

        float radius = Manager.IsPossessing ? 15f : 6f;
        rubberRadius += (radius - rubberRadius) * 0.045f;

        if (rubberRadius < 5f)
        {
            rubberRadius = radius;
        }

        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            FSprite pip = sLeaser.sprites[i];

            pip.alpha = alpha;
            pip.color = Color.Lerp(PipColor, FlashingPipColor, colorTime);

            pip.SetPosition(pos + Custom.rotateVectorDeg(Vector2.one * rubberRadius, (i - 15) * (360f / PipSpritesLength)));
        }

        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[PipSpritesLength];

        for (int i = 0; i < PipSpritesLength; i++)
        {
            sLeaser.sprites[i] = new FSprite("WormEye");
        }

        base.InitiateSprites(sLeaser, rCam);
    }

    public static Color GetPlayerColor(PlayerGraphics graphics) =>
        IsSofanthielSlugpup(graphics.player)
            ? Color.red
            : PlayerGraphics.SlugcatColor(graphics.CharacterForColor);

    public static bool IsSofanthielSlugpup(Player player) =>
        player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup
        && player.room?.world.game.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel;
}