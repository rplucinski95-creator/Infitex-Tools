using System;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace InfitexTools
{
    public class PropertiesForm : Form
    {
        private readonly ModelDoc2 _model;
        private readonly CustomPropertyManager _cust;

        private readonly TextBox txtIndex = new TextBox();
        private readonly TextBox txtRev = new TextBox();
        private readonly TextBox txtPartNo = new TextBox();
        private readonly TextBox txtDescPL = new TextBox();
        private readonly TextBox txtDescEN = new TextBox();
        private readonly TextBox txtStatus = new TextBox();

        private readonly Button btnSave = new Button();
        private readonly Button btnClose = new Button();

        public PropertiesForm(ModelDoc2 model)
        {
            if (model == null) throw new ArgumentNullException("model");

            _model = model;
            _cust = _model.Extension.CustomPropertyManager[""];

            Text = "Infitex — Properties";
            Width = 520;
            Height = 360;
            StartPosition = FormStartPosition.CenterScreen;

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(10),
                AutoSize = true
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(grid, 0, "Index", txtIndex);
            AddRow(grid, 1, "ActualRevision", txtRev);
            AddRow(grid, 2, "PartNo", txtPartNo);
            AddRow(grid, 3, "Description_PL", txtDescPL);
            AddRow(grid, 4, "Description_EN", txtDescEN);
            AddRow(grid, 5, "Status", txtStatus);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true
            };

            btnSave.Text = "Save";
            btnSave.Width = 100;
            btnSave.Click += (s, e) => SaveProps();

            btnClose.Text = "Close";
            btnClose.Width = 100;
            btnClose.Click += (s, e) => Close();

            buttons.Controls.Add(btnClose);
            buttons.Controls.Add(btnSave);

            grid.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 6);
            grid.Controls.Add(buttons, 1, 6);

            Controls.Add(grid);

            LoadProps();
        }

        private void AddRow(TableLayoutPanel grid, int row, string label, Control editor)
        {
            var lbl = new Label { Text = label, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
            editor.Dock = DockStyle.Fill;

            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            grid.Controls.Add(lbl, 0, row);
            grid.Controls.Add(editor, 1, row);
        }

        private void LoadProps()
        {
            txtIndex.Text = GetProp("Index");
            txtRev.Text = GetProp("ActualRevision");
            txtPartNo.Text = GetProp("PartNo");
            txtDescPL.Text = GetProp("Description_PL");
            txtDescEN.Text = GetProp("Description_EN");
            txtStatus.Text = GetProp("Status");
        }

        private string GetProp(string name)
        {
            string valOut = "";
            string resolvedOut = "";

            // Get4: (name, useCached, out val, out resolved)
            _cust.Get4(name, false, out valOut, out resolvedOut);

            if (!string.IsNullOrWhiteSpace(resolvedOut)) return resolvedOut;
            return valOut ?? "";
        }

        private void SetProp(string name, string value)
        {
            _cust.Add3(
                name,
                (int)swCustomInfoType_e.swCustomInfoText,
                value ?? "",
                (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue
            );
        }

        private void SaveProps()
        {
            try
            {
                SetProp("Index", (txtIndex.Text ?? "").Trim());
                SetProp("ActualRevision", (txtRev.Text ?? "").Trim());
                SetProp("PartNo", (txtPartNo.Text ?? "").Trim());
                SetProp("Description_PL", (txtDescPL.Text ?? "").Trim());
                SetProp("Description_EN", (txtDescEN.Text ?? "").Trim());
                SetProp("Status", (txtStatus.Text ?? "").Trim());

                int errs = 0, warns = 0;
                _model.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref errs, ref warns);

                MessageBox.Show("Saved.", "Infitex");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Infitex — Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // PropertiesForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "PropertiesForm";
            this.Load += new System.EventHandler(this.PropertiesForm_Load);
            this.ResumeLayout(false);

        }

        private void PropertiesForm_Load(object sender, EventArgs e)
        {

        }
    }
}