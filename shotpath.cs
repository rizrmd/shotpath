using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

public class ShotPath : Form
{
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;
    private string lastScreenshotPath;
    private Image lastScreenshotImage;
    private SelectionForm selectionForm;
    private ToolStripMenuItem startupMenuItem;
    private const string APP_NAME = "ShotPath";
    private const string REGISTRY_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private string screenshotFolder;
    
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
    
    private const int HOTKEY_ID = 9000;
    private const int HOTKEY_ID_CTRL = 9001;
    private const uint VK_SNAPSHOT = 0x2C; // PrintScreen key
    private const uint MOD_CONTROL = 0x0002;
    
    public ShotPath()
    {
        InitializeComponent();
        RegisterHotKey(this.Handle, HOTKEY_ID, 0, VK_SNAPSHOT);
        RegisterHotKey(this.Handle, HOTKEY_ID_CTRL, MOD_CONTROL, VK_SNAPSHOT);
        
        // Create shotpath directory in temp
        screenshotFolder = Path.Combine(Path.GetTempPath(), "shotpath");
        if (!Directory.Exists(screenshotFolder))
        {
            Directory.CreateDirectory(screenshotFolder);
        }
        
        // Ensure run at startup is enabled
        EnsureStartupEnabled();
    }
    
    private void InitializeComponent()
    {
        this.Text = "Screenshot Tool";
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Copy as Path (PrintScreen)", null, CopyAsPath);
        trayMenu.Items.Add("Copy as Image (Ctrl+PrintScreen)", null, CopyAsImage);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Open Folder", null, OpenFolder);
        trayMenu.Items.Add("Clear Folder", null, ClearFolder);
        trayMenu.Items.Add(new ToolStripSeparator());
        
        startupMenuItem = new ToolStripMenuItem("Run at Startup");
        startupMenuItem.CheckOnClick = true;
        startupMenuItem.Checked = IsInStartup();
        startupMenuItem.CheckedChanged += StartupMenuItem_CheckedChanged;
        trayMenu.Items.Add(startupMenuItem);
        
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, Exit);
        
        trayIcon = new NotifyIcon();
        trayIcon.Text = "Screenshot Tool";
        
        // Try to load tray.png if it exists
        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tray.png");
            if (!File.Exists(iconPath))
            {
                iconPath = "tray.png";
            }
            
            if (File.Exists(iconPath))
            {
                trayIcon.Icon = CreateIconFromPng(iconPath);
                // Don't set form icon here - let it use the embedded icon
            }
            else
            {
                trayIcon.Icon = SystemIcons.Application;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to load tray icon: " + ex.Message);
            trayIcon.Icon = SystemIcons.Application;
        }
        
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        
        this.Load += (s, e) => this.Hide();
    }
    
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312)
        {
            int id = m.WParam.ToInt32();
            if (id == HOTKEY_ID)
            {
                TakeScreenshot(false); // Copy as path
            }
            else if (id == HOTKEY_ID_CTRL)
            {
                TakeScreenshot(true); // Copy as image
            }
        }
        base.WndProc(ref m);
    }
    
    private void TakeScreenshot(bool copyAsImage = false)
    {
        this.Hide();
        
        selectionForm = new SelectionForm();
        if (selectionForm.ShowDialog() == DialogResult.OK)
        {
            Rectangle selection = selectionForm.Selection;
            
            using (Bitmap bitmap = new Bitmap(selection.Width, selection.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(selection.Location, Point.Empty, selection.Size);
                }
                
                string fileName = string.Format("screenshot_{0:yyyyMMdd_HHmmss}.png", DateTime.Now);
                lastScreenshotPath = Path.Combine(screenshotFolder, fileName);
                
                bitmap.Save(lastScreenshotPath, ImageFormat.Png);
                lastScreenshotImage = (Image)bitmap.Clone();
            }
            
            if (copyAsImage)
            {
                // Copy as image
                Clipboard.SetImage(lastScreenshotImage);
                trayIcon.ShowBalloonTip(1000, "Screenshot Saved", "Image copied to clipboard", ToolTipIcon.Info);
            }
            else
            {
                // Copy path by default
                Clipboard.SetText(lastScreenshotPath);
                trayIcon.ShowBalloonTip(1000, "Screenshot Saved", Path.GetFileName(lastScreenshotPath), ToolTipIcon.Info);
            }
        }
    }
    
    private void CopyAsPath(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(lastScreenshotPath) && File.Exists(lastScreenshotPath))
        {
            Clipboard.SetText(lastScreenshotPath);
            trayIcon.ShowBalloonTip(1000, "Copied", "Path copied to clipboard", ToolTipIcon.Info);
        }
    }
    
    private void CopyAsImage(object sender, EventArgs e)
    {
        if (lastScreenshotImage != null)
        {
            Clipboard.SetImage(lastScreenshotImage);
            trayIcon.ShowBalloonTip(1000, "Copied", "Image copied to clipboard", ToolTipIcon.Info);
        }
    }
    
    private void OpenFolder(object sender, EventArgs e)
    {
        try
        {
            if (!Directory.Exists(screenshotFolder))
            {
                Directory.CreateDirectory(screenshotFolder);
            }
            System.Diagnostics.Process.Start("explorer.exe", screenshotFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to open folder: " + ex.Message, "Error", 
                           MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    private void ClearFolder(object sender, EventArgs e)
    {
        try
        {
            if (Directory.Exists(screenshotFolder))
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete all screenshots in the folder?", 
                    "Confirm Clear", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.Yes)
                {
                    DirectoryInfo di = new DirectoryInfo(screenshotFolder);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch { }
                    }
                    
                    lastScreenshotPath = null;
                    if (lastScreenshotImage != null)
                    {
                        lastScreenshotImage.Dispose();
                        lastScreenshotImage = null;
                    }
                    
                    trayIcon.ShowBalloonTip(1000, "Folder Cleared", "All screenshots have been deleted", ToolTipIcon.Info);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to clear folder: " + ex.Message, "Error", 
                           MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    private void Exit(object sender, EventArgs e)
    {
        UnregisterHotKey(this.Handle, HOTKEY_ID);
        UnregisterHotKey(this.Handle, HOTKEY_ID_CTRL);
        trayIcon.Visible = false;
        Application.Exit();
    }
    
    private bool IsInStartup()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, false))
            {
                return key != null && key.GetValue(APP_NAME) != null;
            }
        }
        catch
        {
            return false;
        }
    }
    
    private void StartupMenuItem_CheckedChanged(object sender, EventArgs e)
    {
        SetStartup(startupMenuItem.Checked);
    }
    
    private void SetStartup(bool enable)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
            {
                if (enable)
                {
                    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue(APP_NAME, exePath);
                }
                else
                {
                    key.DeleteValue(APP_NAME, false);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to update startup settings: " + ex.Message, "Error", 
                           MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (startupMenuItem != null)
            {
                startupMenuItem.Checked = !enable;
            }
        }
    }
    
    private void EnsureStartupEnabled()
    {
        if (!IsInStartup())
        {
            SetStartup(true);
            if (startupMenuItem != null)
            {
                startupMenuItem.Checked = true;
            }
        }
    }
    
    private Icon CreateIconFromPng(string pngPath)
    {
        using (Bitmap original = new Bitmap(pngPath))
        {
            // Determine the best size - system tray typically uses 16x16 or 32x32
            int targetSize = 32; // Use 32x32 for better quality on high DPI
            
            // Create a high-quality resized version if needed
            Bitmap resized;
            if (original.Width != targetSize || original.Height != targetSize)
            {
                resized = new Bitmap(targetSize, targetSize);
                using (Graphics g = Graphics.FromImage(resized))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(original, 0, 0, targetSize, targetSize);
                }
            }
            else
            {
                resized = new Bitmap(original);
            }
            
            IntPtr hIcon = resized.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            
            // Create a copy since FromHandle doesn't own the handle
            Icon iconCopy = (Icon)icon.Clone();
            DestroyIcon(hIcon);
            resized.Dispose();
            
            return iconCopy;
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (trayIcon != null)
            {
                trayIcon.Dispose();
            }
            if (lastScreenshotImage != null)
            {
                lastScreenshotImage.Dispose();
            }
        }
        base.Dispose(disposing);
    }
    
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new ShotPath());
    }
}

public class SelectionForm : Form
{
    private Point startPoint;
    private Rectangle selection;
    private bool isSelecting;
    
    public Rectangle Selection { get { return selection; } }
    
    public SelectionForm()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.Black;
        this.Opacity = 0.3;
        this.Cursor = Cursors.Cross;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.DoubleBuffered = true;
        
        this.MouseDown += OnMouseDown;
        this.MouseMove += OnMouseMove;
        this.MouseUp += OnMouseUp;
        this.Paint += OnPaint;
        this.KeyDown += OnKeyDown;
    }
    
    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            startPoint = e.Location;
            isSelecting = true;
        }
    }
    
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (isSelecting)
        {
            int x = Math.Min(startPoint.X, e.X);
            int y = Math.Min(startPoint.Y, e.Y);
            int width = Math.Abs(startPoint.X - e.X);
            int height = Math.Abs(startPoint.Y - e.Y);
            
            selection = new Rectangle(x, y, width, height);
            this.Invalidate();
        }
    }
    
    private void OnMouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && isSelecting)
        {
            isSelecting = false;
            if (selection.Width > 0 && selection.Height > 0)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
    
    private void OnPaint(object sender, PaintEventArgs e)
    {
        if (selection.Width > 0 && selection.Height > 0)
        {
            using (Pen pen = new Pen(Color.Red, 2))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                e.Graphics.DrawRectangle(pen, selection);
            }
            
            Region region = new Region(this.ClientRectangle);
            region.Exclude(selection);
            
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            {
                e.Graphics.FillRegion(brush, region);
            }
        }
    }
    
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}