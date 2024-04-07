using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Numerics;

using Dalamud.Interface.Spannables.Controls.RecyclerViews;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Text;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.TODO;

#pragma warning disable SA1010

/// <summary>A text box control.</summary>
public class TextBoxControl : ControlSpannable
{
    private readonly ObservingRecyclerViewControl<ObservableCollection<StyledTextSpannable>> rvc;

    private readonly List<int> linesWithCarets = [0];

    /// <summary>Initializes a new instance of the <see cref="TextBoxControl"/> class.</summary>
    public TextBoxControl()
    {
        this.AddChild(
            this.rvc = new()
            {
                Collection = [],
            });
        this.TakeKeyboardInputsOnFocus = true;
        this.Focusable = true;
        this.Text = string.Empty;
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(Vector2 suggestedSize)
    {
        this.rvc.RenderPassMeasure(suggestedSize);
        return this.rvc.Boundary;
    }

    /// <inheritdoc/>
    protected override void OnPlace(SpannableEventArgs args)
    {
        this.rvc.RenderPassPlace(Matrix4x4.Identity, this.FullTransformation);
        base.OnPlace(args);
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        this.rvc.RenderPassDraw(args.DrawListPtr);
        base.OnDrawInside(args);
    }

    /// <inheritdoc/>
    protected override void OnTextChange(PropertyChangeEventArgs<string?> args)
    {
        base.OnTextChange(args);
        if (args.State != PropertyChangeState.After)
            return;

        this.rvc.Collection!.Clear();
        using var reader = new StringReader(args.NewValue);
        for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            this.rvc.Collection.Add(new(new StyledText(line)) { EventEnabled = false });
        this.linesWithCarets.Clear();
        this.linesWithCarets.Add(0);
        this.rvc.Collection[0].SetSelection(0, 0, 0);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(SpannableKeyEventArgs args)
    {
        base.OnKeyDown(args);
        if (args.SuppressHandling)
            return;
        if (this.linesWithCarets.Count == 0)
            this.linesWithCarets.Add(0);
        for (var i = 0; i < this.linesWithCarets.Count; i++)
        {
            var line = this.rvc.Collection[i];
            line.
        }
    }
}


// TODO: databind: bind a property of a control to an arbitrary property of an object
