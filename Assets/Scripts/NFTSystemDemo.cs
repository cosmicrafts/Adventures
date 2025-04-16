using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cosmicrafts.backend.Models;

/// <summary>
/// Demo script for showcasing the NFT system functionality
/// </summary>
public class NFTSystemDemo : MonoBehaviour
{
    [Header("NFT System")]
    [SerializeField] private NFTSystemController nftSystem;
    [SerializeField] private NFTManager nftManager;
    
    [Header("Demo UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button demoMintAvatarButton;
    [SerializeField] private Button demoMintUnitButton;
    [SerializeField] private Button demoMintChestButton;
    [SerializeField] private Button demoMintDeckButton;
    [SerializeField] private TMP_InputField principalIdInput;
    [SerializeField] private GameObject loadingPanel;
    
    [Header("NFT Stats Display")]
    [SerializeField] private TMP_Text totalNFTsText;
    [SerializeField] private TMP_Text avatarsCountText;
    [SerializeField] private TMP_Text unitsCountText;
    [SerializeField] private TMP_Text chestsCountText;
    [SerializeField] private TMP_Text trophiesCountText;
    
    [Header("Demo Settings")]
    [SerializeField] private bool enableQuickMint = true;
    [SerializeField] private float autoRefreshInterval = 10f;
    
    private bool isRefreshing = false;
    private Coroutine autoRefreshCoroutine;
    
    private void Start()
    {
        // Find references if not set
        if (nftSystem == null)
        {
            nftSystem = FindObjectOfType<NFTSystemController>();
        }
        
        if (nftManager == null && nftSystem != null)
        {
            nftManager = NFTManager.Instance;
        }
        
        // Setup button listeners
        SetupDemoButtons();
        
        // Subscribe to events
        if (nftManager != null)
        {
            nftManager.OnNFTsLoaded += UpdateStats;
            nftManager.OnNFTAdded += (_) => UpdateStats();
            nftManager.OnNFTRemoved += (_) => UpdateStats();
        }
        
        // Start auto-refresh if enabled
        if (autoRefreshInterval > 0)
        {
            autoRefreshCoroutine = StartCoroutine(AutoRefreshStats());
        }
        
        // Set initial status
        if (statusText != null)
        {
            statusText.text = "NFT System Demo initialized. Click buttons to test functionality.";
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (nftManager != null)
        {
            nftManager.OnNFTsLoaded -= UpdateStats;
        }
        
        // Stop auto-refresh
        if (autoRefreshCoroutine != null)
        {
            StopCoroutine(autoRefreshCoroutine);
        }
    }
    
    /// <summary>
    /// Setup demo button listeners
    /// </summary>
    private void SetupDemoButtons()
    {
        if (demoMintAvatarButton != null)
        {
            demoMintAvatarButton.onClick.AddListener(() =>
            {
                if (enableQuickMint)
                {
                    QuickMintAvatar();
                }
                else
                {
                    nftSystem.OpenMinting();
                }
            });
        }
        
        if (demoMintUnitButton != null)
        {
            demoMintUnitButton.onClick.AddListener(() =>
            {
                if (enableQuickMint)
                {
                    QuickMintUnit();
                }
                else
                {
                    nftSystem.OpenMinting();
                }
            });
        }
        
        if (demoMintChestButton != null)
        {
            demoMintChestButton.onClick.AddListener(() =>
            {
                if (enableQuickMint)
                {
                    QuickMintChest();
                }
                else
                {
                    nftSystem.OpenMinting();
                }
            });
        }
        
        if (demoMintDeckButton != null)
        {
            demoMintDeckButton.onClick.AddListener(() =>
            {
                QuickMintDeck();
            });
        }
    }
    
    /// <summary>
    /// Update NFT statistics display
    /// </summary>
    private void UpdateStats()
    {
        if (nftManager == null) return;
        
        int totalNFTs = nftManager.AllNFTs.Count;
        int avatars = nftManager.Avatars.Count;
        int units = nftManager.Units.Count;
        int chests = nftManager.Chests.Count;
        int trophies = nftManager.Trophies.Count;
        
        if (totalNFTsText != null) totalNFTsText.text = $"Total NFTs: {totalNFTs}";
        if (avatarsCountText != null) avatarsCountText.text = $"Avatars: {avatars}";
        if (unitsCountText != null) unitsCountText.text = $"Units: {units}";
        if (chestsCountText != null) chestsCountText.text = $"Chests: {chests}";
        if (trophiesCountText != null) trophiesCountText.text = $"Trophies: {trophies}";
        
        if (statusText != null)
        {
            statusText.text = $"NFT Stats updated: {totalNFTs} total NFTs.";
        }
    }
    
    /// <summary>
    /// Auto-refresh stats at interval
    /// </summary>
    private IEnumerator AutoRefreshStats()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoRefreshInterval);
            
            if (!isRefreshing && nftManager != null)
            {
                yield return RefreshNFTs();
            }
        }
    }
    
    /// <summary>
    /// Refresh NFTs from blockchain
    /// </summary>
    private IEnumerator RefreshNFTs()
    {
        if (isRefreshing || nftManager == null) yield break;
        
        isRefreshing = true;
        
        // Show loading if available
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        if (statusText != null)
        {
            statusText.text = "Refreshing NFTs from blockchain...";
        }
        
        // Refresh NFTs
        var refreshTask = nftManager.RefreshNFTs();
        
        // Wait for refresh to complete
        while (!refreshTask.IsCompleted)
        {
            yield return null;
        }
        
        // Hide loading
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        if (statusText != null)
        {
            statusText.text = "NFTs refreshed successfully.";
        }
        
        isRefreshing = false;
    }
    
    /// <summary>
    /// Quick mint an avatar without UI
    /// </summary>
    private async void QuickMintAvatar()
    {
        if (nftManager == null) return;
        
        if (statusText != null)
        {
            statusText.text = "Quick minting Avatar...";
        }
        
        // Show loading if available
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        // Generate random name
        string[] adjectives = { "Fierce", "Mystic", "Ancient", "Shadow", "Royal" };
        string[] nouns = { "Guardian", "Dragon", "Phoenix", "Knight", "Warrior" };
        
        string name = $"{adjectives[Random.Range(0, adjectives.Length)]} {nouns[Random.Range(0, nouns.Length)]}";
        string description = "A demo avatar minted using quick mint.";
        string imageUrl = "https://placehold.co/400x400/png";
        
        try
        {
            var avatar = await nftManager.MintAvatar(name, description, imageUrl);
            
            if (avatar != null)
            {
                if (statusText != null)
                {
                    statusText.text = $"Successfully minted Avatar: {name}";
                }
                
                UpdateStats();
            }
            else
            {
                if (statusText != null)
                {
                    statusText.text = "Failed to mint Avatar.";
                }
            }
        }
        catch (System.Exception e)
        {
            if (statusText != null)
            {
                statusText.text = $"Error minting Avatar: {e.Message}";
            }
        }
        finally
        {
            // Hide loading
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Quick mint a unit without UI
    /// </summary>
    private async void QuickMintUnit()
    {
        if (nftManager == null) return;
        
        if (statusText != null)
        {
            statusText.text = "Quick minting Unit...";
        }
        
        // Show loading if available
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        // Generate random name
        string[] adjectives = { "Elite", "Savage", "Deadly", "Veteran", "Heroic" };
        string[] nouns = { "Soldier", "Archer", "Mage", "Knight", "Assassin" };
        
        string name = $"{adjectives[Random.Range(0, adjectives.Length)]} {nouns[Random.Range(0, nouns.Length)]}";
        string description = "A demo unit minted using quick mint.";
        string imageUrl = "https://placehold.co/400x400/png";
        
        try
        {
            // Create a Unit info object
            Unit unitInfo = new Unit();
            
            var unit = await nftManager.MintUnit(name, description, imageUrl, unitInfo);
            
            if (unit != null)
            {
                if (statusText != null)
                {
                    statusText.text = $"Successfully minted Unit: {name}";
                }
                
                UpdateStats();
            }
            else
            {
                if (statusText != null)
                {
                    statusText.text = "Failed to mint Unit.";
                }
            }
        }
        catch (System.Exception e)
        {
            if (statusText != null)
            {
                statusText.text = $"Error minting Unit: {e.Message}";
            }
        }
        finally
        {
            // Hide loading
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Quick mint a chest without UI
    /// </summary>
    private async void QuickMintChest()
    {
        if (nftManager == null) return;
        
        if (statusText != null)
        {
            statusText.text = "Quick minting Chest...";
        }
        
        // Show loading if available
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        // Generate random name
        string[] adjectives = { "Mysterious", "Ancient", "Golden", "Enchanted", "Lost" };
        string[] nouns = { "Chest", "Crate", "Box", "Container", "Coffer" };
        
        string name = $"{adjectives[Random.Range(0, adjectives.Length)]} {nouns[Random.Range(0, nouns.Length)]}";
        string description = "A demo chest minted using quick mint. Open to discover its contents!";
        string imageUrl = "https://placehold.co/400x400/png";
        
        try
        {
            var chest = await nftManager.MintChest(name, description, imageUrl);
            
            if (chest != null)
            {
                if (statusText != null)
                {
                    statusText.text = $"Successfully minted Chest: {name}";
                }
                
                UpdateStats();
            }
            else
            {
                if (statusText != null)
                {
                    statusText.text = "Failed to mint Chest.";
                }
            }
        }
        catch (System.Exception e)
        {
            if (statusText != null)
            {
                statusText.text = $"Error minting Chest: {e.Message}";
            }
        }
        finally
        {
            // Hide loading
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Quick mint a deck without UI
    /// </summary>
    private async void QuickMintDeck()
    {
        if (nftManager == null) return;
        
        if (statusText != null)
        {
            statusText.text = "Minting a starter deck...";
        }
        
        // Show loading if available
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        try
        {
            var deck = await nftManager.MintDeck();
            
            if (deck != null && deck.Count > 0)
            {
                if (statusText != null)
                {
                    statusText.text = $"Successfully minted Deck with {deck.Count} NFTs!";
                }
                
                UpdateStats();
            }
            else
            {
                if (statusText != null)
                {
                    statusText.text = "Failed to mint Deck.";
                }
            }
        }
        catch (System.Exception e)
        {
            if (statusText != null)
            {
                statusText.text = $"Error minting Deck: {e.Message}";
            }
        }
        finally
        {
            // Hide loading
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Open the NFT gallery
    /// </summary>
    public void OpenGallery()
    {
        if (nftSystem != null)
        {
            nftSystem.OpenGallery();
        }
    }
    
    /// <summary>
    /// Open the minting UI
    /// </summary>
    public void OpenMinting()
    {
        if (nftSystem != null)
        {
            nftSystem.OpenMinting();
        }
    }
    
    /// <summary>
    /// Manually refresh NFTs
    /// </summary>
    public void ManualRefresh()
    {
        StartCoroutine(RefreshNFTs());
    }
} 