using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using WF = System.Windows.Forms;

[assembly: AssemblyTitle("Kyklos")]
[assembly: AssemblyProduct("Kyklos")]
[assembly: AssemblyCompany("Florian Rasche")]
[assembly: AssemblyDescription("Auswahlrad für Textbausteine und Makros")]
[assembly: AssemblyCopyright("© 2026 Florian Rasche · MIT-Lizenz")]
[assembly: AssemblyVersion("1.6.0.0")]
[assembly: AssemblyFileVersion("1.6.0.0")]
// Ohne diese Angabe behandelt WPF die App wie ein altes Programm und skaliert nicht pro Monitor.
[assembly: TargetFramework(".NETFramework,Version=v4.8", FrameworkDisplayName = ".NET Framework 4.8")]

namespace Kyklos
{
    public static class Program
    {
        public const string Version = "1.6";

        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length >= 1 && args[0].StartsWith("--dev-", StringComparison.Ordinal)) return Dev.Run(args);

            bool first;
            using (var mutex = new Mutex(true, "Kyklos.Rad.Instanz", out first))
            using (var show = new EventWaitHandle(false, EventResetMode.AutoReset, "Kyklos.Rad.Zeigen"))
            using (var quit = new EventWaitHandle(false, EventResetMode.AutoReset, "Kyklos.Rad.Beenden"))
            {
                if (!first)
                {
                    // Läuft schon: mit --quit beenden, sonst dessen Einstellungen nach vorn holen.
                    if (args.Contains("--quit")) quit.Set(); else show.Set();
                    return 0;
                }
                if (args.Contains("--quit")) return 0;

                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                Theme.Apply(app);
                var host = new AppHost();
                app.DispatcherUnhandledException += (s, e) =>
                {
                    Log.Write("Unbehandelter Fehler: " + e.Exception);
                    MessageBox.Show("Es ist ein Fehler aufgetreten:\n\n" + e.Exception.Message + "\n\nDetails stehen in Kyklos.log.",
                                    "Kyklos", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Handled = true;
                };

                var watcher = new Thread(() =>
                {
                    try
                    {
                        while (true)
                        {
                            int which = WaitHandle.WaitAny(new WaitHandle[] { show, quit });
                            app.Dispatcher.BeginInvoke(which == 0 ? (Action)host.ShowSettings : host.Quit);
                        }
                    }
                    catch (ObjectDisposedException) { }     // Programm wird gerade beendet
                });
                watcher.IsBackground = true;
                watcher.Start();

                host.Start(args.Contains("--settings"));
                app.Run();
                GC.KeepAlive(mutex);
                return 0;
            }
        }
    }

    public static class Theme
    {
        public static object LoadXaml(string name)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Kyklos." + name))
            {
                if (s == null) throw new FileNotFoundException("Eingebettete Ressource fehlt: " + name);
                return XamlReader.Load(s);
            }
        }

        public static void Apply(Application app)
        {
            app.Resources.MergedDictionaries.Add((ResourceDictionary)LoadXaml("Theme.xaml"));
        }

        public static System.Drawing.Icon AppIcon(int size)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Kyklos.app.ico"))
            {
                if (s == null) return System.Drawing.SystemIcons.Application;
                return new System.Drawing.Icon(s, new System.Drawing.Size(size, size));
            }
        }
    }

    /// <summary>Hält Konfiguration, Rad, Tray-Symbol und Einstellungsfenster zusammen.</summary>
    public sealed class AppHost
    {
        public Config Config;
        public string ConfigPath;
        public event Action Saved;
        public event Action SaveFailed;

        Overlay _overlay;
        SettingsWindow _settings;
        WF.NotifyIcon _tray;
        WF.MenuItem _pauseItem;
        DispatcherTimer _saveTimer;
        bool _dirty;                // es gibt Änderungen, die noch nicht in der Datei stehen
        int _saveFailures;          // Fehlversuche in Folge
        const int WarnAfter = 8;    // erst dann eine Meldung – davor wird still weiterprobiert (rund 17 Sekunden)

        public void Start(bool showSettings)
        {
            ConfigPath = ConfigStore.ResolvePath();
            Log.File = Path.Combine(Path.GetDirectoryName(ConfigPath), "Kyklos.log");
            bool created;
            string problem;
            Config = ConfigStore.Load(ConfigPath, out created, out problem);

            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _saveTimer.Tick += (s, e) => SaveNow();

            _overlay = new Overlay(this);
            InputHook.TriggerDown += OnTriggerDown;
            InputHook.TriggerUp += () => _overlay.OnTriggerUp();
            InputHook.Repress += () => _overlay.OnRepress();
            InputHook.Click += () => _overlay.OnClick();
            InputHook.Cancel += () => _overlay.Cancel();
            ApplyBindings();
            InputHook.Start(Dispatcher.CurrentDispatcher);

            BuildTray();

            if (problem != null) MessageBox.Show(problem, "Kyklos", MessageBoxButton.OK, MessageBoxImage.Warning);
            if (!InputHook.Installed)
                MessageBox.Show("Windows hat die Tastatur- und Maus-Überwachung nicht zugelassen. Die Auslöser funktionieren deshalb nicht.\n\n" +
                                "Häufige Ursache ist ein Virenschutz, der das Programm blockiert.", "Kyklos",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            if (created) SaveNow();
            if (created || showSettings) ShowSettings();
        }

        void OnTriggerDown(InputHook.Binding b)
        {
            var wheel = Config.Wheels.FirstOrDefault(w => w.Id == b.WheelId);
            if (wheel == null) { InputHook.EndSession(); return; }
            // Alt oder Win allein loszulassen öffnet sonst Menüleiste bzw. Startmenü des Zielprogramms.
            if (b.Trigger.Alt || b.Trigger.Win) KeySender.Tap(0xE8);
            _overlay.Open(wheel);
        }

        public void Run(ActionDef action, Slot origin = null)
        {
            ActionRunner.Enqueue(action, Config.Settings, origin);
        }

        void ApplyBindings()
        {
            InputHook.SetBindings(Config.Wheels.SelectMany(w => w.Triggers
                                               .Select(t => new InputHook.Binding
                                               {
                                                   Trigger = t.Clone(), WheelId = w.Id,
                                                   Apps = w.Apps.Where(a => !a.IsEmpty).Select(a => a.Clone()).ToArray()
                                               })));
        }

        /// <summary>Nach jeder Änderung in den Einstellungen: Auslöser sofort übernehmen, Datei kurz darauf schreiben.</summary>
        public void ConfigChanged()
        {
            ApplyBindings();
            _dirty = true;
            if (_saveTimer == null) return;
            _saveTimer.Stop();
            if (_saveFailures == 0) _saveTimer.Interval = TimeSpan.FromMilliseconds(400);
            _saveTimer.Start();
        }

        public void ReplaceConfig(Config cfg)
        {
            Config = cfg;
            ConfigChanged();
        }

        /// <summary>
        /// Schreibt die Konfiguration. Scheitert das, wird in wachsenden Abständen still weiterprobiert: Meist hält nur
        /// für einen Moment ein Virenscanner oder die Synchronisierung die Datei fest. Eine Meldung gibt es erst, wenn es
        /// dauerhaft nicht klappt – und dann genau einmal.
        /// </summary>
        public bool SaveNow()
        {
            if (_saveTimer == null) return false;
            _saveTimer.Stop();
            try
            {
                ConfigStore.Save(Config, ConfigPath);
            }
            catch (Exception ex)
            {
                _saveFailures++;
                if (_saveFailures == 1 || _saveFailures == WarnAfter)
                    Log.Write("Speichern fehlgeschlagen (Versuch " + _saveFailures + "): " + ex.Message);
                _saveTimer.Interval = TimeSpan.FromMilliseconds(Math.Min(5000, 600 * _saveFailures));
                _saveTimer.Start();
                var failed = SaveFailed;
                if (failed != null) failed();
                if (_saveFailures == WarnAfter)
                    MessageBox.Show("Die Konfiguration lässt sich seit einigen Sekunden nicht speichern:\n\n" + ex.Message +
                                    "\n\nDatei: " + ConfigPath + "\n\nKyklos versucht es weiter. Deine Änderungen gelten bereits; " +
                                    "sie stehen nur noch nicht in der Datei.", "Kyklos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            _dirty = false;
            _saveFailures = 0;
            _saveTimer.Interval = TimeSpan.FromMilliseconds(400);
            var saved = Saved;
            if (saved != null) saved();
            return true;
        }

        public void ShowSettings()
        {
            if (_settings == null)
            {
                _settings = new SettingsWindow(this);
                _settings.Closed += () => { _settings = null; if (_dirty) SaveNow(); };
            }
            _settings.Show();
        }

        void BuildTray()
        {
            _pauseItem = new WF.MenuItem("Pausieren", (s, e) => SetPaused(!InputHook.Paused));
            var open = new WF.MenuItem("Einstellungen …", (s, e) => ShowSettings()) { DefaultItem = true };
            var quit = new WF.MenuItem("Beenden", (s, e) => Quit());
            _tray = new WF.NotifyIcon
            {
                Icon = Theme.AppIcon(WF.SystemInformation.SmallIconSize.Width),
                Text = "Kyklos",
                ContextMenu = new WF.ContextMenu(new[] { open, _pauseItem, new WF.MenuItem("-"), quit }),
                Visible = true
            };
            _tray.MouseClick += (s, e) => { if (e.Button == WF.MouseButtons.Left) ShowSettings(); };
        }

        void SetPaused(bool paused)
        {
            InputHook.Paused = paused;
            _pauseItem.Checked = paused;
            _tray.Text = paused ? "Kyklos (pausiert)" : "Kyklos";
        }

        public void Quit()
        {
            if (_dirty)
            {
                for (int n = 0; n < 4 && !SaveNow(); n++) Thread.Sleep(200);
                _saveTimer.Stop();
                if (_dirty)
                {
                    bool parked = File.Exists(ConfigPath + ".tmp");
                    MessageBox.Show(parked
                        ? "Die letzten Änderungen konnten nicht in die Konfigurationsdatei geschrieben werden, weil ein anderes Programm sie festhält.\n\n" +
                          "Sie liegen in „" + Path.GetFileName(ConfigPath) + ".tmp“ daneben und werden beim nächsten Start übernommen."
                        : "Die letzten Änderungen konnten nicht gespeichert werden.\n\nDatei: " + ConfigPath,
                        "Kyklos", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            _tray.Visible = false;
            _tray.Dispose();
            InputHook.Stop();
            Application.Current.Shutdown();
        }
    }
}
