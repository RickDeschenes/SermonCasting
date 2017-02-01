using Android.Util;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Xml;

namespace SermonCasting.Services
{
    internal class LoadRSS: IDisposable
    {
        internal static string AppName { get { return "Sermon Casting by Anotherdad"; } }

        internal ArrayAdapter<string> pcNames;
        internal ArrayAdapter<string> pcUrls;
        internal ArrayAdapter<string> pcDurations;
        internal ArrayAdapter<string> pcDates;
        internal ArrayAdapter<string> pcPassages;

        private List<string> items = new List<string>(new[] { "No Items found." });
        private List<string> urls = new List<string>(new[] { "No Items found." });
        private List<string> durs = new List<string>(new[] { "00:00:00" });
        private List<string> passages = new List<string>(new[] { "No Items found." });
        private List<string> dates = new List<string>(new[] { "No Items found." });

        private string _rss = "http://faithjourney.sermonpodcasts.net/audio/rss.xml";
        private string _tag = "SermonCasting.LoadRSS";
        
        private static string raw = "<?xml version='1.0' encoding='utf-8'?>" +
                                    "<SermonCasting>" +
                                    "  <Church>" +
                                    "    <string name = 'Title' > Anotherdad's Sermon Podcasting</string>" +
                                    "    <string name = 'Name' > Anotherdad's Sermon Podcasting, Bentonville</string>" +
                                    "    <string name = 'Website' > </string>" +
                                    "    <string name = 'Feed' > </string>" +
                                    "    <string name = 'IconUrl' > </string>" +
                                    "    <string name = 'BackgroundUrl' > </string>" +
                                    "    <string name = 'PlayEnabled' > </string>" +
                                    "    <string name = 'PlayDisabled' > </string>" +
                                    "    <string name = 'PauseEnabled' > </string>" +
                                    "    <string name = 'PauseDisabled' > </string>" +
                                    "    <string name = 'StopEnabled' > " +
                                    "    <string name = 'StopDisabled' > </string>" +
                                    "    <string name = 'WebsiteEnabled' > </string>" +
                                    "    <string name = 'WebsiteDisabled' > </string>" +
                                    "  </Church>" +
                                    "</SermonCasting>";

        internal string RSS
        {
            get { return _rss; }
            set { _rss = value; }
        }

        internal string URL { get; set; }
        internal string Name { get; set; }
        internal MainActivity Main { get; set; }

        /// <summary>
        /// Internal clean up
        /// </summary>
        public void Dispose()
        {
            pcNames.Dispose();
            pcUrls.Dispose();
            pcDurations.Dispose();
            pcPassages.Dispose();
            pcDates.Dispose();
            items.Clear();
            urls.Clear();
            durs.Clear();
            passages.Clear();
            dates.Clear();

            Main.Dispose();
        }

        /// <summary>
        /// Entry into the class
        /// </summary>
        internal LoadRSS()
        {
            
        }

        /// <summary>
        /// Used to load class all at once and call the load functions
        /// </summary>
        /// <param name="main"></param>
        /// <param name="_feed"></param>
        /// <param name="_tag"></param>
        internal LoadRSS(MainActivity main, string _feed, string _tag)
        {
            Main = main;
            _rss = _feed;
            this._tag = _tag;
            LoadAllCasts();
        }

        /// <summary>
        /// Private procedure to load all sermon casts for a set
        /// (church)
        /// </summary>
        private void LoadAllCasts()
        {
            if (_rss.Length == 0)
                return;

            items.Clear();
            urls.Clear();
            durs.Clear();
            passages.Clear();
            dates.Clear();

            //Initializer the class level objects
            pcNames = new ArrayAdapter<string>(Main, Android.Resource.Layout.SimpleListItem1, items);
            pcUrls = new ArrayAdapter<string>(Main, Android.Resource.Layout.SimpleListItem1, urls);
            pcDurations = new ArrayAdapter<string>(Main, Android.Resource.Layout.SimpleListItem1, durs);
            pcPassages = new ArrayAdapter<string>(Main, Android.Resource.Layout.SimpleListItem1, passages);
            pcDates = new ArrayAdapter<string>(Main, Android.Resource.Layout.SimpleListItem1, dates);

            //call out to the loadRSS class
            XmlDocument doc = new XmlDocument();
            if (_rss.Contains(".php"))
            {
                string content = ReadPHP(_rss);
                doc.LoadXml(content);
            }
            else
                doc.Load(_rss);
            var namespaceName = "itunes";
            var namespacePrefix = string.Empty;
            XmlNamespaceManager nm = null;
            if (doc.SelectSingleNode("rss").Attributes != null)
            {
                var xmlns = doc.SelectSingleNode("rss").Attributes["xmlns:itunes"];
                if (xmlns != null)
                {
                    nm = new XmlNamespaceManager(doc.NameTable);
                    nm.AddNamespace(namespaceName, xmlns.Value);
                    namespacePrefix = namespaceName + ":";
                }
            }
            string search = "//item";
            Log.Debug(LoadRSS.AppName, "There are items in the RSS.");
            //This loops through the list and writes out the title and URL.
            foreach (XmlNode nd in doc.SelectNodes(search))
            {
                if (nd.SelectSingleNode("enclosure") != null)
                {
                    // Add the casts to our listview
                    if (nd.InnerXml.Contains("<description>") && nd.InnerXml.Contains("Speaker or Performer: "))
                    {
                        string fulltitle = ParseDiscription(nd);
                        pcNames.Add(fulltitle);
                    }
                    else
                    {
                        pcNames.Add(nd.SelectSingleNode("title").InnerText);
                        pcDates.Add(nd.SelectSingleNode("pubDate").InnerText);

                        if (nd.SelectSingleNode("itunes:subtitle", nm) != null)
                            pcPassages.Add(nd.SelectSingleNode("itunes:subtitle", nm).InnerText);
                        else
                            pcPassages.Add("John 3.16");
                    }
                   
                    pcUrls.Add(nd.SelectSingleNode("enclosure").Attributes.GetNamedItem("url").InnerText);
                    if (nd.SelectSingleNode("itunes:duration", nm) != null)
                    {
                        string formated = FormatDuration(nd.SelectSingleNode("itunes:duration", nm).InnerText);
                        pcDurations.Add(formated);
                    }
                    else
                        pcDurations.Add("0:00");
                }
                else if (nd.SelectSingleNode("link") != null)
                {
                    //locate mpx from HTML
                    string raw = ProcessWebPage(nd.SelectSingleNode("link").InnerText);
                    if (raw != null)
                    {
                        pcUrls.Add(raw);
                        // Add the casts to our listview
                        if (nd.InnerXml.Contains("<description>") && nd.InnerXml.Contains("Speaker or Performer: "))
                        {
                            string fulltitle = ParseDiscription(nd);
                            pcNames.Add(fulltitle);
                        }
                        // Add the casts to our listview
                        else
                        {
                            pcNames.Add(nd.SelectSingleNode("title").InnerText);
                            if (nd.SelectSingleNode("itunes:subtitle", nm) != null)
                                pcPassages.Add(nd.SelectSingleNode("itunes:subtitle", nm).InnerText);
                            else
                                pcPassages.Add("John 3.16");
                        }

                        if (nd.SelectSingleNode("itunes:duration", nm) != null)
                        {
                            string formated = string.Format("00:00:00", nd.SelectSingleNode("itunes:duration", nm).InnerText);
                            pcDurations.Add(formated);
                        }
                        else
                            pcDurations.Add("0:00");

                        pcDates.Add(nd.SelectSingleNode("pubDate").InnerText);                       
                    }
                }
                else
                {
                    Log.Debug(LoadRSS.AppName, "unhandled node: \n" + nd.InnerXml);
                    Debug.Print(LoadRSS.AppName + " - unhandled node: \n" + nd.InnerXml);
                }
            }

            if (pcNames.Count == 0)
                pcNames.Add("No Items found.");
            Log.Debug(LoadRSS.AppName, "There are " + pcNames.Count + " items in the RSS.");

            pcNames.NotifyDataSetChanged();
        }

        private string FormatDuration(string text)
        {
            string results = text;
            if (text == null || text == "")
                results = "00:00:00";
            if (text.Length > 8)
            {
                // wee need to parse this
                Log.Debug("Value {0} is invalid as a duration'", text);
                results = "00:00:00";
            }
            char[] chars = text.ToCharArray();
            if (text.Length == 8 && chars[2] == ':' && chars[5] == ':')
                results = text;

            if (text.Contains(":"))
            {
                string[] split = text.Split(':');
                if (split.Length > 3)
                    results = "00:00:00";
                if (split.Length == 3)
                {
                    results = int.Parse(split[0]).ToString("00") + ":";
                    results += int.Parse(split[1]).ToString("00") + ":";
                    results += int.Parse(split[2]).ToString("00");
                }
                else if (split.Length == 2)
                {
                    results = "00:";
                    results += int.Parse(split[0]).ToString("00") + ":";
                    results += int.Parse(split[1]).ToString("00");
                }
                else if (split.Length == 1)
                {
                    results = "00:00:";
                    results += int.Parse(split[0]).ToString("00");
                }
            }
            return results;
        }

        private string ParseDiscription(XmlNode nd)
        {
            string data = nd.SelectSingleNode("description").InnerText;
            string pubDate = nd.SelectSingleNode("pubDate").InnerText;
            string title = nd.SelectSingleNode("title").InnerText;
            int paster = data.IndexOf("Speaker or Performer: ") + "Speaker or Performer: ".Length;
            int passage = data.IndexOf("Scripture Passage(s): ", paster);

            string spaster = data.Substring(paster, passage - paster).Replace("\n", "");
            passage += "Scripture Passage(s): ".Length;
            int dateS = data.IndexOf("Date of Delivery: ", paster);
            string spassage = data.Substring(passage, dateS - passage).Replace("\n", "");

            dateS += "Date of Delivery: ".Length;
            string sdate = data.Substring(dateS).Replace("\n", "");
            DateTime odate = DateTime.Now;
            if (DateTime.TryParse(sdate, out odate))
            {
                sdate = DateTime.Parse(sdate).ToShortDateString();
            }
            else if (DateTime.TryParse(pubDate, out odate))
            {
                sdate = DateTime.Parse(pubDate).ToShortDateString();
            }
            else
                sdate = "";

            pcDates.Add(sdate);
            pcPassages.Add(spassage);
            string fulltitle = title + " - " + spaster;
            if (sdate != "")
                fulltitle += " - " + sdate;
            return fulltitle;
        }

        private string ProcessWebPage(string innerText)
        {
            string results = "";
            string raw = ReadPHP(innerText);

            string search = '"'.ToString() + "http://";
            if (raw.Contains("<source src=" + search + "d347th6f9xsx6x.cloudfront.net/c2733/"))
            {
                //<a href="http://d347th6f9xsx6x.cloudfront.net/c2733/joiningthebattle.mp3" download>
                int dtmp = raw.IndexOf("d347th6f9xsx6x.cloudfront.net/c2733/") + "d347th6f9xsx6x.cloudfront.net/c2733/".Length;

                int ahref = raw.IndexOf("<a href=", dtmp) + 8;
                int refend = raw.IndexOf("\" target=\"_blank\">", ahref) - 1;
                if (dtmp == "d347th6f9xsx6x.cloudfront.net/c2733/".Length)
                    return "";
                string mp = raw.Substring(ahref + 1, refend - ahref);
                return mp;
            }
            return results;
        }

        private string ReadPHP(string _rss)
        {
            string results = "";
            WebRequest request = WebRequest.Create(_rss);
            Stream dataStream;
            WebResponse response;
            StreamReader reader;

            response = request.GetResponse();
            dataStream = response.GetResponseStream();
            reader = new StreamReader(dataStream);
            results = reader.ReadToEnd();

            return results;
        }

        /// <summary>
        /// Open file and return list of xml nodes
        /// </summary>
        /// <param name="url"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        internal static XmlNodeList GetPage(string url, string search = "//item")
        {
            XmlNodeList itms = null;

            string xmlStr = "";

            //Use a WebClient to load feed
            using (var wc = new WebClient())
            {
                xmlStr = wc.DownloadString(url);
            }
            //Validate the xml
            if (xmlStr.StartsWith("<?xml version="))
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.LoadXml(xmlStr);
                }
                catch (Exception e)
                {
                    Log.Error(AppName, String.Format("error loading xml: {0}", e.Message));
                }
                if (xmlDoc == null)
                    xmlDoc.LoadXml(raw);
                //This builds a list of the Item nodes
                if (xmlDoc != null)
                    itms = xmlDoc.SelectNodes(search);
            }
            return itms;
        }

        /// <summary>
        /// validate URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal static bool isValidURL(string url)
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

    }
}