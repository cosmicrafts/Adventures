using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;
using EdjCase.ICP.Candid.Models;

// Type aliases 
using TokenId = EdjCase.ICP.Candid.Models.UnboundedUInt;

/// <summary>
/// UI component for displaying NFT collections in a gallery format with filtering and sorting
/// </summary>
public class NFTGalleryUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private NFTManager nftManager;
    
    [Header("UI References")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject emptyStateMessage;
    [SerializeField] private TMP_Text galleryTitleText;
    [SerializeField] private GameObject loadingIndicator;
    
    [Header("Filtering")]
    [SerializeField] private TMP_Dropdown filterDropdown;
    [SerializeField] private TMP_InputField searchInputField;
    
    [Header("Sorting")]
    [SerializeField] private TMP_Dropdown sortDropdown;
    [SerializeField] private Button sortDirectionButton;
    [SerializeField] private Image sortDirectionIcon;
    [SerializeField] private Sprite ascendingSprite;
    [SerializeField] private Sprite descendingSprite;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject nftDisplayPrefab;
    
    [Header("Pagination")]
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TMP_Text pageInfoText;
    [SerializeField] private int itemsPerPage = 12;
    
    // Runtime variables
    private List<NFTData> currentNFTs = new List<NFTData>();
    private List<NFTData> filteredNFTs = new List<NFTData>();
    private List<NFTDisplayItem> displayItems = new List<NFTDisplayItem>();
    private NFTType currentFilter = NFTType.Unknown;
    private string currentSearch = "";
    private SortOption currentSortOption = SortOption.NameAZ;
    private bool sortAscending = true;
    private int currentPage = 0;
    private int totalPages = 0;
    
    // Events
    public System.Action<NFTData> OnNFTSelected;
    
    // Caching
    private Dictionary<NFTData, NFTDisplayItem> displayItemsCache = new Dictionary<NFTData, NFTDisplayItem>();
    
    private void Awake()
    {
        // Initialize UI components
        InitializeUI();
    }
    
    private void Start()
    {
        // Get reference to NFTManager if not set
        if (nftManager == null)
        {
            nftManager = NFTManager.Instance;
        }
        
        if (nftManager != null)
        {
            // Subscribe to events
            nftManager.OnNFTsLoaded += OnNFTsLoaded;
            nftManager.OnNFTAdded += OnNFTAdded;
            nftManager.OnNFTUpdated += OnNFTUpdated;
            nftManager.OnNFTRemoved += OnNFTRemoved;
            
            // Load NFTs if already available
            if (nftManager.AllNFTs.Count > 0)
            {
                OnNFTsLoaded();
            }
        }
        else
        {
            Debug.LogError("[NFTGalleryUI] NFTManager not found!");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (nftManager != null)
        {
            nftManager.OnNFTsLoaded -= OnNFTsLoaded;
            nftManager.OnNFTAdded -= OnNFTAdded;
            nftManager.OnNFTUpdated -= OnNFTUpdated;
            nftManager.OnNFTRemoved -= OnNFTRemoved;
        }
    }
    
    /// <summary>
    /// Initialize UI components and add listeners
    /// </summary>
    private void InitializeUI()
    {
        // Initialize filtering dropdown
        if (filterDropdown != null)
        {
            filterDropdown.ClearOptions();
            
            var options = new List<string>
            {
                "All NFTs",
                "Avatars",
                "Characters",
                "Units",
                "Chests",
                "Trophies"
            };
            
            filterDropdown.AddOptions(options);
            filterDropdown.onValueChanged.AddListener(OnFilterChanged);
        }
        
        // Initialize sorting dropdown
        if (sortDropdown != null)
        {
            sortDropdown.ClearOptions();
            
            var options = new List<string>
            {
                "Name (A-Z)",
                "Name (Z-A)",
                "Newest First",
                "Oldest First",
                "Level (High-Low)",
                "Level (Low-High)"
            };
            
            sortDropdown.AddOptions(options);
            sortDropdown.onValueChanged.AddListener(OnSortChanged);
        }
        
        // Initialize sort direction button
        if (sortDirectionButton != null)
        {
            sortDirectionButton.onClick.AddListener(ToggleSortDirection);
            UpdateSortDirectionIcon();
        }
        
        // Initialize search
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchChanged);
        }
        
        // Initialize pagination buttons
        if (prevPageButton != null)
        {
            prevPageButton.onClick.AddListener(PreviousPage);
        }
        
        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(NextPage);
        }
        
        // Initialize empty state
        UpdateEmptyState(true);
        
        // Initialize loading state
        SetLoading(true);
    }
    
    #region NFT Data Handling
    
    /// <summary>
    /// Handle NFTs loaded event
    /// </summary>
    private void OnNFTsLoaded()
    {
        // Get all NFTs
        UpdateNFTList();
        SetLoading(false);
    }
    
    /// <summary>
    /// Handle NFT added event
    /// </summary>
    private void OnNFTAdded(NFTData nft)
    {
        // Check if NFT should be included in the current filter
        if (ShouldIncludeNFT(nft))
        {
            filteredNFTs.Add(nft);
            SortNFTs();
            RefreshUI();
        }
    }
    
    /// <summary>
    /// Handle NFT updated event
    /// </summary>
    private void OnNFTUpdated(NFTData nft)
    {
        // Update the display item if it exists
        if (displayItemsCache.TryGetValue(nft, out NFTDisplayItem displayItem))
        {
            displayItem.Setup(nft);
        }
        
        // Re-sort in case sort criteria changed
        SortNFTs();
        RefreshUI();
    }
    
    /// <summary>
    /// Handle NFT removed event
    /// </summary>
    private void OnNFTRemoved(TokenId tokenId)
    {
        // Find and remove the NFT from filtered list
        var nftToRemove = filteredNFTs.FirstOrDefault(n => n.Id.Equals(tokenId));
        if (nftToRemove != null)
        {
            filteredNFTs.Remove(nftToRemove);
            
            // Also remove from cache if it exists
            if (displayItemsCache.TryGetValue(nftToRemove, out NFTDisplayItem displayItem))
            {
                displayItemsCache.Remove(nftToRemove);
                
                // Destroy the display item if it's active
                if (displayItem != null && displayItem.gameObject != null)
                {
                    Destroy(displayItem.gameObject);
                }
            }
            
            RefreshUI();
        }
    }
    
    /// <summary>
    /// Update the current NFT list based on filtering and sorting
    /// </summary>
    private void UpdateNFTList()
    {
        if (nftManager == null) return;
        
        // Get all NFTs from manager
        currentNFTs = nftManager.AllNFTs.Values.ToList();
        
        // Apply filtering
        ApplyFilters();
        
        // Apply sorting
        SortNFTs();
        
        // Refresh UI
        RefreshUI();
    }
    
    /// <summary>
    /// Apply current filters to the NFT list
    /// </summary>
    private void ApplyFilters()
    {
        filteredNFTs.Clear();
        
        // Filter by type if specified
        foreach (var nft in currentNFTs)
        {
            if (ShouldIncludeNFT(nft))
            {
                filteredNFTs.Add(nft);
            }
        }
        
        // Update pagination
        UpdatePagination();
    }
    
    /// <summary>
    /// Determine if an NFT should be included based on current filters
    /// </summary>
    private bool ShouldIncludeNFT(NFTData nft)
    {
        // Type filter
        if (currentFilter != NFTType.Unknown && nft.NFTType != currentFilter)
        {
            return false;
        }
        
        // Search filter
        if (!string.IsNullOrEmpty(currentSearch))
        {
            string search = currentSearch.ToLower();
            if (!nft.Name.ToLower().Contains(search) && 
                !nft.Description.ToLower().Contains(search) &&
                !nft.Id.ToString().ToLower().Contains(search))
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Sort the filtered NFT list based on current sort option
    /// </summary>
    private void SortNFTs()
    {
        switch (currentSortOption)
        {
            case SortOption.NameAZ:
                filteredNFTs = sortAscending 
                    ? filteredNFTs.OrderBy(n => n.Name).ToList() 
                    : filteredNFTs.OrderByDescending(n => n.Name).ToList();
                break;
                
            case SortOption.NameZA:
                filteredNFTs = sortAscending 
                    ? filteredNFTs.OrderByDescending(n => n.Name).ToList() 
                    : filteredNFTs.OrderBy(n => n.Name).ToList();
                break;
                
            case SortOption.Newest:
                filteredNFTs = sortAscending 
                    ? filteredNFTs.OrderByDescending(n => n.DateAdded).ToList() 
                    : filteredNFTs.OrderBy(n => n.DateAdded).ToList();
                break;
                
            case SortOption.Oldest:
                filteredNFTs = sortAscending 
                    ? filteredNFTs.OrderBy(n => n.DateAdded).ToList() 
                    : filteredNFTs.OrderByDescending(n => n.DateAdded).ToList();
                break;
                
            case SortOption.LevelHighLow:
                filteredNFTs = sortAscending 
                    ? filteredNFTs.OrderByDescending(n => n.Level).ToList() 
                    : filteredNFTs.OrderBy(n => n.Level).ToList();
                break;
                
            case SortOption.LevelLowHigh:
                filteredNFTs = sortAscending 
                    ? filteredNFTs.OrderBy(n => n.Level).ToList() 
                    : filteredNFTs.OrderByDescending(n => n.Level).ToList();
                break;
        }
        
        // Reset to first page
        currentPage = 0;
        UpdatePagination();
    }
    #endregion
    
    #region UI Handling
    
    /// <summary>
    /// Clear and rebuild the display items for the current page
    /// </summary>
    private void RefreshUI()
    {
        if (contentContainer == null) return;
        
        // Clear existing items
        displayItems.Clear();
        foreach (Transform child in contentContainer)
        {
            child.gameObject.SetActive(false);
        }
        
        // Show empty state if no NFTs
        UpdateEmptyState(filteredNFTs.Count == 0);
        if (filteredNFTs.Count == 0) return;
        
        // Calculate page bounds
        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, filteredNFTs.Count);
        
        // Instantiate or reuse display items for current page
        for (int i = startIndex; i < endIndex; i++)
        {
            NFTData nft = filteredNFTs[i];
            NFTDisplayItem displayItem;
            
            // Reuse existing item if possible
            if (displayItemsCache.TryGetValue(nft, out displayItem))
            {
                if (displayItem != null && displayItem.gameObject != null)
                {
                    displayItem.gameObject.SetActive(true);
                    displayItem.Setup(nft); // Refresh data
                }
                else
                {
                    // Create new item if cached one is null or destroyed
                    CreateDisplayItem(nft, i - startIndex);
                }
            }
            else
            {
                // Create new item
                CreateDisplayItem(nft, i - startIndex);
            }
        }
        
        // Update pagination
        UpdatePaginationControls();
    }
    
    /// <summary>
    /// Create a display item for an NFT
    /// </summary>
    private void CreateDisplayItem(NFTData nft, int index)
    {
        GameObject itemObj = Instantiate(nftDisplayPrefab, contentContainer);
        NFTDisplayItem displayItem = itemObj.GetComponent<NFTDisplayItem>();
        
        if (displayItem != null)
        {
            // Setup the display item
            displayItem.Setup(nft);
            
            // Add event handlers
            displayItem.OnItemClicked += OnDisplayItemClicked;
            displayItem.OnUseRequested += OnDisplayItemUseRequested;
            displayItem.OnOpenRequested += OnDisplayItemOpenRequested;
            displayItem.OnUpgradeRequested += OnDisplayItemUpgradeRequested;
            
            // Add to tracking collections
            displayItems.Add(displayItem);
            displayItemsCache[nft] = displayItem;
        }
    }
    
    /// <summary>
    /// Update the empty state message
    /// </summary>
    private void UpdateEmptyState(bool isEmpty)
    {
        if (emptyStateMessage != null)
        {
            emptyStateMessage.SetActive(isEmpty);
        }
    }
    
    /// <summary>
    /// Set loading state
    /// </summary>
    private void SetLoading(bool isLoading)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(isLoading);
        }
    }
    
    /// <summary>
    /// Update pagination info and controls
    /// </summary>
    private void UpdatePagination()
    {
        totalPages = Mathf.CeilToInt((float)filteredNFTs.Count / itemsPerPage);
        
        // Clamp current page
        currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, totalPages - 1));
        
        UpdatePaginationControls();
    }
    
    /// <summary>
    /// Update pagination controls state
    /// </summary>
    private void UpdatePaginationControls()
    {
        // Update page info text
        if (pageInfoText != null)
        {
            if (totalPages > 0)
            {
                pageInfoText.text = $"Page {currentPage + 1} of {totalPages}";
            }
            else
            {
                pageInfoText.text = "No items";
            }
        }
        
        // Update button states
        if (prevPageButton != null)
        {
            prevPageButton.interactable = (currentPage > 0);
        }
        
        if (nextPageButton != null)
        {
            nextPageButton.interactable = (currentPage < totalPages - 1);
        }
    }
    
    /// <summary>
    /// Update the sort direction icon
    /// </summary>
    private void UpdateSortDirectionIcon()
    {
        if (sortDirectionIcon != null)
        {
            sortDirectionIcon.sprite = sortAscending ? ascendingSprite : descendingSprite;
        }
    }
    #endregion
    
    #region User Interaction Handlers
    
    /// <summary>
    /// Handle filter dropdown change
    /// </summary>
    private void OnFilterChanged(int index)
    {
        // Map dropdown index to NFT type
        switch (index)
        {
            case 0: // All
                currentFilter = NFTType.Unknown;
                break;
            case 1: // Avatars
                currentFilter = NFTType.Avatar;
                break;
            case 2: // Characters
                currentFilter = NFTType.Character;
                break;
            case 3: // Units
                currentFilter = NFTType.Unit;
                break;
            case 4: // Chests
                currentFilter = NFTType.Chest;
                break;
            case 5: // Trophies
                currentFilter = NFTType.Trophy;
                break;
        }
        
        // Update gallery title
        if (galleryTitleText != null)
        {
            galleryTitleText.text = index == 0 ? "All NFTs" : $"{currentFilter} Gallery";
        }
        
        // Apply the new filter
        ApplyFilters();
        RefreshUI();
    }
    
    /// <summary>
    /// Handle sort dropdown change
    /// </summary>
    private void OnSortChanged(int index)
    {
        // Map dropdown index to sort option
        currentSortOption = (SortOption)index;
        
        // Sort NFTs
        SortNFTs();
        RefreshUI();
    }
    
    /// <summary>
    /// Handle sort direction toggle
    /// </summary>
    private void ToggleSortDirection()
    {
        sortAscending = !sortAscending;
        UpdateSortDirectionIcon();
        
        // Re-sort
        SortNFTs();
        RefreshUI();
    }
    
    /// <summary>
    /// Handle search input change
    /// </summary>
    private void OnSearchChanged(string searchText)
    {
        // Debounce search to avoid too frequent updates
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
        }
        
        searchCoroutine = StartCoroutine(DebounceSearch(searchText));
    }
    
    private Coroutine searchCoroutine;
    
    /// <summary>
    /// Debounce search to avoid too frequent updates
    /// </summary>
    private IEnumerator DebounceSearch(string searchText)
    {
        yield return new WaitForSeconds(0.3f); // Wait for 300ms
        
        currentSearch = searchText;
        ApplyFilters();
        RefreshUI();
        
        searchCoroutine = null;
    }
    
    /// <summary>
    /// Navigate to previous page
    /// </summary>
    private void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshUI();
        }
    }
    
    /// <summary>
    /// Navigate to next page
    /// </summary>
    private void NextPage()
    {
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            RefreshUI();
        }
    }
    
    /// <summary>
    /// Handle when an NFT display item is clicked
    /// </summary>
    private void OnDisplayItemClicked(NFTDisplayItem item)
    {
        NFTData nft = item.GetNFTData();
        if (nft != null)
        {
            // Select the NFT
            foreach (var displayItem in displayItems)
            {
                displayItem.SetSelected(displayItem == item);
            }
            
            // Notify listeners
            OnNFTSelected?.Invoke(nft);
        }
    }
    
    /// <summary>
    /// Handle when an NFT display item's Use button is clicked
    /// </summary>
    private void OnDisplayItemUseRequested(NFTDisplayItem item)
    {
        NFTData nft = item.GetNFTData();
        if (nft != null)
        {
            if (nft.NFTType == NFTType.Avatar)
            {
                // Select avatar
                StartCoroutine(SelectAvatarAsync(nft, item));
            }
            else if (nft.NFTType == NFTType.Unit)
            {
                // Add to deck logic
                HandleUnitDeckOperation(nft, item);
            }
        }
    }
    
    /// <summary>
    /// Handle when an NFT display item's Open button is clicked
    /// </summary>
    private void OnDisplayItemOpenRequested(NFTDisplayItem item)
    {
        NFTData nft = item.GetNFTData();
        if (nft != null && nft.NFTType == NFTType.Chest)
        {
            // Open chest
            StartCoroutine(OpenChestAsync(nft, item));
        }
    }
    
    /// <summary>
    /// Handle when an NFT display item's Upgrade button is clicked
    /// </summary>
    private void OnDisplayItemUpgradeRequested(NFTDisplayItem item)
    {
        NFTData nft = item.GetNFTData();
        if (nft != null)
        {
            // Upgrade NFT
            StartCoroutine(UpgradeNFTAsync(nft, item));
        }
    }
    
    /// <summary>
    /// Select an avatar asynchronously
    /// </summary>
    private IEnumerator SelectAvatarAsync(NFTData nft, NFTDisplayItem item)
    {
        // Disable the button temporarily
        Button useButton = item.transform.GetComponentInChildren<Button>();
        if (useButton != null)
        {
            useButton.interactable = false;
        }
        
        // Select avatar
        var task = nftManager.SelectAvatar(nft.Id);
        
        // Wait for operation to complete
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        // Re-enable button
        if (useButton != null)
        {
            useButton.interactable = true;
        }
        
        // Update selected state
        if (task.Result)
        {
            foreach (var displayItem in displayItems)
            {
                displayItem.SetSelected(displayItem == item);
            }
        }
    }
    
    /// <summary>
    /// Open a chest asynchronously
    /// </summary>
    private IEnumerator OpenChestAsync(NFTData nft, NFTDisplayItem item)
    {
        // Disable the button temporarily
        Button openButton = item.transform.GetComponentInChildren<Button>();
        if (openButton != null)
        {
            openButton.interactable = false;
        }
        
        // Open chest
        var task = nftManager.OpenChest(nft.Id);
        
        // Wait for operation to complete
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        // No need to re-enable button as the chest will be removed on successful open
    }
    
    /// <summary>
    /// Upgrade an NFT asynchronously
    /// </summary>
    private IEnumerator UpgradeNFTAsync(NFTData nft, NFTDisplayItem item)
    {
        // Disable the button temporarily
        Button upgradeButton = item.transform.GetComponentInChildren<Button>();
        if (upgradeButton != null)
        {
            upgradeButton.interactable = false;
        }
        
        // Upgrade NFT
        var task = nftManager.UpgradeNFT(nft.Id);
        
        // Wait for operation to complete
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        // Re-enable button
        if (upgradeButton != null)
        {
            upgradeButton.interactable = true;
        }
    }
    
    /// <summary>
    /// Handle unit deck operations
    /// </summary>
    private void HandleUnitDeckOperation(NFTData nft, NFTDisplayItem item)
    {
        bool isInDeck = nft.IsInDeck;
        
        if (isInDeck)
        {
            // Remove from deck
            var newDeck = new List<NFTData>(nftManager.CurrentDeck);
            newDeck.Remove(nft);
            StartCoroutine(UpdateDeckAsync(newDeck, nft, item, false));
        }
        else
        {
            // Add to deck if not already full
            if (nftManager.CurrentDeck.Count < 5) // Assuming max deck size is 5
            {
                var newDeck = new List<NFTData>(nftManager.CurrentDeck);
                newDeck.Add(nft);
                StartCoroutine(UpdateDeckAsync(newDeck, nft, item, true));
            }
            else
            {
                // Show deck full message
                Debug.Log("Deck is full! Remove a unit first.");
                // You could show a UI message to the user here
            }
        }
    }
    
    /// <summary>
    /// Update deck asynchronously
    /// </summary>
    private IEnumerator UpdateDeckAsync(List<NFTData> newDeck, NFTData nft, NFTDisplayItem item, bool isAdding)
    {
        // Disable the button temporarily
        Button useButton = item.transform.GetComponentInChildren<Button>();
        if (useButton != null)
        {
            useButton.interactable = false;
        }
        
        // Update deck
        var task = nftManager.UpdateDeck(newDeck);
        
        // Wait for operation to complete
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        // Re-enable button
        if (useButton != null)
        {
            useButton.interactable = true;
            
            // Update button text
            TMP_Text buttonText = useButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = isAdding ? "Remove from Deck" : "Add to Deck";
            }
        }
        
        // Update in-deck visual state
        item.SetInDeck(isAdding);
    }
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Set filter to a specific NFT type
    /// </summary>
    public void SetTypeFilter(NFTType type)
    {
        currentFilter = type;
        
        // Update dropdown if it doesn't match
        if (filterDropdown != null)
        {
            int index = 0; // Default to "All"
            
            switch (type)
            {
                case NFTType.Unknown: index = 0; break; // All
                case NFTType.Avatar: index = 1; break;
                case NFTType.Character: index = 2; break;
                case NFTType.Unit: index = 3; break;
                case NFTType.Chest: index = 4; break;
                case NFTType.Trophy: index = 5; break;
            }
            
            filterDropdown.value = index;
        }
        else
        {
            // If no dropdown, apply filter directly
            ApplyFilters();
            RefreshUI();
        }
    }
    
    /// <summary>
    /// Refresh the gallery
    /// </summary>
    public void RefreshGallery()
    {
        UpdateNFTList();
    }
    #endregion
}

/// <summary>
/// Sort options for NFT gallery
/// </summary>
public enum SortOption
{
    NameAZ = 0,
    NameZA = 1,
    Newest = 2,
    Oldest = 3,
    LevelHighLow = 4,
    LevelLowHigh = 5
} 