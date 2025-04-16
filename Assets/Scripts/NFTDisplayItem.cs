using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// Enhanced component for displaying and interacting with an individual NFT in the UI
/// </summary>
public class NFTDisplayItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Core UI Elements")]
    [SerializeField] private Image background;
    [SerializeField] private Image frame;
    [SerializeField] private Image rarityIndicator;
    [SerializeField] private TMP_Text textName;
    [SerializeField] private TMP_Text textType;
    [SerializeField] private TMP_Text textLevel;
    
    [Header("NFT Image")]
    [SerializeField] private Image imageNFT;
    [SerializeField] private RawImage rawImageNFT;
    [SerializeField] private AspectRatioFitter imageFitter;
    [SerializeField] private GameObject loadingIndicator;
    
    [Header("Selection UI")]
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private GameObject deckIndicator;
    
    [Header("Detail UI")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TMP_Text textId;
    [SerializeField] private TMP_Text textDescription;
    [SerializeField] private Transform statsContainer;
    [SerializeField] private Transform skillsContainer;
    [SerializeField] private GameObject statPrefab;
    [SerializeField] private GameObject skillPrefab;
    
    [Header("Buttons")]
    [SerializeField] private Button buttonCopyId;
    [SerializeField] private Button buttonViewDetails;
    [SerializeField] private Button buttonUse;
    [SerializeField] private Button buttonUpgrade;
    [SerializeField] private Button buttonOpen; // For chests
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hoverTrigger = "Hover";
    [SerializeField] private string selectTrigger = "Select";
    
    [Header("Type Colors")]
    [SerializeField] private Color avatarColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color characterColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color unitColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color chestColor = new Color(0.8f, 0.7f, 0.2f);
    [SerializeField] private Color trophyColor = new Color(0.8f, 0.4f, 0.8f);
    
    // NFT Data
    private NFTData nftData;
    private NFTScriptableObject nftSO;
    private bool isSelected = false;
    private bool showingDetails = false;
    
    // Events
    public System.Action<NFTDisplayItem> OnItemClicked;
    public System.Action<NFTDisplayItem> OnDetailRequested;
    public System.Action<NFTDisplayItem> OnUseRequested;
    public System.Action<NFTDisplayItem> OnUpgradeRequested; 
    public System.Action<NFTDisplayItem> OnOpenRequested;
    
    // Cached components
    private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Hide detail panel by default
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
        
        // Initial setup of buttons
        SetupButtons();
    }
    
    /// <summary>
    /// Setup button listeners
    /// </summary>
    private void SetupButtons()
    {
        if (buttonCopyId != null)
        {
            buttonCopyId.onClick.RemoveAllListeners();
            buttonCopyId.onClick.AddListener(CopyIdToClipboard);
        }
        
        if (buttonViewDetails != null)
        {
            buttonViewDetails.onClick.RemoveAllListeners();
            buttonViewDetails.onClick.AddListener(ToggleDetails);
        }
        
        if (buttonUse != null)
        {
            buttonUse.onClick.RemoveAllListeners();
            buttonUse.onClick.AddListener(() => OnUseRequested?.Invoke(this));
        }
        
        if (buttonUpgrade != null)
        {
            buttonUpgrade.onClick.RemoveAllListeners();
            buttonUpgrade.onClick.AddListener(() => OnUpgradeRequested?.Invoke(this));
        }
        
        if (buttonOpen != null)
        {
            buttonOpen.onClick.RemoveAllListeners();
            buttonOpen.onClick.AddListener(() => OnOpenRequested?.Invoke(this));
        }
    }
    
    /// <summary>
    /// Setup this NFT display item with NFTData
    /// </summary>
    public void Setup(NFTData data)
    {
        nftData = data;
        SetupUI();
        
        // Load image if URL provided
        if (!string.IsNullOrEmpty(data.ImageUrl))
        {
            StartCoroutine(LoadImageFromUrl(data.ImageUrl));
        }
        else if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }
    }
    
    /// <summary>
    /// Setup this NFT display item with NFTScriptableObject
    /// </summary>
    public void Setup(NFTScriptableObject scriptableObject)
    {
        nftSO = scriptableObject;
        nftData = scriptableObject.ToNFTData();
        SetupUI();
        
        // Use sprite if available, otherwise load from URL
        if (nftSO.thumbnail != null)
        {
            if (imageNFT != null)
            {
                imageNFT.sprite = nftSO.thumbnail;
                imageNFT.color = Color.white;
                if (loadingIndicator != null) loadingIndicator.SetActive(false);
            }
        }
        else if (!string.IsNullOrEmpty(nftData.ImageUrl))
        {
            StartCoroutine(LoadImageFromUrl(nftData.ImageUrl));
        }
        else if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }
    }
    
    /// <summary>
    /// Setup UI elements with NFT data
    /// </summary>
    private void SetupUI()
    {
        // Set text fields
        if (textName != null) textName.text = nftData.Name;
        if (textType != null) textType.text = nftData.NFTType.ToString();
        if (textLevel != null) textLevel.text = $"Lvl {nftData.Level}";
        if (textId != null) textId.text = $"ID: {nftData.Id}";
        if (textDescription != null) textDescription.text = nftData.Description;
        
        // Set color based on NFT type
        Color typeColor = GetColorForType(nftData.NFTType);
        if (frame != null) frame.color = typeColor;
        if (textType != null) textType.color = typeColor;
        
        // Set selection indicator
        SetSelected(nftData.IsSelected);
        
        // Set deck indicator
        if (deckIndicator != null)
        {
            deckIndicator.SetActive(nftData.IsInDeck);
        }
        
        // Setup stats
        PopulateStats();
        
        // Setup skills
        PopulateSkills();
        
        // Configure buttons based on NFT type
        ConfigureButtonsForType();
    }
    
    /// <summary>
    /// Configure which buttons are shown based on NFT type
    /// </summary>
    private void ConfigureButtonsForType()
    {
        if (buttonUse != null)
        {
            // "Use" button only for Avatars (for selection) and Units (for deck)
            bool canUse = nftData.NFTType == NFTType.Avatar || nftData.NFTType == NFTType.Unit;
            buttonUse.gameObject.SetActive(canUse);
            
            // Update button text based on type
            TMP_Text buttonText = buttonUse.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = nftData.NFTType == NFTType.Avatar ? "Select" : "Add to Deck";
            }
        }
        
        if (buttonOpen != null)
        {
            // "Open" button only for Chests
            buttonOpen.gameObject.SetActive(nftData.NFTType == NFTType.Chest);
        }
    }
    
    /// <summary>
    /// Populate the stats container with stat items
    /// </summary>
    private void PopulateStats()
    {
        if (statsContainer == null || statPrefab == null) return;
        
        // Clear existing stats
        foreach (Transform child in statsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Add new stats
        foreach (var stat in nftData.Stats)
        {
            GameObject statItem = Instantiate(statPrefab, statsContainer);
            
            TMP_Text statNameText = statItem.transform.Find("StatName")?.GetComponent<TMP_Text>();
            TMP_Text statValueText = statItem.transform.Find("StatValue")?.GetComponent<TMP_Text>();
            
            if (statNameText != null) statNameText.text = stat.Key;
            if (statValueText != null) statValueText.text = stat.Value;
        }
    }
    
    /// <summary>
    /// Populate the skills container with skill items
    /// </summary>
    private void PopulateSkills()
    {
        if (skillsContainer == null || skillPrefab == null) return;
        
        // Clear existing skills
        foreach (Transform child in skillsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Add new skills
        foreach (var skill in nftData.Skills)
        {
            GameObject skillItem = Instantiate(skillPrefab, skillsContainer);
            
            TMP_Text skillNameText = skillItem.transform.Find("SkillName")?.GetComponent<TMP_Text>();
            TMP_Text skillDescText = skillItem.transform.Find("SkillDescription")?.GetComponent<TMP_Text>();
            TMP_Text skillDamageText = skillItem.transform.Find("SkillDamage")?.GetComponent<TMP_Text>();
            TMP_Text skillCooldownText = skillItem.transform.Find("SkillCooldown")?.GetComponent<TMP_Text>();
            
            if (skillNameText != null) skillNameText.text = skill.Name;
            if (skillDescText != null) skillDescText.text = skill.Description;
            if (skillDamageText != null) skillDamageText.text = $"DMG: {skill.Damage}";
            if (skillCooldownText != null) skillCooldownText.text = $"CD: {skill.Cooldown}s";
        }
    }
    
    /// <summary>
    /// Get the appropriate color for an NFT type
    /// </summary>
    private Color GetColorForType(NFTType type)
    {
        switch (type)
        {
            case NFTType.Avatar:
                return avatarColor;
            case NFTType.Character:
                return characterColor;
            case NFTType.Unit:
                return unitColor;
            case NFTType.Chest:
                return chestColor;
            case NFTType.Trophy:
                return trophyColor;
            default:
                return Color.gray;
        }
    }
    
    /// <summary>
    /// Load an image from a URL
    /// </summary>
    private IEnumerator LoadImageFromUrl(string url)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();
            
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                
                // Set aspect ratio if available
                if (imageFitter != null && texture != null)
                {
                    imageFitter.aspectRatio = (float)texture.width / texture.height;
                }
                
                // Use RawImage if available, otherwise use Image with Sprite
                if (rawImageNFT != null)
                {
                    rawImageNFT.texture = texture;
                    rawImageNFT.color = Color.white;
                }
                else if (imageNFT != null)
                {
                    // Create sprite from texture
                    Rect rect = new Rect(0, 0, texture.width, texture.height);
                    Vector2 pivot = new Vector2(0.5f, 0.5f);
                    Sprite sprite = Sprite.Create(texture, rect, pivot);
                    
                    imageNFT.sprite = sprite;
                    imageNFT.color = Color.white;
                    
                    // Cache the sprite in the scriptable object if we have one
                    if (nftSO != null)
                    {
                        nftSO.thumbnail = sprite;
                    }
                }
            }
            else
            {
                Debug.LogError($"[NFTDisplayItem] Failed to load image: {webRequest.error}");
                
                // Set placeholder image or error message
                if (rawImageNFT != null)
                {
                    rawImageNFT.color = Color.gray;
                }
                else if (imageNFT != null)
                {
                    imageNFT.color = Color.gray;
                }
            }
        }
    }
    
    /// <summary>
    /// Copy the NFT ID to clipboard
    /// </summary>
    public void CopyIdToClipboard()
    {
        if (nftData != null && nftData.Id != null)
        {
            string id = nftData.Id.ToString();
            GUIUtility.systemCopyBuffer = id;
            Debug.Log($"[NFTDisplayItem] Copied ID to clipboard: {id}");
            
            // Show feedback
            if (textId != null)
            {
                StartCoroutine(FlashText(textId));
            }
        }
    }
    
    /// <summary>
    /// Toggle the details panel
    /// </summary>
    public void ToggleDetails()
    {
        if (detailPanel != null)
        {
            showingDetails = !showingDetails;
            detailPanel.SetActive(showingDetails);
            
            if (showingDetails)
            {
                OnDetailRequested?.Invoke(this);
            }
        }
    }
    
    /// <summary>
    /// Show the details panel
    /// </summary>
    public void ShowDetails()
    {
        if (detailPanel != null && !showingDetails)
        {
            showingDetails = true;
            detailPanel.SetActive(true);
            OnDetailRequested?.Invoke(this);
        }
    }
    
    /// <summary>
    /// Hide the details panel
    /// </summary>
    public void HideDetails()
    {
        if (detailPanel != null && showingDetails)
        {
            showingDetails = false;
            detailPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Set the selected state of this item
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(selected);
        }
        
        if (animator != null && selected)
        {
            animator.SetTrigger(selectTrigger);
        }
    }
    
    /// <summary>
    /// Set the "in deck" state of this item
    /// </summary>
    public void SetInDeck(bool inDeck)
    {
        if (nftData != null)
        {
            nftData.IsInDeck = inDeck;
        }
        
        if (deckIndicator != null)
        {
            deckIndicator.SetActive(inDeck);
        }
    }
    
    /// <summary>
    /// Flash a text component to show feedback
    /// </summary>
    private IEnumerator FlashText(TMP_Text text)
    {
        if (text == null) yield break;
        
        Color originalColor = text.color;
        text.color = Color.green;
        
        yield return new WaitForSeconds(0.5f);
        
        text.color = originalColor;
    }
    
    /// <summary>
    /// Get the NFT data for this display item
    /// </summary>
    public NFTData GetNFTData()
    {
        return nftData;
    }
    
    /// <summary>
    /// Get the NFT ScriptableObject for this display item (if available)
    /// </summary>
    public NFTScriptableObject GetNFTScriptableObject()
    {
        return nftSO;
    }
    
    #region Event Handlers
    public void OnPointerClick(PointerEventData eventData)
    {
        OnItemClicked?.Invoke(this);
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (animator != null)
        {
            animator.SetTrigger(hoverTrigger);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // Optional animation for hover exit
    }
    #endregion
} 
