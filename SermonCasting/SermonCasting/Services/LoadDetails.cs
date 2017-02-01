using System;
using System.Net;
using System.IO;
using System.Xml;
using Android.Graphics;
using System.Threading.Tasks;
using Android.Widget;
using System.Collections.Generic;
using Android.Util;
using Android.Media;
using Android.Graphics.Drawables;

namespace SermonCasting.Services
{
    internal struct ChurchItems
    {
        internal string Title;
        internal string Name;
        internal string Feed;
        internal string Website;
        internal string IconUrl;
        internal string BackgroundUrl;
    }

    /// <summary>
    /// I am almost a collection class
    /// I will open the list of Churches 
    /// Process their details into an array
    /// I will serve out the active Church
    /// </summary>
    internal class LoadDetails : IDisposable
    {
        internal List<ChurchItems> ChurchDetails = new List<Services.ChurchItems>();
        internal ArrayAdapter<string> ChurchNames;

        private List<string> items = new List<string>(new[] { "No Items found." });

        private ChurchItems _church = new ChurchItems();
        internal ChurchItems Church = new ChurchItems();

        private const string _siteurl = "http://www.faithjourney.us/wp-content/uploads/2016/11/";

        private string _churchFeed = "http://www.faithjourney.us/wp-content/uploads/2016/12/Churchs.txt";

        /// <summary>
        /// The church list location
        /// </summary>
        internal string ChurchFeed
        {
            get { return _churchFeed; }
            set { _churchFeed = value; }
        }

        private string _title = "Your Church's Sermon Podcast Application";
        /// <summary>
        /// Active Church's Title
        /// </summary>
        internal string Title
        {
            get { return _church.Title; }
            set { _church.Title = value; }
        }

        private string _name = "Your Church's Full Name, City";
        /// <summary>
        /// Active Church's Name
        /// </summary>
        internal string Name
        {
            get { return _church.Name; }
            set { _church.Name = value; }
        }

        private string _feed = "";
        /// <summary>
        /// Active Church's feed, location of sermons (RSS)
        /// </summary>
        internal string Feed
        {
            get { return _church.Feed; }
            set { _church.Feed = value; }
        }

        private string _website = "";
        /// <summary>
        /// Active Church's Web Site
        /// </summary>
        internal string Website
        {
            get { return _church.Website; }
            set { _church.Website = value; }
        }

        private string _iconUrl = _siteurl + "adicon.png";
        /// <summary>
        /// Active Church's Icon URL
        /// </summary>
        internal string IconUrl
        {
            get { return _church.IconUrl; }
            set { _church.IconUrl = value; }
        }

        private string _backgroundUrl = _siteurl + "adbackground.png";
        /// <summary>
        /// Active Church's background image URL
        /// </summary>
        internal string BackgroundUrl
        {
            get { return _church.BackgroundUrl; }
            set { _church.BackgroundUrl = value; }
        }

        private XmlNodeList xml;
        private int _index;

        internal MainActivity Main { get; set; }

        public void Dispose()
        {
            ChurchNames.Dispose();
        }

        internal LoadDetails()
        {

        }

        /// <summary>
        /// Used to load class all at once and call the load functions
        /// </summary>
        /// <param name="main">the Activity calling</param>
        /// <param name="feed">the RUL of the church list</param>
        /// <param name="tag">debug log title</param>
        /// <param name="filter">filter to your song table (//SermonCasting/Church</param>
        internal LoadDetails(MainActivity main, string feed, string tag, string filter)
        {
            bool create = true;

            Main = main;
            _churchFeed = feed;
            _title = tag;

            //load the remote file
            xml = LoadRSS.GetPage(_churchFeed, filter);

            //was it any good?
            create = (xml == null) || xml.Count <= 1;

            //if we need to create a new file
            if (create)
            {
                CreateSettings();
            }

            _church = FillCurrent(_church);

            ChurchDetails.Add(_church);
            Church = _church;
            _index = 0;
            FillSettings();
        }

        private ChurchItems FillCurrent(ChurchItems _church)
        {
            _church.Name = _name;
            _church.BackgroundUrl = _backgroundUrl;
            _church.Feed = _feed;
            _church.IconUrl = _iconUrl;;
            _church.Title = _title;
            _church.Website = _website;

            return _church;
        }

        private void ValidateDetails()
        {
            if (!checkURL(_feed))
                Log.Debug(LoadRSS.AppName, _feed + " not found!");
            if (!checkURL(_website))
                Log.Debug(LoadRSS.AppName, _website + " not found!");
            if (!checkURL(_iconUrl))
                Log.Debug(LoadRSS.AppName, _iconUrl + " not found!");
            if (!checkURL(_backgroundUrl))
                Log.Debug(LoadRSS.AppName, _backgroundUrl + " not found!");
        }

        /// <summary>
        /// Return active church's index
        /// -1, no churchs
        /// </summary>
        /// <returns></returns>
        internal int GetIndex()
        {
            return _index;
        }

        /// <summary>
        /// Select active church
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal int SetIndex(int index)
        {
            if (ChurchDetails == null || index < 0)
                return -1;
            if (index > ChurchDetails.Count)
            { //set to default
                _church = ChurchDetails[0];
                _index = 0;
                return 0;
            }
            _church = ChurchDetails[index];
            _index = index;
            return index;
        }

        private void CreateSettings()
        {

            XmlDocument xmla = new XmlDocument();
            XmlDeclaration xmlDeclaration = xmla.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = xmla.DocumentElement;

            xmla.InsertBefore(xmlDeclaration, root);
            XmlNode table = xmla.CreateNode(XmlNodeType.Element, "SermonCasting", "");
            XmlNode attr = xmla.CreateNode(XmlNodeType.Attribute, "Name", "");

            attr.Value = "SermonCasting";
            table.Attributes.SetNamedItem(attr);
            xmla.AppendChild(table);

            //create node Title
            table.AppendChild(CreateNodes(xmla, "string", "name", "Title", _title));
            //create node Feed
            table.AppendChild(CreateNodes(xmla, "string", "name", "Name", _name));
            //create node Feed
            table.AppendChild(CreateNodes(xmla, "string", "name", "Feed", _feed));
            //create node
            table.AppendChild(CreateNodes(xmla, "string", "name", "Website", _website));
            //create node Icon
            table.AppendChild(CreateNodes(xmla, "string", "name", "Icon", _iconUrl));
            //create node BackGround
            table.AppendChild(CreateNodes(xmla, "string", "name", "BackGround", _backgroundUrl));

            xml = xmla.SelectNodes("//String");
            //xml.Save(location);
        }
    
        private XmlNode CreateNodes(XmlDocument xmla, string element, string attributeName, string attributeValue, string value)
        {
            XmlNode nd = xmla.CreateNode(XmlNodeType.Element, element, "");
            XmlNode attr = attr = xmla.CreateNode(XmlNodeType.Attribute, attributeName, "");
            attr.Value = attributeValue;
            nd.Attributes.SetNamedItem(attr);
            nd.InnerText = value;
            return nd;
        }

        private void FillSettings()
        {
            //Reset to default
            items.Clear();
            ChurchDetails.Clear();
            ChurchNames = new ArrayAdapter<string>(Main, Android.Resource.Layout.SimpleListItem1, items);
            ChurchNames.Add(_church.Name);
            ChurchDetails.Add(_church);
            _index = 0;

            //This loops through the list and writes out the title and URL.
            for (int i = 0; i < xml.Count; i++)
            {
                ChurchItems ci = new ChurchItems();
                foreach (XmlNode xl in xml[i])
                {
                    string name = xl.Attributes.GetNamedItem("name").InnerText;
                    switch (name)
                    {
                        case "Title":
                            ci.Title = xl.InnerText;
                            break;
                        case "Name":
                            ci.Name = xl.InnerText;
                            break;
                        case "Feed":
                            ci.Feed = xl.InnerText;
                            break;
                        case "Website":
                            ci.Website = xl.InnerText;
                            break;
                        case "IconUrl":
                            ci.IconUrl = xl.InnerText;
                            break;
                        case "BackgroundUrl":
                            ci.BackgroundUrl = xl.InnerText;
                            break;
                        default:
                            Log.Debug(LoadRSS.AppName, name + " not found, check your spelling!");
                            break;
                    }
                }
                //we have the values add them
                ChurchDetails.Add(ci);
                ChurchNames.Add(ci.Name);
            }
            _index = ChurchDetails.Count;
        }

        private void FillSettingsValidations()
        {
            //Reset to default
            items.Clear();
            ChurchDetails.Clear();
            ChurchNames = new ArrayAdapter<string>(Main, Android.Resource.Layout.SimpleListItem1, items);
            ChurchNames.Add(_church.Name);
            ChurchDetails.Add(_church);
            _index = 0;

            //This loops through the list and writes out the title and URL.
            for (int i = 0; i < xml.Count; i++)
            {
                ChurchItems ci = new ChurchItems();
                foreach (XmlNode xl in xml[i])
                {
                    string name = xl.Attributes.GetNamedItem("name").InnerText;
                    switch (name)
                    {
                        case "Title":
                            string vl = xl.InnerText;
                            if (vl != null)
                                ci.Title = vl;
                            if (ci.Title == null)
                                Log.Debug(LoadRSS.AppName, name + " empty!");
                            break;
                        case "Name":
                            vl = xl.InnerText;
                            if (vl != null)
                                ci.Name = vl;
                            if (ci.Name == null)
                                Log.Debug(LoadRSS.AppName, name + " empty!");
                            break;
                        case "Feed":
                            vl = xl.InnerText;
                            if (checkURL(vl))
                                ci.Feed = vl;
                            if (ci.Feed == null)
                                Log.Debug(LoadRSS.AppName, name + " empty!");
                            break;
                        case "Website":
                            vl = xl.InnerText;
                            if (checkURL(vl))
                                ci.Website = vl;
                            if (ci.Website == null)
                                Log.Debug(LoadRSS.AppName, name + " empty!");
                            break;
                        case "Icon":
                        case "IconUrl":
                            vl = xl.InnerText;
                            if (checkURL(vl))
                                ci.IconUrl = vl;
                            if (ci.IconUrl == null)
                                Log.Debug(LoadRSS.AppName, name + " empty!");
                            break;
                        case "Background":
                        case "BackgroundUrl":
                            vl = xl.InnerText;
                            if (checkURL(vl))
                                ci.BackgroundUrl = vl;
                            if (ci.BackgroundUrl == null)
                                Log.Debug(LoadRSS.AppName, name + " empty!");
                            break;
                        default:
                            Log.Debug(LoadRSS.AppName, name + " not found, check your spelling!");
                            break;
                    }
                }
                //we have the values add them
                ChurchDetails.Add(ci);
                ChurchNames.Add(ci.Name);
            }
            _index = ChurchDetails.Count;
        }

        /// <summary>
        /// Send in the URL to load and a default image
        /// </summary>
        /// <param name="url"></param>
        /// <param name="_default"></param>
        /// <returns></returns>
        internal static Drawable LoadPicture(string url, Drawable _default)
        {
            System.Net.HttpWebRequest wreq;
            System.Net.HttpWebResponse wresp;
            System.IO.Stream mystream;
            Drawable bmp = _default;

            bmp = null;
            mystream = null;
            wresp = null;
            try
            {
                wreq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(url);
                wreq.AllowWriteStreamBuffering = true;

                wresp = (System.Net.HttpWebResponse)wreq.GetResponse();

                if ((mystream = wresp.GetResponseStream()) != null)
                    bmp = new BitmapDrawable(mystream);
            }
            catch
            {
                //Return the current image
                bmp = _default;
            }
            finally
            {
                if (mystream != null)
                    mystream.Close();

                if (wresp != null)
                    wresp.Close();
            }

            return (bmp);
        }

        private bool checkURL(string url)
        {
            bool pageExists = false;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Http.Head;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                pageExists = response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                //Do what ever you want when its no working...
                Log.Debug(LoadRSS.AppName, ex.ToString());
            }
            return pageExists;
        }

        private Task<System.IO.Stream> GetStreamAsync(string url)
        {
            var webClient = new WebClient();
            return webClient.OpenReadTaskAsync(new Uri(url));
        }

        internal static Bitmap DownloadUrl(string url)
        {
            using (StreamReader imageStream = new StreamReader(url))
            {
                return BitmapFactory.DecodeStream(imageStream.BaseStream);
            }
        }

        private async Task<Bitmap> AsyncDownloadUrl(string url)
        {
            System.IO.Stream imageStream = await GetStreamAsync(url);
            return BitmapFactory.DecodeStream(imageStream);
        }
    }
}