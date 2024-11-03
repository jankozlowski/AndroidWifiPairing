using AndroidMultipleDeviceLauncher.Services;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Task = System.Threading.Tasks.Task;

namespace AndroidWifiPairing
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ConnectCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4177;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("1096f233-40f3-4559-aa1a-ac770634cecb");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ConnectCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ConnectCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ConnectCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ConnectCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        /// 

        private void Execute(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(PairingSettings.ConnectionIP))
                System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "No connection data submited"), "No connection", MessageBoxButton.OK);

            if (string.IsNullOrEmpty(PairingSettings.ConnectionPort))
                System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "No port data submited"), "No port", MessageBoxButton.OK);

            Adb adb = new Adb();
            var result = adb.AdbCommandWithResult($"connect {PairingSettings.ConnectionIP}:{PairingSettings.ConnectionPort}");

            System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, result), "Connection result", MessageBoxButton.OK);

        }
    }
}
