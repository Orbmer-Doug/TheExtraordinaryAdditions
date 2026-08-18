using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Systems;

namespace TheExtraordinaryAdditions.Core.Graphics.Resources;

public readonly struct DrawAction(
    Action renderAction,
    BlendState blend,
    bool isTexture,
    ManagedShader effect = null,
    string groupId = null)
{
    public Action RenderAction { get; } = renderAction;
    public BlendState Blend { get; } = blend;
    public ManagedShader Shader { get; } = effect;
    public string GroupID { get; } = groupId != null ? string.Intern(groupId) : null;
    public bool IsTexture { get; } = isTexture;
}

public static class DrawActionGrouper
{
    private const string UngroupedSentinel = nameof(DrawActionGrouper);
    private static readonly Dictionary<BlendState, Dictionary<string, List<DrawAction>>> BlendGroupGroups = [];
    private static readonly Dictionary<BlendState, List<DrawAction>> BlendFallback = [];
    private static readonly List<DrawAction>[] GroupListPool = new List<DrawAction>[32];
    private static int _groupListPoolIndex = 0;

    static DrawActionGrouper()
    {
        foreach (BlendState blend in PixelationSystem.SupportedBlendStates)
        {
            BlendGroupGroups[blend] = [];
            BlendFallback[blend] = [];
        }

        for (int i = 0; i < GroupListPool.Length; i++)
            GroupListPool[i] = [];
    }

    private static List<DrawAction> RentGroupList()
    {
        if (_groupListPoolIndex < GroupListPool.Length)
        {
            List<DrawAction> list = GroupListPool[_groupListPoolIndex++];
            list.Clear();
            return list;
        }

        return [];
    }

    private static void ResetGroupListPool()
    {
        _groupListPoolIndex = 0;
    }

    public static void GroupByBlendAndGroupId(ReadOnlySpan<DrawAction> primitiveActions,
        ReadOnlySpan<DrawAction> textureActions,
        Action<BlendState, Dictionary<string, List<DrawAction>>> processBlendGroup)
    {
        ResetGroupListPool();

        // Clear previous frame's data
        foreach (Dictionary<string, List<DrawAction>> blendDict in BlendGroupGroups.Values)
        {
            foreach (List<DrawAction> groupList in blendDict.Values)
                groupList.Clear();
            blendDict.Clear();
        }

        foreach (List<DrawAction> blendList in BlendFallback.Values)
            blendList.Clear();

        // Group primitive actions
        foreach (DrawAction action in primitiveActions)
        {
            BlendFallback[action.Blend].Add(action);
        }

        // Group texture actions
        foreach (DrawAction action in textureActions)
        {
            Dictionary<string, List<DrawAction>> blendDict = BlendGroupGroups[action.Blend];
            if (action.GroupID != null)
            {
                if (!blendDict.TryGetValue(action.GroupID, out List<DrawAction> groupList))
                {
                    groupList = RentGroupList();
                    blendDict[action.GroupID] = groupList;
                }

                groupList.Add(action);
            }
            else
            {
                BlendFallback[action.Blend].Add(action);
            }
        }

        // Process grouped actions
        foreach (KeyValuePair<BlendState, Dictionary<string, List<DrawAction>>> blendEntry in BlendGroupGroups)
        {
            if (blendEntry.Value.Count > 0)
                processBlendGroup(blendEntry.Key, blendEntry.Value);
        }

        // Process grouped and ungrouped actions
        foreach (KeyValuePair<BlendState, Dictionary<string, List<DrawAction>>> blendEntry in BlendGroupGroups)
        {
            Dictionary<string, List<DrawAction>> groupDict = blendEntry.Value;
            List<DrawAction> fallbackList = BlendFallback[blendEntry.Key];
            if (fallbackList.Count > 0)
            {
                groupDict[UngroupedSentinel] = fallbackList;
            }

            if (groupDict.Count > 0)
                processBlendGroup(blendEntry.Key, groupDict);
        }
    }

    public static string UngroupedKey => UngroupedSentinel;
}
