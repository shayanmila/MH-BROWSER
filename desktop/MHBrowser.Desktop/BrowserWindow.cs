using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Drawing;
using System.Windows.Forms;

namespace MHBrowser.Desktop;

public sealed class BrowserWindow : Form
{
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    private readonly TextBox address = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11) };
    private readonly ToolStripButton back = new("Back");
    private readonly ToolStripButton forward = new("Forward");
    private readonly ToolStripButton reload = new("Reload");
    private readonly ToolStripButton home = new("Home");
    private readonly ToolStripButton newTab = new("+ New tab");
    private readonly ToolStripButton closeTab = new("Close tab");
    private readonly ToolStripButton go = new("Go");
    private const string HomeUrl = "https://www.google.com/";
    private const string SearchUrl = "https://www.google.com/search?q=";

    public BrowserWindow()
    {
        Text = "MH Browser — Desktop";
        Width = 1160;
        Height = 760;
        MinimumSize = new Size(520, 340);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 24, 43);

        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 43,
            ColumnCount = 2,
            BackColor = Color.FromArgb(16, 34, 58),
            Padding = new Padding(4)
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var actions = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Fill };
        actions.Items.AddRange(new ToolStripItem[] { back, forward, reload, home, newTab, closeTab, go });
        back.Click += (_, _) => { if (Current?.CanGoBack == true) Current.GoBack(); };
        forward.Click += (_, _) => { if (Current?.CanGoForward == true) Current.GoForward(); };
        reload.Click += (_, _) => Current?.Reload();
        home.Click += (_, _) => Navigate(HomeUrl);
        newTab.Click += async (_, _) => await AddTabAsync(HomeUrl);
        closeTab.Click += (_, _) => CloseCurrent();
        go.Click += (_, _) => Navigate(address.Text);
        address.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Navigate(address.Text);
        };

        bar.Controls.Add(actions, 0, 0);
        bar.Controls.Add(address, 1, 0);
        Controls.Add(tabs);
        Controls.Add(bar);
        tabs.SelectedIndexChanged += (_, _) => UpdateChrome();
        Shown += async (_, _) => await AddTabAsync(HomeUrl);
        FormClosing += (_, _) =>
        {
            foreach (TabPage tab in tabs.TabPages)
                if (tab.Controls.OfType<WebView2>().FirstOrDefault() is WebView2 view)
                    view.Dispose();
        };
    }

    private WebView2? Current => tabs.SelectedTab?.Controls.OfType<WebView2>().FirstOrDefault();

    private static string Normalize(string raw)
    {
        string input = raw.Trim();
        if (string.IsNullOrEmpty(input)) return HomeUrl;
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return uri.ToString();
        if (!input.Contains(' ') && input.Contains('.') &&
            Uri.TryCreate("https://" + input, UriKind.Absolute, out uri))
            return uri.ToString();
        return SearchUrl + Uri.EscapeDataString(input);
    }

    private void Navigate(string url)
    {
        if (Current is { CoreWebView2: not null } view)
            view.CoreWebView2.Navigate(Normalize(url));
    }

    private async Task AddTabAsync(string url)
    {
        var page = new TabPage("New Tab");
        var view = new WebView2 { Dock = DockStyle.Fill };
        page.Controls.Add(view);
        tabs.TabPages.Add(page);
        tabs.SelectedTab = page;
        try
        {
            await view.EnsureCoreWebView2Async();
            if (view.IsDisposed || page.IsDisposed) return;
            view.CoreWebView2.Settings.AreDevToolsEnabled = true;
            view.CoreWebView2.NewWindowRequested += async (_, e) =>
            {
                e.Handled = true;
                await AddTabAsync(e.Uri);
            };
            view.CoreWebView2.NavigationStarting += (_, _) => UpdateChrome();
            view.CoreWebView2.NavigationCompleted += (_, _) => UpdateChrome();
            view.CoreWebView2.DocumentTitleChanged += (_, _) =>
            {
                if (page.IsDisposed) return;
                page.Text = ShortTitle(view.CoreWebView2.DocumentTitle);
                UpdateChrome();
            };
            view.CoreWebView2.SourceChanged += (_, _) => UpdateChrome();
            view.CoreWebView2.Navigate(Normalize(url));
            UpdateChrome();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not initialize Microsoft Edge WebView2. Install or repair the WebView2 Runtime.\n\n" + ex.Message,
                "MH Browser", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (!page.IsDisposed) tabs.TabPages.Remove(page);
            view.Dispose();
        }
    }

    private static string ShortTitle(string title) =>
        string.IsNullOrWhiteSpace(title) ? "New Tab" : title.Length > 22 ? title[..22] + "…" : title;

    private void CloseCurrent()
    {
        var page = tabs.SelectedTab;
        if (page is null) return;
        tabs.TabPages.Remove(page);
        foreach (var view in page.Controls.OfType<WebView2>().ToArray()) view.Dispose();
        page.Dispose();
        if (tabs.TabCount == 0) _ = AddTabAsync(HomeUrl);
        UpdateChrome();
    }

    private void UpdateChrome()
    {
        var view = Current;
        if (view?.CoreWebView2 is not null)
        {
            if (!address.Focused) address.Text = view.Source?.ToString() ?? "";
            back.Enabled = view.CanGoBack;
            forward.Enabled = view.CanGoForward;
            Text = "MH Browser — " + (string.IsNullOrWhiteSpace(view.CoreWebView2.DocumentTitle) ? "Desktop" : view.CoreWebView2.DocumentTitle);
        }
        else
        {
            address.Text = "";
            back.Enabled = false;
            forward.Enabled = false;
            Text = "MH Browser — Desktop";
        }
        closeTab.Enabled = tabs.TabCount > 0;
    }
}
