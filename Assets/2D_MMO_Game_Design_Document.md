# Infinite Realms: 2D Procedural MMO
## Game Design Document

### Table of Contents

1. [Game Overview](#1-game-overview)
2. [Core Gameplay](#2-core-gameplay)
3. [World Design](#3-world-design)
4. [Zone & Portal System](#4-zone--portal-system)
5. [NPC Systems](#5-npc-systems)
6. [Quest Systems](#6-quest-systems)
7. [Procedural Generation](#7-procedural-generation)
8. [Technical Implementation](#8-technical-implementation)
9. [Netick Integration](#9-netick-integration)
10. [Performance Optimization](#10-performance-optimization)
11. [Launch Strategy](#11-launch-strategy)
12. [Netick Implementation Details](#12-netick-implementation-details)
13. [Procedural Generation Implementation](#13-procedural-generation-implementation)
14. [Portal & Boundary System Implementation](#14-portal--boundary-system-implementation)
15. [NPC & Quest Implementation](#15-npc--quest-implementation)

---

## 1. Game Overview

**Infinite Realms** is a 2D top-down procedurally generated MMORPG where players explore an ever-expanding world, complete quests, fight enemies, and collect loot. The game features infinite progression, procedurally generated content, and a social multiplayer experience.

### Key Features

- Top-down 2D world with retro-inspired visuals
- Seamless multiplayer using Netick networking
- Procedural content generation (zones, mobs, items, quests)
- Portal system for zone transitions
- NPC interaction and reputation system
- Infinite progression systems
- Memory-efficient object pooling

### Target Audience
- Casual MMORPG players
- Fans of procedural games
- Players looking for quick-session gameplay
- Retro game enthusiasts

### Platform
- PC Web (Crazy Games platform)
- Potential for desktop standalone version

---

## 2. Core Gameplay

### Player Experience
Players control their character from a top-down perspective, navigating through different zones, interacting with NPCs, fighting enemies, and completing quests. The game emphasizes exploration, combat, and collection.

### Character System
- Characters have basic attributes (health, attack, defense)
- Infinite leveling system with gradually increasing stats
- Equipment slots for weapons, armor, and accessories
- Character customization options

### Combat System
- Real-time combat with basic attacks and abilities
- Cooldown-based skills
- Auto-targeting for nearest enemies
- Visual feedback for damage and effects

### Progression System
- Experience points from defeating enemies and completing quests
- Level-ups provide stat increases and new abilities
- Gear progression through crafting and drops
- Reputation levels with various NPC factions

---

## 3. World Design

### Visual Style
- 2D pixel art with vibrant colors
- Distinct visual themes for different zone types
- Atmospheric lighting and particle effects
- Minimalist UI for maximum game visibility

### Home Zone (Lobby)
- Central hub for players to gather
- Safe zone with no enemies
- Contains main service NPCs (trainers, traders, quest givers)
- Houses all portals to adventure zones
- Communal areas for player interaction

### Adventure Zones
- Procedurally generated areas with themes:
  - Forests, caves, deserts, swamps, mountains
  - Ruins, castles, villages, mines
- Each zone has difficulty tiers
- Increasing rewards and challenges as players venture deeper
- Dynamic weather and time system

### World Boundaries
- Invisible barriers at zone edges
- Players approaching boundaries trigger elite "Boundary Guardian" enemies
- Guardians increase in power the longer player remains out-of-bounds
- Visual warning effects when approaching boundaries

---

## 4. Zone & Portal System

### Portal Design
- Visually distinct portal objects placed throughout the world
- Portal animations when activated
- Portal state indication (active/inactive)
- Information display of destination when approached

### Portal Mechanics
- Interact to activate
- Brief loading transition when traveling
- Cooldown between uses to prevent portal hopping
- Possible portal keys for special zones

### Zone Management
- Zones are instanced when first player enters
- Empty zones unloaded after all players leave (with timer)
- Zone difficulty scaling based on player levels
- Zone "hot spots" with increased enemy density and rewards

### Zone Discovery
- Players must discover new portal locations
- Portal maps can be purchased from NPCs
- Achievement system for zone exploration
- Special events can temporarily open new portals

---

## 5. NPC Systems

### NPC Types
- **Vendors**: Buy/sell items, repair equipment
- **Quest Givers**: Provide main and side quests
- **Trainers**: Teach new skills and abilities
- **Enemies**: Hostile mobs with different behaviors
- **Neutrals**: Ambient life and background characters
- **Elite**: Special boss-type enemies with unique drops

### Interaction System
- Dialog system with multiple response options
- Simple trading interface
- Quest acceptance and turn-in
- Reputation influence based on choices

### Reputation System
- Multiple factions with independent reputation meters
- Reputation levels: Hostile, Unfriendly, Neutral, Friendly, Honored, Exalted
- Reputation-gated content (quests, items, zones)
- Reputation gain/loss through quests and combat

### NPC AI
- Basic pathfinding for movement
- Combat routines with different behaviors
- Idle animations and ambient movement
- Day/night cycle affecting NPC behavior

---

## 6. Quest Systems

### Quest Types
- **Main Quests**: Story-driven progression quests
- **Side Quests**: Optional content with rewards
- **Daily Quests**: Repeatable quests that reset daily
- **Event Quests**: Limited-time quests during special events
- **Hidden Quests**: Discovered through exploration

### Quest Design
- Clear objectives with tracking
- Multiple stages for complex quests
- Varied tasks (combat, collection, exploration, escort)
- Meaningful rewards scaling with difficulty

### Quest Tracking
- Quest log interface
- Objective markers on minimap
- Progress indicators for multi-stage quests
- Visual effects for quest-related objects

### Events System
- Periodic world events that spawn special content
- Server-wide announcements for events
- Unique rewards for event participation
- Seasonal events with themed content

---

## 7. Procedural Generation

### Content Generation
- **Zones**: Layout, terrain features, obstacles
- **Mobs**: Appearance, stats, abilities, spawn locations
- **Items**: Attributes, stats, visual effects
- **Quests**: Objectives, locations, reward scaling

### Generation Rules
- Balanced difficulty curve
- Visual coherence within themes
- Logical placement of elements
- Performance-conscious generation

### Seeding System
- Consistent generation for shared experiences
- Unique identifiers for interesting generations
- Server-based seed management for synchronization

### Content Validation
- Automated checks for playability
- Ensure paths are navigable
- Prevent impossible quest objectives
- Balance resource distribution

### Procedural Zone Generator
- **Seed**: Unique identifier for generation consistency
- **Random**: System for generating varied content
- **GenerateZone**: Method for creating zone layouts

#### GenerateZone Method
- **Parameters**: Zone ID and difficulty level
- **Implementation**: Simple grid-based generation for prototype
- **Purpose**: Place enemies, resources, and portals

## 8. Technical Implementation

### Unity Architecture
- Scene-based structure with additive loading
- Component-based design
- Scriptable objects for data management
- Event-driven communication

### Object Pooling
- Centralized pool manager
- Type-based pools for different game elements
- Runtime expansion of pools as needed
- Performance monitoring and adjustment

### Persistence
- Player data stored on ICP backend
- Local caching for performance
- Periodic sync for data security
- Offline progress capabilities

### Client-Server Model
- Server authoritative for game logic
- Client prediction for responsive gameplay
- Delta compression for network updates
- Fallback systems for connection issues

---

## 9. Netick Integration

### Network Objects
- Player characters as NetworkObjects
- NPCs and enemies as NetworkObjects
- Interactive elements (portals, chests) as NetworkObjects
- Optimized NetworkBehaviours for game elements

### Network State Management
- Networked properties for game state
- Change callbacks for state updates
- RPCs for important events
- Input handling for player actions

### Zone Management
- Server-managed zone instances
- Network object parenting for zone organization
- Object pooling integrated with network lifecycle
- Area of Interest for network optimization

### Network Events
- Player connections/disconnections
- Zone transitions
- Combat events
- Quest updates
- Item transactions

---

## 10. Performance Optimization

### Network Optimization
- Interest management to limit updates
- Relevancy system for distant objects
- State compression for bandwidth reduction
- Priority-based update scheduling

### Memory Management
- Strict object pooling for all dynamic elements
- Texture atlasing for sprites
- Asset bundle loading for on-demand content
- Memory budgets for different systems

### CPU Optimization
- Job system for parallel processing
- LOD system for distant objects
- Batching for rendering optimization
- Efficient pathfinding with spatial indexing

### Scalability
- Player count targets: 
  - Minimum: 50 players per server
  - Target: 100+ players per server
  - Stretch goal: 200+ players per server
- Dynamic server allocation based on population
- Server region distribution for latency reduction

---

## 11. Launch Strategy

### Development Phases
1. **Prototype**: Core gameplay, networking, one zone
2. **Alpha**: Basic procedural systems, multiple zones
3. **Beta**: Complete quest system, events, full progression
4. **Launch**: Polish, optimization, content expansion

### Minimal Viable Product
- Player movement and basic combat
- One home zone and two adventure zones
- Simplified quest system
- Basic item and progression system
- Functional portals and boundaries

### Crazy Games Launch
- Web-optimized build
- Tutorial integration
- Screenshot and video showcase
- Player retention hooks
- Analytics integration

### Post-Launch Support
- Weekly content updates
- Monthly feature additions
- Community feedback integration
- Performance monitoring and optimization
- Seasonal events calendar

---

## 12. Netick Implementation Details

### Netick Architecture Overview
Based on our comprehensive study of the Netick networking framework, we'll implement the following architecture for our 2D MMO:

#### Network Initialization
- **NetworkSandbox**: Central multiplayer controller
  - Server initialization with appropriate tick rate (30-60Hz)
  - Client connection handling with reconnection support
  - Network event registration for core game systems
  - Transport configuration for web-based gameplay

#### Networked Object Hierarchy
- **Player Character Implementation**
  - NetworkObject with NetworkBehaviours for:
    - PlayerMovement (input handling with client prediction)
    - PlayerCombat (attack synchronization)
    - PlayerInventory (item management)
    - PlayerQuest (quest state tracking)
    - PlayerStats (attributes and level progression)

- **Zone Management**
  - ZoneManager as NetworkBehaviour
    - Tracks all active zones
    - Manages zone transitions
    - Controls player zone assignments
    - Handles zone instantiation and destruction

- **NPC Implementation**
  - NPCController as NetworkBehaviour
    - State machine for AI behavior
    - Interaction handlers for player engagement
    - Combat routines for enemies
    - Movement patterns with pathfinding

- **Portal System**
  - PortalController as NetworkBehaviour
    - Portal state tracking (active/inactive)
    - Player transition handling
    - Zone loading coordination
    - Teleportation effects

#### Network State Synchronization
- **Change Callbacks**
  - Player state changes ([OnChanged] for health, stats, inventory)
  - NPC state changes (aggro state, health, behavior mode)
  - World state changes (time, weather, events)
  - Zone state changes (enemy density, resource availability)

- **Remote Procedure Calls (RPCs)**
  - Critical game events (player death, boss spawns)
  - One-time interactions (dialog, quest completion)
  - Visual effects that don't need state persistence
  - Server-to-client notifications

- **Network State**
  - All persistent game elements use [Networked] properties
  - NetworkDictionaries for collections of game objects
  - NetworkCollections for dynamic lists (active quests, inventory)
  - Efficient serialization of game state

#### Object Lifecycle Management
- **Object Instantiation**
  - Prefab pools for all NetworkObjects
  - Server-controlled spawning with spawn prediction where appropriate
  - Recycling of destroyed objects back to pools
  - Zone-based object management

- **Object Parenting**
  - Hierarchical organization of objects by zone
  - Formation management for grouped units
  - Container entities for logical grouping
  - Proper parent-child relationship management

### Netick Optimization Strategies
- **Area of Interest Management**
  - Spatial grid-based relevancy
  - Distance-based update frequency
  - Zone-based filtering of updates
  - Priority for player-adjacent objects

- **Input Handling**
  - Input prediction on client
  - Server validation of all actions
  - Rollback and resimulation when needed
  - Input buffering for smooth experience

- **State Compression**
  - Delta compression for network state
  - Quantization of physics values
  - Bitpacking for small value types
  - Priority-based update sequencing

- **Network Object Pooling**
  - Pre-initialized pools for common objects
  - Runtime expansion of pools as needed
  - Proper NetworkObject recycling
  - Pool size optimization based on usage metrics

### Netick Event Utilization
- **Connection Events**
  - OnPlayerJoined: Initialize player in home zone
  - OnPlayerLeft: Clean up player resources
  - OnConnected: Synchronize full game state
  - OnDisconnected: Handle graceful disconnection

- **Game Events**
  - Player zone transitions
  - Combat initiation and completion
  - Quest stage advancement
  - Item acquisition and usage

- **System Events**
  - Performance metrics monitoring
  - Error handling and recovery
  - Server state management
  - Load balancing decisions

### Script Execution Order
- **Early Update Systems**
  - Input collection
  - Network state reception
  - World manager update
  
- **Mid Update Systems**
  - Player controllers
  - NPC AI systems
  - Combat resolution
  
- **Late Update Systems**
  - Visual effects
  - UI updates
  - Camera follow systems

## Implementation Priorities

1. Core networking and player movement
2. Zone and portal system
3. Basic combat and enemy AI
4. Procedural generation framework
5. NPC and interaction systems
6. Quest tracking and events
7. Item and progression systems
8. Performance optimization
9. UI and polish
10. Launch preparations 

---

## 13. Procedural Generation Implementation

### Procedural System Architecture
Our procedural generation system is designed to create infinite, varied content while maintaining performance and network efficiency.

#### Deterministic Generation
- **Seeded Generation**
  - Server provides generation seeds for consistency
  - Deterministic algorithms ensure all clients see identical content
  - Seed catalog for recreating interesting generation results
  - Version-controlled generation algorithms to prevent desynchronization

- **Layered Generation Process**
  - Base layer: Terrain and walkable areas
  - Feature layer: Obstacles, paths, and landmarks
  - Decoration layer: Visual details and ambiance
  - Gameplay layer: Enemies, resources, and interactive elements

#### Zone Generation
- **Procedural Zone Layout**
  - Grid-based cellular automata for natural-feeling terrain
  - Weighted distribution of features based on zone type
  - Path generation ensuring traversability
  - Room-based designs for indoor areas
  - Perlin noise for terrain height and feature distribution

- **Zone Persistence**
  - Zones regenerate with same seed for consistency
  - Key locations remain constant between visits
  - Progressive changes tracked as delta from base generation
  - Resource depletion and respawn cycles

#### Entity Generation
- **NPC Generation**
  - Attribute scaling based on zone difficulty
  - Behavior patterns selected from templates
  - Visual customization from component parts
  - Faction and group assignment
  - Special abilities based on NPC tier

- **Item Generation**
  - Base item templates with modifiable properties
  - Attribute scaling with player progression
  - Rarity tiers affecting attribute ranges
  - Visual customization based on attributes
  - Set items with synergistic properties

#### Quest Generation
- **Procedural Quest Framework**
  - Template-based quest structure
  - Dynamic objective locations
  - Scaling difficulty and rewards
  - Context-aware objective selection
  - Multi-stage quest chains

- **Objective Types**
  - Kill X enemies of type Y
  - Collect X items from source Y
  - Interact with X objects
  - Escort NPC to location
  - Defend location for time period
  - Discover and explore new area

### Generation Balancing
- **Difficulty Scaling**
  - Progressive challenge increase with player level
  - Group size factored into enemy strength
  - Adaptive difficulty based on player performance
  - Challenge rating system for content

- **Reward Scaling**
  - Loot tables weighted by challenge rating
  - Experience curves balanced for steady progression
  - Diminishing returns for farming same content
  - Bonus rewards for first-time completion

- **Content Density**
  - Appropriate enemy-to-space ratio
  - Resource distribution balanced for economy
  - Interactive object placement for engagement
  - Visual element density for aesthetic appeal

### Technical Implementation
- **Generation Pipeline**
  - Multi-threaded generation process
  - Pre-generation of adjacent zones
  - LOD system for distant areas
  - Chunked loading for seamless exploration

- **Memory Efficiency**
  - Minimal storage of generation data
  - Procedural reconstruction from seeds
  - Delta storage for player modifications
  - Compression of persistent state

- **Network Considerations**
  - Minimal seed transfer instead of full content
  - Server validation of critical generation
  - Local generation of visual-only elements
  - Prioritized synchronization of gameplay elements 

## 14. Portal & Boundary System Implementation

### Portal System Architecture
The portal system serves as the backbone of our zone-based world design, allowing seamless travel between different procedurally generated areas while maintaining network performance.

#### Portal Types
- **Standard Portals**
  - Connect commonly traversed zones
  - Always active and accessible
  - Visual indication of destination
  - No special requirements to use

- **Discovery Portals**
  - Initially inactive until discovered by players
  - Require specific interaction to activate
  - Added to player's discovered portal list
  - May close after certain conditions

- **Event Portals**
  - Temporary portals for special events
  - Time-limited availability
  - Distinct visual style for urgency
  - Lead to special event zones

- **Elite Portals**
  - Lead to high-difficulty zones
  - Require level/reputation/item requirements
  - Significant rewards in destination
  - Cooldown or limited use restrictions

#### Portal Mechanics
- **Implementation Details**
  - Portal as NetworkObject with NetworkBehaviour
  - Server authoritative activation state
  - Client-side visual effects
  - Collision trigger for interaction

- **Transition Process**
  ```
  1. Player enters portal collision trigger
  2. Client sends portal interaction request
  3. Server validates request (level, items, etc.)
  4. If valid, server prepares destination zone
  5. Server sends zone transition command to client
  6. Client shows loading animation
  7. Server moves player NetworkObject to new zone
  8. Server sends new zone state to client
  9. Client completes loading and shows new zone
  ```

- **Portal Networking**
  - Minimal state synchronization for performance
  - [Networked] properties:
    - PortalState (Active/Inactive)
    - DestinationZoneID
    - RequirementsMet (per player)
    - CooldownRemaining
  - RPCs for visual effects and special interactions

#### Zone Transition Handling
- **Zone Loading**
  - Asynchronous zone generation/loading
  - Preloading of frequently used zones
  - Memory management during transitions
  - Connection stabilization during transition

- **Player State Preservation**
  - Complete state transfer between zones
  - Continuous NetworkObject identity
  - Preservation of all player properties
  - Group/party transition coordination

### Boundary System Implementation
The boundary system creates natural limits to exploration while adding gameplay elements rather than hard barriers.

#### Boundary Detection
- **Implementation**
  - Zone-specific boundary polygons
  - Player position checking against boundaries
  - Warning indicators as players approach edges
  - Progressive danger increase near boundaries

- **Networked Components**
  - Server-authoritative boundary verification
  - Client-side prediction for responsive UI
  - Boundary warning as [Networked] property
  - Boundary breach event handling

#### Guardian Enemy System
- **Guardian Types**
  - Scaled to player level + additional difficulty
  - Zone-appropriate themes with enhanced abilities
  - Visual indicators of boundary guardian status
  - Progressive power increase while player remains out-of-bounds

- **Guardian Behavior**
  - Aggressive pursuit of boundary-crossing players
  - Abilities that inhibit movement/escape
  - Return to boundary when player retreats
  - Despawn after player returns to safe zone

- **Implementation**
  - Special enemy NetworkObject pool
  - Server-initiated spawn on boundary crossing
  - Enhanced AI behavior component
  - Priority network updates for responsive combat

#### Visual Boundary Feedback
- **Client-Side Effects**
  - Screen edge effects indicating boundary proximity
  - Directional indicators showing safe direction
  - Warning sounds and messages
  - Environment changes near boundaries (fog, color shift)

- **Environment Transitions**
  - Gradual visual changes approaching boundaries
  - Weather effects intensification
  - Ominous audio cues
  - Lighting and particle effects

### Zone Connectivity
- **World Map Design**
  - Logical connections between zone types
  - Fast-travel hub in home zone
  - Progression-based zone unlocking
  - Visualized world map for navigation

- **Portal Placement Logic**
  - Strategic positioning for exploration flow
  - Difficulty-based distribution
  - Secret/hidden portal locations
  - Return portals for convenience

- **Cross-Zone Mechanics**
  - Events spanning multiple zones
  - Resources/crafting requiring multi-zone travel
  - Reputation effects crossing zone boundaries
  - Environmental effects propagating across connected zones 

## 15. NPC & Quest Implementation

### NPC System Architecture
Our NPC system creates a living world populated with interactive characters that provide quests, services, and combat challenges, all synchronized across the network.

#### NPC Core Systems
- **NPC Base Framework**
  - NetworkObject with specialized NetworkBehaviours
  - Common attributes across all NPC types:
    - [Networked] Health, Level, Faction
    - [Networked] InteractionState
    - [Networked] Position, Rotation, Animation state
  - Type-specific behaviors through component composition

- **NPC Categories Implementation**
  ```
  - BaseBPC (NetworkBehaviour)
    ├─ VendorNPC
    |   ├─ ShopInventory (NetworkDictionary<ItemID, ItemData>)
    |   └─ TransactionHandler
    ├─ QuestGiverNPC
    |   ├─ QuestInventory (NetworkCollection<QuestData>)
    |   └─ QuestStateManager
    ├─ CombatNPC
    |   ├─ CombatBehavior
    |   ├─ AIController
    |   └─ LootTable
    └─ AmbientNPC
        ├─ MovementPattern
        └─ AmbientBehavior
  ```

- **Interaction System**
  - Detection radius for player approach
  - Interaction prompt display logic
  - Multi-option interaction menu
  - Dialog system integration
  - Server-validated interactions

#### NPC AI Implementation
- **Behavior State Machine**
  - Server-authoritative state transitions
  - Client-side animation prediction
  - States synchronized via [Networked] properties
  - Common states: Idle, Patrol, Interact, Combat, Flee

- **Pathfinding Integration**
  - Grid-based A* pathfinding
  - Path smoothing for natural movement
  - Obstacle avoidance logic
  - Movement synchronized via position updates
  - Path recalculation triggers

- **Combat AI**
  - Aggro management system
  - Ability selection logic
  - Target prioritization
  - Tactical positioning
  - Group behavior coordination
  - Difficulty scaling with player level

#### NPC Networking
- **Optimization Strategies**
  - Low update frequency for distant NPCs
  - Higher precision for interactive NPCs
  - Animation state synchronization
  - Relevancy-based updates

- **State Synchronization**
  - [Networked] properties for persistent state
  - [OnChanged] callbacks for state transitions
  - RPCs for one-time events and animations
  - Serialized AI decision making

### Quest System Implementation
Our quest system provides procedurally generated and hand-crafted missions that scale with player progression and integrate with the networked game world.

#### Quest Data Structure
- **Quest Components**
  - QuestData (ScriptableObject template)
    - Basic information (title, description, level)
    - Prerequisite quests
    - Faction requirements
    - Reward definitions
  - QuestObjective (Collection of objectives)
    - Objective type
    - Target entities
    - Required counts
    - Location information
  - QuestState (Player-specific quest progress)
    - [Networked] Completion state
    - [Networked] Objective progress
    - [Networked] Time remaining (timed quests)

- **Quest Storage**
  - Server-side master quest database
  - Player-specific active quest collection
  - Completed quest history
  - Quest dependency graph

#### Quest Progression System
- **Objective Tracking**
  - Event-based progress updates
  - Zone-specific objective listeners
  - Multi-player quest sharing
  - Progress persistence across sessions

- **Quest State Synchronization**
  - [Networked] quest collection per player
  - Efficient delta updates for progress
  - RPC calls for milestone completions
  - Client-side objective display

- **Reward Distribution**
  - Server-validated completion
  - Atomically processed rewards
  - Item generation for quest rewards
  - Experience and reputation calculation

#### Procedural Quest Generation
- **Template-Based Generation**
  - Quest templates with variable components
  - Context-aware objective placement
  - Difficulty scaling based on player level
  - Reward scaling based on challenge

- **Quest Chains**
  - Procedurally chained objectives
  - Story element integration
  - Branching quest paths
  - Consequences from previous choices

- **Quest Variety**
  - Kill quests with varied enemy types
  - Collection quests with item generation
  - Exploration quests with discovery points
  - Defense quests with time-based challenges
  - Escort quests with NPC pathing
  - Boss quests with special enemy spawning

### Reputation System
- **Faction Framework**
  - [Networked] FactionData per player
  - Multiple independent faction reputations
  - Level-based reputation thresholds
  - Cross-faction reputation effects

- **Reputation Effects**
  - NPC dialog and pricing changes
  - Access to faction-specific quests
  - Hostile/friendly NPC reactions
  - Zone access permissions
  - Special item availability

- **Reputation Gain Mechanics**
  - Quest completion reputation rewards
  - Enemy defeat reputation changes
  - Item turn-in reputation boosts
  - Repeatable reputation activities
  - Decay prevention mechanics

### Event Integration
- **World Event System**
  - Server-scheduled events
  - Zone-specific event spawning
  - Player participation tracking
  - Dynamically generated objectives

- **Event Quest Generation**
  - Special event-only quest templates
  - Time-limited availability
  - Enhanced rewards
  - Multi-stage event progression
  - Server-wide coordination 

// Create this first - the foundation of everything
public class ZoneManager : NetworkBehaviour
{
    [Networked] private NetworkDictionary<int, ZoneData> _activeZones;
    private Dictionary<int, ProceduralZoneGenerator> _zoneGenerators = new();
    
    // Generate zones based on player's location and seed
    public void GenerateOrLoadZone(int zoneId, Vector2 playerPosition)
    {
        // Use existing spawn points for first demo
        // Later expand to procedural
    }
} 

// Simple portal prefab
public class Portal : NetworkBehaviour
{
    [Networked] public int DestinationZoneId { get; set; }
    [Networked] public NetworkBool IsActive { get; set; }
    
    // OnTriggerEnter2D to teleport player
} 

// Start with simple deterministic generation
public class ProceduralZoneGenerator
{
    public int Seed { get; private set; }
    private System.Random _random;
    
    // Generate zone layout based on seed
    public ZoneLayout GenerateZone(int zoneId, int difficulty)
    {
        // Simple grid-based generation for prototype
        // Place enemies, resources, and portals
    }
} 

// Implement boundary detection and guardian spawning
public class ZoneBoundary : NetworkBehaviour
{
    [Networked] private NetworkBool _playerOutOfBounds { get; set; }
    
    // Check player position against boundary
    // Spawn guardian when crossed
} 