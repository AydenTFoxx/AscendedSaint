using System.Linq;
using ControlLib.Utils.Generics;
using static ControlLib.Utils.OptionUtils;

namespace ControlLib.Possession;

public partial class TargetSelector
{
    public abstract class TargetSelectionState(int order)
    {
        public static TargetSelectionState IdleState => new IdleState();
        public static TargetSelectionState QueryingState => new QueryingState();
        public static TargetSelectionState ReadyState => new ReadyState();

        public readonly int Order = order;

        public abstract void UpdatePhase(TargetSelector selector);
    }

    public class IdleState() : TargetSelectionState(0)
    {
        public override void UpdatePhase(TargetSelector selector)
        {
            if (selector.PossessionManager.IsPossessing)
            {
                selector.PossessionManager.ResetAllPossessions();

                selector.Input.LockAction = true;
                selector.Input.Initialized = true;

                return;
            }

            selector.Player.controller ??= PossessionManager.GetFadeOutController(selector.Player);

            selector.queryCreatures = QueryCreatures(selector.Player);

            selector.MoveToState(QueryingState);
        }
    }

    public class QueryingState() : TargetSelectionState(1)
    {
        public override void UpdatePhase(TargetSelector selector) => UpdatePhase(selector, false);

        public void UpdatePhase(TargetSelector selector, bool isRecursive)
        {
            selector.Player.mushroomCounter = SetMushroomCounter(selector.Player, 10);

            if (!selector.UpdateInputOffset() && !isRecursive) return;

            if (selector.queryCreatures.Count > 0)
            {
                bool forceMultiTarget = IsOptionEnabled(CLOptions.FORCE_MULTITARGET_POSSESSION)
                                        || IsOptionEnabled(CLOptions.WORLDWIDE_MIND_CONTROL);

                if (selector.TrySelectNewTarget(selector.Targets.ElementAtOrDefault(0), out Creature? target))
                {
                    selector.Targets = [target!];

                    if (((forceMultiTarget && !selector.Player.monkAscension) || (!forceMultiTarget && selector.Player.monkAscension))
                        && selector.TrySelectNewTarget(selector.Targets.First().Template, out WeakList<Creature>? targets))
                    {
                        selector.Targets = targets;
                    }
                }
                else
                {
                    CLLogger.LogInfo("Target was invalid, ignoring.");
                }
            }
            else if (!isRecursive)
            {
                CLLogger.LogInfo("Query is empty; Refreshing.");

                selector.queryCreatures = QueryCreatures(selector.Player);

                UpdatePhase(selector, isRecursive: true);
            }
            else
            {
                selector.Input.LockAction = true;

                selector.Player.mushroomCounter = SetMushroomCounter(selector.Player, 20);

                CLLogger.LogWarning("Failed to query for creatures in the room; Aborting operation.");

                selector.MoveToState(IdleState);
            }
        }
    }

    public class ReadyState() : TargetSelectionState(2)
    {
        public override void UpdatePhase(TargetSelector selector)
        {
            selector.queryCreatures.Clear();

            if (selector.Targets is null or { Count: 0 })
            {
                CLLogger.LogWarning("List is null or empty; Aborting operation.");

                selector.MoveToState(IdleState);
                return;
            }

            if (selector.Input.InputTime > selector.PossessionManager.PossessionTimePotential)
            {
                CLLogger.LogInfo("Player took too long, ignoring input.");

                selector.Targets.Clear();

                selector.MoveToState(IdleState);
                return;
            }

            foreach (Creature target in selector.Targets)
            {
                if (selector.PossessionManager.CanPossessCreature(target))
                {
                    selector.PossessionManager.StartPossession(target);
                }
            }

            CLLogger.LogInfo($"Started the possession of {selector.Targets.Count} target(s): {PossessionManager.FormatPossessions(selector.Targets)}");

            selector.Player.monkAscension = false;
            selector.Targets.Clear();

            selector.MoveToState(IdleState);
        }
    }
}