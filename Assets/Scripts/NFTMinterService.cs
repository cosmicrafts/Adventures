using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using EdjCase.ICP.Candid.Models;
using Cosmicrafts.backend;
using Cosmicrafts.backend.Models;

// Type aliases 
using TokenId = EdjCase.ICP.Candid.Models.UnboundedUInt;
using Account = Cosmicrafts.backend.Models.Account;
using MintReceiptTag = Cosmicrafts.backend.Models.MintReceiptTag;
using TokenMetadata = Cosmicrafts.backend.Models.TokenMetadata;
using BasicMetadata = Cosmicrafts.backend.Models.BasicMetadata;
using SkillMetadata = Cosmicrafts.backend.Models.SkillMetadata;
using SkinMetadata = Cosmicrafts.backend.Models.SkinMetadata;
using SoulMetadata = Cosmicrafts.backend.Models.SoulMetadata;
using GeneralMetadata = Cosmicrafts.backend.Models.GeneralMetadata;

/// <summary>
/// Service for minting different types of NFTs in the game
/// </summary>
public class NFTMinterService : MonoBehaviour
{
    // Singleton instance
    public static NFTMinterService Instance { get; private set; }
    
    // Events
    public event Action<TokenId> OnNFTMinted;
    public event Action<string> OnMintError;
    public event Action<List<TokenId>> OnDeckMinted;
    public event Action<TokenId> OnChestMinted;
    
    // Dependencies
    private ICPService icpService;
    private BackendApiClient backendApiClient => icpService?.MainCanister;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Log("Initialized");
    }
    
    private void Start()
    {
        // Get reference to ICPService
        icpService = ICPService.Instance;
        
        if (icpService == null)
        {
            LogError("ICPService not found. NFTMinterService requires ICPService.");
        }
    }
    
    /// <summary>
    /// Mint a generic NFT with custom metadata
    /// </summary>
    public async Task<TokenId> MintNFT(Category category, GeneralMetadata generalMetadata, 
        OptionalValue<BasicMetadata> basicMetadata = default, 
        OptionalValue<SkillMetadata> skillMetadata = default, 
        OptionalValue<SkinMetadata> skinMetadata = default, 
        OptionalValue<SoulMetadata> soulMetadata = default)
    {
        if (!IsInitialized())
        {
            LogError("Cannot mint NFT: service not initialized");
            return null;
        }
        
        try
        {
            // Create metadata
            var metadata = new Metadata(
                basic: basicMetadata,
                category: category,
                general: generalMetadata,
                skills: skillMetadata,
                skins: skinMetadata,
                soul: soulMetadata
            );
            
            // Create account for the current user
            var account = new Account(Principal.FromText(icpService.PrincipalId), default);
            
            // Generate a unique token ID based on timestamp
            var tokenId = (UnboundedUInt)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // Create mint arguments
            var mintArgs = new MintArgs(
                metadata: metadata,
                to: account,
                tokenId: tokenId
            );
            
            Log($"Minting NFT with category: {category.Tag}");
            
            // Call the mintNFT function
            var receipt = await backendApiClient.MintNFT(mintArgs);
            
            if (receipt.Tag == MintReceiptTag.Ok)
            {
                var mintedTokenId = receipt.AsOk();
                Log($"Successfully minted NFT with token ID: {mintedTokenId}");
                OnNFTMinted?.Invoke(mintedTokenId);
                return mintedTokenId;
            }
            else
            {
                var error = receipt.AsErr();
                LogError($"Failed to mint NFT: {error.Tag}");
                OnMintError?.Invoke($"Mint error: {error.Tag}");
                return null;
            }
        }
        catch (Exception e)
        {
            LogError($"Error during NFT minting: {e.Message}");
            OnMintError?.Invoke($"Exception: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Mint an avatar NFT
    /// </summary>
    public async Task<TokenId> MintAvatar(string name, string description, string imageUrl)
    {
        // Generate a unique token ID based on timestamp
        var id = (UnboundedUInt)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        // Create general metadata
        var generalMetadata = new GeneralMetadata(
            description: description,
            faction: default,
            id: id,
            image: imageUrl,
            name: name,
            rarity: default
        );
        
        // Create avatar category
        var category = Category.Avatar();
        
        return await MintNFT(category, generalMetadata);
    }
    
    /// <summary>
    /// Mint a unit NFT
    /// </summary>
    public async Task<TokenId> MintUnit(string name, string description, string imageUrl, Unit unitInfo)
    {
        // Generate a unique token ID based on timestamp
        var id = (UnboundedUInt)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        // Create general metadata
        var generalMetadata = new GeneralMetadata(
            description: description,
            faction: default,
            id: id,
            image: imageUrl,
            name: name,
            rarity: default
        );
        
        // Create unit category with unit info
        var category = Category.Unit(unitInfo);
        
        return await MintNFT(category, generalMetadata);
    }
    
    /// <summary>
    /// Mint a chest NFT
    /// </summary>
    public async Task<TokenId> MintChest(string name, string description, string imageUrl)
    {
        // Generate a unique token ID based on timestamp
        var id = (UnboundedUInt)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        // Create general metadata
        var generalMetadata = new GeneralMetadata(
            description: description,
            faction: default,
            id: id,
            image: imageUrl,
            name: name,
            rarity: default
        );
        
        // Create chest category
        var category = Category.Chest();
        
        return await MintNFT(category, generalMetadata);
    }
    
    /// <summary>
    /// Mint a trophy NFT
    /// </summary>
    public async Task<TokenId> MintTrophy(string name, string description, string imageUrl)
    {
        // Generate a unique token ID based on timestamp
        var id = (UnboundedUInt)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        // Create general metadata
        var generalMetadata = new GeneralMetadata(
            description: description,
            faction: default,
            id: id,
            image: imageUrl,
            name: name,
            rarity: default
        );
        
        // Create trophy category
        var category = Category.Trophy();
        
        return await MintNFT(category, generalMetadata);
    }
    
    /// <summary>
    /// Mint a deck of NFTs (this uses a specialized endpoint)
    /// </summary>
    public async Task<List<TokenId>> MintDeck()
    {
        if (!IsInitialized())
        {
            LogError("Cannot mint deck: service not initialized");
            return null;
        }
        
        try
        {
            Log("Minting deck of NFTs");
            
            // Call the mintDeck function
            var result = await backendApiClient.MintDeck();
            
            if (result.ReturnArg0)
            {
                Log($"Successfully minted deck with {result.ReturnArg2.Count} NFTs");
                OnDeckMinted?.Invoke(result.ReturnArg2);
                return result.ReturnArg2;
            }
            else
            {
                LogError($"Failed to mint deck: {result.ReturnArg1}");
                OnMintError?.Invoke(result.ReturnArg1);
                return null;
            }
        }
        catch (Exception e)
        {
            LogError($"Error during deck minting: {e.Message}");
            OnMintError?.Invoke($"Exception: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Mint a chest for a specific user (requires admin permission)
    /// </summary>
    public async Task<bool> MintChestForUser(string principalId, int quantity = 1)
    {
        if (!IsInitialized())
        {
            LogError("Cannot mint chest: service not initialized");
            return false;
        }
        
        try
        {
            Log($"Minting {quantity} chest(s) for user {principalId}");
            
            // Create Principal from string
            var principal = Principal.FromText(principalId);
            
            // Call the mintChest function
            var result = await backendApiClient.MintChest(principal, (UnboundedUInt)quantity);
            
            if (result.ReturnArg0)
            {
                Log($"Successfully minted chest(s) for user {principalId}");
                return true;
            }
            else
            {
                LogError($"Failed to mint chest: {result.ReturnArg1}");
                OnMintError?.Invoke(result.ReturnArg1);
                return false;
            }
        }
        catch (Exception e)
        {
            LogError($"Error during chest minting: {e.Message}");
            OnMintError?.Invoke($"Exception: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Open a chest to receive its contents
    /// </summary>
    public async Task<bool> OpenChest(TokenId chestTokenId)
    {
        if (!IsInitialized())
        {
            LogError("Cannot open chest: service not initialized");
            return false;
        }
        
        try
        {
            Log($"Opening chest with token ID: {chestTokenId}");
            
            // Call the openChest function
            var result = await backendApiClient.OpenChest(chestTokenId);
            
            if (result.ReturnArg0)
            {
                Log($"Successfully opened chest: {result.ReturnArg1}");
                return true;
            }
            else
            {
                LogError($"Failed to open chest: {result.ReturnArg1}");
                OnMintError?.Invoke(result.ReturnArg1);
                return false;
            }
        }
        catch (Exception e)
        {
            LogError($"Error during chest opening: {e.Message}");
            OnMintError?.Invoke($"Exception: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get all NFTs owned by a user
    /// </summary>
    public async Task<List<BackendApiClient.GetNFTsReturnArg0.GetNFTsReturnArg0Element>> GetUserNFTs(string principalId = null)
    {
        if (!IsInitialized())
        {
            LogError("Cannot get NFTs: service not initialized");
            return null;
        }
        
        try
        {
            // Use current user's principal if none provided
            var principal = string.IsNullOrEmpty(principalId) 
                ? Principal.FromText(icpService.PrincipalId) 
                : Principal.FromText(principalId);
            
            Log($"Fetching NFTs for user {principal.ToText()}");
            
            // Call the getNFTs function
            var nfts = await backendApiClient.GetNFTs(principal);
            
            Log($"Found {nfts.Count} NFTs");
            return nfts;
        }
        catch (Exception e)
        {
            LogError($"Error fetching NFTs: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Get all avatars owned by a user
    /// </summary>
    public async Task<List<BackendApiClient.GetAvatarsReturnArg0.GetAvatarsReturnArg0Element>> GetUserAvatars(string principalId = null)
    {
        if (!IsInitialized())
        {
            LogError("Cannot get avatars: service not initialized");
            return null;
        }
        
        try
        {
            // Use current user's principal if none provided
            var principal = string.IsNullOrEmpty(principalId) 
                ? Principal.FromText(icpService.PrincipalId) 
                : Principal.FromText(principalId);
            
            Log($"Fetching avatars for user {principal.ToText()}");
            
            // Call the getAvatars function
            var avatars = await backendApiClient.GetAvatars(principal);
            
            Log($"Found {avatars.Count} avatars");
            return avatars;
        }
        catch (Exception e)
        {
            LogError($"Error fetching avatars: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Get all units owned by a user
    /// </summary>
    public async Task<List<BackendApiClient.GetUnitsReturnArg0.GetUnitsReturnArg0Element>> GetUserUnits(string principalId = null)
    {
        if (!IsInitialized())
        {
            LogError("Cannot get units: service not initialized");
            return null;
        }
        
        try
        {
            // Use current user's principal if none provided
            var principal = string.IsNullOrEmpty(principalId) 
                ? Principal.FromText(icpService.PrincipalId) 
                : Principal.FromText(principalId);
            
            Log($"Fetching units for user {principal.ToText()}");
            
            // Call the getUnits function
            var units = await backendApiClient.GetUnits(principal);
            
            Log($"Found {units.Count} units");
            return units;
        }
        catch (Exception e)
        {
            LogError($"Error fetching units: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Get all chests owned by a user
    /// </summary>
    public async Task<List<BackendApiClient.GetChestsReturnArg0.GetChestsReturnArg0Element>> GetUserChests(string principalId = null)
    {
        if (!IsInitialized())
        {
            LogError("Cannot get chests: service not initialized");
            return null;
        }
        
        try
        {
            // Use current user's principal if none provided
            var principal = string.IsNullOrEmpty(principalId) 
                ? Principal.FromText(icpService.PrincipalId) 
                : Principal.FromText(principalId);
            
            Log($"Fetching chests for user {principal.ToText()}");
            
            // Call the getChests function
            var chests = await backendApiClient.GetChests(principal);
            
            Log($"Found {chests.Count} chests");
            return chests;
        }
        catch (Exception e)
        {
            LogError($"Error fetching chests: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Get info about minted assets for a user
    /// </summary>
    public async Task<(int Chests, int GameNFTs, long Stardust)> GetMintedInfo(string principalId = null)
    {
        if (!IsInitialized())
        {
            LogError("Cannot get minted info: service not initialized");
            return (0, 0, 0);
        }
        
        try
        {
            // Use current user's principal if none provided
            var principal = string.IsNullOrEmpty(principalId) 
                ? Principal.FromText(icpService.PrincipalId) 
                : Principal.FromText(principalId);
            
            Log($"Fetching minted info for user {principal.ToText()}");
            
            // Call the getMintedInfo function
            var mintedInfo = await backendApiClient.GetMintedInfo(principal);
            
            var chestsCount = (int)mintedInfo.Chests.Quantity.ToUInt64();
            var gameNFTsCount = (int)mintedInfo.GameNFTs.Quantity.ToUInt64();
            var stardust = (long)mintedInfo.Stardust.ToUInt64();
            
            Log($"Minted info: {chestsCount} chests, {gameNFTsCount} game NFTs, {stardust} stardust");
            return (chestsCount, gameNFTsCount, stardust);
        }
        catch (Exception e)
        {
            LogError($"Error fetching minted info: {e.Message}");
            return (0, 0, 0);
        }
    }
    
    // Check if the service is properly initialized
    private bool IsInitialized()
    {
        return icpService != null && icpService.IsInitialized && backendApiClient != null;
    }
    
    // Logging helpers
    private void Log(string message) => Debug.Log($"[NFTMinterService] {message}");
    private void LogWarning(string message) => Debug.LogWarning($"[NFTMinterService] {message}");
    private void LogError(string message) => Debug.LogError($"[NFTMinterService] {message}");

    public class SimplifiedNFTData
    {
        public string TokenId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string ImageUrl { get; set; }
        public Dictionary<string, string> Stats { get; set; } = new Dictionary<string, string>();
    }

    private SimplifiedNFTData ParseNFT(BackendApiClient.GetNFTsReturnArg0.GetNFTsReturnArg0Element nft)
    {
        var data = new SimplifiedNFTData { TokenId = nft.F0.ToString() };
        
        // Parse general metadata
        var general = nft.F1.Metadata.General;
        data.Name = general.Name;
        data.Description = general.Description;
        data.ImageUrl = general.Image;
        
        // Parse type from category
        data.Type = nft.F1.Metadata.Category.Tag.ToString();
        
        // Parse basic stats if available
        if (nft.F1.Metadata.Basic.HasValue)
        {
            var basic = nft.F1.Metadata.Basic.ValueOrDefault;
            data.Stats["Level"] = basic.Level.ToString();
            data.Stats["Health"] = basic.Health.ToString();
            data.Stats["Damage"] = basic.Damage.ToString();
        }
        
        return data;
    }

    private void DisplayNFTs<T>(List<T> nfts) where T : class
    {
        var parsedNFTs = new List<SimplifiedNFTData>();
        foreach (var nft in nfts)
        {
            parsedNFTs.Add(ParseNFT(nft as BackendApiClient.GetNFTsReturnArg0.GetNFTsReturnArg0Element));
        }
        
        // Then display the parsed NFTs...
    }
} 