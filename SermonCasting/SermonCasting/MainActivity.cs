using System;
using System.Threading;
using SermonCasting.Services;
using Android.App;
using Android.Util;
using Android.Widget;
using Android.OS;
using Android.Media;
using Android.Net;
using Android.Net.Wifi;
using Android.Content;
using Android.Views.Animations;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Webkit;
using Android.Views;
using Android.Runtime;

namespace SermonCasting
{
    /// <summary>
    /// Players state
    /// </summary>
    public enum Settings
    {
        Empty,
        Playing,
        Paused,
        Stopped,
        Ready
    }
    
    [Activity(Label = "Sermon Casting",
        MainLauncher = true,
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation |
        Android.Content.PM.ConfigChanges.ScreenSize,
        Theme = "@android:style/Theme.NoTitleBar",
        AlwaysRetainTaskState = true,
        LaunchMode = Android.Content.PM.LaunchMode.SingleTop,
        Icon = "@drawable/adicon")]
    public class MainActivity : Activity
    {
        #region "Variables"

        /// <summary>
        /// Church settings
        /// </summary>
        private struct MyButtons
        {
            internal string Feed;
            internal string Name;
            internal string Title;
            internal string Website;
            internal Drawable Icon;
            internal Drawable Background;
        }

        private Settings _status;
        private MyButtons button;
        private MyButtons btnDefaults;
        
        private bool _closing;

        private string _churches = "http://www.faithjourney.us/wp-content/uploads/2016/12/Churchs.txt";
        private string _castingWeb = "matthew1.1";
        private string _feed = "";
        private string _name = "";
        private string _title = "Sermon Casting - Select your Church"; private string _website = "";
        private Drawable DefaultIcon = null;
        private Drawable DefaultBackground = null;
        private string _activeUrl;
        private string _activeName;
        private string _activePassages;
        private string _lastPassage;
        private string[] _Passages = new string[4];
        private string _activeDate;
        private string _activeVersion = "KJV";
        private string _activeSermon;

        private TimeSpan _activeDuration;

        private int _index = -9999;
        private int _position;
        private int _maxPosition;
        private int _seekto = 0;

        private MediaPlayer _player;
        private AudioManager audioManager;
        private WifiManager wifiManager;
        private WifiManager.WifiLock wifiLock;

        private Button churchButton;
        private Button playButton;
        private Button stopButton;
        private Button settingsButton;
        private Button passageButton;

        private ListView playlist;
        private ListView churchList;

        private TextView titleText;
        private TextView selectedText;
        private TextView currentText;
        private TextView durationText;
        private TextView versionText;

        private SeekBar seekbar;
        private ImageView iconView;
        private LinearLayout titleBar;
        private LinearLayout layoutWeb;
        private LinearLayout layoutList;
        private LinearLayout linearTabs;
        private WebView webview1;

        LoadDetails cl = new LoadDetails();
        LoadRSS ls = new LoadRSS();

        private NotificationManager Notifys;

        #endregion "Variables"

        #region "Main Activty"

        #region "Stoping"

        /// <summary>
        /// Make sure the App closes the player
        /// And closes all Notifications
        /// Can this replace OnBackPressed?
        /// /// </summary>
        protected override void OnDestroy()
        {
            _closing = true;

            if (_player != null)
                _player.Dispose();
            
            if (Notifys != null)
                Notifys.CancelAll();
            base.OnDestroy();
        }

        /// <summary>
        /// Close Notifications
        /// </summary>
        public override void OnBackPressed()
        {
            if (Notifys != null)
                Notifys.CancelAll();
            base.OnBackPressed();
        }

        /// <summary>
        /// Can we move On Focus Changed here?
        /// Show the notification
        /// </summary>
        protected override void OnPause()
        {
            ShowNotification();
            base.OnPause();
        }

        /// <summary>
        /// pause on lost focus
        /// resume on Focus
        /// Might need to add a override option (user preference)
        /// </summary>
        /// <param name="hasFocus"></param>
        public override void OnWindowFocusChanged(bool hasFocus)
        {
            //if (_player != null)
            //{
            //    if (hasFocus)
            //    {
            //        if (_status == Settings.Paused)
            //            ProcessPlay();
            //        if (Notifys != null)
            //            Notifys.CancelAll();
            //    }
            //    else
            //    {
            //        if (_player.IsPlaying)
            //            ProcessPlay();
            //    }
            //}

            base.OnWindowFocusChanged(hasFocus);
        }

        /// <summary>
        /// If we stop
        /// Puase the player
        /// Save the settings
        /// </summary>
        protected override void OnStop()
        {
            saveSettings();
            base.OnStop();
        }

        #endregion "Stoping"

        #region "Starting"

        /// <summary>
        /// This is the start up procedure
        /// Kill any active notifications
        /// Set the content view
        /// Load the Player
        /// Create the Button and control events
        /// set up the view based on the data
        /// </summary>
        /// <param name="bundle"></param>
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            //load the data
            ProcessStartup();
        }

        private void ProcessStartup()
        {
            //load our player
            SetupPlayer();

            //Load our settings
            LoadSettings();

            //Find our audio and notificaton managers
            audioManager = (AudioManager)GetSystemService(AudioService);
            wifiManager = (WifiManager)GetSystemService(WifiService);

            // Get our button from the layout resource, and attach an event to it
            titleBar = FindViewById<LinearLayout>(Resource.Id.TitleBar);
            layoutList = FindViewById<LinearLayout>(Resource.Id.layoutList);
            layoutWeb = FindViewById<LinearLayout>(Resource.Id.layoutWeb);
            linearTabs = FindViewById <LinearLayout>(Resource.Id.linearTabs);

            churchButton = FindViewById<Button>(Resource.Id.churchButton);
            playButton = FindViewById<Button>(Resource.Id.playButton);
            settingsButton = FindViewById<Button>(Resource.Id.settingsButton);

            passageButton = FindViewById<Button>(Resource.Id.passageButton1);

            churchList = FindViewById<ListView>(Resource.Id.churchList);
            playlist = FindViewById<ListView>(Resource.Id.playList);
            stopButton = FindViewById<Button>(Resource.Id.stopButton);

            currentText = FindViewById<TextView>(Resource.Id.currentText);
            durationText = FindViewById<TextView>(Resource.Id.durationText);
            selectedText = FindViewById<TextView>(Resource.Id.selectedText);
            titleText = FindViewById<TextView>(Resource.Id.titleText);
            versionText = FindViewById<TextView>(Resource.Id.versionText);

            iconView = FindViewById<ImageView>(Resource.Id.Icon);
            seekbar = FindViewById<SeekBar>(Resource.Id.seekBar);
            webview1 = FindViewById<WebView>(Resource.Id.webView1);

            webview1.Settings.JavaScriptEnabled = true;
            webview1.Settings.BuiltInZoomControls = true;
            webview1.Settings.SetSupportZoom(true);
            webview1.ScrollBarStyle = Android.Views.ScrollbarStyles.OutsideOverlay;
            webview1.ScrollbarFadingEnabled = false;

            RegisterForContextMenu(settingsButton);
            
            settingsButton.Click += SettingsButton_Click;
            
            playButton.Click += PlayButton_Click;
            stopButton.Click += StopButton_Click;
            passageButton.Click += PassageButton_Click;

            playlist.ItemClick += Playlist_ItemClick;
            churchList.ItemClick += ChurchList_ItemClick;

            titleText.Click += TitleText_Click;
            versionText.Click += VersionText_Click;
            
            seekbar.ProgressChanged += Seekbar_ProgressChanged;
            
            versionText.Text = "Version: " + _activeVersion;

            //Set up the lists
            SetupChurchList(false);

            DefaultIcon = new BitmapDrawable(BitmapFactory.DecodeResource(Resources, Resource.Drawable.adicon));
            DefaultBackground = new BitmapDrawable(BitmapFactory.DecodeResource(Resources, Resource.Drawable.adbackground));

            //setup active church
            SetupActiveChurch();

            //set view
            SetButtons(_status);

            //create this lait enough so it does not fire early
            churchButton.Click += ChurchButton_Click;

            //Store is running flag
            _closing = false;

            //set up last played
            if (LoadRSS.isValidURL(_activeUrl))
                LastPlayed();
        }

        /// <summary>
        /// Update active sermon with last played
        /// </summary>
        private void LastPlayed()
        {
            int id = -1;
            //locate this in the list
            for (int i = 0; i < playlist.Adapter.Count - 1; i++)
            {
                string v = (string)ls.pcUrls.GetItem(i);
                if (v == _activeUrl)
                {
                    id = i;
                    break;
                }
            }
            if (id >= 0)
            {
                playlist.SetItemChecked(id, true);
                //no event is fired as this is still startup
                ProcessItemSelected(id);
            }
        }

        #endregion "Starting"

        #endregion "Main Activty"
        
        #region "Controls Events"

        /// <summary>
        /// Handle passage switching
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PassageButton_Click(object sender, EventArgs e)
        {
            Button bt = (Button)sender;
            int p = (int)bt.Tag;

            if (_Passages[p] != null)
            {
                _activePassages = _Passages[p];
                LoadPassage(_activeVersion, _activePassages, p);
            }

            SetFocusedButton(p);
        }

        /// <summary>
        /// Set the Font on focused passage
        /// </summary>
        /// <param name="p"></param>
        private void SetFocusedButton(int p)
        {
            int count = linearTabs.ChildCount;
            for (int i = 0; i < count; i++)
            {
                View view = linearTabs.GetChildAt(i);
                if (view is Button)
                {
                    Button vw = (Button)view;
                    if (i == p)
                        vw.SetTextColor(Color.DarkGreen);
                    else
                        vw.SetTextColor(playButton.TextColors);
                }
            }
        }

        /// <summary>
        /// Show the menu of version options
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VersionText_Click(object sender, EventArgs e)
        {
            PopupMenu menu = new PopupMenu(this, versionText);
            menu.Inflate(Resource.Menu.menu);

            menu.MenuItemClick += Menu_MenuItemClick;
            menu.Show();
        }

        /// <summary>
        /// Update the Version to the selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_MenuItemClick(object sender, PopupMenu.MenuItemClickEventArgs e)
        {
            //store the old version
            string oldv = _activeVersion.ToString();
            string version = e.Item.ToString();

            if (version != oldv)
            {
                _activeVersion = version;
                versionText.Text = "Version: " + _activeVersion;
                if (layoutWeb.Visibility == ViewStates.Visible)
                    LoadPassage(_activeVersion, _activePassages, 0);
            }
        }

        /// <summary>
        /// Handle the progressbar changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Seekbar_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            if (_player == null)
                return;

            if (e.FromUser)
                _player.SeekTo(seekbar.Progress);
            else
                SetDuration(true);
        }

        /// <summary>
        /// Close the church list selecter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TitleText_Click(object sender, EventArgs e)
        {
            churchList.Adapter = null;
        }

        /// <summary>
        /// Church list item selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChurchList_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            ProcessChurchSelected(e.Position);
        }

        /// <summary>
        /// Show the churches
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsButton_Click(object sender, EventArgs e)
        {
            if (cl.ChurchNames.Count <= 0)
                return;
            if (churchList.Adapter == null)
            {
                churchList.Adapter = cl.ChurchNames;
            }
            else
            {
                churchList.Adapter = null;
            }
        }

        /// <summary>
        /// Show an resume option in notification list
        /// </summary>
        private void ShowNotification()
        {
            // Set up an intent so that tapping the notifications returns to this app:
            Intent intent = new Intent(this, typeof(MainActivity));

            // Create a PendingIntent; we're only using one PendingIntent (ID = 0):
            const int pendingIntentId = 0;
            PendingIntent pendingIntent =
                PendingIntent.GetActivity(this, pendingIntentId, intent, PendingIntentFlags.OneShot);

            // Instantiate the builder and set notification elements, including pending intent:
            Notification.Builder builder = new Notification.Builder(this)
                .SetContentIntent(pendingIntent)
                .SetContentTitle(_title)
                .SetContentText("Click to Resume.")
                .SetSmallIcon(Resource.Drawable.adicon)
                .SetAutoCancel(true);

            // Build the notification:
            Notification notification = builder.Build();
            
            // Get the notification manager:
            Notifys = GetSystemService(Context.NotificationService) as NotificationManager;

            // Publish the notification:
            const int notificationId = 0;
            Notifys.Notify(notificationId, notification);
        }

        /// <summary>
        /// Open the church website
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChurchButton_Click(object sender, EventArgs e)
        {
            if (LoadRSS.isValidURL(_website))
            {
                var uri = Android.Net.Uri.Parse(_website);
                var intent = new Intent(Intent.ActionView, uri);
                StartActivity(intent);
            }
            else
            {
                ShowMessage("Update Details", "Would you like to open the Configuration site?");
            }
        }

        private void ShowMessage(string title, string message)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            // Add the buttons events
            builder.SetPositiveButton("Yes", (Alert_YesClick));
            builder.SetNegativeButton("No", (Alert_NoClick));
            // Set other dialog properties
            builder.SetMessage(message);
            builder.SetTitle(title);
            builder.SetCancelable(false);

            // Create the AlertDialog
            AlertDialog dialog = builder.Create();

            dialog.Show();
        }

        private void Alert_YesClick(object sender, DialogClickEventArgs e)
        {
            var uri = Android.Net.Uri.Parse(_castingWeb);
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void Alert_NoClick(object sender, DialogClickEventArgs e)
        {

        }
        
        /// <summary>
        /// Play list item selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            ProcessItemSelected(e.Position);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopButton_Click(object sender, EventArgs e)
        {
            StopPlayer();
        }

        /// <summary>
        /// Process Play or Pause
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayButton_Click(object sender, EventArgs e)
        {
            ProcessPlay();
        }

        private void ProcessPlay()
        {
            if (_status == Settings.Stopped || _status == Settings.Ready || _status == Settings.Paused)
            {
                StartPlayer();
                if (_status != Settings.Paused)
                {
                    ShowWebview(true);
                    playButton.SetText(Resource.String.Pause);
                }
            }
            else
            {
                if (_player == null)
                    return;
                _player.Pause();
                _status = Settings.Paused;
                playButton.SetText(Resource.String.Play);
            }
        }

        #endregion "ControlsEvents"

        #region UserUpdates

        private void ShowWebview(bool show)
        {
            if (show == true)
            {
                layoutList.Visibility = Android.Views.ViewStates.Gone;
                layoutWeb.Visibility = Android.Views.ViewStates.Visible;
                int count = linearTabs.ChildCount - 1;
                for (int i = count; i >= 0; i--)
                {
                    bool delete= false;
                    View view = linearTabs.GetChildAt(i);
                    if (view is Button)
                    {
                        Button vw = (Button)view;
                        delete = ((string)vw.GetTag(vw.Id) != "passageButton1");
                    }
                    if (delete)
                        linearTabs.RemoveViewAt(i);
                }
                passageButton.Visibility = ViewStates.Gone;
                int p = 0;
                foreach (string s in _Passages)
                {
                    if (s != null || s != "")
                    {
                        Button bt = new Button(this);
                        bt.LayoutParameters = passageButton.LayoutParameters;
                        bt.Id = passageButton.Id + p;
                        bt.Text = "Psg " + (p + 1).ToString();
                        bt.Tag = p;
                        bt.Click += PassageButton_Click;
                        linearTabs.AddView(bt, p);
                        p++;
                    }
                }
                LoadPassage(_activeVersion, _activePassages, 0);
            }
            else
            {
                layoutList.Visibility = ViewStates.Visible;
                layoutWeb.Visibility = ViewStates.Gone;
                webview1.LoadUrl(""); 
            }
        }

        private void LoadPassage(string version, string passages, int page)
        {
            webview1.ClearHistory();
            string url = "https://biblia.com/books/" + version + "/";
            string updatefeed = passages;
            if (!LoadRSS.isValidURL(url + passages.Replace(" ", "")))
            {
                updatefeed = "Passage not found! Update feed: " + _castingWeb;
                url += _castingWeb;
            }
            else
                url += passages.Replace(" ", "");

            webview1.LoadUrl(url);

            SetFocusedButton(page);
            selectedText.Text = _activeSermon + "\n" + updatefeed;
            _lastPassage = webview1.OriginalUrl;
        }

        /// <summary>
        /// Handle the church selected
        /// </summary>
        /// <param name="id"></param>
        private void ProcessChurchSelected(int id)
        {
            //CLear up any active player
            if (_player != null)
                StopPlayer();

            //load the selected church
            _index = cl.SetIndex(id);

            //Store the defaults
            MyButtons btn = btnDefaults;

            btn.Icon = DefaultIcon;

            _name = cl.Name;
            _title = cl.Title;
            _feed = cl.Feed;
            _website = cl.Website;

            button = btn;
            //set the updates
            playlist.Background = btn.Background;
            
            iconView.SetImageURI(Android.Net.Uri.Parse(cl.IconUrl));

            churchButton.Enabled =  true;
            playButton.Enabled = false;
            stopButton.Enabled = false;

            titleText.Text = _title;

            //Set up the sermon list
            SetupSermonList();

            //set view
            SetButtons(Settings.Stopped);

            //close the church list
            if (playlist.Count > 0)
                churchList.Adapter = null;
        }

        /// <summary>
        /// Handle the listview click
        /// </summary>
        /// <param name="id"></param>
        private void ProcessItemSelected(int id, int pos = 0)
        {
            if (ls.pcUrls.Count < 0)
                return;

            _activeUrl = ls.pcUrls.GetItem(id);
            _activeName = ls.pcNames.GetItem(id);
            _activeDuration = TimeSpan.Parse(ls.pcDurations.GetItem(id));
            _activePassages = ls.pcPassages.GetItem(id);
            _Passages = SplitPassages();
            _activeDate = ls.pcDates.GetItem(id);

            if (_activeUrl.Length > 0)
            {
                SetupPlayer();

                //Display the title
                string tmp = _activeName;
                tmp = tmp.Replace("\n", "");
                selectedText.Text = tmp;
                _activeSermon = tmp;
                durationText.Text = FormatTimeSpan(_activeDuration);
                currentText.Text = "0:0";

                //set the buttons ready to play
                SetButtons(Settings.Ready);

                //set position
                if (pos > 0)
                    _player.SeekTo(pos);
            }
            else
            {
                selectedText.Text = "Selected: ";
                _activeSermon = "";
                durationText.Text = "0:0";
                currentText.Text = "0:0";
                SetButtons(Settings.Stopped);
            }
        }

        private string[] SplitPassages()
        {
            string[] split = _activePassages.Split(';');

            if (split[0] == null || split[0] == "")
                split[0] = "John 3.16";
            //string[] results = new string[14];

                //Array.Copy(split, results, Math.Min(split.Length, 4));

                //for (int i = 0; i <= 13; i++)
                //{
                //    if (results[i] == null)
                //        results[i] = "John 3.1" + i.ToString();
                //}

            _activePassages = split[0];
            return split;
        }

        /// <summary>
        /// format the cuurent or duration field
        /// </summary>
        /// <param name="current"></param>
        private void SetDuration(bool current)
        {
            if (_player == null)
                return;
            TimeSpan duration = TimeSpan.FromMilliseconds(_player.Duration);
            if (current)
                duration = TimeSpan.FromMilliseconds(_player.CurrentPosition);
            string vl = FormatTimeSpan(duration);
            if (!current)
                durationText.Text = vl;
            else
                currentText.Text = vl;
        }

        /// <summary>
        /// Format Time Span "0:00", "0:00:00"
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        private string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.Ticks < 0)
                return "0:00";
            string results = new DateTime(ts.Ticks).ToString("mm:ss");
            if (ts.TotalHours > 1)
                results = new DateTime(ts.Ticks).ToString("hh:mm:ss");
            return results;
        }

        /// <summary>
        /// load the list of churches
        /// </summary>
        private void SetupChurchList(bool feedOnly = true)
        {
            MainActivity main = this;

            //Initializer the church list
            cl = new LoadDetails(main, _churches, _title, "//SermonCasting/Church");

            //set the playlist to the adapter items
            churchList.Adapter = cl.ChurchNames;
        }

        /// <summary>
        /// load the list of sermons
        /// </summary>
        private void SetupSermonList()
        {
            MainActivity main = this;

            //Initializer the RSS Feed Class
            ls = new LoadRSS(main, _feed, _title);

            //set the playlist to the adapter items
            playlist.Adapter = ls.pcNames;
        }

        /// <summary>
        /// Control the buttons
        /// </summary>
        /// <param name="status"></param>
        private void SetButtons(Settings status)
        {
            _status = status;
            playlist.Enabled = false;
            switch (status)
            {
                case Settings.Empty:
                    playButton.Text = "Play";
                    playButton.Enabled = false;
                    stopButton.Enabled = false;
                    playlist.Enabled = true;
                    seekbar.Enabled = false;
                    break;
                case Settings.Paused:
                    playButton.Text = "Play";
                    playButton.Enabled = true;
                    stopButton.Enabled = true;
                    seekbar.Enabled = false;
                    break;
                case Settings.Playing:
                    playButton.Text = "Pause";
                    playButton.Enabled = true;
                    stopButton.Enabled = true;
                    seekbar.Enabled = true;
                    break;
                case Settings.Ready:
                    playButton.Text = "Play";
                    playButton.Enabled = true;
                    stopButton.Enabled = false;
                    seekbar.Enabled = false;
                    playlist.Enabled = true;
                    break;
                case Settings.Stopped:
                    playButton.Text = "Play";
                    playButton.Enabled = ItemsSelected();
                    stopButton.Enabled = false;
                    seekbar.Enabled = false;
                    playlist.Enabled = true;
                    break;
                default:
                    break;
            }
        }

        private bool ItemsSelected()
        {
            bool results = playlist.CheckedItemPosition >= 0;
            return results;
        }

        /// <summary>
        /// Start a timer to update progressBar
        /// </summary>
        /// <param name="dueTime"></param>
        private void StartTimer(int dueTime)
        {
            Timer t = new Timer(new TimerCallback(TimerProc));
            t.Change(dueTime, 0);
        }

        /// <summary>
        /// update the progressBar
        /// </summary>
        /// <param name="state"></param>
        private void TimerProc(object state)
        {
            if (_player == null || _closing)
                return;
            // The state object is the Timer object.
            Timer t = (Timer)state;
            // have we stopped
            if (!_player.IsPlaying)
                t.Dispose();
            else
            {
                seekbar.Progress = _player.CurrentPosition;
                _position = seekbar.Progress;
                _maxPosition = seekbar.Progress;
                StartTimer(1000);
            }
        }

        /// <summary>
        /// Spins the Icon to show busy
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="repeatcount"></param>
        private void startAnimation(long duration, int repeatcount)
        {
            // Create an animation
            RotateAnimation rotation = new RotateAnimation(
                0f,
                360f,
                Dimension.RelativeToSelf,
                0.5f,
                Dimension.RelativeToSelf,
                0.5f);
            rotation.Duration = 1200;
            rotation.Interpolator = new LinearInterpolator();
            rotation.RepeatMode = RepeatMode.Restart;
            rotation.RepeatCount = 5;

            FindViewById(Resource.Id.Icon).StartAnimation(rotation);
        }

        #endregion UserUpdates

        #region Player

        /// <summary>
        /// Handles the starting and pausing
        /// </summary>
        private async void StartPlayer(bool sync = true)
        {
            SetupPlayer(); 

            //We are simply paused so just continue again
            if (_status == Settings.Paused)
            {
                _player.Start();
                _status = Settings.Playing;
                return;
            }

            //start an animation to let the user know we are working
            startAnimation(1200, 5);

            //try to load the file
            try
            {
                if (sync)
                {
                    _player.Reset();
                    _player.SetDataSource(_activeUrl);
                }
                else
                    await _player.SetDataSourceAsync(ApplicationContext, Android.Net.Uri.Parse(_activeUrl));
            }
            catch (Exception er)
            {
                Log.Debug(LoadRSS.AppName, "Could not load audio file: " + _activeUrl + " : " + er.Message);
            }

            //set the async
            _player.PrepareAsync();

            //Set the WifiLock
            AquireWifiLock();
        }
        
        /// <summary>
        /// Set up the Active CHurch
        /// </summary>
        private void SetupActiveChurch()
        {
            _website = "";
            //Validate valid church
            if (_index == -999 || _index >= cl.ChurchDetails.Count)
                SetDefualtChurch();
            //Load the Church
            ProcessChurchSelected(_index);
        }

        /// <summary>
        /// Set to default church
        /// </summary>
        private void SetDefualtChurch()
        {
            _index = 0;
            _feed = "";
            _title = "Sermon Casting - Select Church";
            _website = "";
            _activeName = "";
            _activeUrl = "";
            _position = 0;
        }

        /// <summary>
        /// Set up MediaPlayer
        /// </summary>
        private void SetupPlayer()
        {
            if (_player != null)
                return;

            //create it
            _player = new MediaPlayer();

            //Tell our player to stream music
            _player.SetAudioStreamType(Android.Media.Stream.Music);

            //Wake mode will be partial to keep the CPU still running under lock screen
            _player.SetWakeMode(ApplicationContext, WakeLockFlags.Partial);

            //When we have prepared the song start playback
            _player.Prepared += (sender, args) => _player.Start();

            //want to capture player started
            _player.Prepared += Player_Prepared;

            //When we have reached the end of the song stop ourselves, however you could signal next track here.
            _player.Completion += (sender, args) => StopPlayer();

            _player.Error += (sender, args) =>
            {
                //playback error
                Console.WriteLine("Error in playback resetting: " + args.What);
                //this will clean up and reset properly.
                StopPlayer();
            };
            _status = Settings.Empty;
        }

        /// <summary>
        /// Called as _player is prepared
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Player_Prepared(object sender, EventArgs e)
        {
            //set up the buttons
            SetButtons(Settings.Playing);
            if (_player.Duration < 0)
                seekbar.Max = _position;
            else
                seekbar.Max = _player.Duration;
            if (_seekto > 0)
                _player.SeekTo(_seekto);
            _seekto = 0;
            seekbar.Progress = _player.CurrentPosition;
            SetDuration(true);
            SetDuration(false);
            StartTimer(1000);
        }

        /// <summary>
        /// Error logging
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Player_Error(object sender, MediaPlayer.ErrorEventArgs e)
        {
            //playback error
            Log.Debug(LoadRSS.AppName, "Error in playback resetting: " + e.What);
            //this will clean up and reset properly.
            StopPlayer();
        }

        /// <summary>
        /// Stop the player
        /// called by _player when stream ends
        /// </summary>
        private void StopPlayer()
        {
            SetupPlayer();

            //stop and set
            if (_status == Settings.Playing || _status == Settings.Paused)
            {
                _player.Reset();
                _player.Stop();
                _status = Settings.Empty;
            }

            //now clean up the details
            ShowWebview(false);
            _position = seekbar.Progress;
            seekbar.Progress = 0;
            durationText.Text = "0:0";
            currentText.Text = "0:0";
            SetButtons(Settings.Stopped);
        }

        /// <summary>
        /// Lock the wifi
        /// Allows streaming under lock screen
        /// </summary>
        private void AquireWifiLock()
        {
            if (wifiLock == null)
            {
                wifiLock = wifiManager.CreateWifiLock(WifiMode.Full, "xamarin_wifi_lock");
            }
            wifiLock.Acquire();
        }

        /// <summary>
        /// Release the wifi lock
        /// </summary>
        private void ReleaseWifiLock()
        {
            if (wifiLock == null)
                return;

            wifiLock.Release();
            wifiLock = null;
        }

        #endregion

        #region Settings

        /// <summary>
        /// Save the users setting
        /// </summary>
        protected void saveSettings()
        {
            var prefs = Application.Context.GetSharedPreferences("SermonCasting", FileCreationMode.Private);
            var prefEditor = prefs.Edit();
            prefEditor.PutInt("Index", _index);
            prefEditor.PutString("Feed", _feed);
            prefEditor.PutString("Title", _title);
            prefEditor.PutString("Website", _website);
            prefEditor.PutString("ActiveName", _activeName);
            prefEditor.PutString("ActiveVersion", _activeVersion);
            prefEditor.PutString("Active", _activeUrl);
            prefEditor.PutInt("Position", _position);
            prefEditor.PutInt("MaxPosition", _maxPosition);

            prefEditor.Commit();
        }

        /// <summary>
        /// Reload the users settings
        /// </summary>
        protected void LoadSettings()
        {
            var prefs = Application.Context.GetSharedPreferences("SermonCasting", FileCreationMode.Private);
            _index = prefs.GetInt("Index", 0);
            _feed = prefs.GetString("Feed", "");
            _title = prefs.GetString("Title", "Sermon Casting - Select your Church");
            _website = prefs.GetString("Website", "");
            _activeName = prefs.GetString("ActiveName", null);
            _activeVersion = prefs.GetString("ActiveVersion", "KJV");
            _activeUrl = prefs.GetString("Active", null);
            _seekto = prefs.GetInt("Position", 0);
            _position = _seekto;
            _maxPosition = prefs.GetInt("MaxPosition", 0);
            
            btnDefaults.Icon = new BitmapDrawable(Resources, BitmapFactory.DecodeResource(Resources, Resource.Drawable.adicon));
            btnDefaults.Background = new BitmapDrawable(Resources, BitmapFactory.DecodeResource(Resources, Resource.Drawable.adbackground));

            btnDefaults.Feed = _feed;
            btnDefaults.Name = _activeName;
            btnDefaults.Title = _title;
            btnDefaults.Website = _website;

            button = btnDefaults;
        }

        #endregion Settings
    }
}