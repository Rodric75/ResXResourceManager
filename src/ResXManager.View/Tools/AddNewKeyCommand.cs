﻿namespace ResXManager.View.Tools
{
    using System;
    using System.Composition;
    using System.Linq;
    using System.Windows;

    using ResXManager.View.Properties;
    using ResXManager.View.Visuals;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf;

    [Export, Shared]
    internal class AddNewKeyCommand : DelegateCommand<DependencyObject>
    {
        private readonly ResourceViewModel _resourceViewModel;
        private readonly IExportProvider _exportProvider;

        [ImportingConstructor]
        public AddNewKeyCommand(ResourceViewModel resourceViewModel, IExportProvider exportProvider)
        {
            _resourceViewModel = resourceViewModel;
            _exportProvider = exportProvider;

            ExecuteCallback = InternalExecute;
        }

        private void InternalExecute(DependencyObject? parameter)
        {
            if (_resourceViewModel.SelectedEntities.Count != 1)
            {
                MessageBox.Show(Resources.NeedSingleEntitySelection, Resources.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var resourceFile = _resourceViewModel.SelectedEntities.Single();

            if (resourceFile.IsWinFormsDesignerResource)
            {
                if (MessageBox.Show(Resources.AddEntryToWinFormsResourceWarning, Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    return;
            }

            if (!resourceFile.CanEdit(null))
                return;

            var application = Application.Current;

            var owner = parameter != null ? Window.GetWindow(parameter) : application.MainWindow;

            var inputBox = new InputBox(_exportProvider)
            {
                Title = Resources.Title,
                Prompt = Resources.NewKeyPrompt,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            inputBox.TextChanged += (_, args) =>
            {
                var text = args?.Text;

                inputBox.IsInputValid = text != null &&
                                        !string.IsNullOrWhiteSpace(text)
                                        && !resourceFile.Entries.Any(entry => entry.Key.Equals(text, StringComparison.OrdinalIgnoreCase))
                                        && !text.Equals(resourceFile.BaseName, StringComparison.OrdinalIgnoreCase);
            };

            if (inputBox.ShowDialog() != true)
                return;

            var key = inputBox.Text ?? string.Empty;
            key = key.Trim();

            try
            {
                _resourceViewModel.AddNewKey(resourceFile, key);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Resources.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}