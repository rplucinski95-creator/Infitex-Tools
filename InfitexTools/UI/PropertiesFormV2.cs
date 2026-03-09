using System;
using System.Drawing;
using System.Windows.Forms;
using InfitexTools.Models;
using InfitexTools.Services;
using SolidWorks.Interop.sldworks;

namespace InfitexTools.UI
{
    public partial class PropertiesFormV2 : Form
    {
        private ModelDoc2 _model;
        private readonly PropertiesService _propertiesService;

        private TabControl tabControlMain;
        private TabPage tabMain;
        private TabPage tabComments;

        private TextBox txtIndex;
        private TextBox txtActualRevision;
        private TextBox txtFileType;

        private ComboBox cmbCreatedBy;

        private TextBox txtDescriptionEN;
        private TextBox txtDescriptionPL;

        private ComboBox cmbPartType;
        private ComboBox cmbProjectNumber;

        private TextBox txtSupplier;
        private TextBox txtProjectName;
        private TextBox txtRemark;
        private TextBox txtDownloadLink;

        private TextBox txtComments;

        private Button btnSave;
        private Button btnCancel;

        public PropertiesFormV2()
        {
            _propertiesService = new PropertiesService();
            BuildUi();
            InitializeUiDefaults();
        }

        public PropertiesFormV2(ModelDoc2 model) : this()
        {
            _model = model;
            LoadData();
        }

        private void BuildUi()
        {
            Text = "Properties";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(760, 560);
            MinimumSize = new Size(760, 560);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            Controls.Add(root);

            tabControlMain = new TabControl();
            tabControlMain.Dock = DockStyle.Fill;

            tabMain = new TabPage("Main");
            tabComments = new TabPage("Comments");

            tabControlMain.TabPages.Add(tabMain);
            tabControlMain.TabPages.Add(tabComments);

            root.Controls.Add(tabControlMain, 0, 0);

            BuildMainTab();
            BuildCommentsTab();
            BuildButtons(root);
        }

        private void BuildMainTab()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            tabMain.Controls.Add(panel);

            var tbl = new TableLayoutPanel();
            tbl.Dock = DockStyle.Top;
            tbl.AutoSize = true;
            tbl.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tbl.Padding = new Padding(12);
            tbl.ColumnCount = 2;
            tbl.RowCount = 12;
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.Controls.Add(tbl);

            txtIndex = CreateReadOnlyTextBox();
            txtActualRevision = CreateReadOnlyTextBox();
            txtFileType = CreateReadOnlyTextBox();

            cmbCreatedBy = CreateComboBox();
            txtDescriptionEN = CreateTextBox();
            txtDescriptionPL = CreateTextBox();

            cmbPartType = CreateComboBox();
            txtSupplier = CreateTextBox();
            cmbProjectNumber = CreateComboBox();
            cmbProjectNumber.DropDownStyle = ComboBoxStyle.DropDown;

            txtProjectName = CreateReadOnlyTextBox();
            txtRemark = CreateTextBox();
            txtDownloadLink = CreateTextBox();

            AddRow(tbl, "Index", txtIndex);
            AddRow(tbl, "Actual Revision", txtActualRevision);
            AddRow(tbl, "File Type", txtFileType);
            AddRow(tbl, "Created By", cmbCreatedBy);
            AddRow(tbl, "Description_EN", txtDescriptionEN);
            AddRow(tbl, "Description_PL", txtDescriptionPL);
            AddRow(tbl, "Part Type", cmbPartType);
            AddRow(tbl, "Supplier", txtSupplier);
            AddRow(tbl, "Project Number", cmbProjectNumber);
            AddRow(tbl, "Project Name", txtProjectName);
            AddRow(tbl, "Remark", txtRemark);
            AddRow(tbl, "Download Link", txtDownloadLink);
        }

        private void BuildCommentsTab()
        {
            txtComments = new TextBox();
            txtComments.Dock = DockStyle.Fill;
            txtComments.Multiline = true;
            txtComments.ScrollBars = ScrollBars.Vertical;
            txtComments.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            tabComments.Controls.Add(txtComments);
        }

        private void BuildButtons(TableLayoutPanel root)
        {
            var buttonPanel = new Panel();
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.Padding = new Padding(12, 8, 12, 8);
            root.Controls.Add(buttonPanel, 0, 1);

            btnSave = new Button();
            btnSave.Text = "Save to Model";
            btnSave.Size = new Size(130, 30);
            btnSave.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnSave.Location = new Point(470, 10);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(100, 30);
            btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnCancel.Location = new Point(610, 10);

            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);
        }

        private void InitializeUiDefaults()
        {
            if (cmbCreatedBy.Items.Count == 0)
                cmbCreatedBy.Items.Add("RPl");

            if (cmbPartType.Items.Count == 0)
            {
                cmbPartType.Items.Add("MAKE");
                cmbPartType.Items.Add("BUY");
            }

            cmbCreatedBy.Text = "RPl";
            cmbPartType.Text = "MAKE";

            cmbPartType.SelectedIndexChanged -= cmbPartType_SelectedIndexChanged;
            cmbPartType.SelectedIndexChanged += cmbPartType_SelectedIndexChanged;

            btnSave.Click -= btnSave_Click;
            btnSave.Click += btnSave_Click;

            btnCancel.Click -= btnCancel_Click;
            btnCancel.Click += btnCancel_Click;

            UpdateSupplierState();
        }

        private void LoadData()
        {
            if (_model == null) return;

            var props = _propertiesService.LoadModelProperties(_model);
            if (props == null) return;

            txtIndex.Text = props.Index ?? "";
            txtActualRevision.Text = props.ActualRevision ?? "";
            txtFileType.Text = props.FileType ?? "";

            cmbCreatedBy.Text = string.IsNullOrWhiteSpace(props.CreatedBy) ? "RPl" : props.CreatedBy;

            txtDescriptionEN.Text = props.Description_EN ?? "";
            txtDescriptionPL.Text = props.Description_PL ?? "";

            cmbPartType.Text = string.IsNullOrWhiteSpace(props.PartType) ? "MAKE" : props.PartType;
            txtSupplier.Text = props.Supplier ?? "";

            cmbProjectNumber.Text = props.ProjectNumber ?? "";
            txtProjectName.Text = props.ProjectName ?? "";

            txtRemark.Text = props.Remark ?? "";
            txtDownloadLink.Text = props.DownloadLink ?? "";

            txtComments.Text = props.Comments ?? "";

            UpdateSupplierState();
        }

        private void UpdateSupplierState()
        {
            bool isBuy = string.Equals(cmbPartType.Text, "BUY", StringComparison.OrdinalIgnoreCase);
            txtSupplier.Enabled = isBuy;

            if (!isBuy)
                txtSupplier.Text = "";
        }

        private void cmbPartType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSupplierState();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_model == null)
            {
                MessageBox.Show("No model loaded.", "Infitex Tools");
                return;
            }

            var props = new ModelProperties
            {
                CreatedBy = cmbCreatedBy.Text,

                Description_EN = txtDescriptionEN.Text,
                Description_PL = txtDescriptionPL.Text,

                PartType = cmbPartType.Text,
                Supplier = txtSupplier.Text,

                ProjectNumber = cmbProjectNumber.Text,
                ProjectName = txtProjectName.Text,

                Remark = txtRemark.Text,
                DownloadLink = txtDownloadLink.Text,

                Comments = txtComments.Text
            };

            _propertiesService.SaveModelProperties(_model, props);

            MessageBox.Show("Properties saved to model.", "Infitex Tools");
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private static TextBox CreateTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Top,
                Margin = new Padding(3, 3, 3, 8),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
        }

        private static TextBox CreateReadOnlyTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Top,
                Margin = new Padding(3, 3, 3, 8),
                ReadOnly = true,
                BackColor = SystemColors.ControlLight,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
        }

        private static ComboBox CreateComboBox()
        {
            return new ComboBox
            {
                Dock = DockStyle.Top,
                Margin = new Padding(3, 3, 3, 8),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
        }

        private static void AddRow(TableLayoutPanel tbl, string labelText, Control control)
        {
            int row = tbl.RowCount;
            tbl.RowCount++;
            tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lbl = new Label
            {
                Text = labelText,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 6, 3, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };

            tbl.Controls.Add(lbl, 0, row);
            tbl.Controls.Add(control, 1, row);
        }
    }
}