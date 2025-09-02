using ControlLib.Utils;
using UnityEngine;

namespace ControlLib.Possession.Graphics;

public class TargetCursor(PossessionManager manager)
    : PlayerAccessory(manager)
{
    public Vector2 targetPos;
    public Vector2 lastTargetPos;

    private float CursorSpeed =>
        CompatibilityManager.IsRainMeadowEnabled()
        && !OptionUtils.IsOptionEnabled(CLOptions.MEADOW_SLOWDOWN)
            ? 7.5f
            : 15f;

    public void ResetCursor(bool isVisible = false)
    {
        targetPos = pos;
        lastTargetPos = targetPos;

        this.isVisible = isVisible;
    }

    public void UpdateCursor(Player.InputPackage input)
    {
        if (player.room is not null && player.room != room)
        {
            TryRealizeInRoom(player.room);
        }

        lastTargetPos = targetPos;

        Vector2 goalPos = targetPos + (new Vector2(input.x, input.y) * CursorSpeed);
        float maxDist = TargetSelector.GetPossessionRange(null) * 4f;

        targetPos = ClampedDist(goalPos, pos, maxDist);
    }

    public override void Update(bool eu)
    {
        base.Update(eu);

        lastTargetPos = targetPos;
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        alpha += ((isVisible ? 1f : 0f) - alpha) * 0.05f;

        if (alpha <= 0f) return;

        UpdateColorLerp(Manager.TargetSelector.ExceededTimeLimit);

        sLeaser.sprites[0].alpha = alpha;
        sLeaser.sprites[0].color = Color.Lerp(Color.white, Color.red, colorTime);

        float smoothTime = timeStacker * timeStacker * (3f + (2f * timeStacker));

        sLeaser.sprites[0].SetPosition(Vector2.Lerp(lastTargetPos, targetPos, smoothTime));

        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];

        sLeaser.sprites[0] = new FSprite("guardEye");

        base.InitiateSprites(sLeaser, rCam);
    }
}