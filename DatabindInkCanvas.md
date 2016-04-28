# Data binding an`InkCanvas`control

The FamilyNotes app implements the notes through a partnership between  [StickyNote](FamilyNotes/StickyNote.cs), which provides the data for a note, and [Note.xaml](FamilyNotes/Controls/Note.xaml) which displays the note's contents.

The note's inking capability is provided by a [InkCanvas control](https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.controls.inkcanvas.aspx). To bind ink data from XAML you bind to the [InkStrokeContainer](https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.input.inking.inkstrokecontainer.aspx) on the `InkCanvas`. Unfortunately, this property is not available for binding from XAML. [BindableInkCanvas.cs](FamilyNotes/Controls/BindableInkCanvas.cs) adds this capability.

`BindableInkCanvas` derives from `InkCanvas` and adds a `Strokes` [dependency property](https://msdn.microsoft.com/windows/uwp/xaml-platform/dependency-properties-overview):
``` csharp

public class BindableInkCanvas : InkCanvas
{
    public InkStrokeContainer Strokes
    {
        get { return (InkStrokeContainer)GetValue(StrokesProperty); }
        set { SetValue(StrokesProperty, value); }
    }

    public static readonly DependencyProperty StrokesProperty = DependencyProperty.RegisterAttached(
         "Strokes",
         typeof(InkStrokeContainer),
         typeof(BindableInkCanvas),
         new PropertyMetadata(null, StrokesChanged)
       );

    private static void StrokesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var instance = d as BindableInkCanvas;
        if (instance != null)
        {
            instance.InkPresenter.StrokeContainer = instance.Strokes;
        }
    }
}

```

With the `Strokes` dependency property we can bind the InkCanvas's `InkPresenter.StrokeContainer` from XAML:
```xml

 <local:BindableInkCanvas
    x:Name="containerForInk"
    Strokes="{Binding Path=Ink, Mode=TwoWay}"

```
The FamilyNotes app uses this binding to connect the ink data stored in the `StickyNote` to the `InkCanvas` that will display it.
