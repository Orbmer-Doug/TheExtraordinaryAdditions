using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.UI;

namespace TheExtraordinaryAdditions.UI;

// Infinitely better than the legacy code gore that was god dummy
public class UIManager : ModSystem
{
    public List<UserInterface> UserInterfaces = [];
    public List<SmartUIState> UIStates = [];
    public override void Load()
    {
        if (Main.dedServ)
            return;

        UserInterfaces = [];
        UIStates = [];

        foreach (Type type in AssemblyManager.GetLoadableTypes(Mod.Code))
        {
            // Dont attempt to load abstact types
            if (!type.IsAbstract && type.IsSubclassOf(typeof(SmartUIState)))
            {
                SmartUIState state = (SmartUIState)Activator.CreateInstance(type, null);
                UserInterface userInterface = new();
                userInterface.SetState(state);
                state.UserInterface = userInterface;

                UIStates?.Add(state);
                UserInterfaces?.Add(userInterface);
            }
        }
    }

    public override void Unload()
    {
        UIStates.ForEach(n => n.Unload());
        UserInterfaces = null;
        UIStates = null;
    }

    public static void AddLayer(List<GameInterfaceLayer> layers, UIState state, int index, bool visible, InterfaceScaleType scaleType)
    {
        layers.Insert(index, new LegacyGameInterfaceLayer("The Extraordinary Additions" + state == null ? "Unknown" : state.ToString(), () =>
        {
            if (visible)
                state.Draw(Main.spriteBatch);

            return true;
        }, scaleType));
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        for (int k = 0; k < UIStates.Count; k++)
        {
            SmartUIState state = UIStates[k];
            AddLayer(layers, state, state.InsertionIndex(layers), state.Visible, state.Scale);
        }
    }

    public override void UpdateUI(GameTime gameTime)
    {
        foreach (UserInterface eachState in UserInterfaces)
        {
            if (eachState?.CurrentState != null && ((SmartUIState)eachState.CurrentState).Visible)
                eachState.Update(gameTime);
        }
    }
}
