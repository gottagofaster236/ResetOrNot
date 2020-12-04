using System;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.UI;

namespace ResetOrNot.UI.Components
{
    public partial class ResetOrNotSettings : UserControl
    {
        public int AttemptCount { get; set; }
        public int TimeToReset { get; set; }
        public bool SettingsLoaded { get; set; } = false;

        public event EventHandler SettingChanged;

        public ResetOrNotSettings()
        {
            InitializeComponent();

            AttemptCount = 50;
            TimeToReset = 30;

            AttemptCountBox.DataBindings.Add("Value", this, "AttemptCount", true, DataSourceUpdateMode.OnPropertyChanged).BindingComplete += OnSettingChanged;
            TimeToResetCountBox.DataBindings.Add("Value", this, "TimeToReset", true, DataSourceUpdateMode.OnPropertyChanged).BindingComplete += OnSettingChanged;
        }

        private void OnSettingChanged(object sender, BindingCompleteEventArgs e)
        {
            SettingChanged?.Invoke(this, e);
        }

        public LayoutMode Mode { get; internal set; }

        internal XmlNode GetSettings(XmlDocument document)
        {
            var parent = document.CreateElement("Settings");
            CreateSettingsNode(document, parent);
            return parent;
        }

        private int CreateSettingsNode(XmlDocument document, XmlElement parent)
        {
            return SettingsHelper.CreateSetting(document, parent, "Version", "0.1") ^
                SettingsHelper.CreateSetting(document, parent, "AttemptCount", AttemptCount) ^
                SettingsHelper.CreateSetting(document, parent, "TimeToReset", TimeToReset);
        }

        internal void SetSettings(XmlNode settings)
        {
            AttemptCount = SettingsHelper.ParseInt(settings["AttemptCount"]);
            TimeToReset = SettingsHelper.ParseInt(settings["TimeToReset"]);
            SettingsLoaded = true;
        }
    }
}
