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
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace FamilyNotes
{
    /// <summary>
    /// Dialog to add a person and take their picture
    /// </summary>
    public sealed partial class AddPersonContentDialog : ContentDialog
    {
        static string TemporaryImagePath = "ms-appx:///Assets/face_1.png";

        public AddPersonContentDialog()
        {
            this.InitializeComponent();

          


        }

        public void ProvideExistingPerson(Person currentPerson)
        {
            if (currentPerson == null)
                return;

            // Change UI slightly depending on if a photo already exists, or if this is the first time
            // we're seeing this dialog to create a person.
            PersonName.Text = currentPerson.FriendlyName;
            PersonName.IsReadOnly = true;
            textBlock.Text = "";
            IsPrimaryButtonEnabled = true;
            PrimaryButtonText = "Update photo";

            // Set the image to the current image, if it exists
            if (currentPerson.ImageFileName != TemporaryImagePath)
            {
                Uri imageUri = new Uri(currentPerson.ImageFileName, UriKind.Relative);
                BitmapImage imageBitmap = new BitmapImage(imageUri);
                image.Source = imageBitmap;
            }
        }


        /// <summary>
        /// The Person object that is created when the user selects to add a person through the dialog.
        /// This object is not created if the user cancels the Add User dialog.
        /// </summary>
        public Person AddedPerson { get;  set; }

        /// <summary>
        /// A file that contains the image added as part of the process for creating a user.
        /// This stores the image capture in a temporary location. In the main thread of the app, it moves the file to the appropriate 
        /// user directory after the app verifies that the user is successfully created and added. The moving of the file needs to
        /// be pushed to the main thread in order to avoid a potential race condition between moving files and creating users in the UX.
        /// </summary>
        public StorageFile TemporaryFile = null;
        

        private void ContentDialog_AddPerson_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            AddedPerson = new Person(PersonName.Text, TemporaryImagePath);
        }

        private void ContentDialog_CancelButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            AddedPerson = null; // abandon adding a new person
        }
        
        private async void ContentDialog_TakePhoto_Clicked(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            captureUI.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.MediumXga;
            captureUI.PhotoSettings.AllowCropping = false;

            StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (photo == null)
            {
                // User cancelled photo capture
                return;
            }

            // Otherwise we need to save the temporary file (after doing some clean up if we already have a file)
            if (TemporaryFile != null)
            {
                await TemporaryFile.DeleteAsync();
            }

            // Save off the photo
            TemporaryFile = photo;

            // Update the image        
            BitmapImage bitmapImage = new BitmapImage();
            FileRandomAccessStream stream = (FileRandomAccessStream)await TemporaryFile.OpenAsync(FileAccessMode.Read);
            bitmapImage.SetSource(stream);
            image.Source = bitmapImage;
        }

        private void ContentDialog_PersonName_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if ((this.IsPrimaryButtonEnabled == false) && (e.Key == VirtualKey.Enter))
            {
                e.Handled = true;
            }
        }

        private async void ContentDialog_PersonName_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Make sure that our name exists
            if (String.IsNullOrWhiteSpace(PersonName.Text))
            {
                this.IsPrimaryButtonEnabled = false;
                textBlock.Text = "Please enter a user name.";
                return;
            }

            // Make sure the name if valid - we can check the directory since each person has a local directory

            IStorageItem folderItem = null;
            try
            {
                // See if the folder exists
                folderItem = await ApplicationData.Current.LocalFolder.TryGetItemAsync("Users\\" + PersonName.Text);
            }
            catch (Exception)
            {
                // We don't get an exception if the item doesn't exist. We will if we were passed an illegal
                // path in which case we can't have a user with that name. Or there could be some other
                // issue with that directory name. Regardless you can't use that name.
                this.IsPrimaryButtonEnabled = false;
                textBlock.Text = "Sorry, can't use that name.";
                return;
            }

            // If it doesn't exist, add the user
            if (folderItem == null)
            {
                this.IsPrimaryButtonEnabled = true;
                textBlock.Text = "";
            }
            // If it does, then this user already exists
            else
            {
                this.IsPrimaryButtonEnabled = false;
                textBlock.Text = "This user name already exists.";
            }
        }
    }
}
