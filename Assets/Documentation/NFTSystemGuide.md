# NFT System Documentation

## Overview

This document provides comprehensive documentation for the NFT system implemented in the game. The system allows players to mint, view, and manage their NFTs within the game, with integration to the Internet Computer Protocol (ICP) blockchain.

## Architecture

The NFT system consists of several core components:

1. **NFTManager**: Central singleton that handles all NFT operations and maintains the collection of NFTs.
2. **NFTGalleryUI**: UI component for displaying and filtering NFTs in a gallery format.
3. **NFTMintingUI**: UI component for minting new NFTs with a premium user experience.
4. **NFTDisplayItem**: UI component for displaying individual NFTs with their details.
5. **NFTScriptableObject**: ScriptableObject representation of NFTs for use in the Unity Editor.
6. **NFTSystemController**: Controller that coordinates all NFT components and UI navigation.

## Setup Guide

### Required Components

1. **ICPService**: The NFT system depends on the ICPService to connect to the blockchain.
2. **UI Components**: Canvas with various UI panels and prefabs for the NFT display.

### Step-by-Step Setup

1. **Create a new scene or use an existing one**:
   - Ensure you have a Canvas setup for UI.

2. **Add the NFTManager**:
   - Create an empty GameObject and add the `NFTManager` component.
   - This component will be automatically set as a singleton, so only one should exist.
   - You can also create a prefab of it and assign to the NFTSystemController.

3. **Add the NFTSystemController**:
   - Create an empty GameObject and add the `NFTSystemController` component.
   - Assign references to all UI components and buttons.

4. **Setup the Gallery UI**:
   - Create a panel for the gallery UI with a scroll view for content.
   - Add the `NFTGalleryUI` component to this panel.
   - Setup filtering dropdown, search field, and pagination controls.
   - Create an NFT display item prefab and assign it to the gallery.

5. **Setup the Minting UI**:
   - Create a panel for the minting UI with input fields and buttons.
   - Add the `NFTMintingUI` component to this panel.
   - Setup template toggle group with options for different NFT types.

6. **Setup the Detail View**:
   - Create a panel for the detail view of an NFT.
   - Add an `NFTDisplayItem` component configured to show detailed information.

7. **Connect the ICPService**:
   - Ensure the ICPService is in the scene and properly configured.
   - The NFTManager will automatically find it if it's not explicitly assigned.

## Usage Guide

### Minting NFTs

1. **Open the Minting UI**: 
   - Click the "Mint NFT" button in your game UI.
   - The NFTMintingUI panel will appear.

2. **Select NFT Type**:
   - Choose the type of NFT you want to mint (Avatar, Unit, Chest, Trophy).
   - Each type has different properties and uses in the game.

3. **Fill in NFT Details**:
   - Provide a name, description, and image URL for your NFT.
   - You can use the "Random" button to generate random details.
   - Optionally, expand "Advanced Options" to set specific stats.

4. **Review Preview**:
   - Check how your NFT will look in the preview panel.
   - Use the "Refresh Preview" button if needed.

5. **Mint the NFT**:
   - Click the "Mint [NFT Type]" button.
   - Wait for the minting process to complete.
   - On success, you'll see a confirmation screen.

6. **Mint a Deck**:
   - To mint a starter deck of NFTs, use the "Mint Deck" button.
   - This will create a collection of NFTs in one operation.

### Viewing NFTs

1. **Open the Gallery**:
   - Click the "Gallery" button in your game UI.
   - The NFTGalleryUI panel will appear.

2. **Navigate and Filter**:
   - Use the filter dropdown to view specific types of NFTs.
   - Use the search field to find NFTs by name or description.
   - Use pagination controls to navigate through pages.

3. **Sort NFTs**:
   - Use the sort dropdown to order NFTs by name, date, or level.
   - Toggle ascending/descending sort.

4. **View NFT Details**:
   - Click on an NFT to see its detailed information.
   - The detail panel will show all properties, stats, and skills.

### Managing NFTs

1. **Select an Avatar**:
   - In the gallery, find an Avatar NFT.
   - Click the "Select" button to set it as your active avatar.

2. **Manage Deck**:
   - In the gallery, find Unit NFTs.
   - Click "Add to Deck" to include them in your game deck.
   - Click "Remove from Deck" to remove them.

3. **Open Chests**:
   - In the gallery, find Chest NFTs.
   - Click the "Open" button to reveal the contents.
   - New NFTs will be added to your collection.

4. **Upgrade NFTs**:
   - In the NFT detail view, click the "Upgrade" button.
   - This will increase the NFT's level and stats.

## ScriptableObjects for NFTs

The system includes ScriptableObject support for NFTs, allowing you to:

1. **Create NFTs in the Editor**:
   - Use `Assets > Create > NFT System > NFT` to create an NFT asset.
   - Fill in the details in the inspector.

2. **Use in Game Content**:
   - Reference NFT ScriptableObjects in other components.
   - Use them as templates for minting.

3. **Runtime Creation**:
   - The system can create ScriptableObjects at runtime from NFTData.
   - Use `nftManager.CreateScriptableObject(nftData)` to create one.

## Troubleshooting

### Common Issues

1. **NFTs not loading**:
   - Check if ICPService is properly initialized
   - Check console for connection errors
   - Verify principal ID is correct

2. **Minting fails**:
   - Check connection to blockchain
   - Ensure image URL is valid and accessible
   - Check for error messages in console

3. **UI components not working**:
   - Verify all references are assigned in inspector
   - Check for missing prefabs
   - Ensure Canvas is setup correctly

### Debug Logging

The system includes extensive logging. Look for log messages with these prefixes:
- `[NFTManager]`: Core NFT operations
- `[NFTGalleryUI]`: Gallery UI operations
- `[NFTMintingUI]`: Minting operations
- `[NFTSystemController]`: System coordination

## Extending the System

### Adding New NFT Types

1. Add a new entry to the `NFTType` enum in `NFTManager.cs`.
2. Update the `DetermineNFTType` method to handle the new type.
3. Add a method to mint the new type in `NFTManager.cs`.
4. Add a UI template in `NFTMintingUI`.
5. Update filtering in `NFTGalleryUI`.

### Customizing the UI

1. The NFT display item can be customized by creating a new prefab.
2. The gallery layout can be modified by changing the grid layout or scroll rect.
3. Custom UI themes can be applied to all panels.

### Adding New NFT Properties

1. Extend the `NFTData` class with new properties.
2. Update `NFTScriptableObject` to include the new properties.
3. Modify `NFTDisplayItem` to show the new properties.
4. Update the minting UI to allow setting these properties.

## Performance Considerations

- **Pagination**: The gallery uses pagination to handle large collections efficiently.
- **Object Pooling**: Display items are reused to minimize instantiation.
- **Caching**: NFT data and textures are cached to reduce blockchain queries and texture loads.
- **Async Operations**: All blockchain operations are performed asynchronously to avoid freezing the UI.

## Best Practices

1. Always check if operations completed successfully before updating the UI.
2. Use events to communicate between components rather than direct references.
3. Cache NFT data when possible to reduce blockchain queries.
4. Provide clear feedback to users during long operations.
5. Handle errors gracefully with informative messages.

## API Reference

See the XML documentation in each script for detailed API references:

- `NFTManager.cs`: Core NFT operations
- `NFTGalleryUI.cs`: Gallery display
- `NFTMintingUI.cs`: NFT creation
- `NFTDisplayItem.cs`: Individual NFT display
- `NFTScriptableObject.cs`: Editor integration
- `NFTSystemController.cs`: System coordination

## Technical Support

For technical issues or questions about the NFT system, please contact the development team. 