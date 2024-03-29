/*
Copyright (C) 2008 Dominik Laufer

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using ActivityPicturePlugin.Helper;
using ActivityPicturePlugin.Properties;
using ActivityPicturePlugin.Settings;
#if !ST_2_1
using ActivityPicturePlugin.UI.MapLayers;
#endif
using ZoneFiveSoftware.Common.Data.Fitness;
using ZoneFiveSoftware.Common.Data;
using ZoneFiveSoftware.Common.Visuals;
using com.drew.metadata.exif;

using System.Globalization;

namespace ActivityPicturePlugin.UI
{
    public partial class ImportControl : UserControl
    {
        #region public methods
        public ImportControl()
        {
            InitializeComponent();
            InitComponent();

            this.ActivityImagesChanged += new ActivityImagesChangedEventHandler( ImportControl_ActivityImagesChanged );
            treeViewActivities.TreeViewNodeSorter = new NodeSorter();
            this.m_CheckedDirNodes = new List<TreeNode>();
            this.m_ActivityNodes = new List<TreeNode>();
        }

        public void InitComponent()
        {
            ImportControl_Resize( this, new EventArgs() );
            m_viewActivity = ActivityPicturePlugin.Source.Settings.ActivityView; //View of ListviewAct
            m_viewFolder = ActivityPicturePlugin.Source.Settings.FolderView;	 //View of ListviewDrive

            if ( !m_showallactivities )
            {
                splitContainer1.SplitterDistance = Source.Settings.ImportSplitter1Offset > splitContainer1.Width ? splitContainer1.Width : Source.Settings.ImportSplitter1Offset;
                splitContainer2.SplitterDistance = Source.Settings.ImportSplitter2Offset > splitContainer2.Height ? splitContainer2.Height : Source.Settings.ImportSplitter2Offset;
                splitContainer3.SplitterDistance = Source.Settings.ImportSplitter3Offset > splitContainer3.Height ? splitContainer3.Height : Source.Settings.ImportSplitter3Offset;
            }
            else
            {
                splitContainer1.SplitterDistance = Source.Settings.SettingsSplitter1Offset > splitContainer1.Width ? splitContainer1.Width : Source.Settings.SettingsSplitter1Offset;
                splitContainer2.SplitterDistance = Source.Settings.SettingsSplitter2Offset > splitContainer2.Height ? splitContainer2.Height : Source.Settings.SettingsSplitter2Offset;
                splitContainer3.SplitterDistance = Source.Settings.SettingsSplitter3Offset > splitContainer3.Height ? splitContainer3.Height : Source.Settings.SettingsSplitter3Offset;
            }

            colDImage.Width = Source.Settings.ListDriveColThumbnailWidth;
            colDDateTime.Width = Source.Settings.ListDriveColDateTimeWidth;
            colDGPS.Width = Source.Settings.ListDriveColGPSWidth;
            colDTitle.Width = Source.Settings.ListDriveColTitleWidth;
            colDDescription.Width = Source.Settings.ListDriveColCommentWidth;

            colImage.Width = Source.Settings.ListActColThumbnailWidth;
            colDateTime.Width = Source.Settings.ListActColDateTimeWidth;
            colGPS.Width = Source.Settings.ListActColGPSWidth;
            colTitle.Width = Source.Settings.ListActColTitleWidth;
            colDescription.Width = Source.Settings.ListActColCommentWidth;

            this.listViewDrive.View = (View)( m_viewFolder );
            this.listViewAct.View = (View)( m_viewActivity );
            this.HideProgressBar();
        }

        public void UpdateUICulture( System.Globalization.CultureInfo culture )
        {
            using ( Graphics g = this.CreateGraphics() )
            {
                this.btnChangeFolderView.Text = Resources.ImportControl_changeView;
                this.btnChangeFolderView.Width = (int)g.MeasureString( this.btnChangeFolderView.Text, this.btnChangeFolderView.Font ).Width + 10; ;
                this.toolTip1.SetToolTip( this.btnChangeFolderView, Resources.ChangeFileListview_Text );
                this.toolTip1.SetToolTip( this.btnChangeActivityView, Resources.ChangeImageListview_Text );
                this.btnScan.Text = Resources.ActionImport_Text;
                this.toolTip1.SetToolTip( this.btnScan, Resources.AutoImportFromFolders_Text );
                this.btnScan.Width = (int)g.MeasureString( this.btnScan.Text, this.btnScan.Font ).Width + 10; ;
                this.btnScan.Left = btnChangeFolderView.Location.X + btnChangeFolderView.Width + 10;
                this.btnChangeActivityView.Text = Resources.ImportControl_changeView;
                this.btnChangeActivityView.Width = (int)g.MeasureString( this.btnChangeActivityView.Text, this.btnChangeActivityView.Font ).Width + 10; ;

                if ( m_showallactivities )
                {
                    this.btnExpandAll.Text = Resources.btnExpandAll_Text;
                    this.btnExpandAll.Left = btnChangeActivityView.Location.X + btnChangeActivityView.Width + 10;
                    this.btnCollapseAll.Text = Resources.btnCollapseAll_Text;
                    this.btnExpandAll.Visible = true;
                    this.btnCollapseAll.Visible = true;
                }
                else
                {
                    // btnExpandAll and btnCollapseAll are not very useful
                    // in Detail View and just add clutter to an already small
                    // display.  Either change their text or make them invisible
                    // Decided to do both.
                    this.btnExpandAll.Text = "+";
                    this.toolTip1.SetToolTip( this.btnExpandAll, Resources.btnExpandAll_Text );
                    this.btnCollapseAll.Text = "-";
                    this.toolTip1.SetToolTip( this.btnCollapseAll, Resources.btnCollapseAll_Text );
                    this.btnExpandAll.Visible = false;
                    this.btnCollapseAll.Visible = false;
                }

                this.btnExpandAll.Width = (int)g.MeasureString( this.btnExpandAll.Text, this.btnExpandAll.Font ).Width + 10; ;
                this.btnCollapseAll.Width = (int)g.MeasureString( this.btnCollapseAll.Text, this.btnCollapseAll.Font ).Width + 10; ;
                this.btnCollapseAll.Left = btnExpandAll.Location.X + btnExpandAll.Width + 10;

                this.splitContainer1.Panel1MinSize = btnScan.Width + btnScan.Left + 10;
                this.colImage.Text = Resources.thumbnailDataGridViewImageColumn_HeaderText;
                this.colDateTime.Text = CommonResources.Text.LabelDate;
                this.colGPS.Text = CommonResources.Text.LabelGPSLocation;
                this.colTitle.Text = Resources.titleDataGridViewTextBoxColumn_HeaderText;
                this.colDescription.Text = Resources.commentDataGridViewTextBoxColumn_HeaderText;

                this.colDImage.Text = Resources.thumbnailDataGridViewImageColumn_HeaderText;
                this.colDDateTime.Text = CommonResources.Text.LabelDate;
                this.colDGPS.Text = CommonResources.Text.LabelGPSLocation;
                this.colDTitle.Text = Resources.titleDataGridViewTextBoxColumn_HeaderText;
                this.colDDescription.Text = Resources.commentDataGridViewTextBoxColumn_HeaderText;

                this.toolStripMenuAdd.Text = CommonResources.Text.ActionAdd;
                this.toolStripMenuCopyToClipboard.Text = CommonResources.Text.ActionCopy;
                this.toolStripMenuMigratePaths.Text = Resources.MigratePaths_Text;
                this.toolStripMenuRemove.Text = CommonResources.Text.ActionRemove;
                this.toolStripMenuRefresh.Text = CommonResources.Text.ActionRefresh;
                this.toolStripMenuOpenImage.Text = CommonResources.Text.ActionOpen;
                this.toolStripMenuOpenFolder.Text = Resources.OpenContainingFolder_Text;
                this.toolStripMenuOpenThumbnail.Text = CommonResources.Text.ActionOpen + " " + Resources.thumbnailDataGridViewImageColumn_HeaderText;

                if ( m_standardpathalreadyshown ) LoadActivityNodes( true );
            }
        }

        public void ThemeChanged( ZoneFiveSoftware.Common.Visuals.ITheme visualTheme )
        {
            m_visualTheme = visualTheme;
            this.BackColor = visualTheme.Control;
            this.ForeColor = visualTheme.ControlText;
            this.progressBar2.BackColor = visualTheme.Control;
            this.progressBar2.ForeColor = visualTheme.Selected;
            if ( this.Enabled )
            {
                treeViewImages.BackColor = visualTheme.Window;
                treeViewActivities.BackColor = visualTheme.Window;
                listViewDrive.BackColor = visualTheme.Window;
                listViewAct.BackColor = visualTheme.Window;
            }
            else
            {
                treeViewImages.BackColor = visualTheme.Control;
                treeViewActivities.BackColor = visualTheme.Control;
                listViewDrive.BackColor = visualTheme.Control;
                listViewAct.BackColor = visualTheme.Control;
            }
        }
        #endregion

        #region Helper Classes
        // Used for file sorting
        public class FileInfoEx
        {
            public FileInfo fi;
            public DateTime exif;
        }
        public class ActivityImagesChangedEventArgs : EventArgs
        {
            public ActivityImagesChangedEventArgs()
            {
            }
            public ActivityImagesChangedEventArgs( ListView.ListViewItemCollection items )
            {
                _items = items;
            }

            private ListView.ListViewItemCollection _items = null;
            public ListView.ListViewItemCollection Items
            {
                get { return _items; }
            }
        }
        private class ImportControlState : IEquatable<ImportControlState>
        {
            private IList<IActivity> _activities = null;
            public IList<IActivity> Activities
            {
                get { return _activities; }
                set { _activities = value; }
            }
            private bool _visible = false;
            public bool Visible
            {
                get { return _visible; }
                set { _visible = value; }
            }

            public ImportControlState( IList<IActivity> activities, bool bVisible )
            {
                Activities = activities;
                Visible = bVisible;
            }

            public bool Equals( ImportControlState obj )
            {
                // If both are null we might be in Settings.  Not an error condition.
                // If one is null and the other is not, though, that is an error condition.
                if ( ( this._activities == null ) ^ ( obj._activities == null ) ) return false;
                if ( this._activities != null )
                {
                    if ( this._activities.Count != obj._activities.Count ) return false;
                    for ( int i = 0; i < this._activities.Count; i++ )
                    {
                        if ( this._activities[i].ToString() != obj._activities[i].ToString() ) return false;
                    }
                }
                if ( this.Visible != obj.Visible ) return false;
                return true;
            }
        }
        [Serializable()]
        public class ImportControlException : Exception
        {
            public static readonly string Error_ActivityChanged = Properties.Resources.ActivityChanged_Text;

            public ImportControlException() : base() { }
            public ImportControlException( string message ) : base( message ) { }
            public ImportControlException( string message, Exception inner ) : base( message, inner ) { }

            // A constructor is needed for serialization when an
            // exception propagates from a remoting server to the client.
            protected ImportControlException( System.Runtime.Serialization.SerializationInfo info,
                System.Runtime.Serialization.StreamingContext context ) { }
        }
        #endregion

        #region Callbacks
        delegate void SetTextCallback( string text );
        public delegate void ActivityImagesChangedEventHandler( System.Object sender, ActivityImagesChangedEventArgs e );
        public event ActivityImagesChangedEventHandler ActivityImagesChanged;

        #endregion

        #region private variables
        private const string gDummyFolder = "*****";

        private List<TreeNode> m_ActivityNodes = new List<TreeNode>();//List of all activities (used for comparing and organizing)
        private List<TreeNode> m_CheckedDirNodes = new List<TreeNode>();//Nodes that are selected in the Drive TreeView
        private bool m_showallactivities;
        private bool m_standardpathalreadyshown;
        private int m_viewActivity = 0; //View of ListviewAct
        private int m_viewFolder = 0; //View of ListviewDrive

        private TreeNode CapturedTreeViewActNode = null;
        private TreeNode CapturedTreeViewImagesNode = null;
        private ZoneFiveSoftware.Common.Visuals.ITheme m_visualTheme = null;
#if !ST_2_1
        //TODO: fix datastructure...
        internal PicturesLayer m_layer = null;
#endif

        #endregion

        #region Public properties
        private IList<IActivity> m_Activities;
        private IEnumerable<IActivity> GetActivities()
        {
                if (this.ShowAllActivities)
                { //add all activities
                    return ActivityPicturePlugin.Plugin.GetApplication().Logbook.Activities;
                }
                else
                { //add only current activity
                    return m_Activities;
                }
        }

        public IList<IActivity> Activities
        {
            set
            {
                m_Activities = value;
            }
        }

        public bool ShowAllActivities
        {
            get { return m_showallactivities; }
            set { m_showallactivities = value; }
        }
        #endregion

        #region private methods

        #region TreeViewImages
        public void LoadActivityNodes( bool bReload = false )
        {
            try
            {
                m_standardpathalreadyshown &= !bReload;
                if ( !m_standardpathalreadyshown )
                {
                    FillTreeViewImages();
                }
                if ( !m_standardpathalreadyshown | this.m_showallactivities == false )
                {
                    SetTreeActivitiesEvents( false );
                    FillTreeViewActivities();
                    SetTreeActivitiesEvents( true );
                    FindImagesInActivities();

                    m_standardpathalreadyshown = true;
                }
            }
            catch ( ImportControlException ex)
            {
                MessageBox.Show( ex.Message, 
                    Properties.Resources.OperationAborted_Text, 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Exclamation );
                //System.Diagnostics.Debug.Print( ex.Message );
            }
        }

        private void SetTreeEvents(bool t)
        {
            if (t)
            {
                this.treeViewImages.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeViewImages_AfterCheck);
                this.treeViewImages.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeViewImages_BeforeExpand);
                //this.treeViewImages.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewImages_AfterSelect);
                //this.treeViewImages.EnabledChanged += new System.EventHandler(this.treeViewImages_EnabledChanged);
                //this.treeViewImages.MouseClick += new System.Windows.Forms.MouseEventHandler(this.treeViewImages_MouseClick);
            }
            else
            {
                this.treeViewImages.AfterCheck -= new System.Windows.Forms.TreeViewEventHandler(this.treeViewImages_AfterCheck);
                this.treeViewImages.BeforeExpand -= new System.Windows.Forms.TreeViewCancelEventHandler(this.treeViewImages_BeforeExpand);
                //this.treeViewImages.AfterSelect -= new System.Windows.Forms.TreeViewEventHandler(this.treeViewImages_AfterSelect);
                //this.treeViewImages.EnabledChanged -= new System.EventHandler(this.treeViewImages_EnabledChanged);
                //this.treeViewImages.MouseClick -= new System.Windows.Forms.MouseEventHandler(this.treeViewImages_MouseClick);
            }
        }
        private void SetTreeActivitiesEvents( bool t )
        {
            if ( t )
            {
                this.treeViewActivities.AfterSelect += new System.Windows.Forms.TreeViewEventHandler( this.treeViewActivities_AfterSelect );
            }
            else
            {
                this.treeViewActivities.AfterSelect -= new System.Windows.Forms.TreeViewEventHandler( this.treeViewActivities_AfterSelect );
            }
        }

        private void SetCheck( TreeNode node, bool check )
        {
            //TODO: Should sub nodes be set here?
            // If by sub nodes you mean ALL sub nodes, then only 
            // if it's done in a separate thread.  It could make
            // the progress bar work better if it knew in advance
            // the number of folders.  Not worth holding up the GUI
            // as it checks 100,000 folders, though.
            if ( node.Checked )
            {
                if ( !this.m_CheckedDirNodes.Contains( node ) ) this.m_CheckedDirNodes.Add( node );
            }
            else
            {
                if ( this.m_CheckedDirNodes.Contains( node ) ) this.m_CheckedDirNodes.Remove( node );
            }
            foreach ( TreeNode n in node.Nodes )
            {
                this.SetTreeEvents( false );
                n.Checked = check;
                this.SetTreeEvents(true);
                if ( ( n.Tag is FileInfo ) | ( n.Tag is DirectoryInfo ) )
                {
                    if ( n.Checked )
                    {
                        if ( !this.m_CheckedDirNodes.Contains( n ) ) this.m_CheckedDirNodes.Add( n );
                    }
                    else
                    {
                        if ( this.m_CheckedDirNodes.Contains( n ) ) this.m_CheckedDirNodes.Remove( n );
                    }
                }
                if ( n.Nodes.Count != 0 ) SetCheck( n, check );
            }
        }

        private static void GetSubDirectoryNodes(TreeNode parentNode)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(parentNode.FullPath);
                DirectoryInfo[] dirSubs;
                try
                {
                    dirSubs = dir.GetDirectories();
                }
                catch (UnauthorizedAccessException)
                { /* Move on to the next folder.*/
                    return;
                }
                foreach (DirectoryInfo dirsub in dirSubs)
                {
                    if (!Functions.IsNormalFile(dirsub))
                    {
                        continue;	// Do not display hidden or system folders
                    }

                    TreeNode subNode = new TreeNode(dirsub.Name);
                    try
                    {
                        if (dirsub.GetDirectories().Length != 0) subNode.Nodes.Add(gDummyFolder);
                        subNode.Tag = dirsub; //treeViewImages
                        parentNode.Nodes.Add(subNode);
                        subNode.Checked = parentNode.Checked;
                    }
                    catch (UnauthorizedAccessException)
                    { /* Move on to the next folder.*/
                    }
                }
            }
            catch (Exception ex )
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );
            }
        }

        private void ShowFolderPics(DirectoryInfo driveDir) //Adds the images of a directory to the ListViewDrive
        {
            ImageList lvImgL = null;
            ImageList lvImgS = null;
            ListViewItemSorter lvSorter = (ListViewItemSorter)this.listViewDrive.ListViewItemSorter;

            try
            {
                List<FileInfo> folderFiles = new List<FileInfo>();
                try
                {
                    folderFiles.InsertRange(0, driveDir.GetFiles());
                }
                catch ( UnauthorizedAccessException )
                {
                    return;
                }

                for (int i = 0; i < folderFiles.Count; i++)
                {
                    if (!Functions.IsNormalFile(folderFiles[i]) ||
                        Functions.GetMediaType(folderFiles[i].Extension) == ImageData.DataTypes.Nothing)
                    {
                        folderFiles.RemoveAt(i);
                        i--;
                    }
                }

                listViewDrive.Items.Clear();

                lvImgL = new ImageList();
                lvImgL.ImageSize = new Size( 100, 100 );
                lvImgL.ColorDepth = ColorDepth.Depth32Bit;

                if ( listViewDrive.LargeImageList != null )
                {
                    listViewDrive.LargeImageList.Dispose();
                    listViewDrive.LargeImageList = null;
                }
                listViewDrive.LargeImageList = lvImgL;

                lvImgS = new ImageList();
                lvImgS.ImageSize = new Size( 50, 50 );
                lvImgS.ColorDepth = ColorDepth.Depth32Bit;

                if ( listViewDrive.SmallImageList != null )
                {
                    listViewDrive.SmallImageList.Dispose();
                    listViewDrive.SmallImageList = null;
                }
                listViewDrive.SmallImageList = lvImgS;

                progressBar2.Style = ProgressBarStyle.Continuous;

                //Either speed up adding to listview or show progressbar
                using ( Image bmp = (Bitmap)( Resources.image ).Clone() )
                {
                    System.Collections.ArrayList lvItems = new System.Collections.ArrayList();

                    // Create a list of ListItems 
                    foreach (FileInfo fi in folderFiles)
                    {
                        ListViewItem lvi = NewListViewImagesItem(fi);
                        lvItems.Add(lvi);
                    }

                    // Sort the list if a ListViewItemSorter has been set
                    if ( lvSorter != null )
                    {
                        ListViewItemSorter lviSorter = new ListViewItemSorter();
                        lviSorter.ColumnIndex = lvSorter.ColumnIndex;
                        lviSorter.ColumnType = lvSorter.ColumnType;
                        lviSorter.SortDirection = lvSorter.SortDirection;

                        this.listViewDrive.ListViewItemSorter = null;
                        lvItems.Sort( lviSorter );
                    }

                    // Bulk add the listview items to the listViewDrive
                    listViewDrive.Items.AddRange( (ListViewItem[])lvItems.ToArray( typeof( ListViewItem ) ) );

                    ImportControlState state = new ImportControlState( m_Activities, this.Visible );
                    // Add the images
                    for ( int j = 0; j < listViewDrive.Items.Count; j++ )
                    {
                        if (progressBar2.Maximum != folderFiles.Count) progressBar2.Maximum = folderFiles.Count; //The expected number
                        if (progressBar2.Maximum <= listViewDrive.Items.Count) progressBar2.Maximum = listViewDrive.Items.Count; //If items existed for some reason
                        progressBar2.Value = j;

                        FileInfo fi = new FileInfo( listViewDrive.Items[j].ImageKey );
                        ImageData.DataTypes dt = Functions.GetMediaType( fi.Extension );
                        lvImgL.Images.Add( Functions.getThumbnailWithBorder( lvImgL.ImageSize.Width, bmp ) );
                        lvImgS.Images.Add( Functions.getThumbnailWithBorder( lvImgS.ImageSize.Width, bmp ) );

                        AddImageToListView( lvImgL, lvImgS, dt, j, listViewDrive.Items[j].ImageKey );	//read images and add thumbnails
                        lblProgress.Text = String.Format( Resources.FoundImagesInFolder_Text, j + 1 );

                        Application.DoEvents();
                        if ( !state.Equals( new ImportControlState( m_Activities, this.Visible ) ) )
                            throw new ImportControlException( ImportControlException.Error_ActivityChanged );
                    }

                    // The last image for the last listitem doesn't always get drawn so
                    // we're invalidating the region of the last listitem.
                    // The reason is probably because we're creating the imagelist after
                    // adding the listitems to the control.  I think I prefer this 
                    // behaviour, though.
                    int nIndex = listViewDrive.Items.Count;
                    if ( nIndex > 0 )
                        listViewDrive.Invalidate( listViewDrive.Items[nIndex - 1].Bounds );

                    this.listViewDrive.ListViewItemSorter = (System.Collections.IComparer)lvSorter;
                    Application.DoEvents();
                }
            }
            catch ( ImportControlException )
            {
                if ( lvImgS != null )
                    lvImgS.Dispose();
                lvImgS = null;

                if ( lvImgL != null )
                    lvImgL.Dispose();
                lvImgL = null;

                listViewDrive.SmallImageList = null;
                listViewDrive.LargeImageList = null;

                throw;
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );

                if ( lvImgS != null )
                    lvImgS.Dispose();
                lvImgS = null;

                if ( lvImgL != null )
                    lvImgL.Dispose();
                lvImgL = null;

                // Apparently we need to set these to null after 
                // disposing BOTH of the above imagelists.
                listViewDrive.SmallImageList = null;
                listViewDrive.LargeImageList = null;
            }
            finally
            {
                listViewDrive.ListViewItemSorter = lvSorter;    //Restore the sorter
            }
        }

        private void FillTreeViewImages()
        {
            try
            {
                this.treeViewImages.Nodes.Clear();
                this.m_CheckedDirNodes.Clear();

                string[] drives = Environment.GetLogicalDrives();
                DirectoryInfo startdir = new DirectoryInfo(ActivityPicturePlugin.Source.Settings.LastImportDirectory);

                foreach ( string rootDir in drives )
                {
                    DirectoryInfo dir = new DirectoryInfo( rootDir );
                    TreeNode ndRoot = new TreeNode( rootDir );
                    ndRoot.Tag = dir; //treeViewImages

                    //Fill standard path
                    this.treeViewImages.Nodes.Add( ndRoot );
                    if ( dir.Root.Name == startdir.Root.Name )
                    {
                        AddStandardPath( this.treeViewImages, startdir, ndRoot );
                    }
                    else
                    {
                        ndRoot.Nodes.Add( gDummyFolder );
                    }
                }
            }
            catch ( ImportControlException )
            {
                throw;
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );
            }

        }

        private void AddStandardPath( TreeView tvw, DirectoryInfo startdir, TreeNode ndRoot )
        {
            try
            {
                TreeNode parentNode = null;
                List<DirectoryInfo> dirs = new List<DirectoryInfo>();

                while ( true )
                {
                    if ( startdir.Parent != null )
                    {
                        dirs.Add( startdir );
                        startdir = startdir.Parent;
                    }
                    else break;
                }

                dirs.Reverse();
                parentNode = ndRoot;

                foreach ( DirectoryInfo subdir in dirs )
                {
                    TreeNode subNodeP = new TreeNode( subdir.Name );
                    subNodeP.Tag = subdir;  //treeViewImages
                    parentNode.Nodes.Add( subNodeP );

                    DirectoryInfo dir = new DirectoryInfo( parentNode.FullPath );
                    DirectoryInfo[] dirSubs = dir.GetDirectories();
                    foreach ( DirectoryInfo dirsub in dirSubs )
                    {
                        if ( !Functions.IsNormalFile( dirsub ) )
                        {
                            continue;	// Do not display hidden or system folders
                        }
                        DirectoryInfo dirP = (DirectoryInfo)( subNodeP.Tag );
                        if ( dirsub.Name != dirP.Name )
                        //otherwise the folders of the standard path will be created twice
                        {
                            TreeNode subNode = new TreeNode( dirsub.Name );
                            int i = 0;
                            try
                            {
                                i = dirsub.GetDirectories().Length;
                            }
                            catch ( UnauthorizedAccessException )
                            {
                                // Folder is inaccessible for whatever reason
                                // Don't add the node
                                i = 0;
                            }
                            catch ( Exception ex )
                            {
                                System.Diagnostics.Debug.Assert( false, ex.Message );
                                // Don't add the node
                                i = 0;
                            }
                            if ( i != 0 ) subNode.Nodes.Add( gDummyFolder );
                            subNode.Tag = dirsub; //treeViewImages
                            parentNode.Nodes.Add( subNode );
                        }
                    }
                    parentNode = subNodeP;
                }

                DirectoryInfo driveDir = new DirectoryInfo( parentNode.FullPath );
                ShowFolderPics( driveDir );

                if ( driveDir.GetDirectories().Length != 0 ) parentNode.Nodes.Add( gDummyFolder );
                this.SetTreeEvents( false );
                parentNode.EnsureVisible();
                parentNode.TreeView.SelectedNode = parentNode;
                this.SetTreeEvents( true );
            }
            catch ( ImportControlException )
            {
                throw;
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );
            }
        }

        #endregion

        #region TreeViewActivities

        private void FillTreeViewActivities(bool bSelectCurrentActivity = true)
        {
            this.treeViewActivities.Nodes.Clear();
            //TODO: Get current activity, select if still in list (otherwise newest?)
            this.m_ActivityNodes.Clear();
            try
            {
                TreeNode dayNode = null;
                IEnumerable<IActivity> acts = this.GetActivities();
                if (acts != null)
                {
                    foreach (IActivity act in acts)
                    {
                        TreeNode yearNode, monthNode;
                        DateTime localTime = act.StartTime.ToLocalTime();
                        //Year
                        string year = localTime.Year.ToString();
                        TreeNode[] yearNodes = treeViewActivities.Nodes.Find(year, false);
                        if (yearNodes.Length == 0)
                        {
                            yearNode = new TreeNode(year);
                            yearNode.Name = year;
                            treeViewActivities.Nodes.Add(yearNode);
                        }
                        else yearNode = yearNodes[0];

                        //Month
                        string month = localTime.ToString("MM");
                        TreeNode[] monthNodes = yearNode.Nodes.Find(month, false);
                        if (monthNodes.Length == 0)
                        {
                            monthNode = new TreeNode(localTime.ToString("MMMM"));
                            monthNode.Name = month;
                            yearNode.Nodes.Add(monthNode);
                        }
                        else monthNode = monthNodes[0];

                        //Day
                        string day = localTime.ToString("dd");

                        string strLongDate = "";
                        string strShortTime = "";
                        CultureInfo specificCulture = Functions.NeutralToSpecificCulture( CultureInfo.CurrentUICulture.Name );
                        if ( specificCulture != null )
                        {
                            strLongDate = localTime.ToString( specificCulture.DateTimeFormat.LongDatePattern, specificCulture );
                            strShortTime = localTime.ToString( specificCulture.DateTimeFormat.ShortTimePattern, specificCulture );
                        }
                        else
                        {
                            strLongDate = localTime.ToLongDateString();
                            strShortTime = localTime.ToShortTimeString();
                        }

                        dayNode = new TreeNode( strLongDate + " " +
                            strShortTime + " "
                            + Resources.ImportControl_in + " " + act.Location);

                        dayNode.Name = localTime.ToString("u");
                        dayNode.Tag = act; //treeViewAct
                        this.m_ActivityNodes.Add(dayNode);
                        monthNode.Nodes.Add(dayNode);
                    }
                    this.treeViewActivities.Sort();
                    this.treeViewActivities.CollapseAll();
                    //Gets the latest added activity in list (if several selected), normally latest in time
                    if (bSelectCurrentActivity) treeViewActivities.SelectedNode = dayNode;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
                throw;
            }
        }

        private void CheckColorTreeNode( TreeNode node )
        {
            bool foundimages = false;
            foreach ( TreeNode n in node.Nodes )
            {
                IActivity a = (IActivity)( n.Tag );
                PluginData d = Helper.Functions.ReadExtensionData( a );
                if ( d.Images.Count != 0 )
                {
                    foundimages = true;
                    break;
                }
            }
            if ( !foundimages ) node.BackColor = node.TreeView.BackColor;
        }

        private TreeNode GetNodeFromPath( TreeView tv, string sPath )
        {
            TreeNode retNode = null;
            string sDelimiter = tv.PathSeparator;
            string[] sFolders = sPath.Split( sDelimiter.ToCharArray() );
            List<string> lFolders = new List<string>();

            for ( int i = 0; i < sFolders.Length; i++ )
            {
                if ( sFolders[i] != "" )
                    lFolders.Add( sFolders[i] );
            }

            foreach ( TreeNode tn in tv.Nodes )
            {
                if ( ( tn.Text == sFolders[0] ) || ( tn.Text == sFolders[0] + sDelimiter ) )
                {
                    retNode = GetNextNodeInPath( tn, lFolders, 1 );
                    break;
                }
            }
            return retNode;
        }

        // Recursively drill down the path until we find the node we're looking for
        // iDepth is the current index in lFolders that we're looking for.
        private static TreeNode GetNextNodeInPath( TreeNode tn, List<string> lFolders, int iDepth )
        {
            TreeNode retNode = null;
            try
            {
                if ( tn.Nodes[0].Text == gDummyFolder )
                {
                    tn.Nodes.Clear();
                    GetSubDirectoryNodes( tn );
                }

                if ( ( tn != null ) && ( iDepth < lFolders.Count ) )
                {
                    foreach ( TreeNode n in tn.Nodes )
                    {
                        if ( n.Text == lFolders[iDepth] )
                        {
                            if ( iDepth + 1 < lFolders.Count )	// Need to drill deeper
                                retNode = GetNextNodeInPath( n, lFolders, ++iDepth );
                            else retNode = n;	// We struck oil
                            break;
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );
            }

            return retNode;
        }

        private void FindImagesInActivities()
        {
            this.progressBar2.Minimum = 0;
            this.progressBar2.Maximum = this.m_ActivityNodes.Count;
            this.progressBar2.Step = 1;
            //ProgressBar not set
            this.listViewAct.Items.Clear();
            foreach ( TreeNode year in this.treeViewActivities.Nodes )
            {
                foreach ( TreeNode month in year.Nodes )
                {
                    foreach ( TreeNode day in month.Nodes )
                    {
                        this.lblProgress.Text = Resources.ImportControl_scanning + " " + day.Text;
                        this.progressBar2.PerformStep();
                        ImportControlState state = new ImportControlState( m_Activities, this.Visible );
                        Application.DoEvents();
                        if ( !state.Equals( new ImportControlState( m_Activities, this.Visible ) ) )
                            throw new ImportControlException( ImportControlException.Error_ActivityChanged );
                        //Show Thumbs if called from activity
                        if ( day.Tag is IActivity ) AddImagesToListViewAct( day, !this.m_showallactivities );
                    }
                }
            }
        }

        private void RemoveSelectedImagesFromActivity( ListViewItem[] lvisel )
        {
            //Delete selected images
            IActivity act = (IActivity)( this.treeViewActivities.SelectedNode.Tag );
            PluginData data = Helper.Functions.ReadExtensionData( act );
            listViewAct.SuspendLayout();
            IList<ImageData> rem = new List<ImageData>();

            foreach (ListViewItem lvi in lvisel)
            {
                ImageData im = (ImageData)(lvi.Tag);
                ImageDataSerializable ids = im.GetSerialzable(data.Images);
                if (ids != null)
                {
                    data.Images.Remove(ids);
                }
                rem.Add(im);
                if (lvi.ListView.SmallImageList.Images.Count > lvi.Index)
                    lvi.ListView.SmallImageList.Images.RemoveAt(lvi.Index);
                if (lvi.ListView.LargeImageList.Images.Count > lvi.Index)
                    lvi.ListView.LargeImageList.Images.RemoveAt(lvi.Index);
                lvi.Remove();
            }
            listViewAct.ResumeLayout();
            Functions.WriteExtensionData( act, data );
            if ( data.Images.Count == 0 ) this.treeViewActivities.SelectedNode.BackColor = treeViewActivities.BackColor;    // Color.White;
            CheckColorTreeNode( this.treeViewActivities.SelectedNode.Parent );
            CheckColorTreeNode( this.treeViewActivities.SelectedNode.Parent.Parent );

            foreach (ImageData im in rem)
            {
                im.DeleteThumbnail();
            }
        }

        private void AddSelectedImagesToActivity( ListViewItem[] lvisel )
        {
            try
            {
                IActivity act = (IActivity)( this.treeViewActivities.SelectedNode.Tag );
                //Contains the list of images as they're being added
                PluginData data = Helper.Functions.ReadExtensionData( act );

                //Contains the original list of images.  Used to prevent data.Images.Count! checks
                PluginData dataTmp = Helper.Functions.ReadExtensionData( act );

                int iCount = 0;
                progressBar2.Style = ProgressBarStyle.Continuous;
                progressBar2.Maximum = lvisel.Length;

                ImportControlState state = new ImportControlState( m_Activities, this.Visible );

                //Check if Image does already exist in the current activity
                foreach ( ListViewItem lvi in lvisel )
                {
                    lblProgress.Text = String.Format( Resources.CheckingForDuplicates_Text, ++iCount, lvisel.Length );
                    progressBar2.Value = iCount;

                    FileInfo fi = (FileInfo)(lvi.Tag);
                    if ( !ImageAlreadyExistsInActivity( fi.Name, dataTmp ) )
                    {
                        ImageDataSerializable ids = ImageDataSerializable.FromFile( fi );
                        if ( ids != null ) data.Images.Add( ids );
                        Functions.WriteExtensionData( act, data );

                        // Maintain a list of thumbnails that were created.
                        // The next time Sporttracks runs and the plugin in launched
                        // these images will be deleted if the logbook wasn't saved
                        // removing orphaned thumbnails.
                        ActivityPicturePlugin.Source.Settings.NewThumbnailsCreated += ImageData.thumbnailPath(ids.ReferenceID) + "\t";

                    }
                    Application.DoEvents();
                    if ( !state.Equals( new ImportControlState( m_Activities, this.Visible ) ) )
                        throw new ImportControlException( ImportControlException.Error_ActivityChanged );
                }

                //to refresh the ListView
                AddImagesToListViewAct( this.treeViewActivities.SelectedNode, true );
            }
            catch ( ImportControlException )
            {
                throw;
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );
                throw;
            }
        }

        private class ActivityTreeViewInfo
        {
            public ImageData image;
            public IActivity Activity;
        }

        //Drills from 'node' to find all child activities
        private IList<ActivityTreeViewInfo> treeViewActivities_GetImageData(TreeNode node)
        {
            IList<ActivityTreeViewInfo> res = new List<ActivityTreeViewInfo>();

            if (node.Nodes.Count > 0)
            {
                foreach (TreeNode tn in node.Nodes)
                {
                    foreach (ActivityTreeViewInfo im in treeViewActivities_GetImageData(tn))
                    {
                        res.Add(im);
                    }
                }
            }
            if (node.Tag != null)
            {
                if (node.Tag is IActivity) //Node is an Activity
                {
                    IActivity act = (IActivity)(node.Tag);
                    PluginData data = Helper.Functions.ReadExtensionData(act);

                    List<ImageData> il = data.LoadImageData(data.Images, act);
                    foreach (ImageData id in il)
                    {
                        ActivityTreeViewInfo a = new ActivityTreeViewInfo();
                        a.image = id;
                        a.Activity = act;
                        res.Add(a);
                    }
                }
            }
            return res;
        }
        #endregion

        #region listViewDrive
        private void AddImageToListView( ImageList lvImgL, ImageList lvImgS, ImageData.DataTypes dt, int ixImage, string strKey )
        {
            try
            {
                if ( dt == ImageData.DataTypes.Image )
                {
                    //Test: try showing thumbnail without opening whole image
                    //byte[] imgbyte = com.SimpleRun.ShowOneFileThumbnailData(file.FullName);
                    //MemoryStream stream = new MemoryStream(imgbyte.Length);
                    //stream.Write(imgbyte, 0, imgbyte.Length);
                    //Image img = Image.FromStream(stream);
                    //if (img != null) lvImg.Images.Add(img);

                    using (Image img = Image.FromFile(strKey))
                    {
                        lvImgL.Images[ixImage] = Functions.getThumbnailWithBorder(lvImgL.ImageSize.Width, img);
                        lvImgL.Images.SetKeyName(ixImage, strKey);
                        lvImgS.Images[ixImage] = Functions.getThumbnailWithBorder(lvImgS.ImageSize.Width, img);
                        lvImgS.Images.SetKeyName(ixImage, strKey);
                    }
                }
                else if ( dt == ImageData.DataTypes.Video )
                {
                    using (Image bmp = (Bitmap)(Resources.video).Clone())
                    {
                        lvImgL.Images[ixImage] = Functions.getThumbnailWithBorder(lvImgL.ImageSize.Width, bmp);
                        lvImgL.Images.SetKeyName(ixImage, strKey);
                        lvImgS.Images[ixImage] = Functions.getThumbnailWithBorder(lvImgS.ImageSize.Width, bmp);
                        lvImgS.Images.SetKeyName(ixImage, strKey);
                    }
                }
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );
            }
        }

        private ListViewItem NewListViewImagesItem( FileInfo file )
        {
            ListViewItem lvi = new ListViewItem();
            ImageData.DataTypes dt = Functions.GetMediaType(file.Extension);
            lvi.Text = file.Name;
            lvi.ImageKey = file.FullName;
            lvi.Tag = file; //For listViewDrive

            if ( dt == ImageData.DataTypes.Image )
            {
                try
                {
                    ExifDirectory ed = null;
                    GpsDirectory gps = null;

                    if ( Functions.IsExifFileExt( file ) )
                    {
                        ed = SimpleRun.ShowOneFileExifDirectory( file.FullName );
                        gps = SimpleRun.ShowOneFileGPSDirectory( file.FullName );
                    }

                    // TODO: de-DE is used as en is default and cannot be set. Rewrite the use of culture
                    DateTime exifTime = Functions.GetExifOriginalTime(file, ed);
                    if (exifTime > DateTime.MinValue)
                    {
                        string strDateTime = "";
                        System.Globalization.CultureInfo specificCulture = Functions.NeutralToSpecificCulture(System.Globalization.CultureInfo.CurrentUICulture.Name);
                        if (specificCulture != null)
                            strDateTime = exifTime.ToString(specificCulture);
                        else
                            strDateTime = exifTime.ToString();
                        lvi.SubItems.Add(strDateTime);
                    }
                    //IFormatProvider culture = new System.Globalization.CultureInfo( "de-DE", true );
                    //if ( ed != null )
                    //{
                    //    string s = ed.GetDescription( ExifDirectory.TAG_DATETIME_ORIGINAL );
                    //    if ( !string.IsNullOrEmpty( s ) )
                    //    {
                    //        DateTime dtTmp = new DateTime();
                    //        // If we're hardcoding the format, we need to use an appropriate culture
                    //        if ( DateTime.TryParseExact( s, Functions.NeutralDateTimeFormat, culture, System.Globalization.DateTimeStyles.AssumeLocal, out dtTmp ) )
                    //        {
                    //            string strDateTime = "";
                    //            System.Globalization.CultureInfo specificCulture = Functions.NeutralToSpecificCulture( System.Globalization.CultureInfo.CurrentUICulture.Name );
                    //            if ( specificCulture != null )
                    //                strDateTime = dtTmp.ToString( specificCulture );
                    //            else
                    //                strDateTime = dtTmp.ToString();
                    //            lvi.SubItems.Add( strDateTime );
                    //            //lvi.SubItems.Add( dtTmp.ToString() );   // ToString() returns date/time in culture of the current thread
                    //        }
                    //    }
                    //}

                    // The first item is considered a SubItem.  We want to check to
                    // see if a second item was added (from just above).
                    //TODO: needed?
                    // Adds DateTime to ListViewDrive items for files that do not have 
                    // exif data, ie. bmp, etc.
                    if ( lvi.SubItems.Count == 1 )
                    {
                        DateTime dtBest;
                        if ( !m_showallactivities && m_Activities != null && m_Activities.Count > 0 )
                        {
                            DateTime dtEnd = GetActivityEndTime( m_Activities[0] );
                            dtBest = Functions.GetBestTime( file,
                                DateTime.MinValue,
                                m_Activities[0].StartTime,
                                dtEnd );
                        }
                        else
                        {
                            dtBest = Functions.GetBestTime( file, file.CreationTimeUtc, DateTime.MinValue, DateTime.MaxValue );
                        }

                        dtBest = dtBest.ToLocalTime();
                        string strDateTime = "";
                        System.Globalization.CultureInfo specificCulture = Functions.NeutralToSpecificCulture( System.Globalization.CultureInfo.CurrentUICulture.Name );
                        if ( specificCulture != null )
                            strDateTime = dtBest.ToString( specificCulture );
                        else
                            strDateTime = dtBest.ToString();   // ToString() returns date/time in culture of the current thread

                        lvi.SubItems.Add( strDateTime );
                    }

                    if ( gps != null )
                    {
                        string latref = gps.GetDescription( GpsDirectory.TAG_GPS_LATITUDE_REF );
                        string latitude = gps.GetDescription( GpsDirectory.TAG_GPS_LATITUDE );
                        string longitude = gps.GetDescription( GpsDirectory.TAG_GPS_LONGITUDE );
                        string longref = gps.GetDescription( GpsDirectory.TAG_GPS_LONGITUDE_REF );
                        string gpsstr = latitude + " " + latref + ", " + longitude + " " + longref;
                        if ( latitude != null ) lvi.SubItems.Add( gpsstr );
                        else lvi.SubItems.Add( "" );
                    }

                    if ( ed != null )
                    {
                        string s = (string)( ed.GetDescription( ExifDirectory.TAG_XP_TITLE ) );
                        if ( !string.IsNullOrEmpty( s ) )
                        {
                            lvi.SubItems.Add( s );
                        }
                        s = (string)( ed.GetDescription( ExifDirectory.TAG_XP_COMMENTS ) );
                        if ( !string.IsNullOrEmpty( s ) )
                        {
                            lvi.SubItems.Add( s );
                        }
                    }
                }
                catch ( Exception ex )
                {
                    System.Diagnostics.Debug.Assert( false, ex.Message );
                }

            }
            else if ( dt == ImageData.DataTypes.Video )
            {
                try
                {
                    DateTime dtBest = Functions.GetBestTime( file, file.CreationTimeUtc, DateTime.MinValue, DateTime.MaxValue );
                    dtBest = dtBest.ToLocalTime();

                    string strDateTime = "";
                    System.Globalization.CultureInfo specificCulture = Functions.NeutralToSpecificCulture( System.Globalization.CultureInfo.CurrentUICulture.Name );
                    if ( specificCulture != null )
                        strDateTime = dtBest.ToString( specificCulture );
                    else
                        strDateTime = dtBest.ToString();   // ToString() returns date/time in culture of the current thread
                    lvi.SubItems.Add( strDateTime );   // ToString() returns date/time in culture of the current thread
                }
                catch ( Exception ex )
                {
                    System.Diagnostics.Debug.Assert( false, ex.Message );
                }
            }

            return lvi;
        }


        #endregion

        #region listViewAct
        private void AddImagesToListViewAct( TreeNode tn, bool AddThumbs )
        {
            ImageList lil = null;
            ImageList lis = null;
            ListViewItemSorter lvSorter = (ListViewItemSorter)this.listViewAct.ListViewItemSorter;

            try
            {
                this.listViewAct.Items.Clear();
                if ( tn == null )
                {
                    //No selection, nothing to do
                    return;
                }
                IActivity act = (IActivity)( tn.Tag );
                PluginData data = Helper.Functions.ReadExtensionData( act );

                if ( data.Images.Count > 0 )
                {
                    //change color
                    if ( tn.BackColor != Color.Yellow )
                    {
                        tn.BackColor = Color.LightBlue;
                        if ( tn.Parent != null )
                        {
                            if ( tn.Parent.BackColor != Color.Yellow ) tn.Parent.BackColor = Color.LightBlue;
                            if ( tn.Parent.Parent != null & tn.Parent.Parent.BackColor != Color.Yellow ) tn.Parent.Parent.BackColor = Color.LightBlue;
                        }
                    }

                    //Load imagedata
                    if ( AddThumbs )
                    {
                        lil = new ImageList();
                        lis = new ImageList();
                        lil.ImageSize = new Size( 100, 100 );
                        lis.ImageSize = new Size( 50, 50 );
                        lil.ColorDepth = ColorDepth.Depth32Bit;
                        lis.ColorDepth = ColorDepth.Depth32Bit;
                        int j = 0;

                        // Dispose of the old imagelists if they exist
                        if ( this.listViewAct.LargeImageList != null )
                            this.listViewAct.LargeImageList.Dispose();
                        this.listViewAct.LargeImageList = null;

                        if ( this.listViewAct.SmallImageList != null )
                            this.listViewAct.SmallImageList.Dispose();
                        this.listViewAct.SmallImageList = null;

                        // assign the new imagelists
                        this.listViewAct.LargeImageList = lil;
                        this.listViewAct.SmallImageList = lis; 
                        System.Collections.ArrayList lvItems = new System.Collections.ArrayList();

                        IList<ImageData> il = data.LoadImageData( data.Images, act );

                        progressBar2.Style = ProgressBarStyle.Continuous;
                        progressBar2.Maximum = il.Count;
                        foreach ( ImageData id in il )
                        {
                            // Create a list of ListItems
                            //List view items
                            ListViewItem lvi = new ListViewItem();
                            lvi.Text = id.PhotoSourceFileName;
                            lvi.Tag = id; //listViewAct
                            lvi.Name = id.ThumbnailPath;
                            lvi.ImageKey = id.PhotoSource;
                            lvi.SubItems.Add( Functions.DateTimeString(id.DateTimeOriginal).Replace( Environment.NewLine, ", " ) );
                            lvi.SubItems.Add( GPS.GpsString(id.GpsPoint).Replace( Environment.NewLine, ", " ) );
                            lvi.SubItems.Add( id.Title );
                            lvi.SubItems.Add( id.Comments );
                            lvItems.Add( lvi );
                        }

                        if ( lvSorter != null )
                        {
                            ListViewItemSorter lviSorter = new ListViewItemSorter();
                            lviSorter.ColumnIndex = lvSorter.ColumnIndex;
                            lviSorter.ColumnType = lvSorter.ColumnType;
                            lviSorter.SortDirection = lvSorter.SortDirection;

                            this.listViewAct.ListViewItemSorter = null;
                            lvItems.Sort( lviSorter );
                        }

                        ImportControlState state = new ImportControlState( m_Activities, this.Visible );

                        // Bulk add the listview items to the listViewDrive
                        listViewAct.Items.AddRange( (ListViewItem[])lvItems.ToArray( typeof( ListViewItem ) ) );
                        Application.DoEvents();
                        if ( !state.Equals( new ImportControlState( m_Activities, this.Visible ) ) )
                            throw new ImportControlException( ImportControlException.Error_ActivityChanged );

                        // Add the images
                        for ( int i = 0; i < listViewAct.Items.Count; i++ )
                        {
                            try
                            {
                                //images (large and small icons)
                                Image img = null;
                                try
                                {
                                    if ( new FileInfo( listViewAct.Items[i].Name ).Exists )
                                        img = Image.FromFile( listViewAct.Items[i].Name );
                                    else
                                        img = (Image)ZoneFiveSoftware.Common.Visuals.CommonResources.Images.Delete16.Clone();
                                }
                                catch ( Exception ex )
                                {
                                    System.Diagnostics.Debug.Assert( false, ex.Message );

                                    //Something was wrong with the thumbnail.
                                    //Add a 'delete' thumbnail as a placeholder so the item can
                                    //officially be deleted by the user.
                                    System.Diagnostics.Debug.Print( ex.Message );
                                    img = (Image)ZoneFiveSoftware.Common.Visuals.CommonResources.Images.Delete16.Clone();
                                }
                                lil.Images.Add( listViewAct.Items[i].ImageKey, Functions.getThumbnailWithBorder( lil.ImageSize.Width, img ) );
                                lis.Images.Add( listViewAct.Items[i].ImageKey, Functions.getThumbnailWithBorder( lis.ImageSize.Width, img ) );

                                Application.DoEvents();
                                if ( !state.Equals( new ImportControlState( m_Activities, this.Visible ) ) )
                                    throw new ImportControlException( ImportControlException.Error_ActivityChanged );

                                img.Dispose();
                                img = null;
                            }
                            catch ( ImportControlException )
                            {
                                throw;
                            }
                            catch ( Exception ex )
                            {
                                System.Diagnostics.Debug.Assert( false, ex.Message );
                                System.Diagnostics.Debug.Print( ex.Message );
                            }
                            j++;
                            progressBar2.Value = j;
                            lblProgress.Text = String.Format( Resources.FoundImagesInActivity_Text, j );

                        }

                        // The last image for the last listitem doesn't always get drawn so
                        // we're invalidating the region of the last listitem.
                        // The reason is probably because we're creating the imagelist after
                        // adding the listitems to the control.  I think I prefer this 
                        // behaviour, though.
                        int nIndex = listViewAct.Items.Count;
                        if ( nIndex > 0 )
                            listViewAct.Invalidate( listViewAct.Items[nIndex - 1].Bounds );

#if !ST_2_1
                        if (this.m_layer != null)
                        {
                            this.m_layer.HidePage(); //defer updates
                            //this.m_layer.PictureSize = this.sliderImageSize.Value;
                            this.m_layer.Pictures = il;
                            this.m_layer.ShowPage("");//Refresh
                        }
#endif
                    }
                    else
                    {
                        this.listViewAct.Items.Clear();
                    }
                }
                else
                {
                    this.listViewAct.Items.Clear();
                }
            }
            catch ( ImportControlException )
            {
                if ( lil != null )
                    lil.Dispose();
                lil = null;

                if ( lis != null )
                    lis.Dispose();
                lis = null;

                listViewAct.LargeImageList = null;
                listViewAct.SmallImageList = null;

                throw;
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );

                if ( lil != null )
                    lil.Dispose();
                lil = null;

                if ( lis != null )
                    lis.Dispose();
                lis = null;

                listViewAct.LargeImageList = null;
                listViewAct.SmallImageList = null;
            }
            finally
            {
                listViewAct.ListViewItemSorter = lvSorter;    //Restore the sorter
            }
        }

        private static bool ImageAlreadyExistsInActivity( string fileName, PluginData dataRef )
        {
            if ( dataRef.Images.Count != 0 )
            {
                foreach ( ImageDataSerializable ids in dataRef.Images )
                {
                    FileInfo fi = new FileInfo( ids.PhotoSource );
                    if ( fi.Name == fileName ) return true;
                }
            }
            return false;
        }
        #endregion

        private void EnableControl( bool bEnable )
        {
            this.treeViewActivities.Enabled = bEnable;
            this.treeViewImages.Enabled = bEnable;
            this.listViewAct.Enabled = bEnable;
            this.listViewDrive.Enabled = bEnable;
            this.btnChangeActivityView.Enabled = bEnable;
            this.btnChangeFolderView.Enabled = bEnable;
            this.btnChangeActivityView.Enabled = bEnable;
            this.btnCollapseAll.Enabled = bEnable;
            this.btnExpandAll.Enabled = bEnable;
            this.btnScan.Enabled = bEnable;
        }

        private void ResetProgressBar()
        {
            this.lblProgress.Text = "";
            this.lblProgress.Visible = true;
            this.progressBar2.Value = 0;
            this.progressBar2.Visible = true;
            this.timerProgressBar.Enabled = false;
            this.listViewAct.Visible = false;
            this.treeViewActivities.Visible = false;
        }

        private void HideProgressBar( int hideDelay = 0 )
        {
            if ( hideDelay > 0 )
            {
                timerProgressBar.Interval = hideDelay;
                timerProgressBar.Enabled = true;
                timerProgressBar.Start();
            }
            else
            {
                this.timerProgressBar.Enabled = false;
                this.lblProgress.Text = "";
                this.lblProgress.Visible = false;
                this.progressBar2.Visible = false;
                this.progressBar2.Value = this.progressBar2.Maximum;
            }
            this.listViewAct.Visible = true;
            this.treeViewActivities.Visible = true;
        }

        private void SetLabelText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if ( this.lblProgress.InvokeRequired )
            {
                SetTextCallback d = new SetTextCallback( SetLabelText );
                this.Invoke( d, new object[] { text } );
            }
            else
            {
                this.lblProgress.Text = text;
            }
        }

        private void MarkTreeViewActNode(IActivity act, TreeNodeCollection nodes)
        {
            if (nodes != null)
            {
                foreach (TreeNode node in nodes)
                {
                    this.MarkTreeViewActNode(act, node.Nodes);
                    if (node.Tag != null && node.Tag is IActivity && 
                        (node.Tag as IActivity) == act)
                    {
                        //activity node plus parents will be marked yellow to track acts to which images have been added
                        node.BackColor = Color.Yellow;
                        node.Parent.BackColor = Color.Yellow;
                        node.Parent.Parent.BackColor = Color.Yellow;
                    }
                }
            }
        }

        private void MarkTreeViewActNode(IActivity act)
        {
            this.MarkTreeViewActNode(act, this.treeViewActivities.Nodes);
        }

        private int FindAndImportImages(List<FileInfoEx> fiexs, IList<IActivity> selActs)
        {
            int numFilesImported = 0;
            try
            {
                this.progressBar2.Style = ProgressBarStyle.Continuous;
                this.progressBar2.Minimum = 0;
                this.progressBar2.Maximum = fiexs.Count * this.m_ActivityNodes.Count;
                this.ResetProgressBar();

                ImportControlState state = new ImportControlState(m_Activities, this.Visible);
                foreach (IActivity activity in selActs)
                {
                    DateTime actStart = activity.StartTime.ToLocalTime();
                    DateTime actEnd = GetActivityEndTime(activity);
                    this.lblProgress.Text = actStart.ToString()+" "+activity.Name;
                    foreach (FileInfoEx file in fiexs)
                    {
                        Application.DoEvents();
                        if (!state.Equals(new ImportControlState(m_Activities, this.Visible)))
                            throw new ImportControlException(ImportControlException.Error_ActivityChanged);

                        this.progressBar2.Value++;
                        
                        DateTime FileTime = Functions.GetBestTime(file.fi, file.exif, actStart, actEnd);

                        if ((FileTime > actStart) &
                            (FileTime < actEnd))
                        {                                    
                            //the picture has been taken during the activity
                            PluginData data = Helper.Functions.ReadExtensionData(activity);

                            //Check if Image does already exist in the current activity
                            if (!ImageAlreadyExistsInActivity(file.fi.Name, data))
                            {
                                MarkTreeViewActNode(activity);
                                ImageDataSerializable ids = ImageDataSerializable.FromFile(file.fi);
                                if (ids != null)
                                {
                                    data.Images.Add(ids);
                                    Functions.WriteExtensionData(activity, data);
                                    numFilesImported++;

                                    ActivityPicturePlugin.Source.Settings.NewThumbnailsCreated += ImageData.thumbnailPath(ids.ReferenceID) + "\t";
                                }
                            }
                        }
                    }
                }
            }
            catch (ImportControlException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }
            finally
            {
                this.HideProgressBar(5000);
            }

            return numFilesImported;
        }

        private static DateTime GetActivityEndTime( IActivity Act )
        {
            ActivityInfo info = ActivityInfoCache.Instance.GetInfo(Act);
            return info.EndTime.ToLocalTime();
        }

        private IList<FileInfo> ThreadGetImages(DateTime first)
        {
            IList<FileInfo> importFiles = new List<FileInfo>();

            //DateTime.Minvalue should not occur, but no assert
            if (first > DateTime.MinValue)
            {
                //Some slack for time zone etc
                first -= TimeSpan.FromDays(1);
            }

            this.ResetProgressBar();
            this.progressBar2.Style = ProgressBarStyle.Continuous;
            this.progressBar2.Maximum = this.m_CheckedDirNodes.Count;
            this.progressBar2.Value = 0;

            foreach ( TreeNode n in this.m_CheckedDirNodes )
            {                
                GetImageFiles( n, first, importFiles );
            }
            this.HideProgressBar();
            return importFiles;
        }

        //finds all images of the selected directory
        private void GetImageFiles(TreeNode node, DateTime first, IList<FileInfo> importFiles)
        {
            try
            {
                if ( node.Tag is DirectoryInfo )
                {
                    if ( this.progressBar2.Value < this.progressBar2.Maximum )
                        this.progressBar2.Value++;
                    else
                    {
                        //Should not get here
                        System.Diagnostics.Debug.Assert( this.progressBar2.Value < this.progressBar2.Maximum );
                    }

                    DirectoryInfo dir = (DirectoryInfo)( node.Tag );

                    //Check if directory has been expanded before
                    if ( node.Nodes.Count != 0 )
                    {
                        if ( node.Nodes[0].Text == gDummyFolder )
                        {
                            //node is checked and has not been expanded before!
                            //==> check all paths below
                            DirectoryInfo[] dirSubs = dir.GetDirectories();

                            // We found more nodes!  Increase the progressbar maximum.
                            // The alternative is to get an accurate number of nodes/subnodes
                            // before we start.
                            progressBar2.Maximum += dirSubs.Length;
                            foreach ( DirectoryInfo dirsub in dirSubs )
                            {
                                TreeNode subNode = new TreeNode( dirsub.Name );
                                subNode.Nodes.Add( gDummyFolder );
                                subNode.Tag = dirsub; //treeViewImages
                                GetImageFiles( subNode, first, importFiles );
                            }
                        }
                    }
                    FileInfo[] dirfiles;
                    try
                    {
                        dirfiles = dir.GetFiles();
                    }
                    catch ( UnauthorizedAccessException )
                    {
                        return;
                    }

                    this.lblProgress.Text = dir.FullName;
                    ImportControlState state = new ImportControlState( m_Activities, this.Visible );
                    foreach ( FileInfo file in dirfiles )
                    {
                        if ( Functions.IsNormalFile( file ) &&
                            (Functions.GetMediaType( file.Extension ) != ImageData.DataTypes.Nothing) &&
                            ( !importFiles.Contains( file ) ) &&
                            //prune filter: Use file modified date
                                (file.LastWriteTimeUtc > first) )
                        {
                            importFiles.Add( file );
                            SetLabelText( Resources.ImportControl_addingFile + " " + file.Name );
                            Application.DoEvents();
                            if ( !state.Equals( new ImportControlState( m_Activities, this.Visible ) ) )
                                throw new ImportControlException( ImportControlException.Error_ActivityChanged );
                        }
                    }
                }
            }
            catch ( ImportControlException )
            {
                throw;
            }
            catch ( UnauthorizedAccessException ex )
            {
                //Nothing to do
                Console.WriteLine( ex.Message );
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );
                throw;
            }
        }

        // Get exif data for all files once and only one for sort (slow operation)
        //This is separated from finding files and add to activities to have a separate progress bar
        private List<FileInfoEx> GetExifFileTimes(IList<FileInfo> importFiles, DateTime FirstStart, DateTime LastEnd)
        {
            Application.DoEvents();
            List<FileInfoEx> fiexs = new List<FileInfoEx>();
            this.progressBar2.Style = ProgressBarStyle.Continuous;
            this.progressBar2.Maximum = importFiles.Count;
            this.ResetProgressBar();
            this.lblProgress.Text = string.Format(Resources.ExifTimeXImages, importFiles.Count);

            ImportControlState state = new ImportControlState( m_Activities, this.Visible );

            int j = 0;
            foreach (FileInfo fi in importFiles)
            {
                progressBar2.Value = j++;
                DateTime exif = Functions.GetExifOriginalTime(fi);
                //Find if image time is relevant
                //This is not necessary for single activities, but minimizes the list for multi activities
                DateTime time = Functions.GetBestTime(fi, exif, FirstStart, LastEnd);
                if (FirstStart <= time && time <= LastEnd)
                {
                    FileInfoEx fiex = new FileInfoEx();
                    fiex.fi = fi;
                    fiex.exif = exif;
                    fiexs.Add(fiex);
                }
                Application.DoEvents();
                if (!state.Equals(new ImportControlState(m_Activities, this.Visible)))
                    throw new ImportControlException(ImportControlException.Error_ActivityChanged);
            }

            this.HideProgressBar();

            return fiexs;
        }

        private ListViewItemSorter GetListViewSorter( int columnIndex, ListView lv )
        {
            ListViewItemSorter sorter = (ListViewItemSorter)lv.ListViewItemSorter;
            if ( sorter == null )
                sorter = new ListViewItemSorter();

            sorter.ColumnIndex = columnIndex;

            string columnName = lv.Columns[columnIndex].Tag as string;
            switch ( columnName )
            {
                case "colDateTime":
                    sorter.ColumnType = ListViewItemSorter.ColumnDataType.DateTime;
                    break;
                case "colImage":
                case "colGPS":
                case "colTitle":
                case "colDescription":
                    sorter.ColumnType = ListViewItemSorter.ColumnDataType.String;
                    break;
                /*case "NUMERIC":
                    sorter.ColumnType = ListViewItemSorter.ColumnDataType.Decimal;
                    break;*/
                default:
                    sorter.ColumnType = ListViewItemSorter.ColumnDataType.String;
                    break;
            }

            if ( sorter.SortDirection == SortOrder.Ascending )
                sorter.SortDirection = SortOrder.Descending;
            else
                sorter.SortDirection = SortOrder.Ascending;

            return sorter;
        }

        private static Bitmap MakeSelectedBitmap( Bitmap bmp, Color bgColor )
        {
            double dblAlpha = 128f / 255;
            for ( int x = 0; x < bmp.Width; x++ )
            {
                for ( int y = 0; y < bmp.Height; y++ )
                {
                    Color c = bmp.GetPixel( x, y );
                    Color c2 = Color.FromArgb( 255,
                        (byte)( dblAlpha * c.R + ( 1 - dblAlpha ) * bgColor.R ),
                        (byte)( dblAlpha * c.G + ( 1 - dblAlpha ) * bgColor.G ),
                        (byte)( dblAlpha * c.B + ( 1 - dblAlpha ) * bgColor.B ) );
                    bmp.SetPixel( x, y, c2 );
                }
            }
            return bmp;
        }

        private void listView_DrawTile( SolidBrush backBrush, SolidBrush foreBrush, DrawListViewItemEventArgs e )
        {
            Rectangle rectIcon = e.Item.GetBounds( ItemBoundsPortion.Icon );
            Rectangle rectItem = e.Item.GetBounds( ItemBoundsPortion.Label );

            //Draw the icon
            ImageList il = e.Item.ImageList;
            RectangleF rectImage = new RectangleF();
            if ( ( il != null ) && ( il.Images.Keys.Contains( e.Item.ImageKey ) ) )
            {
                Image img = il.Images[e.Item.ImageKey];

                rectImage = new RectangleF( (float)( rectIcon.Width - img.Width ) / 2 + rectIcon.Left,
                    (float)( rectIcon.Height - img.Height ) / 2 + rectIcon.Top,
                    img.Width,
                    img.Height );

                System.Drawing.Bitmap bmpSelected = (Bitmap)img;
                if ( ( e.State & ListViewItemStates.Selected ) != 0 )
                    bmpSelected = MakeSelectedBitmap( bmpSelected, backBrush.Color );

                e.Graphics.DrawImage( bmpSelected, rectImage );
            }

            using ( StringFormat sf = new StringFormat() )
            {
                sf.LineAlignment = StringAlignment.Near;
                sf.Alignment = StringAlignment.Near;
                sf.FormatFlags |= StringFormatFlags.NoWrap;
                sf.Trimming = StringTrimming.EllipsisCharacter | StringTrimming.Word;

                // The first 'subitem' is the item which we already drew above.
                string sDetails = "";
                for ( int i = 0; i < e.Item.SubItems.Count; i++ )
                    sDetails += e.Item.SubItems[i].Text + Environment.NewLine;
                sDetails = sDetails.Trim();

                SizeF layoutArea = new SizeF( rectItem.Width, rectItem.Height );
                SizeF s = e.Graphics.MeasureString( sDetails, e.Item.ListView.Font, layoutArea );

                RectangleF rectF = new RectangleF( rectItem.X,
                    rectItem.Top + ( rectItem.Height - s.Height ) / 2,
                    s.Width,
                    s.Height );

                e.Graphics.FillRectangle( backBrush, rectF );

                if ( ( e.State & ListViewItemStates.Selected ) != 0 )
                    e.DrawFocusRectangle();

                e.Graphics.DrawString( sDetails,
                    e.Item.ListView.Font,
                    foreBrush,
                    rectF, sf );
            }
        }

        private void listView_DrawSmallIcon( SolidBrush backBrush, SolidBrush foreBrush, DrawListViewItemEventArgs e )
        {
            //Paint the background
            Rectangle rectIcon = e.Item.GetBounds( ItemBoundsPortion.Icon );
            Rectangle rectItem = e.Item.GetBounds( ItemBoundsPortion.Label );
            e.Graphics.FillRectangle( backBrush, rectItem.X + 2,
                rectItem.Y,
                rectItem.Width - 2,
                rectItem.Height );

            if ( ( e.State & ListViewItemStates.Selected ) != 0 )
                e.DrawFocusRectangle();

            //Draw the icon
            ImageList il = e.Item.ImageList;
            RectangleF rectImage = new RectangleF();
            if ( ( il != null ) && ( il.Images.Keys.Contains( e.Item.ImageKey ) ) )
            {
                Image img = il.Images[e.Item.ImageKey];

                rectImage = new RectangleF( (float)( rectIcon.Width - img.Width ) / 2 + rectIcon.Left,
                    (float)( rectIcon.Height - img.Height ) / 2 + rectIcon.Top,
                    img.Width,
                    img.Height );

                System.Drawing.Bitmap bmpSelected = (Bitmap)img;
                if ( ( e.State & ListViewItemStates.Selected ) != 0 )
                    bmpSelected = MakeSelectedBitmap( bmpSelected, backBrush.Color );
                e.Graphics.DrawImage( bmpSelected, rectImage );
            }

            SizeF s = e.Graphics.MeasureString( e.Item.Text, e.Item.ListView.Font );
            RectangleF rectF = new RectangleF( rectItem.Left,
                ( rectItem.Height - s.Height ) / 2 + rectItem.Top,
                rectItem.Width,
                rectItem.Height );

            using ( StringFormat sf = new StringFormat() )
            {
                sf.LineAlignment = StringAlignment.Near;
                sf.Alignment = StringAlignment.Near;
                sf.FormatFlags |= StringFormatFlags.NoWrap;
                sf.Trimming = StringTrimming.EllipsisCharacter | StringTrimming.Word;

                e.Graphics.DrawString( e.Item.Text,
                    e.Item.ListView.Font,
                    foreBrush,
                    rectF.X, rectF.Y, sf );
            }
        }

        private void listView_DrawList( SolidBrush backBrush, SolidBrush foreBrush, DrawListViewItemEventArgs e )
        {
            //Paint the background
            Rectangle rectIcon = e.Item.GetBounds( ItemBoundsPortion.Icon );
            Rectangle rectItem = e.Item.GetBounds( ItemBoundsPortion.Label );
            e.Graphics.FillRectangle( backBrush, rectItem.X + 2,
                rectItem.Y,
                rectItem.Width - 2,
                rectItem.Height );

            if ( ( e.State & ListViewItemStates.Selected ) != 0 )
                e.DrawFocusRectangle();

            //Draw the icon
            ImageList il = e.Item.ImageList;
            RectangleF rectImage = new RectangleF();
            if ( ( il != null ) && ( il.Images.Keys.Contains( e.Item.ImageKey ) ) )
            {
                Image img = il.Images[e.Item.ImageKey];

                rectImage = new RectangleF( (float)( rectIcon.Width - img.Width ) / 2 + rectIcon.Left,
                    (float)( rectIcon.Height - img.Height ) / 2 + rectIcon.Top,
                    img.Width,
                    img.Height );

                System.Drawing.Bitmap bmpSelected = (Bitmap)img;
                if ( ( e.State & ListViewItemStates.Selected ) != 0 )
                    bmpSelected = MakeSelectedBitmap( bmpSelected, backBrush.Color );
                e.Graphics.DrawImage( bmpSelected, rectImage );

            }

            SizeF s = e.Graphics.MeasureString( e.Item.Text, e.Item.ListView.Font );

            RectangleF rectF = new RectangleF( rectItem.Left,
                rectItem.Top,
                rectItem.Width,
                rectItem.Height );

            using ( StringFormat sf = new StringFormat() )
            {
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Near;
                sf.FormatFlags |= StringFormatFlags.NoWrap;
                sf.Trimming = StringTrimming.EllipsisCharacter | StringTrimming.Word;

                e.Graphics.DrawString( e.Item.Text,
                    e.Item.ListView.Font,
                    foreBrush,
                    rectF, sf );
            }
        }

        private void listView_DrawLargeIcon( SolidBrush backBrush, SolidBrush foreBrush, DrawListViewItemEventArgs e )
        {
            //Paint the background
            Rectangle rectIcon = e.Item.GetBounds( ItemBoundsPortion.Icon );
            Rectangle rectItem = e.Item.GetBounds( ItemBoundsPortion.Label );
            e.Graphics.FillRectangle( backBrush, rectItem );

            if ( ( e.State & ListViewItemStates.Selected ) != 0 )
                e.DrawFocusRectangle();

            //Draw the icon
            ImageList il = e.Item.ImageList;
            RectangleF rectImage = new RectangleF();
            if ( ( il != null ) && ( il.Images.Keys.Contains( e.Item.ImageKey ) ) )
            {
                Image img = il.Images[e.Item.ImageKey];

                rectImage = new RectangleF( (float)( rectIcon.Width - img.Width ) / 2 + rectIcon.Left,
                    (float)( rectIcon.Height - img.Height ) / 2 + rectIcon.Top,
                    img.Width,
                    img.Height );

                System.Drawing.Bitmap bmpSelected = (Bitmap)img;
                if ( ( e.State & ListViewItemStates.Selected ) != 0 )
                    bmpSelected = MakeSelectedBitmap( bmpSelected, backBrush.Color );
                e.Graphics.DrawImage( bmpSelected, rectImage );
            }

            RectangleF rectF = new RectangleF( rectItem.Left - 5,
                rectItem.Top,
                rectItem.Width + 10,
                rectItem.Height );

            using ( StringFormat sf = new StringFormat() )
            {
                sf.LineAlignment = StringAlignment.Far;
                sf.Alignment = StringAlignment.Center;
                sf.FormatFlags |= StringFormatFlags.LineLimit;
                sf.Trimming = StringTrimming.EllipsisCharacter | StringTrimming.Word;

                e.Graphics.DrawString( e.Item.Text,
                    e.Item.ListView.Font,
                    foreBrush,
                    rectF, sf );
            }
        }

        private void listView_DrawDetails( SolidBrush backBrush, SolidBrush foreBrush, DrawListViewItemEventArgs e )
        {
            //Paint the background
            Rectangle rectIcon = e.Item.GetBounds( ItemBoundsPortion.Icon );
            Rectangle rectItem = e.Item.GetBounds( ItemBoundsPortion.ItemOnly );
            e.Graphics.FillRectangle( backBrush, rectIcon.Right + 2,
                rectItem.Y,
                rectItem.Width - ( rectIcon.Right + 2 ),
                rectItem.Height );

            if ( ( e.State & ListViewItemStates.Selected ) != 0 )
                e.DrawFocusRectangle();

            //Draw the icon
            e.Item.IndentCount = 0;	// Set the indent
            ImageList il = e.Item.ImageList;
            if ( ( il != null ) && ( il.Images.Keys.Contains( e.Item.ImageKey ) ) )
            {
                System.Drawing.Bitmap bmpSelected = (Bitmap)il.Images[e.Item.ImageKey];
                if ( ( e.State & ListViewItemStates.Selected ) != 0 )
                    bmpSelected = MakeSelectedBitmap( bmpSelected, backBrush.Color );
                e.Graphics.DrawImage( bmpSelected, rectIcon );
            }

            // Calculate the rectangle for the first label
            int colTextWidth = e.Item.ListView.Columns[0].Width;
            if ( rectItem.Width < e.Item.ListView.Columns[0].Width )
                colTextWidth = rectItem.Width;

            colTextWidth -= ( rectIcon.Width + 2 );

            RectangleF rectF = new RectangleF( rectIcon.Right + 2,
                rectItem.Y,
                colTextWidth,
                rectItem.Height );

            using ( StringFormat sf = new StringFormat() )
            {
                // Set an appropriate alignment
                sf.LineAlignment = StringAlignment.Center;
                sf.Trimming = StringTrimming.EllipsisWord;

                if ( e.Item.Text.Contains( " " ) )
                    sf.FormatFlags |= StringFormatFlags.LineLimit;
                else
                    sf.FormatFlags |= StringFormatFlags.NoWrap;

                // Draw the string
                e.Graphics.DrawString( e.Item.Text,
                    e.Item.ListView.Font,
                    foreBrush,
                    rectF, sf );
            }

            // The first 'subitem' is the item which we alredy drew above.
            for ( int i = 1; i < e.Item.SubItems.Count; i++ )
            {
                using ( StringFormat sf = new StringFormat() )
                {
                    // Set an appropriate alignment
                    sf.LineAlignment = StringAlignment.Center;
                    sf.Trimming = StringTrimming.EllipsisWord;
                    if ( e.Item.SubItems[i].Text.Contains( " " ) )
                        sf.FormatFlags |= StringFormatFlags.LineLimit;
                    else
                        sf.FormatFlags |= StringFormatFlags.NoWrap;

                    // Draw the subitems.
                    Rectangle rSubs = e.Item.SubItems[i].Bounds;
                    e.Graphics.DrawString( e.Item.SubItems[i].Text,
                        e.Item.ListView.Font,
                        foreBrush, rSubs, sf );
                }
            }

        }

        #endregion

        #region Eventhandlers

        #region treeViewImages
        private void treeViewImages_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {

            //prevent recursive firing of events, disable callbacks when setting
            TreeView tvw = (TreeView)(sender);
            TreeNode currentNode = e.Node;

            //Check if the node has been expanded before,
            // if not, add new nodes
            if (currentNode.Nodes.Count > 0)
            {
                if (currentNode.Nodes[0].Text == gDummyFolder)
                {
                    currentNode.Nodes.Clear();
                    GetSubDirectoryNodes(currentNode);
                }
            }
        }

        private void treeViewImages_AfterCheck( object sender, TreeViewEventArgs e )
        {
            SetCheck( e.Node, e.Node.Checked );
            if (e.Node.Checked && ! e.Node.IsSelected)
            {
                //revert this change, not setting subnodes correctly
                //this.treeViewImages.AfterCheck -= new System.Windows.Forms.TreeViewEventHandler(this.treeViewImages_AfterCheck);
                //this.treeViewImages.SelectedNode = e.Node;
                //this.treeViewImages.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeViewImages_AfterCheck);
            }
        }
        private void treeViewImages_AfterSelect( object sender, TreeViewEventArgs e )
        {
            TreeNode currentNode = e.Node;
            if (currentNode.Tag is DirectoryInfo)
            {
                DirectoryInfo driveDir = currentNode.Tag as DirectoryInfo;

                //Directory selected
                ActivityPicturePlugin.Source.Settings.LastImportDirectory = driveDir.FullName;
                ShowFolderPics(driveDir);
                if (currentNode.Nodes.Count > 0)
                {
                    if (currentNode.Nodes[0].Text == gDummyFolder)
                    {
                        currentNode.Nodes.Clear();
                        GetSubDirectoryNodes(currentNode);
                    }
                }
            }
        }

        private void treeViewImages_EnabledChanged( object sender, EventArgs e )
        {
            if ( treeViewImages.Enabled )
                treeViewImages.BackColor = m_visualTheme.Window;
            else
                treeViewImages.BackColor = m_visualTheme.Control;
        }

        private void treeViewImages_MouseClick( object sender, MouseEventArgs e )
        {
            // Left mouse button already selects properly.
            // Need to make this distinction here due to issues with 
            // expanding nodes with left button, autoscroll, and GetNodeAt.
            if ( e.Button == System.Windows.Forms.MouseButtons.Right )
                treeViewImages.SelectedNode = treeViewImages.GetNodeAt( e.Location );
        }

        private void treeViewImages_DrawNode( object sender, DrawTreeNodeEventArgs e )
        {
            using ( SolidBrush selectedBrush = new SolidBrush( m_visualTheme.Selected ) )
            using ( SolidBrush highlightedBrush = new SolidBrush( m_visualTheme.Highlight ) )
            using ( SolidBrush treeBackBrush = new SolidBrush( treeViewImages.BackColor ) )
            {
                Color foreColor = e.Node.TreeView.ForeColor;
                // Highlight the captured node and only the captured node
                if ( CapturedTreeViewImagesNode != null )
                {
                    if ( e.Node == CapturedTreeViewImagesNode )
                    {
                        if ( treeViewImages.Focused )
                        {
                            foreColor = m_visualTheme.SelectedText;
                            e.Graphics.FillRectangle( selectedBrush, e.Node.Bounds );
                        }
                        else
                        {
                            foreColor = m_visualTheme.HighlightText;
                            e.Graphics.FillRectangle( highlightedBrush, e.Node.Bounds );
                        }
                    }
                    else  // This node is not captured.  Use default background
                    {
                        e.Graphics.FillRectangle( treeBackBrush, e.Node.Bounds );
                    }
                }
                else  // No node is captured.  If a node is selected, highlight it.
                {
                    if ( e.Node.IsSelected )
                    {
                        if ( treeViewImages.Focused )
                        {
                            foreColor = m_visualTheme.SelectedText;
                            e.Graphics.FillRectangle( selectedBrush, e.Node.Bounds );
                        }
                        else
                        {
                            foreColor = m_visualTheme.HighlightText;
                            e.Graphics.FillRectangle( highlightedBrush, e.Node.Bounds );
                        }
                    }
                    else  // This node is not selected.  Use default background
                        e.Graphics.FillRectangle( treeBackBrush, e.Node.Bounds );
                }

                Rectangle rZero = new Rectangle( 0, 0, 0, 0 );
                if ( e.Node.Bounds != rZero )
                {
                    using ( SolidBrush foreBrush = new SolidBrush( foreColor ) )
                    {
                        // Must pass e.Bounds.X and e.Bounds.Y.
                        // Using the e.Bounds overload can result in
                        // node length vs string length issues.
                        e.Graphics.DrawString( e.Node.Text,
                            e.Node.TreeView.Font,
                            foreBrush,
                            e.Bounds.X, e.Bounds.Y );
                    }
                }
            }

        }

        private void treeViewImages_MouseDown( object sender, MouseEventArgs e )
        {
            if ( e.Button == System.Windows.Forms.MouseButtons.Left )
            {
                TreeViewHitTestInfo hit = treeViewImages.HitTest( e.Location );

                // We want to make sure they hit the item and not the +- or checkbox
                if ( ( hit.Node != null ) && ( hit.Node.Bounds.Right >= e.X ) && ( hit.Node.Bounds.Left <= e.X ) )
                {
                    CapturedTreeViewImagesNode = hit.Node;
                }
            }
        }

        private void treeViewImages_MouseUp( object sender, MouseEventArgs e )
        {
            if ( e.Button == System.Windows.Forms.MouseButtons.Left )
            {
                if ( CapturedTreeViewImagesNode != null )
                    CapturedTreeViewImagesNode = null;
            }
        }

        private void treeViewImages_MouseMove( object sender, MouseEventArgs e )
        {
            if ( e.Button == System.Windows.Forms.MouseButtons.Left )
            {
                if ( CapturedTreeViewImagesNode != null )
                    CapturedTreeViewImagesNode = null;
            }
        }
        #endregion

        #region treeViewActivities
        private void treeViewActivities_AfterSelect( object sender, TreeViewEventArgs e )
        {
            if ( e.Node.Tag != null )
            {
                if ( e.Node.Tag is IActivity ) //Activity is selected
                {
                    AddImagesToListViewAct( e.Node, true );
                    return;
                }
            }
            //if no activity is selected
            this.listViewAct.Items.Clear();
        }

        private void treeViewActivities_EnabledChanged( object sender, EventArgs e )
        {
            if ( treeViewActivities.Enabled )
                treeViewActivities.BackColor = m_visualTheme.Window;
            else
                treeViewActivities.BackColor = m_visualTheme.Control;
        }

        private void treeViewActivities_NodeMouseClick( object sender, TreeNodeMouseClickEventArgs e )
        {
            // Left mouse button already selects properly.
            // Need to make this distinction here due to issues with
            // expanding nodes with left button, autoscroll, and GetNodeAt.
            if ( e.Button == System.Windows.Forms.MouseButtons.Right )
                treeViewActivities.SelectedNode = treeViewActivities.GetNodeAt( e.Location );
        }

        private void treeViewActivities_NodeMouseDoubleClick(object sender, System.Windows.Forms.TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is IActivity)
            {
                IActivity act = e.Node.Tag as IActivity;

                string bookmark = "id=" + act;
                Plugin.GetApplication().ShowView(GUIDs.DailyActivityView, bookmark);

#if ST_2_1
                if ( act.HasStartTime )
                    Plugin.GetApplication().Calendar.Selected = act.StartTime;
                else
                    Plugin.GetApplication().Calendar.Selected = ActivityInfoCache.Instance.GetInfo( act ).ActualTrackStart;
#endif
            }
        }

        private void treeViewActivities_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            using ( SolidBrush selectedBrush = new SolidBrush( m_visualTheme.Selected ) )
            using ( SolidBrush highlightedBrush = new SolidBrush( m_visualTheme.Highlight ) )
            using ( SolidBrush unselectedBrush = new SolidBrush( e.Node.BackColor ) )
            {
                Color foreColor = e.Node.TreeView.ForeColor;
                // Highlight the captured node and only the captured node
                if ( CapturedTreeViewActNode != null )
                {
                    if ( e.Node == CapturedTreeViewActNode )
                    {
                        if ( treeViewActivities.Focused )
                        {
                            foreColor = m_visualTheme.SelectedText;
                            e.Graphics.FillRectangle( selectedBrush, e.Node.Bounds );
                        }
                        else
                        {
                            foreColor = m_visualTheme.HighlightText;
                            e.Graphics.FillRectangle( highlightedBrush, e.Node.Bounds );
                        }
                    }
                    else  // This node is not captured.  Use default background
                    {
                        e.Graphics.FillRectangle( unselectedBrush, e.Node.Bounds );
                    }
                }
                else  // No node is captured.  If a node is selected, highlight it.
                {
                    if ( e.Node.IsSelected )
                    {
                        if ( treeViewActivities.Focused )
                        {
                            foreColor = m_visualTheme.SelectedText;
                            e.Graphics.FillRectangle( selectedBrush, e.Node.Bounds );
                        }
                        else
                        {
                            foreColor = m_visualTheme.HighlightText;
                            e.Graphics.FillRectangle( highlightedBrush, e.Node.Bounds );
                        }
                    }
                    else  // This node is not selected.  Use default background
                        e.Graphics.FillRectangle( unselectedBrush, e.Node.Bounds );
                }

                using ( SolidBrush foreBrush = new SolidBrush( foreColor ) )
                {
                    Rectangle rZero = new Rectangle( 0, 0, 0, 0 );
                    if ( e.Node.Bounds != rZero )
                    {
                        e.Graphics.DrawString( e.Node.Text,
                            e.Node.TreeView.Font,
                            foreBrush,
                            e.Bounds.X, e.Bounds.Y );

                        if ( e.State == TreeNodeStates.Hot )
                        {
                            Pen penHot = SystemPens.HotTrack;
                            SizeF fSize = e.Graphics.MeasureString( e.Node.Text, e.Node.TreeView.Font );
                            PointF p1 = new PointF( e.Node.Bounds.Left, e.Node.Bounds.Top + fSize.Height - 2 );
                            PointF p2 = new PointF( e.Node.Bounds.Right - 4, e.Node.Bounds.Top + fSize.Height - 2 );

                            e.Graphics.DrawLine( penHot,
                                p1,
                                p2 );
                        }
                    }
                }
            }

        }

        private void treeViewActivities_MouseDown( object sender, MouseEventArgs e )
        {
            if ( e.Button == System.Windows.Forms.MouseButtons.Left )
            {
                TreeViewHitTestInfo hit = treeViewActivities.HitTest( e.Location );
                if ( ( hit.Node != null ) && ( hit.Node.Bounds.Right >= e.X ) && ( hit.Node.Bounds.Left <= e.X ) )
                    CapturedTreeViewActNode = hit.Node;
            }
        }

        private void treeViewActivities_MouseUp( object sender, MouseEventArgs e )
        {
            if ( e.Button == System.Windows.Forms.MouseButtons.Left )
            {
                if ( CapturedTreeViewActNode != null )
                    CapturedTreeViewActNode = null;
            }
        }

        private void treeViewActivities_MouseMove( object sender, MouseEventArgs e )
        {
            if ( e.Button == System.Windows.Forms.MouseButtons.Left )
            {
                if ( CapturedTreeViewActNode != null )
                    CapturedTreeViewActNode = null;
            }
        }

        #endregion

        #region listViewDrive
        private void listViewDrive_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.Control )
            {
                switch ( e.KeyCode )
                {
                    case Keys.A:
                        foreach ( ListViewItem l in listViewDrive.Items )
                        {
                            l.Selected = true;
                        }
                        break;
                    case Keys.C:
                        break;
                }
            }
        }

        private void listViewDrive_ItemDrag( object sender, ItemDragEventArgs e )
        {
            ListViewItem[] l = new ListViewItem[this.listViewDrive.SelectedItems.Count];
            for ( int i = 0; i < this.listViewDrive.SelectedItems.Count; i++ )
            {
                l[i] = this.listViewDrive.SelectedItems[i];
            }
            DoDragDrop( l, DragDropEffects.Copy );
        }

        private void listViewDrive_DoubleClick( object sender, EventArgs e )
        {
            try
            {
                FileInfo file = (FileInfo)(listViewDrive.FocusedItem.Tag);
                ImageData.DataTypes dt = Functions.GetMediaType(file.FullName);
                Functions.OpenExternal(file.FullName, dt);
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }
        }

        private void listViewDrive_ColumnClick( object sender, ColumnClickEventArgs e )
        {
            ListViewItemSorter sorter = GetListViewSorter( e.Column, listViewDrive );

            listViewDrive.ListViewItemSorter = sorter;
            listViewDrive.Sort();
        }

        private void listViewDrive_DrawItem( object sender, DrawListViewItemEventArgs e )
        {
            Color foreColor = e.Item.ListView.ForeColor;
            Color backColor = listViewDrive.BackColor;

            if ( e.Item.Selected )
            {
                if ( listViewDrive.Focused )
                {
                    foreColor = m_visualTheme.SelectedText;
                    backColor = m_visualTheme.Selected;
                }
                else if ( !e.Item.ListView.HideSelection )
                {
                    foreColor = m_visualTheme.HighlightText;
                    backColor = m_visualTheme.Highlight;
                }
            }

            using ( SolidBrush backBrush = new SolidBrush( backColor ),
                               foreBrush = new SolidBrush( foreColor ) )
            {
                switch ( e.Item.ListView.View )
                {
                    case View.Details:
                        listView_DrawDetails( backBrush, foreBrush, e );
                        break;
                    case View.LargeIcon:
                        listView_DrawLargeIcon( backBrush, foreBrush, e );
                        break;
                    case View.List:
                        listView_DrawList( backBrush, foreBrush, e );
                        break;
                    case View.SmallIcon:
                        listView_DrawSmallIcon( backBrush, foreBrush, e );
                        break;
                    case View.Tile:
                        listView_DrawTile( backBrush, foreBrush, e );
                        break;
                }
            }
        }

        private void listViewDrive_DrawColumnHeader( object sender, DrawListViewColumnHeaderEventArgs e )
        {
            // Would be nice to have a sort direction glyph.
            // Might be too much work.
            e.DrawDefault = true;
        }

        #endregion

        #region listViewAct
        private void listViewAct_DragEnter( object sender, DragEventArgs e )
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void listViewAct_DragDrop( object sender, DragEventArgs e )
        {
            try
            {
                if ( e.Data.GetDataPresent( "System.Windows.Forms.ListViewItem[]", false ) )
                {
                    ListViewItem[] l = (ListViewItem[])( e.Data.GetData( "System.Windows.Forms.ListViewItem[]" ) );
                    this.UseWaitCursor = true;
                    AddSelectedImagesToActivity( l );
                    this.UseWaitCursor = false;
                    Cursor.Position = Cursor.Position;  // Trigger cursor update: UseWaitCursor = false
                    this.ActivityImagesChanged( this, new ActivityImagesChangedEventArgs( listViewAct.Items ) );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
                throw;
            }
        }

        private void listViewAct_KeyDown( object sender, KeyEventArgs e )
        {
            switch ( e.KeyCode )
            {
                case Keys.Delete:
                case Keys.Back:
                    if ( MessageBox.Show( Resources.ConfirmDeleteLong_Text, Resources.ConfirmDeleteShort_Text,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation ) == DialogResult.Yes )
                    {
                        //Delete selected images
                        ListView.SelectedListViewItemCollection sel = listViewAct.SelectedItems;
                        ListViewItem[] lvis = new ListViewItem[sel.Count];
                        sel.CopyTo( (Array)lvis, 0 );
                        RemoveSelectedImagesFromActivity( lvis );
                        this.ActivityImagesChanged( this, new ActivityImagesChangedEventArgs( listViewAct.Items ) );
                        lblProgress.Text = String.Format( Resources.FoundImagesInActivity_Text, this.listViewAct.Items.Count );
                    }
                    break;
                case Keys.A:
                    foreach ( ListViewItem l in listViewAct.Items )
                    {
                        l.Selected = true;
                    }
                    break;
                case Keys.V:
                    if ( e.Control )
                    {
                        ListViewItem[] lvisel = new ListViewItem[this.listViewDrive.SelectedItems.Count];
                        this.listViewDrive.SelectedItems.CopyTo( lvisel, 0 );
                        this.UseWaitCursor = true;
                        AddSelectedImagesToActivity( lvisel );
                        this.UseWaitCursor = false;
                        Cursor.Position = Cursor.Position;  // Trigger cursor update: UseWaitCursor = false
                    }
                    break;
            }
        }

        private void listViewAct_DoubleClick( object sender, EventArgs e )
        {
            try
            {
                ImageData im = (ImageData)(listViewAct.FocusedItem.Tag);
                Functions.OpenExternal(im.PhotoSource, im.Type);
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );
            }
        }

        void listViewAct_ItemSelectionChanged(object sender, System.Windows.Forms.ListViewItemSelectionChangedEventArgs e)
        {
#if !ST_2_1
            if (listViewAct.SelectedItems != null && listViewAct.SelectedItems.Count > 0 && this.m_layer != null)
            {
                IList<ImageData> images = new List<ImageData>();
                foreach (ListViewItem lvi in listViewAct.SelectedItems)
                {
                    ImageData im = (ImageData)(lvi.Tag);
                    images.Add(im);
                }
                this.m_layer.SelectedPictures = images;
            }
#endif
        }

        private void listViewAct_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListViewItemSorter sorter = GetListViewSorter( e.Column, listViewAct );

            listViewAct.ListViewItemSorter = (System.Collections.IComparer)sorter;
            listViewAct.Sort();
        }

        private void listViewAct_DrawItem( object sender, DrawListViewItemEventArgs e )
        {
            Color foreColor = e.Item.ListView.ForeColor;
            Color backColor = listViewAct.BackColor;

            if ( e.Item.Selected )
            {
                if ( listViewAct.Focused )
                {
                    foreColor = m_visualTheme.SelectedText;
                    backColor = m_visualTheme.Selected;
                }
                else if ( !e.Item.ListView.HideSelection )
                {
                    foreColor = m_visualTheme.HighlightText;
                    backColor = m_visualTheme.Highlight;
                }
            }

            using ( SolidBrush backBrush = new SolidBrush( backColor ),
                               foreBrush = new SolidBrush( foreColor ) )
            {
                switch ( e.Item.ListView.View )
                {
                    case View.Details:
                        listView_DrawDetails( backBrush, foreBrush, e );
                        break;
                    case View.LargeIcon:
                        listView_DrawLargeIcon( backBrush, foreBrush, e );
                        break;
                    case View.List:
                        listView_DrawList( backBrush, foreBrush, e );
                        break;
                    case View.SmallIcon:
                        listView_DrawSmallIcon( backBrush, foreBrush, e );
                        break;
                    case View.Tile:
                        listView_DrawTile( backBrush, foreBrush, e );
                        break;
                }
            }
        }

        private void listViewAct_DrawColumnHeader( object sender, DrawListViewColumnHeaderEventArgs e )
        {
            // Would be nice to have a sort direction glyph.
            // Might be too much work.
            e.DrawDefault = true;
        }

        #endregion

        private void btnExpandAll_Click( object sender, EventArgs e )
        {
            this.treeViewActivities.ExpandAll();
        }

        private void btnCollapsAll_Click( object sender, EventArgs e )
        {
            while ( this.treeViewActivities.SelectedNode.Parent != null )
                this.treeViewActivities.SelectedNode = this.treeViewActivities.SelectedNode.Parent;
            this.treeViewActivities.CollapseAll();
        }

        private void ImportControl_Load( object sender, EventArgs e )
        {
            this.Size = this.Parent.Size;
        }

        private void GetTreeSelectedActivities2(IList<TreeNode> nodes, IList<IActivity> acts)
        {
            if (nodes != null)
            {
                foreach (TreeNode node in nodes)
                {
                    if (node.Nodes != null)
                    {
                        IList<TreeNode> n2s = new List<TreeNode>();
                        foreach (TreeNode n2 in node.Nodes)
                        {
                            n2s.Add(n2);
                        }
                        GetTreeSelectedActivities2(n2s, acts);
                    }
                    if(node.Tag != null && node.Tag is IActivity)
                    {
                        acts.Add(node.Tag as IActivity);
                    }
                }
            }

        }
        private IList<IActivity> GetTreeSelectedActivities()
        {
            IList<IActivity> selActs;
            TreeNode selNode = this.treeViewActivities.SelectedNode;
            if (selNode == null)
            {
                //Safety check, this should not occur
                selActs = this.m_Activities;
            }
            else
            {
                selActs = new List<IActivity>();
                GetTreeSelectedActivities2(new List<TreeNode>{selNode}, selActs);
            }
            return selActs;
        }

        private void btnScan_Click( object sender, EventArgs e )
        {
            try
            {
                ImportControlState state = new ImportControlState( m_Activities, this.Visible );
                this.UseWaitCursor = true;

                EnableControl( false );
                this.progressBar2.Style = ProgressBarStyle.Marquee;
                Application.DoEvents();
                if ( !state.Equals( new ImportControlState( m_Activities, this.Visible ) ) )
                    throw new ImportControlException( ImportControlException.Error_ActivityChanged );

                IList<IActivity> selActs = this.GetTreeSelectedActivities();
                DateTime FirstStart = DateTime.MaxValue;
                DateTime LastEnd = DateTime.MinValue;

                foreach (IActivity activity in selActs)
                {
                    DateTime start;
                    if (activity.HasStartTime)
                    {
                        start = activity.StartTime;
                    }
                    else
                    {
                        //not normally occurring as activities should have tracks...
                        start = ActivityInfoCache.Instance.GetInfo(activity).ActualTrackStart;
                    }
                    if (start < FirstStart)
                    {
                        FirstStart = start;
                    }

                    DateTime end = GetActivityEndTime(activity);
                    if (end > LastEnd)
                    {
                        LastEnd = end;
                    }
                }

                IList<FileInfo> importFiles = ThreadGetImages(FirstStart);
                //Gets EXIF time for images
                List<FileInfoEx> fiexs = GetExifFileTimes(importFiles, FirstStart, LastEnd);
                int numFilesImported = FindAndImportImages(fiexs, selActs);
                
                AddImagesToListViewAct( this.treeViewActivities.SelectedNode, true );

                if ( numFilesImported > 0 )
                {
                    this.ActivityImagesChanged( this, new ActivityImagesChangedEventArgs( listViewAct.Items ) );
                }

                this.lblProgress.Text = String.Format( Properties.Resources.ImportControl_scanDone, numFilesImported );
            }
            catch ( ImportControlException ex )
            {
                HideProgressBar();
                MessageBox.Show( ex.Message,
                    Properties.Resources.OperationAborted_Text, 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Exclamation );

                //System.Diagnostics.Debug.Print( ex.Message );
            }
            finally
            {
                EnableControl( true );
                this.UseWaitCursor = false;
                Cursor.Position = Cursor.Position;  // Trigger cursor update: UseWaitCursor = false
            }
        }

        private void btnChangeFolderView_Click( object sender, EventArgs e )
        {
            if ( m_viewFolder == 4 ) m_viewFolder = 0;
            else m_viewFolder++;
            this.listViewDrive.View = (View)( m_viewFolder );
            ActivityPicturePlugin.Source.Settings.FolderView = m_viewFolder;
            this.listViewDrive.Invalidate();
        }

        private void btnChangeActivityView_Click( object sender, EventArgs e )
        {
            if ( m_viewActivity == 4 ) m_viewActivity = 0;
            else m_viewActivity++;
            this.listViewAct.View = (View)( m_viewActivity );
            ActivityPicturePlugin.Source.Settings.ActivityView = m_viewActivity;
            this.listViewAct.Invalidate();
        }

        private void timerProgressBar_Tick( object sender, EventArgs e )
        {
            timerProgressBar.Stop();
            HideProgressBar();
        }

        private void toolStripMenuRemove_Click( object sender, EventArgs e )
        {
            if ( MessageBox.Show(Resources.ConfirmDeleteLong_Text , Resources.ConfirmDeleteShort_Text,
                 MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation ) == DialogResult.Yes )
            {
                ListView.SelectedListViewItemCollection sel = listViewAct.SelectedItems;
                ListViewItem[] lvis = new ListViewItem[sel.Count];
                sel.CopyTo( (Array)lvis, 0 );
                RemoveSelectedImagesFromActivity( lvis );
                lblProgress.Text = String.Format( Resources.FoundImagesInActivity_Text, this.listViewAct.Items.Count );
                this.ActivityImagesChanged( this, new ActivityImagesChangedEventArgs( listViewAct.Items ) );
            }
        }

        private void toolStripMenuAdd_Click( object sender, EventArgs e )
        {
            ListView.SelectedListViewItemCollection sel = listViewDrive.SelectedItems;
            ListViewItem[] lvis = new ListViewItem[sel.Count];
            sel.CopyTo( (Array)lvis, 0 );
            this.UseWaitCursor = true;
            AddSelectedImagesToActivity( lvis );
            this.UseWaitCursor = false;
            Cursor.Position = Cursor.Position;  // Trigger cursor update: UseWaitCursor = false
            lblProgress.Text = String.Format( Properties.Resources.FoundImagesInActivity_Text, this.listViewAct.Items.Count );
            this.ActivityImagesChanged( this, new ActivityImagesChangedEventArgs( listViewAct.Items ) );
        }

        private void contextMenuListViewDrive_Opening( object sender, CancelEventArgs e )
        {
            if ( listViewDrive.SelectedItems.Count == 0 )
                e.Cancel = true;
        }

        private void contextMenuListViewAct_Opening( object sender, CancelEventArgs e )
        {
            if ( listViewAct.SelectedItems.Count == 0 )
                e.Cancel = true;
        }

        private void toolStripMenuRefresh_Click( object sender, EventArgs e )
        {
            TreeNode selNode = treeViewImages.SelectedNode;
            selNode.Nodes.Clear();
            GetSubDirectoryNodes( selNode );
        }

        private void splitContainer2_MouseDoubleClick( object sender, MouseEventArgs e )
        {
            if ( e.Button == System.Windows.Forms.MouseButtons.Left )
            {
                if ( this.splitContainer2.SplitterRectangle.Contains( e.X, e.Y ) )
                    this.splitContainer2.SplitterDistance = this.splitContainer3.SplitterDistance;
            }
        }

        private void splitContainer3_MouseDoubleClick( object sender, MouseEventArgs e )
        {
            if ( e.Button == System.Windows.Forms.MouseButtons.Left )
            {
                if ( this.splitContainer3.SplitterRectangle.Contains( e.X, e.Y ) )
                    this.splitContainer3.SplitterDistance = this.splitContainer2.SplitterDistance;
            }
        }

        private void toolStripMenuCopyToClipboard_Click( object sender, EventArgs e )
        {
            TreeNode selNode = this.treeViewActivities.SelectedNode;
            //List separator (tab could be used too)
            string sep = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            if (selNode != null)
            {
                //Hardcoded fields and order at this time
                string sClipboard =
                    CommonResources.Text.LabelName + sep +
                    Resources.photoSourceDataGridViewTextBoxColumn_HeaderText + sep +
                    CommonResources.Text.LabelDate + sep +
                    CommonResources.Text.LabelGPSLocation + sep +
                    Resources.titleDataGridViewTextBoxColumn_HeaderText + sep +
                    Resources.commentDataGridViewTextBoxColumn_HeaderText + sep +
                    Resources.referenceIDDataGridViewTextBoxColumn_HeaderText + sep +
                    Resources.FilePath_Text + sep +
                    Resources.thumbnailDataGridViewImageColumn_HeaderText;

                sClipboard += Environment.NewLine;
                foreach (ActivityTreeViewInfo a in treeViewActivities_GetImageData(selNode))
                {
                    ImageData id = a.image;
                    string sFile = a.Activity.Name + sep +
                        id.PhotoSourceFileName + sep +
                             Functions.DateTimeString(id.DateTimeOriginal).Replace(Environment.NewLine, ", ") + sep +
                             GPS.GpsString(id.GpsPoint).Replace(Environment.NewLine, ", ") + sep +
                             id.Title + sep +
                             id.Comments + sep +
                             id.ReferenceID + sep +
                             id.PhotoSource + sep +
                             id.ThumbnailPath;

                    sClipboard += sFile + Environment.NewLine;
                }
                Clipboard.SetText( sClipboard );
            }
        }

        private void toolStripMenuMigratePaths_Click(object sender, EventArgs e)
        {
            TreeNode selNode = this.treeViewActivities.SelectedNode;
            if (selNode != null)
            {
                IList<ActivityTreeViewInfo> ims = treeViewActivities_GetImageData(selNode);
                if (ims != null && ims.Count > 0)
                {
                    //Use "reasonable" default for replace path
                    string current = ims[0].image.PhotoSource;
                    int i = current.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
                    if (i > 0)
                    {
                        current = current.Substring(0, i);
                        string dest = SourcePathPopup(ref current);
                        if (!current.Equals(dest))
                        {
                            foreach (ActivityTreeViewInfo a in ims)
                            {
                                ImageData im = a.image;
                                im.SetPhotoSource (im.PhotoSource.Replace(current, dest));
                                PluginData data = Helper.Functions.ReadExtensionData(a.Activity);
                                listViewAct.SuspendLayout();

                                ImageDataSerializable ids = im.GetSerialzable(data.Images);
                                if (ids != null)
                                {
                                    ids.PhotoSource = im.PhotoSource;
                                }
                                //Thumbnail may need to be recreated
                                im.SetThumbnail();
                                listViewAct.ResumeLayout();
                                Functions.WriteExtensionData(a.Activity, data);
                            }
                        }
                    }
                }
            }
        }

        //A simple popup, could be separate control
        private string SourcePathPopup( ref string source )
        {
            string dest = source;
            string tmpSource = source;

            using ( System.Windows.Forms.Form formMigrate = new System.Windows.Forms.Form() )
            using ( ZoneFiveSoftware.Common.Visuals.Panel panel = new ZoneFiveSoftware.Common.Visuals.Panel() )
            using ( ZoneFiveSoftware.Common.Visuals.TextBox SourcePath_TextBox = new ZoneFiveSoftware.Common.Visuals.TextBox() )
            using ( ZoneFiveSoftware.Common.Visuals.TextBox DestPath_TextBox = new ZoneFiveSoftware.Common.Visuals.TextBox() )
            //using ( System.Windows.Forms.Button b = new System.Windows.Forms.Button() )
            //using ( System.Windows.Forms.Button c = new System.Windows.Forms.Button() )
            using ( ZoneFiveSoftware.Common.Visuals.Button btnOK = new ZoneFiveSoftware.Common.Visuals.Button() )
            using ( ZoneFiveSoftware.Common.Visuals.Button btnCancel = new ZoneFiveSoftware.Common.Visuals.Button() )
            using ( System.Windows.Forms.Label lblSource = new System.Windows.Forms.Label() )
            using ( System.Windows.Forms.Label lblDestination = new System.Windows.Forms.Label() )
            {
                formMigrate.SuspendLayout();

                formMigrate.Size = new System.Drawing.Size( 370, 180 );
                formMigrate.Text = Resources.MigrateSourcePath_Text;
                formMigrate.Controls.Add( panel );

                formMigrate.MaximizeBox = false;
                formMigrate.MinimizeBox = false;
                formMigrate.ShowIcon = false;
                formMigrate.ShowInTaskbar = false;
                formMigrate.SizeGripStyle = SizeGripStyle.Hide;

                panel.Dock = DockStyle.Fill;
                panel.Controls.Add( lblSource );
                panel.Controls.Add( SourcePath_TextBox );
                panel.Controls.Add( lblDestination );
                panel.Controls.Add( DestPath_TextBox );
                panel.Controls.Add( btnOK );
                panel.Controls.Add( btnCancel );
                formMigrate.AcceptButton = btnOK;
                formMigrate.CancelButton = btnCancel;

                lblSource.Location = new System.Drawing.Point( 10, 10 );
                lblSource.Text = Resources.Source_Text;
                lblSource.AutoSize = true;
                SourcePath_TextBox.Width = formMigrate.Width - 37;
                SourcePath_TextBox.Location = new System.Drawing.Point( lblSource.Left, lblSource.Bottom + 0 );
                SourcePath_TextBox.ButtonImage = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.Folder16;
                SourcePath_TextBox.ButtonClick += new EventHandler( PathTextBox_ButtonClick );


                lblDestination.Location = new System.Drawing.Point( lblSource.Left, SourcePath_TextBox.Bottom + 5 );
                lblDestination.Text = Resources.Destination_Text;
                lblDestination.AutoSize = true;
                DestPath_TextBox.Width = formMigrate.Width - 37;
                DestPath_TextBox.Location = new System.Drawing.Point( SourcePath_TextBox.Left, lblDestination.Bottom + 0 );
                DestPath_TextBox.ButtonImage = ZoneFiveSoftware.Common.Visuals.CommonResources.Images.Folder16;
                DestPath_TextBox.ButtonClick += new EventHandler( PathTextBox_ButtonClick );

                btnCancel.BackColor = System.Drawing.Color.Transparent;
                btnCancel.BorderColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 100 ) ) ) ), ( (int)( ( (byte)( 40 ) ) ) ), ( (int)( ( (byte)( 50 ) ) ) ), ( (int)( ( (byte)( 120 ) ) ) ) );
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Location = new System.Drawing.Point( formMigrate.Size.Width - 25 - btnCancel.Size.Width, formMigrate.Height - 45 - btnCancel.Height );
                btnCancel.Text = ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionCancel;
                btnCancel.Click +=
                    delegate( object sender2, EventArgs args )
                    {
                        formMigrate.Close();
                    };

                btnOK.BackColor = System.Drawing.Color.Transparent;
                btnOK.BorderColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 100 ) ) ) ), ( (int)( ( (byte)( 40 ) ) ) ), ( (int)( ( (byte)( 50 ) ) ) ), ( (int)( ( (byte)( 120 ) ) ) ) );
                btnOK.DialogResult = DialogResult.OK;
                btnOK.Location = new System.Drawing.Point( btnCancel.Location.X - 15 - btnOK.Size.Width, formMigrate.Height - 45 - btnOK.Height );
                btnOK.Text = ZoneFiveSoftware.Common.Visuals.CommonResources.Text.ActionOk;
                btnOK.Click +=
                    delegate( object sender2, EventArgs args )
                    {
                        tmpSource = SourcePath_TextBox.Text;
                        dest = DestPath_TextBox.Text;
                        formMigrate.Close();
                    };

                panel.ThemeChanged( this.m_visualTheme );
                formMigrate.BackColor = this.m_visualTheme.Control;
                SourcePath_TextBox.ThemeChanged( this.m_visualTheme );
                DestPath_TextBox.ThemeChanged( this.m_visualTheme );

                SourcePath_TextBox.Text = source;
                DestPath_TextBox.Text = source;

                formMigrate.ResumeLayout();

                //update is done in clicking OK/Enter
                formMigrate.StartPosition = FormStartPosition.CenterParent;
                formMigrate.ShowDialog( this );
                source = tmpSource;
            }

            return dest;
        }

        void PathTextBox_ButtonClick( object sender, EventArgs e )
        {
            if ( sender is ZoneFiveSoftware.Common.Visuals.TextBox )
            {
                ZoneFiveSoftware.Common.Visuals.TextBox txtFolderSelect = sender as ZoneFiveSoftware.Common.Visuals.TextBox;
                using ( FolderSelect.FolderSelectDialog fsd = new FolderSelect.FolderSelectDialog() )
                {
                    //Use ActivityPicturePlugin.Source.Settings.LastImportDirectory also here, 
                    //more likely better than  Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                    if ( !String.IsNullOrEmpty( txtFolderSelect.Text ) )
                    {
                        fsd.InitialDirectory = txtFolderSelect.Text;
                        ActivityPicturePlugin.Source.Settings.LastImportDirectory = fsd.InitialDirectory;
                    }
                    else
                        fsd.InitialDirectory = ActivityPicturePlugin.Source.Settings.LastImportDirectory;

                    fsd.Title = Resources.SelectFolder_Text;
                    if ( fsd.ShowDialog() )
                        txtFolderSelect.Text = fsd.FileName;
                }
            }
        }

        private void toolStripMenuOpenImage_Click(object sender, EventArgs e)
        {
            this.listViewAct_DoubleClick( sender, e );
        }

        private void toolStripMenuOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                IActivity act = (IActivity)( this.treeViewActivities.SelectedNode.Tag );
                PluginData data = Helper.Functions.ReadExtensionData( act );

                List<string> sFolders = new List<string>();
                foreach (ListViewItem lvi in listViewAct.SelectedItems)
                {
                    ImageData im = (ImageData)(lvi.Tag);
                    System.IO.FileInfo fi = new FileInfo(im.PhotoSource);

                    if (!sFolders.Contains(fi.DirectoryName))
                        sFolders.Add(fi.DirectoryName);
                }

                foreach ( string sFolder in sFolders )
                    Functions.OpenExternal( sFolder );

            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );
            }
        }

        private void toolStripMenuOpenThumbnail_Click(object sender, EventArgs e)
        {
            try
            {
                ImageData im = (ImageData)(listViewAct.FocusedItem.Tag);
                Functions.OpenExternal(im.ThumbnailPath, im.Type);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }
        }

        private void ImportControl_Resize( object sender, EventArgs e )
        {
            this.SuspendLayout();
            splitContainer1.Height = this.Height - ( splitContainer1.Top );
            splitContainer1.Width = this.Width - ( splitContainer1.Left * 2 );
            splitContainer3.Height = splitContainer1.Height - splitContainer3.Top;
            splitContainer2.Height = splitContainer1.Height - splitContainer2.Top;
            splitContainer3.Width = splitContainer1.Panel1.Width;
            splitContainer2.Width = splitContainer1.Panel2.Width;

            progressBar2.Top = splitContainer1.Bottom - progressBar2.Height;
            progressBar2.Left = splitContainer1.Left;
            progressBar2.Width = splitContainer1.Width;
            lblProgress.Top = progressBar2.Top + ( progressBar2.Height - lblProgress.Height ) / 2;
            lblProgress.Left = progressBar2.Left + 3;

            this.ResumeLayout();
        }

        private void splitContainer1_SplitterMoved( object sender, SplitterEventArgs e )
        {
            splitContainer3.Width = splitContainer1.Panel1.Width - splitContainer3.Left;
            splitContainer2.Width = splitContainer1.Panel2.Width;

            if ( !m_showallactivities )
                Source.Settings.ImportSplitter1Offset = splitContainer1.SplitterDistance;
            else
                Source.Settings.SettingsSplitter1Offset = splitContainer1.SplitterDistance;

        }

        private void splitContainer2_SplitterMoved( object sender, SplitterEventArgs e )
        {
            if ( !m_showallactivities )
                Source.Settings.ImportSplitter2Offset = splitContainer2.SplitterDistance;
            else
                Source.Settings.SettingsSplitter2Offset = splitContainer2.SplitterDistance;
        }

        private void splitContainer3_SplitterMoved( object sender, SplitterEventArgs e )
        {
            if ( !m_showallactivities )
                Source.Settings.ImportSplitter3Offset = splitContainer3.SplitterDistance;
            else
                Source.Settings.SettingsSplitter3Offset = splitContainer3.SplitterDistance;
        }

        private void ImportControl_ActivityImagesChanged( object sender, ImportControl.ActivityImagesChangedEventArgs e )
        {
            // Noop
        }

        private void listViewDrive_ColumnWidthChanged( object sender, ColumnWidthChangedEventArgs e )
        {
            int nWidth = listViewDrive.Columns[e.ColumnIndex].Width;
            switch ( e.ColumnIndex )
            {
                case 0: //colDImage
                    Source.Settings.ListDriveColThumbnailWidth = nWidth;
                    break;
                case 1: //colDDateTime
                    Source.Settings.ListDriveColDateTimeWidth = nWidth;
                    break;
                case 2: //colDGPS
                    Source.Settings.ListDriveColGPSWidth = nWidth;
                    break;
                case 3: //colDTitle
                    Source.Settings.ListDriveColTitleWidth = nWidth;
                    break;
                case 4: //colDDescription
                    Source.Settings.ListDriveColCommentWidth = nWidth;
                    break;
            }
        }

        private void listViewAct_ColumnWidthChanged( object sender, ColumnWidthChangedEventArgs e )
        {
            int nWidth = listViewAct.Columns[e.ColumnIndex].Width;
            switch ( e.ColumnIndex )
            {
                case 0: //colDImage
                    Source.Settings.ListActColThumbnailWidth = nWidth;
                    break;
                case 1: //colDDateTime
                    Source.Settings.ListActColDateTimeWidth = nWidth;
                    break;
                case 2: //colDGPS
                    Source.Settings.ListActColGPSWidth = nWidth;
                    break;
                case 3: //colDTitle
                    Source.Settings.ListActColTitleWidth = nWidth;
                    break;
                case 4: //colDDescription
                    Source.Settings.ListActColCommentWidth = nWidth;
                    break;
            }
        }

        #endregion

    }

    #region IComparer Implementations
    public class ListViewItemSorter : System.Collections.IComparer
    {
        public enum ColumnDataType
        {
            Decimal = 0,
            String,
            DateTime
        };

        private int columnIndex = 0;
        public int ColumnIndex
        {
            get { return columnIndex; }
            set { columnIndex = value; }
        }
        private ColumnDataType columnType;
        public ColumnDataType ColumnType
        {
            get { return columnType; }
            set { columnType = value; }
        }
        private SortOrder sortDirection = SortOrder.None;
        public SortOrder SortDirection
        {
            get { return sortDirection; }
            set { sortDirection = value; }
        }

        public int Compare( object x, object y )
        {
            ListViewItem tx = x as ListViewItem;
            ListViewItem ty = y as ListViewItem;
            string strx = ""; ;
            string stry = "";
            double dblx = double.MinValue;
            double dbly = double.MinValue;

            if ( tx.SubItems.Count > columnIndex ) strx = tx.SubItems[columnIndex].Text;
            if ( ty.SubItems.Count > columnIndex ) stry = ty.SubItems[columnIndex].Text;

            int iResult = 0;

            try
            {
                System.Globalization.CultureInfo specificCulture = Functions.NeutralToSpecificCulture( System.Globalization.CultureInfo.CurrentUICulture.Name );

                switch ( columnType )
                {
                    case ColumnDataType.DateTime:
                        DateTime datex = DateTime.MinValue;
                        DateTime datey = DateTime.MinValue;
                        if ( strx != "" ) datex = DateTime.Parse( strx, specificCulture );
                        if ( stry != "" ) datey = DateTime.Parse( stry, specificCulture );
                        iResult = DateTime.Compare( datex, datey );
                        break;
                    case ColumnDataType.Decimal:
                        if ( strx != "" ) dblx = Double.Parse( strx, specificCulture );
                        if ( stry != "" ) dbly = Double.Parse( stry, specificCulture );
                        if ( dblx < dbly ) iResult = -1;
                        else if ( dblx > dbly ) iResult = 1;
                        else iResult = 0;
                        break;
                    case ColumnDataType.String:
                        iResult = string.Compare( strx, stry );
                        break;
                    default:
                        break;
                }
            }
            catch ( Exception ex)
            {
                System.Diagnostics.Debug.Assert( false, ex.Message );
            }

            if ( sortDirection == SortOrder.None ) return 0;
            else if ( sortDirection == SortOrder.Descending ) return -iResult;
            else return iResult;

        }
    }

    public class NodeSorter : System.Collections.IComparer
    {
        public int Compare( object x, object y )
        {
            TreeNode tx = x as TreeNode;
            TreeNode ty = y as TreeNode;
            string strx, stry;

            if ( tx.Tag is ImageData ) strx = ( (ImageData)( tx.Tag ) ).PhotoSourceFileName;
            else strx = tx.Name;

            if ( ty.Tag is ImageData ) stry = ( (ImageData)( ty.Tag ) ).PhotoSourceFileName;
            else stry = ty.Name;

            return string.Compare( strx, stry );
        }
    }

    public class FileExSorter : System.Collections.IComparer
    {
        public int Compare( object x, object y )
        {
            ImportControl.FileInfoEx tx = x as ImportControl.FileInfoEx;
            ImportControl.FileInfoEx ty = y as ImportControl.FileInfoEx;
            return DateTime.Compare(Functions.GetBestTime(tx.fi, tx.exif, DateTime.MinValue, DateTime.MaxValue),
                Functions.GetBestTime(ty.fi, ty.exif, DateTime.MinValue, DateTime.MaxValue));
            //return string.Compare(tx.strDateTime, ty.strDateTime);
        }
    }

    #endregion

    #region ListViewEx
    public class ListViewEx : System.Windows.Forms.ListView
    {
        public bool DoubleBufferedEnabled
        {
            get { return base.DoubleBuffered; }
            set { base.DoubleBuffered = value; }
        }

        public ListViewEx()
        {
        }
    }
    #endregion
}