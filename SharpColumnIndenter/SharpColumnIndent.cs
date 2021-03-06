﻿using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;
using System.IO;
using SharpColumnIndenter.ColumnIndenter;
using SharpColumnIndenter.Languages.CSharp;
using SharpColumnIndenter.Languages.Base;

namespace SharpColumnIndenter
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SharpColumnIndent
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("3dafd2c6-604b-48d4-bd5c-b074871f2724");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharpColumnIndent"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SharpColumnIndent(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SharpColumnIndent Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
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
        public static void Initialize(Package package)
        {
            Instance = new SharpColumnIndent(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            DTE dte = (DTE)ServiceProvider.GetService(typeof(DTE));
            if (dte.ActiveDocument is null)
            {
                ShowMessage("Oops!","Please select some text in active document.");
                return;
            }

            Indenter columnIndenter;            
            string fileExtension = Path.GetExtension(dte.ActiveDocument.FullName).ToLower().Remove(0, 1);
            switch (fileExtension)
            {
                case "cs":
                    columnIndenter= new Indenter(new CSharpLanguage());
                    break;

                default:
                    columnIndenter = new Indenter(new BaseLanguage());
                    break;
            }
            
            var textDocument = dte.ActiveDocument.Object() as TextDocument;
            if (textDocument is null || textDocument.Selection is null || string.IsNullOrWhiteSpace(textDocument.Selection.Text))
            {
                ShowMessage("Oops!", "Please select some text in active document.");
                return;
            }
            var selectedText = textDocument.Selection.Text;
            var editPoint = textDocument.CreateEditPoint(textDocument.Selection.TopPoint);
            editPoint.ReplaceText(textDocument.Selection.BottomPoint, columnIndenter.Apply(selectedText),0);
        }

        private void ShowMessage(string title, string message)
        {
            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
