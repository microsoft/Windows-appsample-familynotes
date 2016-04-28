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
using System.Runtime.Serialization;
using Windows.UI.Xaml.Media.Imaging;

namespace FamilyNotes
{
    /// <summary>
    /// Represents a person in the FamilyNotes data model.
    /// </summary>
    [DataContract]
    public class Person
    {

        /// <summary>
        ///  Create a person
        /// </summary>
        /// <param name="name">Name of the person as it will be displayed on the note</param>
        /// <param name="imagePath">Path to the image for the person</param>
        public Person(string name, string imagePath)
        {
            FriendlyName = name;
            ImageFileName = imagePath;
            IsProfileImage = false;
        }
        
        /// <summary>
        /// Gets or sets the user-readable name for the current <see cref="Person"/> instance.
        /// </summary>
        [DataMember]
        public string FriendlyName { get; set; }

        /// <summary>
        /// Whether the person has a profile image or not.
        /// </summary>
        public bool IsProfileImage { get; set; }

        /// <summary>
        /// Gets the profile image for the current <see cref="Person"/> instance.
        /// </summary>
        /// <remarks>This property isn't serialized.</remarks>
        public BitmapImage FaceImage => new BitmapImage(IsProfileImage ? new Uri(ImageFileName) : new Uri(new Uri("ms-appx://"), ImageFileName));

        /// <summary>
        /// Gets or sets the name of the image file for the current <see cref="Person"/> instance.
        /// </summary>
        [DataMember]
        public string ImageFileName { get; set; }

    }
}