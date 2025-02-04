﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class EffectInfo
{
    private static readonly EffectInfo[] EmptyEffects = new EffectInfo[] { };
    public string name;
    public readonly string id;
    public readonly string description;
    public readonly Type type;
    public EffectInfo[] conflicts => _computedConflicts = _computedConflicts ?? ComputeConflicts().ToArray();
    public EffectInfo[] reloadOnEnable => _reloadEnable = _reloadEnable ?? GetInfos(_reloadOnEnable).ToArray();
    public EffectInfo[] reloadOnDisable => _reloadDisable = _reloadDisable ?? GetInfos(_reloadOnDisable).ToArray();
    public EffectInfo[] children => _children = _children ?? GetChildren().ToArray();
    public bool valid => (bool)(_valid?.Invoke(null, null) ?? true);
    public bool impulse;
    public bool noCheat;
    public bool splitCheats;
    public EffectType effectType;
    public Alignment alignment;
    public bool isGroup;
    public bool isChild;
    public EffectInfo parent;
    public float duration = 5f;
    public EffectInfo(EffectAttribute attribute, Type type)
    {
        effectType = EffectType.Independent;
        alignment = attribute.Alignment;
        _children = EmptyEffects;
        id = attribute.Id;
        this.type = type;

        name = attribute.Name ?? type.FullName;
        description = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
        impulse = type.GetCustomAttribute<ImpulseAttribute>() != null;
        noCheat = type.GetCustomAttribute<HideInCheatGUIAttribute>() != null;

        _valid = type.GetMethod("Valid", BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);

        _conflicts = type.GetCustomAttribute<ConflictsWithAttribute>()?.Conflicts ?? Type.EmptyTypes;
        _reloadOnEnable = type.GetCustomAttribute<Reload.OnEnable>()?.Effects ?? Type.EmptyTypes;
        _reloadOnDisable = type.GetCustomAttribute<Reload.OnDisable>()?.Effects ?? Type.EmptyTypes;
    }

    public EffectInfo(ChildEffectAttribute attribute, Type type)
    {
        effectType = EffectType.Orphan;
        _children = EmptyEffects;
        id = attribute.Id;
        name = attribute.Name ?? type.FullName;
        description = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
        this.type = type;
        impulse = type.GetCustomAttribute<ImpulseAttribute>() != null;
        noCheat = type.GetCustomAttribute<HideInCheatGUIAttribute>() != null;

        _valid = type.GetMethod("Valid", BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);

        _conflicts = type.GetCustomAttribute<ConflictsWithAttribute>()?.Conflicts ?? Type.EmptyTypes;
        _reloadOnEnable = type.GetCustomAttribute<Reload.OnEnable>()?.Effects ?? Type.EmptyTypes;
        _reloadOnDisable = type.GetCustomAttribute<Reload.OnDisable>()?.Effects ?? Type.EmptyTypes;
    }

    public EffectInfo(EffectGroupAttribute attribute, Type type)
    {
        effectType = EffectType.UnknownGroup;
        alignment = attribute.Alignment;
        isGroup = true;
        id = attribute.Id;
        name = attribute.Name ?? type.FullName;
        splitCheats = attribute.SeparateCheats;
        description = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
        this.type = type;

        _conflicts = Type.EmptyTypes;
        _reloadEnable = EmptyEffects;
        _reloadDisable = EmptyEffects;
        _reloadOnEnable = Type.EmptyTypes;
        _reloadOnDisable = Type.EmptyTypes;
        _computedConflicts = EmptyEffects;

        impulse = type.GetCustomAttribute<ImpulseAttribute>() != null;
        noCheat = type.GetCustomAttribute<HideInCheatGUIAttribute>() != null;

        _valid = type.GetMethod("Valid", BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);
    }

    private readonly MethodInfo _valid;
    private readonly Type[] _conflicts;
    private EffectInfo[] _computedConflicts;

    private EffectInfo[] _reloadEnable;
    private EffectInfo[] _reloadDisable;

    private readonly Type[] _reloadOnEnable;
    private readonly Type[] _reloadOnDisable;

    private EffectInfo[] _children;

    public void Setup()
    {
        _computedConflicts = _computedConflicts ?? ComputeConflicts().ToArray();
        _reloadEnable = _reloadEnable ?? GetInfos(_reloadOnEnable).ToArray();
        _reloadDisable = _reloadDisable ?? GetInfos(_reloadOnDisable).ToArray();
        _children = _children ?? GetChildren().ToArray();
    }


    private IEnumerable<EffectInfo> ComputeConflicts()
    {
        var cleared = new HashSet<EffectInfo> { this };
        foreach (var effect in GetInfos(_conflicts))
        {
            if (effect.isGroup) continue;
            cleared.Add(effect);
            yield return effect;
        }
        foreach (var effect in ChaosController.effects)
        {
            if (effect.isGroup) continue;
            if (cleared.Contains(effect)) continue;
            var thisType = type; // argh, can't use this in lambda for checking cross-conflicts
            if (effect._conflicts.Any(type => type.IsAssignableFrom(thisType)))
            {
                cleared.Add(effect);
                yield return effect;
            }
        }
    }

    private IEnumerable<EffectInfo> GetInfos(Type[] types)
    {
        var cleared = new HashSet<EffectInfo> { this };
        foreach (var type in types)
        {
            foreach (var effect in ChaosController.effects)
            {
                if (cleared.Contains(effect)) continue;
                if (type.IsAssignableFrom(effect.type))
                {
                    cleared.Add(effect);
                    yield return effect;
                }
            }
        }
    }

    private IEnumerable<EffectInfo> GetChildren()
    {
        var multi = false;
        var exclusive = false;
        foreach (var effect in ChaosController.effects)
        {
            if (type.IsAssignableFrom(effect.type))
            {
                switch (effect.effectType)
                {
                    case EffectType.Orphan:
                        effectType = EffectType.MultiGroup;
                        multi = true;
                        effect.effectType = EffectType.Child;
                        effect.alignment = alignment;
                        effect.isChild = true;
                        effect.parent = this;
                        yield return effect;
                        break;
                    case EffectType.Independent:
                        effectType = EffectType.ExclusiveGroup;
                        exclusive = true;
                        effect.effectType = EffectType.LonelyChild;
                        effect.isChild = true;
                        effect.parent = this;
                        yield return effect;
                        break;
                }
            }
        }
        if (multi && exclusive) UnityEngine.Debug.Log($"{id} has lonely and regular children!");
    }

    public enum EffectType
    {
        MultiGroup, // group with all effects at once (Child children)
        ExclusiveGroup, // group but only one effect at once (LonelyChild children)
        Child, // child of MultiGroup, active with other Child
        LonelyChild, // child of ExclusiveGroup, not active with other LonelyChild
        Independent, // no parent and no children, might turn into LonelyChild

        UnknownGroup, // group with unknown type
        Orphan, // Child with unknown parent
    }

    [Flags]
    public enum Alignment
    {
        Good = 1, // the player might actively want this effect
        Neutral = 2, // the player might not care if they get this effect
        Bad = 4, // the player might be actively hoping not to get this effect
    }
}
