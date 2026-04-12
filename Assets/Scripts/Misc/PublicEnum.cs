public enum NeedType
{
    Hunger,
    Thirst,
    Happiness,
    Cleanliness
}

public enum DailyTaskType
{
    Login = 0,
    FeedCat = 1,
    GiveWater = 2,
    PetCat = 3,
    PlayFeather = 4
}

public enum ShopCategory
{
    Room,
    Furniture
}

public enum FurnitureInteractionType
{
    None,
    SitOn,
    SleepOn,
    Scratch,
    PlayWith,
    HideIn
}

/// <summary>
/// Determines how furniture interacts with the isometric grid.
/// Normal: blocks tiles (cat can't walk, can't stack).
/// Rug: does NOT block tiles (cat walks over, other furniture can be placed on top).
/// Surface: blocks walking but allows other furniture to be placed on top (table, counter).
/// Wall: placed on room walls (col=0 for left wall, row=0 for right wall) with Y offset.
///       Blocks floor furniture on that tile but does NOT block cat walking.
/// Window: same as Wall, but also emits an isometric light source onto the floor.
/// </summary>
public enum FurniturePlacementType
{
    Normal,
    Rug,
    Surface,
    Wall,
    Window
}

public enum AchievementId
{
    FirstTouch,
    HungryKitty,
    HydrationStation,
    Playtime,
    CatWhisperer,
    MasterChef,
    Bartender,
    PlayBuddy,
    BondAcquaintance,
    BondFriend,
    BondCompanion,
    BondBestFriend,
    BondSoulmate,
    Day1,
    OneWeek,
    TwoWeeks,
    CleanKitty,
    InteriorDesigner,
    Homeowner,
    FullHouse,
    Completionist,
    Dedicated,
    FishingPro,
    GoldenCatch,
    Shopaholic
}
