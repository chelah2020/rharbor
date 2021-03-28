﻿using kenzauros.RHarbor.Models;
using kenzauros.RHarbor.Properties;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace kenzauros.RHarbor.ViewModels
{
    internal class ExternalProgramDefinitionSettingsViewModel : CompositeDisposableViewModelBase
    {
        protected MainWindowViewModel MainWindow => MainWindowViewModel.Singleton;

        public ObservableCollection<ExternalProgramDefinition> Items { get; } = new();

        public ReactiveProperty<ExternalProgramDefinition> SelectedItem { get; } = new();
        public ReadOnlyReactiveProperty<bool> IsNotItemEditing { get; }
        public ReadOnlyReactiveProperty<bool> IsItemEditing => IsItemSelected;
        public ReadOnlyReactiveProperty<bool> IsItemSelected { get; }

        public ReactiveCommand<string> AddNewItemCommand { get; } = new();
        public ReactiveCommand RemoveCommand { get; }
        public ReactiveCommand SelectExePathCommand { get; }

        /// <summary>
        /// Items to be removed on save
        /// </summary>
        private readonly HashSet<ExternalProgramDefinition> RemovingItems = new();

        public ExternalProgramDefinitionSettingsViewModel()
        {
            IsItemSelected = SelectedItem.Select(x => x != null).ToReadOnlyReactiveProperty();
            IsNotItemEditing = IsItemSelected.Inverse().ToReadOnlyReactiveProperty();

            AddNewItemCommand.Subscribe(param =>
            {
                SelectedItem.Value = param switch
                {
                    "TeraTerm" => ExternalProgramDefinition.CreateTeraTermDefinition(),
                    "RLogin" => ExternalProgramDefinition.CreateRLoginDefinition(),
                    _ => new()
                    {
                        Name = "No Name",
                    },
                };
                Items.Add(SelectedItem.Value);
            }).AddTo(Disposable);

            RemoveCommand = IsItemSelected.ToReactiveCommand();
            RemoveCommand.Subscribe(() =>
            {
                var item = SelectedItem.Value;
                if (Items.Remove(item))
                {
                    RemovingItems.Add(item);
                    SelectedItem.Value = null;
                }
            }).AddTo(Disposable);

            SelectExePathCommand = IsItemEditing.ToReactiveCommand();
            SelectExePathCommand.Subscribe(() =>
            {
                if (SelectedItem.Value is null) return;
                var openFileDialog = new OpenFileDialog
                {
                    FilterIndex = 1,
                    Filter = "Executable Files (*.exe)|*.exe",
                };
                if (!string.IsNullOrWhiteSpace(SelectedItem.Value.ExePath)
                    && Directory.Exists(Path.GetDirectoryName(SelectedItem.Value.ExePath)))
                {
                    openFileDialog.InitialDirectory = Path.GetDirectoryName(SelectedItem.Value.ExePath);
                }
                if (openFileDialog.ShowDialog() == true)
                {
                    SelectedItem.Value.ExePath = openFileDialog.FileName;
                }
            }).AddTo(Disposable);
        }

        public void ResetItems(IEnumerable<ExternalProgramDefinition> items)
        {
            SelectedItem.Value = null;
            Items.Clear();
            foreach (var item in items.Select(x => x.CloneDeep()))
            {
                Items.Add(item);
            }
        }

        public async Task SaveChanges()
        {
            MyLogger.Log($"Saving {nameof(ExternalProgramDefinition)}s...");
            try
            {
                var set = MainWindow.DbContext.ExternalProgramDefinitions;
                foreach (var item in RemovingItems)
                {
                    var currentItem = set.FirstOrDefault(x => x.Id == item.Id);
                    if (currentItem != null)
                    {
                        set.Remove(currentItem); // delete
                    }
                }
                foreach (var item in Items)
                {
                    var currentItem = set.FirstOrDefault(x => x.Id == item.Id);
                    if (currentItem == null)
                    {
                        set.Add(item); // insert
                    }
                    else
                    {
                        currentItem.RewriteWith(item); // update
                    }
                }
                await MainWindow.DbContext.SaveChangesAsync().ConfigureAwait(false);
                MainWindow.SSHConnectionInfos.SetExternalProgramDefinitions(set.ToList());
                MyLogger.Log($"Saved {nameof(ExternalProgramDefinition)}s...");
            }
            catch (Exception ex)
            {
                MyLogger.Log($"Failed to save {nameof(ExternalProgramDefinition)}s...", ex);
                if (ex is System.Data.Entity.Validation.DbEntityValidationException dbex)
                {
                    var messages = dbex.EntityValidationErrors.SelectMany(x => x.ValidationErrors.Select(x => $"{x.PropertyName}: {x.ErrorMessage}"));
                    throw new Exception(Resources.ExternalProgramDefinition_Exception_ValidationException + "\n" + string.Join("\n", messages));
                }
                throw;
            }
        }
    }
}
