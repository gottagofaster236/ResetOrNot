﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.UI;

namespace ResetOrNot.UI.Components
{
    public partial class ResetOrNotSettings : UserControl
    {
        public Boolean UsePercentOfAttempts { get; set; }
        public Boolean UseFixedAttempts { get; set; }
        public int AttemptCount { get; set; }
        public bool IgnoreRunCount { get; set; }
        public int TimeToReset { get; set; }
        public bool SettingsLoaded { get; set; } = false;

        public event EventHandler SettingChanged;

        public ResetOrNotSettings()
        {
            InitializeComponent();

            UsePercentOfAttempts = true;
            UseFixedAttempts = true;
            AttemptCount = 50;
            TimeToReset = 15;

            PercentOfAttempts.DataBindings.Add("Checked", this, "UsePercentOfAttempts", true, DataSourceUpdateMode.OnPropertyChanged).BindingComplete += OnSettingChanged;
            FixedAttempts.DataBindings.Add("Checked", this, "UseFixedAttempts", true, DataSourceUpdateMode.OnPropertyChanged).BindingComplete += OnSettingChanged;
            AttemptCountBox.DataBindings.Add("Value", this, "AttemptCount", true, DataSourceUpdateMode.OnPropertyChanged).BindingComplete += OnSettingChanged;
            IgnoreRunCountBox.DataBindings.Add("Checked", this, "IgnoreRunCount", true, DataSourceUpdateMode.OnPropertyChanged).BindingComplete += OnSettingChanged;
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
                SettingsHelper.CreateSetting(document, parent, "UsePercentOfAttempts", UsePercentOfAttempts) ^
                SettingsHelper.CreateSetting(document, parent, "UseFixedAttempts", UseFixedAttempts) ^
                SettingsHelper.CreateSetting(document, parent, "IgnoreRunCount", IgnoreRunCount) ^
                SettingsHelper.CreateSetting(document, parent, "TimeToReset", TimeToReset);
        }

        internal void SetSettings(XmlNode settings)
        {
            AttemptCount = SettingsHelper.ParseInt(settings["AttemptCount"]);
            UsePercentOfAttempts = SettingsHelper.ParseBool(settings["UsePercentOfAttempts"]);
            UseFixedAttempts = SettingsHelper.ParseBool(settings["UseFixedAttempts"]);
            IgnoreRunCount = SettingsHelper.ParseBool(settings["IgnoreRunCount"]);
            TimeToReset = SettingsHelper.ParseInt(settings["TimeToReset"]);
            SettingsLoaded = true;
        }
    }
}
