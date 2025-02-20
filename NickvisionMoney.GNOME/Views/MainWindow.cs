﻿using NickvisionMoney.Shared.Controllers;
using NickvisionMoney.Shared.Events;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NickvisionMoney.GNOME.Views;

/// <summary>
/// EventArgs for WidthChanged Event
/// </summary>
public class WidthChangedEventArgs : EventArgs
{
    public bool SmallWidth { get; init; }

    public WidthChangedEventArgs(bool smallWidth) => SmallWidth = smallWidth;
}

/// <summary>
/// The MainWindow for the application
/// </summary>
public partial class MainWindow : Adw.ApplicationWindow
{
    [LibraryImport("adwaita-1", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint adw_show_about_window(nint parent,
        string appNameKey, string appNameValue,
        string iconKey, string iconValue,
        string versionKey, string versionValue,
        string commentsKey, string commentsValue,
        string developerNameKey, string developerNameValue,
        string licenseKey, int licenseValue,
        string copyrightKey, string copyrightValue,
        string websiteKey, string websiteValue,
        string issueTrackerKey, string issueTrackerValue,
        string supportUrlKey, string supportUrlValue,
        string developersKey, string[] developersValue,
        string designersKey, string[] designersValue,
        string artistsKey, string[] artistsValue,
        string translatorCreditsKey, string translatorCreditsValue,
        string releaseNotesKey, string releaseNotesValue,
        nint terminator);

    [LibraryImport("adwaita-1", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint gtk_file_chooser_get_file(nint chooser);

    [LibraryImport("adwaita-1", StringMarshalling = StringMarshalling.Utf8)]
    private static partial string g_file_get_path(nint file);

    [LibraryImport("adwaita-1", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nuint g_file_get_type();

    private delegate void SignalCallback(nint gObject, nint gParamSpec, nint data);

    [LibraryImport("adwaita-1", StringMarshalling = StringMarshalling.Utf8)]
    private static partial ulong g_signal_connect_data(nint instance, string detailed_signal, [MarshalAs(UnmanagedType.FunctionPtr)] SignalCallback c_handler, nint data, nint destroy_data, int connect_flags);

    private readonly MainWindowController _controller;
    private readonly Adw.Application _application;
    private readonly Gtk.Box _mainBox;
    private readonly Adw.HeaderBar _headerBar;
    private readonly Adw.WindowTitle _windowTitle;
    private readonly Gtk.MenuButton _btnMenuAccount;
    private readonly Gtk.Popover _popoverAccount;
    private readonly Gtk.Box _popBoxAccount;
    private readonly Gtk.Box _popBoxButtons;
    private readonly Gtk.Button _popBtnNewAccount;
    private readonly Adw.ButtonContent _popBtnNewAccountContext;
    private readonly Gtk.Button _popBtnOpenAccount;
    private readonly List<Adw.ActionRow> _listRecentAccountsRows;
    private readonly Adw.PreferencesGroup _groupRecentAccounts;
    private readonly Gtk.ToggleButton _btnFlapToggle;
    private readonly Gtk.MenuButton _btnMainMenu;
    private readonly Adw.ToastOverlay _toastOverlay;
    private readonly Adw.ViewStack _viewStack;
    private readonly Adw.StatusPage _pageStatusNoAccounts;
    private readonly Gtk.Box _boxStatusPage;
    private readonly List<Adw.ActionRow> _listRecentAccountsOnStartRows;
    private readonly Adw.Clamp _clampRecentAccountsOnStart;
    private readonly Adw.PreferencesGroup _grpRecentAccountsOnStart;
    private readonly Gtk.FlowBox _flowBoxStatusButtons;
    private readonly Gtk.Button _btnNewAccount;
    private readonly Gtk.Button _btnOpenAccount;
    private readonly Gtk.Label _lblDrag;
    private readonly Gtk.Box _pageTabs;
    private readonly Adw.TabView _tabView;
    private readonly Adw.TabBar _tabBar;
    private readonly List<Adw.TabPage> _accountViews;
    private readonly Gtk.DropTarget _dropTarget;
    private readonly Gio.SimpleAction _actNewAccount;
    private readonly Gio.SimpleAction _actOpenAccount;
    private readonly Gio.SimpleAction _actCloseAccount;

    /// <summary>
    /// Occurs when the window's width is changed
    /// </summary>
    public event EventHandler<WidthChangedEventArgs>? WidthChanged;

    /// <summary>
    /// Constructs a MainWindow
    /// </summary>
    /// <param name="controller">The MainWindowController</param>
    /// <param name="application">The Adw.Application</param>
    public MainWindow(MainWindowController controller, Adw.Application application)
    {
        //Window Settings
        _controller = controller;
        _application = application;
        _listRecentAccountsRows = new List<Adw.ActionRow>();
        _listRecentAccountsOnStartRows = new List<Adw.ActionRow>();
        _accountViews = new List<Adw.TabPage>();
        SetDefaultSize(900, 720);
        SetSizeRequest(360, -1);
        if(_controller.IsDevVersion)
        {
            AddCssClass("devel");
        }
        //Register Events
        _controller.NotificationSent += NotificationSent;
        _controller.AccountAdded += AccountAdded;
        g_signal_connect_data(Handle, "notify::default-width", (nint sender, nint gParamSpec, nint data) => OnWidthChanged(), IntPtr.Zero, IntPtr.Zero, 0);
        //Main Box
        _mainBox = Gtk.Box.New(Gtk.Orientation.Vertical, 0);
        //Header Bar
        _headerBar = Adw.HeaderBar.New();
        _windowTitle = Adw.WindowTitle.New(_controller.AppInfo.ShortName, "");
        _headerBar.SetTitleWidget(_windowTitle);
        _mainBox.Append(_headerBar);
        //Account Popover
        _popoverAccount = Gtk.Popover.New();
        //Account Popover Buttons Box
        _popBoxButtons = Gtk.Box.New(Gtk.Orientation.Horizontal, 0);
        _popBoxButtons.AddCssClass("linked");
        _popBoxButtons.SetHalign(Gtk.Align.Center);
        _popBoxButtons.SetValign(Gtk.Align.Center);
        //Account Popover New Account Button
        _popBtnNewAccount = Gtk.Button.New();
        _popBtnNewAccount.AddCssClass("suggested-action");
        _popBtnNewAccountContext = Adw.ButtonContent.New();
        _popBtnNewAccountContext.SetLabel(_controller.Localizer["NewAccountPopover", "GTK"]);
        _popBtnNewAccountContext.SetIconName("document-new-symbolic");
        _popBtnNewAccount.SetChild(_popBtnNewAccountContext);
        _popBtnNewAccount.SetTooltipText(_controller.Localizer["NewAccount", "Tooltip"]);
        _popBtnNewAccount.SetDetailedActionName("win.newAccount");
        _popBoxButtons.Append(_popBtnNewAccount);
        //Account Popover Open Account Button
        _popBtnOpenAccount = Gtk.Button.New();
        _popBtnOpenAccount.SetIconName("document-open-symbolic");
        _popBtnOpenAccount.SetTooltipText(_controller.Localizer["OpenAccount", "Tooltip"]);
        _popBtnOpenAccount.SetDetailedActionName("win.openAccount");
        _popBoxButtons.Append(_popBtnOpenAccount);
        //List Recent Accounts
        _groupRecentAccounts = Adw.PreferencesGroup.New();
        _groupRecentAccounts.SetTitle(_controller.Localizer["Recents", "GTK"]);
        _groupRecentAccounts.SetHeaderSuffix(_popBoxButtons);
        _groupRecentAccounts.SetSizeRequest(200, 55);
        //Account Popover Box
        _popBoxAccount = Gtk.Box.New(Gtk.Orientation.Vertical, 10);
        _popBoxAccount.SetMarginStart(5);
        _popBoxAccount.SetMarginEnd(5);
        _popBoxAccount.SetMarginTop(5);
        _popBoxAccount.SetMarginBottom(5);
        _popBoxAccount.Append(_groupRecentAccounts);
        _popoverAccount.SetChild(_popBoxAccount);
        //Menu Account Button
        _btnMenuAccount = Gtk.MenuButton.New();
        _btnMenuAccount.SetVisible(false);
        _btnMenuAccount.SetIconName("bank-symbolic");
        _btnMenuAccount.SetPopover(_popoverAccount);
        _btnMenuAccount.SetTooltipText(_controller.Localizer["AccountMenu", "GTK"]);
        _headerBar.PackStart(_btnMenuAccount);
        //Flap Toggle Button
        _btnFlapToggle = Gtk.ToggleButton.New();
        _btnFlapToggle.SetVisible(false);
        _btnFlapToggle.SetActive(true);
        _btnFlapToggle.SetIconName("sidebar-show-symbolic");
        _btnFlapToggle.SetTooltipText(_controller.Localizer["ToggleSidebar", "GTK"]);
        _headerBar.PackStart(_btnFlapToggle);
        //Main Menu Button
        _btnMainMenu = Gtk.MenuButton.New();
        var mainMenu = Gio.Menu.New();
        mainMenu.Append(_controller.Localizer["Preferences"], "win.preferences");
        mainMenu.Append(_controller.Localizer["KeyboardShortcuts"], "win.keyboardShortcuts");
        mainMenu.Append(string.Format(_controller.Localizer["About"], _controller.AppInfo.ShortName), "win.about");
        _btnMainMenu.SetDirection(Gtk.ArrowType.None);
        _btnMainMenu.SetMenuModel(mainMenu);
        _btnMainMenu.SetTooltipText(_controller.Localizer["MainMenu", "GTK"]);
        _headerBar.PackEnd(_btnMainMenu);
        //Toast Overlay
        _toastOverlay = Adw.ToastOverlay.New();
        _toastOverlay.SetHexpand(true);
        _toastOverlay.SetVexpand(true);
        _mainBox.Append(_toastOverlay);
        //Status Buttons
        _flowBoxStatusButtons = Gtk.FlowBox.New();
        _flowBoxStatusButtons.SetColumnSpacing(12);
        _flowBoxStatusButtons.SetRowSpacing(6);
        _flowBoxStatusButtons.SetMaxChildrenPerLine(2);
        _flowBoxStatusButtons.SetHomogeneous(true);
        _flowBoxStatusButtons.SetHexpand(true);
        _flowBoxStatusButtons.SetHalign(Gtk.Align.Center);
        _flowBoxStatusButtons.SetSelectionMode(Gtk.SelectionMode.None);
        //List Recent Accounts On The Start Screen
        _clampRecentAccountsOnStart = Adw.Clamp.New();
        _clampRecentAccountsOnStart.SetMaximumSize(420);
        _grpRecentAccountsOnStart = Adw.PreferencesGroup.New();
        _grpRecentAccountsOnStart.SetTitle(_controller.Localizer["RecentAccounts"]);
        _grpRecentAccountsOnStart.SetSizeRequest(200, 55);
        _grpRecentAccountsOnStart.SetMarginTop(24);
        _grpRecentAccountsOnStart.SetMarginBottom(24);
        _grpRecentAccountsOnStart.SetVisible(false);
        _clampRecentAccountsOnStart.SetChild(_grpRecentAccountsOnStart);
        //New Account Button
        _btnNewAccount = Gtk.Button.NewWithLabel(_controller.Localizer["NewAccount"]);
        _btnNewAccount.SetHalign(Gtk.Align.Center);
        _btnNewAccount.SetSizeRequest(200, 50);
        _btnNewAccount.AddCssClass("pill");
        _btnNewAccount.AddCssClass("suggested-action");
        _btnNewAccount.SetDetailedActionName("win.newAccount");
        _flowBoxStatusButtons.Append(_btnNewAccount);
        //Open Account Button
        _btnOpenAccount = Gtk.Button.NewWithLabel(_controller.Localizer["OpenAccount"]);
        _btnOpenAccount.SetHalign(Gtk.Align.Center);
        _btnOpenAccount.SetSizeRequest(200, 50);
        _btnOpenAccount.AddCssClass("pill");
        _btnOpenAccount.SetDetailedActionName("win.openAccount");
        _flowBoxStatusButtons.Append(_btnOpenAccount);
        //Drag Label
        _lblDrag = Gtk.Label.New(_controller.Localizer["DragLabel"]);
        _lblDrag.AddCssClass("dim-label");
        _lblDrag.SetWrap(true);
        _lblDrag.SetJustify(Gtk.Justification.Center);
        //Status Page Box
        _boxStatusPage = Gtk.Box.New(Gtk.Orientation.Vertical, 12);
        _boxStatusPage.SetHexpand(true);
        _boxStatusPage.SetHalign(Gtk.Align.Fill);
        _boxStatusPage.Append(_clampRecentAccountsOnStart);
        _boxStatusPage.Append(_flowBoxStatusButtons);
        _boxStatusPage.Append(_lblDrag);
        //Page No Accounts
        _pageStatusNoAccounts = Adw.StatusPage.New();
        _pageStatusNoAccounts.SetIconName(_controller.ShowSun ? "sun-alt-symbolic" : "moon-symbolic");
        _pageStatusNoAccounts.SetTitle(_controller.Greeting);
        _pageStatusNoAccounts.SetDescription(_controller.Localizer["StartPageDescription"]);
        _pageStatusNoAccounts.SetChild(_boxStatusPage);
        //Page Tabs
        _pageTabs = Gtk.Box.New(Gtk.Orientation.Vertical, 0);
        _tabView = Adw.TabView.New();
        _tabView.OnClosePage += OnCloseAccountPage;
        _tabBar = Adw.TabBar.New();
        _tabBar.SetView(_tabView);
        _pageTabs.Append(_tabBar);
        _pageTabs.Append(_tabView);
        //View Stack
        _viewStack = Adw.ViewStack.New();
        _viewStack.AddNamed(_pageStatusNoAccounts, "pageNoAccounts");
        _viewStack.AddNamed(_pageTabs, "pageTabs");
        _toastOverlay.SetChild(_viewStack);
        //Layout
        SetContent(_mainBox);
        //New Account Action
        _actNewAccount = Gio.SimpleAction.New("newAccount", null);
        _actNewAccount.OnActivate += OnNewAccount;
        AddAction(_actNewAccount);
        application.SetAccelsForAction("win.newAccount", new string[] { "<Ctrl>N" });
        //Open Account Action
        _actOpenAccount = Gio.SimpleAction.New("openAccount", null);
        _actOpenAccount.OnActivate += OnOpenAccount;
        AddAction(_actOpenAccount);
        application.SetAccelsForAction("win.openAccount", new string[] { "<Ctrl>O" });
        //Close Account Action
        _actCloseAccount = Gio.SimpleAction.New("closeAccount", null);
        _actCloseAccount.OnActivate += OnCloseAccount;
        AddAction(_actCloseAccount);
        application.SetAccelsForAction("win.closeAccount", new string[] { "<Ctrl>W" });
        _actCloseAccount.SetEnabled(false);
        //Preferences Action
        var actPreferences = Gio.SimpleAction.New("preferences", null);
        actPreferences.OnActivate += Preferences;
        AddAction(actPreferences);
        application.SetAccelsForAction("win.preferences", new string[] { "<Ctrl>comma" });
        //Keyboard Shortcuts Action
        var actKeyboardShortcuts = Gio.SimpleAction.New("keyboardShortcuts", null);
        actKeyboardShortcuts.OnActivate += KeyboardShortcuts;
        AddAction(actKeyboardShortcuts);
        application.SetAccelsForAction("win.keyboardShortcuts", new string[] { "<Ctrl>question" });
        //About Action
        var actAbout = Gio.SimpleAction.New("about", null);
        actAbout.OnActivate += About;
        AddAction(actAbout);
        application.SetAccelsForAction("win.about", new string[] { "F1" });
        //Drop Target
        _dropTarget = Gtk.DropTarget.New(g_file_get_type(), Gdk.DragAction.Copy);
        _dropTarget.OnDrop += OnDrop;
        AddController(_dropTarget);
    }

    /// <summary>
    /// Starts the MainWindow
    /// </summary>
    public void Start()
    {
        Show();
        if(_controller.RecentAccounts.Count > 0)
        {
            UpdateRecentAccountsOnStart();
            _pageStatusNoAccounts.SetDescription("");
            _grpRecentAccountsOnStart.SetVisible(true);
        }
    }

    /// <summary>
    /// Occurs when a notification is sent from the controller
    /// </summary>
    /// <param name="sender">object?</param>
    /// <param name="e">NotificationSentEventArgs</param>
    private void NotificationSent(object? sender, NotificationSentEventArgs e) => _toastOverlay.AddToast(Adw.Toast.New(e.Message));

    /// <summary>
    /// Occurs when an account is created or opened
    /// </summary>
    private void AccountAdded(object? sender, EventArgs e)
    {
        _actCloseAccount.SetEnabled(true);
        _viewStack.SetVisibleChildName("pageTabs");
        var newAccountView = new AccountView(_controller.OpenAccounts[_controller.OpenAccounts.Count - 1], this, _tabView, _btnFlapToggle);
        _tabView.SetSelectedPage(newAccountView.Page);
        _accountViews.Add(newAccountView.Page);
        _windowTitle.SetSubtitle(_controller.OpenAccounts.Count == 1 ? _controller.OpenAccounts[0].AccountPath : "");
        UpdateRecentAccounts();
        _btnMenuAccount.SetVisible(true);
        _btnFlapToggle.SetVisible(true);
    }

    /// <summary>
    /// Creates a new account
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void OnNewAccount(Gio.SimpleAction sender, EventArgs e)
    {
        _popoverAccount.Popdown();
        var saveFileDialog = Gtk.FileChooserNative.New(_controller.Localizer["NewAccount"], this, Gtk.FileChooserAction.Save, _controller.Localizer["Save"], _controller.Localizer["Cancel"]);
        saveFileDialog.SetModal(true);
        var filter = Gtk.FileFilter.New();
        filter.SetName(_controller.Localizer["NMoneyFilter"]);
        filter.AddPattern("*.nmoney");
        saveFileDialog.AddFilter(filter);
        saveFileDialog.OnResponse += (sender, e) =>
        {
            if (e.ResponseId == (int)Gtk.ResponseType.Accept)
            {
                var path = g_file_get_path(gtk_file_chooser_get_file(saveFileDialog.Handle));
                if(_controller.IsAccountOpen(path))
                {
                    _toastOverlay.AddToast(Adw.Toast.New(_controller.Localizer["UnableToOverride"]));
                }
                else
                {
                    if(File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    _controller.AddAccount(path);
                }
            }
        };
        saveFileDialog.Show();
    }

    /// <summary>
    /// Opens a new account
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void OnOpenAccount(Gio.SimpleAction sender, EventArgs e)
    {
        _popoverAccount.Popdown();
        var openFileDialog = Gtk.FileChooserNative.New(_controller.Localizer["OpenAccount"], this, Gtk.FileChooserAction.Open, _controller.Localizer["Open"], _controller.Localizer["Cancel"]);
        openFileDialog.SetModal(true);
        var filter = Gtk.FileFilter.New();
        filter.SetName(_controller.Localizer["NMoneyFilter"]);
        filter.AddPattern("*.nmoney");
        openFileDialog.AddFilter(filter);
        openFileDialog.OnResponse += (sender, e) =>
        {
            if (e.ResponseId == (int)Gtk.ResponseType.Accept)
            {
                var path = g_file_get_path(gtk_file_chooser_get_file(openFileDialog.Handle));
                _controller.AddAccount(path);
            }
        };
        openFileDialog.Show();
    }

    /// <summary>
    /// Closes an opened account
    /// </summary>
    private void OnCloseAccount(Gio.SimpleAction sender, EventArgs e)
    {
        _popoverAccount.Popdown();
        _tabView.ClosePage(_tabView.GetSelectedPage());
    }

    /// <summary>
    /// Occurs when an account page is closing
    /// </summary>
    /// <param name="page">Adw.TabPage</param>
    private void OnCloseAccountPage(Adw.TabView view, Adw.TabView.ClosePageSignalArgs args)
    {
        var indexPage = _tabView.GetPagePosition(args.Page);
        _controller.CloseAccount(indexPage);
        _accountViews.RemoveAt(indexPage);
        _windowTitle.SetSubtitle(_controller.OpenAccounts.Count == 1 ? _controller.OpenAccounts[0].AccountPath : "");
        if (_controller.OpenAccounts.Count == 0)
        {
            _actCloseAccount.SetEnabled(false);
            _viewStack.SetVisibleChildName("pageNoAccounts");
            _btnMenuAccount.SetVisible(false);
            _btnFlapToggle.SetVisible(false);
            UpdateRecentAccountsOnStart();
            _grpRecentAccountsOnStart.SetVisible(true);
        }
    }

    /// <summary>
    /// Occurs when the preferences action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void Preferences(Gio.SimpleAction sender, EventArgs e)
    {
        var preferencesDialog = new PreferencesDialog(_controller.PreferencesViewController, _application, this);
        preferencesDialog.Show();
    }

    /// <summary>
    /// Occurs when the keyboard shortcuts action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void KeyboardShortcuts(Gio.SimpleAction sender, EventArgs e)
    {
        var shortcutsDialog = new ShortcutsDialog(_controller.Localizer, _controller.AppInfo.ShortName, this);
        shortcutsDialog.Show();
    }

    /// <summary>
    /// Occurs when the about action is triggered
    /// </summary>
    /// <param name="sender">Gio.SimpleAction</param>
    /// <param name="e">EventArgs</param>
    private void About(Gio.SimpleAction sender, EventArgs e)
    {
        var developersCredits = new List<string>(_controller.Localizer["Developers", "Credits"].Split(Environment.NewLine));
        developersCredits.Add(null);
        var designersCredits = new List<string>(_controller.Localizer["Designers", "Credits"].Split(Environment.NewLine));
        designersCredits.Add(null);
        var artistsCredits = new List<string>(_controller.Localizer["Artists", "Credits"].Split(Environment.NewLine));
        artistsCredits.Add(null);
        adw_show_about_window(Handle,
            "application-name", _controller.AppInfo.ShortName,
            "application-icon", (_controller.AppInfo.ID + (_controller.AppInfo.GetIsDevelVersion() ? "-devel" : "")),
            "version", _controller.AppInfo.Version,
            "comments", _controller.AppInfo.Description,
            "developer-name", "Nickvision",
            "license-type", (int)Gtk.License.MitX11,
            "copyright", "© Nickvision 2021-2022",
            "website", _controller.AppInfo.GitHubRepo.ToString(),
            "issue-url", _controller.AppInfo.IssueTracker.ToString(),
            "support-url", _controller.AppInfo.SupportUrl.ToString(),
            "developers", developersCredits.ToArray(),
            "designers", designersCredits.ToArray(),
            "artists", artistsCredits.ToArray(),
            "translator-credits", (string.IsNullOrEmpty(_controller.Localizer["Translators", "Credits"]) ? "" : _controller.Localizer["Translators", "Credits"]),
            "release-notes", _controller.AppInfo.Changelog,
            IntPtr.Zero);
    }

    /// <summary>
    /// Occurs when the preferences action is triggered
    /// </summary>
    /// <param name="dropValue">GObject.Value</param>
    /// <param name="e">EventArgs</param>
    private void OnDrop(Gtk.DropTarget sender, Gtk.DropTarget.DropSignalArgs e)
    {
        var obj = e.Value.GetObject();
        if(obj != null)
        {
            var path = g_file_get_path(obj.Handle);
            if(File.Exists(path))
            {
                _controller.AddAccount(path);
            }
        }
    }

    /// <summary>
    /// Updates the list of recent accounts
    /// </summary>
    private void UpdateRecentAccounts()
    {
        foreach(var row in _listRecentAccountsRows)
        {
            _groupRecentAccounts.Remove(row);
        }
        _listRecentAccountsRows.Clear();
        foreach(var accountPath in _controller.RecentAccounts)
        {
            var row = CreateRecentAccountRow(accountPath);
            _groupRecentAccounts.Add(row);
            _listRecentAccountsRows.Add(row);
        }
    }

    /// <summary>
    /// Updates the list of recent accounts on start screen
    /// </summary>
    private void UpdateRecentAccountsOnStart()
    {
        foreach(var row in _listRecentAccountsOnStartRows)
        {
            _grpRecentAccountsOnStart.Remove(row);
        }
        _listRecentAccountsOnStartRows.Clear();
        foreach(var accountPath in _controller.RecentAccounts)
        {
            var row = CreateRecentAccountRow(accountPath);
            _grpRecentAccountsOnStart.Add(row);
            _listRecentAccountsOnStartRows.Add(row);
        }
    }

    /// <summary>
    /// Creates a row for recent accounts lists
    /// </summary>
    /// <param name="accountPath">string</param>
    private Adw.ActionRow CreateRecentAccountRow(string accountPath)
    {
        var row = Adw.ActionRow.New();
        row.SetTitle(Path.GetFileName(accountPath));
        row.SetSubtitle(accountPath);
        var button = Gtk.Button.NewFromIconName("wallet2-symbolic");
        button.SetHalign(Gtk.Align.Center);
        button.SetValign(Gtk.Align.Center);
        button.AddCssClass("wallet-button");
        button.OnClicked += (Gtk.Button sender, EventArgs e) => 
        {
            _popoverAccount.Popdown();
            _controller.AddAccount(row.GetSubtitle()!);
        };
        row.AddPrefix(button);
        row.SetActivatableWidget(button);
        return row;
    }

    /// <summary>
    /// Occurs when the window's width is changed
    /// </summary>
    public void OnWidthChanged() => WidthChanged?.Invoke(this, new WidthChangedEventArgs(DefaultWidth < 450));
}
