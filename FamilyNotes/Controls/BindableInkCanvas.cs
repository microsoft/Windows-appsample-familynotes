//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FamilyNotes
{
    /// <summary>
    /// Provides methods that enable binding to an InkStrokeContainer
    /// on an <see cref="InkCanvas"/> control.
    /// </summary>
    public class BindableInkCanvas : InkCanvas
    {
        /// <summary>
        /// Gets or sets the <see cref="InkStrokeContainer"/> for the current ink canvas.
        /// </summary>
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
}
