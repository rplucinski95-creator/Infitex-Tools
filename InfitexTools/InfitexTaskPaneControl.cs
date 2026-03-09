using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace InfitexTools
{
    public class InfitexTaskPaneControl : UserControl
    {
        public event Action<object> PropertiesRequested;
        public event Action<object> OpenRequested;
        public event Action<object> OpenDrawingRequested;
        public event Action<object> SelectionChanged;
        public event Action<object> NodeExpandRequested;
        public event Action RefreshRequested;

        private readonly ToolStrip _toolbar = new ToolStrip();
        private readonly ToolStripButton _btnRefresh = new ToolStripButton("Refresh");

        private readonly SplitContainer _mainSplit = new SplitContainer();
        private readonly SplitContainer _bottomSplit = new SplitContainer();

        private readonly TreeView _tree = new TreeView();
        private readonly ImageList _treeImages = new ImageList();

        private readonly ContextMenuStrip _ctx = new ContextMenuStrip();
        private readonly ToolStripMenuItem _miProps = new ToolStripMenuItem("Properties...");
        private readonly ToolStripMenuItem _miOpen = new ToolStripMenuItem("Open");
        private readonly ToolStripMenuItem _miOpenDrawing = new ToolStripMenuItem("Open Drawing");

        private readonly Panel _previewPanel = new Panel();
        private readonly PictureBox _previewBox = new PictureBox();

        private readonly ListView _infoList = new ListView();

        public InfitexTaskPaneControl()
        {
            Dock = DockStyle.Fill;

            _toolbar.GripStyle = ToolStripGripStyle.Hidden;
            _toolbar.Dock = DockStyle.Top;
            _btnRefresh.Click += (s, e) => RefreshRequested?.Invoke();
            _toolbar.Items.Add(_btnRefresh);

            _treeImages.ImageSize = new Size(16, 16);
            _treeImages.ColorDepth = ColorDepth.Depth32Bit;
            _treeImages.Images.Add("part", LoadIcon("PART.png"));
            _treeImages.Images.Add("asm", LoadIcon("ASSEMBLY.png"));
            _treeImages.Images.Add("drw", LoadIcon("DRAWING.png"));

            _mainSplit.Dock = DockStyle.Fill;
            _mainSplit.Orientation = Orientation.Horizontal;
            _mainSplit.SplitterWidth = 6;

            _bottomSplit.Dock = DockStyle.Fill;
            _bottomSplit.Orientation = Orientation.Horizontal;
            _bottomSplit.SplitterWidth = 6;

            // TREE
            _tree.Dock = DockStyle.Fill;
            _tree.HideSelection = false;
            _tree.FullRowSelect = true;
            _tree.ImageList = _treeImages;
            _tree.ContextMenuStrip = _ctx;

            _tree.AfterSelect += (s, e) =>
            {
                if (e.Node != null && e.Node.Tag != null)
                    SelectionChanged?.Invoke(e.Node.Tag);
            };

            _tree.BeforeExpand += (s, e) =>
            {
                if (e.Node != null && e.Node.Tag != null)
                    NodeExpandRequested?.Invoke(e.Node);
            };

            _tree.NodeMouseDoubleClick += (s, e) =>
            {
                if (e.Node != null && e.Node.Tag != null)
                    PropertiesRequested?.Invoke(e.Node.Tag);
            };

            // MENU
            _ctx.Items.Add(_miProps);
            _ctx.Items.Add(new ToolStripSeparator());
            _ctx.Items.Add(_miOpen);
            _ctx.Items.Add(_miOpenDrawing);

            _ctx.Opening += (s, e) =>
            {
                Point p = _tree.PointToClient(Cursor.Position);
                TreeViewHitTestInfo hit = _tree.HitTest(p);

                if (hit == null || hit.Node == null)
                {
                    e.Cancel = true;
                    return;
                }

                _tree.SelectedNode = hit.Node;

                bool hasTag = hit.Node.Tag != null;
                _miProps.Enabled = hasTag;
                _miOpen.Enabled = hasTag;
                _miOpenDrawing.Enabled = hasTag;
            };

            _miProps.Click += (s, e) =>
            {
                var tag = GetCurrentTag();
                if (tag != null) PropertiesRequested?.Invoke(tag);
            };

            _miOpen.Click += (s, e) =>
            {
                var tag = GetCurrentTag();
                if (tag != null) OpenRequested?.Invoke(tag);
            };

            _miOpenDrawing.Click += (s, e) =>
            {
                var tag = GetCurrentTag();
                if (tag != null) OpenDrawingRequested?.Invoke(tag);
            };

            // PREVIEW
            _previewPanel.Dock = DockStyle.Fill;
            _previewPanel.BackColor = Color.WhiteSmoke;

            _previewBox.Size = new Size(64, 64);
            _previewBox.SizeMode = PictureBoxSizeMode.Zoom;
            _previewBox.BackColor = Color.Transparent;
            _previewBox.Anchor = AnchorStyles.None;

            _previewPanel.Controls.Add(_previewBox);
            _previewPanel.Resize += (s, e) =>
            {
                _previewBox.Left = (_previewPanel.Width - _previewBox.Width) / 2;
                _previewBox.Top = (_previewPanel.Height - _previewBox.Height) / 2;
            };

            // INFO
            _infoList.Dock = DockStyle.Fill;
            _infoList.View = View.Details;
            _infoList.GridLines = true;
            _infoList.FullRowSelect = true;
            _infoList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _infoList.Columns.Add("Property", 120);
            _infoList.Columns.Add("Value", 420);

            _mainSplit.Panel1.Controls.Add(_tree);
            _bottomSplit.Panel1.Controls.Add(_previewPanel);
            _bottomSplit.Panel2.Controls.Add(_infoList);
            _mainSplit.Panel2.Controls.Add(_bottomSplit);

            Controls.Add(_mainSplit);
            Controls.Add(_toolbar);

            ClearPreview();
            ClearDetails();

            // Najważniejszy fix: ustaw proporcje po załadowaniu kontrolki
            this.Load += (s, e) =>
            {
                ApplyInitialLayout();
            };
        }

        private void ApplyInitialLayout()
        {
            try
            {
                if (_mainSplit.Height > 150)
                    _mainSplit.SplitterDistance = 210;

                if (_bottomSplit.Height > 120)
                    _bottomSplit.SplitterDistance = 100;
            }
            catch
            {
                // nic
            }
        }

        private Image LoadIcon(string name)
        {
            var asm = Assembly.GetExecutingAssembly();

            foreach (string res in asm.GetManifestResourceNames())
            {
                if (res.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = asm.GetManifestResourceStream(res))
                    {
                        return Image.FromStream(stream);
                    }
                }
            }

            return new Bitmap(64, 64);
        }

        public Image GetLargePreviewIcon(string key)
        {
            string file = "PART_PREVIEW.png";
            if (string.Equals(key, "asm", StringComparison.OrdinalIgnoreCase)) file = "ASSEMBLY_PREVIEW.png";
            else if (string.Equals(key, "drw", StringComparison.OrdinalIgnoreCase)) file = "DRAWING_PREVIEW.png";

            Image img = LoadIcon(file);
            return new Bitmap(img, new Size(64, 64));
        }

        public void SetPreviewImage(Image image)
        {
            if (_previewBox.Image != null)
            {
                var old = _previewBox.Image;
                _previewBox.Image = null;
                old.Dispose();
            }

            _previewBox.Image = image;
        }

        public void ClearPreview()
        {
            if (_previewBox.Image != null)
            {
                var old = _previewBox.Image;
                _previewBox.Image = null;
                old.Dispose();
            }
        }

        public void SetDetails(string part, string rev, string descEn, string descPl, string status, string path)
        {
            _infoList.Items.Clear();
            AddDetail("Part", part);
            AddDetail("Rev", rev);
            AddDetail("Description_EN", descEn);
            AddDetail("Description_PL", descPl);
            AddDetail("Status", status);
            AddDetail("Path", path);
        }

        public void ClearDetails()
        {
            _infoList.Items.Clear();
        }

        private void AddDetail(string name, string value)
        {
            var item = new ListViewItem(name ?? "");
            item.SubItems.Add(value ?? "");
            _infoList.Items.Add(item);
        }

        private object GetCurrentTag()
        {
            if (_tree.SelectedNode != null)
                return _tree.SelectedNode.Tag;

            return null;
        }

        public void SetRootNode(TreeNode root)
        {
            _tree.BeginUpdate();
            _tree.Nodes.Clear();
            _tree.Nodes.Add(root);
            root.Expand();
            _tree.EndUpdate();

            _tree.SelectedNode = root;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // InfitexTaskPaneControl
            // 
            this.Name = "InfitexTaskPaneControl";
            this.Load += new System.EventHandler(this.InfitexTaskPaneControl_Load);
            this.ResumeLayout(false);

        }

        private void InfitexTaskPaneControl_Load(object sender, EventArgs e)
        {

        }
    }
}