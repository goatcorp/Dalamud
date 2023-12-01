namespace Dalamud.Game.Inventory;

/// <summary>
/// Enum representing various player inventories.
/// </summary>
public enum GameInventoryType : ushort
{
    /// <summary>
    /// First panel of main player inventory.
    /// </summary>
    Inventory1 = 0,

    /// <summary>
    /// Second panel of main player inventory.
    /// </summary>
    Inventory2 = 1,

    /// <summary>
    /// Third panel of main player inventory.
    /// </summary>
    Inventory3 = 2,

    /// <summary>
    /// Fourth panel of main player inventory.
    /// </summary>
    Inventory4 = 3,

    /// <summary>
    /// Items that are currently equipped by the player.
    /// </summary>
    EquippedItems = 1000,

    /// <summary>
    /// Player currency container.
    /// ie, gil, serpent seals, sacks of nuts.
    /// </summary>
    Currency = 2000,

    /// <summary>
    /// Crystal container.
    /// </summary>
    Crystals = 2001,

    /// <summary>
    /// Mail container.
    /// </summary>
    Mail = 2003,

    /// <summary>
    /// Key item container.
    /// </summary>
    KeyItems = 2004,

    /// <summary>
    /// Quest item hand-in inventory.
    /// </summary>
    HandIn = 2005,

    /// <summary>
    /// DamagedGear container.
    /// </summary>
    DamagedGear = 2007,

    /// <summary>
    /// Examine window container.
    /// </summary>
    Examine = 2009,

    /// <summary>
    /// Doman Enclave Reconstruction Reclamation Box.
    /// </summary>
    ReconstructionBuyback = 2013,

    /// <summary>
    /// Armory off-hand weapon container.
    /// </summary>
    ArmoryOffHand = 3200,

    /// <summary>
    /// Armory head container.
    /// </summary>  
    ArmoryHead = 3201,

    /// <summary>
    /// Armory body container.
    /// </summary>   
    ArmoryBody = 3202,

    /// <summary>
    /// Armory hand/gloves container.
    /// </summary>  
    ArmoryHands = 3203,

    /// <summary>
    /// Armory waist container.
    /// <remarks>
    /// This container should be unused as belt items were removed from the game in Shadowbringers.
    /// </remarks>
    /// </summary>  
    ArmoryWaist = 3204,

    /// <summary>
    /// Armory legs/pants/skirt container.
    /// </summary>  
    ArmoryLegs = 3205,

    /// <summary>
    /// Armory feet/boots/shoes container.
    /// </summary>  
    ArmoryFeets = 3206,

    /// <summary>
    /// Armory earring container.
    /// </summary>  
    ArmoryEar = 3207,

    /// <summary>
    /// Armory necklace container.
    /// </summary>  
    ArmoryNeck = 3208,

    /// <summary>
    /// Armory bracelet container.
    /// </summary>  
    ArmoryWrist = 3209,

    /// <summary>
    /// Armory ring container.
    /// </summary>  
    ArmoryRings = 3300,

    /// <summary>
    /// Armory soul crystal container.
    /// </summary>  
    ArmorySoulCrystal = 3400,

    /// <summary>
    /// Armory main-hand weapon container.
    /// </summary>  
    ArmoryMainHand = 3500,

    /// <summary>
    /// First panel of saddelbag inventory.
    /// </summary>
    SaddleBag1 = 4000,

    /// <summary>
    /// Second panel of Saddlebag inventory.
    /// </summary>
    SaddleBag2 = 4001,

    /// <summary>
    /// First panel of premium saddlebag inventory.
    /// </summary>
    PremiumSaddleBag1 = 4100,

    /// <summary>
    /// Second panel of premium saddlebag inventory.
    /// </summary>
    PremiumSaddleBag2 = 4101,

    /// <summary>
    /// First panel of retainer inventory.
    /// </summary>
    RetainerPage1 = 10000,

    /// <summary>
    /// Second panel of retainer inventory.
    /// </summary>
    RetainerPage2 = 10001,

    /// <summary>
    /// Third panel of retainer inventory.
    /// </summary>
    RetainerPage3 = 10002,

    /// <summary>
    /// Fourth panel of retainer inventory.
    /// </summary>
    RetainerPage4 = 10003,

    /// <summary>
    /// Fifth panel of retainer inventory.
    /// </summary>
    RetainerPage5 = 10004,

    /// <summary>
    /// Sixth panel of retainer inventory.
    /// </summary>
    RetainerPage6 = 10005,

    /// <summary>
    /// Seventh panel of retainer inventory.
    /// </summary>
    RetainerPage7 = 10006,

    /// <summary>
    /// Retainer equipment container.
    /// </summary>
    RetainerEquippedItems = 11000,

    /// <summary>
    /// Retainer currency container.
    /// </summary>
    RetainerGil = 12000,

    /// <summary>
    /// Retainer crystal container.
    /// </summary>
    RetainerCrystals = 12001,

    /// <summary>
    /// Retainer market item container.
    /// </summary>
    RetainerMarket = 12002,

    /// <summary>
    /// First panel of Free Company inventory.
    /// </summary>
    FreeCompanyPage1 = 20000,

    /// <summary>
    /// Second panel of Free Company inventory.
    /// </summary>
    FreeCompanyPage2 = 20001,

    /// <summary>
    /// Third panel of Free Company inventory.
    /// </summary>
    FreeCompanyPage3 = 20002,

    /// <summary>
    /// Fourth panel of Free Company inventory.
    /// </summary>
    FreeCompanyPage4 = 20003,

    /// <summary>
    /// Fifth panel of Free Company inventory.
    /// </summary>
    FreeCompanyPage5 = 20004,

    /// <summary>
    /// Free Company currency container.
    /// </summary>
    FreeCompanyGil = 22000,

    /// <summary>
    /// Free Company crystal container.
    /// </summary>
    FreeCompanyCrystals = 22001,

    /// <summary>
    /// Housing exterior appearance container.
    /// </summary>
    HousingExteriorAppearance = 25000,

    /// <summary>
    /// Housing exterior placed items container.
    /// </summary>
    HousingExteriorPlacedItems = 25001,

    /// <summary>
    /// Housing interior appearance container.
    /// </summary>
    HousingInteriorAppearance = 25002,

    /// <summary>
    /// First panel of housing interior inventory.
    /// </summary>
    HousingInteriorPlacedItems1 = 25003,

    /// <summary>
    /// Second panel of housing interior inventory.
    /// </summary>
    HousingInteriorPlacedItems2 = 25004,

    /// <summary>
    /// Third panel of housing interior inventory.
    /// </summary>
    HousingInteriorPlacedItems3 = 25005,

    /// <summary>
    /// Fourth panel of housing interior inventory.
    /// </summary>
    HousingInteriorPlacedItems4 = 25006,

    /// <summary>
    /// Fifth panel of housing interior inventory.
    /// </summary>
    HousingInteriorPlacedItems5 = 25007,

    /// <summary>
    /// Sixth panel of housing interior inventory.
    /// </summary>
    HousingInteriorPlacedItems6 = 25008,

    /// <summary>
    /// Seventh panel of housing interior inventory.
    /// </summary>
    HousingInteriorPlacedItems7 = 25009,

    /// <summary>
    /// Eighth panel of housing interior inventory.
    /// </summary>
    HousingInteriorPlacedItems8 = 25010,

    /// <summary>
    /// Housing exterior storeroom inventory.
    /// </summary>
    HousingExteriorStoreroom = 27000,

    /// <summary>
    /// First panel of housing interior storeroom inventory.
    /// </summary>
    HousingInteriorStoreroom1 = 27001,

    /// <summary>
    /// Second panel of housing interior storeroom inventory.
    /// </summary>
    HousingInteriorStoreroom2 = 27002,

    /// <summary>
    /// Third panel of housing interior storeroom inventory.
    /// </summary>
    HousingInteriorStoreroom3 = 27003,

    /// <summary>
    /// Fourth panel of housing interior storeroom inventory.
    /// </summary>
    HousingInteriorStoreroom4 = 27004,

    /// <summary>
    /// Fifth panel of housing interior storeroom inventory.
    /// </summary>
    HousingInteriorStoreroom5 = 27005,

    /// <summary>
    /// Sixth panel of housing interior storeroom inventory.
    /// </summary>
    HousingInteriorStoreroom6 = 27006,

    /// <summary>
    /// Seventh panel of housing interior storeroom inventory.
    /// </summary>
    HousingInteriorStoreroom7 = 27007,

    /// <summary>
    /// Eighth panel of housing interior storeroom inventory.
    /// </summary>
    HousingInteriorStoreroom8 = 27008,

    /// <summary>
    /// An invalid value.
    /// </summary>
    Invalid = ushort.MaxValue,
}
