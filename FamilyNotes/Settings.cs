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
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace FamilyNotes
{
    /// <summary>
    /// Represents the settings for the FamilyNotes app.
    /// This class is designed to be serializable.
    /// BindableBase is part of a collection of task snippets
    /// that you can freely use in your code.
    /// See https://github.com/Microsoft/Windows-task-snippets/blob/master/tasks/Data-binding-change-notification.md
    /// </summary>
    public class Settings : BindableBase
    {

        /// <summary>
        /// The wallpaper bitmap to use for the background
        /// </summary>
        [IgnoreDataMemberAttribute]
        public BitmapImage FamilyNotesWallPaper
        {
            get
            {
                return _familyNotesWallPaper;
            }
            set
            {
                SetProperty(ref _familyNotesWallPaper, value);
            }
        }

        /// <summary>
        /// Your key for the Microsoft Face API that allows you to use the service
        /// </summary>
        [DataMember]
        public string FaceApiKey
        {
            get
            {
                return _faceApiKey;
            }
            set
            {
                SetProperty(ref _faceApiKey, value);
            }
        }

        /// <summary>
        /// The default CameraID
        /// </summary>
        [DataMember]
        public string DefaultCameraID
        {
            get
            {
                return _defaultCameraID;
            }
            set
            {
                SetProperty(ref _defaultCameraID, value);
            }
        }

        /// <summary>
        /// Gets or sets whether the app background should be the Bing image of the day or not.
        /// </summary>
        [DataMember]
        public bool UseBingImageOfTheDay
        {
            get
            {
                return _useBingImageOfTheDay;
            }
            set
            {
                if (value != _useBingImageOfTheDay)
                {
                    _useBingImageOfTheDay = value;
                    var fireAndForget = ChangeWallPaperAsync(value); // binding will update the UI when the FamilyNotesWallpaper gets set
                    OnPropertyChanged(nameof(UseBingImageOfTheDay)); // notifies UI that is binding to property that it has changed.
                }
            }
        }
        /// <summary>
        /// Gets or sets a flag to determine if this is the very first time the app has been launched
        /// </summary>
        [DataMember]
        public bool LaunchedPreviously
        {
            get
            {
                return _launchedPreviously;
            }
            set
            {
                SetProperty(ref _launchedPreviously, value);
            }
        }

        /// <summary>
        /// Save app settings. These settings will roam.
        /// </summary>
        public void SaveSettings()
        {
            ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
            settings.Values[WALLPAPER] = UseBingImageOfTheDay;
            settings.Values[MICRFOSOFT_FACESERVICE_KEY] = FaceApiKey;
            settings.Values[NOTFIRSTLAUNCH] = true;
            settings.Values[DEFAULTCAMERAID] = DefaultCameraID;
        }

        /// <summary>
        /// Load app settings.
        /// </summary>
        public void LoadSettings()
        {
            ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
            bool? useBingImage = (bool?)settings.Values[WALLPAPER];
            UseBingImageOfTheDay = useBingImage.HasValue ? useBingImage.Value : false;
            string faceApiKey = (string)settings.Values[MICRFOSOFT_FACESERVICE_KEY];
            FaceApiKey = faceApiKey != null ? faceApiKey : "";
            bool? notFirstLaunch = (bool?)settings.Values[NOTFIRSTLAUNCH];
            LaunchedPreviously = notFirstLaunch.HasValue ? notFirstLaunch.Value : false;
            string defaultCameraID = (string)settings.Values[DEFAULTCAMERAID];
            DefaultCameraID = defaultCameraID != null ? defaultCameraID : "";
        }

        
        /// <summary>
        /// Set the wallpaper (either the bing image of the day or the default brushed steel)
        /// </summary>
        /// <param name="useBingImageOfTheDay">True = use the Bing image of the day; False = use the brushed steel wallpaper</param>
        private async Task ChangeWallPaperAsync(bool useBingImageOfTheDay)
        {
            if (useBingImageOfTheDay == true)
            {
                try
                {
                    FamilyNotesWallPaper = new BitmapImage(await GetBingImageOfTheDayUriAsync());
                    return;
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    // Fall through to use the default brush steel image instead.
                }
            }

            // When we aren't using the bing image of the day, default to the brush steel background appearance
            FamilyNotesWallPaper = new BitmapImage(new Uri(new Uri("ms-appx://"), "Assets/brushed_metal_texture.jpg"));
        }

        private enum Resolution { Unspecified, _800x600, _1024x768, _1366x768, _1920x1080, _1920x1200 }

        /// <summary>
        /// Gets the Uri for the Bing imageof the day.
        /// Note that this task snippet is available on GitHub at https://github.com/Microsoft/Windows-task-snippets
        /// </summary>
        /// <param name="resolution"></param>
        /// <param name="market"></param>
        /// <returns></returns>
        private async Task<Uri> GetBingImageOfTheDayUriAsync(
            Resolution resolution = Resolution.Unspecified,
            string market = "en-ww")
        {
            var request = new Uri($"http://www.bing.com/hpimagearchive.aspx?n=1&mkt={market}");
            string result = null;
            using (var httpClient = new HttpClient())
            {
                result = await httpClient.GetStringAsync(request);
            }
            var targetElement = resolution == Resolution.Unspecified ? "url" : "urlBase";
            var pathString = XDocument.Parse(result).Descendants(targetElement).First().Value;
            var resolutionString = resolution == Resolution.Unspecified ? "" : $"{resolution}.jpg";
            return new Uri($"http://www.bing.com{pathString}{resolutionString}");
        }

        private bool _useBingImageOfTheDay;
        private bool _launchedPreviously;
        private string _defaultCameraID;
        private string _faceApiKey = "";
        private BitmapImage _familyNotesWallPaper = new BitmapImage(new Uri(new Uri("ms-appx://"), "Assets/brushed_metal_texture.jpg")); // Before the user has decided on the background, use the brushed steel.
        private const string WALLPAPER = "UseBingImageOfTheDay";
        private const string MICRFOSOFT_FACESERVICE_KEY = "MicrosoftFaceServiceKey";
        private const string NOTFIRSTLAUNCH = "NotTheAppFirstLaunch";
        private const string DEFAULTCAMERAID = "DefaultCameraID";
    }
}