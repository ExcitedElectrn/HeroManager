namespace HeroManager;

internal sealed class MainForm : Form
{
    private readonly ProcessMonitor _processMonitor = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 1000 };
    private readonly DataGridView _processGrid = new();
    private readonly MetricGraphControl _graph = new();
    private readonly Label _memorySummary = new();
    private readonly Label _selectedProcessSummary = new();
    private int? _selectedProcessId;

    public MainForm()
    {
        Text = "HeroManager - Task Manager";
        MinimumSize = new Size(1050, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(28, 32, 40);
        ForeColor = Color.WhiteSmoke;

        Controls.Add(BuildLayout());
        ConfigureProcessGrid();

        _refreshTimer.Tick += (_, _) => RefreshMetrics();
        Shown += (_, _) =>
        {
            RefreshMetrics();
            _refreshTimer.Start();
        };
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 38));

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Text = "Processes",
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            ForeColor = Color.White
        };

        _memorySummary.Dock = DockStyle.Fill;
        _memorySummary.Font = new Font(Font.FontFamily, 11, FontStyle.Regular);
        _memorySummary.ForeColor = Color.Gainsboro;

        var header = new Panel { Dock = DockStyle.Fill, BackColor = BackColor };
        header.Controls.Add(_memorySummary);
        header.Controls.Add(title);

        _selectedProcessSummary.Dock = DockStyle.Top;
        _selectedProcessSummary.Height = 34;
        _selectedProcessSummary.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
        _selectedProcessSummary.ForeColor = Color.Gainsboro;
        _graph.Dock = DockStyle.Fill;

        var graphPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0), BackColor = BackColor };
        graphPanel.Controls.Add(_graph);
        graphPanel.Controls.Add(_selectedProcessSummary);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_processGrid, 0, 1);
        root.Controls.Add(graphPanel, 0, 2);
        return root;
    }

    private void ConfigureProcessGrid()
    {
        _processGrid.Dock = DockStyle.Fill;
        _processGrid.AllowUserToAddRows = false;
        _processGrid.AllowUserToDeleteRows = false;
        _processGrid.AllowUserToResizeRows = false;
        _processGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _processGrid.BackgroundColor = Color.FromArgb(20, 24, 31);
        _processGrid.BorderStyle = BorderStyle.None;
        _processGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _processGrid.MultiSelect = false;
        _processGrid.ReadOnly = true;
        _processGrid.RowHeadersVisible = false;
        _processGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _processGrid.CellClick += (_, _) => CaptureSelectedProcess();
        _processGrid.SelectionChanged += (_, _) => CaptureSelectedProcess();

        _processGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "pid", HeaderText = "PID", FillWeight = 12 });
        _processGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "Name", FillWeight = 45 });
        _processGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "cpu", HeaderText = "CPU", FillWeight = 18 });
        _processGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "memory", HeaderText = "Memory", FillWeight = 25 });
    }

    private void RefreshMetrics()
    {
        UpdateMemorySummary();
        var selectedProcessIdBeforeRefresh = _selectedProcessId;
        var processes = _processMonitor.GetProcesses();
        _processGrid.Rows.Clear();

        foreach (var process in processes)
        {
            var rowIndex = _processGrid.Rows.Add(process.Id, process.Name, $"{process.CpuPercent:N1}%", FormatBytes(process.WorkingSetBytes));
            _processGrid.Rows[rowIndex].Tag = process;
            if (selectedProcessIdBeforeRefresh == process.Id)
            {
                _processGrid.Rows[rowIndex].Selected = true;
                UpdateSelectedProcess(process);
            }
        }

        if (selectedProcessIdBeforeRefresh is not null && processes.All(process => process.Id != selectedProcessIdBeforeRefresh.Value))
        {
            _selectedProcessId = null;
            _graph.ClearSamples();
            _selectedProcessSummary.Text = "Selected process exited.";
        }
    }

    private void UpdateMemorySummary()
    {
        try
        {
            var memory = MemoryStatus.GetSnapshot();
            _memorySummary.Text = $"System RAM: {FormatBytes((long)memory.UsedBytes)} used / {FormatBytes((long)memory.TotalBytes)} total ({memory.UsedPercent:N1}%)  •  {FormatBytes((long)memory.AvailableBytes)} free";
        }
        catch (Exception ex)
        {
            _memorySummary.Text = $"System RAM: unavailable ({ex.Message})";
        }
    }

    private void CaptureSelectedProcess()
    {
        if (_processGrid.SelectedRows.Count == 0 || _processGrid.SelectedRows[0].Tag is not ProcessInfo process)
        {
            return;
        }

        if (_selectedProcessId != process.Id)
        {
            _selectedProcessId = process.Id;
            _graph.ClearSamples();
        }

        UpdateSelectedProcess(process);
    }

    private void UpdateSelectedProcess(ProcessInfo process)
    {
        _selectedProcessId = process.Id;
        var memoryMegabytes = process.WorkingSetBytes / 1024d / 1024d;
        _selectedProcessSummary.Text = $"Selected: {process.Name} (PID {process.Id})  •  CPU {process.CpuPercent:N1}%  •  RAM {FormatBytes(process.WorkingSetBytes)}";
        _graph.AddSample(process.CpuPercent, memoryMegabytes);
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:N1} {units[unit]}";
    }
}
