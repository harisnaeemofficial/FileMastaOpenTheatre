﻿using CButtonLib;
using FileMasta.Bookmarks;
using FileMasta.Controls;
using FileMasta.Extensions;
using FileMasta.Files;
using FileMasta.GitHub;
using FileMasta.Models;
using FileMasta.Utilities;
using FileMasta.Windows;
using FileMasta.Worker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Text;
using System.Windows.Forms;

namespace FileMasta
{
    public partial class MainForm : Form
    {
        public static MainForm Form { get; set; } = null; // This is only instance of the main form, set after InitializeComponent()
        public static SplashScreen FrmSplashScreen { get; } = new SplashScreen(); // Splash Screen (one instance)
        public static FileDetails FrmFileDetails { get; set; } = null; // File Details Window (one instance, set when first shown)
        public static ShortcutsWindow FrmKeyboardShortcuts { get; } = new ShortcutsWindow(); // Keyboard Shortcuts Window (one instance)

        public MainForm()
        {
            Program.log.Info("Initializing");

            // Combines all supported file types into one array
            Types.All.AddRange(Types.Video); Types.All.AddRange(Types.Audio); Types.All.AddRange(Types.Books); Types.All.AddRange(Types.Subtitle); Types.All.AddRange(Types.Torrent); Types.All.AddRange(Types.Software); Types.All.AddRange(Types.Other);

            SelectedFilesType = Types.All; // Set selected files type to All
            InitializeComponent(); // Initialize
            Form = this; // Set this instance
            labelChangeLog.Text = String.Format(labelChangeLog.Text, Application.ProductVersion); // Set this version on Change Log label in the About tab

            // Show Splash Screen
            Controls.Add(FrmSplashScreen);
            FrmSplashScreen.Dock = DockStyle.Fill;
            FrmSplashScreen.Location = new Point(0, 0);
            FrmSplashScreen.ClientSize = ClientSize;
            FrmSplashScreen.BringToFront();
            FrmSplashScreen.Show();

            comboBoxSearchHome.DropDownWidth = ControlExtensions.DropDownWidth(comboBoxSearchHome); // Set home search button combobox to fit its contents
            comboBoxSortFiles.DropDownWidth = ControlExtensions.DropDownWidth(comboBoxSortFiles); // Set files sort combobox to fit its contents

            Program.log.Info("Initialized");
        }


        /*************************************************************************/
        /* Share on Social Media                                                 */
        /*************************************************************************/

        public string shareMessage = "Hey guys, I found a brilliant way to find Direct download links for anything.";

        private void imageShareTwitter_Click(object sender, EventArgs e)
        {
            Process.Start($"https://twitter.com/intent/tweet?hashtags=FileMasta&original_referer=https%3A%2F%2Fgithub.com/HerbL27/FileMasta%2F&ref_src=twsrc%5Etfw&text={shareMessage}&tw_p=tweetbutton&url=https%3A%2F%2Fgithub.com/HerbL27/FileMasta");
        }

        private void imageShareFacebook_Click(object sender, EventArgs e)
        {
            Process.Start("https://facebook.com/sharer/sharer.php?app_id=248335808680372&kid_directed_site=0&sdk=joey&u=http%3A%2F%2Fgithub.com/HerbL27/FileMasta%2F&display=popup&ref=plugin&src=share_button");
        }


        /*************************************************************************/
        /* Form / Startup Events                                                 */
        /*************************************************************************/

        private void MainForm_Load(object sender, EventArgs e)
        {
            Program.log.Info("Starting load events");

            LoadUserSettings();

            CurrentTab = tabHome;
            CurrentTabTitle = titleHome;

            Directory.CreateDirectory(LocalExtensions.pathRoot);
            Directory.CreateDirectory(LocalExtensions.pathData);

            if (LocalExtensions.CheckForInternetConnection())
            {
                BackGroundWorker.RunWorkAsync(() => Updates.CheckForUpdate());
                LoadTopSearches();
                BackGroundWorker.RunWorkAsync(() => Database.UpdateLocalDatabase(), CompletedTasks);
            }
            else
            {
                Controls.Remove(FrmSplashScreen);
                MessageBox.Show(this, "No Internet connection found. You need to be connected to the Internet to use FileMasta. Please check your connection and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Program.log.Info("Completed load events");
        }

        private void CompletedTasks(object sender, RunWorkerCompletedEventArgs e)
        {
            SetDatabaseInfo(); // Get database info and show in form
            Controls.Remove(FrmSplashScreen); // Remove splash screen
            Program.log.Info("All startup tasks completed");
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing | e.CloseReason == CloseReason.ApplicationExitCall)
            {
                Properties.Settings.Default.Save();

                // Delete app Data directory if user settings is true
                if (Properties.Settings.Default.clearDataOnClose)
                    if (Directory.Exists(LocalExtensions.pathData))
                        Directory.Delete(LocalExtensions.pathData, true);
            }

            Program.log.Info("Closing application");
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            Refresh();
        }

        protected override void OnPaint(PaintEventArgs e) { } // Fix paint form 

        /// <summary>
        /// Declare new lists to store database info in variables
        /// </summary>
        public static List<WebFile> FilesOpenDatabase { get; set; } = new List<WebFile>();
        public static List<string> DataOpenDirectories { get; set; } = new List<string>();
        public static string DatabaseInfo { get; set; }


        /*************************************************************************/
        /* Navigation Events/Variables                                           */
        /*************************************************************************/

        /// <summary>
        /// Get/Set current tab/title
        /// </summary>
        public TabPage CurrentTab { get; set; }
        public CButton CurrentTabTitle { get; set; }

        private void titleHome_ClickButtonArea(object sender, MouseEventArgs e)
        {
            tab.SelectedTab = tabHome;
        }

        private void titleSearch_ClickButtonArea(object sender, MouseEventArgs e)
        {
            tab.SelectedTab = tabSearch;
        }

        private void titleDiscover_ClickButtonArea(object sender, MouseEventArgs e)
        {
            tab.SelectedTab = tabDiscover;
        }

        private void titleSubmit_ClickButtonArea(object sender, MouseEventArgs e)
        {
            tab.SelectedTab = tabSubmit;
        }

        private void titleSettings_ClickButtonArea(object sender, MouseEventArgs e)
        {
            tab.SelectedTab = tabSettings;
        }

        private void titleInformation_ClickButtonArea(object sender, MouseEventArgs e)
        {
            tab.SelectedTab = tabInformation;
        }

        private void tab_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Sets current tab and selects appropriate tab title
            if (tab.SelectedTab == tabHome)
            { CurrentTab = tabHome; ControlExtensions.SelectTabTitle(titleHome); }
            else if (tab.SelectedTab == tabSearch)
            { CurrentTab = tabSearch; ControlExtensions.SelectTabTitle(titleSearch); }
            else if (tab.SelectedTab == tabDiscover)
            { CurrentTab = tabDiscover; ControlExtensions.SelectTabTitle(titleDiscover); ShowHosts(); }
            else if (tab.SelectedTab == tabSubmit)
            { CurrentTab = tabDiscover; ControlExtensions.SelectTabTitle(titleSubmit); }
            else if (tab.SelectedTab == tabSettings)
            { CurrentTab = tabSettings; ControlExtensions.SelectTabTitle(titleSettings); LoadUserSettings(); }
            else if (tab.SelectedTab == tabInformation)
            { CurrentTab = tabInformation; ControlExtensions.SelectTabTitle(titleInformation); tabBlank.Controls.Clear(); }
        }

        /*************************************************************************/
        /* Home Tab                                                              */
        /*************************************************************************/

        /// <summary>
        /// Set database to UI
        /// </summary>
        public void SetDatabaseInfo()
        {
            labelDatabaseStats.Text = String.Format(labelDatabaseStats.Text, TextExtensions.GetFormattedNumber(FilesOpenDatabase.Count.ToString()), TextExtensions.BytesToString(Database.GetTotalFileSize()), TextExtensions.GetFormattedNumber(DataOpenDirectories.Count.ToString()));
            labelDatabaseUpdatedDate.Text = String.Format(labelDatabaseUpdatedDate.Text, Database.GetLastUpdateDate().ToShortDateString());
        }

        /// <summary>
        /// Powered by the HackerTarget API to get Top Searches from FileChef.com
        /// </summary>
        delegate void LoadTopSearchesCallBack();
        private void LoadTopSearches()
        {
            try
            {
                Program.log.Info("Processing top searches");

                BackGroundWorker.RunWorkAsync<List<string>>(() => GetTopSearches(), (data) =>
                {
                    if (tabHome.InvokeRequired)
                    {
                        var b = new LoadTopSearchesCallBack(LoadTopSearches);
                        Invoke(b, new object[] { });
                    }
                    else
                    {
                        int count = 0;
                        foreach (var tag in data)
                            if (count <= 100)
                            {
                                flowLayoutTopSearches.Controls.Add(ControlExtensions.AddTopSearchTag(tag, count));
                                count++;
                            }

                        // Credits Label at the end of Top Searches
                        var a = new Label
                        {
                            Text = "Powered by FileChef",
                            Font = new Font(buttonFileType.Font.Name, 9, FontStyle.Regular),
                            BackColor = Color.Transparent,
                            ForeColor = Color.LightGray,
                            Margin = new Padding(0, 8, 0, 3),
                            Cursor = Cursors.Hand,
                            Name = "btnFileChef",
                            AutoSize = true,
                        };

                        a.Click += buttonFileChef_Click;
                        flowLayoutTopSearches.Controls.Add(a);
                        Program.log.Info("Processed top searches");
                    }
                });
            }
            catch (Exception ex) { labelTopSearches.Visible = false; splitterHeaderTopSearches.Visible = false; flowLayoutTopSearches.Visible = false; Program.log.Error("Error getting top searches", ex); } /* Error occurred, so hide controls/skip... */
        }

        private List<string> GetTopSearches()
        {
            List<string> listTopSearches = new List<string>();
            var client = new WebClient();
            using (var stream = client.OpenRead(Database.UrlTopSearches))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    listTopSearches.Add(line);
            }
            listTopSearches.Reverse();
            return listTopSearches;
        }

        private void buttonFileChef_Click(object Sender, EventArgs e)
        {
            Process.Start("http://filechef.com/");
        }

        // Filetype to search from Home tab
        private void buttonHomeFileType_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            comboBoxFileType.DroppedDown = true;
        }

        private void comboBoxHomeFileType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxFileType.SelectedIndex == -1) SelectedFilesType = Types.All;
            else if (comboBoxFileType.SelectedIndex == 0) SelectedFilesType = Types.All;
            else if (comboBoxFileType.SelectedIndex == 1) SelectedFilesType = Types.Video;
            else if (comboBoxFileType.SelectedIndex == 2) SelectedFilesType = Types.Audio;
            else if (comboBoxFileType.SelectedIndex == 3) SelectedFilesType = Types.Books;
            else if (comboBoxFileType.SelectedIndex == 4) SelectedFilesType = Types.Subtitle;
            else if (comboBoxFileType.SelectedIndex == 5) SelectedFilesType = Types.Torrent;
            else if (comboBoxFileType.SelectedIndex == 6) SelectedFilesType = Types.Software;
            else if (comboBoxFileType.SelectedIndex == 7) SelectedFilesType = Types.Other;

            var startText = buttonFileType.Text.Split(':');
            buttonFileType.Text = startText[0] + ": " + comboBoxFileType.GetItemText(comboBoxFileType.SelectedItem);
            containerFileType.Width = ControlExtensions.GetPanelComboBoxWidth(buttonFileType);
        }

        /// <summary>
        /// External search engine URLs
        /// </summary>
        public static string exploreUrlGoogle = "https://google.com/search?q=";
        public static string exploreUrlGoogol = "https://googol.warriordudimanche.net/?q=";
        public static string exploreUrlStartpage = "https://startpage.com/do/dsearch?query=";
        public static string exploreUrlSearx = "https://searx.me/?q=";

        /// <summary>
        /// Selected external search
        /// </summary>
        public static string SelectedFilesSearch { get; set; }

        private void comboBoxSearchHome_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxSearchHome.SelectedIndex == 0 | comboBoxSearchHome.SelectedIndex == -1)
                SelectedFilesSearch = exploreUrlGoogle;
            else if (comboBoxSearchHome.SelectedIndex == 1)
                SelectedFilesSearch = exploreUrlGoogol;
            else if (comboBoxSearchHome.SelectedIndex == 2)
                SelectedFilesSearch = exploreUrlStartpage;
            else if (comboBoxSearchHome.SelectedIndex == 3)
                SelectedFilesSearch = exploreUrlSearx;

            Process.Start(SelectedFilesSearch + String.Format("{0} %2B({1}) %2Dinurl:(jsp|pl|php|html|aspx|htm|cf|shtml) intitle:index.of %2Dinurl:(listen77|mp3raid|mp3toss|mp3drug|index_of|index-of|wallywashis|downloadmana)", textBoxSearchHome.Text, String.Join("|", SelectedFilesType.ToArray()).ToLower()));
        }

        private void bgSearchFilesHome_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            textBoxSearchHome.Focus();
        }

        private void textBoxSearchFilesHome_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                DoSearchFilesFromHome();
        }

        private void buttonSearchHome_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            DoSearchFilesFromHome();
        }

        private void buttonSearchHome_SideImageClicked(object Sender, MouseEventArgs e)
        {
            comboBoxSearchHome.DroppedDown = true;
        }

        public void DoSearchFilesFromHome()
        {
            textBoxSearchFiles.Text = textBoxSearchHome.Text;

            if (comboBoxFileType.SelectedIndex == -1) { SelectedFilesType = Types.All; ControlExtensions.SelectFilesTab(buttonFilesAll); SelectedFiles = FilesOpenDatabase; tab.SelectedTab = tabSearch; ShowFiles(SelectedFiles); }
            else if (comboBoxFileType.SelectedIndex == 0) { SelectedFilesType = Types.All; ControlExtensions.SelectFilesTab(buttonFilesAll); SelectedFiles = FilesOpenDatabase; tab.SelectedTab = tabSearch; ShowFiles(SelectedFiles); }
            else if (comboBoxFileType.SelectedIndex == 1) { SelectedFilesType = Types.Video; ControlExtensions.SelectFilesTab(buttonFilesVideo); SelectedFiles = FilesOpenDatabase; tab.SelectedTab = tabSearch; ShowFiles(SelectedFiles); }
            else if (comboBoxFileType.SelectedIndex == 2) { SelectedFilesType = Types.Audio; ControlExtensions.SelectFilesTab(buttonFilesAudio); SelectedFiles = FilesOpenDatabase; tab.SelectedTab = tabSearch; ShowFiles(SelectedFiles); }
            else if (comboBoxFileType.SelectedIndex == 3) { SelectedFilesType = Types.Books; ControlExtensions.SelectFilesTab(buttonFilesBooks); SelectedFiles = FilesOpenDatabase; tab.SelectedTab = tabSearch; ShowFiles(SelectedFiles); }
            else if (comboBoxFileType.SelectedIndex == 4) { SelectedFilesType = Types.Subtitle; ControlExtensions.SelectFilesTab(buttonFilesSubtitles); SelectedFiles = FilesOpenDatabase; tab.SelectedTab = tabSearch; ShowFiles(SelectedFiles); }
            else if (comboBoxFileType.SelectedIndex == 5) { SelectedFilesType = Types.Torrent; ControlExtensions.SelectFilesTab(buttonFilesTorrents); SelectedFiles = FilesOpenDatabase; tab.SelectedTab = tabSearch; ShowFiles(SelectedFiles); }
            else if (comboBoxFileType.SelectedIndex == 7) { SelectedFilesType = Types.Software; ControlExtensions.SelectFilesTab(buttonFilesSoftware); SelectedFiles = FilesOpenDatabase; tab.SelectedTab = tabSearch; ShowFiles(SelectedFiles); }
            else if (comboBoxFileType.SelectedIndex == 8) { SelectedFilesType = Types.Other; ControlExtensions.SelectFilesTab(buttonFilesOther); SelectedFiles = FilesOpenDatabase; tab.SelectedTab = tabSearch; ShowFiles(SelectedFiles); }
        }

        /*************************************************************************/
        /* Search Tab                                                            */
        /*************************************************************************/

        // Filter Preferences
        public static List<WebFile> SelectedFiles { get; set; } = FilesOpenDatabase;
        public Query.SortBy SelectedFilesSort { get; set; } = Query.SortBy.Name;
        public List<string> SelectedFilesType { get; set; } = Types.All;
        public string SelectedFilesHost { get; set; } = "";

        // Select/Click File Type title
        private void buttonFilesAll_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            SelectedFilesType = Types.All; ControlExtensions.SelectFilesTab(buttonFilesAll); SelectedFiles = FilesOpenDatabase; ShowFiles(SelectedFiles);
        }

        private void buttonFilesVideo_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            SelectedFilesType = Types.Video; ControlExtensions.SelectFilesTab(buttonFilesVideo); SelectedFiles = FilesOpenDatabase; ShowFiles(SelectedFiles);
        }

        private void buttonFilesAudio_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            SelectedFilesType = Types.Audio; ControlExtensions.SelectFilesTab(buttonFilesAudio); SelectedFiles = FilesOpenDatabase; ShowFiles(SelectedFiles);
        }

        private void buttonFilesBooks_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            SelectedFilesType = Types.Books; ControlExtensions.SelectFilesTab(buttonFilesBooks); SelectedFiles = FilesOpenDatabase; ShowFiles(SelectedFiles);
        }

        private void buttonFilesSubtitles_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            SelectedFilesType = Types.Subtitle; ControlExtensions.SelectFilesTab(buttonFilesSubtitles); SelectedFiles = FilesOpenDatabase; ShowFiles(SelectedFiles);
        }

        private void buttonFilesTorrents_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            SelectedFilesType = Types.Torrent; ControlExtensions.SelectFilesTab(buttonFilesTorrents); SelectedFiles = FilesOpenDatabase; ShowFiles(SelectedFiles);
        }

        private void buttonFilesSoftware_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            SelectedFilesType = Types.Software; ControlExtensions.SelectFilesTab(buttonFilesSoftware); SelectedFiles = FilesOpenDatabase; ShowFiles(SelectedFiles);
        }

        private void buttonFilesOther_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            SelectedFilesType = Types.Other; ControlExtensions.SelectFilesTab(buttonFilesOther); SelectedFiles = FilesOpenDatabase; ShowFiles(SelectedFiles);
        }

        private void buttonFilesCustom_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            string getUserResponse = Microsoft.VisualBasic.Interaction.InputBox("File Type/Extensions (enter only one extension):", "Custom*", "e.g. MP4");
            string userResponse = getUserResponse.Replace(".", "");
            if (userResponse != "")
                SelectedFilesType = new List<string>() { userResponse.ToUpper() }; ControlExtensions.SelectFilesTab(buttonFilesCustom); SelectedFiles = FilesOpenDatabase; ShowFiles(SelectedFiles);
        }

        private void buttonFilesLocal_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            SelectedFilesType = Types.All; ControlExtensions.SelectFilesTab(buttonFilesLocal); SelectedFiles = LocalExtensions.GetLocalFiles(); ShowFiles(SelectedFiles);
        }

        private void buttonFilesBookmarks_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            SelectedFilesType = Types.All; ControlExtensions.SelectFilesTab(buttonFilesBookmarks); SelectedFiles = UserBookmarks.GetBookmarks(); ShowFiles(SelectedFiles);
        }

        // Files dataGridView click event for row, will get the last (hidden) column (URL) in the selected row and use to get WebFile properties
        private void dataGridFiles_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex != -1)
            {
                var URL = dataGridFiles.CurrentRow.Cells[5].Value.ToString();

                if (dataGridFiles.CurrentRow.Cells[4].Value.ToString() == "Local")
                    ShowFileDetails(new WebFile(Path.GetExtension(URL).Replace(".", "").ToUpper(), Path.GetFileNameWithoutExtension(new Uri(URL).LocalPath), new FileInfo(URL).Length, File.GetCreationTime(URL), URL, URL));
                else
                    ShowFileDetails(Database.FileInfoFromURL(URL));
            }
        }

        private void dataGridFiles_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            e.PaintParts &= ~DataGridViewPaintParts.Focus;
        }

        delegate void loadFilesCallBack(List<WebFile> dataFiles);
        public void ShowFiles(List<WebFile> dataFiles)
        {
            EnableSearchControls(false);
            var stopWatch = new Stopwatch();
            Program.log.InfoFormat("Processing files query. Preferences - Files: {0}, Sort: {1}, Type: {2}, Host: {3}", SelectedFiles.ToString(), SelectedFilesSort.ToString(), SelectedFilesType.ToString(), SelectedFilesHost);
            imageSearchFiles.Image = Properties.Resources.loader;
            BackGroundWorker.RunWorkAsync<List<WebFile>>(() => Query.Search(dataFiles, textBoxSearchFiles.Text, SelectedFilesSort), (data) =>
            {
                if (tabSearch.InvokeRequired)
                {
                    var b = new loadFilesCallBack(ShowFiles);
                    Invoke(b, new object[] { dataFiles });
                }
                else
                {
                    dataGridFiles.Rows.Clear();
                    comboBoxFilterFiles.Items.Clear(); comboBoxFilterFiles.Items.Add("Any");
                    stopWatch.Start();
                    foreach (var jsonData in data)
                    {
                        if (SelectedFilesType.Contains(jsonData.Type) && jsonData.Host.Contains(SelectedFilesHost))
                        {
                            dataGridFiles.Rows.Add(jsonData.Type, jsonData.Name, TextExtensions.BytesToString(jsonData.Size), TextExtensions.GetTimeAgo(jsonData.DateUploaded), jsonData.Host, jsonData.URL);
                            if (!(comboBoxFilterFiles.Items.Contains(jsonData.Host)))
                                comboBoxFilterFiles.Items.Add(jsonData.Host);
                        }
                    }

                    stopWatch.Stop(); TimeSpan ts = stopWatch.Elapsed;
                    labelResultsInfo.Text = TextExtensions.GetFormattedNumber(dataGridFiles.Rows.Count.ToString()) + " / " + TextExtensions.GetFormattedNumber(dataFiles.Count.ToString()) + " Files (" + String.Format("{0:0.000}", ts.TotalSeconds) + " Seconds)"; stopWatch.Reset();

                    tab.SelectedTab = CurrentTab;

                    comboBoxFilterFiles.DropDownWidth = ControlExtensions.DropDownWidth(comboBoxFilterFiles);

                    imageSearchFiles.Image = Properties.Resources.magnify_orange;

                    if (dataGridFiles.Rows.Count == 0) labelNoResultsFound.Visible = true;
                    else labelNoResultsFound.Visible = false;

                    EnableSearchControls(true);

                    Program.log.Info("Successfully returned search results");
                }
            });
        }

        /// <summary>
        /// Enable/Disable search controls, to prevent another search process which causes a crash
        /// </summary>
        /// <param name="isEnabled">Enable/Disable Controls</param>
        private void EnableSearchControls(bool isEnabled)
        {
            textBoxSearchHome.Enabled = isEnabled;
            buttonFileType.Enabled = isEnabled;
            buttonSearchHome.Enabled = isEnabled;
            comboBoxFileType.Enabled = isEnabled;

            foreach (var ctrl in flowLayoutTopSearches.Controls)
                if (ctrl is CButton)
                    ((CButton)ctrl).Enabled = isEnabled;

            textBoxSearchFiles.Enabled = isEnabled;
            buttonSortFiles.Enabled = isEnabled;
            comboBoxSortFiles.Enabled = isEnabled;
            buttonFilterFiles.Enabled = isEnabled;
            comboBoxFilterFiles.Enabled = isEnabled;

            buttonFilesAll.Enabled = isEnabled;
            buttonFilesVideo.Enabled = isEnabled;
            buttonFilesAudio.Enabled = isEnabled;
            buttonFilesBooks.Enabled = isEnabled;
            buttonFilesSoftware.Enabled = isEnabled;
            buttonFilesTorrents.Enabled = isEnabled;
            buttonFilesSubtitles.Enabled = isEnabled;
            buttonFilesOther.Enabled = isEnabled;
            buttonFilesCustom.Enabled = isEnabled;
            buttonFilesLocal.Enabled = isEnabled;
            buttonFilesBookmarks.Enabled = isEnabled;
        }

        /// <summary>
        /// Show details/info for a WebFile
        /// </summary>
        /// <param name="File">WebFile object</param>
        /// <param name="createNewInstance">Whether to create a new instance</param>
        public void ShowFileDetails(WebFile File, bool createNewInstance = true)
        {
            Program.log.Info("Showing file details dialog : " + File.URL);

            if (createNewInstance)
                FrmFileDetails = new FileDetails();

            FrmFileDetails.currentFile = File;
            FrmFileDetails.infoFileName.Text = File.Name;
            FrmFileDetails.infoName.Text = File.Name;
            FrmFileDetails.infoReferrer.Text = File.Host;
            FrmFileDetails.infoType.Text = File.Type;
            FrmFileDetails.infoFileURL.Text = Uri.UnescapeDataString(File.URL);

            // Builds parts of the URL into a better presented string, simply replaces '/' with '>' and no filename
            var url = new Uri(File.URL);
            var directories = new StringBuilder();
            if (!File.URL.StartsWith(LocalExtensions.userDownloadsDirectory)) { directories.Append(File.Host); } else { FrmFileDetails.infoReferrer.Text = "Local"; }// Appends the Host at the start if not Local, else it will be named 'Local'
                foreach (string path in url.LocalPath.Split('/', '\\'))
                if (!Path.HasExtension(path))
                    directories.Append(path + "> ");
            FrmFileDetails.infoDirectory.Text = directories.ToString();

            FrmFileDetails.infoSize.Text = TextExtensions.BytesToString(File.Size);
            FrmFileDetails.infoAge.Text = TextExtensions.GetTimeAgo(File.DateUploaded);

            FrmFileDetails.Dock = DockStyle.Fill;
            if (!createNewInstance) FrmFileDetails.CheckFileEvents();
            if (createNewInstance) { tabBlank.Controls.Clear(); tabBlank.Controls.Add(FrmFileDetails); }
            tab.SelectedTab = tabBlank;

            Program.log.Info("Successfully loaded file details dialog");
        }

        // Sort Files
        private void buttonFilesSort_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            comboBoxSortFiles.DroppedDown = true;
        }

        private void comboBoxFilesSort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxSortFiles.SelectedIndex == 0)
                SelectedFilesSort = Query.SortBy.Name;
            else if (comboBoxSortFiles.SelectedIndex == 1)
                SelectedFilesSort = Query.SortBy.Size;
            else if (comboBoxSortFiles.SelectedIndex == 2)
                SelectedFilesSort = Query.SortBy.Date;

            var startText = buttonSortFiles.Text.Split(':');
            buttonSortFiles.Text = startText[0] + ": " + comboBoxSortFiles.GetItemText(comboBoxSortFiles.SelectedItem);
            flowLayoutSortFiles.Width = ControlExtensions.GetPanelComboBoxWidth(buttonSortFiles);
        }

        // Filter Files by Host
        private void buttonFilesHost_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            comboBoxFilterFiles.DroppedDown = true;
        }

        private void comboBoxFilesHost_SelectedIndexChanged(object sender, EventArgs e)
        {
            var startText = buttonFilterFiles.Text.Split(':');
            buttonFilterFiles.Text = startText[0] + ": " + comboBoxFilterFiles.GetItemText(comboBoxFilterFiles.SelectedItem);
            flowLayoutFilterFiles.Width = ControlExtensions.GetPanelComboBoxWidth(buttonFilterFiles);
            Refresh();

            comboBoxFilterFiles.DropDownWidth = ControlExtensions.DropDownWidth(comboBoxFilterFiles);

            if (comboBoxFilterFiles.SelectedIndex == 0)
                SelectedFilesHost = "";
            else
                SelectedFilesHost = comboBoxFilterFiles.SelectedItem.ToString();
        }

        private void bgSearchFiles_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            textBoxSearchFiles.Focus();
        }

        private void textBoxSearchFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                SearchFilesFromTextBox();
        }

        private void imgSearch_Click(object sender, EventArgs e)
        {
            SearchFilesFromTextBox();
        }

        public void SearchFilesFromTextBox()
        {
            imageSearchFiles.Image = Properties.Resources.loader;
            tab.SelectedTab = tabSearch;

            if (Path.HasExtension(textBoxSearchFiles.Text))
            {
                ShowFileDetails(Database.FileInfoFromURL(textBoxSearchFiles.Text));
                imageSearchFiles.Image = Properties.Resources.magnify_orange;
            }
            else
                ShowFiles(SelectedFiles);
        }

        /*************************************************************************/
        /* Discover Tab                                                          */
        /*************************************************************************/

        private void dataGridDiscover_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            Process.Start(dataGridDiscover.CurrentRow.Cells[3].Value.ToString());
        }

        delegate void loadHostsCallBack();
        private void ShowHosts()
        {
            Program.log.Info("Getting file hosts for Discover tab");

            BackGroundWorker.RunWorkAsync<List<string>>(() => GetFileHosts(), (data) => {
                if (tabDiscover.InvokeRequired)
                {
                    var b = new loadHostsCallBack(ShowHosts);
                    Invoke(b, new object[] { });
                }
                else
                {
                    dataGridDiscover.Rows.Clear();

                    int count = 1;
                    foreach (string url in data)
                    {
                        dataGridDiscover.Rows.Add(count.ToString(), url, "Web", url);
                        count += 1;
                    }

                    tab.SelectedTab = CurrentTab;
                }
            });

            Program.log.Info("Successfully loaded hosts/servers");
        }

        object loadHostsLock = new object();
        private List<string> GetFileHosts()
        {
            lock (loadHostsLock)
            {
                List<string> urls = new List<string>();
                foreach (string file in DataOpenDirectories)
                    if (!urls.Contains(new Uri(file.Replace("www.", "")).GetLeftPart(UriPartial.Scheme) + new Uri(file.Replace("www.", "")).Authority))
                        urls.Add(new Uri(file.Replace("www.", "")).GetLeftPart(UriPartial.Scheme) + new Uri(file.Replace("www.", "")).Authority);

                return urls;
            }
        }

        /*************************************************************************/
        /* Submit Tab                                                            */
        /*************************************************************************/

        // Submit URL button
        private void buttonSubmitUrl_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            // If textbox isn't blank, isn't a file and is a uri string, open new issue template on GitHub
            if (textBoxSubmitLink.Text != "")
                if (!Path.HasExtension(textBoxSubmitLink.Text))
                    if (Uri.IsWellFormedUriString(textBoxSubmitLink.Text, UriKind.Absolute))
                    {
                        // Add a '/' if not present
                        var formattedText = textBoxSubmitLink.Text;
                        if (!textBoxSubmitLink.Text.EndsWith("/"))
                            formattedText = textBoxSubmitLink.Text + "/";
                        OpenLink.SubmitLink(new Uri(formattedText)); textBoxSubmitLink.Text = "";
                    }
                    else MessageBox.Show(this, "This isn't a public web directory.");
                else MessageBox.Show(this, "This isn't a public web directory.");
        }

        /*************************************************************************/
        /* Settings Tab                                                          */
        /*************************************************************************/

        /// <summary>
        /// Set UI Properties
        /// </summary>
        public void LoadUserSettings()
        {
            checkBoxClearDataOnClose.Checked = Properties.Settings.Default.clearDataOnClose;
        }

        // Save settings button
        private void buttonSettingsSave_ClickButtonArea(object sender, MouseEventArgs e)
        {
            // Save users settings to Properties.Settings.Default
            Properties.Settings.Default.clearDataOnClose = checkBoxClearDataOnClose.Checked;
            Thread.Sleep(500);
            Properties.Settings.Default.Save();
            Thread.Sleep(500);

            Program.log.Info("Saaved user settings");
        }

        // Restore settings button
        private void buttonSettingsRestoreDefault_ClickButtonArea(object Sender, MouseEventArgs e)
        {
            // Set the default settings manually here
            Properties.Settings.Default.clearDataOnClose = false;
            Thread.Sleep(500);
            LoadUserSettings();
            Thread.Sleep(500);

            Program.log.Info("Restored user settings");
        }
        
        /*************************************************************************/
        /* Information Tab                                                       */
        /*************************************************************************/

        // Report Issue button
        private void labelReportIssue_Click(object sender, EventArgs e)
        {
            Process.Start($"{OpenLink.urlGitHub}issues/new");
        }

        // Terms of Use button
        private void buttonTermsOfUse_Click(object sender, EventArgs e)
        {
            ControlExtensions.ShowDataWindow(((Label)sender).Text, Resources.UrlTermsOfUse);
        }

        // Privacy Policy button
        private void buttonPrivacyPolicy_Click(object sender, EventArgs e)
        {
            ControlExtensions.ShowDataWindow(((Label)sender).Text, Resources.UrlPrivacyPolicy);
        }

        // Keyboard Shortcuts button
        private void labelKeyboardShortcuts_Click(object sender, EventArgs e)
        {
            FrmKeyboardShortcuts.ShowDialog(this);
        }

        // Change Log button
        private void labelChangeLog_Click(object sender, EventArgs e)
        {
            ControlExtensions.ShowDataWindow(((Label)sender).Text, Resources.UrlChangeLog);
        }

        /*************************************************************************/
        /* Keyboard Shortcuts                                                    */
        /*************************************************************************/

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                // Focus on Search Box
                case Keys.Control | Keys.F:
                    if (CurrentTab == tabHome) textBoxSearchHome.Focus();
                    else tab.SelectedTab = tabSearch; textBoxSearchFiles.Focus();
                    return true;
                // Show Shortcuts Window
                case Keys.Control | Keys.OemQuestion:
                    FrmKeyboardShortcuts.ShowDialog(this);
                    return true;
                // Navigate Tabs
                case Keys.Control | Keys.Right:
                    if (tab.SelectedIndex != tab.TabPages.Count - 2) { if (tab.SelectedIndex == 6) { tab.SelectedIndex = 2; } else { tab.SelectedIndex++; } }
                    return true;
                case Keys.Control | Keys.Left:
                    if (tab.SelectedIndex != 0) { if (tab.SelectedIndex == 6) { tab.SelectedIndex = 0; } else { tab.SelectedIndex--; } }
                    return true;
                // Close File Details if open
                case Keys.Escape:
                    if (FrmFileDetails != null)
                    {
                        tab.SelectedTab = CurrentTab;
                        FrmFileDetails.Dispose();
                    }
                    return true;
                // Close application
                case Keys.Control | Keys.W:
                    Application.Exit();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}