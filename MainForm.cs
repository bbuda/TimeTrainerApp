using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.IO;
using System.Text.Json;

namespace TimeTrainer;

public partial class MainForm : Form
{
    private DateTime _targetTime;
    private DateTime _currentTime = new DateTime(2000, 1, 1, 12, 0, 0);

    private DateTime _startTime2;
    private DateTime _endTime2;
    private Label? targetTimeDisplay2;
    private TextBox? answerTextBox2;
    private Button? checkButton2;
    private Panel? clockPanelStart;
    private Panel? clockPanelEnd;
    private Panel? task2Container;

    private Label? targetTimeDisplay1;
    private Panel? clockPanel1;
    private Button? checkButton1;
    private Panel? task1Container;

    private RadioButton? task1Radio;
    private RadioButton? task2Radio;
    private RadioButton? task3Radio;

    // Игровой режим
    private Panel? task3Container;
    private System.Windows.Forms.Timer? gameTimer;
    private int gameTimeSeconds = 60;
    private int correctAnswers = 0;
    private int incorrectAnswers = 0;
    private Label? timerLabel;
    private Label? statsLabel;
    private Label? bestScoreLabel;
    private Button? startGameButton;
    private Button? checkButton3;
    private Panel? gameClockPanel1;
    private Panel? gameClockPanel2;
    private Panel? gameClockPanel3;
    private TextBox? gameAnswerTextBox;
    private bool gameModeActive = false;
    private int currentGameTask = 1; // 1 - показать время, 2 - сколько прошло

    private GameRecord? bestRecord;

    private const int ClockSize = 320;
    private const int Radius = ClockSize / 2;
    private const int ClockCenterX = ClockSize / 2;
    private const int ClockCenterY = ClockSize / 2;

    private bool isDragging = false;
    private Point _dragCenter = new Point(ClockCenterX, ClockCenterY);
    private HandType _draggingHand = HandType.None;

    private enum HandType { None, Minute, Hour }

    private class GameRecord
    {
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int GameTimeSeconds { get; set; }
        public DateTime Date { get; set; }
    }

    public MainForm()
    {
        InitializeCustomComponents();
        Text = "Тренажер времени (WinForms)";
        
        LoadBestRecord();

        if (clockPanel1 != null)
            SetDoubleBuffered(clockPanel1);
        if (clockPanelStart != null)
            SetDoubleBuffered(clockPanelStart);
        if (clockPanelEnd != null)
            SetDoubleBuffered(clockPanelEnd);
        if (gameClockPanel1 != null)
            SetDoubleBuffered(gameClockPanel1);
        if (gameClockPanel2 != null)
            SetDoubleBuffered(gameClockPanel2);

        SetTaskVisibility(1);
    }

    private void SetDoubleBuffered(Panel panel)
    {
        if (panel == null)
            return;

        PropertyInfo? pi = typeof(Panel).GetProperty(
            "DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);

        if (pi != null)
        {
            pi.SetValue(panel, true, null);
        }
    }

    private void InitializeCustomComponents()

    {

        this.SuspendLayout();

        this.Controls.Clear();

        this.ClientSize = new Size(900, 750);

        this.BackColor = ColorTranslator.FromHtml("#F5F5F5");

        this.FormBorderStyle = FormBorderStyle.Sizable;

        this.MinimumSize = new Size(900, 750);

        this.StartPosition = FormStartPosition.CenterScreen;



        TableLayoutPanel rootLayout = new TableLayoutPanel

        {

            Dock = DockStyle.Fill,

            BackColor = ColorTranslator.FromHtml("#F5F5F5"),

            RowCount = 3,

            ColumnCount = 1,

            Margin = new Padding(0),

            Padding = new Padding(0)

        };

        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));

        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));

        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        this.Controls.Add(rootLayout);



        Panel headerPanel = new Panel

        {

            Dock = DockStyle.Fill,

            BackColor = ColorTranslator.FromHtml("#43A047"),

            Padding = new Padding(30, 10, 30, 10)

        };

        rootLayout.Controls.Add(headerPanel, 0, 0);



        Label titleLabel = new Label

        {

            Text = "ТРЕНАЖЕР ВРЕМЕНИ",

            Dock = DockStyle.Fill,

            Font = new Font("Arial", 22, FontStyle.Bold),

            ForeColor = Color.White,

            TextAlign = ContentAlignment.MiddleCenter

        };

        headerPanel.Controls.Add(titleLabel);



        Panel navWrapper = new Panel

        {

            Dock = DockStyle.Fill,

            BackColor = ColorTranslator.FromHtml("#E8F5E9"),

            Padding = new Padding(0),

            Margin = new Padding(0)

        };

        rootLayout.Controls.Add(navWrapper, 0, 1);



        FlowLayoutPanel navPanel = new FlowLayoutPanel

        {

            AutoSize = true,

            FlowDirection = FlowDirection.LeftToRight,

            WrapContents = false,

            Margin = new Padding(0),

            Padding = new Padding(0)

        };

        navWrapper.Controls.Add(navPanel);

        EventHandler centerNavHandler = (s, e) =>

        {

            navPanel.Location = new Point(

                Math.Max(0, (navWrapper.Width - navPanel.Width) / 2),

                Math.Max(0, (navWrapper.Height - navPanel.Height) / 2));

        };

        navWrapper.Resize += centerNavHandler;



        task1Radio = CreateNavigationRadio("1. Укажи время");

        if (task1Radio != null)

            task1Radio.Checked = true;

        task2Radio = CreateNavigationRadio("2. Сколько прошло?");

        task3Radio = CreateNavigationRadio("3. Игровой режим");



        if (task3Radio != null)

            task3Radio.ForeColor = ColorTranslator.FromHtml("#D84315");



        if (task1Radio != null)

            navPanel.Controls.Add(task1Radio);

        if (task2Radio != null)

            navPanel.Controls.Add(task2Radio);

        if (task3Radio != null)

            navPanel.Controls.Add(task3Radio);

        centerNavHandler.Invoke(this, EventArgs.Empty);



        if (task1Radio != null)

            task1Radio.CheckedChanged += (s, e) =>

            {

                if (task1Radio.Checked)

                    SetTaskVisibility(1);

                UpdateTaskRadioStyles();

            };

        if (task2Radio != null)

            task2Radio.CheckedChanged += (s, e) =>

            {

                if (task2Radio.Checked)

                    SetTaskVisibility(2);

                UpdateTaskRadioStyles();

            };

        if (task3Radio != null)

            task3Radio.CheckedChanged += (s, e) =>

            {

                if (task3Radio.Checked)

                    SetTaskVisibility(3);

                UpdateTaskRadioStyles();

            };



        Panel contentWrapper = new Panel

        {

            Dock = DockStyle.Fill,

            Padding = new Padding(25),

            BackColor = ColorTranslator.FromHtml("#F5F5F5")

        };

        rootLayout.Controls.Add(contentWrapper, 0, 2);



        Panel tasksPanel = new Panel

        {

            Dock = DockStyle.Fill,

            BackColor = Color.Transparent

        };

        contentWrapper.Controls.Add(tasksPanel);



        task1Container =

            new Panel 

            { 

                Dock = DockStyle.Fill,

                BackColor = Color.White,

                Padding = new Padding(30)

            };

        task1Container.Paint += (s, e) =>

        {

            ControlPaint.DrawBorder(e.Graphics, task1Container.ClientRectangle, 

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid,

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid,

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid,

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid);

        };

        tasksPanel.Controls.Add(task1Container);

        if (task1Container != null)

            InitializeTask1UI(task1Container);



        task2Container =

            new Panel 

            { 

                Dock = DockStyle.Fill,

                BackColor = Color.White,

                Padding = new Padding(30)

            };

        task2Container.Paint += (s, e) =>

        {

            ControlPaint.DrawBorder(e.Graphics, task2Container.ClientRectangle, 

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid,

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid,

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid,

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid);

        };

        tasksPanel.Controls.Add(task2Container);

        if (task2Container != null)

            InitializeTask2UI(task2Container);



        task3Container =

            new Panel 

            { 

                Dock = DockStyle.Fill,

                BackColor = Color.White,

                Padding = new Padding(30)

            };

        task3Container.Paint += (s, e) =>

        {

            ControlPaint.DrawBorder(e.Graphics, task3Container.ClientRectangle, 

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid,

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid,

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid,

                ColorTranslator.FromHtml("#E0E0E0"), 2, ButtonBorderStyle.Solid);

        };

        tasksPanel.Controls.Add(task3Container);

        if (task3Container != null)

            InitializeTask3UI(task3Container);



        this.ResumeLayout(false);

        UpdateTaskRadioStyles();

    }



    private RadioButton CreateNavigationRadio(string text)

    {

        RadioButton radio = new RadioButton

        {

            Text = text,

            Appearance = Appearance.Button,

            AutoSize = false,

            Size = new Size(250, 50),

            Font = new Font("Arial", 11, FontStyle.Bold),

            FlatStyle = FlatStyle.Flat,

            TextAlign = ContentAlignment.MiddleCenter,

            ForeColor = ColorTranslator.FromHtml("#2E7D32"),

            BackColor = Color.White,

            Margin = new Padding(8, 10, 8, 10),

            Cursor = Cursors.Hand

        };

        radio.FlatAppearance.BorderSize = 0;

        radio.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#C8E6C9");

        radio.FlatAppearance.CheckedBackColor = ColorTranslator.FromHtml("#43A047");

        return radio;

    }



    private void UpdateTaskRadioStyles()

    {

        Action<RadioButton?, bool> styleRadio = (radio, accent) =>

        {

            if (radio == null)

                return;



            if (radio.Checked)

            {

                radio.BackColor = ColorTranslator.FromHtml("#43A047");

                radio.ForeColor = Color.White;

            }

            else

            {

                radio.BackColor = Color.White;

                radio.ForeColor = accent

                    ? ColorTranslator.FromHtml("#D84315")

                    : ColorTranslator.FromHtml("#2E7D32");

            }

        };



        styleRadio(task1Radio, false);

        styleRadio(task2Radio, false);

        styleRadio(task3Radio, true);

    }

    private void InitializeTask1UI(Panel container)
    {
        container.Controls.Clear();

        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));
        container.Controls.Add(layout);

        Label title1 = new Label
        {
            Text = "Покажите время",
            Font = new Font("Arial", 22, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 10)
        };
        layout.Controls.Add(title1, 0, 0);

        targetTimeDisplay1 =
            new Label
            {
                Text = "00:00",
                Font = new Font("Arial", 64, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#4CAF50"),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 10)
            };
        layout.Controls.Add(targetTimeDisplay1, 0, 1);

        Panel clockHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 20, 0, 20),
            BackColor = Color.Transparent
        };
        layout.Controls.Add(clockHost, 0, 2);

        clockPanel1 =
            new Panel
            {
                Size = new Size(ClockSize, ClockSize),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        clockPanel1.Paint += ClockPanel_Paint;
        clockPanel1.MouseDown += ClockPanel_MouseDown;
        clockPanel1.MouseMove += ClockPanel_MouseMove;
        clockPanel1.MouseUp += ClockPanel_MouseUp;
        SetDoubleBuffered(clockPanel1);
        clockHost.Controls.Add(clockPanel1);
        EventHandler clockHostResize = (s, e) =>
        {
            clockPanel1.Location = new Point((clockHost.Width - ClockSize) / 2,
                                             (clockHost.Height - ClockSize) / 2);
        };
        clockHost.Resize += clockHostResize;
        clockHostResize(clockHost, EventArgs.Empty);

        Panel buttonHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        layout.Controls.Add(buttonHost, 0, 3);

        checkButton1 = new Button
        {
            Text = "✓ ПРОВЕРИТЬ",
            Font = new Font("Arial", 18, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#4CAF50"),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(250, 60),
            Cursor = Cursors.Hand
        };
        checkButton1.FlatAppearance.BorderSize = 0;
        checkButton1.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#45A049");
        checkButton1.Click += CheckTime1_Click;
        buttonHost.Controls.Add(checkButton1);
        EventHandler buttonHostResize = (s, e) =>
        {
            checkButton1.Location = new Point((buttonHost.Width - checkButton1.Width) / 2,
                                              (buttonHost.Height - checkButton1.Height) / 2);
        };
        buttonHost.Resize += buttonHostResize;
        buttonHostResize(buttonHost, EventArgs.Empty);
    }

    private void InitializeTask2UI(Panel container)
    {
        container.Controls.Clear();

        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));
        container.Controls.Add(layout);

        Label title2 = new Label
        {
            Text = "Сколько минут прошло?",
            Font = new Font("Arial", 22, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 10)
        };
        layout.Controls.Add(title2, 0, 0);

        TableLayoutPanel clockArea = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(40, 10, 40, 10)
        };
        clockArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        clockArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        clockArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.Controls.Add(clockArea, 0, 1);

        TableLayoutPanel startColumn = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        startColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        startColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        clockArea.Controls.Add(startColumn, 0, 0);

        Label startLabel = new Label
        {
            Text = "НАЧАЛО",
            Font = new Font("Arial", 16, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#4CAF50"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        startColumn.Controls.Add(startLabel, 0, 0);

        Panel startClockHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        startColumn.Controls.Add(startClockHost, 0, 1);

        clockPanelStart =
            new Panel
            {
                Size = new Size(ClockSize, ClockSize),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        clockPanelStart.Tag = "Start";
        clockPanelStart.Paint += ClockPanel_Paint;
        SetDoubleBuffered(clockPanelStart);
        startClockHost.Controls.Add(clockPanelStart);
        EventHandler startHostResize = (s, e) =>
        {
            clockPanelStart.Location = new Point((startClockHost.Width - ClockSize) / 2,
                                                Math.Max(0, (startClockHost.Height - ClockSize) / 2));
        };
        startClockHost.Resize += startHostResize;
        startHostResize(startClockHost, EventArgs.Empty);

        TableLayoutPanel endColumn = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        endColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        endColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        clockArea.Controls.Add(endColumn, 1, 0);

        Label endLabel = new Label
        {
            Text = "КОНЕЦ",
            Font = new Font("Arial", 16, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#FF5722"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        endColumn.Controls.Add(endLabel, 0, 0);

        Panel endClockHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        endColumn.Controls.Add(endClockHost, 0, 1);

        clockPanelEnd =
            new Panel
            {
                Size = new Size(ClockSize, ClockSize),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        clockPanelEnd.Tag = "End";
        clockPanelEnd.Paint += ClockPanel_Paint;
        SetDoubleBuffered(clockPanelEnd);
        endClockHost.Controls.Add(clockPanelEnd);
        EventHandler endHostResize = (s, e) =>
        {
            clockPanelEnd.Location = new Point((endClockHost.Width - ClockSize) / 2,
                                              Math.Max(0, (endClockHost.Height - ClockSize) / 2));
        };
        endClockHost.Resize += endHostResize;
        endHostResize(endClockHost, EventArgs.Empty);

        Panel answerHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        layout.Controls.Add(answerHost, 0, 2);

        FlowLayoutPanel answerPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        answerHost.Controls.Add(answerPanel);
        EventHandler answerHostResize = (s, e) =>
        {
            answerPanel.Location = new Point((answerHost.Width - answerPanel.Width) / 2,
                                             (answerHost.Height - answerPanel.Height) / 2);
        };
        answerHost.Resize += answerHostResize;
        answerHostResize(answerHost, EventArgs.Empty);

        targetTimeDisplay2 =
            new Label
            {
                Text = "Прошло:",
                Font = new Font("Arial", 20, FontStyle.Bold),
                AutoSize = true,
                ForeColor = ColorTranslator.FromHtml("#333333"),
                Margin = new Padding(0, 8, 15, 0)
            };
        answerPanel.Controls.Add(targetTimeDisplay2);

        answerTextBox2 =
            new TextBox
            {
                Font = new Font("Arial", 20),
                Size = new Size(240, 44),
                PlaceholderText = "Минут или часов",
                BorderStyle = BorderStyle.FixedSingle
            };
        answerPanel.Controls.Add(answerTextBox2);

        Panel buttonHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        layout.Controls.Add(buttonHost, 0, 3);

        checkButton2 = new Button
        {
            Text = "✓ ПРОВЕРИТЬ",
            Font = new Font("Arial", 18, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#4CAF50"),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(250, 60),
            Cursor = Cursors.Hand
        };
        checkButton2.FlatAppearance.BorderSize = 0;
        checkButton2.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#45A049");
        checkButton2.Click += CheckTime2_Click;
        buttonHost.Controls.Add(checkButton2);
        EventHandler buttonHostResize = (s, e) =>
        {
            checkButton2.Location = new Point((buttonHost.Width - checkButton2.Width) / 2,
                                              (buttonHost.Height - checkButton2.Height) / 2);
        };
        buttonHost.Resize += buttonHostResize;
        buttonHostResize(buttonHost, EventArgs.Empty);
    }

    private void SetTaskVisibility(int taskNumber)
    {
        if (task1Container != null)
            task1Container.Visible = (taskNumber == 1);
        if (task2Container != null)
            task2Container.Visible = (taskNumber == 2);
        if (task3Container != null)
            task3Container.Visible = (taskNumber == 3);

        if (taskNumber == 1)
        {
            GenerateTargetTime1();
        }
        else if (taskNumber == 2)
        {
            GenerateTask2Times();
        }
        else if (taskNumber == 3)
        {
            StopGameMode();
        }
    }

    private void InitializeTask3UI(Panel container)
    {
        container.Controls.Clear();

        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180f));
        container.Controls.Add(layout);

        Label title3 = new Label
        {
            Text = "🎮 ИГРОВОЙ РЕЖИМ 🎮",
            Font = new Font("Arial", 24, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#FF5722"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        layout.Controls.Add(title3, 0, 0);

        bestScoreLabel = new Label
        {
            Text = "Лучший результат: -",
            Font = new Font("Arial", 16, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#2196F3"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        UpdateBestScoreLabel();
        layout.Controls.Add(bestScoreLabel, 0, 1);

        timerLabel = new Label
        {
            Text = "Время: 60 сек",
            Font = new Font("Arial", 32, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#FF5722"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        layout.Controls.Add(timerLabel, 0, 2);

        statsLabel = new Label
        {
            Text = "Правильно: 0 | Неправильно: 0",
            Font = new Font("Arial", 16, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        layout.Controls.Add(statsLabel, 0, 3);

        Panel gameTaskPanel = new Panel
        {
            BackColor = ColorTranslator.FromHtml("#FAFAFA"),
            Dock = DockStyle.Fill,
            Padding = new Padding(30),
            Tag = "GameTaskPanel"
        };
        layout.Controls.Add(gameTaskPanel, 0, 4);

        TableLayoutPanel gameContentLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        gameContentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        gameContentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
        gameTaskPanel.Controls.Add(gameContentLayout);

        Panel clocksHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        gameContentLayout.Controls.Add(clocksHost, 0, 0);

        Label gameTaskLabel = new Label
        {
            Text = "Задание появится после начала игры",
            Font = new Font("Arial", 18, FontStyle.Regular),
            ForeColor = ColorTranslator.FromHtml("#666666"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        gameTaskLabel.Tag = "GameTaskLabel";
        clocksHost.Controls.Add(gameTaskLabel);

        gameClockPanel1 = new Panel
        {
            Size = new Size(ClockSize, ClockSize),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };
        gameClockPanel1.Paint += ClockPanel_Paint;
        gameClockPanel1.MouseDown += ClockPanel_MouseDown;
        gameClockPanel1.MouseMove += ClockPanel_MouseMove;
        gameClockPanel1.MouseUp += ClockPanel_MouseUp;
        SetDoubleBuffered(gameClockPanel1);
        clocksHost.Controls.Add(gameClockPanel1);

        Label gameTargetLabel = new Label
        {
            Text = "00:00",
            Font = new Font("Arial", 40, FontStyle.Bold),
            Size = new Size(ClockSize, 60),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false,
            Tag = "GameTargetLabel",
            ForeColor = ColorTranslator.FromHtml("#4CAF50")
        };
        clocksHost.Controls.Add(gameTargetLabel);

        gameClockPanel2 = new Panel
        {
            Size = new Size(ClockSize, ClockSize),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false,
            Tag = "GameStart"
        };
        gameClockPanel2.Paint += ClockPanel_Paint;
        SetDoubleBuffered(gameClockPanel2);
        clocksHost.Controls.Add(gameClockPanel2);

        Label gameStartLabel = new Label
        {
            Text = "НАЧАЛО",
            Font = new Font("Arial", 14, FontStyle.Bold),
            Size = new Size(ClockSize, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false,
            Tag = "GameStartLabel",
            ForeColor = ColorTranslator.FromHtml("#4CAF50")
        };
        clocksHost.Controls.Add(gameStartLabel);

        gameClockPanel3 = new Panel
        {
            Size = new Size(ClockSize, ClockSize),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false,
            Tag = "GameEnd"
        };
        gameClockPanel3.Paint += ClockPanel_Paint;
        SetDoubleBuffered(gameClockPanel3);
        clocksHost.Controls.Add(gameClockPanel3);

        Label gameEndLabel = new Label
        {
            Text = "КОНЕЦ",
            Font = new Font("Arial", 14, FontStyle.Bold),
            Size = new Size(ClockSize, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false,
            Tag = "GameEndLabel",
            ForeColor = ColorTranslator.FromHtml("#FF5722")
        };
        clocksHost.Controls.Add(gameEndLabel);

        EventHandler clocksHostResize = (s, e) =>
        {
            int topMargin = 30;
            int singleClockY = topMargin + 60;
            gameClockPanel1.Location = new Point((clocksHost.Width - ClockSize) / 2,
                                                 singleClockY);
            gameTargetLabel.Location = new Point((clocksHost.Width - gameTargetLabel.Width) / 2,
                                                 topMargin);

            int spacing = 120;
            int totalWidth = ClockSize * 2 + spacing;
            int startX = Math.Max(0, (clocksHost.Width - totalWidth) / 2);
            int clocksY = topMargin + 80;

            gameStartLabel.Location = new Point(startX,
                                                topMargin);
            gameClockPanel2.Location = new Point(startX,
                                                 clocksY);

            int endX = startX + ClockSize + spacing;
            gameEndLabel.Location = new Point(endX,
                                              topMargin);
            gameClockPanel3.Location = new Point(endX,
                                                 clocksY);
        };
        clocksHost.Resize += clocksHostResize;
        clocksHostResize(clocksHost, EventArgs.Empty);

        Panel questionHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        gameContentLayout.Controls.Add(questionHost, 0, 1);

        Label gameQuestionLabel = new Label
        {
            Text = "Сколько минут прошло?",
            Font = new Font("Arial", 20, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#333333"),
            Visible = false,
            Tag = "GameQuestionLabel",
            AutoSize = true
        };
        questionHost.Controls.Add(gameQuestionLabel);
        EventHandler questionHostResize = (s, e) =>
        {
            gameQuestionLabel.Location = new Point((questionHost.Width - gameQuestionLabel.Width) / 2,
                                                   (questionHost.Height - gameQuestionLabel.Height) / 2);
        };
        questionHost.Resize += questionHostResize;
        questionHostResize(questionHost, EventArgs.Empty);

        TableLayoutPanel actionsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        actionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
        layout.Controls.Add(actionsLayout, 0, 5);

        Panel answerControlsHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        actionsLayout.Controls.Add(answerControlsHost, 0, 0);

        TableLayoutPanel answerControls = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent
        };
        answerControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
        answerControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));
        answerControlsHost.Controls.Add(answerControls);
        EventHandler answerControlsHostResize = (s, e) =>
        {
            answerControls.Location = new Point((answerControlsHost.Width - answerControls.Width) / 2,
                                                (answerControlsHost.Height - answerControls.Height) / 2);
        };
        answerControlsHost.Resize += answerControlsHostResize;
        answerControlsHostResize(answerControlsHost, EventArgs.Empty);

        gameAnswerTextBox = new TextBox
        {
            Font = new Font("Arial", 20),
            Size = new Size(260, 44),
            PlaceholderText = "Ответ",
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };
        answerControls.Controls.Add(gameAnswerTextBox, 0, 0);

        checkButton3 = new Button
        {
            Text = "✓ ПРОВЕРИТЬ",
            Font = new Font("Arial", 18, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#4CAF50"),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(250, 60),
            Cursor = Cursors.Hand,
            Visible = false
        };
        checkButton3.FlatAppearance.BorderSize = 0;
        checkButton3.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#45A049");
        checkButton3.Click += CheckGameAnswer_Click;
        answerControls.Controls.Add(checkButton3, 0, 1);

        Panel startButtonHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        actionsLayout.Controls.Add(startButtonHost, 0, 1);

        startGameButton = new Button
        {
            Text = "▶ НАЧАТЬ ИГРУ",
            Font = new Font("Arial", 20, FontStyle.Bold),
            BackColor = ColorTranslator.FromHtml("#FF5722"),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(320, 70),
            Cursor = Cursors.Hand
        };
        startGameButton.FlatAppearance.BorderSize = 0;
        startGameButton.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#E64A19");
        startGameButton.Click += StartGame_Click;
        startButtonHost.Controls.Add(startGameButton);
        EventHandler startButtonHostResize = (s, e) =>
        {
            startGameButton.Location = new Point((startButtonHost.Width - startGameButton.Width) / 2,
                                                 (startButtonHost.Height - startGameButton.Height) / 2);
        };
        startButtonHost.Resize += startButtonHostResize;
        startButtonHostResize(startButtonHost, EventArgs.Empty);
    }

    private void GenerateTargetTime1()
    {
        Random rand = new Random();
        int hour = rand.Next(1, 12);
        int minute = rand.Next(0, 12) * 5;

        _targetTime = new DateTime(2000, 1, 1, hour, minute, 0);

        if (targetTimeDisplay1 != null)
            targetTimeDisplay1.Text = _targetTime.ToString(
                "h:mm ", System.Globalization.CultureInfo.InvariantCulture);

        _currentTime = new DateTime(2000, 1, 1, 12, 0, 0);
        if (clockPanel1 != null)
            clockPanel1.Invalidate();
    }

    private void CheckTime1_Click(object? sender, EventArgs e)
    {
        TimeSpan timeDiff = _currentTime - _targetTime;

        if (Math.Abs(timeDiff.TotalMinutes) < 3)
        {
            MessageBox.Show(
                $"Верно! Вы установили время {_currentTime.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture)}.",
                "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
            GenerateTargetTime1();
        }
        else
        {
            MessageBox.Show(
                $"Неверно. Вы установили {_currentTime.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture)}. Целевое время: {_targetTime.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture)}. Попробуйте еще раз!",
                "Результат", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void GenerateTask2Times()
    {
        Random rand = new Random();

        int startHour = rand.Next(1, 12);
        int startMinute = rand.Next(0, 12) * 5;
        _startTime2 = new DateTime(2000, 1, 1, startHour, startMinute, 0);

        int durationMinutes = rand.Next(1, 72) * 5;

        _endTime2 = _startTime2.AddMinutes(durationMinutes);

        if (clockPanelStart != null)
            clockPanelStart.Invalidate();
        if (clockPanelEnd != null)
            clockPanelEnd.Invalidate();

        if (answerTextBox2 != null)
            answerTextBox2.Clear();
    }

    private void CheckTime2_Click(object? sender, EventArgs e)
    {
        if (answerTextBox2 == null ||
            string.IsNullOrWhiteSpace(answerTextBox2.Text))
        {
            MessageBox.Show("Пожалуйста, введите ответ.", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!double.TryParse(answerTextBox2.Text.Replace('.', ','),
                             out double userAnswerValue))
        {
            MessageBox.Show("Ответ должен быть числом (в минутах или часах).",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        TimeSpan difference = _endTime2 - _startTime2;
        double correctMinutes = difference.TotalMinutes;
        double correctHours = difference.TotalHours;

        bool isCorrect = (Math.Abs(userAnswerValue - correctMinutes) < 3) ||
                         (Math.Abs((userAnswerValue * 60) - correctMinutes) < 3);

        if (isCorrect)
        {
            MessageBox.Show(
                $"Верно! Прошло {correctMinutes} минут ({correctHours:F2} ч).",
                "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
            GenerateTask2Times();
        }
        else
        {
            MessageBox.Show(
                $"Неверно. Правильный ответ: {correctMinutes} минут ({correctHours:F2} ч). Попробуйте еще раз!",
                "Результат", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ========== ИГРОВОЙ РЕЖИМ ==========

    private void StartGame_Click(object? sender, EventArgs e)
    {
        if (gameModeActive)
            return;

        gameModeActive = true;
        gameTimeSeconds = 60;
        correctAnswers = 0;
        incorrectAnswers = 0;

        if (startGameButton != null)
        {
            startGameButton.Visible = false;
            startGameButton.Enabled = false;
        }

        if (timerLabel != null)
            timerLabel.Text = $"Время: {gameTimeSeconds} сек";

        if (statsLabel != null)
            statsLabel.Text = "Правильно: 0 | Неправильно: 0";

        if (gameAnswerTextBox != null)
        {
            gameAnswerTextBox.Clear();
            gameAnswerTextBox.Visible = true;
            gameAnswerTextBox.Focus();
        }

        if (checkButton3 != null)
            checkButton3.Visible = true;

        // Скрываем все элементы задания
        HideAllGameTaskElements();

        // Генерируем первое задание
        GenerateGameTask();

        // Запускаем таймер
        gameTimer = new System.Windows.Forms.Timer();
        gameTimer.Interval = 1000; // 1 секунда
        gameTimer.Tick += GameTimer_Tick;
        gameTimer.Start();
    }

    private void GameTimer_Tick(object? sender, EventArgs e)
    {
        gameTimeSeconds--;

        if (timerLabel != null)
        {
            if (gameTimeSeconds <= 10)
                timerLabel.ForeColor = ColorTranslator.FromHtml("#FF0000");
            else
                timerLabel.ForeColor = ColorTranslator.FromHtml("#FF5722");
            timerLabel.Text = $"Время: {gameTimeSeconds} сек";
        }

        if (gameTimeSeconds <= 0)
        {
            StopGameMode();
            ShowGameResults();
        }
    }

    private void StopGameMode()
    {
        gameModeActive = false;

        if (gameTimer != null)
        {
            gameTimer.Stop();
            gameTimer.Dispose();
            gameTimer = null;
        }

        if (startGameButton != null)
        {
            startGameButton.Visible = true;
            startGameButton.Enabled = true;
            startGameButton.Text = "▶ НАЧАТЬ ИГРУ СНОВА";
        }

        if (gameAnswerTextBox != null)
        {
            gameAnswerTextBox.Visible = false;
            gameAnswerTextBox.Clear();
        }

        if (checkButton3 != null)
            checkButton3.Visible = false;

        HideAllGameTaskElements();

        // Показываем сообщение о задании
        if (task3Container != null)
        {
            foreach (Control control in GetAllControls(task3Container))
            {
                if (control.Tag?.ToString() == "GameTaskLabel")
                {
                    control.Visible = true;
                    control.Text = "Игра завершена! Нажмите 'НАЧАТЬ ИГРУ СНОВА' для новой игры.";
                    break;
                }
            }
        }
    }

    private void HideAllGameTaskElements()
    {
        if (gameClockPanel1 != null)
            gameClockPanel1.Visible = false;
        if (gameClockPanel2 != null)
            gameClockPanel2.Visible = false;
        if (gameClockPanel3 != null)
            gameClockPanel3.Visible = false;

        if (task3Container != null)
        {
            foreach (Control control in GetAllControls(task3Container))
            {
                if (control.Tag?.ToString() == "GameTargetLabel" ||
                    control.Tag?.ToString() == "GameQuestionLabel" ||
                    control.Tag?.ToString() == "GameTaskLabel" ||
                    control.Tag?.ToString() == "GameStartLabel" ||
                    control.Tag?.ToString() == "GameEndLabel")
                {
                    control.Visible = false;
                }
            }
        }
    }

    private List<Control> GetAllControls(Control container)
    {
        List<Control> controls = new List<Control>();
        foreach (Control control in container.Controls)
        {
            controls.Add(control);
            if (control.HasChildren)
                controls.AddRange(GetAllControls(control));
        }
        return controls;
    }

    private void GenerateGameTask()
    {
        Random rand = new Random();
        currentGameTask = rand.Next(1, 3); // 1 или 2

        HideAllGameTaskElements();

        if (currentGameTask == 1)
        {
            // Задание: показать время
            int hour = rand.Next(1, 12);
            int minute = rand.Next(0, 12) * 5;
            _targetTime = new DateTime(2000, 1, 1, hour, minute, 0);
            _currentTime = new DateTime(2000, 1, 1, 12, 0, 0);

            if (gameClockPanel1 != null)
            {
                gameClockPanel1.Visible = true;
                gameClockPanel1.Invalidate();
            }

            if (task3Container != null)
            {
                foreach (Control control in GetAllControls(task3Container))
                {
                    if (control.Tag?.ToString() == "GameTargetLabel")
                    {
                        control.Text = _targetTime.ToString("h:mm", System.Globalization.CultureInfo.InvariantCulture);
                        control.Visible = true;
                        break;
                    }
                }
            }
        }
        else
        {
            // Задание: сколько прошло
            int startHour = rand.Next(1, 12);
            int startMinute = rand.Next(0, 12) * 5;
            _startTime2 = new DateTime(2000, 1, 1, startHour, startMinute, 0);

            int durationMinutes = rand.Next(1, 72) * 5;
            _endTime2 = _startTime2.AddMinutes(durationMinutes);

            if (gameClockPanel2 != null)
            {
                gameClockPanel2.Visible = true;
                gameClockPanel2.Invalidate();
            }
            if (gameClockPanel3 != null)
            {
                gameClockPanel3.Visible = true;
                gameClockPanel3.Invalidate();
            }

            if (task3Container != null)
            {
                foreach (Control control in GetAllControls(task3Container))
                {
                    if (control.Tag?.ToString() == "GameQuestionLabel")
                    {
                        control.Visible = true;
                    }
                    else if (control.Tag?.ToString() == "GameStartLabel" || 
                             control.Tag?.ToString() == "GameEndLabel")
                    {
                        control.Visible = true;
                    }
                }
            }
        }

        if (gameAnswerTextBox != null)
        {
            gameAnswerTextBox.Clear();
            gameAnswerTextBox.Focus();
        }
    }

    private void CheckGameAnswer_Click(object? sender, EventArgs e)
    {
        if (!gameModeActive || gameAnswerTextBox == null ||
            string.IsNullOrWhiteSpace(gameAnswerTextBox.Text))
            return;

        bool isCorrect = false;

        if (currentGameTask == 1)
        {
            // Проверка: показать время
            TimeSpan timeDiff = _currentTime - _targetTime;
            isCorrect = Math.Abs(timeDiff.TotalMinutes) < 3;
        }
        else
        {
            // Проверка: сколько прошло
            if (!double.TryParse(gameAnswerTextBox.Text.Replace('.', ','),
                                 out double userAnswerValue))
            {
                MessageBox.Show("Ответ должен быть числом.",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TimeSpan difference = _endTime2 - _startTime2;
            double correctMinutes = difference.TotalMinutes;
            isCorrect = (Math.Abs(userAnswerValue - correctMinutes) < 3) ||
                       (Math.Abs((userAnswerValue * 60) - correctMinutes) < 3);
        }

        if (isCorrect)
        {
            correctAnswers++;
        }
        else
        {
            incorrectAnswers++;
        }

        if (statsLabel != null)
            statsLabel.Text = $"Правильно: {correctAnswers} | Неправильно: {incorrectAnswers}";

        // Генерируем новое задание
        GenerateGameTask();
    }

    private void ShowGameResults()
    {
        string message = $"Игра завершена!\n\n" +
                        $"Правильных ответов: {correctAnswers}\n" +
                        $"Неправильных ответов: {incorrectAnswers}\n" +
                        $"Точность: {(correctAnswers + incorrectAnswers > 0 ? (correctAnswers * 100.0 / (correctAnswers + incorrectAnswers)):0):F1}%";

        bool isNewRecord = false;
        if (bestRecord == null || correctAnswers > bestRecord.CorrectAnswers ||
            (correctAnswers == bestRecord.CorrectAnswers && incorrectAnswers < bestRecord.IncorrectAnswers))
        {
            isNewRecord = true;
            bestRecord = new GameRecord
            {
                CorrectAnswers = correctAnswers,
                IncorrectAnswers = incorrectAnswers,
                GameTimeSeconds = 60,
                Date = DateTime.Now
            };
            SaveBestRecord();
            UpdateBestScoreLabel();
            message += "\n\n🎉 НОВЫЙ РЕКОРД! 🎉";
        }

        MessageBox.Show(message, "Результаты игры",
                       MessageBoxButtons.OK,
                       isNewRecord ? MessageBoxIcon.Information : MessageBoxIcon.None);
    }

    private void LoadBestRecord()
    {
        try
        {
            string filePath = Path.Combine(Application.UserAppDataPath, "best_record.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                bestRecord = JsonSerializer.Deserialize<GameRecord>(json);
            }
        }
        catch
        {
            // Игнорируем ошибки загрузки
        }
    }

    private void SaveBestRecord()
    {
        try
        {
            string filePath = Path.Combine(Application.UserAppDataPath, "best_record.json");
            string directory = Path.GetDirectoryName(filePath)!;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(bestRecord);
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Игнорируем ошибки сохранения
        }
    }

    private void UpdateBestScoreLabel()
    {
        if (bestScoreLabel != null)
        {
            if (bestRecord != null)
            {
                bestScoreLabel.Text = $"🏆 Лучший результат: {bestRecord.CorrectAnswers} правильных, " +
                                     $"{bestRecord.IncorrectAnswers} неправильных " +
                                     $"(Точность: {(bestRecord.CorrectAnswers + bestRecord.IncorrectAnswers > 0 ? (bestRecord.CorrectAnswers * 100.0 / (bestRecord.CorrectAnswers + bestRecord.IncorrectAnswers)):0):F1}%)";
            }
            else
            {
                bestScoreLabel.Text = "🏆 Лучший результат: пока нет рекордов";
            }
        }
    }

    private void ClockPanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Panel? panel = sender as Panel;
            bool isGameMode = panel == gameClockPanel1;
            bool isNormalMode = task1Radio != null && task1Radio.Checked && panel == clockPanel1;

            if (isNormalMode || isGameMode)
            {
                Point mousePos = e.Location;

                _draggingHand = GetHandAtMousePosition(mousePos);
                if (_draggingHand != HandType.None)
                {
                    isDragging = true;
                }
            }
        }
    }

    private void ClockPanel_MouseUp(object? sender, MouseEventArgs e)
    {
        isDragging = false;
        _draggingHand = HandType.None;
    }

    private void ClockPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (isDragging && _draggingHand != HandType.None)
        {
            int deltaX = e.X - _dragCenter.X;
            int deltaY = _dragCenter.Y - e.Y;

            double angleRad = Math.Atan2(deltaY, deltaX);
            double angleDeg = angleRad * (180.0 / Math.PI);

            double normalizedAngle = (90 - angleDeg + 360) % 360;

            int oldHour12 = _currentTime.Hour % 12;
            if (oldHour12 == 0)
                oldHour12 = 12;

            int oldMinute = _currentTime.Minute;

            if (_draggingHand == HandType.Minute)
            {
                int totalMinutes = (int)Math.Round(normalizedAngle / 6.0);
                int newMinute = (totalMinutes / 5) * 5;
                newMinute = (newMinute + 60) % 60;

                int newHour12 = oldHour12;

                if (oldMinute >= 55 && newMinute < 5)
                {
                    newHour12 = (oldHour12 % 12) + 1;
                    if (newHour12 > 12)
                        newHour12 = 1;
                }
                else if (oldMinute < 5 && newMinute >= 55)
                {
                    newHour12 = oldHour12 - 1;
                    if (newHour12 < 1)
                        newHour12 = 12;
                }

                _currentTime = new DateTime(2000, 1, 1, newHour12, newMinute, 0);

            }
            else if (_draggingHand == HandType.Hour)
            {
                int newHour12 = (int)Math.Round(normalizedAngle / 30.0);
                newHour12 = (newHour12 + 12) % 12;
                if (newHour12 == 0)
                    newHour12 = 12;

                _currentTime =
                    new DateTime(2000, 1, 1, newHour12, _currentTime.Minute, 0);
            }

            if (clockPanel1 != null && task1Radio != null && task1Radio.Checked)
                clockPanel1.Invalidate();
            if (gameClockPanel1 != null && gameModeActive)
                gameClockPanel1.Invalidate();
        }
    }

    private HandType GetHandAtMousePosition(Point mousePos)
    {
        double hourAngle =
            (_currentTime.Hour % 12 + _currentTime.Minute / 60.0) * 30.0;
        Point hourEndPoint = GetHandEndPoint(hourAngle, Radius - 60);

        if (IsPointNearLine(mousePos, _dragCenter, hourEndPoint, 20))
        {
            return HandType.Hour;
        }

        double minuteAngle = _currentTime.Minute * 6.0;
        Point minuteEndPoint = GetHandEndPoint(minuteAngle, Radius - 30);

        if (IsPointNearLine(mousePos, _dragCenter, minuteEndPoint, 20))
        {
            return HandType.Minute;
        }

        return HandType.None;
    }

    private bool IsPointNearLine(Point p, Point p1, Point p2, int tolerance)
    {
        double L2 = (p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y);
        if (L2 == 0.0)
            return Distance(p, p1) < tolerance;

        double t =
            ((p.X - p1.X) * (p2.X - p1.X) + (p.Y - p1.Y) * (p2.Y - p1.Y)) / L2;
        t = Math.Max(0, Math.Min(1, t));

        Point projection = new Point((int)(p1.X + t * (p2.X - p1.X)),
                                     (int)(p1.Y + t * (p2.Y - p1.Y)));
        return Distance(p, projection) < tolerance;
    }

    private double Distance(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }

    private Point GetHandEndPoint(double angle, int length)
    {
        double radianAngle = (angle - 90) * Math.PI / 180;
        int x2 = ClockCenterX + (int)(length * Math.Cos(radianAngle));
        int y2 = ClockCenterY + (int)(length * Math.Sin(radianAngle));
        return new Point(x2, y2);
    }

    private void ClockPanel_Paint(object? sender, PaintEventArgs e)
    {
        Panel? panel = sender as Panel;
        if (panel == null)
            return;

        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        DateTime timeToDraw;
        if (panel == clockPanel1 || panel == gameClockPanel1)
        {
            timeToDraw = _currentTime;
        }
        else if (panel == clockPanelStart || panel == gameClockPanel2)
        {
            timeToDraw = _startTime2;
        }
        else if (panel == clockPanelEnd || panel == gameClockPanel3)
        {
            timeToDraw = _endTime2;
        }
        else
        {
            return;
        }

        // Градиентный фон для часов
        using (LinearGradientBrush brush = new LinearGradientBrush(
            new Rectangle(0, 0, ClockSize, ClockSize),
            Color.White,
            ColorTranslator.FromHtml("#F5F5F5"),
            45f))
        {
            g.FillEllipse(brush, 0, 0, ClockSize, ClockSize);
        }

        // Тень для часов
        using (GraphicsPath path = new GraphicsPath())
        {
            path.AddEllipse(3, 3, ClockSize - 1, ClockSize - 1);
            using (Pen shadowPen = new Pen(ColorTranslator.FromHtml("#CCCCCC"), 4))
            {
                g.DrawPath(shadowPen, path);
            }
        }

        // Градиентная рамка
        using (Pen borderPen = new Pen(ColorTranslator.FromHtml("#66BB6A"), 4))
        {
            g.DrawEllipse(borderPen, 2, 2, ClockSize - 5, ClockSize - 5);
        }

        using (Font font = new Font("Arial", 12, FontStyle.Bold))
        {
            for (int i = 1; i <= 12; i++)
            {
                double angle = i * 30 * Math.PI / 180;
                int x = (int)(ClockCenterX + (Radius - 20) * Math.Sin(angle));
                int y = (int)(ClockCenterY - (Radius - 20) * Math.Cos(angle));

                SizeF size = g.MeasureString(i.ToString(), font);
                g.DrawString(i.ToString(), font, Brushes.Black, x - size.Width / 2,
                             y - size.Height / 2);
            }

            for (int i = 0; i < 60; i++)
            {
                double angle = i * 6 * Math.PI / 180;
                int dotX = (int)(ClockCenterX + (Radius - 5) * Math.Sin(angle));
                int dotY = (int)(ClockCenterY - (Radius - 5) * Math.Cos(angle));

                if (i % 5 == 0)
                {
                    g.FillEllipse(Brushes.Gray, dotX - 3, dotY - 3, 6, 6);
                }
                else
                {
                    g.FillEllipse(Brushes.LightGray, dotX - 2, dotY - 2, 4, 4);
                }
            }
        }

        double minuteAngle = timeToDraw.Minute * 6.0;
        DrawArrowHand(g, minuteAngle, Radius - 30,
                      new SolidBrush(ColorTranslator.FromHtml("#3F51B5")), 6);

        double hour = timeToDraw.Hour % 12 + timeToDraw.Minute / 60.0;
        double hourAngle = hour * 30.0;
        DrawArrowHand(g, hourAngle, Radius - 60,
                      new SolidBrush(ColorTranslator.FromHtml("#4DD0E1")), 8);

        g.FillEllipse(Brushes.Black, ClockCenterX - 5, ClockCenterY - 5, 10, 10);
    }

    private void DrawArrowHand(Graphics g, double angle, int length,
                               SolidBrush brush, int baseWidth)
    {
        double radianAngle = (angle - 90) * Math.PI / 180;

        int tipX = ClockCenterX + (int)(length * Math.Cos(radianAngle));
        int tipY = ClockCenterY + (int)(length * Math.Sin(radianAngle));

        int baseStartX = ClockCenterX + (int)(5 * Math.Cos(radianAngle));
        int baseStartY = ClockCenterY + (int)(5 * Math.Sin(radianAngle));

        double baseAngleOffset = Math.PI / 2;

        int p1X = (int)(baseStartX + (baseWidth / 2.0) *
                                         Math.Cos(radianAngle + baseAngleOffset));
        int p1Y = (int)(baseStartY + (baseWidth / 2.0) *
                                         Math.Sin(radianAngle + baseAngleOffset));

        int p2X = (int)(baseStartX + (baseWidth / 2.0) *
                                         Math.Cos(radianAngle - baseAngleOffset));
        int p2Y = (int)(baseStartY + (baseWidth / 2.0) *
                                         Math.Sin(radianAngle - baseAngleOffset));

        int arrowHeadLength = 15;
        int arrowHeadWidth = baseWidth + 5;

        int neckX = ClockCenterX +
                    (int)((length - arrowHeadLength) * Math.Cos(radianAngle));
        int neckY = ClockCenterY +
                    (int)((length - arrowHeadLength) * Math.Sin(radianAngle));

        int leftWingX = (int)(neckX + (arrowHeadWidth / 2.0) *
                                          Math.Cos(radianAngle + baseAngleOffset));
        int leftWingY = (int)(neckY + (arrowHeadWidth / 2.0) *
                                          Math.Sin(radianAngle + baseAngleOffset));

        int rightWingX = (int)(neckX + (arrowHeadWidth / 2.0) *
                                           Math.Cos(radianAngle - baseAngleOffset));
        int rightWingY = (int)(neckY + (arrowHeadWidth / 2.0) *
                                           Math.Sin(radianAngle - baseAngleOffset));

        Point[] arrowPoints = { new Point(p1X, p1Y),
                            new Point(leftWingX, leftWingY),
                            new Point(tipX, tipY),
                            new Point(rightWingX, rightWingY),
                            new Point(p2X, p2Y) };

        g.FillPolygon(brush, arrowPoints);
    }
}