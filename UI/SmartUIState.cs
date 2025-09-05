using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.UI;

namespace TheExtraordinaryAdditions.UI;

public abstract class SmartUIState : UIState
{
    protected internal virtual UserInterface UserInterface { get; set; }

    public abstract int InsertionIndex(List<GameInterfaceLayer> layers);

    public virtual bool Visible { get; set; } = false;

    public virtual InterfaceScaleType Scale { get; set; } = InterfaceScaleType.UI;

    public virtual void Unload() { }

    internal void AddElement(UIElement element, Point position, int width, int height)
    {
        element.Left.Set(position.X, 0);
        element.Top.Set(position.Y, 0);
        element.Width.Set(width, 0);
        element.Height.Set(height, 0);
        Append(element);
    }
    internal void AddElement(UIElement element, Point position, int width, int height, UIElement appendTo)
    {
        element.Left.Set(position.X, 0);
        element.Top.Set(position.Y, 0);
        element.Width.Set(width, 0);
        element.Height.Set(height, 0);
        appendTo.Append(element);
    }

    #region SmartUIElement Mitosis
    #region XButton1
    public virtual void SafeXButton1MouseUp(UIMouseEvent evt) { }

    public sealed override void XButton1MouseUp(UIMouseEvent evt)
    {
        base.XButton1MouseUp(evt);
        SafeXButton1MouseUp(evt);
    }

    public virtual void SafeXButton1MouseDown(UIMouseEvent evt) { }

    public sealed override void XButton1MouseDown(UIMouseEvent evt)
    {
        base.XButton1MouseDown(evt);
        SafeXButton1MouseDown(evt);
    }

    public virtual void SafeXButton1Click(UIMouseEvent evt) { }

    public sealed override void XButton1Click(UIMouseEvent evt)
    {
        base.XButton1Click(evt);
        SafeXButton1Click(evt);
    }
    public virtual void SafeXButton1DoubleClick(UIMouseEvent evt) { }

    public sealed override void XButton1DoubleClick(UIMouseEvent evt)
    {
        base.XButton1DoubleClick(evt);
        SafeXButton1DoubleClick(evt);
    }
    #endregion

    #region XButton2
    public virtual void SafeXButton2MouseUp(UIMouseEvent evt) { }

    public sealed override void XButton2MouseUp(UIMouseEvent evt)
    {
        base.XButton2MouseUp(evt);
        SafeXButton2MouseUp(evt);
    }

    public virtual void SafeXButton2MouseDown(UIMouseEvent evt) { }

    public sealed override void XButton2MouseDown(UIMouseEvent evt)
    {
        base.XButton2MouseDown(evt);
        SafeXButton2MouseDown(evt);
    }

    public virtual void SafeXButton2Click(UIMouseEvent evt) { }

    public sealed override void XButton2Click(UIMouseEvent evt)
    {
        base.XButton2Click(evt);
        SafeXButton2Click(evt);
    }

    public virtual void SafeXButton2DoubleClick(UIMouseEvent evt) { }

    public sealed override void XButton2DoubleClick(UIMouseEvent evt)
    {
        base.XButton2DoubleClick(evt);
        SafeXButton2DoubleClick(evt);
    }
    #endregion

    #region LMB
    public virtual void SafeMouseUp(UIMouseEvent evt) { }

    public sealed override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        SafeMouseUp(evt);
    }

    public virtual void SafeMouseDown(UIMouseEvent evt) { }

    public sealed override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        SafeMouseDown(evt);
    }

    public virtual void SafeClick(UIMouseEvent evt) { }

    public sealed override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        SafeClick(evt);
    }

    public virtual void SafeDoubleClick(UIMouseEvent evt) { }

    public sealed override void LeftDoubleClick(UIMouseEvent evt)
    {
        base.LeftDoubleClick(evt);
        SafeDoubleClick(evt);
    }
    #endregion

    #region RMB
    public virtual void SafeRightMouseUp(UIMouseEvent evt) { }

    public sealed override void RightMouseUp(UIMouseEvent evt)
    {
        base.RightMouseUp(evt);
        SafeRightMouseUp(evt);
    }

    public virtual void SafeRightMouseDown(UIMouseEvent evt) { }

    public sealed override void RightMouseDown(UIMouseEvent evt)
    {
        base.RightMouseDown(evt);
        SafeRightMouseDown(evt);
    }

    public virtual void SafeRightClick(UIMouseEvent evt) { }

    public sealed override void RightClick(UIMouseEvent evt)
    {
        base.RightClick(evt);
        SafeRightClick(evt);
    }

    public virtual void SafeRightDoubleClick(UIMouseEvent evt) { }

    public sealed override void RightDoubleClick(UIMouseEvent evt)
    {
        base.RightDoubleClick(evt);
        SafeRightDoubleClick(evt);
    }
    #endregion

    #region MMB
    public virtual void SafeMiddleMouseUp(UIMouseEvent evt) { }

    public sealed override void MiddleMouseUp(UIMouseEvent evt)
    {
        base.MiddleMouseUp(evt);
        SafeMiddleMouseUp(evt);
    }

    public virtual void SafeMiddleMouseDown(UIMouseEvent evt) { }

    public sealed override void MiddleMouseDown(UIMouseEvent evt)
    {
        base.MiddleMouseDown(evt);
        SafeMiddleMouseDown(evt);
    }

    public virtual void SafeMiddleClick(UIMouseEvent evt) { }

    public sealed override void MiddleClick(UIMouseEvent evt)
    {
        base.MiddleClick(evt);
        SafeMiddleClick(evt);
    }

    public virtual void SafeMiddleDoubleClick(UIMouseEvent evt) { }

    public sealed override void MiddleDoubleClick(UIMouseEvent evt)
    {
        base.MiddleDoubleClick(evt);
        SafeMiddleDoubleClick(evt);
    }
    #endregion

    #region Misc
    public virtual void SafeMouseOver(UIMouseEvent evt) { }

    public sealed override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SafeMouseOver(evt);
    }

    public virtual void SafeUpdate(GameTime gameTime) { }

    public sealed override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        SafeUpdate(gameTime);
    }

    public virtual void SafeScrollWheel(UIScrollWheelEvent evt) { }

    public sealed override void ScrollWheel(UIScrollWheelEvent evt)
    {
        base.ScrollWheel(evt);
        SafeScrollWheel(evt);
    }
    #endregion

    #endregion SmartUIElement Mitosis
}
