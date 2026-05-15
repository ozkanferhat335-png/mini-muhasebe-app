using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Mali dönem yönetimi formu
    /// </summary>
    public class FiscalPeriodForm : Form
    {
        private DataGridView dgv;
        private Button btnNew, btnClose, btnDelete;
        private FiscalPeriodService _service;
        private List<FiscalPeriod> _periods;

        public FiscalPeriodForm()
        {
            _service = new FiscalPeriodService(Program.ConnectionString);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Mali Dönem Yönetimi";
            this.Size = new Size(800, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(52, 73, 94), Padding = new Padding(10, 7, 10, 7) };
            btnNew = CreateBtn("➕ Yeni Dönem", Color.FromArgb(39, 174, 96)); btnNew.Click += BtnNew_Click;
            btnClose = CreateBtn("🔒 Dönemi Kapat", Color.FromArgb(230, 126, 34)); btnClose.Click += BtnClose_Click;
            btnDelete = CreateBtn("🗑️ Sil", Color.FromArgb(231, 76, 60)); btnDelete.Click += BtnDelete_Click;

            int x = 10;
            foreach (var btn in new[] { btnNew, btnClose, btnDelete })
            { btn.Location = new Point(x, 7); pnlToolbar.Controls.Add(btn); x += btn.Width + 5; }

            dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, Font = new Font("Segoe UI", 9), AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 249, 250) } };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;

            this.Controls.Add(dgv);
            this.Controls.Add(pnlToolbar);
        }

        private Button CreateBtn(string text, Color color)
        {
            var btn = new Button { Text = text, Size = new Size(140, 30), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadData()
        {
            if (!Program.ActiveCompanyId.HasValue) return;
            _periods = _service.GetPeriodsByCompany(Program.ActiveCompanyId.Value);
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PeriodId", HeaderText = "ID", DataPropertyName = "PeriodId", Width = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PeriodName", HeaderText = "Dönem Adı", DataPropertyName = "PeriodName" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StartDate", HeaderText = "Başlangıç", DataPropertyName = "StartDate", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "EndDate", HeaderText = "Bitiş", DataPropertyName = "EndDate", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsClosed", HeaderText = "Kapalı", DataPropertyName = "IsClosed", Width = 70 });
            dgv.DataSource = _periods;

            // Kapalı dönemleri gri göster
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.DataBoundItem is FiscalPeriod p && p.IsClosed)
                    row.DefaultCellStyle.ForeColor = Color.Gray;
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            var form = new FiscalPeriodEditForm();
            if (form.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Lütfen bir dönem seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["PeriodId"].Value);
            if (MessageBox.Show("Seçili dönemi kapatmak istediğinizden emin misiniz?\nKapalı dönemde değişiklik yapılamaz.", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (_service.ClosePeriod(id)) LoadData();
                else MessageBox.Show("Dönem kapatılamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Lütfen bir dönem seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("Dönemi silmek istediğinizden emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["PeriodId"].Value);
                if (_service.DeletePeriod(id)) LoadData();
                else MessageBox.Show("Silme başarısız. Kapalı dönem veya kayıtlı işlem var.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class FiscalPeriodEditForm : Form
    {
        private TextBox txtName;
        private DateTimePicker dtpStart, dtpEnd;
        private Button btnSave, btnCancel;
        private FiscalPeriodService _service;

        public FiscalPeriodEditForm()
        {
            _service = new FiscalPeriodService(Program.ConnectionString);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Yeni Mali Dönem";
            this.Size = new Size(420, 260);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            int y = 20, lx = 20, cx = 150, cw = 230;
            AddLbl("Dönem Adı:", lx, y); txtName = new TextBox { Location = new Point(cx, y), Size = new Size(cw, 25), Font = new Font("Segoe UI", 9) }; this.Controls.Add(txtName); y += 35;
            AddLbl("Başlangıç Tarihi: *", lx, y); dtpStart = new DateTimePicker { Location = new Point(cx, y), Size = new Size(cw, 25), Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, 1, 1) }; dtpStart.ValueChanged += AutoFillName; this.Controls.Add(dtpStart); y += 35;
            AddLbl("Bitiş Tarihi: *", lx, y); dtpEnd = new DateTimePicker { Location = new Point(cx, y), Size = new Size(cw, 25), Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, 12, 31) }; this.Controls.Add(dtpEnd); y += 45;

            btnSave = new Button { Text = "💾 Kaydet", Location = new Point(cx, y), Size = new Size(110, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            btnCancel = new Button { Text = "İptal", Location = new Point(cx + 120, y), Size = new Size(80, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            AutoFillName(null, null);
        }

        private void AddLbl(string t, int x, int y) { var l = new Label { Text = t, Location = new Point(x, y + 3), Size = new Size(125, 20), Font = new Font("Segoe UI", 9) }; this.Controls.Add(l); }

        private void AutoFillName(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtName.Text))
                txtName.Text = $"{dtpStart.Value.Year} - {dtpStart.Value:MMMM}";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!Program.ActiveCompanyId.HasValue) return;
            if (dtpStart.Value >= dtpEnd.Value) { MessageBox.Show("Başlangıç tarihi bitiş tarihinden önce olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var result = _service.CreatePeriod(Program.ActiveCompanyId.Value, txtName.Text.Trim(), dtpStart.Value, dtpEnd.Value);
            if (result != null) { this.DialogResult = DialogResult.OK; this.Close(); }
            else MessageBox.Show("Dönem oluşturulamadı. Aynı tarih aralığında dönem zaten var olabilir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
