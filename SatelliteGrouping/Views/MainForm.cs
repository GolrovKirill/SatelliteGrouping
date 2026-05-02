using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SatelliteGrouping.Models;
using SatelliteGrouping.Services;

namespace SatelliteGrouping.Views
{
    public partial class MainForm : Form
    {
        private int n;
        private List<SatelliteLink> links;
        private int T_max;
        private List<TimeSlot> timeSlots;          // тактовая модель
        private List<TimeSegment> timeSegments;    // непрерывные сегменты
        private int currentSlotIndex = 0;

        private Color[] groupColors = {
            Color.LightBlue, Color.LightGreen, Color.Orange, Color.Plum, Color.LightSalmon,
            Color.Aquamarine, Color.Gold, Color.LightCoral, Color.LightSeaGreen, Color.Violet
        };

        private Panel ganttPanel;
        private Panel graphPanel;
        private TrackBar slotTrackBar;
        private Label lblSlot;
        private Button btnPrevSlot, btnNextSlot;
        private Button btnLoad, btnExample, btnMatrix;
        private TextBox txtOutput;
        private PointF[] vertexPositions;

        public MainForm()
        {
            InitializeComponent();
            LoadExample();
        }

        private void InitializeComponent()
        {
            this.Text = "Спутники и группировки";
            this.Size = new Size(1050, 750);
            this.MinimumSize = new Size(900, 600);

            ganttPanel = new Panel()
            {
                Location = new Point(10, 10),
                Size = new Size(600, 200),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            ganttPanel.Paint += GanttPanel_Paint;

            graphPanel = new Panel()
            {
                Location = new Point(10, 220),
                Size = new Size(600, 300),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            graphPanel.Paint += GraphPanel_Paint;

            slotTrackBar = new TrackBar()
            {
                Location = new Point(10, 530),
                Size = new Size(300, 45),
                Minimum = 0,
                Maximum = 0,
                TickStyle = TickStyle.None
            };
            slotTrackBar.Scroll += (s, e) =>
            {
                currentSlotIndex = slotTrackBar.Value;
                RefreshAll();
            };

            lblSlot = new Label()
            {
                Location = new Point(320, 530),
                Size = new Size(200, 25),
                Text = "Такт 0: [0-10)"
            };

            btnPrevSlot = new Button()
            {
                Text = "◀",
                Location = new Point(320, 555),
                Size = new Size(40, 30)
            };
            btnPrevSlot.Click += (s, e) =>
            {
                if (currentSlotIndex > 0)
                {
                    currentSlotIndex--;
                    RefreshAll();
                }
            };

            btnNextSlot = new Button()
            {
                Text = "▶",
                Location = new Point(370, 555),
                Size = new Size(40, 30)
            };
            btnNextSlot.Click += (s, e) =>
            {
                if (timeSlots != null && currentSlotIndex < timeSlots.Count - 1)
                {
                    currentSlotIndex++;
                    RefreshAll();
                }
            };

            btnLoad = new Button()
            {
                Text = "Загрузить из файла",
                Location = new Point(10, 600),
                Size = new Size(130, 30)
            };
            btnLoad.Click += BtnLoad_Click;

            btnExample = new Button()
            {
                Text = "Пример",
                Location = new Point(150, 600),
                Size = new Size(80, 30)
            };
            btnExample.Click += (s, e) => { LoadExample(); };

            btnMatrix = new Button()
            {
                Text = "Матрица интервалов",
                Location = new Point(240, 600),
                Size = new Size(140, 30)
            };
            btnMatrix.Click += BtnMatrix_Click;

            txtOutput = new TextBox()
            {
                Location = new Point(620, 10),
                Size = new Size(400, 600),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 12)
            };

            this.Controls.Add(ganttPanel);
            this.Controls.Add(graphPanel);
            this.Controls.Add(slotTrackBar);
            this.Controls.Add(lblSlot);
            this.Controls.Add(btnPrevSlot);
            this.Controls.Add(btnNextSlot);
            this.Controls.Add(btnLoad);
            this.Controls.Add(btnExample);
            this.Controls.Add(btnMatrix);
            this.Controls.Add(txtOutput);
        }

        private void LoadExample()
        {
            n = 5;
            T_max = 30;
            links = new List<SatelliteLink>
            {
                new SatelliteLink { SatelliteA = 0, SatelliteB = 1, StartTime = 0, EndTime = 20 },
                new SatelliteLink { SatelliteA = 1, SatelliteB = 2, StartTime = 10, EndTime = 30 },
                new SatelliteLink { SatelliteA = 3, SatelliteB = 4, StartTime = 5, EndTime = 15 }
            };
            ProcessData();
        }

        private void ProcessData()
        {
            timeSlots = Grouper.GenerateTimeSlots(n, T_max, links);
            timeSegments = BuildTimeSegments();

            slotTrackBar.Maximum = Math.Max(0, timeSlots.Count - 1);
            currentSlotIndex = 0;
            slotTrackBar.Value = 0;

            // позиции вершин для графа
            vertexPositions = new PointF[n];
            float cx = graphPanel.Width / 2f;
            float cy = graphPanel.Height / 2f;
            float radius = Math.Min(cx, cy) - 30;
            for (int i = 0; i < n; i++)
            {
                double angle = 2 * Math.PI * i / n - Math.PI / 2;
                vertexPositions[i] = new PointF(
                    cx + (float)(radius * Math.Cos(angle)),
                    cy + (float)(radius * Math.Sin(angle))
                );
            }

            RefreshAll();
        }

        private List<TimeSegment> BuildTimeSegments()
        {
            if (links == null || links.Count == 0)
                return new List<TimeSegment>();

            var events = new HashSet<int>();
            foreach (var link in links)
            {
                events.Add(link.StartTime);
                events.Add(link.EndTime);
            }
            events.Add(0);
            events.Add(T_max);
            var sortedEvents = events.OrderBy(e => e).ToList();

            var segments = new List<TimeSegment>();
            for (int i = 0; i < sortedEvents.Count - 1; i++)
            {
                int start = sortedEvents[i];
                int end = sortedEvents[i + 1];
                if (start == end) continue;

                var activeLinks = links
                    .Where(l => l.StartTime <= start && l.EndTime >= end)
                    .ToList();

                var adj = new List<int>[n];
                for (int v = 0; v < n; v++) adj[v] = new List<int>();
                foreach (var link in activeLinks)
                {
                    adj[link.SatelliteA].Add(link.SatelliteB);
                    adj[link.SatelliteB].Add(link.SatelliteA);
                }

                bool[] visited = new bool[n];
                var groups = new List<HashSet<int>>();
                for (int v = 0; v < n; v++)
                {
                    if (!visited[v])
                    {
                        var group = new HashSet<int>();
                        var queue = new Queue<int>();
                        queue.Enqueue(v);
                        visited[v] = true;
                        while (queue.Count > 0)
                        {
                            int cur = queue.Dequeue();
                            group.Add(cur);
                            foreach (int nb in adj[cur])
                                if (!visited[nb])
                                {
                                    visited[nb] = true;
                                    queue.Enqueue(nb);
                                }
                        }
                        groups.Add(group);
                    }
                }
                segments.Add(new TimeSegment
                {
                    Start = start,
                    End = end,
                    Groups = groups,
                    ActiveLinks = activeLinks
                });
            }
            return segments;
        }

        private void RefreshAll()
        {
            UpdateSlotInfo();
            slotTrackBar.Value = currentSlotIndex;
            ganttPanel.Invalidate();
            graphPanel.Invalidate();
        }

        private void UpdateSlotInfo()
        {
            if (timeSlots == null || timeSlots.Count == 0) return;
            var slot = timeSlots[currentSlotIndex];
            lblSlot.Text = $"Такт {currentSlotIndex}: [{slot.Start}-{slot.End})";

            string text = $"Такт {currentSlotIndex} [{slot.Start}-{slot.End} мин]\r\n";
            for (int gIdx = 0; gIdx < slot.Groups.Count; gIdx++)
            {
                var members = slot.Groups[gIdx].Select(v => v + 1).OrderBy(x => x);
                text += $"  Группа {gIdx + 1}: {{ {string.Join(", ", members)} }}\r\n";
            }
            text += "\r\n(Всего тактов: " + timeSlots.Count + ")";
            txtOutput.Text = text;
        }

        private void GanttPanel_Paint(object sender, PaintEventArgs e)
        {
            if (timeSegments == null || timeSegments.Count == 0) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            int left = 50, top = 30, right = 10, bottom = 20;
            int chartW = ganttPanel.Width - left - right;
            int chartH = ganttPanel.Height - top - bottom;
            float scaleX = (float)chartW / T_max;
            float satH = (float)chartH / n;

            // подписи времени
            using (Font font = new Font("Arial", 8))
                for (int t = 0; t <= T_max; t += 10)
                {
                    float x = left + t * scaleX;
                    g.DrawString(t.ToString(), font, Brushes.Black, x, top - 18);
                }

            // спутники
            for (int sat = 0; sat < n; sat++)
            {
                float y = top + sat * satH;

                // чёрная линия дорожки
                using (Pen linePen = new Pen(Color.Black, 1))
                    g.DrawLine(linePen, left, y + satH / 2, left + chartW, y + satH / 2);

                // подпись спутника
                g.DrawString($"КА{sat + 1}", SystemFonts.DefaultFont, Brushes.Black, 2, y + satH / 2 - 7);

                // прямоугольники активности по непрерывным сегментам
                for (int i = 0; i < timeSegments.Count; i++)
                {
                    var seg = timeSegments[i];

                    // Определяем группу для спутника
                    int grpIdx = -1;
                    for (int gIdx = 0; gIdx < seg.Groups.Count; gIdx++)
                        if (seg.Groups[gIdx].Contains(sat))
                        {
                            grpIdx = gIdx;
                            break;
                        }

                    // Является ли спутник одиночкой в этом сегменте?
                    bool isAlone = (grpIdx == -1) || (seg.Groups[grpIdx].Count == 1);
                    Color fillColor = isAlone ? Color.White : groupColors[grpIdx % groupColors.Length];

                    float x = left + seg.Start * scaleX;
                    float w = (seg.End - seg.Start) * scaleX;
                    float rectHeight = satH * 0.6f;
                    float rectTop = y + (satH - rectHeight) / 2;

                    // Рисуем заполненный прямоугольник
                    using (Brush br = new SolidBrush(fillColor))
                        g.FillRectangle(br, x, rectTop, w, rectHeight);

                    // Граница: для одиночек — светло-серая, для групп — тёмно-серая
                    using (Pen borderPen = new Pen(isAlone ? Color.LightGray : Color.DarkGray, 1))
                        g.DrawRectangle(borderPen, x, rectTop, w, rectHeight);
                }
            }

            // красный курсор текущего такта (дискретный)
            if (timeSlots != null && currentSlotIndex >= 0 && currentSlotIndex < timeSlots.Count)
            {
                int cursorTime = timeSlots[currentSlotIndex].Start;
                float cursorX = left + cursorTime * scaleX;
                using (Pen cursorPen = new Pen(Color.Red, 2))
                    g.DrawLine(cursorPen, cursorX, top, cursorX, top + n * satH);
            }
        }

        private void GraphPanel_Paint(object sender, PaintEventArgs e)
        {
            if (timeSlots == null || vertexPositions == null) return;
            var slot = timeSlots[currentSlotIndex];
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (Pen pen = new Pen(Color.Gray, 2))
                foreach (var link in slot.ActiveLinks)
                    g.DrawLine(pen, vertexPositions[link.SatelliteA], vertexPositions[link.SatelliteB]);

            int r = 20;
            for (int i = 0; i < n; i++)
            {
                int grpIdx = -1;
                for (int gIdx = 0; gIdx < slot.Groups.Count; gIdx++)
                    if (slot.Groups[gIdx].Contains(i)) { grpIdx = gIdx; break; }
                Color fill = (grpIdx >= 0) ? groupColors[grpIdx % groupColors.Length] : Color.LightGray;

                var pt = vertexPositions[i];
                RectangleF rect = new RectangleF(pt.X - r, pt.Y - r, r * 2, r * 2);
                using (Brush br = new SolidBrush(fill))
                    g.FillEllipse(br, rect);
                g.DrawEllipse(Pens.Black, rect);
                g.DrawString((i + 1).ToString(), new Font("Arial", 10, FontStyle.Bold), Brushes.Black, pt.X - 8, pt.Y - 8);
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var (nn, mm, tmax, l) = InputParser.Parse(ofd.FileName);
                        n = nn;
                        T_max = tmax;
                        links = l;
                        ProcessData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при загрузке:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnMatrix_Click(object sender, EventArgs e)
        {
            if (links == null || n == 0) return;

            var form = new Form
            {
                Text = "Матрица временных интервалов",
                Size = new Size(700, 500),
                StartPosition = FormStartPosition.CenterParent
            };

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = true,
                ColumnHeadersVisible = true
            };

            var intervalsByPair = new Dictionary<(int, int), List<SatelliteLink>>();
            foreach (var link in links)
            {
                int a = Math.Min(link.SatelliteA, link.SatelliteB);
                int b = Math.Max(link.SatelliteA, link.SatelliteB);
                if (!intervalsByPair.ContainsKey((a, b)))
                    intervalsByPair[(a, b)] = new List<SatelliteLink>();
                intervalsByPair[(a, b)].Add(link);
            }

            for (int i = 0; i < n; i++)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    HeaderText = (i + 1).ToString(),
                    Width = 100,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };
                dgv.Columns.Add(col);
            }

            for (int i = 0; i < n; i++)
            {
                var row = new DataGridViewRow();
                row.HeaderCell.Value = (i + 1).ToString();
                for (int j = 0; j < n; j++)
                {
                    var cell = new DataGridViewTextBoxCell();
                    if (i == j)
                    {
                        cell.Value = "—";
                    }
                    else
                    {
                        int a = Math.Min(i, j);
                        int b = Math.Max(i, j);
                        if (intervalsByPair.TryGetValue((a, b), out var list))
                        {
                            cell.Value = string.Join(", ", list.Select(l => l.ToString()));
                        }
                        else
                        {
                            cell.Value = "";
                        }
                    }
                    row.Cells.Add(cell);
                }
                dgv.Rows.Add(row);
            }

            form.Controls.Add(dgv);
            form.ShowDialog();
        }
    }
}