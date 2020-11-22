using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;

[assembly: ComponentFactory(typeof(ResetOrNot.UI.Components.ResetOrNotFactory))]

namespace ResetOrNot.UI.Components
{
    class ResetOrNotFactory : IComponentFactory
    {
        public string ComponentName => "Reset Or Not";

        public string Description => "Advices you if you should reset your run.";

        public ComponentCategory Category => ComponentCategory.Information;

        public IComponent Create(LiveSplitState state) => new ResetOrNotComponent(state);

        public string UpdateName => ComponentName;

        public string XMLURL => "http://livesplit.org/update/Components/update.ResetOrNot.xml";

        public string UpdateURL => "http://livesplit.org/update/";

        public Version Version => Version.Parse("0.1.1");
    }
}
