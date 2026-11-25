namespace QaCommerceLab.Tests.Data;

/// <summary>A catalog product as the shop's business rules describe it. Prices in USD cents.</summary>
public sealed record ProductInfo(
    string Id,
    string Name,
    string Subcategory,
    int Price,
    int? CompareAtPrice = null,
    bool FreeShipping = false);

/// <summary>
/// Products the scenarios refer to, from qa-commerce-lab docs/BUSINESS.md. Feature files name products
/// the way a customer sees them; <see cref="ByName"/> turns that name into the expected data.
/// </summary>
public static class Catalog
{
    public static readonly ProductInfo ArcHeadphones = new("p1", "Arc Wireless Headphones", "Audio", 12_900);
    public static readonly ProductInfo OrbitLamp = new("p2", "Orbit Desk Lamp", "Workspace", 7_900);
    public static readonly ProductInfo StillMug = new("p5", "Still Ceramic Mug", "Kitchen", 2_400);
    public static readonly ProductInfo PocketSpeaker = new("p6", "Pocket Mini Speaker", "Audio", 5_900);
    public static readonly ProductInfo FoldOrganizer = new("p8", "Fold Travel Organizer", "Travel Accessories", 3_500);
    public static readonly ProductInfo BrewKettle = new("p10", "Brew Pour-Over Kettle", "Kitchen", 6_900, CompareAtPrice: 9_900);
    public static readonly ProductInfo PulseController = new("p16", "Pulse Wireless Controller", "Gaming", 5_900, CompareAtPrice: 7_900);
    public static readonly ProductInfo NovaChair = new("p19", "Nova Gaming Chair", "Gaming", 24_900, CompareAtPrice: 34_900);
    public static readonly ProductInfo TrailBottle = new("p26", "Trail Insulated Bottle", "Outdoor & Camping", 2_900, FreeShipping: true);
    public static readonly ProductInfo NoteJournal = new("p29", "Note Dotted Journal", "Stationery", 1_800, FreeShipping: true);
    public static readonly ProductInfo PureToothbrush = new("p34", "Pure Bamboo Toothbrush Set", "Personal Care", 1_200, FreeShipping: true);
    public static readonly ProductInfo ShellPhoneCase = new("p40", "Shell Phone Case", "Phones & Tablets", 1_900, FreeShipping: true);
    public static readonly ProductInfo FlowYogaMat = new("p23", "Flow Yoga Mat", "Fitness", 3_900, FreeShipping: true);
    public static readonly ProductInfo JumpRope = new("p62", "Jump Speed Rope", "Fitness", 1_400, FreeShipping: true);
    public static readonly ProductInfo PocketNotebook = new("p87", "Pocket Notebook 3-pack", "Stationery", 1_995);
    public static readonly ProductInfo TwistGrip = new("p88", "Twist Phone Grip", "Phones & Tablets", 1_295);

    private static readonly IReadOnlyDictionary<string, ProductInfo> Named = typeof(Catalog)
        .GetFields()
        .Where(f => f.FieldType == typeof(ProductInfo))
        .Select(f => (ProductInfo)f.GetValue(null)!)
        .ToDictionary(p => p.Name);

    public static ProductInfo ByName(string name) =>
        Named.TryGetValue(name, out var product)
            ? product
            : throw new ArgumentException($"'{name}' is not in Data/Catalog.cs. Add it there with its expected price.");
}
