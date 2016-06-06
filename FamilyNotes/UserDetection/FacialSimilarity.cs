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

/*
    Please note: When the Microsoft Cognitive Services Face APIs are used images are transmitted to  
    Microsoft for processing. Microsoft will receive the images and other data that’s uploaded and may 
    use them for service improvement purposes. For more information about Microsoft 
    privacy policies and usage guidance, see https://privacy.microsoft.com/privacystatement and 
    http://research.microsoft.com/en-us/UM/legal/DeveloperCodeofConductforCognitiveServices.htm.
*/

using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Xaml;

namespace FamilyNotes.UserDetection
{
    public static class FacialSimilarity
    {

        /// <summary>
        /// Properties that stores the service key for Microsoft Face APIs and 
        /// that indicate whether the facelist has been created.
        /// </summary>
        public static bool InitialTrainingPerformed { get; private set; } = false;

        

        /// <summary>
        /// Prepares for facial recognition by cacheing a single image for each user in a 
        /// persistent FaceList.
        /// </summary>
        public static async Task<int> TrainDetectionAsync()
        {
            StorageFolder LocalFolder = ApplicationData.Current.LocalFolder;
            await _semaphore.WaitAsync();

            await LoadSettingsAsync();
            await CheckTransactionCapAsync();
            _userImages.Clear();
            _userFacialIDs.Clear();
            _userNames.Clear();
            int FacesAdded = 0;

            // Find Users directory and load each folder of user images
            try
            {
                StorageFolder Users = await LocalFolder.GetFolderAsync("Users");
                IReadOnlyList<StorageFolder> UserDirectories = await Users.GetFoldersAsync();
                foreach(StorageFolder UserFolder in UserDirectories)
                {
                    string UserName = UserFolder.Name;
                    IReadOnlyList<StorageFile> UserPictures = await UserFolder.GetFilesAsync();

                    //Just retrieve the first image for each user, assuming the user has an image. 
                    if (UserPictures.Any<StorageFile>())
                    {
                        _userImages.Add(UserName, UserPictures.First<StorageFile>());
                    }
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                _semaphore.Release();
                return FacesAdded;
            }

            // Delete any existing FaceList held by the Microsoft Face service. Exception is thrown and surpressed if specified list didn't exist.
            try
            {
                await WaitOnTransactionCapAsync();
                await _faceClient.DeleteFaceListAsync(_listKey);
                _transactionCount++;
            }
            catch (Exception)
            {
            }

            // Create a new FaceList in the Microsoft Face service for persistent Face storage.
            try
            {
                await WaitOnTransactionCapAsync();
                await _faceClient.CreateFaceListAsync(_listKey, _listKey, "");
                _transactionCount++;
            }
            catch (Exception)
            {
                _semaphore.Release();
                return FacesAdded;
            }


            var UserNames = _userImages.Keys;

            foreach(var UserName in UserNames)
            {
                var UserImageStorageFile = _userImages[UserName];

                using(var UserImageFilestream = File.OpenRead(UserImageStorageFile.Path))
                {
                    try
                    {
                        // Adds face to list and gets persistent face ID.
                        await WaitOnTransactionCapAsync();
                        var DetectedFaceID = await _faceClient.AddFaceToFaceListAsync(_listKey, UserImageFilestream, UserName);
                        _transactionCount++;
                        _userFacialIDs.Add(UserName, DetectedFaceID.PersistedFaceId);
                        _userNames.Add(DetectedFaceID.PersistedFaceId, UserName);
                        FacesAdded++;
                    }
                    catch (Microsoft.ProjectOxford.Face.FaceAPIException)
                    {
                        // This exception occurs when the Microsoft Face API AddFaceToListAsync service call isn't able to detect a singlular face 
                        // in the profile picture. Additional logic could be added to better determine the cause of failure and surface that to
                        // the app for retry.
                    }

                }
            }

            InitialTrainingPerformed = true;
            _semaphore.Release();
            return FacesAdded;
        }

        /// <summary>
        /// Adds a new user to the existing persistent FaceList without starting from scratch.
        /// </summary>
        public static async Task<bool> AddTrainingImageAsync(string Name, Uri Image)
        {
            await _semaphore.WaitAsync();

            if (_listKey != String.Empty && InitialTrainingPerformed)
            {
                try
                {
                    StorageFile UserImage = await StorageFile.GetFileFromApplicationUriAsync(Image);
                    var UserImageFilestream = File.OpenRead(UserImage.Path);
                    await WaitOnTransactionCapAsync();
                    var DetectedFaceID = await _faceClient.AddFaceToFaceListAsync(_listKey, UserImageFilestream, Name);
                    _transactionCount++;
                    _userFacialIDs.Add(Name, DetectedFaceID.PersistedFaceId);
                    _userNames.Add(DetectedFaceID.PersistedFaceId, Name);
                    _semaphore.Release();
                    return true;
                }
                catch (Exception)
                {
                    _semaphore.Release();
                    return false;
                }
            }
            else
            {
                _semaphore.Release();
                return (await TrainDetectionAsync() > 0 ? true : false);
            }
        }

        /// <summary>
        /// Deletes an existing user from the persistent FaceList, while leaving the rest of the list intact.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteFaceFromUserFaceListAsync(string name)
        {
            Guid FaceID;
            await _semaphore.WaitAsync();
            try
            {
                FaceID = _userFacialIDs[name];
                await WaitOnTransactionCapAsync();
                await _faceClient.DeleteFaceFromFaceListAsync(_listKey, FaceID);
                _transactionCount++;
            }
            catch (Exception)
            {
                _semaphore.Release();
                return false;
            }

            _userFacialIDs.Remove(name);
            _userNames.Remove(FaceID);
            _userImages.Remove(name);
            _semaphore.Release();

            return true;
        }

        /// <summary>
        /// Submits a dynamically taken image to the Microsoft Face API for Similarity comparison against stored detected faces
        /// from TrainDetectionAsync. Of the faces checked, the one that is the closest match (if a match is found) is returned.
        /// </summary>
        public static async Task<string> CheckForUserAsync(Uri UnidentifiedImage)
        {
            await _semaphore.WaitAsync();
            var DynamicUserImageStorageFile = await StorageFile.GetFileFromApplicationUriAsync(UnidentifiedImage);

            using(var DynamicUserImageFilestream = File.OpenRead(DynamicUserImageStorageFile.Path))
            {
                //Gets ID for face, which is good for 24 hours. 
                //Should we error check for multiple faces or no faces?
                Face[] DetectedFaces;
                await WaitOnTransactionCapAsync();
                try
                {
                    DetectedFaces = await _faceClient.DetectAsync(DynamicUserImageFilestream);
                    _transactionCount++;
                }
                catch (Exception)
                {
                    _semaphore.Release();
                    return "";
                }

                Guid DynamicID;
                if (DetectedFaces.Length > 0)
                    DynamicID = DetectedFaces[0].FaceId;

                FaceList SavedUserFaces = null;
                SimilarPersistedFace[] FacialSimilarityResults;
                try
                {
                    await WaitOnTransactionCapAsync();
                    SavedUserFaces = await _faceClient.GetFaceListAsync(_listKey);
                    _transactionCount++;

                    await WaitOnTransactionCapAsync();
                    _transactionCount++;
                    FacialSimilarityResults = await _faceClient.FindSimilarAsync(DynamicID, _listKey);
                }
                catch
                {
                    _semaphore.Release();
                    return "";
                }

                _semaphore.Release();
                return FacialSimilarityResults.Length == 0 ? "" : _userNames[FacialSimilarityResults[0].PersistedFaceId];
            }
        }

        /// <summary>
        /// Deletes persistent face data from the Microsoft Face service, as well as the file storing keys.
        /// </summary>
        public static async Task ClearFaceDetectionDataAsync()
        {
            await _semaphore.WaitAsync();
            await CheckTransactionCapAsync();
            // Delete data from the service
            await WaitOnTransactionCapAsync();
            await _faceClient.DeleteFaceListAsync(_listKey);
            _transactionCount++;
            _listKey = "";

            if (await ApplicationData.Current.LocalFolder.TryGetItemAsync("FaceSettings.xml") != null)
            {
                StorageFile SettingsInfo = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/FaceSettings.xml"));
                await SettingsInfo.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            _semaphore.Release();
        }


        /// <summary>
        /// Loads stored settings, instantiates FaceServiceClient, and generates a GUID for FaceList if one didn't previously exist.
        /// </summary>
        private static async Task LoadSettingsAsync()
        {
            _faceClient = new FaceServiceClient(((App)Application.Current).AppSettings.FaceApiKey);

            if (await ApplicationData.Current.LocalFolder.TryGetItemAsync("FaceSettings.xml") != null)
            {
                StorageFile SettingsInfo = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/FaceSettings.xml"));
                var SettingsDocument = await XmlDocument.LoadFromFileAsync(SettingsInfo);
                _listKey = SettingsDocument.GetElementsByTagName("ListKey").FirstOrDefault<IXmlNode>().Attributes.GetNamedItem("Key").InnerText;

                if (_listKey == "")
                {
                    _listKey = Guid.NewGuid().ToString();
                    await SaveSettingsAsync();
                }
            }
            else
            {
                XmlDocument SettingsDocument = new XmlDocument();
                var DefaultXml = "<FacialVerificationSettings>\n" +
                                        "\t<ListKey Key =\"\"/>\n" +
                                    "</FacialVerificationSettings>\n";
                SettingsDocument.LoadXml(DefaultXml);
                _listKey = Guid.NewGuid().ToString();
                SettingsDocument.GetElementsByTagName("ListKey").FirstOrDefault<IXmlNode>().Attributes.GetNamedItem("Key").InnerText = _listKey;
                StorageFile SettingsInfo = await ApplicationData.Current.LocalFolder.CreateFileAsync("FaceSettings.xml");
                await SettingsDocument.SaveToFileAsync(SettingsInfo);
            }
        }

        /// <summary>
        /// Saves the face list associated with the app, allowing for clean-up when retraining is performed.
        /// </summary>
        private static async Task SaveSettingsAsync()
        {
            StorageFile SettingsInfo = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/FaceSettings.xml"));
            var SettingsDocument = await XmlDocument.LoadFromFileAsync(SettingsInfo);

            SettingsDocument.GetElementsByTagName("ListKey").FirstOrDefault<IXmlNode>().Attributes.GetNamedItem("Key").InnerText = _listKey;
            await SettingsDocument.SaveToFileAsync(SettingsInfo);
        }


        /// <summary>
        /// Checks elapsed time since initial transaction time and determines if the local Face API transaction cap can be reset.
        /// </summary>
        private static async Task CheckTransactionCapAsync()
        {
            DateTime Current = DateTime.Now;
            await Task.Run(() =>
            {
                TimeSpan TimeSinceLastIntialTransction = Current.Subtract(_initialTransactionStartTime);
                if (TimeSinceLastIntialTransction > TimeSpan.FromSeconds(_transactionTimeCap))
                {
                    _transactionCount = 0;
                    _initialTransactionStartTime = DateTime.Now;
                }
            });
        }

        /// <summary>
        /// Prevents continuation of Face API transactions until the transaction cap resets.
        /// </summary>
        private static async Task WaitOnTransactionCapAsync()
        {
            while(_transactionCount >= _transactionCap)
            {
                Debug.WriteLine("Waiting for transaction cap to reset");
                await Task.Delay(_retryTime);
                await CheckTransactionCapAsync();
            }
        }



        // Necessary fields for storing and looking up user images, names, and Face API IDs.
        private static Dictionary<string, StorageFile> _userImages = new Dictionary<string, StorageFile>();
        private static Dictionary<string, Guid> _userFacialIDs = new Dictionary<string, Guid>();
        private static Dictionary<Guid, string> _userNames = new Dictionary<Guid, string>();

        // Microsoft Face API service client object and keys for the Face service and persistent face list.
        // The Service key is set by bound UI setting.
        private static FaceServiceClient _faceClient;
        private static string _listKey;


        // Tracking fields to help keep within Face API transaction limits.
        private static DateTime _initialTransactionStartTime = DateTime.Now;
        private static int _transactionCount = 0;
        private const int _retryTime = 1000;
        private const int _transactionCap = 20;
        private const double _transactionTimeCap = 60.0;


        // Threading lock to prevent against unexpected modification of global static fields if static method is called from multiple threads.
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        
    }
}