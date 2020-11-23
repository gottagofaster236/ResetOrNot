using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveSplit.UI;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using static ResetOrNot.UI.Components.ResetOrNotCalculator;

namespace ResetOrNot.UI.Components
{
    class ResetOrNotComponent : IComponent
    {
        protected InfoTextComponent InternalComponent { get; set; }
        protected ResetOrNotSettings Settings { get; set; }
        protected LiveSplitState State;
        protected ResetOrNotCalculator resetOrNotCalculator;
        protected string category;

        string IComponent.ComponentName => "Reset Or Not";

        IDictionary<string, Action> IComponent.ContextMenuControls => null;
        float IComponent.HorizontalWidth => InternalComponent.HorizontalWidth;
        float IComponent.MinimumHeight => InternalComponent.MinimumHeight;
        float IComponent.MinimumWidth => InternalComponent.MinimumWidth;
        float IComponent.PaddingBottom => InternalComponent.PaddingBottom;
        float IComponent.PaddingLeft => InternalComponent.PaddingLeft;
        float IComponent.PaddingRight => InternalComponent.PaddingRight;
        float IComponent.PaddingTop => InternalComponent.PaddingTop;
        float IComponent.VerticalHeight => InternalComponent.VerticalHeight;

        XmlNode IComponent.GetSettings(XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        Control IComponent.GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        void IComponent.SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public ResetOrNotComponent(LiveSplitState state)
        {
            State = state;
            InternalComponent = new InfoTextComponent("Reset Or Not", "Reset")
            {
                AlternateNameText = new string[]
                {
                    "Reset Or Not",
                    "Should reset?"
                }
            };
            Settings = new ResetOrNotSettings();
            Settings.SettingChanged += OnSettingChanged;
            category = State.Run.GameName + State.Run.CategoryName;

            resetOrNotCalculator = new ResetOrNotCalculator(State, Settings);

            state.OnSplit += OnSplit;
            state.OnReset += OnReset;
            state.OnSkipSplit += OnSkipSplit;
            state.OnUndoSplit += OnUndoSplit;
            state.OnStart += OnStart;
            state.RunManuallyModified += OnRunManuallyModified;

            Recalculate();
        }

        private void OnRunManuallyModified(object sender, EventArgs e)
        {
            Recalculate();
        }

        private void OnSettingChanged(object sender, EventArgs e)
        {
            Recalculate();
        }

        private void OnStart(object sender, EventArgs e)
        {
            Recalculate();
        }

        protected void OnUndoSplit(object sender, EventArgs e)
        {
            Recalculate();
        }

        protected void OnSkipSplit(object sender, EventArgs e)
        {
            Recalculate();
        }

        protected void OnReset(object sender, TimerPhase value)
        {
            Recalculate();
        }

        protected void OnSplit(object sender, EventArgs e)
        {
            Recalculate();
        }

        protected async void Recalculate()
        {
            ResetAction shouldReset = await resetOrNotCalculator.ShouldReset();
            string resultText = "";
            switch (shouldReset)
            {
                case ResetAction.CONTINUE_RUN:
                    resultText = "Continue the run";
                    break;
                case ResetAction.RESET:
                    resultText = "Reset";
                    break;
                case ResetAction.NOT_APPLICABLE:
                    resultText = "N/A";
                    break;
            }
            InternalComponent.InformationValue = resultText;
        }

        void IComponent.DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            PrepareDraw(state, LayoutMode.Horizontal);
            InternalComponent.DrawHorizontal(g, state, height, clipRegion);
        }

        void IComponent.DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            InternalComponent.PrepareDraw(state, LayoutMode.Vertical);
            PrepareDraw(state, LayoutMode.Vertical);
            InternalComponent.DrawVertical(g, state, width, clipRegion);
        }

        void PrepareDraw(LiveSplitState state, LayoutMode mode)
        {
            InternalComponent.NameLabel.ForeColor = state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = state.LayoutSettings.TextColor;
            InternalComponent.PrepareDraw(state, mode);
        }

        void IComponent.Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            string newCategory = State.Run.GameName + State.Run.CategoryName;
            if (newCategory != category)
            {
                Recalculate();
                category = newCategory;
            }
            
            InternalComponent.Update(invalidator, state, width, height, mode);
        }

        void IDisposable.Dispose()
        {
            
        }
    }
}
