namespace TheExtraordinaryAdditions.Content.NPCs;
/*
public class BestiaryBackgroundOverlay : IBestiaryInfoElement, IBestiaryBackgroundOverlayAndColorProvider
{
    Asset<Texture2D> BestiaryMapOverlayAsset;
    Color? BestiaryMapOverlayColor;
    public float DisplayPriority { get; set; }
    public UIElement ProvideUIElement(BestiaryUICollectionInfo info) => null;
    public Color? GetBackgroundOverlayColor() => BestiaryMapOverlayColor;
    public Asset<Texture2D> GetBackgroundOverlayImage()
    {
        return BestiaryMapOverlayAsset ?? null;
    }
    public BestiaryBackgroundOverlay(Asset<Texture2D> mapOverlay = null, Color? mapOverlayColor = null)
    {
        BestiaryMapOverlayAsset = mapOverlay;
        BestiaryMapOverlayColor = mapOverlayColor;
    }
}
public class BestiaryBackground : IBestiaryInfoElement, IBestiaryBackgroundImagePathAndColorProvider
{
    private Asset<Texture2D> _backgroundImage;
    private Color? _backgroundColor;
    public float OrderPriority { get; set; }
    public UIElement ProvideUIElement(BestiaryUICollectionInfo info)
    {
        return null;
    }
    public Color? GetBackgroundColor()
    {
        return _backgroundColor;
    }
    public Asset<Texture2D> GetBackgroundImage()
    {
        return _backgroundImage;
    }
    public BestiaryBackground(Asset<Texture2D> backgroundImage, Color? backgroundColor)
    {
        _backgroundImage = backgroundImage;
        _backgroundColor = backgroundColor;
    }
}
*/