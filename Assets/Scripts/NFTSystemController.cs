using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Main controller for the NFT system that integrates the manager, UI, and other components
/// </summary>
public class NFTSystemController : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] private NFTManager nftManager;
    [SerializeField] private GameObject nftManagerPrefab;
    
    [Header("UI Components")]
    [SerializeField] private NFTGalleryUI galleryUI;
    [SerializeField] private NFTMintingUI mintingUI;
    [SerializeField] private GameObject detailViewPanel;
    [SerializeField] private NFTDisplayItem detailDisplayItem;
    
    [Header("Navigation")]
    [SerializeField] private Button openGalleryButton;
    [SerializeField] private Button openMintingButton;
    [SerializeField] private Button closeGalleryButton;
    [SerializeField] private Button closeMintingButton;
    [SerializeField] private Button closeDetailButton;
    [SerializeField] private Button refreshButton;
    
    [Header("Category Buttons")]
    [SerializeField] private Button allNFTsButton;
    [SerializeField] private Button avatarsButton;
    [SerializeField] private Button unitsButton;
    [SerializeField] private Button chestsButton;
    [SerializeField] private Button trophiesButton;
    
    [Header("Loading")]
    [SerializeField] private GameObject initialLoadingPanel;
    [SerializeField] private float minLoadTime = 2f;
    
    private bool isInitialized = false;
    
    private void Awake()
    {
        // Ensure NFTManager exists
        EnsureNFTManager();
        
        // Setup UI events
        SetupButtonListeners();
    }
    
    private void Start()
    {
        // Start initialization
        StartCoroutine(InitializeSystem());
    }
    
    /// <summary>
    /// Make sure NFTManager exists in the scene
    /// </summary>
    private void EnsureNFTManager()
    {
        if (nftManager == null)
        {
            nftManager = FindObjectOfType<NFTManager>();
            
            if (nftManager == null && nftManagerPrefab != null)
            {
                var managerObj = Instantiate(nftManagerPrefab);
                nftManager = managerObj.GetComponent<NFTManager>();
                
                if (nftManager != null)
                {
                    Debug.Log("[NFTSystemController] Created NFTManager from prefab");
                }
            }
        }
    }
    
    /// <summary>
    /// Setup all button event listeners
    /// </summary>
    private void SetupButtonListeners()
    {
        // Navigation buttons
        if (openGalleryButton != null)
        {
            openGalleryButton.onClick.AddListener(OpenGallery);
        }
        
        if (openMintingButton != null)
        {
            openMintingButton.onClick.AddListener(OpenMinting);
        }
        
        if (closeGalleryButton != null)
        {
            closeGalleryButton.onClick.AddListener(CloseGallery);
        }
        
        if (closeMintingButton != null)
        {
            closeMintingButton.onClick.AddListener(CloseMinting);
        }
        
        if (closeDetailButton != null)
        {
            closeDetailButton.onClick.AddListener(CloseDetailView);
        }
        
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshNFTs);
        }
        
        // Category buttons
        if (allNFTsButton != null)
        {
            allNFTsButton.onClick.AddListener(() => FilterCategory(NFTType.Unknown));
        }
        
        if (avatarsButton != null)
        {
            avatarsButton.onClick.AddListener(() => FilterCategory(NFTType.Avatar));
        }
        
        if (unitsButton != null)
        {
            unitsButton.onClick.AddListener(() => FilterCategory(NFTType.Unit));
        }
        
        if (chestsButton != null)
        {
            chestsButton.onClick.AddListener(() => FilterCategory(NFTType.Chest));
        }
        
        if (trophiesButton != null)
        {
            trophiesButton.onClick.AddListener(() => FilterCategory(NFTType.Trophy));
        }
        
        // Connect UI components
        if (galleryUI != null)
        {
            galleryUI.OnNFTSelected += OnNFTSelected;
        }
        
        if (mintingUI != null)
        {
            mintingUI.OnNFTMinted += OnNFTMinted;
            mintingUI.OnViewGalleryRequested += OpenGallery;
        }
    }
    
    /// <summary>
    /// Initialize the NFT system with a loading screen
    /// </summary>
    private IEnumerator InitializeSystem()
    {
        // Show loading panel
        if (initialLoadingPanel != null)
        {
            initialLoadingPanel.SetActive(true);
        }
        
        // Start loading timer
        float startTime = Time.time;
        
        // Ensure NFTManager is ready
        if (nftManager != null)
        {
            // Wait for NFTManager to initialize if needed
            if (!nftManager.IsInitialized())
            {
                bool managerInitialized = false;
                System.Action onInitCallback = () => managerInitialized = true;
                
                // Subscribe to initialization event
                nftManager.OnInitialized += onInitCallback;
                
                // Wait for initialization
                while (!managerInitialized)
                {
                    yield return null;
                }
                
                // Clean up callback
                nftManager.OnInitialized -= onInitCallback;
            }
            
            // Wait for minimum load time for better UX
            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minLoadTime)
            {
                yield return new WaitForSeconds(minLoadTime - elapsedTime);
            }
        }
        else
        {
            Debug.LogError("[NFTSystemController] NFTManager not found. System will not function properly.");
            yield return new WaitForSeconds(minLoadTime);
        }
        
        // Hide loading panel
        if (initialLoadingPanel != null)
        {
            initialLoadingPanel.SetActive(false);
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Open the NFT gallery
    /// </summary>
    public void OpenGallery()
    {
        if (!isInitialized) return;
        
        if (galleryUI != null)
        {
            galleryUI.gameObject.SetActive(true);
            galleryUI.RefreshGallery();
        }
        
        if (mintingUI != null)
        {
            mintingUI.Hide();
        }
        
        if (detailViewPanel != null)
        {
            detailViewPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Close the NFT gallery
    /// </summary>
    public void CloseGallery()
    {
        if (galleryUI != null)
        {
            galleryUI.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Open the NFT minting UI
    /// </summary>
    public void OpenMinting()
    {
        if (!isInitialized) return;
        
        if (mintingUI != null)
        {
            mintingUI.Show();
        }
        
        if (galleryUI != null)
        {
            galleryUI.gameObject.SetActive(false);
        }
        
        if (detailViewPanel != null)
        {
            detailViewPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Close the NFT minting UI
    /// </summary>
    public void CloseMinting()
    {
        if (mintingUI != null)
        {
            mintingUI.Hide();
        }
    }
    
    /// <summary>
    /// Open the detail view for an NFT
    /// </summary>
    public void OpenDetailView(NFTData nft)
    {
        if (!isInitialized || nft == null) return;
        
        if (detailViewPanel != null && detailDisplayItem != null)
        {
            detailViewPanel.SetActive(true);
            detailDisplayItem.Setup(nft);
        }
    }
    
    /// <summary>
    /// Close the detail view
    /// </summary>
    public void CloseDetailView()
    {
        if (detailViewPanel != null)
        {
            detailViewPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Filter NFTs by category
    /// </summary>
    public void FilterCategory(NFTType type)
    {
        if (!isInitialized) return;
        
        if (galleryUI != null)
        {
            galleryUI.SetTypeFilter(type);
        }
    }
    
    /// <summary>
    /// Refresh NFTs from blockchain
    /// </summary>
    public async void RefreshNFTs()
    {
        if (!isInitialized || nftManager == null) return;
        
        // Show loading indicator
        // You could add a loading indicator here
        
        // Refresh NFTs
        await nftManager.RefreshNFTs();
        
        // Update UI
        if (galleryUI != null && galleryUI.gameObject.activeSelf)
        {
            galleryUI.RefreshGallery();
        }
    }
    
    /// <summary>
    /// Handle NFT selection in gallery
    /// </summary>
    private void OnNFTSelected(NFTData nft)
    {
        OpenDetailView(nft);
    }
    
    /// <summary>
    /// Handle newly minted NFT
    /// </summary>
    private void OnNFTMinted(NFTData nft)
    {
        // Refresh gallery if visible
        if (galleryUI != null && galleryUI.gameObject.activeSelf)
        {
            galleryUI.RefreshGallery();
        }
    }
} 