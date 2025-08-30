using System;
using RWCustom;
using UnityEngine;

namespace ControlLib.Possession.Graphics;

public abstract class PlayerAccessory : CosmeticSprite
{
    public PossessionManager Manager { get; }

    public Vector2 camPos;
    public float alpha;
    public bool isVisible;

    protected float colorTime;
    protected bool invertColorLerp;

    protected readonly Player player;

    public PlayerAccessory(PossessionManager manager)
    {
        Manager = manager;
        player = manager.GetPlayer();

        player.room?.AddObject(this);
    }

    public void TryRealizeInRoom(Room playerRoom)
    {
        room?.RemoveObject(this);
        playerRoom.AddObject(this);
    }

    public void UpdateColorLerp(bool applyLerp) =>
        UpdateLerpFunction(applyLerp, ref colorTime, ref invertColorLerp);

    public override void Update(bool eu)
    {
        base.Update(eu);

        if (player.dead)
        {
            isVisible = false;

            if (alpha <= 0f)
            {
                Destroy();
            }
        }
        else if (player.room is not null && player.room != room)
        {
            TryRealizeInRoom(player.room);
        }

        pos = GetMarkPos(player, camPos, 1f);
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        newContatiner ??= rCam.ReturnFContainer("Shortcuts");

        foreach (FSprite sprite in sLeaser.sprites)
        {
            sprite.RemoveFromContainer();
            newContatiner.AddChild(sprite);
        }

        if (sLeaser.containers != null)
        {
            foreach (FContainer node2 in sLeaser.containers)
            {
                newContatiner.AddChild(node2);
            }
        }
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        this.camPos = camPos;

        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        camPos = rCam.pos;

        AddToContainer(sLeaser, rCam, null!);
    }

    protected static float ClampedDist(float targetPos, float refPos, float maxDist) =>
        Mathf.Clamp(targetPos, refPos - maxDist, refPos + maxDist);

    protected static Vector2 ClampedDist(Vector2 targetPos, Vector2 refPos, float maxDist) =>
        new(ClampedDist(targetPos.x, refPos.x, maxDist), ClampedDist(targetPos.y, refPos.y, maxDist));

    protected static Vector2 GetMarkPos(Player player, Vector2 camPos, float timeStacker)
    {
        if (player.graphicsModule is not PlayerGraphics playerGraphics) return default;

        Vector2 vector2 = Vector2.Lerp(playerGraphics.drawPositions[1, 1], playerGraphics.drawPositions[1, 0], timeStacker);
        Vector2 vector3 = Vector2.Lerp(playerGraphics.head.lastPos, playerGraphics.head.pos, timeStacker);

        return vector3 + Custom.DirVec(vector2, vector3) + new Vector2(0f, 30f) - camPos;
    }

    protected static void UpdateLerpFunction(bool applyLerp, ref float lerpTime, ref bool invertLerp)
    {
        if (applyLerp)
        {
            lerpTime += invertLerp ? 0.1f : -0.1f;

            if (Math.Abs(lerpTime) >= 1f)
                invertLerp = !invertLerp;
        }
        else
        {
            lerpTime = Math.Max(0, lerpTime - 0.1f);
        }
    }
}