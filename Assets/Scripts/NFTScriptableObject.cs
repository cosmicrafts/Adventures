using UnityEngine;
using System.Collections.Generic;
using EdjCase.ICP.Candid.Models;

// Type aliases 
using TokenId = EdjCase.ICP.Candid.Models.UnboundedUInt;

/// <summary>
/// ScriptableObject that stores NFT data for use in the Unity Editor and runtime.
/// This can be created dynamically at runtime or manually in the editor.
/// </summary>
[CreateAssetMenu(fileName = "New NFT", menuName = "NFT System/NFT", order = 1)]
public class NFTScriptableObject : ScriptableObject
{
    [Header("Basic Information")]
    public string tokenId;
    public string name;
    public string description;
    public string imageUrl;
    public NFTType nftType;
    
    [Header("Game Properties")]
    public int level = 1;
    public bool isSelected;
    public bool isInDeck;
    
    [Header("Stats")]
    public List<StatEntry> stats = new List<StatEntry>();
    
    [Header("Skills")]
    public List<SkillEntry> skills = new List<SkillEntry>();
    
    [Header("Visual Assets")]
    public Sprite thumbnail;
    public Sprite fullArt;
    public RuntimeAnimatorController animatorController;
    
    /// <summary>
    /// Initialize this ScriptableObject from NFTData
    /// </summary>
    public void InitializeFromNFTData(NFTData data)
    {
        tokenId = data.Id.ToString();
        name = data.Name;
        description = data.Description;
        imageUrl = data.ImageUrl;
        nftType = data.NFTType;
        level = data.Level;
        isSelected = data.IsSelected;
        isInDeck = data.IsInDeck;
        
        // Convert stats dictionary to list
        stats.Clear();
        foreach (var kvp in data.Stats)
        {
            stats.Add(new StatEntry { statName = kvp.Key, statValue = kvp.Value });
        }
        
        // Convert skills
        skills.Clear();
        foreach (var skill in data.Skills)
        {
            skills.Add(new SkillEntry
            {
                name = skill.Name,
                description = skill.Description,
                damage = skill.Damage,
                cooldown = skill.Cooldown
            });
        }
    }
    
    /// <summary>
    /// Convert this ScriptableObject to NFTData
    /// </summary>
    public NFTData ToNFTData()
    {
        // Parse TokenId from string
        TokenId id = null;
        if (ulong.TryParse(tokenId, out ulong tokenIdValue))
        {
            id = (UnboundedUInt)tokenIdValue;
        }
        
        NFTData data = new NFTData
        {
            Id = id,
            Name = name,
            Description = description,
            ImageUrl = imageUrl,
            NFTType = nftType,
            Level = level,
            IsSelected = isSelected,
            IsInDeck = isInDeck
        };
        
        // Convert stats to dictionary
        foreach (var stat in stats)
        {
            data.Stats[stat.statName] = stat.statValue;
        }
        
        // Convert skills
        foreach (var skill in skills)
        {
            data.Skills.Add(new NFTSkill
            {
                Name = skill.name,
                Description = skill.description,
                Damage = skill.damage,
                Cooldown = skill.cooldown
            });
        }
        
        return data;
    }
}

/// <summary>
/// Represents a single stat entry for an NFT
/// </summary>
[System.Serializable]
public class StatEntry
{
    public string statName;
    public string statValue;
}

/// <summary>
/// Represents a single skill for an NFT
/// </summary>
[System.Serializable]
public class SkillEntry
{
    public string name;
    public string description;
    public int damage;
    public int cooldown;
} 