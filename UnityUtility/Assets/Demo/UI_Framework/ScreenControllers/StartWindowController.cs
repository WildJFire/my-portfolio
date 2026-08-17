using Utils;

namespace UIFramework.Examples
{
    public class StartDemoSignal : ASignal { }

    public class StartWindowController : WindowController
    {
        public void UI_Start()
        {
            Signals.Get<StartDemoSignal>().Dispatch();
        }
    }
}
