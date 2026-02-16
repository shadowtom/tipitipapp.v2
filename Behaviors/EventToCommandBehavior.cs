using System.Reflection;
using System.Windows.Input;

namespace tipitipapp.Behaviors;

public class EventToCommandBehavior : Behavior<VisualElement>
{
    public static readonly BindableProperty EventNameProperty =
        BindableProperty.Create(nameof(EventName), typeof(string), typeof(EventToCommandBehavior));

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(EventToCommandBehavior));

    public string EventName
    {
        get => (string)GetValue(EventNameProperty);
        set => SetValue(EventNameProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        // Find and subscribe to the event
        var eventInfo = bindable.GetType().GetRuntimeEvent(EventName);
        if (eventInfo != null)
        {
            var methodInfo = typeof(EventToCommandBehavior).GetMethod(nameof(OnEvent),
                BindingFlags.NonPublic | BindingFlags.Instance);
            var handler = methodInfo.CreateDelegate(eventInfo.EventHandlerType, this);
            eventInfo.AddEventHandler(bindable, handler);
        }
    }

    private void OnEvent(object sender, EventArgs e)
    {
        Command?.Execute(e);
    }
}