using EFT;
using EFT.UI;
using HideoutCat.CatData;

namespace HideoutCat.Utils;

internal static class InteractionStateUtils
{
    internal static AvailableInteractionState GetCatAvailableActions(Cat cat, HideoutPlayerOwner owner)
    {
        return new AvailableInteractionState
        {
            Actions =
            [

                new InteractionAction
                {
                    Name = "Pet",
                    Action = () =>
                    {
                        cat.Pet();
                        owner.Player.SetInteractInHands(EInteraction.ContainerOpenDefault);
                        owner.ClearInteractionState();
                    },
                    Disabled = !cat.IsPettable()
                },
                
                new InteractionAction
                {
                    Name = "Wake up",
                    Action = () =>
                    {
                        cat.WakeUp();
                        owner.ClearInteractionState();
                    },
                    Disabled = !cat.IsSleeping()
                }
            ]
        };
    }
}