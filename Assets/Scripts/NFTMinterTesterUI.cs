using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Cosmicrafts.backend.Models;
using EdjCase.ICP.Candid.Models;
using System.Threading.Tasks;
using Microsoft.CSharp; // Add reference for dynamic binding

// Type aliases 
using TokenId = EdjCase.ICP.Candid.Models.UnboundedUInt;

/// <summary>
/// UI test harness for the NFTMinterService
/// This provides a complete UI for testing all NFT minting and retrieval functions
/// </summary>
public class NFTMinterTesterUI : MonoBehaviour
{
    [Header("Services")]
    private NFTMinterService nftService;
    private ICPService icpService;

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField inputName;
    [SerializeField] private TMP_InputField inputDescription;
    [SerializeField] private TMP_InputField inputImageUrl;
    [SerializeField] private TMP_InputField inputChestTokenId;
    
    [Header("Output")]
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private ScrollRect outputScrollRect;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject nftDisplayPrefab;
    [SerializeField] private Transform nftDisplayContainer;

    private void Start()
    {
        // Find required services
        nftService = NFTMinterService.Instance;
        icpService = ICPService.Instance;
        
        // Check for services
        if (nftService == null)
        {
            LogError("NFTMinterService not found! Make sure it's in the scene.");
            Debug.LogError("NFTMinterService not found! Create a GameObject and add NFTMinterService component.");
        }
        
        if (icpService == null)
        {
            LogError("ICPService not found! Make sure it's in the scene.");
            Debug.LogError("ICPService not found! Create a GameObject and add ICPService component.");
        }
        
        // Setup event listeners
        if (nftService != null)
        {
            nftService.OnNFTMinted += HandleNFTMinted;
            nftService.OnMintError += HandleMintError;
            nftService.OnDeckMinted += HandleDeckMinted;
            nftService.OnChestMinted += HandleChestMinted;
        }
        
        // Set default values for testing
        if (inputName != null) inputName.text = "Test NFT " + UnityEngine.Random.Range(1000, 9999);
        if (inputDescription != null) inputDescription.text = "This is a test NFT created through the NFT Minter UI.";
        if (inputImageUrl != null) inputImageUrl.text = "https://placehold.co/400x400/png";
        
        Log("NFT Minter Tester initialized. Connected to services: " + 
            (nftService != null ? "✓ NFTMinterService " : "✗ NFTMinterService ") + 
            (icpService != null ? "✓ ICPService" : "✗ ICPService"));
        Log("Fill in the fields above and click any minting action button to test.");
    }
    
    private void OnDestroy()
    {
        // Clean up event listeners to prevent memory leaks
        if (nftService != null)
        {
            nftService.OnNFTMinted -= HandleNFTMinted;
            nftService.OnMintError -= HandleMintError;
            nftService.OnDeckMinted -= HandleDeckMinted;
            nftService.OnChestMinted -= HandleChestMinted;
        }
    }
    
    // Event handlers
    private void HandleNFTMinted(TokenId tokenId)
    {
        Log($"<color=green>Event: NFT minted successfully with token ID: {tokenId}</color>");
    }
    
    private void HandleMintError(string errorMessage)
    {
        LogError($"Event: Mint error: {errorMessage}");
    }
    
    private void HandleDeckMinted(List<TokenId> tokenIds)
    {
        Log($"<color=green>Event: Deck minted with {tokenIds.Count} tokens</color>");
        foreach (var id in tokenIds)
        {
            Log($" - Token ID: {id}");
        }
    }
    
    private void HandleChestMinted(TokenId tokenId)
    {
        Log($"<color=green>Event: Chest minted with token ID: {tokenId}</color>");
    }
    
    // UI Button Actions
    
    /// <summary>
    /// Mint an Avatar NFT with the input field values
    /// </summary>
    public async void MintAvatar()
    {
        if (!ValidateInputs()) return;
        
        Log($"Minting Avatar NFT with name: {inputName.text}...");
        try
        {
            TokenId tokenId = await nftService.MintAvatar(
                inputName.text,
                inputDescription.text,
                inputImageUrl.text
            );
            
            if (tokenId != null)
            {
                Log($"<color=green>Avatar NFT minted successfully!</color> Token ID: {tokenId}");
            }
            else
            {
                LogError("Avatar mint failed. Check console for details.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error minting avatar: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Mint a Unit NFT with the input field values
    /// </summary>
    public async void MintUnit()
    {
        if (!ValidateInputs()) return;
        
        // For demo purposes, create a simple Unit
        Unit unitInfo = new Unit();
        
        Log($"Minting Unit NFT with name: {inputName.text}...");
        try
        {
            TokenId tokenId = await nftService.MintUnit(
                inputName.text,
                inputDescription.text,
                inputImageUrl.text,
                unitInfo
            );
            
            if (tokenId != null)
            {
                Log($"<color=green>Unit NFT minted successfully!</color> Token ID: {tokenId}");
            }
            else
            {
                LogError("Unit mint failed. Check console for details.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error minting unit: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Mint a Chest NFT with the input field values
    /// </summary>
    public async void MintChest()
    {
        if (!ValidateInputs()) return;
        
        Log($"Minting Chest NFT with name: {inputName.text}...");
        try
        {
            TokenId tokenId = await nftService.MintChest(
                inputName.text,
                inputDescription.text,
                inputImageUrl.text
            );
            
            if (tokenId != null)
            {
                Log($"<color=green>Chest NFT minted successfully!</color> Token ID: {tokenId}");
                // Auto-populate the chest ID field for easy opening
                if (inputChestTokenId != null)
                {
                    inputChestTokenId.text = tokenId.ToString();
                }
            }
            else
            {
                LogError("Chest mint failed. Check console for details.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error minting chest: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Mint a Trophy NFT with the input field values
    /// </summary>
    public async void MintTrophy()
    {
        if (!ValidateInputs()) return;
        
        Log($"Minting Trophy NFT with name: {inputName.text}...");
        try
        {
            TokenId tokenId = await nftService.MintTrophy(
                inputName.text,
                inputDescription.text,
                inputImageUrl.text
            );
            
            if (tokenId != null)
            {
                Log($"<color=green>Trophy NFT minted successfully!</color> Token ID: {tokenId}");
            }
            else
            {
                LogError("Trophy mint failed. Check console for details.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error minting trophy: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Mint an initial deck of NFTs
    /// </summary>
    public async void MintDeck()
    {
        Log("Minting initial deck of NFTs...");
        try
        {
            List<TokenId> tokenIds = await nftService.MintDeck();
            
            if (tokenIds != null && tokenIds.Count > 0)
            {
                Log($"<color=green>Deck minted successfully!</color> {tokenIds.Count} NFTs received:");
                foreach (var id in tokenIds)
                {
                    Log($" - Token ID: {id}");
                }
            }
            else
            {
                LogError("Deck mint failed or returned empty. Check console for details.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error minting deck: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Open a chest using the chest token ID input field
    /// </summary>
    public async void OpenChest()
    {
        if (string.IsNullOrWhiteSpace(inputChestTokenId.text))
        {
            LogError("Please enter a Chest Token ID to open");
            return;
        }
        
        TokenId chestId;
        try
        {
            // First parse the string to a numeric value, then cast to UnboundedUInt
            ulong numericValue = ulong.Parse(inputChestTokenId.text);
            chestId = (UnboundedUInt)numericValue;
            Log($"Opening chest with Token ID: {chestId}...");
        }
        catch
        {
            LogError($"Invalid chest ID format: {inputChestTokenId.text}");
            return;
        }
        
        try
        {
            bool success = await nftService.OpenChest(chestId);
            
            if (success)
            {
                Log($"<color=green>Chest opened successfully!</color> Check inventory for new items.");
                // Refresh NFT list after opening
                GetAllNFTs();
            }
            else
            {
                LogError("Failed to open chest. Check console for details.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error opening chest: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get all NFTs owned by the current user
    /// </summary>
    public async void GetAllNFTs()
    {
        Log("Fetching all NFTs...");
        try
        {
            var nfts = await nftService.GetUserNFTs();
            
            if (nfts != null)
            {
                Log($"<color=green>Found {nfts.Count} NFTs in your collection:</color>");
                DisplayNFTs(nfts);
            }
            else
            {
                Log("No NFTs found or error fetching NFTs. Check console for details.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error fetching NFTs: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get all Avatars owned by the current user
    /// </summary>
    public async void GetAvatars()
    {
        Log("Fetching your Avatars...");
        try
        {
            var avatars = await nftService.GetUserAvatars();
            
            if (avatars != null)
            {
                Log($"<color=green>Found {avatars.Count} Avatars in your collection:</color>");
                DisplayGenericNFTs(avatars, "Avatar");
            }
            else
            {
                Log("No Avatars found or error fetching Avatars. Check console for details.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error fetching Avatars: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get all Units owned by the current user
    /// </summary>
    public async void GetUnits()
    {
        Log("Fetching your Units...");
        try
        {
            var units = await nftService.GetUserUnits();
            
            if (units != null)
            {
                Log($"<color=green>Found {units.Count} Units in your collection:</color>");
                DisplayGenericNFTs(units, "Unit");
            }
            else
            {
                Log("No Units found or error fetching Units. Check console for details.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error fetching Units: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get all Chests owned by the current user
    /// </summary>
    public async void GetChests()
    {
        Log("Fetching your Chests...");
        try
        {
            var chests = await nftService.GetUserChests();
            
            if (chests != null)
            {
                Log($"<color=green>Found {chests.Count} Chests in your collection:</color>");
                DisplayGenericNFTs(chests, "Chest");
                
                // Auto-populate chest ID field if any chests found
                if (chests.Count > 0 && inputChestTokenId != null)
                {
                    inputChestTokenId.text = chests[0].F0.ToString();
                    Log($"Populated Chest Token ID input with the first available chest: {chests[0].F0}");
                }
            }
            else
            {
                Log("No Chests found or error fetching Chests. Check console for details.");
            }
        }
        catch (Exception ex)
        {
            LogError($"Error fetching Chests: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get minted assets info for the current user
    /// </summary>
    public async void GetMintedInfo()
    {
        Log("Fetching minted assets info...");
        try
        {
            var (chests, gameNFTs, stardust) = await nftService.GetMintedInfo();
            Log($"<color=green>Minted Assets Info:</color>");
            Log($" - Chests: {chests}");
            Log($" - Game NFTs: {gameNFTs}");
            Log($" - Stardust: {stardust}");
        }
        catch (Exception ex)
        {
            LogError($"Error fetching minted info: {ex.Message}");
        }
    }
    
    // Helper methods
    
    /// <summary>
    /// Clear all displayed NFTs
    /// </summary>
    public void ClearDisplayedNFTs()
    {
        if (nftDisplayContainer != null)
        {
            foreach (Transform child in nftDisplayContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Display a list of NFTs in the UI
    /// </summary>
    private void DisplayNFTs<T>(List<T> nfts) where T : class
    {
        ClearDisplayedNFTs();
        
        if (nftDisplayContainer == null || nftDisplayPrefab == null)
        {
            Log("NFT display container or prefab not set. Only showing in log.");
            
            // Just log the NFTs
            foreach (var nft in nfts)
            {
                try
                {
                    // Use reflection instead of dynamic
                    var type = nft.GetType();
                    var f0Property = type.GetProperty("F0");
                    var f1Property = type.GetProperty("F1");
                    
                    if (f0Property != null && f1Property != null)
                    {
                        var f0Value = f0Property.GetValue(nft);
                        var f1Value = f1Property.GetValue(nft);
                        
                        var generalProperty = f1Value.GetType().GetProperty("General");
                        var categoryProperty = f1Value.GetType().GetProperty("Category");
                        
                        if (generalProperty != null && categoryProperty != null)
                        {
                            var general = generalProperty.GetValue(f1Value);
                            var category = categoryProperty.GetValue(f1Value);
                            
                            var nameProperty = general.GetType().GetProperty("Name");
                            var tagProperty = category.GetType().GetProperty("Tag");
                            
                            var name = nameProperty?.GetValue(general)?.ToString() ?? "Unknown";
                            var tag = tagProperty?.GetValue(category)?.ToString() ?? "Unknown";
                            
                            Log($" - ID: {f0Value}, Name: {name}, Type: {tag}");
                        }
                        else
                        {
                            Log($" - {nft}");
                        }
                    }
                    else
                    {
                        Log($" - {nft}");
                    }
                }
                catch (Exception ex)
                {
                    Log($" - {nft} (Error: {ex.Message})");
                }
            }
            return;
        }
        
        foreach (var nft in nfts)
        {
            try
            {
                // Create a display item for each NFT
                GameObject displayItem = Instantiate(nftDisplayPrefab, nftDisplayContainer);
                NFTDisplayItem displayComponent = displayItem.GetComponent<NFTDisplayItem>();
                
                if (displayComponent != null)
                {
                    // Use reflection instead of dynamic
                    var type = nft.GetType();
                    var f0Property = type.GetProperty("F0");
                    var f1Property = type.GetProperty("F1");
                    
                    if (f0Property != null && f1Property != null)
                    {
                        var f0Value = f0Property.GetValue(nft);
                        var f1Value = f1Property.GetValue(nft);
                        
                        var generalProperty = f1Value.GetType().GetProperty("General");
                        var categoryProperty = f1Value.GetType().GetProperty("Category");
                        
                        if (generalProperty != null && categoryProperty != null)
                        {
                            var general = generalProperty.GetValue(f1Value);
                            var category = categoryProperty.GetValue(f1Value);
                            
                            var nameProperty = general.GetType().GetProperty("Name");
                            var descProperty = general.GetType().GetProperty("Description");
                            var imageProperty = general.GetType().GetProperty("Image");
                            var tagProperty = category.GetType().GetProperty("Tag");
                            
                            var tokenId = f0Value.ToString();
                            var name = nameProperty?.GetValue(general)?.ToString() ?? "Unknown";
                            var description = descProperty?.GetValue(general)?.ToString() ?? "";
                            var nftType = tagProperty?.GetValue(category)?.ToString() ?? "Unknown";
                            var image = imageProperty?.GetValue(general)?.ToString() ?? "";
                            
                            // Create an NFTData object for the display item
                            NFTData nftData = new NFTData
                            {
                                Id = (TokenId)ulong.Parse(tokenId),
                                Name = name,
                                Description = description,
                                ImageUrl = image,
                                NFTType = ParseNFTType(nftType)
                            };
                            
                            displayComponent.Setup(nftData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error displaying NFT: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Display a list of NFTs in the UI
    /// </summary>
    private void DisplayGenericNFTs<T>(List<T> items, string type) where T : class
    {
        // Just use the main display method since the structure is the same
        DisplayNFTs(items);
    }
    
    /// <summary>
    /// Validate required input fields
    /// </summary>
    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(inputName?.text))
        {
            LogError("Please enter a name for the NFT");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(inputDescription?.text))
        {
            LogError("Please enter a description for the NFT");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(inputImageUrl?.text))
        {
            LogError("Please enter an image URL for the NFT");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Log a message to the output text area
    /// </summary>
    private void Log(string message)
    {
        Debug.Log($"[NFTTester] {message}");
        
        if (outputText != null)
        {
            outputText.text += $"\n{message}";
            
            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            if (outputScrollRect != null)
            {
                outputScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
    
    /// <summary>
    /// Log an error message to the output text area
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[NFTTester] {message}");
        
        if (outputText != null)
        {
            outputText.text += $"\n<color=red>{message}</color>";
            
            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            if (outputScrollRect != null)
            {
                outputScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
    
    /// <summary>
    /// Clear the output text area
    /// </summary>
    public void ClearOutput()
    {
        if (outputText != null)
        {
            outputText.text = "";
        }
    }
    
    /// <summary>
    /// Helper to parse NFTType from string
    /// </summary>
    private NFTType ParseNFTType(string typeStr)
    {
        if (Enum.TryParse<NFTType>(typeStr, out var result))
        {
            return result;
        }
        return NFTType.Unknown;
    }
}