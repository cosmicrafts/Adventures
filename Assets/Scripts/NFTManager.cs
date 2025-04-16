using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using EdjCase.ICP.Candid.Models;
using Cosmicrafts.backend;
using Cosmicrafts.backend.Models;
using System.Linq;

// Type aliases 
using TokenId = EdjCase.ICP.Candid.Models.UnboundedUInt;

/// <summary>
/// Central manager for all NFT operations in the game.
/// Handles minting, fetching, displaying, and managing NFTs.
/// </summary>
public class NFTManager : MonoBehaviour
{
    #region Singleton
    private static NFTManager _instance;
    public static NFTManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NFTManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("NFTManager");
                    _instance = obj.AddComponent<NFTManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Services
    private ICPService _icpService;
    private BackendApiClient _backendApiClient => _icpService?.MainCanister;
    #endregion

    #region Collections
    // Collections of NFT data
    public Dictionary<TokenId, NFTData> AllNFTs { get; private set; } = new Dictionary<TokenId, NFTData>();
    public List<NFTData> Characters { get; private set; } = new List<NFTData>();
    public List<NFTData> Units { get; private set; } = new List<NFTData>();
    public List<NFTData> Avatars { get; private set; } = new List<NFTData>();
    public List<NFTData> Chests { get; private set; } = new List<NFTData>();
    public List<NFTData> Trophies { get; private set; } = new List<NFTData>();
    
    // Selected NFTs
    public NFTData SelectedAvatar { get; private set; }
    
    // Player's deck
    public List<NFTData> CurrentDeck { get; private set; } = new List<NFTData>();
    #endregion

    #region Events
    // General events
    public event Action OnInitialized;
    public event Action OnNFTsLoaded;
    public event Action<NFTData> OnNFTAdded;
    public event Action<NFTData> OnNFTUpdated;
    public event Action<TokenId> OnNFTRemoved;
    
    // Type-specific events
    public event Action<List<NFTData>> OnCharactersLoaded;
    public event Action<List<NFTData>> OnUnitsLoaded;
    public event Action<List<NFTData>> OnAvatarsLoaded;
    public event Action<List<NFTData>> OnChestsLoaded;
    public event Action<List<NFTData>> OnTrophiesLoaded;
    
    // Deck events
    public event Action<List<NFTData>> OnDeckUpdated;
    
    // Selection events
    public event Action<NFTData> OnAvatarSelected;
    
    // Minting events
    public event Action<TokenId> OnNFTMinted;
    public event Action<string> OnMintError;
    public event Action<List<TokenId>> OnDeckMinted;
    public event Action<TokenId> OnChestMinted;
    
    // Chest events
    public event Action<TokenId, List<NFTData>> OnChestOpened;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Log("NFTManager initialized");
    }
    
    private void Start()
    {
        // Get reference to ICPService
        _icpService = ICPService.Instance;
        
        if (_icpService == null)
        {
            LogError("ICPService not found. NFTManager requires ICPService.");
            return;
        }
        
        // Wait for ICPService to initialize
        if (!_icpService.IsInitialized)
        {
            _icpService.OnICPInitialized += OnICPServiceInitialized;
        }
        else
        {
            OnICPServiceInitialized();
        }
    }

    private void OnDestroy()
    {
        if (_icpService != null)
        {
            _icpService.OnICPInitialized -= OnICPServiceInitialized;
        }
    }
    #endregion

    #region Initialization
    private void OnICPServiceInitialized()
    {
        Log("ICPService initialized, loading NFT data...");
        LoadAllNFTs();
        OnInitialized?.Invoke();
    }
    
    /// <summary>
    /// Load all NFT data from the blockchain
    /// </summary>
    public async void LoadAllNFTs()
    {
        if (!IsInitialized())
        {
            LogError("Cannot load NFTs: service not initialized");
            return;
        }
        
        Log("Loading all NFTs from blockchain...");
        
        try
        {
            // Clear existing collections
            AllNFTs.Clear();
            Characters.Clear();
            Units.Clear();
            Avatars.Clear();
            Chests.Clear();
            Trophies.Clear();
            
            // Get player's principal ID
            var principalId = _icpService.PrincipalId;
            if (string.IsNullOrEmpty(principalId))
            {
                LogError("Cannot load NFTs: Player ID not available");
                return;
            }
            
            Principal principal = Principal.FromText(principalId);
            
            // Load all NFTs
            var nfts = await _backendApiClient.GetNFTs(principal);
            foreach (var nft in nfts)
            {
                var nftData = CreateNFTData(nft.F0, nft.F1);
                AddNFTToCollections(nftData);
            }
            
            // Load player's current deck
            await LoadDeck(principal);
            
            // Load selected avatar
            await LoadSelectedAvatar();
            
            Log($"Loaded {AllNFTs.Count} NFTs ({Characters.Count} characters, {Units.Count} units, {Avatars.Count} avatars, {Chests.Count} chests, {Trophies.Count} trophies)");
            
            // Fire events
            OnNFTsLoaded?.Invoke();
            OnCharactersLoaded?.Invoke(Characters);
            OnUnitsLoaded?.Invoke(Units);
            OnAvatarsLoaded?.Invoke(Avatars);
            OnChestsLoaded?.Invoke(Chests);
            OnTrophiesLoaded?.Invoke(Trophies);
        }
        catch (Exception e)
        {
            LogError($"Error loading NFTs: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load player's current deck
    /// </summary>
    private async Task LoadDeck(Principal principal)
    {
        try
        {
            CurrentDeck.Clear();
            
            var deckResult = await _backendApiClient.GetPlayerDeck(principal);
            if (deckResult.HasValue)
            {
                var deckIds = deckResult.ValueOrDefault;
                foreach (var id in deckIds)
                {
                    if (AllNFTs.TryGetValue(id, out NFTData nft))
                    {
                        nft.IsInDeck = true;
                        CurrentDeck.Add(nft);
                    }
                }
                
                Log($"Loaded deck with {CurrentDeck.Count} NFTs");
                OnDeckUpdated?.Invoke(CurrentDeck);
            }
        }
        catch (Exception e)
        {
            LogError($"Error loading deck: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load selected avatar
    /// </summary>
    private async Task LoadSelectedAvatar()
    {
        try
        {
            var selectedAvatarResult = await _backendApiClient.GetSelectedAvatar();
            if (selectedAvatarResult.HasValue)
            {
                var avatarId = selectedAvatarResult.ValueOrDefault;
                if (AllNFTs.TryGetValue(avatarId, out NFTData avatar))
                {
                    SelectedAvatar = avatar;
                    avatar.IsSelected = true;
                    Log($"Selected avatar: {avatar.Name}");
                    OnAvatarSelected?.Invoke(avatar);
                }
            }
        }
        catch (Exception e)
        {
            LogError($"Error loading selected avatar: {e.Message}");
        }
    }
    #endregion

    #region NFT Creation & Management
    /// <summary>
    /// Create an NFTData object from the blockchain data
    /// </summary>
    private NFTData CreateNFTData(TokenId id, TokenMetadata metadata)
    {
        NFTData nft = new NFTData
        {
            Id = id,
            Metadata = metadata,
            NFTType = DetermineNFTType(metadata),
            Name = metadata.Metadata.General.Name,
            Description = metadata.Metadata.General.Description,
            ImageUrl = metadata.Metadata.General.Image,
            DateAdded = DateTime.Now
        };
        
        // Parse basic stats if available
        if (metadata.Metadata.Basic.HasValue && metadata.Metadata.Basic.ValueOrDefault != null)
        {
            var basic = metadata.Metadata.Basic.ValueOrDefault;
            nft.Level = (int)basic.Level;
            
            // Add stats
            nft.Stats["Health"] = basic.Health.ToString();
            nft.Stats["Damage"] = basic.Damage.ToString();
            // Remove or comment out properties that don't exist in BasicMetadata
            // nft.Stats["Defense"] = basic.Defense.ToString();
            // nft.Stats["Speed"] = basic.Speed.ToString();
        }
        
        // Parse skills if available
        if (metadata.Metadata.Skills.HasValue && metadata.Metadata.Skills.ValueOrDefault != null)
        {
            var skills = metadata.Metadata.Skills.ValueOrDefault;
            // Add skills (implementation depends on your backend models)
        }
        
        return nft;
    }
    
    /// <summary>
    /// Determine the NFT type from metadata
    /// </summary>
    private NFTType DetermineNFTType(TokenMetadata metadata)
    {
        var category = metadata.Metadata.Category;
        
        switch (category.Tag)
        {
            case CategoryTag.Avatar:
                return NFTType.Avatar;
            case CategoryTag.Unit:
                // For Unit types, we can check if it's a character based on the Unit enum value
                if (category.Tag == CategoryTag.Unit && category.Value != null)
                {
                    Unit unitType = category.AsUnit();
                    if (unitType == Unit.Character)
                    {
                        return NFTType.Character;
                    }
                }
                return NFTType.Unit;
            case CategoryTag.Chest:
                return NFTType.Chest;
            case CategoryTag.Trophy:
                return NFTType.Trophy;
            default:
                return NFTType.Unknown;
        }
    }
    
    /// <summary>
    /// Add an NFT to the appropriate collections
    /// </summary>
    private void AddNFTToCollections(NFTData nft)
    {
        // Add to main collection
        AllNFTs[nft.Id] = nft;
        
        // Add to type-specific collection
        switch (nft.NFTType)
        {
            case NFTType.Character:
                Characters.Add(nft);
                break;
            case NFTType.Unit:
                Units.Add(nft);
                break;
            case NFTType.Avatar:
                Avatars.Add(nft);
                break;
            case NFTType.Chest:
                Chests.Add(nft);
                break;
            case NFTType.Trophy:
                Trophies.Add(nft);
                break;
        }
        
        // Fire event
        OnNFTAdded?.Invoke(nft);
    }
    
    /// <summary>
    /// Get an NFT by ID
    /// </summary>
    public NFTData GetNFT(TokenId id)
    {
        if (AllNFTs.TryGetValue(id, out NFTData nft))
        {
            return nft;
        }
        return null;
    }
    
    /// <summary>
    /// Create a ScriptableObject for the given NFT
    /// </summary>
    public NFTScriptableObject CreateScriptableObject(NFTData nft)
    {
        NFTScriptableObject so = ScriptableObject.CreateInstance<NFTScriptableObject>();
        so.InitializeFromNFTData(nft);
        return so;
    }
    #endregion

    #region Minting Operations
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
            // Create metadata object for the NFT
            var metadataObj = new Metadata(
                basic: basicMetadata,
                category: category,
                general: generalMetadata,
                skills: skillMetadata,
                skins: skinMetadata,
                soul: soulMetadata
            );
            
            // Create account for the current user
            var userAccount = new Account(Principal.FromText(_icpService.PrincipalId), default);
            
            // Generate a unique token ID based on timestamp
            var tokenId = (UnboundedUInt)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // Create mint arguments
            var mintArgs = new MintArgs(
                metadata: metadataObj,
                to: userAccount,
                tokenId: tokenId
            );
            
            Log($"Minting NFT with category: {category.Tag}");
            
            // Call the mintNFT function
            var receipt = await _backendApiClient.MintNFT(mintArgs);
            
            if (receipt.Tag == MintReceiptTag.Ok)
            {
                var mintedTokenId = receipt.AsOk();
                Log($"Successfully minted NFT with token ID: {mintedTokenId}");
                
                // Create NFT data and add to collections
                var metadata = new Metadata(
                    basic: basicMetadata,
                    category: category,
                    general: generalMetadata,
                    skills: skillMetadata,
                    skins: skinMetadata,
                    soul: soulMetadata
                );
                
                // Create Account for owner
                var account = new Account(Principal.FromText(_icpService.PrincipalId), default);
                
                TokenMetadata tokenMetadata = new TokenMetadata(
                    metadata: metadata,
                    owner: account,
                    tokenId: mintedTokenId
                );
                
                NFTData nftData = CreateNFTData(mintedTokenId, tokenMetadata);
                AddNFTToCollections(nftData);
                
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
    public async Task<NFTData> MintAvatar(string name, string description, string imageUrl)
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
        
        var tokenId = await MintNFT(category, generalMetadata);
        if (tokenId != null && AllNFTs.TryGetValue(tokenId, out NFTData nftData))
        {
            return nftData;
        }
        
        return null;
    }
    
    /// <summary>
    /// Mint a unit NFT
    /// </summary>
    public async Task<NFTData> MintUnit(string name, string description, string imageUrl, Unit unitInfo)
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
        
        var tokenId = await MintNFT(category, generalMetadata);
        if (tokenId != null && AllNFTs.TryGetValue(tokenId, out NFTData nftData))
        {
            return nftData;
        }
        
        return null;
    }
    
    /// <summary>
    /// Mint a chest NFT
    /// </summary>
    public async Task<NFTData> MintChest(string name, string description, string imageUrl)
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
        
        var tokenId = await MintNFT(category, generalMetadata);
        if (tokenId != null)
        {
            OnChestMinted?.Invoke(tokenId);
            
            if (AllNFTs.TryGetValue(tokenId, out NFTData nftData))
            {
                return nftData;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Mint a trophy NFT
    /// </summary>
    public async Task<NFTData> MintTrophy(string name, string description, string imageUrl)
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
        
        var tokenId = await MintNFT(category, generalMetadata);
        if (tokenId != null && AllNFTs.TryGetValue(tokenId, out NFTData nftData))
        {
            return nftData;
        }
        
        return null;
    }
    
    /// <summary>
    /// Mint a deck of NFTs (this uses a specialized endpoint)
    /// </summary>
    public async Task<List<NFTData>> MintDeck()
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
            var result = await _backendApiClient.MintDeck();
            
            if (result.ReturnArg0)
            {
                var tokenIds = result.ReturnArg2;
                Log($"Successfully minted deck with {tokenIds.Count} NFTs");
                
                // After minting, refresh NFTs to get the new ones
                await RefreshNFTs();
                
                OnDeckMinted?.Invoke(tokenIds);
                
                // Return NFT data for all minted tokens
                var mintedNFTs = new List<NFTData>();
                foreach (var tokenId in tokenIds)
                {
                    if (AllNFTs.TryGetValue(tokenId, out NFTData nft))
                    {
                        mintedNFTs.Add(nft);
                    }
                }
                
                return mintedNFTs;
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
            var result = await _backendApiClient.MintChest(principal, (UnboundedUInt)quantity);
            
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
    #endregion

    #region NFT Operations
    /// <summary>
    /// Open a chest to receive its contents
    /// </summary>
    public async Task<List<NFTData>> OpenChest(TokenId chestTokenId)
    {
        if (!IsInitialized())
        {
            LogError("Cannot open chest: service not initialized");
            return null;
        }
        
        try
        {
            Log($"Opening chest with token ID: {chestTokenId}");
            
            // Call the openChest function
            var result = await _backendApiClient.OpenChest(chestTokenId);
            
            if (result.ReturnArg0)
            {
                Log($"Successfully opened chest: {result.ReturnArg1}");
                
                // Remove the chest from collections
                if (AllNFTs.TryGetValue(chestTokenId, out NFTData chestData))
                {
                    AllNFTs.Remove(chestTokenId);
                    Chests.Remove(chestData);
                    OnNFTRemoved?.Invoke(chestTokenId);
                }
                
                // Refresh NFT data to get the new items
                await RefreshNFTs();
                
                // Find newly added NFTs (those that weren't in the collection before)
                // This is just a guess since we don't know exactly what was in the chest
                List<NFTData> newNFTs = new List<NFTData>();
                foreach (var nft in AllNFTs.Values)
                {
                    if (nft.Id != chestTokenId && nft.DateAdded > DateTime.Now.AddMinutes(-1))
                    {
                        newNFTs.Add(nft);
                    }
                }
                
                // Fire event
                OnChestOpened?.Invoke(chestTokenId, newNFTs);
                
                return newNFTs;
            }
            else
            {
                LogError($"Failed to open chest: {result.ReturnArg1}");
                OnMintError?.Invoke(result.ReturnArg1);
                return null;
            }
        }
        catch (Exception e)
        {
            LogError($"Error during chest opening: {e.Message}");
            OnMintError?.Invoke($"Exception: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Update the selected avatar
    /// </summary>
    public async Task<bool> SelectAvatar(TokenId avatarId)
    {
        if (!IsInitialized())
        {
            LogError("Cannot select avatar: service not initialized");
            return false;
        }
        
        try
        {
            Log($"Selecting avatar with token ID: {avatarId}");
            
            // Call the updateAvatar function
            var result = await _backendApiClient.UpdateAvatar(avatarId);
            
            if (result.ReturnArg0)
            {
                Log($"Successfully selected avatar: {avatarId}");
                
                // Update local state
                if (SelectedAvatar != null)
                {
                    SelectedAvatar.IsSelected = false;
                }
                
                if (AllNFTs.TryGetValue(avatarId, out NFTData avatar))
                {
                    avatar.IsSelected = true;
                    SelectedAvatar = avatar;
                    OnAvatarSelected?.Invoke(avatar);
                }
                
                return true;
            }
            else
            {
                LogError($"Failed to select avatar: {result.ReturnArg1}");
                return false;
            }
        }
        catch (Exception e)
        {
            LogError($"Error selecting avatar: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Update the player's deck
    /// </summary>
    public async Task<bool> UpdateDeck(List<NFTData> deck)
    {
        if (!IsInitialized())
        {
            LogError("Cannot update deck: service not initialized");
            return false;
        }
        
        try
        {
            // Convert NFTData to TokenIds
            var tokenIds = deck.Select(nft => nft.Id).ToList();
            
            // Create the appropriate argument type
            var deckArg = new BackendApiClient.StoreCurrentDeckArg0();
            deckArg.AddRange(tokenIds);
            
            Log($"Updating deck with {tokenIds.Count} NFTs");
            
            // Call the storeCurrentDeck function
            var result = await _backendApiClient.StoreCurrentDeck(deckArg);
            
            if (result)
            {
                Log($"Successfully updated deck");
                
                // Update local state
                foreach (var nft in AllNFTs.Values)
                {
                    nft.IsInDeck = false;
                }
                
                CurrentDeck.Clear();
                foreach (var nft in deck)
                {
                    nft.IsInDeck = true;
                    CurrentDeck.Add(nft);
                }
                
                OnDeckUpdated?.Invoke(CurrentDeck);
                return true;
            }
            else
            {
                LogError($"Failed to update deck");
                return false;
            }
        }
        catch (Exception e)
        {
            LogError($"Error updating deck: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Upgrade an NFT
    /// </summary>
    public async Task<bool> UpgradeNFT(TokenId nftId)
    {
        if (!IsInitialized())
        {
            LogError("Cannot upgrade NFT: service not initialized");
            return false;
        }
        
        try
        {
            Log($"Upgrading NFT with token ID: {nftId}");
            
            // Call the upgradeNFT function
            var result = await _backendApiClient.UpgradeNFT(nftId);
            
            if (result.ReturnArg0)
            {
                Log($"Successfully upgraded NFT: {result.ReturnArg1}");
                
                // Update local state
                if (AllNFTs.TryGetValue(nftId, out NFTData nft))
                {
                    nft.Level++;
                    if (nft.Stats.ContainsKey("Level"))
                    {
                        nft.Stats["Level"] = nft.Level.ToString();
                    }
                    
                    OnNFTUpdated?.Invoke(nft);
                }
                
                return true;
            }
            else
            {
                LogError($"Failed to upgrade NFT: {result.ReturnArg1}");
                return false;
            }
        }
        catch (Exception e)
        {
            LogError($"Error upgrading NFT: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Refresh all NFT data from the blockchain
    /// </summary>
    public async Task RefreshNFTs()
    {
        // Track the date of existing NFTs to identify new ones later
        foreach (var nft in AllNFTs.Values)
        {
            nft.DateAdded = DateTime.MinValue;
        }
        
        // Reload all NFTs
        await Task.Run(() => LoadAllNFTs());
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Check if the service is properly initialized
    /// </summary>
    public bool IsInitialized()
    {
        return _icpService != null && _icpService.IsInitialized && _backendApiClient != null;
    }
    
    // Logging helpers
    private void Log(string message) => Debug.Log($"[NFTManager] {message}");
    private void LogWarning(string message) => Debug.LogWarning($"[NFTManager] {message}");
    private void LogError(string message) => Debug.LogError($"[NFTManager] {message}");
    #endregion
}

#region Data Structures
/// <summary>
/// Data structure to store NFT information
/// </summary>
[System.Serializable]
public class NFTData
{
    // Core properties
    public TokenId Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public TokenMetadata Metadata { get; set; }
    public NFTType NFTType { get; set; }
    
    // Game properties
    public int Level { get; set; } = 1;
    public bool IsSelected { get; set; }
    public bool IsInDeck { get; set; }
    public Dictionary<string, string> Stats { get; set; } = new Dictionary<string, string>();
    public List<NFTSkill> Skills { get; set; } = new List<NFTSkill>();
    
    // Tracking properties
    public DateTime DateAdded { get; set; } = DateTime.Now;
    
    public override string ToString()
    {
        return $"NFT {Id}: {Name} ({NFTType}) - Level {Level}";
    }
}

/// <summary>
/// Skill data for NFTs
/// </summary>
[System.Serializable]
public class NFTSkill
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int Damage { get; set; }
    public int Cooldown { get; set; }
}

/// <summary>
/// Types of NFTs
/// </summary>
public enum NFTType
{
    Unknown,
    Avatar,
    Character,
    Unit,
    Chest,
    Trophy
}
#endregion 