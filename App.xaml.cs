using System.Diagnostics;

namespace Denly;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
		Trace.AutoFlush = true;

		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			var message = args.ExceptionObject?.ToString() ?? "(null)";
			Debug.WriteLine("********** UnhandledException **********");
			Debug.WriteLine(message);
			Console.WriteLine("********** UnhandledException **********");
			Console.WriteLine(message);
		};

		TaskScheduler.UnobservedTaskException += (sender, args) =>
		{
			var message = args.Exception?.ToString() ?? "(null)";
			Debug.WriteLine("********** UnobservedTaskException **********");
			Debug.WriteLine(message);
			Console.WriteLine("********** UnobservedTaskException **********");
			Console.WriteLine(message);
		};
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage()) { Title = "Denly" };
	}
}
