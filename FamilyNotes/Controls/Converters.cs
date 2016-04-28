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
using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace FamilyNotes.Converters
{
    /// <summary>
    /// Convert a bitmap image to a brush that we can bind against
    /// </summary>
    public class BitmapToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                ImageBrush brush = new ImageBrush
                {
                    Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill,
                    ImageSource = (BitmapImage)value
                };
                return brush;
            }

            return null; // no image
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Used to bind the visiblity of the Ink controls based on the input mode of the note.
    /// </summary>
    public class InputModeConverter : IValueConverter
    {
        /// <summary>
        /// Returns the visibility the passed in control should have
        /// </summary>
        /// <param name="value">The mode that the note has been changed to</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">The control that is asking for visiblity information, e.g. if "Text", the text container is calling the converter</param>
        /// <param name="language"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Debug.Assert(value != null, "Expecting a value to convert");
            Debug.Assert(parameter != null, "Expecting a parameter describing the caller");

            NoteInputMode currentNoteMode = (NoteInputMode)value;

            // If the ink control is calling, make it visible if the note is in Ink mode
            if (parameter.Equals("Ink"))
            {
                return currentNoteMode == NoteInputMode.Ink ? Visibility.Visible : Visibility.Collapsed;
            }

            // If the text control is calling, make it visible if the note is in text mode
            if (parameter.Equals("Text"))
            {
                bool textMode = 
                    currentNoteMode == NoteInputMode.Dictation || 
                    currentNoteMode == NoteInputMode.Text || 
                    currentNoteMode == NoteInputMode.Default;

                return textMode ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Visible; // everything else should be visible.
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
