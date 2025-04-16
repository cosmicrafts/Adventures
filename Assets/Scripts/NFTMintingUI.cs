using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EdjCase.ICP.Candid.Models;
using Cosmicrafts.backend.Models;

// Type aliases 
using TokenId = EdjCase.ICP.Candid.Models.UnboundedUInt;

/// <summary>
/// Modern UI for minting NFTs with a premium user experience
/// </summary>
public class NFTMintingUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NFTManager nftManager;
    
    [Header("Main UI")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private Button closeErrorButton;
    
    [Header("Template Selection")]
    [SerializeField] private ToggleGroup templateToggleGroup;
    [SerializeField] private List<NFTTemplateOption> templateOptions = new List<NFTTemplateOption>();
    
    [Header("Custom NFT Fields")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField descriptionInput;
    [SerializeField] private TMP_InputField imageUrlInput;
    [SerializeField] private TMP_Dropdown rarityDropdown;
    [SerializeField] private Button pickRandomButton;
    
    [Header("Advanced Options")]
    [SerializeField] private GameObject advancedOptionsPanel;
    [SerializeField] private Toggle advancedToggle;
    [SerializeField] private TMP_InputField healthInput;
    [SerializeField] private TMP_InputField damageInput;
    [SerializeField] private TMP_InputField defenseInput;
    [SerializeField] private TMP_InputField speedInput;
    
    [Header("Preview")]
    [SerializeField] private NFTDisplayItem previewItem;
    [SerializeField] private Button refreshPreviewButton;
    
    [Header("Buttons")]
    [SerializeField] private Button mintButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button mintDeckButton;
    [SerializeField] private TMP_Text mintButtonText;
    
    [Header("Success Panel")]
    [SerializeField] private GameObject successPanel;
    [SerializeField] private NFTDisplayItem successDisplayItem;
    [SerializeField] private TMP_Text successMessage;
    [SerializeField] private Button viewInGalleryButton;
    [SerializeField] private Button mintAnotherButton;
    
    [Header("Random Name Generator")]
    [SerializeField] private List<string> nameAdjectives = new List<string>
    {
        "Fierce", "Mystic", "Ancient", "Shadow", "Royal", "Celestial", "Crystal", 
        "Thundering", "Frozen", "Molten", "Radiant", "Cursed", "Divine", "Heroic"
    };
    
    [SerializeField] private List<string> nameNouns = new List<string>
    {
        "Guardian", "Dragon", "Phoenix", "Knight", "Warrior", "Wizard", "Titan", 
        "Hunter", "Beast", "Spirit", "Champion", "Assassin", "Sentinel", "Golem"
    };
    
    // Runtime variables
    private NFTType currentType = NFTType.Unknown;
    private NFTData previewData;
    private string[] rarityOptions = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
    private bool isMinting = false;
    
    // Events
    public System.Action<NFTData> OnNFTMinted;
    public System.Action OnViewGalleryRequested;
    
    private void Awake()
    {
        // Initialize UI
        InitializeUI();
    }
    
    private void Start()
    {
        // Get reference to NFTManager if not set
        if (nftManager == null)
        {
            nftManager = NFTManager.Instance;
        }
        
        if (nftManager == null)
        {
            Debug.LogError("[NFTMintingUI] NFTManager not found!");
            ShowError("NFT Manager not found. Please ensure it exists in the scene.");
        }
        else
        {
            // Subscribe to events
            nftManager.OnNFTMinted += HandleNFTMinted;
            nftManager.OnMintError += HandleMintError;
            nftManager.OnDeckMinted += HandleDeckMinted;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (nftManager != null)
        {
            nftManager.OnNFTMinted -= HandleNFTMinted;
            nftManager.OnMintError -= HandleMintError;
            nftManager.OnDeckMinted -= HandleDeckMinted;
        }
    }
    
    /// <summary>
    /// Initialize UI components
    /// </summary>
    private void InitializeUI()
    {
        // Setup toggle group for template selection
        foreach (var option in templateOptions)
        {
            if (option.toggle != null)
            {
                option.toggle.group = templateToggleGroup;
                option.toggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) OnTemplateSelected(option);
                });
            }
        }
        
        // Set up the rarity dropdown
        if (rarityDropdown != null)
        {
            rarityDropdown.ClearOptions();
            rarityDropdown.AddOptions(new List<string>(rarityOptions));
        }
        
        // Setup advanced options toggle
        if (advancedToggle != null && advancedOptionsPanel != null)
        {
            advancedToggle.onValueChanged.AddListener((isOn) => advancedOptionsPanel.SetActive(isOn));
            advancedOptionsPanel.SetActive(advancedToggle.isOn);
        }
        
        // Setup random button
        if (pickRandomButton != null)
        {
            pickRandomButton.onClick.AddListener(GenerateRandomInfo);
        }
        
        // Setup refresh preview button
        if (refreshPreviewButton != null)
        {
            refreshPreviewButton.onClick.AddListener(UpdatePreview);
        }
        
        // Setup mint button
        if (mintButton != null)
        {
            mintButton.onClick.AddListener(MintNFT);
            mintButton.interactable = false; // Disabled until valid template selected
        }
        
        // Setup cancel button
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(Cancel);
        }
        
        // Setup mint deck button
        if (mintDeckButton != null)
        {
            mintDeckButton.onClick.AddListener(MintDeck);
        }
        
        // Setup close error button
        if (closeErrorButton != null)
        {
            closeErrorButton.onClick.AddListener(() => errorPanel.SetActive(false));
        }
        
        // Setup success panel buttons
        if (viewInGalleryButton != null)
        {
            viewInGalleryButton.onClick.AddListener(() => {
                OnViewGalleryRequested?.Invoke();
                successPanel.SetActive(false);
                mainPanel.SetActive(false);
            });
        }
        
        if (mintAnotherButton != null)
        {
            mintAnotherButton.onClick.AddListener(() => {
                successPanel.SetActive(false);
                mainPanel.SetActive(true);
                ClearInputs();
            });
        }
        
        // Hide panels at start
        if (errorPanel != null) errorPanel.SetActive(false);
        if (successPanel != null) successPanel.SetActive(false);
        if (loadingOverlay != null) loadingOverlay.SetActive(false);
        
        // Set default values
        ClearInputs();
    }
    
    /// <summary>
    /// Handle template selection change
    /// </summary>
    private void OnTemplateSelected(NFTTemplateOption template)
    {
        currentType = template.nftType;
        
        // Update title
        if (titleText != null)
        {
            titleText.text = $"Mint {currentType} NFT";
        }
        
        // Update mint button text
        if (mintButtonText != null)
        {
            mintButtonText.text = $"Mint {currentType}";
        }
        
        // Enable mint button
        if (mintButton != null)
        {
            mintButton.interactable = true;
        }
        
        // Apply template defaults
        if (nameInput != null)
        {
            nameInput.text = template.defaultName;
        }
        
        if (descriptionInput != null)
        {
            descriptionInput.text = template.defaultDescription;
        }
        
        if (imageUrlInput != null)
        {
            imageUrlInput.text = template.defaultImageUrl;
        }
        
        // Update preview
        UpdatePreview();
    }
    
    /// <summary>
    /// Generate random information for the NFT
    /// </summary>
    private void GenerateRandomInfo()
    {
        // Generate random name
        if (nameInput != null)
        {
            string adjective = nameAdjectives[Random.Range(0, nameAdjectives.Count)];
            string noun = nameNouns[Random.Range(0, nameNouns.Count)];
            nameInput.text = $"{adjective} {noun}";
        }
        
        // Generate random description
        if (descriptionInput != null)
        {
            string[] descParts = new string[]
            {
                "A powerful entity from a distant realm.",
                "Ancient artifact of immense power.",
                "Legendary creature with mysterious origins.",
                "Rare collectible sought by many adventurers.",
                "Mythical being with extraordinary abilities."
            };
            
            descriptionInput.text = descParts[Random.Range(0, descParts.Length)];
        }
        
        // Set random rarity
        if (rarityDropdown != null)
        {
            rarityDropdown.value = Random.Range(0, rarityDropdown.options.Count);
        }
        
        // Set random advanced options
        if (healthInput != null) healthInput.text = Random.Range(50, 200).ToString();
        if (damageInput != null) damageInput.text = Random.Range(10, 50).ToString();
        if (defenseInput != null) defenseInput.text = Random.Range(5, 30).ToString();
        if (speedInput != null) speedInput.text = Random.Range(3, 10).ToString();
        
        // Update preview
        UpdatePreview();
    }
    
    /// <summary>
    /// Update the NFT preview based on current inputs
    /// </summary>
    private void UpdatePreview()
    {
        if (previewItem == null) return;
        
        // Create a preview NFT data object
        previewData = new NFTData
        {
            Name = nameInput != null ? nameInput.text : "Preview NFT",
            Description = descriptionInput != null ? descriptionInput.text : "Preview description",
            ImageUrl = imageUrlInput != null ? imageUrlInput.text : "",
            NFTType = currentType,
            Level = 1
        };
        
        // Add stats if advanced options are enabled
        if (advancedToggle != null && advancedToggle.isOn)
        {
            int health = 100;
            int damage = 20;
            int defense = 10;
            int speed = 5;
            
            // Parse input values
            int.TryParse(healthInput?.text, out health);
            int.TryParse(damageInput?.text, out damage);
            int.TryParse(defenseInput?.text, out defense);
            int.TryParse(speedInput?.text, out speed);
            
            // Add to stats dictionary
            previewData.Stats["Health"] = health.ToString();
            previewData.Stats["Damage"] = damage.ToString();
            previewData.Stats["Defense"] = defense.ToString();
            previewData.Stats["Speed"] = speed.ToString();
        }
        
        // Update the preview display
        previewItem.Setup(previewData);
    }
    
    /// <summary>
    /// Mint the NFT with current settings
    /// </summary>
    private async void MintNFT()
    {
        if (isMinting || nftManager == null) return;
        
        // Validate inputs
        if (string.IsNullOrWhiteSpace(nameInput?.text))
        {
            ShowError("Please enter a name for your NFT.");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(descriptionInput?.text))
        {
            ShowError("Please enter a description for your NFT.");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(imageUrlInput?.text))
        {
            ShowError("Please enter an image URL for your NFT.");
            return;
        }
        
        // Start minting
        isMinting = true;
        ShowLoading("Minting your NFT...");
        
        try
        {
            NFTData mintedNFT = null;
            
            // Mint different NFT types
            switch (currentType)
            {
                case NFTType.Avatar:
                    mintedNFT = await nftManager.MintAvatar(
                        nameInput.text,
                        descriptionInput.text,
                        imageUrlInput.text
                    );
                    break;
                
                case NFTType.Unit:
                    // For Unit, we need to pass Unit info
                    Unit unitInfo = new Unit();
                    mintedNFT = await nftManager.MintUnit(
                        nameInput.text,
                        descriptionInput.text,
                        imageUrlInput.text,
                        unitInfo
                    );
                    break;
                
                case NFTType.Chest:
                    mintedNFT = await nftManager.MintChest(
                        nameInput.text,
                        descriptionInput.text,
                        imageUrlInput.text
                    );
                    break;
                
                case NFTType.Trophy:
                    mintedNFT = await nftManager.MintTrophy(
                        nameInput.text,
                        descriptionInput.text,
                        imageUrlInput.text
                    );
                    break;
                
                default:
                    ShowError("Invalid NFT type selected.");
                    break;
            }
            
            // Show success or error
            if (mintedNFT != null)
            {
                ShowSuccess(mintedNFT);
                OnNFTMinted?.Invoke(mintedNFT);
            }
            else
            {
                ShowError("Failed to mint NFT. Please try again.");
            }
        }
        catch (System.Exception e)
        {
            ShowError($"Error minting NFT: {e.Message}");
        }
        finally
        {
            isMinting = false;
            HideLoading();
        }
    }
    
    /// <summary>
    /// Mint a deck of NFTs
    /// </summary>
    private async void MintDeck()
    {
        if (isMinting || nftManager == null) return;
        
        // Start minting
        isMinting = true;
        ShowLoading("Minting your starter deck...");
        
        try
        {
            var mintedNFTs = await nftManager.MintDeck();
            
            // Show success or error
            if (mintedNFTs != null && mintedNFTs.Count > 0)
            {
                ShowSuccessDeck(mintedNFTs);
            }
            else
            {
                ShowError("Failed to mint deck. Please try again.");
            }
        }
        catch (System.Exception e)
        {
            ShowError($"Error minting deck: {e.Message}");
        }
        finally
        {
            isMinting = false;
            HideLoading();
        }
    }
    
    /// <summary>
    /// Cancel minting and clear inputs
    /// </summary>
    private void Cancel()
    {
        ClearInputs();
        mainPanel.SetActive(false);
    }
    
    /// <summary>
    /// Clear all input fields
    /// </summary>
    private void ClearInputs()
    {
        if (nameInput != null) nameInput.text = "";
        if (descriptionInput != null) descriptionInput.text = "";
        if (imageUrlInput != null) imageUrlInput.text = "https://placehold.co/400x400/png";
        if (rarityDropdown != null) rarityDropdown.value = 0;
        
        if (healthInput != null) healthInput.text = "100";
        if (damageInput != null) damageInput.text = "20";
        if (defenseInput != null) defenseInput.text = "10";
        if (speedInput != null) speedInput.text = "5";
        
        if (advancedToggle != null) advancedToggle.isOn = false;
        if (advancedOptionsPanel != null) advancedOptionsPanel.SetActive(false);
        
        // Deselect all template options
        if (templateToggleGroup != null)
        {
            templateToggleGroup.SetAllTogglesOff();
        }
        
        currentType = NFTType.Unknown;
        
        // Update UI
        if (titleText != null)
        {
            titleText.text = "Mint New NFT";
        }
        
        if (mintButtonText != null)
        {
            mintButtonText.text = "Mint NFT";
        }
        
        if (mintButton != null)
        {
            mintButton.interactable = false;
        }
        
        // Clear preview
        if (previewItem != null)
        {
            previewData = null;
            previewItem.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show loading overlay with message
    /// </summary>
    private void ShowLoading(string message)
    {
        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(true);
            
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }
    }
    
    /// <summary>
    /// Hide loading overlay
    /// </summary>
    private void HideLoading()
    {
        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show error panel with message
    /// </summary>
    private void ShowError(string message)
    {
        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
            
            if (errorText != null)
            {
                errorText.text = message;
            }
        }
        else
        {
            Debug.LogError($"[NFTMintingUI] {message}");
        }
    }
    
    /// <summary>
    /// Show success panel with minted NFT
    /// </summary>
    private void ShowSuccess(NFTData mintedNFT)
    {
        if (successPanel != null)
        {
            successPanel.SetActive(true);
            mainPanel.SetActive(false);
            
            if (successDisplayItem != null)
            {
                successDisplayItem.Setup(mintedNFT);
            }
            
            if (successMessage != null)
            {
                successMessage.text = $"Congratulations! Your {mintedNFT.NFTType} NFT '{mintedNFT.Name}' has been successfully minted.";
            }
        }
    }
    
    /// <summary>
    /// Show success panel for deck minting
    /// </summary>
    private void ShowSuccessDeck(List<NFTData> mintedNFTs)
    {
        if (successPanel != null)
        {
            successPanel.SetActive(true);
            mainPanel.SetActive(false);
            
            if (successDisplayItem != null && mintedNFTs.Count > 0)
            {
                // Show the first NFT as a sample
                successDisplayItem.Setup(mintedNFTs[0]);
            }
            
            if (successMessage != null)
            {
                successMessage.text = $"Congratulations! Your starter deck with {mintedNFTs.Count} NFTs has been successfully minted.";
            }
        }
    }
    
    /// <summary>
    /// Show the minting UI
    /// </summary>
    public void Show()
    {
        mainPanel.SetActive(true);
        successPanel.SetActive(false);
        errorPanel.SetActive(false);
        ClearInputs();
    }
    
    /// <summary>
    /// Hide the minting UI
    /// </summary>
    public void Hide()
    {
        mainPanel.SetActive(false);
        successPanel.SetActive(false);
        errorPanel.SetActive(false);
    }
    
    #region Event Handlers
    
    /// <summary>
    /// Handle NFT minted event from NFTManager
    /// </summary>
    private void HandleNFTMinted(TokenId tokenId)
    {
        Debug.Log($"[NFTMintingUI] NFT Minted: {tokenId}");
    }
    
    /// <summary>
    /// Handle mint error event from NFTManager
    /// </summary>
    private void HandleMintError(string errorMessage)
    {
        isMinting = false;
        HideLoading();
        ShowError(errorMessage);
    }
    
    /// <summary>
    /// Handle deck minted event from NFTManager
    /// </summary>
    private void HandleDeckMinted(List<TokenId> tokenIds)
    {
        Debug.Log($"[NFTMintingUI] Deck Minted with {tokenIds.Count} NFTs");
    }
    #endregion
}

/// <summary>
/// Template option for NFT minting
/// </summary>
[System.Serializable]
public class NFTTemplateOption
{
    public Toggle toggle;
    public NFTType nftType;
    public string defaultName;
    public string defaultDescription;
    public string defaultImageUrl;
} 